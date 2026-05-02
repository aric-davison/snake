using Snake.Core.Persistence;

namespace Snake.Core.Upgrades
{
    public class FortuneUpgrade : IUpgrade
    {
        public string Name => "Fortune";
        public string DisplayName => "Fortune";
        public string Description => "Chance for a\nbonus apple\nto spawn.";
        public int CurrentTier { get; set; }
        public int MaxTier => 5;

        public int GetCost(int tier) => 20 * (tier + 1);

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
