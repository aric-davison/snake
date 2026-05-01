using System;
using Microsoft.Xna.Framework;
using Snake.Core.Configuration;
using Snake.Core.Persistence;
using Snake.Core.Upgrades;

namespace Snake.Core.Engine
{
    /// <summary>
    /// Core game logic engine. Contains no rendering code.
    /// Manages snake movement, collision detection, scoring, and food.
    /// </summary>
    public class GameEngine
    {
        public enum UpdateResult
        {
            Continue,
            GameOver,
            FoodEaten
        }

        // Per upgrade tier, snake moves this much faster (smaller interval) until clamped.
        private const double SpeedSecondsPerTier = 0.02;

        // Hard floor on update interval so snake never becomes uncontrollably fast.
        private const double MinUpdateInterval = 0.05;

        // Per Apple Value tier, each food yields this many additional apples.
        private const int ApplesPerValueTier = 1;

        private readonly GameConfig m_config;
        private readonly GameBoard m_board;
        private readonly GameEvents m_events;

        private Snake m_snake;
        private Food m_food;
        private PlayerData m_playerData;
        private int m_sessionApples;
        private double m_timeSinceLastUpdate;
        private double m_effectiveInterval;
        private int m_effectiveAppleValue;

        public Snake Snake => m_snake;
        public Food Food => m_food;
        public int SessionApples => m_sessionApples;
        public int AppleBalance => m_playerData.AppleBalance;
        public GameEvents Events => m_events;

        public GameEngine(GameConfig config)
        {
            m_config = config;
            m_board = new GameBoard(config);
            m_events = new GameEvents();

            // Initialize with empty data; SnakeGame will call Reset(PlayerData) before play.
            Reset(new PlayerData());
        }

        /// <summary>
        /// Resets the game to initial state and applies upgrade tiers from PlayerData.
        /// </summary>
        public void Reset(PlayerData playerData)
        {
            m_playerData = playerData;
            m_snake = new Snake(m_config.GridWidth / 2, m_config.GridHeight / 2);
            m_food = new Food();
            m_sessionApples = 0;
            m_timeSinceLastUpdate = 0;

            int speedTier = playerData.GetUpgradeTier(new SpeedUpgrade().Name);
            m_effectiveInterval = Math.Max(
                MinUpdateInterval,
                m_config.UpdateInterval - SpeedSecondsPerTier * speedTier);

            int appleValueTier = playerData.GetUpgradeTier(new AppleValueUpgrade().Name);
            m_effectiveAppleValue = m_config.ApplesPerFood + ApplesPerValueTier * appleValueTier;

            m_food.Spawn(m_board, m_snake);
        }

        public void SetDirection(Direction direction)
        {
            m_snake.SetDirection(direction);
        }

        public UpdateResult Update(double deltaTime)
        {
            m_timeSinceLastUpdate += deltaTime;

            if (m_timeSinceLastUpdate >= m_effectiveInterval)
            {
                m_timeSinceLastUpdate = 0;

                m_snake.Move();

                if (CheckWallCollision())
                {
                    m_events.RaiseGameOver();
                    return UpdateResult.GameOver;
                }

                if (m_snake.CheckSelfCollision())
                {
                    m_events.RaiseGameOver();
                    return UpdateResult.GameOver;
                }

                if (m_snake.Head == m_food.Position)
                {
                    m_sessionApples += m_effectiveAppleValue;
                    m_playerData.AppleBalance += m_effectiveAppleValue;
                    m_snake.Grow();
                    m_food.Spawn(m_board, m_snake);

                    m_events.RaiseFoodEaten();
                    m_events.RaiseScoreChanged(m_sessionApples);

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
