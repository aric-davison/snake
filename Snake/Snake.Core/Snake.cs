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
        private List<Point> m_previousBody;
        private Direction m_direction;
        private Direction m_nextDirection;
        private bool m_shouldGrow;

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the Snake class at the specified starting position.
        /// </summary>
        /// <param name="startX">The starting X coordinate on the grid.</param>
        /// <param name="startY">The starting Y coordinate on the grid.</param>
        public Snake(int startX, int startY)
        {
            // Initialize snake with 3 segments moving right
            m_body = new List<Point>();
            m_body.Add(new Point(startX, startY));
            m_body.Add(new Point(startX - 1, startY));
            m_body.Add(new Point(startX - 2, startY));

            m_direction = Direction.Right;
            m_nextDirection = Direction.Right;
            m_shouldGrow = false;
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

        /// <summary>
        /// Gets all segments of the snake including the head.
        /// </summary>
        public IEnumerable<Point> AllSegments => m_body;

        /// <summary>
        /// Gets the current movement direction.
        /// </summary>
        public Direction CurrentDirection => m_direction;

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets the next direction for the snake to move.
        /// Prevents 180-degree reversals.
        /// </summary>
        /// <param name="newDirection">The desired new direction.</param>
        public void SetDirection(Direction newDirection)
        {
            // Prevent 180-degree reversal
            if (IsOppositeDirection(m_direction, newDirection))
                return;

            m_nextDirection = newDirection;
        }

        /// <summary>
        /// Moves the snake one cell in the current direction.
        /// </summary>
        public void Move()
        {
            // Apply buffered direction
            m_direction = m_nextDirection;

            // Calculate new head position
            Point newHead = GetNextHeadPosition();

            // Insert new head at the front
            m_body.Insert(0, newHead);

            // Remove tail unless growing
            if (m_shouldGrow)
            {
                m_shouldGrow = false;
            }
            else
            {
                m_body.RemoveAt(m_body.Count - 1);
            }
        }

        /// <summary>
        /// Marks the snake to grow on the next move (skip tail removal).
        /// </summary>
        public void Grow()
        {
            m_shouldGrow = true;
        }

        /// <summary>
        /// Checks if the snake head collides with its own body.
        /// </summary>
        /// <returns>True if self-collision detected.</returns>
        public bool CheckSelfCollision()
        {
            // Check if head position matches any body segment
            return Body.Contains(Head);
        }

        /// <summary>
        /// Gets the position where the head will be after the next move.
        /// </summary>
        /// <returns>The next head position.</returns>
        public Point GetNextHeadPosition()
        {
            Point current = Head;

            return m_nextDirection switch
            {
                Direction.Up => new Point(current.X, current.Y - 1),
                Direction.Down => new Point(current.X, current.Y + 1),
                Direction.Left => new Point(current.X - 1, current.Y),
                Direction.Right => new Point(current.X + 1, current.Y),
                _ => current
            };
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Checks if two directions are opposite to each other.
        /// </summary>
        private bool IsOppositeDirection(Direction current, Direction next)
        {
            return (current == Direction.Up && next == Direction.Down) ||
                   (current == Direction.Down && next == Direction.Up) ||
                   (current == Direction.Left && next == Direction.Right) ||
                   (current == Direction.Right && next == Direction.Left);
        }

        #endregion
    }
}
