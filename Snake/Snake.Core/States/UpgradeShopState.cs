using Microsoft.Xna.Framework;
using Snake.Core.Configuration;
using Snake.Core.Input;
using Snake.Core.Persistence;
using Snake.Core.Rendering;
using Snake.Core.Upgrades;

namespace Snake.Core.States
{
    /// <summary>
    /// Handles the upgrade shop state. Reachable from Paused or GameOver; Back returns to origin.
    /// </summary>
    public class UpgradeShopState : IGameStateHandler, IOriginAware
    {
        private readonly GameConfig m_config;
        private readonly VisualConfig m_visuals;
        private readonly PlayerData m_playerData;
        private readonly IUpgrade[] m_upgrades;
        private readonly SaveManager m_saveManager;

        private GameState m_origin = GameState.GameOver;
        private int m_selectedIndex;

        private int BackOptionIndex => m_upgrades.Length;
        private int TotalOptions => m_upgrades.Length + 1;

        public GameState StateType => GameState.UpgradeShop;

        public void SetOrigin(GameState origin)
        {
            m_origin = origin;
        }

        public UpgradeShopState(
            GameConfig config,
            VisualConfig visuals,
            PlayerData playerData,
            IUpgrade[] upgrades,
            SaveManager saveManager)
        {
            m_config = config;
            m_visuals = visuals;
            m_playerData = playerData;
            m_upgrades = upgrades;
            m_saveManager = saveManager;
        }

        public void Enter()
        {
            m_selectedIndex = 0;

            // Sync each upgrade's CurrentTier from the persisted PlayerData.
            foreach (var upgrade in m_upgrades)
            {
                upgrade.CurrentTier = m_playerData.GetUpgradeTier(upgrade.Name);
            }
        }

        public void Exit()
        {
        }

        public GameState? Update(GameTime gameTime, InputState input)
        {
            if (input.PausePressed)
            {
                return m_origin;
            }

            if (input.DirectionPressed == Direction.Up)
            {
                m_selectedIndex = (m_selectedIndex - 1 + TotalOptions) % TotalOptions;
            }
            else if (input.DirectionPressed == Direction.Down)
            {
                m_selectedIndex = (m_selectedIndex + 1) % TotalOptions;
            }

            if (input.ActionPressed)
            {
                if (m_selectedIndex == BackOptionIndex)
                {
                    return m_origin;
                }

                var upgrade = m_upgrades[m_selectedIndex];
                int beforeTier = upgrade.CurrentTier;
                upgrade.Apply(m_playerData);

                // If the tier moved, the purchase succeeded. Persist immediately
                // (SDD §3 P6: save triggers on upgrade purchase).
                if (upgrade.CurrentTier > beforeTier)
                {
                    m_saveManager.Save(m_playerData);
                }
            }

            return null;
        }

        public void Draw(IGameRenderer renderer)
        {
            renderer.DrawGrid(m_config.GridWidth, m_config.GridHeight);
            renderer.DrawOverlay(new Color(0, 0, 0, 200));

            if (renderer.HasFont)
            {
                renderer.DrawCenteredText("UPGRADE SHOP", m_visuals.TitleColor, -78);
                renderer.DrawCenteredText($"Apples: {m_playerData.AppleBalance}", m_visuals.ScoreColor, -66);

                for (int i = 0; i < m_upgrades.Length; i++)
                {
                    var upgrade = m_upgrades[i];
                    bool selected = i == m_selectedIndex;
                    bool maxed = upgrade.CurrentTier >= upgrade.MaxTier;
                    int cost = maxed ? 0 : upgrade.GetCost(upgrade.CurrentTier);
                    bool affordable = !maxed && m_playerData.AppleBalance >= cost;

                    string costText = maxed ? "MAXED" : $"cost: {cost}";
                    string row = $"{upgrade.DisplayName} {upgrade.CurrentTier}-{upgrade.MaxTier}  {costText}";

                    Color color;
                    if (selected)
                        color = affordable ? m_visuals.HighlightColor : m_visuals.GameOverTextColor;
                    else
                        color = maxed ? m_visuals.PausedTextColor : m_visuals.InstructionColor;

                    renderer.DrawMenuOption(row, color, -40 + i * 16, selected);
                }

                bool backSelected = m_selectedIndex == BackOptionIndex;
                Color backColor = backSelected ? m_visuals.HighlightColor : m_visuals.InstructionColor;
                renderer.DrawMenuOption("Back", backColor, -40 + BackOptionIndex * 16, backSelected);

                if (m_selectedIndex < m_upgrades.Length)
                {
                    var selectedUpgrade = m_upgrades[m_selectedIndex];
                    renderer.DrawCenteredText(selectedUpgrade.Description, m_visuals.InstructionColor, 30);
                }
            }

            renderer.DrawTouchControls();
        }
    }
}
