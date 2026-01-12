using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;

namespace Snake.Core
{
    /// <summary>
    /// Represents the player-controlled snake entity in the game.
    /// Manages snake movement, growth, and collision detection with itself.
    /// </summary>
    public class Snake
    {
        private readonly List<Point> m_body;

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the Snake class at the specified starting position.
        /// </summary>
        /// <param name="startX">The starting X coordinate on the grid.</param>
        /// <param name="startY">The starting Y coordinate on the grid.</param>
        public Snake(int startX, int startY)
        {
            // Initialize snake with 3 segments
            m_body = new List<Point>();
            m_body.Add(new Point(startX, startY));
            m_body.Add(new Point(startX - 1, startY));
            m_body.Add(new Point(startX - 2, startY));
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the position of the snake's head.
        /// </summary>
        public Point Head => m_body[0];

        /// <summary>
        /// Gets the body segments of the snake (excluding the head).
        /// </summary>
        public IEnumerable<Point> Body => m_body.Skip(1);
        #endregion
    }
}
