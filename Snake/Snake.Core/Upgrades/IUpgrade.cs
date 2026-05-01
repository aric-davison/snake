using Snake.Core.Persistence;

namespace Snake.Core.Upgrades
{
    /// <summary>
    /// Represents a purchasable upgrade. Concrete upgrades define their cost curve and
    /// effect. Apply performs the purchase: it deducts the cost from PlayerData and
    /// increments both the upgrade's CurrentTier and the value stored in PlayerData.
    /// </summary>
    public interface IUpgrade
    {
        /// <summary>Stable identifier used as the save-data key. Do not rename.</summary>
        string Name { get; }

        /// <summary>Short label shown in the upgrade shop UI. Safe to change.</summary>
        string DisplayName { get; }

        string Description { get; }
        int CurrentTier { get; set; }
        int MaxTier { get; }

        int GetCost(int tier);

        void Apply(PlayerData data);
    }
}
