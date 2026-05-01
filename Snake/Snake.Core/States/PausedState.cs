using Microsoft.Xna.Framework;
using Snake.Core.Audio;
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
        private static readonly (string Label, GameState Target)[] s_options = new[]
        {
            ("Resume", GameState.Playing),
            ("Upgrade Shop", GameState.UpgradeShop),
            ("Slots", GameState.Slots),
            ("Settings", GameState.Settings),
            ("Quit to Menu", GameState.Menu)
        };

        private readonly GameEngine m_engine;
        private readonly GameConfig m_config;
        private readonly VisualConfig m_visuals;
        private readonly AudioManager m_audio;

        private int m_selectedIndex;

        public GameState StateType => GameState.Paused;

        public PausedState(GameEngine engine, GameConfig config, VisualConfig visuals, AudioManager audio)
        {
            m_engine = engine;
            m_config = config;
            m_visuals = visuals;
            m_audio = audio;
        }

        public void Enter()
        {
            m_selectedIndex = 0;
            m_audio.OnPausedEnter();
        }

        public void Exit()
        {
        }

        public GameState? Update(GameTime gameTime, InputState input)
        {
            if (input.DirectionPressed == Direction.Up)
            {
                m_selectedIndex = (m_selectedIndex - 1 + s_options.Length) % s_options.Length;
            }
            else if (input.DirectionPressed == Direction.Down)
            {
                m_selectedIndex = (m_selectedIndex + 1) % s_options.Length;
            }

            if (input.ActionPressed)
            {
                return s_options[m_selectedIndex].Target;
            }

            // Pause button is a quick Resume shortcut
            if (input.PausePressed)
            {
                return GameState.Playing;
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

            renderer.DrawOverlay(m_visuals.PauseOverlayColor);

            if (renderer.HasFont)
            {
                renderer.DrawCenteredText("PAUSED", m_visuals.PausedTextColor, -70);

                for (int i = 0; i < s_options.Length; i++)
                {
                    bool selected = i == m_selectedIndex;
                    var color = selected ? m_visuals.HighlightColor : m_visuals.InstructionColor;
                    renderer.DrawMenuOption(s_options[i].Label, color, -40 + i * 16, selected);
                }

                renderer.DrawCenteredText("Tap || to resume", m_visuals.InstructionColor, 70);
            }
        }
    }
}
