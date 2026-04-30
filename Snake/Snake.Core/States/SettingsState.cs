using Microsoft.Xna.Framework;
using Snake.Core.Configuration;
using Snake.Core.Input;
using Snake.Core.Persistence;
using Snake.Core.Rendering;

namespace Snake.Core.States
{
    /// <summary>
    /// Handles the settings state. Reachable from Menu or Paused; Back returns to origin.
    /// </summary>
    public class SettingsState : IGameStateHandler, IOriginAware
    {
        private const int OptionCount = 2;
        private const int AudioOptionIndex = 0;
        private const int DeleteSaveOptionIndex = 1;

        private readonly GameConfig m_config;
        private readonly VisualConfig m_visuals;
        private readonly PlayerData m_playerData;
        private readonly SaveManager m_saveManager;

        private GameState m_origin = GameState.Menu;
        private int m_selectedIndex;
        private bool m_confirmingDelete;

        public GameState StateType => GameState.Settings;

        public SettingsState(GameConfig config, VisualConfig visuals, PlayerData playerData, SaveManager saveManager)
        {
            m_config = config;
            m_visuals = visuals;
            m_playerData = playerData;
            m_saveManager = saveManager;
        }

        public void SetOrigin(GameState origin)
        {
            m_origin = origin;
        }

        public void Enter()
        {
            m_selectedIndex = 0;
            m_confirmingDelete = false;
        }

        public void Exit()
        {
        }

        public GameState? Update(GameTime gameTime, InputState input)
        {
            if (m_confirmingDelete)
            {
                if (input.ActionPressed)
                {
                    WipeSave();
                    m_confirmingDelete = false;
                }
                else if (input.PausePressed)
                {
                    m_confirmingDelete = false;
                }
                return null;
            }

            if (input.PausePressed)
            {
                return m_origin;
            }

            if (input.DirectionPressed == Direction.Up)
            {
                m_selectedIndex = (m_selectedIndex - 1 + OptionCount) % OptionCount;
            }
            else if (input.DirectionPressed == Direction.Down)
            {
                m_selectedIndex = (m_selectedIndex + 1) % OptionCount;
            }

            if (input.ActionPressed)
            {
                if (m_selectedIndex == AudioOptionIndex)
                {
                    m_playerData.AudioEnabled = !m_playerData.AudioEnabled;
                    m_saveManager.Save(m_playerData);
                }
                else if (m_selectedIndex == DeleteSaveOptionIndex)
                {
                    m_confirmingDelete = true;
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
                if (m_confirmingDelete)
                {
                    renderer.DrawCenteredText("DELETE SAVE?", m_visuals.GameOverTextColor, -60);
                    renderer.DrawCenteredText("This wipes your apples and upgrades.", m_visuals.InstructionColor, -10);
                    renderer.DrawCenteredText("This cannot be undone.", m_visuals.InstructionColor, 20);
                    renderer.DrawCenteredText("Space = confirm,  || = cancel", m_visuals.HighlightColor, 80);
                }
                else
                {
                    renderer.DrawCenteredText("SETTINGS", m_visuals.TitleColor, -120);

                    string audioLabel = $"Audio: {(m_playerData.AudioEnabled ? "On" : "Off")}";
                    DrawOption(renderer, audioLabel, AudioOptionIndex, -40);
                    DrawOption(renderer, "Delete Save", DeleteSaveOptionIndex, 0);

                    renderer.DrawCenteredText("Up/Down to navigate, Space to select, || to leave", m_visuals.InstructionColor, 90);
                }
            }

            renderer.DrawTouchControls();
        }

        private void DrawOption(IGameRenderer renderer, string label, int index, float yOffset)
        {
            bool selected = m_selectedIndex == index;
            string row = selected ? $"> {label} <" : label;
            Color color = selected ? m_visuals.HighlightColor : m_visuals.InstructionColor;
            renderer.DrawCenteredText(row, color, yOffset);
        }

        private void WipeSave()
        {
            m_playerData.AppleBalance = 0;
            m_playerData.HighScore = 0;
            m_playerData.UpgradeTiers.Clear();
            m_playerData.AudioEnabled = true;
            m_saveManager.Save(m_playerData);
        }
    }
}
