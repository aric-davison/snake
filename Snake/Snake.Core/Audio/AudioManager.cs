using System;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Media;
using Snake.Core.Persistence;

namespace Snake.Core.Audio
{
    /// <summary>
    /// Owns the loop tracks, the game-over one-shot, and SFX. State handlers call
    /// the On*Enter hooks to reflect lifecycle changes; volume is read from
    /// PlayerData.MusicLevel and SfxLevel (each 0..MaxLevel, where 0 is silent).
    /// </summary>
    public class AudioManager
    {
        public const int MaxLevel = 4;

        private readonly PlayerData m_playerData;
        private readonly Random m_random = new Random();
        private Song[] m_loopTracks;
        private Song m_gameOverSong;
        private SoundEffect m_appleCrunch;
        private bool m_gameOverPlayed;
        private bool m_loaded;

        public AudioManager(PlayerData playerData)
        {
            m_playerData = playerData;
        }

        public void LoadContent(ContentManager content)
        {
            try
            {
                m_loopTracks = new[]
                {
                    content.Load<Song>("Audio/pixel_loop01"),
                    content.Load<Song>("Audio/pixel_loop02"),
                };
                m_gameOverSong = content.Load<Song>("Audio/game_over");
                m_appleCrunch = content.Load<SoundEffect>("Audio/apple_crunch");
                m_loaded = true;
            }
            catch
            {
                m_loaded = false;
            }
        }

        public void OnPlayingEnter()
        {
            if (!m_loaded) return;

            MediaPlayer.Volume = MusicVolume();

            if (MediaPlayer.State == MediaState.Paused)
            {
                MediaPlayer.Resume();
                return;
            }

            // Fresh run: arm the game-over one-shot and pick a random loop track.
            m_gameOverPlayed = false;
            var track = m_loopTracks[m_random.Next(m_loopTracks.Length)];
            MediaPlayer.IsRepeating = true;
            MediaPlayer.Play(track);
        }

        public void OnPausedEnter()
        {
            if (!m_loaded) return;
            if (MediaPlayer.State == MediaState.Playing)
            {
                MediaPlayer.Pause();
            }
        }

        public void OnGameOverEnter()
        {
            if (!m_loaded) return;
            if (m_gameOverPlayed) return;

            m_gameOverPlayed = true;
            MediaPlayer.Stop();

            MediaPlayer.Volume = MusicVolume();
            MediaPlayer.IsRepeating = false;
            MediaPlayer.Play(m_gameOverSong);
        }

        public void OnMenuEnter()
        {
            if (!m_loaded) return;
            MediaPlayer.Stop();
        }

        public void PlayAppleCrunch()
        {
            if (!m_loaded) return;
            float vol = SfxVolume();
            if (vol <= 0f) return;
            m_appleCrunch.Play(vol, 0f, 0f);
        }

        public void RefreshMusicVolume()
        {
            if (!m_loaded) return;
            MediaPlayer.Volume = MusicVolume();
        }

        private float MusicVolume() => Clamp01(m_playerData.MusicLevel / (float)MaxLevel);

        private float SfxVolume() => Clamp01(m_playerData.SfxLevel / (float)MaxLevel);

        private static float Clamp01(float v)
        {
            if (v < 0f) return 0f;
            if (v > 1f) return 1f;
            return v;
        }
    }
}
