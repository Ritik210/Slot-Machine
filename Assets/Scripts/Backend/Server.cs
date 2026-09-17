using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using UnityEngine;

/// <summary>
/// Game-logic "server": parses the XML config, generates spin outcomes
/// (reel set selection, stop positions, Ways wins, tumble cascades, scatter /
/// free-spin logic) and tracks RTP statistics. Contains no rendering code.
/// </summary>
public class Server : MonoBehaviour
{
    [Header("Simulation")]
    public bool simulationMode = false;
    public int sampleSize = 100000;

    [Header("Config")]
    public TextAsset configFile;

    [Header("Grid")]
    [SerializeField] int numberOfReels = 6;
    [SerializeField] int slotHeight = 4;

    [Header("Player")]
    [SerializeField] double balance = 5000;
    private double balanceBeforeSpin = 0;
    private double startingBalance = 0;

    // ── Symbol IDs (must match the order of <Paytable> in the XML) ──
    public static readonly string[] SymbolNames =
        { "B", "SCATTER", "Wild", "High1", "High2", "High3", "High4", "Low1", "Low2", "Low3", "Low4", "EMPTY" };
    public const int ScatterSymbolId = 1;
    public const int WildSymbolId = 2;
    public const int EmptySymbolId = 11;   // marks a removed cell during a tumble

    private ISlotMode mode;
    private Tumbler tumbler;

    // ── Bet ──
    private int baseBet = 0;              // from XML, paytable calibrated for this
    private int bet = 0;                  // current bet in credits
    private double betMultiplier = 1.0;   // bet / baseBet — scales all wins
    private string version = "";

    // ── Config data ──
    private List<List<List<int>>> reelSetsList = null;
    private List<List<int>> payoutData = null;
    private List<int> baseReelSelectionValues = null;
    private List<int> baseReelSelectionWeights = null;
    private List<int> freeReelSelectionValues = null;
    private List<int> freeReelSelectionWeights = null;
    private int[] freeSpinCounts = null;        // index = scatter count
    private int[] additionalFreeSpinCounts = null;
    private int[] scatterPayTable = null;

    // ── Screen state ──
    public List<List<int>> screenSymbols = new List<List<int>>();
    private List<List<int>> currentReelSymbols = new List<List<int>>();
    public List<int> stopPositions = new List<int>();
    private List<WinLineInfo> winLines = new List<WinLineInfo>();
    private int currentReelSet = 0;

    // ── Per-spin ──
    private int spinCount = 0;
    private double totalWin = 0;
    private int scatterCount = 0;
    private const int ScatterThreshold = 3;
    private double scatterPay = 0;

    // ── Free spins ──
    private int currentFreeSpin = 0;
    private bool initFS = false;
    private int maxFreeSpins = 0;
    private bool isFreeSpinEnded = false;
    private double freeSpinTotalWin = 0;

    // ── Cheat ──
    private bool forceFreeSpin = false;

    // ── Stats ──
    private int totalBaseSpins = 0;
    private double totalWagers = 0;
    private double totalWins = 0;
    private double baseWins = 0;
    private double fsWins = 0;
    private double scatterTotalPay = 0;
    private int baseGameWinCount = 0;
    private int totalFSSpinsPlayed = 0;
    private int fsWinCount = 0;
    private int freeSpinsSessions = 0;
    private int freeRetriggerSessions = 0;
    private double maxSingleSpinWin = 0;
    private double maxFSSessionWin = 0;
    private int maxCascadeDepth = 0;
    private long totalCascades = 0;
    private int cascadeSpinCount = 0;

    void Start()
    {
        ParseConfig();
        InitGame();

        if (simulationMode)
            RunSimulation();
    }

    private void InitGame()
    {
        mode = new WaysUtil(numberOfReels, slotHeight, this);
        tumbler = new Tumbler(numberOfReels, slotHeight, EmptySymbolId, this);
        balanceBeforeSpin = balance;
        startingBalance = balance;

        for (int i = 0; i < numberOfReels; i++)
        {
            screenSymbols.Add(new List<int>());
            for (int j = 0; j < slotHeight; j++)
                screenSymbols[i].Add(0);
        }
    }

    #region Parsing

    private void ParseConfig()
    {
        XmlDocument config = new XmlDocument();
        config.LoadXml(configFile.ToString());

        XmlNode node = config.SelectSingleNode("/config");
        version = node.Attributes["version"].Value;

        XmlNode betNode = config.SelectSingleNode("/config/Bet");
        baseBet = int.Parse(betNode.Attributes["value"].Value);
        bet = baseBet;
        betMultiplier = 1.0;

        ParsePaytable(config.SelectNodes("/config/Paytable/Symbol"));
        ParseReelSets(config.SelectNodes("/config/ReelSets/ReelSet"));

        freeSpinCounts = ParseIntList(config.SelectSingleNode("/config/Scatter_settings/Spins"), "values").ToArray();
        additionalFreeSpinCounts = ParseIntList(config.SelectSingleNode("/config/Scatter_settings/AdditionalSpins"), "values").ToArray();
        scatterPayTable = ParseIntList(config.SelectSingleNode("/config/Scatter_settings/ScatterPay"), "values").ToArray();

        XmlNode baseSel = config.SelectSingleNode("/config/ReelSet_Selection/baseReelSelection");
        baseReelSelectionValues = ParseIntList(baseSel, "values");
        baseReelSelectionWeights = ParseIntList(baseSel, "weights");

        XmlNode freeSel = config.SelectSingleNode("/config/ReelSet_Selection/freeReelSelection");
        freeReelSelectionValues = ParseIntList(freeSel, "values");
        freeReelSelectionWeights = ParseIntList(freeSel, "weights");
    }

    private static List<int> ParseIntList(XmlNode node, string attribute)
    {
        return node.Attributes[attribute].Value
            .Split(',')
            .Select(s => int.Parse(s.Trim()))
            .ToList();
    }

    private void ParsePaytable(XmlNodeList symbols)
    {
        payoutData = new List<List<int>>();
        foreach (XmlNode symbol in symbols)
            payoutData.Add(ParseIntList(symbol, "payout"));
    }

    private void ParseReelSets(XmlNodeList reelSetNodes)
    {
        reelSetsList = new List<List<List<int>>>();
        foreach (XmlNode reelSetNode in reelSetNodes)
        {
            List<List<int>> reelSet = new List<List<int>>();
            foreach (XmlNode reelNode in reelSetNode.SelectNodes("Reel"))
            {
                List<int> reel = new List<int>();
                string raw = reelNode.InnerText.Replace("\"", "").Trim();
                foreach (string symbol in raw.Split(','))
                {
                    int symbolIndex = Array.IndexOf(SymbolNames, symbol.Trim());
                    if (symbolIndex < 0)
                        Debug.LogError($"[Server] Unknown symbol '{symbol}' in reel strip.");
                    reel.Add(symbolIndex);
                }
                reelSet.Add(reel);
            }
            reelSetsList.Add(reelSet);
        }
    }

    #endregion

    #region Spin helpers

    private int GenerateWeightedRandomNumber(List<int> values, List<int> weights)
    {
        int weightSum = 0;
        for (int i = 0; i < weights.Count; i++)
            weightSum += weights[i];

        int r = UnityEngine.Random.Range(0, weightSum);
        int sum = 0;
        for (int i = 0; i < weights.Count; i++)
        {
            sum += weights[i];
            if (r < sum)
                return (values != null) ? values[i] : i;
        }
        return -1;
    }

    private void ChangeReelSet(int reelSetId)
    {
        currentReelSet = reelSetId;
        currentReelSymbols = new List<List<int>>(reelSetsList[reelSetId]);
    }

    private void GenerateStopPositions()
    {
        stopPositions = new List<int>();
        for (int i = 0; i < numberOfReels; i++)
            stopPositions.Add(UnityEngine.Random.Range(0, currentReelSymbols[i].Count));
    }

    private List<List<int>> ComputeScreenSymbols(List<List<int>> reelSymbolsList)
    {
        List<List<int>> computed = new List<List<int>>();
        for (int i = 0; i < screenSymbols.Count; i++)
        {
            computed.Add(new List<int>());
            for (int j = 0; j < screenSymbols[i].Count; j++)
            {
                int idx = (stopPositions[i] + j) % reelSymbolsList[i].Count;
                computed[i].Add(reelSymbolsList[i][idx]);
            }
        }
        return computed;
    }

    /// <summary>Evaluates the current screen and prices every win (used by the Tumbler each cascade step).</summary>
    public List<WinLineInfo> ComputeWinsForScreen()
    {
        winLines = mode.ComputeScreenWins();
        foreach (var win in winLines)
            win.win = payoutData[win.dominatingSymbol][win.Positions.Length - 1] * betMultiplier;
        return winLines;
    }

    #endregion

    #region Free spins

    public bool IsFreeSpins() => currentFreeSpin > 0;
    public bool IsLastFreeSpin() => currentFreeSpin > maxFreeSpins;

    private void InitFreeSpins(int scatters)
    {
        initFS = true;
        currentFreeSpin = 0;
        maxFreeSpins = freeSpinCounts[Mathf.Clamp(scatters, 0, freeSpinCounts.Length - 1)];
        freeSpinTotalWin = 0;
        freeSpinsSessions++;
    }

    private void AwardAdditionalFreeSpins(int scatters)
    {
        freeRetriggerSessions++;
        maxFreeSpins += additionalFreeSpinCounts[Mathf.Clamp(scatters, 0, additionalFreeSpinCounts.Length - 1)];
    }

    private void ResetFreeSpins()
    {
        initFS = false;
        currentFreeSpin = -1;
        maxFreeSpins = 0;
        ChangeReelSet(0);
    }

    #endregion

    public SpinResult GenerateSpinResponse()
    {
        // ── Bet / balance ──
        if (!IsFreeSpins())
        {
            totalBaseSpins++;
            totalWagers += bet;
            double betInDollars = (double)bet / baseBet;
            balance -= betInDollars;
            balanceBeforeSpin = balance;
            spinCount++;
        }
        else
        {
            balanceBeforeSpin = balance;
        }

        // ── Reel set selection ──
        int reelSet = IsFreeSpins()
            ? GenerateWeightedRandomNumber(freeReelSelectionValues, freeReelSelectionWeights)
            : GenerateWeightedRandomNumber(baseReelSelectionValues, baseReelSelectionWeights);

        isFreeSpinEnded = false;
        winLines = new List<WinLineInfo>();
        totalWin = 0;
        scatterCount = 0;
        scatterPay = 0;
        initFS = false;

        ChangeReelSet(reelSet);
        GenerateStopPositions();
        screenSymbols = ComputeScreenSymbols(currentReelSymbols);

        // ── Cheat: force a free-spin trigger (one shot) ──
        if (forceFreeSpin && !IsFreeSpins())
        {
            ForceScattersOnScreen();
            forceFreeSpin = false;
        }

        // ── Tumble cascade: remove wins → drop → refill → re-evaluate ──
        List<TumbleInfo> tumbles = tumbler.Do(simulationMode, reelSetsList[currentReelSet]);
        double tumbleWin = tumbler.GetWins(tumbles);
        totalWin += tumbleWin;

        // ── Scatters ──
        scatterCount = CountSymbols(ScatterSymbolId, screenSymbols);
        if (scatterCount >= ScatterThreshold)
        {
            int payIdx = Mathf.Clamp(scatterCount, 0, scatterPayTable.Length - 1);
            scatterPay = scatterPayTable[payIdx] * betMultiplier;
            scatterTotalPay += scatterPay;

            if (!IsFreeSpins())
                InitFreeSpins(scatterCount);
            else
                AwardAdditionalFreeSpins(scatterCount);
        }

        totalWin += scatterPay;             // scatter award is part of the spin's win
        double spinTotalWin = totalWin;

        // ── Stats ──
        if (tumbles.Count > 0)
        {
            totalCascades += tumbles.Count;
            cascadeSpinCount++;
            maxCascadeDepth = Mathf.Max(maxCascadeDepth, tumbles.Count);
        }
        if (spinTotalWin > maxSingleSpinWin)
            maxSingleSpinWin = spinTotalWin;

        if (IsFreeSpins() || initFS)
        {
            currentFreeSpin++;
            totalFSSpinsPlayed++;
            freeSpinTotalWin += spinTotalWin;
            if (spinTotalWin > 0) fsWinCount++;
        }
        else if (spinTotalWin > 0)
        {
            baseGameWinCount++;
        }

        if (IsFreeSpins() && currentFreeSpin > 1)
            fsWins += spinTotalWin;
        else
            baseWins += spinTotalWin;

        totalWins += spinTotalWin;
        balance += spinTotalWin / baseBet;

        if (IsLastFreeSpin())
        {
            isFreeSpinEnded = true;
            if (freeSpinTotalWin > maxFSSessionWin)
                maxFSSessionWin = freeSpinTotalWin;
            ResetFreeSpins();
        }

        return BuildResponse(tumbles, tumbleWin);
    }

    #region Public API

    public void ResetGame()
    {
        balance = startingBalance;
        balanceBeforeSpin = balance;

        spinCount = 0; totalWin = 0; totalWins = 0; totalWagers = 0; totalBaseSpins = 0;
        baseWins = 0; fsWins = 0; scatterTotalPay = 0;
        baseGameWinCount = 0; totalFSSpinsPlayed = 0; fsWinCount = 0;
        maxSingleSpinWin = 0; maxFSSessionWin = 0;
        maxCascadeDepth = 0; totalCascades = 0; cascadeSpinCount = 0;

        currentFreeSpin = 0; initFS = false; maxFreeSpins = 0;
        freeSpinsSessions = 0; freeRetriggerSessions = 0;
        isFreeSpinEnded = false; freeSpinTotalWin = 0;
        scatterCount = 0; scatterPay = 0;
        forceFreeSpin = false;

        for (int i = 0; i < numberOfReels; i++)
            for (int j = 0; j < slotHeight; j++)
                screenSymbols[i][j] = 0;

        ChangeReelSet(0);
    }

    public double GetStartingBalance() => startingBalance;
    public int GetReelSymbol(int row, int reel) => screenSymbols[reel][row];
    public bool GetSymbolIsWild(int id) => id == WildSymbolId;
    public List<List<int>> GetPayoutData() => payoutData;
    public double GetBetMultiplier() => betMultiplier;
    public int GetBaseBet() => baseBet;

    /// <summary>Called by BetController. dollarBet e.g. 0.5, 1, 2, 10 — 1$ = baseBet credits.</summary>
    public void SetBet(float dollarBet)
    {
        bet = Mathf.RoundToInt(dollarBet * baseBet);
        betMultiplier = (double)bet / baseBet;
    }

    public double GetBetDollars() => (double)bet / baseBet;

    /// <summary>Toggle forced free-spin trigger (0 = off, 1 = on). Wire to a UI dropdown.</summary>
    public void SetForceFreeSpin(int value) => forceFreeSpin = (value == 1);

    #endregion

    private void ForceScattersOnScreen()
    {
        int target = UnityEngine.Random.Range(ScatterThreshold, numberOfReels + 1);
        int existing = CountSymbols(ScatterSymbolId, screenSymbols);

        List<int> available = new List<int>();
        for (int reel = 0; reel < numberOfReels; reel++)
            for (int row = 0; row < slotHeight; row++)
                if (screenSymbols[reel][row] != ScatterSymbolId)
                    available.Add(reel * slotHeight + row);

        for (int i = 0; i < target - existing && available.Count > 0; i++)
        {
            int idx = UnityEngine.Random.Range(0, available.Count);
            int pos = available[idx];
            screenSymbols[pos / slotHeight][pos % slotHeight] = ScatterSymbolId;
            available.RemoveAt(idx);
        }
    }

    private int CountSymbols(int id, List<List<int>> symbols)
    {
        int count = 0;
        foreach (var reel in symbols)
            foreach (var s in reel)
                if (s == id) count++;
        return count;
    }

    private SpinResult BuildResponse(List<TumbleInfo> tumbles, double tumbleWin)
    {
        SpinResult r = new SpinResult(screenSymbols, totalWin, winLines, IsFreeSpins(), IsLastFreeSpin(), currentFreeSpin, maxFreeSpins);

        r.betAmount = bet;
        r.balanceBeforeSpin = balanceBeforeSpin;
        r.balanceAfterSpin = balance;
        r.winAmount = totalWin;
        r.spinCount = spinCount;
        r.triggeredFreeSpinIntro = initFS;
        r.isFreeSpinEnded = isFreeSpinEnded;
        r.freeSpinTotalWin = freeSpinTotalWin;

        r.scatterCount = scatterCount;
        r.scatterPay = scatterPay;
        r.isRetrigger = IsFreeSpins() && scatterCount >= ScatterThreshold && !initFS;

        r.tumbles = tumbles;
        r.tumbleTotalWin = tumbleWin;

        // ── Live stats ──
        double wager = (totalWagers > 0) ? totalWagers : 1;
        r.stats_BaseRTP = (baseWins / wager) * 100;
        r.stats_FSRTP = (fsWins / wager) * 100;
        r.stats_ScatterRTP = (scatterTotalPay / wager) * 100;
        r.stats_TotalRTP = ((baseWins + fsWins) / wager) * 100;

        r.stats_FSHitRate = (freeSpinsSessions > 0) ? (double)totalBaseSpins / freeSpinsSessions : 0;
        r.stats_FSRetriggerRate = (freeRetriggerSessions > 0) ? (double)totalFSSpinsPlayed / freeRetriggerSessions : 0;
        r.stats_FSSessions = freeSpinsSessions;
        r.stats_FSRetriggers = freeRetriggerSessions;
        r.stats_TotalFSSpins = totalFSSpinsPlayed;
        r.stats_AvgFSSessionWin = (freeSpinsSessions > 0) ? fsWins / freeSpinsSessions : 0;

        r.stats_BaseWinHitRate = (totalBaseSpins > 0) ? (double)baseGameWinCount / totalBaseSpins * 100 : 0;
        r.stats_FSWinHitRate = (totalFSSpinsPlayed > 0) ? (double)fsWinCount / totalFSSpinsPlayed * 100 : 0;

        r.stats_MaxSingleSpinWin = maxSingleSpinWin;
        r.stats_MaxFSSessionWin = maxFSSessionWin;
        r.stats_MaxCascadeDepth = maxCascadeDepth;
        r.stats_AvgCascadeDepth = (cascadeSpinCount > 0) ? (double)totalCascades / cascadeSpinCount : 0;

        return r;
    }

    private void RunSimulation()
    {
        int savedBet = bet;
        double savedMultiplier = betMultiplier;
        bet = baseBet;
        betMultiplier = 1.0;

        DateTime startTime = DateTime.Now;
        for (long i = 0; i < sampleSize; i++)
        {
            GenerateSpinResponse();
            if (IsFreeSpins()) { i--; continue; }   // free spins don't count as sampled base spins
        }
        Debug.LogWarning("Simulation time = " + (DateTime.Now - startTime));

        bet = savedBet;
        betMultiplier = savedMultiplier;

        double wager = (totalWagers > 0) ? totalWagers : 1;
        double baseRTP = (baseWins / wager) * 100;
        double fsRTP = (fsWins / wager) * 100;
        double scatterRTP = (scatterTotalPay / wager) * 100;

        Debug.Log("═══════════════ SIMULATION RESULTS ═══════════════");
        Debug.Log("Total Base Spins: " + totalBaseSpins);
        Debug.Log("Total Wager: " + totalWagers);
        Debug.Log("Total Wins: " + totalWins.ToString("F2"));
        Debug.Log("─── RTP Breakdown ───");
        Debug.Log("Base Game RTP: " + baseRTP.ToString("F3") + "%");
        Debug.Log("Free Spins RTP: " + fsRTP.ToString("F3") + "%");
        Debug.Log("  (of which Scatter pay: " + scatterRTP.ToString("F3") + "%)");
        Debug.Log("TOTAL RTP: " + (baseRTP + fsRTP).ToString("F3") + "%");
        Debug.Log("─── Hit Rates ───");
        Debug.Log("Base Win Hit Rate: " + (totalBaseSpins > 0 ? ((double)baseGameWinCount / totalBaseSpins * 100).ToString("F2") : "0") + "%");
        Debug.Log("FS Win Hit Rate: " + (totalFSSpinsPlayed > 0 ? ((double)fsWinCount / totalFSSpinsPlayed * 100).ToString("F2") : "0") + "%");
        Debug.Log("─── Free Spins ───");
        Debug.Log("FS Sessions: " + freeSpinsSessions);
        Debug.Log("FS Retriggers: " + freeRetriggerSessions);
        Debug.Log("FS Hit Rate: 1 in " + (freeSpinsSessions > 0 ? ((double)totalBaseSpins / freeSpinsSessions).ToString("F1") : "N/A"));
        Debug.Log("FS Retrigger Rate: 1 in " + (freeRetriggerSessions > 0 ? ((double)totalFSSpinsPlayed / freeRetriggerSessions).ToString("F1") : "N/A"));
        Debug.Log("Total FS Spins Played: " + totalFSSpinsPlayed);
        Debug.Log("Avg FS Session Win: " + (freeSpinsSessions > 0 ? (fsWins / freeSpinsSessions).ToString("F2") : "0"));
        Debug.Log("Max FS Session Win: " + maxFSSessionWin.ToString("F2") + " credits (" + (maxFSSessionWin / baseBet).ToString("F2") + "x bet)");
        Debug.Log("─── Cascades ───");
        Debug.Log("Spins With Cascades: " + cascadeSpinCount);
        Debug.Log("Avg Cascade Depth: " + (cascadeSpinCount > 0 ? ((double)totalCascades / cascadeSpinCount).ToString("F2") : "0"));
        Debug.Log("Max Cascade Depth: " + maxCascadeDepth);
        Debug.Log("Max Single Spin Win: " + maxSingleSpinWin.ToString("F2") + " credits (" + (maxSingleSpinWin / baseBet).ToString("F2") + "x bet)");
        Debug.Log("═══════════════════════════════════════════════════");
    }
}
