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
        Menu,

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
        Paused,

        /// <summary>
        /// The game is in the settings menu (optional state for future expansion).
        /// </summary>
        Settings,

        /// <summary>
        /// The game is in the upgrade shop (optional state for future expansion).
        /// </summary>
        UpgradeShop,

        /// <summary>
        /// The game is in the Slots minigame (optional state for future expansion).
        /// </summary>
        Slots
    }
}
