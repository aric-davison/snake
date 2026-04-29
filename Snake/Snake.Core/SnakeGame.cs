using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Snake.Core.Configuration;
using Snake.Core.Engine;
using Snake.Core.Input;
using Snake.Core.Rendering;
using Snake.Core.States;

namespace Snake.Core
{
    /// <summary>
    /// Main game class - thin shell that coordinates subsystems.
    /// MonoGame entry point that delegates to specialized components.
    /// </summary>
    public class SnakeGame : Game
    {
        private GraphicsDeviceManager m_graphics;

        // Configuration
        private GameConfig m_gameConfig;
        private VisualConfig m_visualConfig;
        private LayoutConfig m_layoutConfig;

        // Core systems
        private IInputManager m_inputManager;
        private IGameRenderer m_renderer;
        private GameEngine m_engine;

        // State management
        private Dictionary<GameState, IGameStateHandler> m_states;
        private IGameStateHandler m_currentState;

        public SnakeGame()
        {
            m_graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            // Platform-specific settings
            bool isMobile = OperatingSystem.IsAndroid() || OperatingSystem.IsIOS();

            if (isMobile)
            {
                // Full screen on mobile
                m_graphics.IsFullScreen = true;
                m_graphics.SupportedOrientations = DisplayOrientation.LandscapeLeft | DisplayOrientation.LandscapeRight;
            }
            else
            {
                // Windowed on desktop with reasonable size
                m_graphics.IsFullScreen = false;
                m_graphics.PreferredBackBufferWidth = 1280;
                m_graphics.PreferredBackBufferHeight = 720;
            }
        }

        protected override void Initialize()
        {
            // Create configurations with default values
            m_gameConfig = new GameConfig();
            m_visualConfig = new VisualConfig();
            m_layoutConfig = new LayoutConfig();

            base.Initialize();

            // Detect platform for layout adjustments
            bool isMobile = OperatingSystem.IsAndroid() || OperatingSystem.IsIOS();
            m_layoutConfig.IsMobilePlatform = isMobile;
            m_layoutConfig.ShowTouchControls = isMobile;

            // Calculate layout based on actual screen dimensions
            m_layoutConfig.CalculateLayout(
                GraphicsDevice.Viewport.Width,
                GraphicsDevice.Viewport.Height,
                m_gameConfig.GridWidth,
                m_gameConfig.GridHeight);

            // Create core systems
            m_inputManager = new InputManager(m_layoutConfig);
            m_engine = new GameEngine(m_gameConfig);

            // Create state handlers
            m_states = new Dictionary<GameState, IGameStateHandler>
            {
                { GameState.Menu, new MenuState(m_engine, m_gameConfig, m_visualConfig) },
                { GameState.Playing, new PlayingState(m_engine, m_gameConfig) },
                { GameState.Paused, new PausedState(m_engine, m_gameConfig, m_visualConfig) },
                { GameState.GameOver, new GameOverState(m_engine, m_gameConfig, m_visualConfig) }
            };

            m_currentState = m_states[GameState.Menu];
            m_currentState.Enter();
        }

        protected override void LoadContent()
        {
            // Create and initialize renderer
            m_renderer = new GameRenderer(m_visualConfig, m_layoutConfig);
            m_renderer.LoadContent(GraphicsDevice, Content);
        }

        protected override void Update(GameTime gameTime)
        {
            // Update input (queries all input sources once)
            m_inputManager.Update();
            var input = m_inputManager.CurrentState;

            // Handle exit request
            if (input.ExitRequested)
            {
                Exit();
                return;
            }

            // Update current state and check for transitions
            var nextState = m_currentState.Update(gameTime, input);

            if (nextState.HasValue && nextState.Value != m_currentState.StateType)
            {
                TransitionToState(nextState.Value);
            }

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            // Clear screen
            GraphicsDevice.Clear(m_visualConfig.BackgroundColor);

            // Render current state
            m_renderer.BeginFrame();
            m_currentState.Draw(m_renderer);
            m_renderer.EndFrame();

            base.Draw(gameTime);
        }

        private void TransitionToState(GameState newState)
        {
            m_currentState.Exit();
            m_currentState = m_states[newState];
            m_currentState.Enter();
        }
    }
}
