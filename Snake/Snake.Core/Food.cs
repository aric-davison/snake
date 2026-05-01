using Microsoft.Xna.Framework;
using System;
using System.Linq;

namespace Snake.Core
{
    /// <summary>
    /// Represents a food item that the snake can consume to grow.
    /// Handles spawning food at random valid positions on the game board.
    /// </summary>
    public class Food
    {
        #region Fields
        private Point m_position;
        private Random m_random;
        #endregion

        #region Properties

        /// <summary>
        /// Gets the current position of the food on the grid.
        /// </summary>
        public Point Position => m_position;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the Food class.
        /// The initial position will be set when Spawn is called.
        /// </summary>
        public Food()
        {
            m_random = new Random();
            m_position = new Point(0, 0);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Spawns the food at a random valid position on the game board.
        /// Ensures the food does not spawn on the snake's body.
        /// </summary>
        /// <param name="gameBoard">The game board defining valid spawn locations.</param>
        /// <param name="snake">The snake to avoid when spawning.</param>
        public void Spawn(GameBoard gameBoard, Snake snake)
        {
            Point newPosition;
            int maxAttempts = 100; // Prevent infinite loop in case board is full
            int attempts = 0;
            bool isValidPosition;

            do
            {
                newPosition = GenerateRandomPosition(gameBoard);
                attempts++;

                // Check if position overlaps with snake
                isValidPosition = !snake.AllSegments.Contains(newPosition);

                // Safety check: if we can't find a valid position after many attempts,
                // just place it anywhere (this would only happen if the board is nearly full)
                if (attempts >= maxAttempts)
                {
                    break;
                }
            }
            while (!isValidPosition);

            m_position = newPosition;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Generates a random position within the game board boundaries.
        /// </summary>
        /// <param name="gameBoard">The game board defining the valid area.</param>
        /// <returns>A random position on the game board.</returns>
        private Point GenerateRandomPosition(GameBoard gameBoard)
        {
            int x = m_random.Next(0, gameBoard.GridWidth);
            int y = m_random.Next(0, gameBoard.GridHeight);
            return new Point(x, y);
        }

        #endregion
    }
}
