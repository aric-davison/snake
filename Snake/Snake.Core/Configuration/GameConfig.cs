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
        /// Points awarded for eating food.
        /// </summary>
        public int PointsPerFood { get; set; } = 10;

        /// <summary>
        /// Width of the game grid in cells.
        /// </summary>
        public int GridWidth { get; set; } = 30;

        /// <summary>
        /// Height of the game grid in cells.
        /// </summary>
        public int GridHeight { get; set; } = 20;

        /// <summary>
        /// Initial length of the snake (including head).
        /// </summary>
        public int InitialSnakeLength { get; set; } = 3;
    }
}
