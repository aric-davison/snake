using Microsoft.Xna.Framework;
using Snake.Core.Configuration;
using Snake.Core.Input;
using Snake.Core.Rendering;

namespace Snake.Core.States
{
    /// <summary>
    /// Handles the settings state. Reachable from Menu or Paused; Back returns to origin.
    /// </summary>
    public class SettingsState : IGameStateHandler, IOriginAware
    {
        private readonly GameConfig m_config;
        private readonly VisualConfig m_visuals;

        private GameState m_origin = GameState.Menu;

        public GameState StateType => GameState.Settings;

        public SettingsState(GameConfig config, VisualConfig visuals)
        {
            m_config = config;
            m_visuals = visuals;
        }

        public void SetOrigin(GameState origin)
        {
            m_origin = origin;
        }

        public void Enter()
        {
        }

        public void Exit()
        {
        }

        public GameState? Update(GameTime gameTime, InputState input)
        {
            if (input.PausePressed || input.ActionPressed)
            {
                return m_origin;
            }

            return null;
        }

        public void Draw(IGameRenderer renderer)
        {
            renderer.DrawGrid(m_config.GridWidth, m_config.GridHeight);
            renderer.DrawOverlay(new Color(0, 0, 0, 180));

            if (renderer.HasFont)
            {
                renderer.DrawCenteredText("SETTINGS", m_visuals.TitleColor, -80);
                renderer.DrawCenteredText("(coming soon)", m_visuals.InstructionColor, -20);
                renderer.DrawCenteredText("Tap || to go back", m_visuals.HighlightColor, 70);
            }

            renderer.DrawTouchControls();
        }
    }
}
