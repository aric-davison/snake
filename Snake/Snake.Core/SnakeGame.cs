using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Snake.Core
{
    /// <summary>
    /// Main game class that manages the game loop, rendering, and game state.
    /// This is the entry point for the MonoGame framework.
    /// </summary>
    public class SnakeGame : Game
    {
        #region Fields

        private GraphicsDeviceManager m_graphics;

        /// <summary>
        /// Sprites for rendering 2D graphics.
        /// </summary>
        private SpriteBatch m_spriteBatch;
        /// <summary>
        /// Board for the game
        /// </summary>
        private GameBoard m_gameBoard;
        private Snake m_snake;
        private Food m_food;
        private GameState m_gameState;
        private SpriteFont m_font;
        private Texture2D m_pixelTexture;
        private double m_timeSinceLastUpdate;
        private const double UPDATE_INTERVAL = 0.15; // Seconds between snake moves
        private int m_score;

        // Input tracking for single key press detection
        private KeyboardState m_previousKeyState;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the Game1 class.
        /// </summary>
        public SnakeGame()
        {
            m_graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            // Set window size based on game board dimensions
            m_graphics.PreferredBackBufferWidth = GameBoard.GRID_WIDTH * GameBoard.CELL_SIZE;
            m_graphics.PreferredBackBufferHeight = GameBoard.GRID_HEIGHT * GameBoard.CELL_SIZE + 50; // Extra space for score
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.
        /// </summary>
        protected override void Initialize()
        {
            m_gameBoard = new GameBoard();
            ResetGame();
            m_previousKeyState = Keyboard.GetState();

            base.Initialize();
        }

        /// <summary>
        /// Resets the game to initial state for starting a new game.
        /// </summary>
        private void ResetGame()
        {
            m_snake = new Snake(GameBoard.GRID_WIDTH / 2, GameBoard.GRID_HEIGHT / 2);
            m_food = new Food();
            m_gameState = GameState.Start;
            m_score = 0;
            m_timeSinceLastUpdate = 0;

            // Spawn initial food
            m_food.Spawn(m_gameBoard, m_snake);
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            m_spriteBatch = new SpriteBatch(GraphicsDevice);

            // Create a 1x1 white pixel texture for drawing rectangles
            m_pixelTexture = new Texture2D(GraphicsDevice, 1, 1);
            m_pixelTexture.SetData(new[] { Color.White });

            // Load font (you'll need to add a SpriteFont to your Content project)
            // For now, we'll handle the case where it might not exist
            try
            {
                m_font = Content.Load<SpriteFont>("Fonts/Hud");
            }
            catch
            {
                // Font loading will be handled in the README
                m_font = null;
            }
        }

        #endregion

        #region Update

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            KeyboardState currentKeyState = Keyboard.GetState();

            // Allow exit with Escape key
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
                currentKeyState.IsKeyDown(Keys.Escape))
                Exit();

            switch (m_gameState)
            {
                case GameState.Start:
                    UpdateStart(currentKeyState);
                    break;
                case GameState.Playing:
                    UpdatePlaying(gameTime, currentKeyState);
                    break;
                case GameState.GameOver:
                    UpdateGameOver(currentKeyState);
                    break;
                case GameState.Paused:
                    UpdatePaused(currentKeyState);
                    break;
            }

            m_previousKeyState = currentKeyState;
            base.Update(gameTime);
        }

        /// <summary>
        /// Updates the game logic when in the Playing state.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        /// <param name="keyState">Current keyboard state.</param>
        private void UpdatePlaying(GameTime gameTime, KeyboardState keyState)
        {
            // Handle input for snake direction
            HandleInput(keyState);

            // Check for pause
            if (IsKeyPressed(keyState, Keys.P) || IsKeyPressed(keyState, Keys.Space))
            {
                m_gameState = GameState.Paused;
                return;
            }

            // Update snake position based on time interval
            m_timeSinceLastUpdate += gameTime.ElapsedGameTime.TotalSeconds;

            if (m_timeSinceLastUpdate >= UPDATE_INTERVAL)
            {
                m_timeSinceLastUpdate = 0;

                // Move the snake
                m_snake.Move();

                // Check for wall collision
                if (CheckWallCollision())
                {
                    m_gameState = GameState.GameOver;
                    return;
                }

                // Check for self collision
                if (m_snake.CheckSelfCollision())
                {
                    m_gameState = GameState.GameOver;
                    return;
                }

                // Check for food collision
                if (m_snake.Head == m_food.Position)
                {
                    m_score += 10;
                    m_snake.Grow();
                    m_food.Spawn(m_gameBoard, m_snake);
                }
            }
        }

        /// <summary>
        /// Updates the game logic when in the GameOver state.
        /// </summary>
        /// <param name="keyState">Current keyboard state.</param>
        private void UpdateGameOver(KeyboardState keyState)
        {
            // Press Space or Enter to restart
            if (IsKeyPressed(keyState, Keys.Space) || IsKeyPressed(keyState, Keys.Enter))
            {
                ResetGame();
            }
        }

        /// <summary>
        /// Updates the game logic when in the Paused state.
        /// </summary>
        /// <param name="keyState">Current keyboard state.</param>
        private void UpdatePaused(KeyboardState keyState)
        {
            // Press P or Space to resume
            if (IsKeyPressed(keyState, Keys.P) || IsKeyPressed(keyState, Keys.Space))
            {
                m_gameState = GameState.Playing;
            }
        }

        /// <summary>
        /// Updates the game logic when in the Start state.
        /// </summary>
        /// <param name="keyState">Current keyboard state.</param>
        private void UpdateStart(KeyboardState keyState)
        {
            // Any key starts the game
            if (keyState.GetPressedKeys().Length > 0 && m_previousKeyState.GetPressedKeys().Length == 0)
            {
                m_gameState = GameState.Playing;
            }
        }

        /// <summary>
        /// Checks if a key was just pressed this frame (not held from previous frame).
        /// </summary>
        private bool IsKeyPressed(KeyboardState current, Keys key)
        {
            return current.IsKeyDown(key) && m_previousKeyState.IsKeyUp(key);
        }

        /// <summary>
        /// Checks if the snake head has collided with the wall boundaries.
        /// </summary>
        /// <returns>True if wall collision detected.</returns>
        private bool CheckWallCollision()
        {
            Point head = m_snake.Head;
            return head.X < 0 || head.X >= GameBoard.GRID_WIDTH ||
                   head.Y < 0 || head.Y >= GameBoard.GRID_HEIGHT;
        }

        /// <summary>
        /// Handles keyboard input for controlling the snake direction.
        /// </summary>
        /// <param name="keyState">Current keyboard state.</param>
        private void HandleInput(KeyboardState keyState)
        {
            // Arrow keys
            if (keyState.IsKeyDown(Keys.Up))
                m_snake.SetDirection(Direction.Up);
            else if (keyState.IsKeyDown(Keys.Down))
                m_snake.SetDirection(Direction.Down);
            else if (keyState.IsKeyDown(Keys.Left))
                m_snake.SetDirection(Direction.Left);
            else if (keyState.IsKeyDown(Keys.Right))
                m_snake.SetDirection(Direction.Right);

            // WASD keys
            else if (keyState.IsKeyDown(Keys.W))
                m_snake.SetDirection(Direction.Up);
            else if (keyState.IsKeyDown(Keys.S))
                m_snake.SetDirection(Direction.Down);
            else if (keyState.IsKeyDown(Keys.A))
                m_snake.SetDirection(Direction.Left);
            else if (keyState.IsKeyDown(Keys.D))
                m_snake.SetDirection(Direction.Right);
        }

        #endregion

        #region Draw

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            // Clears
            GraphicsDevice.Clear(Color.Black);

            m_spriteBatch.Begin();

            switch (m_gameState)
            {
                case GameState.Start:
                    DrawStart();
                    break;
                case GameState.Playing:
                    DrawPlaying();
                    break;
                case GameState.GameOver:
                    DrawGameOver();
                    break;
                case GameState.Paused:
                    DrawPaused();
                    break;
            }

            m_spriteBatch.End();

            base.Draw(gameTime);
        }

        /// <summary>
        /// Draws the game when in the Playing state.
        /// </summary>
        private void DrawPlaying()
        {
            // Draw game board grid (optional)
            DrawGrid();

            // Draw food
            DrawCell(m_food.Position, Color.Red);

            // Draw snake
            DrawCell(m_snake.Head, Color.Green);
            foreach (var segment in m_snake.Body)
            {
                DrawCell(segment, Color.LightGreen);
            }

            // Draw score
            DrawScore();
        }

        /// <summary>
        /// Draws the game when in the GameOver state.
        /// </summary>
        private void DrawGameOver()
        {
            DrawPlaying(); // Show final game state

            // Draw semi-transparent overlay
            Rectangle overlay = new Rectangle(0, 0, m_graphics.PreferredBackBufferWidth, m_graphics.PreferredBackBufferHeight);
            m_spriteBatch.Draw(m_pixelTexture, overlay, Color.Black * 0.7f);

            // Draw game over text
            if (m_font != null)
            {
                string gameOverText = "GAME OVER";
                string scoreText = $"Final Score: {m_score}";
                string restartText = "Press SPACE to restart";

                Vector2 gameOverSize = m_font.MeasureString(gameOverText);
                Vector2 scoreSize = m_font.MeasureString(scoreText);
                Vector2 restartSize = m_font.MeasureString(restartText);

                float centerX = m_graphics.PreferredBackBufferWidth / 2;
                float centerY = m_graphics.PreferredBackBufferHeight / 2;

                Vector2 gameOverPos = new Vector2(centerX - gameOverSize.X / 2, centerY - 60);
                Vector2 scorePos = new Vector2(centerX - scoreSize.X / 2, centerY - 10);
                Vector2 restartPos = new Vector2(centerX - restartSize.X / 2, centerY + 40);

                m_spriteBatch.DrawString(m_font, gameOverText, gameOverPos, Color.Red);
                m_spriteBatch.DrawString(m_font, scoreText, scorePos, Color.White);
                m_spriteBatch.DrawString(m_font, restartText, restartPos, Color.Yellow);
            }
        }

        /// <summary>
        /// Draws the game when in the Paused state.
        /// </summary>
        private void DrawPaused()
        {
            DrawPlaying(); // Show current game state

            // Draw semi-transparent overlay
            Rectangle overlay = new Rectangle(0, 0, m_graphics.PreferredBackBufferWidth, m_graphics.PreferredBackBufferHeight);
            m_spriteBatch.Draw(m_pixelTexture, overlay, Color.Black * 0.5f);

            // Draw paused text
            if (m_font != null)
            {
                string pausedText = "PAUSED";
                string resumeText = "Press SPACE to resume";

                Vector2 pausedSize = m_font.MeasureString(pausedText);
                Vector2 resumeSize = m_font.MeasureString(resumeText);

                float centerX = m_graphics.PreferredBackBufferWidth / 2;
                float centerY = m_graphics.PreferredBackBufferHeight / 2;

                Vector2 pausedPos = new Vector2(centerX - pausedSize.X / 2, centerY - 30);
                Vector2 resumePos = new Vector2(centerX - resumeSize.X / 2, centerY + 20);

                m_spriteBatch.DrawString(m_font, pausedText, pausedPos, Color.Yellow);
                m_spriteBatch.DrawString(m_font, resumeText, resumePos, Color.White);
            }
        }

        /// <summary>
        /// Draws the game when in the Start state.
        /// </summary>
        private void DrawStart()
        {
            DrawGrid();  // Show empty grid as background

            if (m_font != null)
            {
                string titleText = "SNAKE";
                string controlsText = "Arrow Keys or WASD to move";
                string pauseText = "P or SPACE to pause";
                string startText = "Press any key to start";

                float centerX = m_graphics.PreferredBackBufferWidth / 2;
                float centerY = m_graphics.PreferredBackBufferHeight / 2;

                Vector2 titleSize = m_font.MeasureString(titleText);
                Vector2 controlsSize = m_font.MeasureString(controlsText);
                Vector2 pauseSize = m_font.MeasureString(pauseText);
                Vector2 startSize = m_font.MeasureString(startText);

                m_spriteBatch.DrawString(m_font, titleText,
                    new Vector2(centerX - titleSize.X / 2, centerY - 80), Color.Green);
                m_spriteBatch.DrawString(m_font, controlsText,
                    new Vector2(centerX - controlsSize.X / 2, centerY - 20), Color.White);
                m_spriteBatch.DrawString(m_font, pauseText,
                    new Vector2(centerX - pauseSize.X / 2, centerY + 20), Color.White);
                m_spriteBatch.DrawString(m_font, startText,
                    new Vector2(centerX - startSize.X / 2, centerY + 70), Color.Yellow);
            }
        }

        /// <summary>
        /// Draws the current score at the top of the screen.
        /// </summary>
        private void DrawScore()
        {
            if (m_font != null)
            {
                string scoreText = $"Score: {m_score}";
                Vector2 scorePosition = new Vector2(10, GameBoard.GRID_HEIGHT * GameBoard.CELL_SIZE + 15);
                m_spriteBatch.DrawString(m_font, scoreText, scorePosition, Color.White);
            }
        }

        /// <summary>
        /// Draws a grid to visualize the game board cells.
        /// </summary>
        private void DrawGrid()
        {
            // Draw vertical lines
            for (int x = 0; x <= GameBoard.GRID_WIDTH; x++)
            {
                Rectangle line = new Rectangle(
                    x * GameBoard.CELL_SIZE,
                    0,
                    1,
                    GameBoard.GRID_HEIGHT * GameBoard.CELL_SIZE
                );
                m_spriteBatch.Draw(m_pixelTexture, line, Color.DarkGray * 0.3f);
            }

            // Draw horizontal lines
            for (int y = 0; y <= GameBoard.GRID_HEIGHT; y++)
            {
                Rectangle line = new Rectangle(
                    0,
                    y * GameBoard.CELL_SIZE,
                    GameBoard.GRID_WIDTH * GameBoard.CELL_SIZE,
                    1
                );
                m_spriteBatch.Draw(m_pixelTexture, line, Color.DarkGray * 0.3f);
            }
        }

        /// <summary>
        /// Draws a single cell on the game board at the specified position.
        /// </summary>
        /// <param name="position">The grid position of the cell.</param>
        /// <param name="color">The color to draw the cell.</param>
        private void DrawCell(Point position, Color color)
        {
            Rectangle cellRect = new Rectangle(
                position.X * GameBoard.CELL_SIZE + 1,
                position.Y * GameBoard.CELL_SIZE + 1,
                GameBoard.CELL_SIZE - 2,
                GameBoard.CELL_SIZE - 2
            );
            m_spriteBatch.Draw(m_pixelTexture, cellRect, color);
        }

        #endregion
    }
}
