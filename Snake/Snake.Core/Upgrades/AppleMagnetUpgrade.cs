using Snake.Core.Persistence;

namespace Snake.Core.Upgrades
{
    public class AppleMagnetUpgrade : IUpgrade
    {
        public string Name => "Apple Magnet";
        public string Description => "Apples are drawn toward the snake from a wider range.";
        public int CurrentTier { get; set; }
        public int MaxTier => 3;

        public int GetCost(int tier) => 25 * (tier + 1);

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
