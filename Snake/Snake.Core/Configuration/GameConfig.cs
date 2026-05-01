namespace Snake.Core.Configuration
{
    /// <summary>
    /// Configuration for core game mechanics and constants.
    /// </summary>
    public class GameConfig
    {
        /// <summary>
        /// Seconds between snake movement updates.
        /// </summary>
        public double UpdateInterval { get; set; } = 0.15;

        /// <summary>
        /// Apples awarded per food eaten (before Apple Value upgrade tier bonus).
        /// </summary>
        public int ApplesPerFood { get; set; } = 1;

        /// <summary>
        /// Width of the game grid in cells.
        /// </summary>
        public int GridWidth { get; set; } = 15;

        /// <summary>
        /// Height of the game grid in cells.
        /// </summary>
        public int GridHeight { get; set; } = 10;

        /// <summary>
        /// Initial length of the snake (including head).
        /// </summary>
        public int InitialSnakeLength { get; set; } = 3;
    }
}
