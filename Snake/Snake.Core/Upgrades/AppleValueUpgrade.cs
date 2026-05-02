using Snake.Core.Persistence;

namespace Snake.Core.Upgrades
{
    public class AppleValueUpgrade : IUpgrade
    {
        public string Name => "Apple Value";
        public string DisplayName => "Value";
        public string Description => "Each apple\nyields more\napples.";
        public int CurrentTier { get; set; }
        public int MaxTier => 5;

        public int GetCost(int tier) => 15 * (tier + 1);

        public void Apply(PlayerData data)
        {
            if (CurrentTier >= MaxTier) return;
            int cost = GetCost(CurrentTier);
            if (data.AppleBalance < cost) return;

            data.AppleBalance -= cost;
            CurrentTier++;
            data.SetUpgradeTier(Name, CurrentTier);
        }
    }
}
