using Microsoft.Xna.Framework;

namespace Snake.Core
{
    /// <summary>
    /// Represents the game board (playing field) for the Snake game.
    /// Defines the dimensions and boundaries of the play area.
    /// </summary>
    public class GameBoard
    {
        #region Constants

        /// <summary>
        /// The width of the game grid in cells.
        /// </summary>
        public const int GRID_WIDTH = 30;

        /// <summary>
        /// The height of the game grid in cells.
        /// </summary>
        public const int GRID_HEIGHT = 20;

        /// <summary>
        /// The size of each cell in pixels.
        /// </summary>
        public const int CELL_SIZE = 25;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the GameBoard class.
        /// </summary>
        public GameBoard()
        {
            // Currently the game board is static, but this class could be extended
            // to support different board sizes, obstacles, etc.
        }

        #endregion
    }
}
