using System.Collections.Generic;

/// <summary>Everything the frontend needs to play back one spin.</summary>
public class SpinResult
{
    public List<List<int>> screen;          // final screen after all cascades
    public double winAmount;                // total win this spin (credits) incl. scatter pay
    public List<WinLineInfo> winLines;      // wins on the final screen (empty when cascades occurred)

    // ── Free spins ──
    public bool isFreeSpin = false;
    public bool isLastFreeSpin = false;
    public int currentFreeSpin = 0;
    public int maxFreeSpin = 0;
    public bool isFreeSpinEnded = false;
    public bool triggeredFreeSpinIntro = false;
    public double freeSpinTotalWin = 0;

    // ── Scatters ──
    public int scatterCount = 0;
    public double scatterPay = 0;
    public bool isRetrigger = false;        // 3+ scatters landed during FS (not the initial trigger)

    // ── Accounting (dollars) ──
    public double betAmount;
    public double balanceBeforeSpin;
    public double balanceAfterSpin;

    // ── Tumbles ──
    public List<TumbleInfo> tumbles;        // full cascade chain (empty list = no tumbles)
    public double tumbleTotalWin;           // sum of wins across all cascade steps
    public bool HasTumbles => tumbles != null && tumbles.Count > 0;

    // ── Live stats ──
    public int spinCount = 0;

    public double stats_BaseRTP = 0;
    public double stats_FSRTP = 0;
    public double stats_ScatterRTP = 0;
    public double stats_TotalRTP = 0;

    public double stats_FSHitRate = 0;          // 1 in X base spins
    public double stats_FSRetriggerRate = 0;    // 1 in X FS spins
    public int stats_FSSessions = 0;
    public int stats_FSRetriggers = 0;
    public int stats_TotalFSSpins = 0;
    public double stats_AvgFSSessionWin = 0;

    public double stats_BaseWinHitRate = 0;     // % of base spins that win
    public double stats_FSWinHitRate = 0;       // % of FS spins that win

    public double stats_MaxSingleSpinWin = 0;
    public double stats_MaxFSSessionWin = 0;
    public int stats_MaxCascadeDepth = 0;
    public double stats_AvgCascadeDepth = 0;

    public SpinResult(List<List<int>> screen = null, double winAmount = 0, List<WinLineInfo> winLines = null,
                      bool isFreeSpin = false, bool isLastFreeSpin = false, int currentFreeSpin = 0, int maxFreeSpin = 0)
    {
        this.screen = screen;
        this.winAmount = winAmount;
        this.winLines = winLines ?? new List<WinLineInfo>();
        this.isFreeSpin = isFreeSpin;
        this.isLastFreeSpin = isLastFreeSpin;
        this.currentFreeSpin = currentFreeSpin;
        this.maxFreeSpin = maxFreeSpin;
        this.tumbles = new List<TumbleInfo>();
    }
}
