using Microsoft.Xna.Framework;
using Snake.Core.Configuration;
using Snake.Core.Engine;
using Snake.Core.Input;
using Snake.Core.Persistence;
using Snake.Core.Rendering;

namespace Snake.Core.States
{
    /// <summary>
    /// Handles the menu/title screen state.
    /// </summary>
    public class MenuState : IGameStateHandler
    {
        private static readonly (string Label, GameState Target)[] s_options = new[]
        {
            ("Play", GameState.Playing),
            ("Slots", GameState.Slots),
            ("Settings", GameState.Settings)
        };

        private readonly GameEngine m_engine;
        private readonly GameConfig m_config;
        private readonly VisualConfig m_visuals;
        private readonly PlayerData m_playerData;

        private int m_selectedIndex;

        public GameState StateType => GameState.Menu;

        public MenuState(GameEngine engine, GameConfig config, VisualConfig visuals, PlayerData playerData)
        {
            m_engine = engine;
            m_config = config;
            m_visuals = visuals;
            m_playerData = playerData;
        }

        public void Enter()
        {
            m_engine.Reset(m_playerData);
            m_selectedIndex = 0;
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

            return null;
        }

        public void Draw(IGameRenderer renderer)
        {
            renderer.DrawGrid(m_config.GridWidth, m_config.GridHeight);

            if (renderer.HasFont)
            {
                renderer.DrawCenteredText("SNAKE", m_visuals.TitleColor, -120);

                for (int i = 0; i < s_options.Length; i++)
                {
                    var color = i == m_selectedIndex ? m_visuals.HighlightColor : m_visuals.InstructionColor;
                    var label = i == m_selectedIndex ? $"> {s_options[i].Label} <" : s_options[i].Label;
                    renderer.DrawCenteredText(label, color, -20 + i * 35);
                }

                renderer.DrawCenteredText("Up/Down to navigate, Space to select", m_visuals.InstructionColor, 130);
            }
        }
    }
}
