public class SpinEndEvent : SlotEvent
{
    public SpinResult result { get; }

    public SpinEndEvent(SpinResult result, float delaySeconds = 0f)
        : base(delaySeconds)
    {
        this.result = result;
    }
}
