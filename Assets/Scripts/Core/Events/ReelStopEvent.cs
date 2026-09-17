using System.Collections.Generic;

public class ReelStopEvent : SlotEvent
{
    public int reelIndex { get; }
    public List<int> symbols { get; }

    public ReelStopEvent(int reelIndex, List<int> symbols, float delaySeconds = 0f)
        : base(delaySeconds)
    {
        this.reelIndex = reelIndex;
        this.symbols = symbols;
    }
}
