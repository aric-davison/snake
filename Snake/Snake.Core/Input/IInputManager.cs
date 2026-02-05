namespace Snake.Core.Input
{
    /// <summary>
    /// Interface for input management.
    /// Abstracts input sources (keyboard, touch, gamepad) into unified input state.
    /// </summary>
    public interface IInputManager
    {
        /// <summary>
        /// Updates the input state. Call once per frame at the start of Update().
        /// </summary>
        void Update();

        /// <summary>
        /// Gets the current input state for this frame.
        /// </summary>
        InputState CurrentState { get; }
    }
}
