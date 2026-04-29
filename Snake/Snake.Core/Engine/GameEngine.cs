using System;
using Microsoft.Xna.Framework;
using Snake.Core.Configuration;

namespace Snake.Core.Engine
{
    /// <summary>
    /// Core game logic engine. Contains no rendering code.
    /// Manages snake movement, collision detection, scoring, and food.
    /// </summary>
    public class GameEngine
    {
        /// <summary>
        /// Result of an update tick.
        /// </summary>
        public enum UpdateResult
        {
            Continue,
            GameOver,
            FoodEaten
        }

        private readonly GameConfig m_config;
        private readonly GameBoard m_board;
        private readonly GameEvents m_events;

        private Snake m_snake;
        private Food m_food;
        private int m_score;
        private double m_timeSinceLastUpdate;

        /// <summary>
        /// The snake entity.
        /// </summary>
        public Snake Snake => m_snake;

        /// <summary>
        /// The food entity.
        /// </summary>
        public Food Food => m_food;

        /// <summary>
        /// Current score.
        /// </summary>
        public int Score => m_score;

        /// <summary>
        /// Game events for observers.
        /// </summary>
        public GameEvents Events => m_events;

        public GameEngine(GameConfig config)
        {
            m_config = config;
            m_board = new GameBoard();
            m_events = new GameEvents();

            Reset();
        }

        /// <summary>
        /// Resets the game to initial state.
        /// </summary>
        public void Reset()
        {
            m_snake = new Snake(m_config.GridWidth / 2, m_config.GridHeight / 2);
            m_food = new Food();
            m_score = 0;
            m_timeSinceLastUpdate = 0;

            // Spawn initial food
            m_food.Spawn(m_board, m_snake);
        }

        /// <summary>
        /// Sets the snake's direction.
        /// </summary>
        public void SetDirection(Direction direction)
        {
            m_snake.SetDirection(direction);
        }

        /// <summary>
        /// Updates the game logic.
        /// </summary>
        /// <param name="deltaTime">Time elapsed since last update in seconds.</param>
        /// <returns>The result of the update.</returns>
        public UpdateResult Update(double deltaTime)
        {
            m_timeSinceLastUpdate += deltaTime;

            if (m_timeSinceLastUpdate >= m_config.UpdateInterval)
            {
                m_timeSinceLastUpdate = 0;

                // Move the snake
                m_snake.Move();

                // Check for wall collision
                if (CheckWallCollision())
                {
                    m_events.RaiseGameOver();
                    return UpdateResult.GameOver;
                }

                // Check for self collision
                if (m_snake.CheckSelfCollision())
                {
                    m_events.RaiseGameOver();
                    return UpdateResult.GameOver;
                }

                // Check for food collision
                if (m_snake.Head == m_food.Position)
                {
                    m_score += m_config.PointsPerFood;
                    m_snake.Grow();
                    m_food.Spawn(m_board, m_snake);

                    m_events.RaiseFoodEaten();
                    m_events.RaiseScoreChanged(m_score);

                    return UpdateResult.FoodEaten;
                }
            }

            return UpdateResult.Continue;
        }

        private bool CheckWallCollision()
        {
            Point head = m_snake.Head;
            return head.X < 0 || head.X >= m_config.GridWidth ||
                   head.Y < 0 || head.Y >= m_config.GridHeight;
        }
    }
}
