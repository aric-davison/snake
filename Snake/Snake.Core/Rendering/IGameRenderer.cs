using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Snake.Core.Rendering
{
    /// <summary>
    /// Interface for game rendering.
    /// Abstracts all drawing operations from game logic.
    /// </summary>
    public interface IGameRenderer
    {
        /// <summary>
        /// Loads content required for rendering.
        /// </summary>
        void LoadContent(GraphicsDevice device, ContentManager content);

        /// <summary>
        /// Begins a new render frame. Call before any Draw methods.
        /// </summary>
        void BeginFrame();

        /// <summary>
        /// Ends the current render frame. Call after all Draw methods.
        /// </summary>
        void EndFrame();

        /// <summary>
        /// Draws the game grid lines.
        /// </summary>
        void DrawGrid(int gridWidth, int gridHeight);

        /// <summary>
        /// Draws the snake entity.
        /// </summary>
        void DrawSnake(Snake snake);

        /// <summary>
        /// Draws the food entity.
        /// </summary>
        void DrawFood(Food food);

        /// <summary>
        /// Draws the current score.
        /// </summary>
        void DrawScore(int score);

        /// <summary>
        /// Draws the touch control buttons.
        /// </summary>
        void DrawTouchControls();

        /// <summary>
        /// Draws a semi-transparent overlay.
        /// </summary>
        void DrawOverlay(Color color);

        /// <summary>
        /// Draws centered text at the specified vertical offset from center.
        /// </summary>
        void DrawCenteredText(string text, Color color, float yOffset);

        /// <summary>
        /// Draws the action button with a label.
        /// </summary>
        void DrawActionButton(string label);

        /// <summary>
        /// Checks if a font is loaded (for conditional text rendering).
        /// </summary>
        bool HasFont { get; }
    }
}
