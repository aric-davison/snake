namespace Snake.Core.Input
{
    /// <summary>
    /// Represents the input state for a single frame.
    /// Captures all input sources (keyboard, touch) into a unified state.
    /// </summary>
    public class InputState
    {
        /// <summary>
        /// The direction requested by the player, if any.
        /// </summary>
        public Direction? RequestedDirection { get; set; }

        /// <summary>
        /// True if pause was requested this frame.
        /// </summary>
        public bool PausePressed { get; set; }

        /// <summary>
        /// True if the action button was pressed (start/restart).
        /// </summary>
        public bool ActionPressed { get; set; }

        /// <summary>
        /// True if any input was pressed (for starting game).
        /// </summary>
        public bool AnyInputPressed { get; set; }

        /// <summary>
        /// True if exit was requested (Escape or Back button).
        /// </summary>
        public bool ExitRequested { get; set; }

        /// <summary>
        /// Resets all input state to defaults.
        /// </summary>
        public void Reset()
        {
            RequestedDirection = null;
            PausePressed = false;
            ActionPressed = false;
            AnyInputPressed = false;
            ExitRequested = false;
        }
    }
}
