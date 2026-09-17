public class WinLineEvent : SlotEvent
{
    public WinLineInfo winLine { get; }

    public WinLineEvent(WinLineInfo winLine, float delaySeconds = 0f)
        : base(delaySeconds)
    {
        this.winLine = winLine;
    }
}
