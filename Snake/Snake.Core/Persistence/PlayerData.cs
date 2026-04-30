using System.Collections.Generic;

namespace Snake.Core.Persistence
{
    /// <summary>
    /// Central in-memory data object for all persistent player state.
    /// Owned by SnakeGame and shared by reference with the states that read or modify it.
    /// </summary>
    public class PlayerData
    {
        public int AppleBalance { get; set; }

        public int HighScore { get; set; }

        public bool AudioEnabled { get; set; } = true;

        public Dictionary<string, int> UpgradeTiers { get; set; } = new Dictionary<string, int>();

        public int GetUpgradeTier(string upgradeId)
        {
            return UpgradeTiers.TryGetValue(upgradeId, out var tier) ? tier : 0;
        }

        public void SetUpgradeTier(string upgradeId, int tier)
        {
            UpgradeTiers[upgradeId] = tier;
        }
    }
}
