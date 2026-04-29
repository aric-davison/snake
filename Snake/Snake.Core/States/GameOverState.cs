using Microsoft.Xna.Framework;
using Snake.Core.Configuration;
using Snake.Core.Engine;
using Snake.Core.Input;
using Snake.Core.Rendering;

namespace Snake.Core.States
{
    /// <summary>
    /// Handles the game over state.
    /// </summary>
    public class GameOverState : IGameStateHandler
    {
        private readonly GameEngine m_engine;
        private readonly GameConfig m_config;
        private readonly VisualConfig m_visuals;

        public GameState StateType => GameState.GameOver;

        public GameOverState(GameEngine engine, GameConfig config, VisualConfig visuals)
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
            // Restart on action button or Enter/Space
            if (input.ActionPressed)
            {
                return GameState.Start;
            }

            return null;
        }

        public void Draw(IGameRenderer renderer)
        {
            // Draw final game state behind overlay
            renderer.DrawGrid(m_config.GridWidth, m_config.GridHeight);
            renderer.DrawFood(m_engine.Food);
            renderer.DrawSnake(m_engine.Snake);
            renderer.DrawScore(m_engine.Score);
            renderer.DrawTouchControls();

            // Draw semi-transparent overlay
            renderer.DrawOverlay(m_visuals.GameOverOverlayColor);

            // Draw game over text
            if (renderer.HasFont)
            {
                renderer.DrawCenteredText("GAME OVER", m_visuals.GameOverTextColor, -60);
                renderer.DrawCenteredText($"Final Score: {m_engine.Score}", m_visuals.ScoreColor, -10);
            }

            // Draw restart button
            renderer.DrawActionButton("TAP TO RESTART");
        }
    }
}
