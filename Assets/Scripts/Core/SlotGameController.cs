using System.Collections;
using UnityEngine;

/// <summary>
/// Game loop: asks the Server for a result, hands it to the visualizer,
/// and chains free spins / auto-spins based on events.
/// </summary>
public class SlotGameController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Server server;
    [SerializeField] private SpinResultVisualizer visualizer;
    [SerializeField] private SlotUIManager uiManager;

    private bool isSpinning = false;
    private bool isAutoSpinning = false;
    private Coroutine autoSpinRoutine;

    [Header("Settings")]
    [SerializeField] private float autoSpinDelay = 0.5f;       // pause between auto spins
    [SerializeField] private float freeSpinChainDelay = 1.0f;  // pause before the next free spin starts

    private SpinResult lastResult;

    public void StartSpin()
    {
        if (isSpinning)
        {
            Debug.LogWarning("Spin already in progress.");
            return;
        }

        isSpinning = true;

        SpinResult result = server.GenerateSpinResponse();
        lastResult = result;
        uiManager.OnSpinStart(result);

        FeatureTextShow(result);

        if (result.isFreeSpin)
            FreespinCount(result);

        StartCoroutine(visualizer.VisualSequence(result));
    }

    private void OnEnable()
    {
        SlotEventHub.Instance.Subscribe<SpinEndEvent>(OnSpinComplete);
        SlotEventHub.Instance.Subscribe<FreeSpinEvent>(OnFreeSpin);
    }

    private void OnDisable()
    {
        SlotEventHub.Instance.Unsubscribe<SpinEndEvent>(OnSpinComplete);
        SlotEventHub.Instance.Unsubscribe<FreeSpinEvent>(OnFreeSpin);
    }

    private void OnSpinComplete(SpinEndEvent evt)
    {
        isSpinning = false;
        Debug.Log("Spin complete. Re-enable button or start next action.");

        var result = lastResult;

        uiManager.LiveStats(result);

        if (result.isFreeSpin && result.currentFreeSpin == 1)
        {
            uiManager.EnableFSPopUp(result);
        }


        if (result.isFreeSpinEnded)
        {
            uiManager.DisableFSPopUp(result.freeSpinTotalWin);
        }

        uiManager.ClearAllFeatures();

    }

    private void OnFreeSpin(FreeSpinEvent evt)
    {
        Debug.Log("[SlotController] Visual-triggered FreeSpin → starting next spin...");


        StartCoroutine(StartNextSpinAfterDelay(freeSpinChainDelay));
    }


    private IEnumerator StartNextSpinAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        StartSpin();
    }



    private IEnumerator StartNextAutoSpin(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (!isSpinning)
            StartSpin();
    }

    private void FeatureTextShow(SpinResult result)
    {
        uiManager.ClearAllFeatures(); // clear old texts first

        if (result.triggeredFreeSpinIntro)
            uiManager.ShowFeature("Free Spins");
        else if (result.isRetrigger)
            uiManager.ShowFeature("Retrigger");
    }

    private void FreespinCount(SpinResult result)
    {

        if (result.isFreeSpin)
        {
            uiManager.UpdateFsCount(result.currentFreeSpin - 1, result.maxFreeSpin);
        }
        else
        {
            uiManager.UpdateFsCount(0, result.maxFreeSpin);
        }

    }


    #region AutoSpin

    public void ToggleAutoSpin(bool isOn)
    {
        if (isOn)
        {
            StartAutoSpin();
        }
        else
        {
            StopAutoSpin();
        }
    }

    public void StartAutoSpin()
    {
        if (isAutoSpinning) return;

        Debug.Log("[AutoSpin] Enabled.");
        isAutoSpinning = true;
        autoSpinRoutine = StartCoroutine(AutoSpinLoop());
    }

    public void StopAutoSpin()
    {
        if (!isAutoSpinning) return;

        Debug.Log("[AutoSpin] Disabled.");
        isAutoSpinning = false;

        if (autoSpinRoutine != null)
            StopCoroutine(autoSpinRoutine);
        autoSpinRoutine = null;
    }

    private IEnumerator AutoSpinLoop()
    {
        // Loop until auto-spin manually stopped or balance too low
        while (isAutoSpinning)
        {
            if (!isSpinning)
            {
                StartSpin();
            }

            yield return new WaitForSeconds(autoSpinDelay);
        }
    }

    #endregion

    public void ResetAll()
    {
        server.ResetGame();
        uiManager.ResetUI(server.GetStartingBalance());

        lastResult = null;
    }
}