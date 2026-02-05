using Microsoft.Xna.Framework;
using Snake.Core.Configuration;
using Snake.Core.Engine;
using Snake.Core.Input;
using Snake.Core.Rendering;

namespace Snake.Core.States
{
    /// <summary>
    /// Handles the start/title screen state.
    /// </summary>
    public class StartState : IGameStateHandler
    {
        private readonly GameEngine m_engine;
        private readonly GameConfig m_config;
        private readonly VisualConfig m_visuals;

        public GameState StateType => GameState.Start;

        public StartState(GameEngine engine, GameConfig config, VisualConfig visuals)
        {
            m_engine = engine;
            m_config = config;
            m_visuals = visuals;
        }

        public void Enter()
        {
            m_engine.Reset();
        }

        public void Exit()
        {
        }

        public GameState? Update(GameTime gameTime, InputState input)
        {
            // Any input starts the game
            if (input.AnyInputPressed)
            {
                return GameState.Playing;
            }

            return null;
        }

        public void Draw(IGameRenderer renderer)
        {
            // Draw empty grid as background
            renderer.DrawGrid(m_config.GridWidth, m_config.GridHeight);

            if (renderer.HasFont)
            {
                renderer.DrawCenteredText("SNAKE", m_visuals.TitleColor, -80);
                renderer.DrawCenteredText("Use D-pad to move", m_visuals.InstructionColor, -20);
                renderer.DrawCenteredText("Tap || to pause", m_visuals.InstructionColor, 20);
                renderer.DrawCenteredText("Tap anywhere to start", m_visuals.HighlightColor, 70);
            }
        }
    }
}
