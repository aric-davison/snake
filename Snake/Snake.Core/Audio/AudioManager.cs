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
        private Song m_menuSong;
        private Song m_slotsMenuSong;
        private SoundEffect m_appleCrunch;
        private SoundEffect m_reelSpin;
        private SoundEffect m_reelStop;
        private SoundEffect m_jackpot;
        private SoundEffectInstance m_reelSpinInstance;
        private bool m_gameOverPlayed;
        private bool m_menuSongActive;
        private bool m_slotsMenuSongActive;
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
                m_menuSong = content.Load<Song>("Audio/menu_screen");
                m_slotsMenuSong = content.Load<Song>("Audio/slots_menu");
                m_appleCrunch = content.Load<SoundEffect>("Audio/apple_crunch");
                m_reelSpin = content.Load<SoundEffect>("Audio/reel_spin");
                m_reelStop = content.Load<SoundEffect>("Audio/reel_stop");
                m_jackpot = content.Load<SoundEffect>("Audio/jackpot");
                m_reelSpinInstance = m_reelSpin.CreateInstance();
                m_reelSpinInstance.IsLooped = true;
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
            m_menuSongActive = false;
            m_slotsMenuSongActive = false;
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
            m_menuSongActive = false;
            m_slotsMenuSongActive = false;
            MediaPlayer.Stop();

            MediaPlayer.Volume = MusicVolume();
            MediaPlayer.IsRepeating = false;
            MediaPlayer.Play(m_gameOverSong);
        }

        public void OnMenuEnter()
        {
            if (!m_loaded) return;

            // Idempotent: if we're already playing the menu song (e.g. re-entered Menu from
            // Settings or Slots), don't restart it.
            if (m_menuSongActive && MediaPlayer.State == MediaState.Playing) return;

            m_menuSongActive = true;
            m_slotsMenuSongActive = false;
            MediaPlayer.Stop();
            MediaPlayer.Volume = MusicVolume();
            MediaPlayer.IsRepeating = true;
            MediaPlayer.Play(m_menuSong);
        }

        public void OnSlotsEnter()
        {
            if (!m_loaded) return;

            // Idempotent (re-entering the slots state shouldn't restart the song).
            if (m_slotsMenuSongActive && MediaPlayer.State == MediaState.Playing) return;

            m_slotsMenuSongActive = true;
            m_menuSongActive = false;
            MediaPlayer.Stop();
            MediaPlayer.Volume = MusicVolume();
            MediaPlayer.IsRepeating = true;
            MediaPlayer.Play(m_slotsMenuSong);
        }

        public void PlayReelSpin()
        {
            if (!m_loaded) return;
            float vol = SfxVolume();
            if (vol <= 0f) return;
            m_reelSpinInstance.Volume = vol;
            if (m_reelSpinInstance.State != SoundState.Playing)
            {
                m_reelSpinInstance.Play();
            }
        }

        public void StopReelSpin()
        {
            if (!m_loaded) return;
            if (m_reelSpinInstance.State != SoundState.Stopped)
            {
                m_reelSpinInstance.Stop();
            }
        }

        public void PlayReelStop()
        {
            if (!m_loaded) return;
            float vol = SfxVolume();
            if (vol <= 0f) return;
            m_reelStop.Play(vol, 0f, 0f);
        }

        public void PlayJackpot()
        {
            if (!m_loaded) return;
            float vol = SfxVolume();
            if (vol <= 0f) return;
            m_jackpot.Play(vol, 0f, 0f);
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
