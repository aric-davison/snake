using Microsoft.Xna.Framework;
using Snake.Core.Configuration;
using Snake.Core.Engine;
using Snake.Core.Input;
using Snake.Core.Persistence;
using Snake.Core.Rendering;

namespace Snake.Core.States
{
    /// <summary>
    /// Handles the game over state.
    /// </summary>
    public class GameOverState : IGameStateHandler
    {
        private static readonly (string Label, GameState Target)[] s_options = new[]
        {
            ("Play Again", GameState.Playing),
            ("Main Menu", GameState.Menu),
            ("Upgrade Shop", GameState.UpgradeShop),
            ("Slots", GameState.Slots),
            ("Settings", GameState.Settings)
        };

        private readonly GameEngine m_engine;
        private readonly GameConfig m_config;
        private readonly VisualConfig m_visuals;
        private readonly PlayerData m_playerData;

        private int m_selectedIndex;

        public GameState StateType => GameState.GameOver;

        public GameOverState(GameEngine engine, GameConfig config, VisualConfig visuals, PlayerData playerData)
        {
            m_engine = engine;
            m_config = config;
            m_visuals = visuals;
            m_playerData = playerData;
        }

        public void Enter()
        {
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
                var target = s_options[m_selectedIndex].Target;
                if (target == GameState.Playing)
                {
                    m_engine.Reset(m_playerData);
                }
                return target;
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

            renderer.DrawOverlay(m_visuals.GameOverOverlayColor);

            if (renderer.HasFont)
            {
                renderer.DrawCenteredText("GAME OVER", m_visuals.GameOverTextColor, -75);
                renderer.DrawCenteredText($"Apples this run: {m_engine.SessionApples}", m_visuals.ScoreColor, -62);

                for (int i = 0; i < s_options.Length; i++)
                {
                    bool selected = i == m_selectedIndex;
                    var color = selected ? m_visuals.HighlightColor : m_visuals.InstructionColor;
                    renderer.DrawMenuOption(s_options[i].Label, color, -40 + i * 11, selected);
                }
            }
        }
    }
}
