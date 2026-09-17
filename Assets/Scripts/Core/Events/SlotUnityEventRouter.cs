using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class SlotUnityEventRouter : MonoBehaviour
{
    [System.Serializable] public class SpinStartUnityEvent : UnityEvent { }
    [System.Serializable] public class ModeUnityEvent : UnityEvent { }

    [Header("Events With Delay")]
    public UnityEventWithDelay<int> OnReelStop;
    public UnityEventWithDelay<WinLineInfo> OnWinLine;
    public UnityEventWithDelay<SpinResult> OnSpinEnd;

    [Header("Game Mode Events With Delay")]
    public UnityEventWithDelay<int> OnFreeSpin;  // Remaining free spins
    public UnityEventWithDelay OnRespin;

    [Header("Enter/Exit Events")]
    public float enterFreeSpinDelay = 0f;
    public ModeUnityEvent OnEnterFreeSpin;

    public float exitFreeSpinDelay = 0f;
    public ModeUnityEvent OnExitFreeSpin;

    public float enterRespinDelay = 0f;
    public ModeUnityEvent OnEnterRespin;

    public float exitRespinDelay = 0f;
    public ModeUnityEvent OnExitRespin;

    [Header("Events Without Delay")]
    public float spinStartDelay = 0f;
    public SpinStartUnityEvent OnSpinStart;

    private void OnEnable()
    {
        SlotEventHub.Instance.Subscribe<SpinStartEvent>(e => StartCoroutine(HandleSpinStart()));
        SlotEventHub.Instance.Subscribe<ReelStopEvent>(e => OnReelStop.InvokeWithDelay(e.reelIndex, this));
        SlotEventHub.Instance.Subscribe<WinLineEvent>(e => OnWinLine.InvokeWithDelay(e.winLine, this));
        SlotEventHub.Instance.Subscribe<SpinEndEvent>(e => OnSpinEnd.InvokeWithDelay(e.result, this));

        SlotEventHub.Instance.Subscribe<FreeSpinEvent>(e => OnFreeSpin.InvokeWithDelay(e.remainingFreeSpins, this));
        SlotEventHub.Instance.Subscribe<RespinEvent>(e => OnRespin.InvokeWithDelay(this));

        SlotEventHub.Instance.Subscribe<EnterFreeSpinEvent>(e => StartCoroutine(InvokeAfterDelay(OnEnterFreeSpin, enterFreeSpinDelay)));
        SlotEventHub.Instance.Subscribe<ExitFreeSpinEvent>(e => StartCoroutine(InvokeAfterDelay(OnExitFreeSpin, exitFreeSpinDelay)));
        SlotEventHub.Instance.Subscribe<EnterRespinEvent>(e => StartCoroutine(InvokeAfterDelay(OnEnterRespin, enterRespinDelay)));
        SlotEventHub.Instance.Subscribe<ExitRespinEvent>(e => StartCoroutine(InvokeAfterDelay(OnExitRespin, exitRespinDelay)));
    }

    private void OnDisable()
    {
        SlotEventHub.Instance.Unsubscribe<SpinStartEvent>(e => StartCoroutine(HandleSpinStart()));
        SlotEventHub.Instance.Unsubscribe<ReelStopEvent>(e => OnReelStop.InvokeWithDelay(e.reelIndex, this));
        SlotEventHub.Instance.Unsubscribe<WinLineEvent>(e => OnWinLine.InvokeWithDelay(e.winLine, this));
        SlotEventHub.Instance.Unsubscribe<SpinEndEvent>(e => OnSpinEnd.InvokeWithDelay(e.result, this));

        SlotEventHub.Instance.Unsubscribe<FreeSpinEvent>(e => OnFreeSpin.InvokeWithDelay(e.remainingFreeSpins, this));
        SlotEventHub.Instance.Unsubscribe<RespinEvent>(e => OnRespin.InvokeWithDelay(this));

        SlotEventHub.Instance.Unsubscribe<EnterFreeSpinEvent>(e => StartCoroutine(InvokeAfterDelay(OnEnterFreeSpin, enterFreeSpinDelay)));
        SlotEventHub.Instance.Unsubscribe<ExitFreeSpinEvent>(e => StartCoroutine(InvokeAfterDelay(OnExitFreeSpin, exitFreeSpinDelay)));
        SlotEventHub.Instance.Unsubscribe<EnterRespinEvent>(e => StartCoroutine(InvokeAfterDelay(OnEnterRespin, enterRespinDelay)));
        SlotEventHub.Instance.Unsubscribe<ExitRespinEvent>(e => StartCoroutine(InvokeAfterDelay(OnExitRespin, exitRespinDelay)));
    }

    private IEnumerator HandleSpinStart()
    {
        if (spinStartDelay > 0f)
            yield return new WaitForSeconds(spinStartDelay);

        OnSpinStart?.Invoke();
    }

    private IEnumerator InvokeAfterDelay(UnityEvent unityEvent, float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        unityEvent?.Invoke();
    }
}
