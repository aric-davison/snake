using Microsoft.Xna.Framework;
using Snake.Core.Input;
using Snake.Core.Rendering;

namespace Snake.Core.States
{
    /// <summary>
    /// Interface for game state handlers.
    /// Each game state (Start, Playing, Paused, GameOver) implements this interface.
    /// </summary>
    public interface IGameStateHandler
    {
        /// <summary>
        /// The state type this handler represents.
        /// </summary>
        GameState StateType { get; }

        /// <summary>
        /// Called when entering this state.
        /// </summary>
        void Enter();

        /// <summary>
        /// Called when exiting this state.
        /// </summary>
        void Exit();

        /// <summary>
        /// Updates the state logic.
        /// </summary>
        /// <param name="gameTime">Timing information.</param>
        /// <param name="input">Current input state.</param>
        /// <returns>The next state to transition to, or null to stay in current state.</returns>
        GameState? Update(GameTime gameTime, InputState input);

        /// <summary>
        /// Draws the state visuals.
        /// </summary>
        /// <param name="renderer">The game renderer.</param>
        void Draw(IGameRenderer renderer);
    }
}
