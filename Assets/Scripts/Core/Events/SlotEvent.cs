public abstract class SlotEvent : IGameEvent
{
    public float delaySeconds { get; private set; }

    protected SlotEvent(float delaySeconds = 0f)
    {
        this.delaySeconds = delaySeconds;
    }
}
