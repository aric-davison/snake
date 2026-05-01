using Microsoft.Xna.Framework;
using Snake.Core.Audio;
using Snake.Core.Configuration;
using Snake.Core.Engine;
using Snake.Core.Input;
using Snake.Core.Rendering;

namespace Snake.Core.States
{
    /// <summary>
    /// Handles the active gameplay state.
    /// </summary>
    public class PlayingState : IGameStateHandler
    {
        private readonly GameEngine m_engine;
        private readonly GameConfig m_config;
        private readonly AudioManager m_audio;

        public GameState StateType => GameState.Playing;

        public PlayingState(GameEngine engine, GameConfig config, AudioManager audio)
        {
            m_engine = engine;
            m_config = config;
            m_audio = audio;
        }

        public void Enter()
        {
            m_audio.OnPlayingEnter();
        }

        public void Exit()
        {
        }

        public GameState? Update(GameTime gameTime, InputState input)
        {
            // Check for pause
            if (input.PausePressed)
            {
                return GameState.Paused;
            }

            // Handle direction input
            if (input.RequestedDirection.HasValue)
            {
                m_engine.SetDirection(input.RequestedDirection.Value);
            }

            // Update game logic
            var result = m_engine.Update(gameTime.ElapsedGameTime.TotalSeconds);

            if (result == GameEngine.UpdateResult.GameOver)
            {
                return GameState.GameOver;
            }

            return null;
        }

        public void Draw(IGameRenderer renderer)
        {
            renderer.DrawGrid(m_config.GridWidth, m_config.GridHeight);
            renderer.DrawFood(m_engine.Food);
            renderer.DrawSnake(m_engine.Snake);
            renderer.DrawApples(m_engine.SessionApples, m_engine.AppleBalance);
            renderer.DrawTouchControls();
        }
    }
}
