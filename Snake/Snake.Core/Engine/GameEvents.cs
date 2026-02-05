using System;

namespace Snake.Core.Engine
{
    /// <summary>
    /// Game event definitions for loose coupling between components.
    /// </summary>
    public class GameEvents
    {
        /// <summary>
        /// Raised when the score changes.
        /// </summary>
        public event Action<int> ScoreChanged;

        /// <summary>
        /// Raised when the game ends.
        /// </summary>
        public event Action GameOver;

        /// <summary>
        /// Raised when food is eaten.
        /// </summary>
        public event Action FoodEaten;

        /// <summary>
        /// Raised when the game state changes.
        /// </summary>
        public event Action<GameState, GameState> StateChanged;

        public void RaiseScoreChanged(int newScore)
        {
            ScoreChanged?.Invoke(newScore);
        }

        public void RaiseGameOver()
        {
            GameOver?.Invoke();
        }

        public void RaiseFoodEaten()
        {
            FoodEaten?.Invoke();
        }

        public void RaiseStateChanged(GameState from, GameState to)
        {
            StateChanged?.Invoke(from, to);
        }
    }
}
