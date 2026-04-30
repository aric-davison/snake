using Snake.Core.Persistence;

namespace Snake.Core.Minigames
{
    /// <summary>
    /// Represents a wagering minigame. The lifecycle is three steps:
    /// PlaceBet (validate and deduct the wager), Play (run the random outcome),
    /// ResolvePayout (apply winnings, if any).
    /// </summary>
    public interface IMinigame
    {
        string Name { get; }
        int MinimumWager { get; }

        bool PlaceBet(int amount, PlayerData data);

        MinigameResult Play();

        void ResolvePayout(PlayerData data);
    }
}
