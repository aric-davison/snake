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
            m_snake = new Snake(GameBoard.GRID_WIDTH / 2, GameBoard.GRID_HEIGHT / 2);
            m_food = new Food();
            m_gameState = GameState.Playing;
            m_score = 0;
            m_timeSinceLastUpdate = 0;

            // Spawn initial food
            m_food.Spawn(m_gameBoard, m_snake);

            base.Initialize();
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
                m_font = Content.Load<SpriteFont>("GameFont");
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
            // Allow exit with Escape key
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || 
                Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            switch (m_gameState)
            {
                case GameState.Playing:
                    UpdatePlaying(gameTime);
                    break;
                case GameState.GameOver:
                    UpdateGameOver();
                    break;
            }

            base.Update(gameTime);
        }

        /// <summary>
        /// Updates the game logic when in the Playing state.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        private void UpdatePlaying(GameTime gameTime)
        {
            // Handle input for snake direction
            HandleInput();

            // Update snake position based on time interval
            m_timeSinceLastUpdate += gameTime.ElapsedGameTime.TotalSeconds;
            
            if (m_timeSinceLastUpdate >= UPDATE_INTERVAL)
            {
                m_timeSinceLastUpdate = 0;
                // Move the snake                
                // Check for collisions with walls or self
                // Check for food collision              
            }
        }

        /// <summary>
        /// Updates the game logic when in the GameOver state.
        /// </summary>
        private void UpdateGameOver()
        {
            // Press Space to restart
            //if (Keyboard.GetState().IsKeyDown(Keys.Space))
            //{
            //    Initialize();
            //}
        }

        /// <summary>
        /// Handles keyboard input for controlling the snake direction.
        /// </summary>
        private void HandleInput()
        {
            KeyboardState keyState = Keyboard.GetState();

            // Keys.Up
            // Keys.Down,
            // Keyse.Left,
            // Keys.Right,

            // Keys.W
            // Keys.A
            // Keys.S
            // Keys.D
            
            //if (keyState.IsKeyDown(Keys.Up) || keyState.IsKeyDown(Keys.W))          
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
            DrawPlaying();            
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

            // Draw game over overlay
            if (m_font != null)
            {
                string gameOverText = "GAME OVER";
                string scoreText = $"Score: {m_score}";
                string restartText = "Press SPACE to restart";

                Vector2 gameOverSize = m_font.MeasureString(gameOverText);
                Vector2 scoreSize = m_font.MeasureString(scoreText);
                Vector2 restartSize = m_font.MeasureString(restartText);

                Vector2 gameOverPos = new Vector2(
                    (m_graphics.PreferredBackBufferWidth - gameOverSize.X) / 2,
                    (m_graphics.PreferredBackBufferHeight - gameOverSize.Y) / 2 - 50
                );

                Vector2 scorePos = new Vector2(
                    (m_graphics.PreferredBackBufferWidth - scoreSize.X) / 2,
                    gameOverPos.Y + 40
                );

                Vector2 restartPos = new Vector2(
                    (m_graphics.PreferredBackBufferWidth - restartSize.X) / 2,
                    scorePos.Y + 40
                );

                m_spriteBatch.DrawString(m_font, gameOverText, gameOverPos, Color.Red);
                m_spriteBatch.DrawString(m_font, scoreText, scorePos, Color.White);
                m_spriteBatch.DrawString(m_font, restartText, restartPos, Color.Yellow);
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
