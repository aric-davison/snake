using Microsoft.Xna.Framework;
using Snake.Core.Configuration;
using Snake.Core.Input;
using Snake.Core.Rendering;

namespace Snake.Core.States
{
    /// <summary>
    /// Handles the upgrade shop state.
    /// </summary>
    public class UpgradeShopState : IGameStateHandler
    {
        private readonly GameConfig m_config;
        private readonly VisualConfig m_visuals;

        public GameState StateType => GameState.UpgradeShop;

        public UpgradeShopState(GameConfig config, VisualConfig visuals)
        {
            m_config = config;
            m_visuals = visuals;
        }

        public void Enter()
        {
        }

        public void Exit()
        {
        }

        public GameState? Update(GameTime gameTime, InputState input)
        {
            if (input.ActionPressed)
            {
                return GameState.Menu;
            }

            return null;
        }

        public void Draw(IGameRenderer renderer)
        {
            renderer.DrawGrid(m_config.GridWidth, m_config.GridHeight);
            renderer.DrawOverlay(new Color(0, 0, 0, 180));

            if (renderer.HasFont)
            {
                renderer.DrawCenteredText("UPGRADE SHOP", m_visuals.TitleColor, -80);
                renderer.DrawCenteredText("(coming soon)", m_visuals.InstructionColor, -20);
                renderer.DrawCenteredText("Tap anywhere to return", m_visuals.HighlightColor, 70);
            }

            renderer.DrawTouchControls();
        }
    }
}
