using Snake.Core.Persistence;

namespace Snake.Core.Upgrades
{
    public class SpeedUpgrade : IUpgrade
    {
        public string Name => "Speed";
        public string Description => "Snake moves faster.";
        public int CurrentTier { get; set; }
        public int MaxTier => 5;

        public int GetCost(int tier) => 10 * (tier + 1);

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
