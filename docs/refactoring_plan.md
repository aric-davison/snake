# Snake Game Refactoring Plan

## Overview

This document outlines a complete refactoring of the Snake game codebase to separate concerns, improve testability, and establish a solid foundation for visual improvements.

**Date:** 2026-02-04
**Current State:** Monolithic 765-line SnakeGame.cs handling all responsibilities
**Target State:** Clean separation of concerns with ~10 focused classes

---

## 1. Current Architecture Analysis

### 1.1 File Structure (Before)

```
Snake.Core/
├── SnakeGame.cs      (765 lines) - GOD CLASS
├── Snake.cs          (159 lines) - Good
├── Food.cs           (95 lines)  - Good
├── GameBoard.cs      (43 lines)  - Good
├── GameState.cs      (28 lines)  - Good (enum)
├── Direction.cs      (13 lines)  - Good (enum)
└── Localization/     - Unused
```

### 1.2 SnakeGame.cs Responsibilities (Problem)

| Responsibility | Lines | Code Location |
|----------------|-------|---------------|
| MonoGame lifecycle | ~50 | Initialize, LoadContent, Update, Draw |
| State management | ~50 | Switch statements, state transitions |
| Input handling (keyboard) | ~40 | HandleInput, IsKeyPressed |
| Input handling (touch) | ~80 | HandleTouchInput, 4 touch methods |
| Game logic | ~40 | UpdatePlaying, CheckWallCollision |
| Screen layout calculation | ~60 | Initialize (button positioning) |
| All rendering | ~200 | Draw methods (8 different methods) |
| Configuration (hard-coded) | ~30 | Magic numbers throughout |

### 1.3 Key Problems

1. **Single Responsibility Violation**: One class does everything
2. **Hard-coded values**: Colors, sizes, margins scattered throughout
3. **No input abstraction**: Touch queried 4 times per frame
4. **No rendering abstraction**: Can't add themes or effects
5. **No state pattern**: Switch statements for state management
6. **Untestable**: Everything coupled to MonoGame framework

---

## 2. Target Architecture

### 2.1 New File Structure

```
Snake.Core/
├── SnakeGame.cs              (reduced to ~150 lines) - MonoGame shell only
├── Snake.cs                  (unchanged)
├── Food.cs                   (unchanged)
├── GameBoard.cs              (unchanged)
├── GameState.cs              (unchanged)
├── Direction.cs              (unchanged)
│
├── Configuration/
│   ├── GameConfig.cs         - All game constants
│   ├── VisualConfig.cs       - Colors and visual settings
│   └── LayoutConfig.cs       - Screen layout and sizing
│
├── Input/
│   ├── IInputManager.cs      - Input abstraction interface
│   ├── InputManager.cs       - Unified keyboard + touch handling
│   └── InputState.cs         - Current frame input state
│
├── Rendering/
│   ├── IGameRenderer.cs      - Renderer interface
│   ├── GameRenderer.cs       - All drawing logic
│   └── ButtonRenderer.cs     - Touch control button rendering
│
├── States/
│   ├── IGameState.cs         - State interface
│   ├── StartState.cs         - Start screen state
│   ├── PlayingState.cs       - Active gameplay state
│   ├── PausedState.cs        - Paused state
│   └── GameOverState.cs      - Game over state
│
├── Engine/
│   ├── GameEngine.cs         - Pure game logic (no rendering)
│   └── GameEvents.cs         - Event definitions
│
└── Localization/             (unchanged, for future use)
```

### 2.2 Dependency Diagram

```
                    ┌─────────────┐
                    │  SnakeGame  │  (MonoGame entry point)
                    └──────┬──────┘
                           │
        ┌──────────────────┼──────────────────┐
        │                  │                  │
        ▼                  ▼                  ▼
┌───────────────┐  ┌───────────────┐  ┌───────────────┐
│ InputManager  │  │  GameEngine   │  │ GameRenderer  │
└───────────────┘  └───────┬───────┘  └───────────────┘
                           │
              ┌────────────┼────────────┐
              │            │            │
              ▼            ▼            ▼
        ┌─────────┐  ┌─────────┐  ┌─────────┐
        │  Snake  │  │  Food   │  │GameBoard│
        └─────────┘  └─────────┘  └─────────┘

All components use: GameConfig, VisualConfig, LayoutConfig
```

---

## 3. New Interfaces and Classes

### 3.1 Configuration Classes

#### GameConfig.cs
```csharp
namespace Snake.Core.Configuration
{
    public class GameConfig
    {
        // Timing
        public double UpdateInterval { get; set; } = 0.15;

        // Scoring
        public int PointsPerFood { get; set; } = 10;

        // Grid
        public int GridWidth { get; set; } = 30;
        public int GridHeight { get; set; } = 20;

        // Snake
        public int InitialSnakeLength { get; set; } = 3;
    }
}
```

#### VisualConfig.cs
```csharp
namespace Snake.Core.Configuration
{
    public class VisualConfig
    {
        // Snake colors
        public Color SnakeHeadColor { get; set; } = Color.Green;
        public Color SnakeBodyColor { get; set; } = Color.LightGreen;

        // Food color
        public Color FoodColor { get; set; } = Color.Red;

        // Grid colors
        public Color BackgroundColor { get; set; } = Color.Black;
        public Color GridLineColor { get; set; } = Color.DarkGray * 0.3f;

        // UI colors
        public Color ButtonFillColor { get; set; } = Color.White * 0.25f;
        public Color ButtonBorderColor { get; set; } = Color.White * 0.4f;
        public Color ButtonTextColor { get; set; } = Color.White * 0.7f;

        // Overlay colors
        public Color PauseOverlayColor { get; set; } = Color.Black * 0.5f;
        public Color GameOverOverlayColor { get; set; } = Color.Black * 0.7f;

        // Text colors
        public Color TitleColor { get; set; } = Color.Green;
        public Color ScoreColor { get; set; } = Color.White;
        public Color GameOverTextColor { get; set; } = Color.Red;
        public Color InstructionColor { get; set; } = Color.Yellow;
    }
}
```

#### LayoutConfig.cs
```csharp
namespace Snake.Core.Configuration
{
    public class LayoutConfig
    {
        // Safe area margins
        public int SafeMarginTop { get; set; } = 80;
        public int SafeMarginBottom { get; set; } = 100;
        public int SafeMarginX { get; set; } = 60;

        // Score area
        public int ScoreAreaHeight { get; set; } = 40;

        // Cell sizing
        public int MinCellSize { get; set; } = 12;

        // Button sizing
        public int MinButtonSize { get; set; } = 120;
        public int ButtonMargin { get; set; } = 10;
        public int ButtonPadding { get; set; } = 20;
        public int DPadOffset { get; set; } = 30;
        public int DPadGap { get; set; } = 5;

        // Computed values (set at runtime)
        public int ScreenWidth { get; set; }
        public int ScreenHeight { get; set; }
        public int CellSize { get; set; }
        public int GridOffsetX { get; set; }
        public int GridOffsetY { get; set; }
        public int ButtonSize { get; set; }

        // Button rectangles (computed)
        public Rectangle ButtonUp { get; set; }
        public Rectangle ButtonDown { get; set; }
        public Rectangle ButtonLeft { get; set; }
        public Rectangle ButtonRight { get; set; }
        public Rectangle ButtonPause { get; set; }
        public Rectangle ButtonAction { get; set; }

        public void CalculateLayout(int screenWidth, int screenHeight, int gridWidth, int gridHeight);
    }
}
```

### 3.2 Input System

#### IInputManager.cs
```csharp
namespace Snake.Core.Input
{
    public interface IInputManager
    {
        void Update();
        InputState GetState();
    }
}
```

#### InputState.cs
```csharp
namespace Snake.Core.Input
{
    public class InputState
    {
        public Direction? RequestedDirection { get; set; }
        public bool PausePressed { get; set; }
        public bool ActionPressed { get; set; }
        public bool AnyInputPressed { get; set; }
        public bool ExitRequested { get; set; }
    }
}
```

#### InputManager.cs
```csharp
namespace Snake.Core.Input
{
    public class InputManager : IInputManager
    {
        private readonly LayoutConfig m_layout;
        private KeyboardState m_previousKeyState;
        private bool m_previousTouchPressed;

        public InputManager(LayoutConfig layout);
        public void Update();
        public InputState GetState();

        private void ProcessKeyboardInput(InputState state, KeyboardState current);
        private void ProcessTouchInput(InputState state, TouchCollection touches);
    }
}
```

### 3.3 Rendering System

#### IGameRenderer.cs
```csharp
namespace Snake.Core.Rendering
{
    public interface IGameRenderer
    {
        void LoadContent(GraphicsDevice device, ContentManager content);
        void BeginFrame();
        void EndFrame();

        void DrawGrid();
        void DrawSnake(Snake snake);
        void DrawFood(Food food);
        void DrawScore(int score);
        void DrawTouchControls();
        void DrawOverlay(Color color);
        void DrawCenteredText(string text, Color color, float yOffset);
        void DrawActionButton(string label);
    }
}
```

#### GameRenderer.cs
```csharp
namespace Snake.Core.Rendering
{
    public class GameRenderer : IGameRenderer
    {
        private readonly VisualConfig m_visuals;
        private readonly LayoutConfig m_layout;
        private readonly GameConfig m_config;

        private SpriteBatch m_spriteBatch;
        private Texture2D m_pixelTexture;
        private SpriteFont m_font;

        public GameRenderer(GameConfig config, VisualConfig visuals, LayoutConfig layout);

        public void LoadContent(GraphicsDevice device, ContentManager content);
        public void BeginFrame();
        public void EndFrame();

        public void DrawGrid();
        public void DrawSnake(Snake snake);
        public void DrawFood(Food food);
        public void DrawScore(int score);
        public void DrawTouchControls();
        public void DrawOverlay(Color color);
        public void DrawCenteredText(string text, Color color, float yOffset);
        public void DrawActionButton(string label);

        private void DrawCell(Point position, Color color);
        private void DrawButton(Rectangle rect, Color fill, Color border, string label);
    }
}
```

### 3.4 State System

#### IGameStateHandler.cs
```csharp
namespace Snake.Core.States
{
    public interface IGameStateHandler
    {
        GameState StateType { get; }
        void Enter();
        void Exit();
        GameState? Update(GameTime gameTime, InputState input);
        void Draw(IGameRenderer renderer);
    }
}
```

#### PlayingState.cs (Example)
```csharp
namespace Snake.Core.States
{
    public class PlayingState : IGameStateHandler
    {
        private readonly GameEngine m_engine;
        private readonly GameConfig m_config;

        public GameState StateType => GameState.Playing;

        public PlayingState(GameEngine engine, GameConfig config);

        public void Enter() { }
        public void Exit() { }

        public GameState? Update(GameTime gameTime, InputState input)
        {
            if (input.PausePressed) return GameState.Paused;
            if (input.RequestedDirection.HasValue)
                m_engine.SetDirection(input.RequestedDirection.Value);

            var result = m_engine.Update(gameTime.ElapsedGameTime.TotalSeconds);
            if (result == GameEngine.UpdateResult.GameOver)
                return GameState.GameOver;

            return null;
        }

        public void Draw(IGameRenderer renderer)
        {
            renderer.DrawGrid();
            renderer.DrawFood(m_engine.Food);
            renderer.DrawSnake(m_engine.Snake);
            renderer.DrawScore(m_engine.Score);
            renderer.DrawTouchControls();
        }
    }
}
```

### 3.5 Game Engine

#### GameEngine.cs
```csharp
namespace Snake.Core.Engine
{
    public class GameEngine
    {
        public enum UpdateResult { Continue, GameOver, FoodEaten }

        private readonly GameConfig m_config;
        private readonly GameBoard m_board;
        private Snake m_snake;
        private Food m_food;
        private int m_score;
        private double m_timeSinceLastUpdate;

        public Snake Snake => m_snake;
        public Food Food => m_food;
        public int Score => m_score;

        public event Action<int> OnScoreChanged;
        public event Action OnGameOver;
        public event Action OnFoodEaten;

        public GameEngine(GameConfig config);

        public void Reset();
        public void SetDirection(Direction direction);
        public UpdateResult Update(double deltaTime);

        private bool CheckWallCollision();
    }
}
```

#### GameEvents.cs
```csharp
namespace Snake.Core.Engine
{
    public class GameEvents
    {
        public event Action<int> ScoreChanged;
        public event Action GameOver;
        public event Action FoodEaten;
        public event Action<GameState, GameState> StateChanged;

        public void RaiseScoreChanged(int newScore);
        public void RaiseGameOver();
        public void RaiseFoodEaten();
        public void RaiseStateChanged(GameState from, GameState to);
    }
}
```

---

## 4. Refactored SnakeGame.cs

The main game class becomes a thin shell:

```csharp
namespace Snake.Core
{
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
            m_graphics.IsFullScreen = true;
            m_graphics.SupportedOrientations = DisplayOrientation.LandscapeLeft | DisplayOrientation.LandscapeRight;
        }

        protected override void Initialize()
        {
            // Create configurations
            m_gameConfig = new GameConfig();
            m_visualConfig = new VisualConfig();
            m_layoutConfig = new LayoutConfig();

            base.Initialize();

            // Calculate layout
            m_layoutConfig.CalculateLayout(
                GraphicsDevice.Viewport.Width,
                GraphicsDevice.Viewport.Height,
                m_gameConfig.GridWidth,
                m_gameConfig.GridHeight);

            // Create systems
            m_inputManager = new InputManager(m_layoutConfig);
            m_engine = new GameEngine(m_gameConfig);

            // Create states
            m_states = new Dictionary<GameState, IGameStateHandler>
            {
                { GameState.Start, new StartState(m_engine) },
                { GameState.Playing, new PlayingState(m_engine, m_gameConfig) },
                { GameState.Paused, new PausedState(m_engine) },
                { GameState.GameOver, new GameOverState(m_engine) }
            };

            m_currentState = m_states[GameState.Start];
            m_currentState.Enter();
        }

        protected override void LoadContent()
        {
            m_renderer = new GameRenderer(m_gameConfig, m_visualConfig, m_layoutConfig);
            m_renderer.LoadContent(GraphicsDevice, Content);
        }

        protected override void Update(GameTime gameTime)
        {
            m_inputManager.Update();
            var input = m_inputManager.GetState();

            if (input.ExitRequested)
                Exit();

            var nextState = m_currentState.Update(gameTime, input);
            if (nextState.HasValue && nextState.Value != m_currentState.StateType)
            {
                m_currentState.Exit();
                m_currentState = m_states[nextState.Value];
                m_currentState.Enter();
            }

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(m_visualConfig.BackgroundColor);
            m_renderer.BeginFrame();
            m_currentState.Draw(m_renderer);
            m_renderer.EndFrame();
            base.Draw(gameTime);
        }
    }
}
```

---

## 5. Implementation Steps

### Phase 1: Configuration Extraction (No Behavior Change)
- [ ] Create `Configuration/` folder
- [ ] Create `GameConfig.cs` - extract all game constants
- [ ] Create `VisualConfig.cs` - extract all color values
- [ ] Create `LayoutConfig.cs` - extract layout calculations
- [ ] Update SnakeGame.cs to use configs (temporary, will be refactored)

### Phase 2: Input Abstraction
- [ ] Create `Input/` folder
- [ ] Create `InputState.cs` - input state container
- [ ] Create `IInputManager.cs` - interface
- [ ] Create `InputManager.cs` - implementation
- [ ] Update SnakeGame.cs to use InputManager

### Phase 3: Rendering Abstraction
- [ ] Create `Rendering/` folder
- [ ] Create `IGameRenderer.cs` - interface
- [ ] Create `GameRenderer.cs` - implementation
- [ ] Move all Draw methods to GameRenderer
- [ ] Update SnakeGame.cs to use GameRenderer

### Phase 4: State Pattern
- [ ] Create `States/` folder
- [ ] Create `IGameStateHandler.cs` - interface
- [ ] Create `StartState.cs`
- [ ] Create `PlayingState.cs`
- [ ] Create `PausedState.cs`
- [ ] Create `GameOverState.cs`
- [ ] Update SnakeGame.cs to use state handlers

### Phase 5: Game Engine Extraction
- [ ] Create `Engine/` folder
- [ ] Create `GameEngine.cs` - pure game logic
- [ ] Create `GameEvents.cs` - event definitions
- [ ] Move collision detection, scoring to GameEngine
- [ ] Update states to use GameEngine

### Phase 6: Final Cleanup
- [ ] Reduce SnakeGame.cs to thin shell (~150 lines)
- [ ] Remove all dead code
- [ ] Verify all tests pass (manual testing)
- [ ] Document any remaining TODOs

---

## 6. Validation Checklist

After refactoring, verify each item:

### Architecture
- [ ] SnakeGame.cs is under 200 lines
- [ ] No rendering code in SnakeGame.cs (except calling renderer)
- [ ] No input processing code in SnakeGame.cs (except calling input manager)
- [ ] No game logic in SnakeGame.cs (except calling engine)
- [ ] All configuration values in Config classes (no magic numbers in SnakeGame)

### Configuration
- [ ] `GameConfig.cs` exists with timing, scoring, grid settings
- [ ] `VisualConfig.cs` exists with all colors
- [ ] `LayoutConfig.cs` exists with layout calculations

### Input
- [ ] `IInputManager.cs` interface exists
- [ ] `InputManager.cs` consolidates keyboard + touch
- [ ] Touch is only queried once per frame
- [ ] `InputState.cs` captures all input for the frame

### Rendering
- [ ] `IGameRenderer.cs` interface exists
- [ ] `GameRenderer.cs` contains all drawing code
- [ ] Colors come from VisualConfig
- [ ] Positions come from LayoutConfig

### States
- [ ] `IGameStateHandler.cs` interface exists
- [ ] Each state (Start, Playing, Paused, GameOver) has its own class
- [ ] No switch statements on GameState in SnakeGame.cs
- [ ] State transitions are clean (Enter/Exit methods called)

### Engine
- [ ] `GameEngine.cs` exists with pure game logic
- [ ] Collision detection in GameEngine
- [ ] Score management in GameEngine
- [ ] No MonoGame dependencies in GameEngine (except Point for positions)

### Functionality
- [ ] Game starts correctly
- [ ] Snake moves with keyboard (arrow keys, WASD)
- [ ] Snake moves with touch controls
- [ ] Pause works (P key, Space, touch button)
- [ ] Game over triggers on wall collision
- [ ] Game over triggers on self collision
- [ ] Food spawns correctly
- [ ] Score increments on food eaten
- [ ] Restart works from game over screen

---

## 7. Files to Create

| File | Purpose | Estimated Lines |
|------|---------|-----------------|
| Configuration/GameConfig.cs | Game constants | ~30 |
| Configuration/VisualConfig.cs | Color settings | ~40 |
| Configuration/LayoutConfig.cs | Layout calculations | ~100 |
| Input/InputState.cs | Input data container | ~20 |
| Input/IInputManager.cs | Input interface | ~15 |
| Input/InputManager.cs | Input implementation | ~120 |
| Rendering/IGameRenderer.cs | Renderer interface | ~25 |
| Rendering/GameRenderer.cs | Renderer implementation | ~200 |
| States/IGameStateHandler.cs | State interface | ~20 |
| States/StartState.cs | Start screen | ~50 |
| States/PlayingState.cs | Active gameplay | ~60 |
| States/PausedState.cs | Paused state | ~50 |
| States/GameOverState.cs | Game over state | ~60 |
| Engine/GameEngine.cs | Pure game logic | ~100 |
| Engine/GameEvents.cs | Event definitions | ~40 |

**Total new code:** ~930 lines across 15 new files
**SnakeGame.cs after:** ~150 lines (down from 765)

---

## 8. Benefits After Refactoring

1. **Visual Improvements Ready**: Can easily modify VisualConfig to change the look
2. **Testable**: GameEngine has no rendering dependencies
3. **Maintainable**: Each class has one responsibility
4. **Extensible**: Easy to add new states, input methods, or rendering effects
5. **Configurable**: All values in one place, could load from file
6. **Performance**: Touch only queried once per frame

---

## Sign-off

This refactoring plan must be completed in full before any visual improvements are made. Each phase should be completed and tested before moving to the next.
