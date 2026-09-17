public class FreeSpinEvent : SlotEvent
{
    public int remainingFreeSpins { get; }
    public bool isLastSpin { get; }

    public FreeSpinEvent(int remaining, bool isLast, float delaySeconds = 0f)
        : base(delaySeconds)
    {
        remainingFreeSpins = remaining;
        isLastSpin = isLast;
    }
}
