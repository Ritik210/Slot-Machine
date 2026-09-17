using System.Collections;
using UnityEngine;

/// <summary>
/// Playback director: turns one SpinResult into a timed sequence of events
/// (reel stops, win highlights, tumble drops, free-spin chaining).
/// </summary>
public class SpinResultVisualizer : MonoBehaviour
{
    [Header("Timing Settings (base values at 1x speed)")]
    public float reelStopDelay = 0.05f;
    public float winDelay = 0.5f;
    public float winLineStepDelay = 0.15f;
    public float spinEndDelay = 0.1f;
    public float totalSpinTime = 0.1f;

    [Header("Tumble Timing (base values at 1x speed)")]
    public float tumbleWinHighlightDuration = 0.8f;
    public float tumbleHighlightFadeWait = 0.2f;
    public float tumbleRemoveDelay = 0.3f;
    public float tumbleDropPause = 0.15f;
    public float tumbleBetweenDelay = 0.2f;

    [SerializeField] private Reelmanager reelManager;
    [SerializeField] private SlotUIManager uiManager;

    /// <summary> Shorthand: scales a duration by current speed multiplier. </summary>
    private float S(float t) => SpinSpeedController.Apply(t);

    public void Visualize(SpinResult result)
    {
        StartCoroutine(VisualSequence(result));
    }

    public IEnumerator VisualSequence(SpinResult result)
    {
        // ── 1. Spin start ──
        SlotEventHub.Instance.Raise(new SpinStartEvent());
        yield return new WaitForSeconds(S(totalSpinTime));

        // ── 2. Screen to show when reels stop (pre-cascade screen if tumbles happened) ──
        var landingScreen = result.HasTumbles ? result.tumbles[0].landed : result.screen;

        // ── 3. Reels stop one-by-one ──
        for (int i = 0; i < landingScreen.Count; i++)
        {
            SlotEventHub.Instance.Raise(new ReelStopEvent(i, landingScreen[i]));
            yield return new WaitForSeconds(S(reelStopDelay));
        }

        // ── 4. Tumble cascade or plain win display ──
        if (result.HasTumbles)
        {
            yield return StartCoroutine(TumbleCascadeSequence(result));
        }
        else
        {
            uiManager.OnWinCollected(result);
            yield return new WaitForSeconds(S(winDelay));

            foreach (var winLine in result.winLines)
            {
                SlotEventHub.Instance.Raise(new WinLineEvent(winLine));
                yield return new WaitForSeconds(S(winLineStepDelay));
            }
        }

        // ── 5. Free spin chaining ──
        if (result.isFreeSpin)
        {
            SlotEventHub.Instance.Raise(new FreeSpinEvent(
                result.maxFreeSpin - result.currentFreeSpin, result.isLastFreeSpin));
        }

        // ── 6. Done ──
        yield return new WaitForSeconds(S(spinEndDelay));
        SlotEventHub.Instance.Raise(new SpinEndEvent(result));
    }

    private IEnumerator TumbleCascadeSequence(SpinResult result)
    {
        double runningWin = 0;
        double baseBalance = result.balanceBeforeSpin; // bet already deducted
        int emptyId = Server.EmptySymbolId;

        for (int t = 0; t < result.tumbles.Count; t++)
        {
            TumbleInfo tumble = result.tumbles[t];

            // A. Highlight winning symbols
            foreach (var winLine in tumble.lines)
                SlotEventHub.Instance.Raise(new WinLineEvent(winLine));

            runningWin += tumble.wins;
            uiManager.OnTumbleWinUpdate(tumble.wins, runningWin, baseBalance);
            yield return new WaitForSeconds(S(tumbleWinHighlightDuration));

            // B. Fade highlights
            SlotEventHub.Instance.Raise(new TumbleClearEvent());
            yield return new WaitForSeconds(S(tumbleHighlightFadeWait));

            // C. Remove winning symbols (show holes)
            reelManager.DisplayScreenWithEmpty(tumble.withHoles, emptyId);
            yield return new WaitForSeconds(S(tumbleRemoveDelay));

            // D. Gravity drop + refill together
            yield return StartCoroutine(
                reelManager.AnimateCombinedDrop(tumble.withHoles, tumble.settled, tumble.merged, emptyId));
            yield return new WaitForSeconds(S(tumbleDropPause));

            // E. Pause before the next cascade
            if (t < result.tumbles.Count - 1)
                yield return new WaitForSeconds(S(tumbleBetweenDelay));
        }

        // Final sync with the authoritative server balance
        uiManager.OnWinCollected(result);
    }
}
