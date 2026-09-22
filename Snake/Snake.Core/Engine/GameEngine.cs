using System;
using System.Linq;
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

        // Per Apple Value tier, each food yields this many additional apples.
        private const int ApplesPerValueTier = 1;

        // Fortune: per-tier chance for a bonus apple to spawn alongside the primary one.
        private const float FortuneChancePerTier = 0.10f;

        // Frenzy: per-tier chance for a golden apple to spawn on a food event. Reward is flat.
        private const float FrenzyChancePerTier = 0.05f;
        private const float GoldenAppleLifetimeSec = 8f;
        public const int GoldenAppleReward = 25;

        private readonly GameConfig m_config;
        private readonly GameBoard m_board;
        private readonly GameEvents m_events;
        private readonly Random m_random = new Random();

        private Snake m_snake;
        private Food m_food;
        private Point? m_bonusApple;
        private GoldenApple m_goldenApple;
        private PlayerData m_playerData;
        private int m_sessionApples;
        private double m_timeSinceLastUpdate;
        private int m_effectiveAppleValue;
        private float m_fortuneChance;
        private float m_frenzyChance;

        public Snake Snake => m_snake;
        public Food Food => m_food;
        public Point? BonusApple => m_bonusApple;
        public GoldenApple GoldenApple => m_goldenApple;
        public int SessionApples => m_sessionApples;
        public int AppleBalance => m_playerData.AppleBalance;
        public GameEvents Events => m_events;
        public int AppleValue => m_effectiveAppleValue;

        /// <summary>
        /// Number of snake moves made in the current run.
        /// </summary>
        public int StepCount { get; private set; }

        /// <summary>
        /// Incremented on every Reset, so (RunNumber, StepCount) identifies one board position.
        /// </summary>
        public int RunNumber { get; private set; }

        public GameEngine(GameConfig config)
        {
            m_config = config;
            m_board = new GameBoard(config);
            m_events = new GameEvents();
            m_goldenApple = new GoldenApple();

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
            m_bonusApple = null;
            m_goldenApple.Deactivate();
            m_sessionApples = 0;
            m_timeSinceLastUpdate = 0;
            StepCount = 0;
            RunNumber++;

            int appleValueTier = playerData.GetUpgradeTier(new AppleValueUpgrade().Name);
            m_effectiveAppleValue = m_config.ApplesPerFood + ApplesPerValueTier * appleValueTier;

            int fortuneTier = playerData.GetUpgradeTier(new FortuneUpgrade().Name);
            m_fortuneChance = FortuneChancePerTier * fortuneTier;

            int frenzyTier = playerData.GetUpgradeTier(new FrenzyUpgrade().Name);
            m_frenzyChance = FrenzyChancePerTier * frenzyTier;

            m_food.Spawn(m_board, m_snake);
        }

        public void SetDirection(Direction direction)
        {
            m_snake.SetDirection(direction);
        }

        /// <summary>
        /// True if an Update with this deltaTime would move the snake.
        /// </summary>
        public bool IsStepDue(double deltaTime)
        {
            return m_timeSinceLastUpdate + deltaTime >= m_config.UpdateInterval;
        }

        public UpdateResult Update(double deltaTime)
        {
            // Tick the golden apple lifetime every frame, independent of snake step rate.
            m_goldenApple.Tick((float)deltaTime);

            m_timeSinceLastUpdate += deltaTime;

            if (m_timeSinceLastUpdate >= m_config.UpdateInterval)
            {
                m_timeSinceLastUpdate = 0;

                m_snake.Move();
                StepCount++;

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

                // Snake-eat checks: golden first (highest reward), then bonus, then primary.
                if (m_goldenApple.Active && m_snake.Head == m_goldenApple.Position)
                {
                    m_sessionApples += GoldenAppleReward;
                    m_playerData.AppleBalance += GoldenAppleReward;
                    m_snake.Grow();
                    m_goldenApple.Deactivate();
                    OnFoodEaten();
                    return UpdateResult.FoodEaten;
                }

                if (m_bonusApple.HasValue && m_snake.Head == m_bonusApple.Value)
                {
                    m_sessionApples += m_effectiveAppleValue;
                    m_playerData.AppleBalance += m_effectiveAppleValue;
                    m_snake.Grow();
                    m_bonusApple = null;
                    OnFoodEaten();
                    return UpdateResult.FoodEaten;
                }

                if (m_snake.Head == m_food.Position)
                {
                    m_sessionApples += m_effectiveAppleValue;
                    m_playerData.AppleBalance += m_effectiveAppleValue;
                    m_snake.Grow();
                    m_food.Spawn(m_board, m_snake);
                    OnFoodEaten();
                    return UpdateResult.FoodEaten;
                }
            }

            return UpdateResult.Continue;
        }

        private void OnFoodEaten()
        {
            // Roll Fortune: if no bonus apple is on the board, maybe spawn one.
            if (!m_bonusApple.HasValue && m_random.NextDouble() < m_fortuneChance)
            {
                Point? pos = PickFreeCell();
                if (pos.HasValue) m_bonusApple = pos;
            }

            // Roll Frenzy: if no golden apple is currently active, maybe spawn one.
            if (!m_goldenApple.Active && m_random.NextDouble() < m_frenzyChance)
            {
                Point? pos = PickFreeCell();
                if (pos.HasValue) m_goldenApple.Spawn(pos.Value, GoldenAppleLifetimeSec);
            }

            m_events.RaiseFoodEaten();
            m_events.RaiseScoreChanged(m_sessionApples);
        }

        /// <summary>
        /// Picks a random grid cell that is not occupied by the snake or any active apple.
        /// Returns null if no free cell could be found within the attempt budget.
        /// </summary>
        private Point? PickFreeCell()
        {
            for (int attempt = 0; attempt < 100; attempt++)
            {
                int x = m_random.Next(0, m_config.GridWidth);
                int y = m_random.Next(0, m_config.GridHeight);
                Point p = new Point(x, y);

                if (m_snake.AllSegments.Contains(p)) continue;
                if (m_food.Position == p) continue;
                if (m_bonusApple.HasValue && m_bonusApple.Value == p) continue;
                if (m_goldenApple.Active && m_goldenApple.Position == p) continue;

                return p;
            }
            return null;
        }

        private bool CheckWallCollision()
        {
            Point head = m_snake.Head;
            return head.X < 0 || head.X >= m_config.GridWidth ||
                   head.Y < 0 || head.Y >= m_config.GridHeight;
        }
    }
}
