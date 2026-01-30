using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;

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

        // Touch input support
        private Rectangle m_buttonUp;
        private Rectangle m_buttonDown;
        private Rectangle m_buttonLeft;
        private Rectangle m_buttonRight;
        private Rectangle m_buttonPause;
        private Rectangle m_buttonAction; // For start/restart
        private int m_buttonSize = 80;
        private int m_buttonMargin = 10;
        private bool m_previousTouchPressed;

        // Scaling for different screen sizes
        private int m_screenWidth;
        private int m_screenHeight;
        private int m_cellSize;
        private int m_gridOffsetX;
        private int m_gridOffsetY;

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

            // Use full screen on mobile platforms
            m_graphics.IsFullScreen = true;
            m_graphics.SupportedOrientations = DisplayOrientation.LandscapeLeft | DisplayOrientation.LandscapeRight;
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
            m_previousKeyState = Keyboard.GetState();
            m_previousTouchPressed = false;

            base.Initialize();

            // Get actual screen dimensions after base.Initialize()
            m_screenWidth = GraphicsDevice.Viewport.Width;
            m_screenHeight = GraphicsDevice.Viewport.Height;

            // Safe area margins for Android system UI (status bar, nav bar, notches)
            int safeMarginTop = 80;
            int safeMarginBottom = 100;
            int safeMarginX = 60;
            int safeHeight = m_screenHeight - safeMarginTop - safeMarginBottom;
            int safeWidth = m_screenWidth - safeMarginX * 2;

            // Score area at top
            int scoreHeight = 40;

            // Calculate cell size to fit grid in safe area (below score)
            int gridAreaHeight = safeHeight - scoreHeight;
            int cellByWidth = safeWidth / GameBoard.GRID_WIDTH;
            int cellByHeight = gridAreaHeight / GameBoard.GRID_HEIGHT;
            m_cellSize = Math.Min(cellByWidth, cellByHeight);
            m_cellSize = Math.Max(m_cellSize, 12); // Minimum cell size

            // Position grid: centered horizontally, below score in safe area
            int gridWidth = GameBoard.GRID_WIDTH * m_cellSize;
            int gridHeight = GameBoard.GRID_HEIGHT * m_cellSize;
            m_gridOffsetX = (m_screenWidth - gridWidth) / 2;
            m_gridOffsetY = safeMarginTop + scoreHeight;

            // BIG buttons for easy touch
            m_buttonSize = Math.Max(120, m_screenHeight / 4);
            m_buttonMargin = 10;

            // D-pad in bottom-left - BIG and easy to hit
            int dpadCenterX = m_buttonSize + 30;
            int dpadCenterY = m_screenHeight - m_buttonSize - 30;

            m_buttonUp = new Rectangle(
                dpadCenterX - m_buttonSize / 2,
                dpadCenterY - m_buttonSize - 5,
                m_buttonSize, m_buttonSize);
            m_buttonDown = new Rectangle(
                dpadCenterX - m_buttonSize / 2,
                dpadCenterY + 5,
                m_buttonSize, m_buttonSize);
            m_buttonLeft = new Rectangle(
                dpadCenterX - m_buttonSize - 5,
                dpadCenterY - m_buttonSize / 2,
                m_buttonSize, m_buttonSize);
            m_buttonRight = new Rectangle(
                dpadCenterX + m_buttonSize / 2 + 5,
                dpadCenterY - m_buttonSize / 2,
                m_buttonSize, m_buttonSize);

            // Pause button in bottom-right - also big
            m_buttonPause = new Rectangle(
                m_screenWidth - m_buttonSize - 30,
                m_screenHeight - m_buttonSize - 30,
                m_buttonSize, m_buttonSize);

            // Action button (start/restart) - centered on screen
            m_buttonAction = new Rectangle(
                m_screenWidth / 2 - m_buttonSize,
                m_screenHeight / 2 + 80,
                m_buttonSize * 2, m_buttonSize);

            ResetGame();
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

            // Check for pause (keyboard or touch)
            if (IsKeyPressed(keyState, Keys.P) || IsKeyPressed(keyState, Keys.Space) || IsPauseButtonPressed())
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
            // Press Space or Enter to restart (keyboard or touch)
            if (IsKeyPressed(keyState, Keys.Space) || IsKeyPressed(keyState, Keys.Enter) || IsActionButtonPressed())
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
            // Press P or Space to resume (keyboard or touch)
            if (IsKeyPressed(keyState, Keys.P) || IsKeyPressed(keyState, Keys.Space) || IsPauseButtonPressed())
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

            // Touch to start
            if (IsAnyTouchPressed())
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

            // Touch input
            HandleTouchInput();
        }

        /// <summary>
        /// Handles touch input for controlling the snake direction.
        /// </summary>
        private void HandleTouchInput()
        {
            TouchCollection touchState = TouchPanel.GetState();

            // Add padding for easier touch detection
            int padding = 20;

            foreach (TouchLocation touch in touchState)
            {
                if (touch.State == TouchLocationState.Pressed || touch.State == TouchLocationState.Moved)
                {
                    Point touchPoint = new Point((int)touch.Position.X, (int)touch.Position.Y);

                    // Expand hit areas with padding
                    Rectangle upHit = new Rectangle(m_buttonUp.X - padding, m_buttonUp.Y - padding,
                        m_buttonUp.Width + padding * 2, m_buttonUp.Height + padding * 2);
                    Rectangle downHit = new Rectangle(m_buttonDown.X - padding, m_buttonDown.Y - padding,
                        m_buttonDown.Width + padding * 2, m_buttonDown.Height + padding * 2);
                    Rectangle leftHit = new Rectangle(m_buttonLeft.X - padding, m_buttonLeft.Y - padding,
                        m_buttonLeft.Width + padding * 2, m_buttonLeft.Height + padding * 2);
                    Rectangle rightHit = new Rectangle(m_buttonRight.X - padding, m_buttonRight.Y - padding,
                        m_buttonRight.Width + padding * 2, m_buttonRight.Height + padding * 2);

                    if (upHit.Contains(touchPoint))
                        m_snake.SetDirection(Direction.Up);
                    else if (downHit.Contains(touchPoint))
                        m_snake.SetDirection(Direction.Down);
                    else if (leftHit.Contains(touchPoint))
                        m_snake.SetDirection(Direction.Left);
                    else if (rightHit.Contains(touchPoint))
                        m_snake.SetDirection(Direction.Right);
                }
            }
        }

        /// <summary>
        /// Checks if the pause button was just touched.
        /// </summary>
        private bool IsPauseButtonPressed()
        {
            TouchCollection touchState = TouchPanel.GetState();

            foreach (TouchLocation touch in touchState)
            {
                if (touch.State == TouchLocationState.Pressed)
                {
                    Point touchPoint = new Point((int)touch.Position.X, (int)touch.Position.Y);
                    if (m_buttonPause.Contains(touchPoint))
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Checks if any touch occurred (for starting game or other actions).
        /// </summary>
        private bool IsAnyTouchPressed()
        {
            TouchCollection touchState = TouchPanel.GetState();
            bool currentlyPressed = touchState.Count > 0;
            bool wasJustPressed = currentlyPressed && !m_previousTouchPressed;
            m_previousTouchPressed = currentlyPressed;
            return wasJustPressed;
        }

        /// <summary>
        /// Checks if the action button was touched.
        /// </summary>
        private bool IsActionButtonPressed()
        {
            TouchCollection touchState = TouchPanel.GetState();

            foreach (TouchLocation touch in touchState)
            {
                if (touch.State == TouchLocationState.Pressed)
                {
                    Point touchPoint = new Point((int)touch.Position.X, (int)touch.Position.Y);
                    if (m_buttonAction.Contains(touchPoint))
                        return true;
                }
            }
            return false;
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

            // Draw touch controls for mobile
            DrawTouchControls();
        }

        /// <summary>
        /// Draws the game when in the GameOver state.
        /// </summary>
        private void DrawGameOver()
        {
            DrawPlaying(); // Show final game state

            // Draw semi-transparent overlay
            Rectangle overlay = new Rectangle(0, 0, m_screenWidth, m_screenHeight);
            m_spriteBatch.Draw(m_pixelTexture, overlay, Color.Black * 0.7f);

            // Draw game over text
            if (m_font != null)
            {
                string gameOverText = "GAME OVER";
                string scoreText = $"Final Score: {m_score}";

                Vector2 gameOverSize = m_font.MeasureString(gameOverText);
                Vector2 scoreSize = m_font.MeasureString(scoreText);

                float centerX = m_screenWidth / 2;
                float centerY = m_screenHeight / 2;

                Vector2 gameOverPos = new Vector2(centerX - gameOverSize.X / 2, centerY - 60);
                Vector2 scorePos = new Vector2(centerX - scoreSize.X / 2, centerY - 10);

                m_spriteBatch.DrawString(m_font, gameOverText, gameOverPos, Color.Red);
                m_spriteBatch.DrawString(m_font, scoreText, scorePos, Color.White);
            }

            // Draw restart button
            DrawActionButton("TAP TO RESTART");
        }

        /// <summary>
        /// Draws the game when in the Paused state.
        /// </summary>
        private void DrawPaused()
        {
            DrawPlaying(); // Show current game state

            // Draw semi-transparent overlay
            Rectangle overlay = new Rectangle(0, 0, m_screenWidth, m_screenHeight);
            m_spriteBatch.Draw(m_pixelTexture, overlay, Color.Black * 0.5f);

            // Draw paused text
            if (m_font != null)
            {
                string pausedText = "PAUSED";
                string resumeText = "Tap || to resume";

                Vector2 pausedSize = m_font.MeasureString(pausedText);
                Vector2 resumeSize = m_font.MeasureString(resumeText);

                float centerX = m_screenWidth / 2;
                float centerY = m_screenHeight / 2;

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
                string controlsText = "Use D-pad to move";
                string pauseText = "Tap || to pause";
                string startText = "Tap anywhere to start";

                float centerX = m_screenWidth / 2;
                float centerY = m_screenHeight / 2;

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
                // Position score above the grid, centered
                Vector2 textSize = m_font.MeasureString(scoreText);
                Vector2 scorePosition = new Vector2(
                    m_screenWidth / 2 - textSize.X / 2,
                    m_gridOffsetY - 35);
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
                    m_gridOffsetX + x * m_cellSize,
                    m_gridOffsetY,
                    1,
                    GameBoard.GRID_HEIGHT * m_cellSize
                );
                m_spriteBatch.Draw(m_pixelTexture, line, Color.DarkGray * 0.3f);
            }

            // Draw horizontal lines
            for (int y = 0; y <= GameBoard.GRID_HEIGHT; y++)
            {
                Rectangle line = new Rectangle(
                    m_gridOffsetX,
                    m_gridOffsetY + y * m_cellSize,
                    GameBoard.GRID_WIDTH * m_cellSize,
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
                m_gridOffsetX + position.X * m_cellSize + 1,
                m_gridOffsetY + position.Y * m_cellSize + 1,
                m_cellSize - 2,
                m_cellSize - 2
            );
            m_spriteBatch.Draw(m_pixelTexture, cellRect, color);
        }

        /// <summary>
        /// Draws the touch control buttons for mobile devices.
        /// </summary>
        private void DrawTouchControls()
        {
            Color buttonColor = Color.White * 0.25f;
            Color buttonBorderColor = Color.White * 0.4f;

            // Draw D-pad buttons (no text labels - position makes it clear)
            DrawButton(m_buttonUp, buttonColor, buttonBorderColor, "U");
            DrawButton(m_buttonDown, buttonColor, buttonBorderColor, "D");
            DrawButton(m_buttonLeft, buttonColor, buttonBorderColor, "L");
            DrawButton(m_buttonRight, buttonColor, buttonBorderColor, "R");

            // Draw pause button
            DrawButton(m_buttonPause, buttonColor, buttonBorderColor, "P");
        }

        /// <summary>
        /// Draws a single button with border and optional label.
        /// </summary>
        private void DrawButton(Rectangle rect, Color fillColor, Color borderColor, string label)
        {
            // Draw border
            m_spriteBatch.Draw(m_pixelTexture, rect, borderColor);

            // Draw fill (slightly smaller)
            Rectangle innerRect = new Rectangle(rect.X + 2, rect.Y + 2, rect.Width - 4, rect.Height - 4);
            m_spriteBatch.Draw(m_pixelTexture, innerRect, fillColor);

            // Draw label
            if (m_font != null && !string.IsNullOrEmpty(label))
            {
                Vector2 labelSize = m_font.MeasureString(label);
                Vector2 labelPos = new Vector2(
                    rect.X + (rect.Width - labelSize.X) / 2,
                    rect.Y + (rect.Height - labelSize.Y) / 2
                );
                m_spriteBatch.DrawString(m_font, label, labelPos, Color.White * 0.7f);
            }
        }

        /// <summary>
        /// Draws the action button for start/restart screens.
        /// </summary>
        private void DrawActionButton(string label)
        {
            Color buttonColor = Color.Green * 0.5f;
            Color borderColor = Color.Green * 0.8f;

            // Draw border
            m_spriteBatch.Draw(m_pixelTexture, m_buttonAction, borderColor);

            // Draw fill
            Rectangle innerRect = new Rectangle(
                m_buttonAction.X + 2, m_buttonAction.Y + 2,
                m_buttonAction.Width - 4, m_buttonAction.Height - 4);
            m_spriteBatch.Draw(m_pixelTexture, innerRect, buttonColor);

            // Draw label
            if (m_font != null)
            {
                Vector2 labelSize = m_font.MeasureString(label);
                Vector2 labelPos = new Vector2(
                    m_buttonAction.X + (m_buttonAction.Width - labelSize.X) / 2,
                    m_buttonAction.Y + (m_buttonAction.Height - labelSize.Y) / 2
                );
                m_spriteBatch.DrawString(m_font, label, labelPos, Color.White);
            }
        }

        #endregion
    }
}
