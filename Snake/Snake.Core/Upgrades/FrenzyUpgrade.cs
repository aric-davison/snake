using Snake.Core.Persistence;

namespace Snake.Core.Upgrades
{
    public class FrenzyUpgrade : IUpgrade
    {
        public string Name => "Frenzy";
        public string DisplayName => "Frenzy";
        public string Description => "Chance for a\ngolden apple\nto appear.";
        public int CurrentTier { get; set; }
        public int MaxTier => 5;

        public int GetCost(int tier) => 30 * (tier + 1);

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
