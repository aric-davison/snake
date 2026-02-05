using Microsoft.Xna.Framework;
using Snake.Core.Configuration;
using Snake.Core.Engine;
using Snake.Core.Input;
using Snake.Core.Rendering;

namespace Snake.Core.States
{
    /// <summary>
    /// Handles the paused state.
    /// </summary>
    public class PausedState : IGameStateHandler
    {
        private readonly GameEngine m_engine;
        private readonly GameConfig m_config;
        private readonly VisualConfig m_visuals;

        public GameState StateType => GameState.Paused;

        public PausedState(GameEngine engine, GameConfig config, VisualConfig visuals)
        {
            m_engine = engine;
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
            // Resume on pause button press
            if (input.PausePressed)
            {
                return GameState.Playing;
            }

            return null;
        }

        public void Draw(IGameRenderer renderer)
        {
            // Draw the game state behind the overlay
            renderer.DrawGrid(m_config.GridWidth, m_config.GridHeight);
            renderer.DrawFood(m_engine.Food);
            renderer.DrawSnake(m_engine.Snake);
            renderer.DrawScore(m_engine.Score);
            renderer.DrawTouchControls();

            // Draw semi-transparent overlay
            renderer.DrawOverlay(m_visuals.PauseOverlayColor);

            // Draw paused text
            if (renderer.HasFont)
            {
                renderer.DrawCenteredText("PAUSED", m_visuals.PausedTextColor, -30);
                renderer.DrawCenteredText("Tap || to resume", m_visuals.InstructionColor, 20);
            }
        }
    }
}
