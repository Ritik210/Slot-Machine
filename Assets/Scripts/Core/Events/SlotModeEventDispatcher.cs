using UnityEngine;

public class SlotModeEventDispatcher : MonoBehaviour
{
    private SlotModeType currentMode = SlotModeType.BaseGame;
    private SlotModeType previousMode = SlotModeType.BaseGame; //store mode before entering Respin

    private void OnEnable()
    {
        SlotEventHub.Instance.Subscribe<FreeSpinEvent>(OnFreeSpin);
        SlotEventHub.Instance.Subscribe<RespinEvent>(OnRespin);
        SlotEventHub.Instance.Subscribe<SpinEndEvent>(OnSpinEnd);
    }

    private void OnDisable()
    {
        SlotEventHub.Instance.Unsubscribe<FreeSpinEvent>(OnFreeSpin);
        SlotEventHub.Instance.Unsubscribe<RespinEvent>(OnRespin);
        SlotEventHub.Instance.Unsubscribe<SpinEndEvent>(OnSpinEnd);
    }

    private void OnFreeSpin(FreeSpinEvent evt)
    {
        if (currentMode != SlotModeType.FreeSpin)
        {
            SlotEventHub.Instance.Raise(new EnterFreeSpinEvent());
            currentMode = SlotModeType.FreeSpin;
        }

        if (evt.isLastSpin)
        {
            SlotEventHub.Instance.Raise(new ExitFreeSpinEvent());
            currentMode = SlotModeType.BaseGame;
        }
    }

    private void OnRespin(RespinEvent evt)
    {
        if (currentMode != SlotModeType.Respin)
        {
            previousMode = currentMode; //save previous mode
            SlotEventHub.Instance.Raise(new EnterRespinEvent());
            currentMode = SlotModeType.Respin;
        }
    }

    private void OnSpinEnd(SpinEndEvent evt)
    {
        if (currentMode == SlotModeType.Respin)
        {
            SlotEventHub.Instance.Raise(new ExitRespinEvent());
            currentMode = previousMode; //restore previous mode (e.g., FreeSpin)
        }
    }
}
