using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Serialization;

/// <summary>
/// Passive UI sink: balance, win, free-spin counter, feature labels, popups and live stats.
/// All amounts arrive in credits and are shown in dollars (credits / base bet).
/// </summary>
public class SlotUIManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text balanceText;
    [SerializeField] private TMP_Text winText;
    [SerializeField] private TMP_Text totalWinText;
    [SerializeField] private TMP_Text freeSpinCountText;

    [SerializeField] private GameObject freeSpinEnterPanel;
    [SerializeField] private GameObject freeSpinExitPanel;
    [SerializeField] private TMP_Text featurePopUpText;
    [SerializeField] private TMP_Text freeSpinsWinText;

    [Header("Feature Texts")]
    [SerializeField] private Transform featureTextContainer;
    [SerializeField] private TMP_Text featureTextPrefab;
    private readonly List<TMP_Text> activeFeatureTexts = new List<TMP_Text>();

    [SerializeField] private float freeEnterDelay = 0.1f;

    [Header("Live Stats")]
    [SerializeField] private TMP_Text spinCountText;
    [SerializeField, FormerlySerializedAs("baseWaysRTPText")] private TMP_Text baseRTPText;
    [SerializeField, FormerlySerializedAs("fsWaysRTPText")] private TMP_Text fsRTPText;
    [SerializeField] private TMP_Text totalRTPText;
    [SerializeField] private TMP_Text fsHitRateText;
    [SerializeField] private TMP_Text maxWinText;
    [SerializeField] private TMP_Text cascadeDepthText;

    [Header("Money")]
    [SerializeField] private double creditsPerDollar = 20.0;  // must match <Bet value> in the config

    private double currentBalance;

    private void Start()
    {
        winText.text = "";
        if (totalWinText != null) totalWinText.text = "";
    }

    private double ToDollars(double credits) => credits / creditsPerDollar;

    // Called right before the spin starts — bet already deducted server-side
    public void OnSpinStart(SpinResult result)
    {
        currentBalance = result.balanceBeforeSpin;
        UpdateBalanceUI();
        winText.text = "Win: 0.00 $";
        if (totalWinText != null) totalWinText.text = "Total: 0.00 $";
    }

    // Called at spin end — final sync with the server balance
    public void OnWinCollected(SpinResult result)
    {
        double winDollars = ToDollars(result.winAmount);
        winText.text = $"Win: {winDollars:F2} $";
        if (totalWinText != null) totalWinText.text = $"Total: {winDollars:F2} $";

        currentBalance = result.balanceAfterSpin;
        UpdateBalanceUI();
    }

    /// <summary>Per cascade step: stepWin = this step's win, runningWin = accumulated (credits); baseBalance in dollars.</summary>
    public void OnTumbleWinUpdate(double stepWin, double runningWin, double baseBalance)
    {
        double totalDollars = ToDollars(runningWin);
        winText.text = $"Win: {ToDollars(stepWin):F2} $";
        if (totalWinText != null) totalWinText.text = $"Total: {totalDollars:F2} $";

        currentBalance = baseBalance + totalDollars;
        UpdateBalanceUI();
    }

    public void LiveStats(SpinResult result)
    {
        if (spinCountText != null) spinCountText.text = "Spins: " + result.spinCount;
        if (baseRTPText != null) baseRTPText.text = "Base RTP: " + result.stats_BaseRTP.ToString("F3") + "%";
        if (fsRTPText != null) fsRTPText.text = "FS RTP: " + result.stats_FSRTP.ToString("F3") + "%";
        if (totalRTPText != null) totalRTPText.text = "Total RTP: " + result.stats_TotalRTP.ToString("F3") + "%";
        if (fsHitRateText != null) fsHitRateText.text = "FS Hit: 1 in " + (result.stats_FSHitRate > 0 ? result.stats_FSHitRate.ToString("F1") : "N/A");
        if (maxWinText != null) maxWinText.text = "Max Win: " + ToDollars(result.stats_MaxSingleSpinWin).ToString("F2") + "$";
        if (cascadeDepthText != null) cascadeDepthText.text = "Avg Cascade: " + result.stats_AvgCascadeDepth.ToString("F2");
    }

    public void UpdateBalanceUI()
    {
        balanceText.text = $"Balance($): {currentBalance:F2}";
    }

    public void UpdateFsCount(int count, int max)
    {
        freeSpinCountText.text = count > 0 ? $"Freespin: {count}/{max}" : "";
    }

    public void EnableFSPopUp(SpinResult result)
    {
        StartCoroutine(ActivateFreespinEnterPopup(result));
    }

    public void DisableFSPopUp(double win)
    {
        StartCoroutine(ActivateFreespinExitPopup(win));
    }

    private IEnumerator ActivateFreespinEnterPopup(SpinResult result)
    {
        freeSpinEnterPanel.SetActive(true);
        featurePopUpText.text = $"You win {result.maxFreeSpin} Free Spins";
        yield return new WaitForSeconds(freeEnterDelay);
        freeSpinEnterPanel.SetActive(false);
    }

    private IEnumerator ActivateFreespinExitPopup(double wins)
    {
        freeSpinExitPanel.SetActive(true);
        freeSpinsWinText.text = $"Total spins win: {ToDollars(wins):F2} $";
        yield return new WaitForSeconds(freeEnterDelay);
        freeSpinCountText.text = "";
        freeSpinExitPanel.SetActive(false);
    }

    public void ShowFeature(string featureName)
    {
        TMP_Text newText = Instantiate(featureTextPrefab, featureTextContainer, false);
        newText.text = featureName.ToUpper();
        newText.gameObject.SetActive(true);
        activeFeatureTexts.Add(newText);
    }

    public void ClearAllFeatures()
    {
        foreach (var text in activeFeatureTexts)
            if (text != null) Destroy(text.gameObject);
        activeFeatureTexts.Clear();
    }

    public void ResetUI(double startingBalance)
    {
        winText.text = "";
        if (totalWinText != null) totalWinText.text = "";

        currentBalance = startingBalance;
        UpdateBalanceUI();

        if (freeSpinCountText != null) freeSpinCountText.text = "";
        ClearAllFeatures();

        if (freeSpinEnterPanel != null) freeSpinEnterPanel.SetActive(false);
        if (freeSpinExitPanel != null) freeSpinExitPanel.SetActive(false);

        if (spinCountText != null) spinCountText.text = "Spins: 0";
        if (baseRTPText != null) baseRTPText.text = "Base RTP: 0";
        if (fsRTPText != null) fsRTPText.text = "FS RTP: 0";
        if (totalRTPText != null) totalRTPText.text = "Total RTP: 0";
        if (fsHitRateText != null) fsHitRateText.text = "FS Hit: 0";
        if (maxWinText != null) maxWinText.text = "";
        if (cascadeDepthText != null) cascadeDepthText.text = "";
        if (freeSpinsWinText != null) freeSpinsWinText.text = "";
    }
}
