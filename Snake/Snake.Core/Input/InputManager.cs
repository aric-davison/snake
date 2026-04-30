using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using Snake.Core.Configuration;

namespace Snake.Core.Input
{
    /// <summary>
    /// Manages all input sources and provides unified input state.
    /// Queries touch and keyboard once per frame for efficiency.
    /// </summary>
    public class InputManager : IInputManager
    {
        private readonly LayoutConfig m_layout;
        private readonly InputState m_currentState;

        private KeyboardState m_previousKeyState;
        private bool m_previousTouchPressed;
        private Direction? m_previousDirection;

        public InputState CurrentState => m_currentState;

        public InputManager(LayoutConfig layout)
        {
            m_layout = layout;
            m_currentState = new InputState();
            m_previousKeyState = Keyboard.GetState();
            m_previousTouchPressed = false;
        }

        public void Update()
        {
            m_currentState.Reset();

            // Get current input states (once per frame)
            KeyboardState keyState = Keyboard.GetState();
            GamePadState padState = GamePad.GetState(PlayerIndex.One);
            TouchCollection touchState = TouchPanel.GetState();

            // Check for exit
            if (padState.Buttons.Back == ButtonState.Pressed || keyState.IsKeyDown(Keys.Escape))
            {
                m_currentState.ExitRequested = true;
            }

            // Process keyboard input
            ProcessKeyboardInput(keyState);

            // Process touch input
            ProcessTouchInput(touchState);

            // Edge-detect direction for menu navigation
            if (m_currentState.RequestedDirection.HasValue &&
                m_currentState.RequestedDirection != m_previousDirection)
            {
                m_currentState.DirectionPressed = m_currentState.RequestedDirection;
            }
            m_previousDirection = m_currentState.RequestedDirection;

            // Check for any input (for start screen)
            bool currentTouchPressed = touchState.Count > 0;
            bool anyKeyPressed = keyState.GetPressedKeys().Length > 0 && m_previousKeyState.GetPressedKeys().Length == 0;
            bool anyTouchPressed = currentTouchPressed && !m_previousTouchPressed;

            if (anyKeyPressed || anyTouchPressed)
            {
                m_currentState.AnyInputPressed = true;
            }

            // Store previous states for next frame
            m_previousKeyState = keyState;
            m_previousTouchPressed = currentTouchPressed;
        }

        private void ProcessKeyboardInput(KeyboardState keyState)
        {
            // Direction input (arrow keys)
            if (keyState.IsKeyDown(Keys.Up) || keyState.IsKeyDown(Keys.W))
                m_currentState.RequestedDirection = Direction.Up;
            else if (keyState.IsKeyDown(Keys.Down) || keyState.IsKeyDown(Keys.S))
                m_currentState.RequestedDirection = Direction.Down;
            else if (keyState.IsKeyDown(Keys.Left) || keyState.IsKeyDown(Keys.A))
                m_currentState.RequestedDirection = Direction.Left;
            else if (keyState.IsKeyDown(Keys.Right) || keyState.IsKeyDown(Keys.D))
                m_currentState.RequestedDirection = Direction.Right;

            // Pause (only on key press, not hold)
            if (IsKeyPressed(keyState, Keys.P) || IsKeyPressed(keyState, Keys.Space))
            {
                m_currentState.PausePressed = true;
            }

            // Action (Space or Enter for start/restart)
            if (IsKeyPressed(keyState, Keys.Space) || IsKeyPressed(keyState, Keys.Enter))
            {
                m_currentState.ActionPressed = true;
            }
        }

        private void ProcessTouchInput(TouchCollection touchState)
        {
            foreach (TouchLocation touch in touchState)
            {
                Point touchPoint = new Point((int)touch.Position.X, (int)touch.Position.Y);

                if (touch.State == TouchLocationState.Pressed || touch.State == TouchLocationState.Moved)
                {
                    // Check D-pad buttons with expanded hit areas
                    if (m_layout.GetExpandedHitArea(m_layout.ButtonUp).Contains(touchPoint))
                        m_currentState.RequestedDirection = Direction.Up;
                    else if (m_layout.GetExpandedHitArea(m_layout.ButtonDown).Contains(touchPoint))
                        m_currentState.RequestedDirection = Direction.Down;
                    else if (m_layout.GetExpandedHitArea(m_layout.ButtonLeft).Contains(touchPoint))
                        m_currentState.RequestedDirection = Direction.Left;
                    else if (m_layout.GetExpandedHitArea(m_layout.ButtonRight).Contains(touchPoint))
                        m_currentState.RequestedDirection = Direction.Right;
                }

                if (touch.State == TouchLocationState.Pressed)
                {
                    // Check pause button
                    if (m_layout.ButtonPause.Contains(touchPoint))
                    {
                        m_currentState.PausePressed = true;
                    }

                    // Check action button
                    if (m_layout.ButtonAction.Contains(touchPoint))
                    {
                        m_currentState.ActionPressed = true;
                    }
                }
            }
        }

        private bool IsKeyPressed(KeyboardState current, Keys key)
        {
            return current.IsKeyDown(key) && m_previousKeyState.IsKeyUp(key);
        }
    }
}
