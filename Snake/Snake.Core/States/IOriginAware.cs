namespace Snake.Core.States
{
    /// <summary>
    /// Implemented by states whose Back action returns to the state that launched them
    /// (e.g., Slots and Settings, which can be reached from multiple origins).
    /// </summary>
    public interface IOriginAware
    {
        void SetOrigin(GameState origin);
    }
}
