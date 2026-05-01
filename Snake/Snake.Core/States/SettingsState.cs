using Microsoft.Xna.Framework;
using Snake.Core.Audio;
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
        private const int OptionCount = 4;
        private const int MusicSliderIndex = 0;
        private const int SfxSliderIndex = 1;
        private const int DeleteSaveOptionIndex = 2;
        private const int BackOptionIndex = 3;

        private const int ConfirmNoIndex = 0;
        private const int ConfirmYesIndex = 1;

        private readonly GameConfig m_config;
        private readonly VisualConfig m_visuals;
        private readonly PlayerData m_playerData;
        private readonly SaveManager m_saveManager;
        private readonly AudioManager m_audio;

        private GameState m_origin = GameState.Menu;
        private int m_selectedIndex;
        private bool m_confirmingDelete;
        private int m_confirmIndex;

        public GameState StateType => GameState.Settings;

        public SettingsState(GameConfig config, VisualConfig visuals, PlayerData playerData, SaveManager saveManager, AudioManager audio)
        {
            m_config = config;
            m_visuals = visuals;
            m_playerData = playerData;
            m_saveManager = saveManager;
            m_audio = audio;
        }

        public void SetOrigin(GameState origin)
        {
            m_origin = origin;
        }

        public void Enter()
        {
            m_selectedIndex = 0;
            m_confirmingDelete = false;
            m_confirmIndex = ConfirmNoIndex;
        }

        public void Exit()
        {
        }

        public GameState? Update(GameTime gameTime, InputState input)
        {
            if (m_confirmingDelete)
            {
                if (input.DirectionPressed == Direction.Up)
                {
                    m_confirmIndex = (m_confirmIndex - 1 + 2) % 2;
                }
                else if (input.DirectionPressed == Direction.Down)
                {
                    m_confirmIndex = (m_confirmIndex + 1) % 2;
                }

                if (input.ActionPressed)
                {
                    if (m_confirmIndex == ConfirmYesIndex)
                    {
                        WipeSave();
                    }
                    m_confirmingDelete = false;
                    m_confirmIndex = ConfirmNoIndex;
                }
                else if (input.PausePressed)
                {
                    m_confirmingDelete = false;
                    m_confirmIndex = ConfirmNoIndex;
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
            else if (input.DirectionPressed == Direction.Left)
            {
                AdjustSlider(-1);
            }
            else if (input.DirectionPressed == Direction.Right)
            {
                AdjustSlider(+1);
            }

            if (input.ActionPressed)
            {
                if (m_selectedIndex == DeleteSaveOptionIndex)
                {
                    m_confirmingDelete = true;
                    m_confirmIndex = ConfirmNoIndex;
                }
                else if (m_selectedIndex == BackOptionIndex)
                {
                    return m_origin;
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
                    renderer.DrawCenteredText("DELETE SAVE?", m_visuals.GameOverTextColor, -40);
                    renderer.DrawCenteredText("This cannot be undone.", m_visuals.InstructionColor, -15);

                    bool noSelected = m_confirmIndex == ConfirmNoIndex;
                    bool yesSelected = m_confirmIndex == ConfirmYesIndex;
                    renderer.DrawMenuOption("No", Color.Green, 16, noSelected);
                    renderer.DrawMenuOption("Yes", Color.Red, 32, yesSelected);
                }
                else
                {
                    renderer.DrawCenteredText("SETTINGS", m_visuals.TitleColor, -70);

                    DrawSliderRow(renderer, "Music", m_playerData.MusicLevel, MusicSliderIndex, -25);
                    DrawSliderRow(renderer, "SFX", m_playerData.SfxLevel, SfxSliderIndex, -9);
                    DrawOption(renderer, "Delete Save", DeleteSaveOptionIndex, 7);
                    DrawOption(renderer, "Back", BackOptionIndex, 23);
                }
            }

            renderer.DrawTouchControls();
        }

        private void AdjustSlider(int delta)
        {
            if (m_selectedIndex == MusicSliderIndex)
            {
                int newLevel = ClampLevel(m_playerData.MusicLevel + delta);
                if (newLevel == m_playerData.MusicLevel) return;
                m_playerData.MusicLevel = newLevel;
                m_audio.RefreshMusicVolume();
                m_saveManager.Save(m_playerData);
            }
            else if (m_selectedIndex == SfxSliderIndex)
            {
                int newLevel = ClampLevel(m_playerData.SfxLevel + delta);
                if (newLevel == m_playerData.SfxLevel) return;
                m_playerData.SfxLevel = newLevel;
                m_saveManager.Save(m_playerData);
            }
        }

        private static int ClampLevel(int level)
        {
            if (level < 0) return 0;
            if (level > AudioManager.MaxLevel) return AudioManager.MaxLevel;
            return level;
        }

        private void DrawSliderRow(IGameRenderer renderer, string label, int level, int index, float yOffset)
        {
            bool selected = m_selectedIndex == index;
            Color labelColor = selected ? m_visuals.HighlightColor : m_visuals.InstructionColor;
            renderer.DrawSlider(label, level, AudioManager.MaxLevel + 1, labelColor, Color.Green, yOffset, selected);
        }

        private void DrawOption(IGameRenderer renderer, string label, int index, float yOffset)
        {
            bool selected = m_selectedIndex == index;
            Color color = selected ? m_visuals.HighlightColor : m_visuals.InstructionColor;
            renderer.DrawMenuOption(label, color, yOffset, selected);
        }

        private void WipeSave()
        {
            m_playerData.AppleBalance = 0;
            m_playerData.HighScore = 0;
            m_playerData.UpgradeTiers.Clear();
            m_playerData.AudioEnabled = true;
            m_playerData.MusicLevel = 1;
            m_playerData.SfxLevel = AudioManager.MaxLevel;
            m_audio.RefreshMusicVolume();
            m_saveManager.Save(m_playerData);
        }
    }
}
