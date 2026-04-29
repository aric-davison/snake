namespace Snake.Core
{
    /// <summary>
    /// Represents the possible states of the game.
    /// </summary>
    public enum GameState
    {
        /// <summary>
        /// The game is at the start screen, waiting for player to begin.
        /// </summary>
        Start,

        /// <summary>
        /// The game is currently being played.
        /// </summary>
        Playing,

        /// <summary>
        /// The game has ended (snake died).
        /// </summary>
        GameOver,

        /// <summary>
        /// The game is paused (optional state for future expansion).
        /// </summary>
        Paused
    }
}
