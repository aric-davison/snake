namespace Snake.Core.Minigames
{
    /// <summary>
    /// Outcome of a single minigame play. Returned by IMinigame.Play().
    /// </summary>
    public class MinigameResult
    {
        public bool Won { get; set; }
        public int Payout { get; set; }
        public string[] ReelResults { get; set; }
    }
}
