using System;
using Snake.Core.Persistence;

namespace Snake.Core.Minigames
{
    /// <summary>
    /// Three-reel slot machine. Three matching symbols pays 5x; two matching pays 2x;
    /// otherwise the wager is lost.
    /// </summary>
    public class SlotsMinigame : IMinigame
    {
        private static readonly string[] s_symbols = { "A", "B", "C", "D", "7" };

        private readonly Random m_random = new Random();
        private MinigameResult m_lastResult;
        private int m_pendingBet;

        public string Name => "Slots";
        public int MinimumWager => 1;

        public bool PlaceBet(int amount, PlayerData data)
        {
            if (amount < MinimumWager) return false;
            if (data.AppleBalance < amount) return false;

            data.AppleBalance -= amount;
            m_pendingBet = amount;
            return true;
        }

        public MinigameResult Play()
        {
            var reels = new[]
            {
                s_symbols[m_random.Next(s_symbols.Length)],
                s_symbols[m_random.Next(s_symbols.Length)],
                s_symbols[m_random.Next(s_symbols.Length)]
            };

            int payout;
            if (reels[0] == reels[1] && reels[1] == reels[2])
            {
                payout = m_pendingBet * 5;
            }
            else if (reels[0] == reels[1] || reels[1] == reels[2] || reels[0] == reels[2])
            {
                payout = m_pendingBet * 2;
            }
            else
            {
                payout = 0;
            }

            m_lastResult = new MinigameResult
            {
                Won = payout > 0,
                Payout = payout,
                ReelResults = reels
            };
            return m_lastResult;
        }

        public void ResolvePayout(PlayerData data)
        {
            if (m_lastResult != null && m_lastResult.Won)
            {
                data.AppleBalance += m_lastResult.Payout;
            }

            m_lastResult = null;
            m_pendingBet = 0;
        }
    }
}
