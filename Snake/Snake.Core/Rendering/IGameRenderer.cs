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
        /// Draws the in-game HUD: apples earned this run and the player's persistent total.
        /// </summary>
        void DrawApples(int sessionApples, int totalBalance);

        /// <summary>
        /// Draws the touch control buttons.
        /// </summary>
        void DrawTouchControls();

        /// <summary>
        /// Draws a sub-rectangle of a registered sprite sheet to the given destination
        /// rectangle on the virtual canvas. Sheets are loaded by name in LoadContent.
        /// </summary>
        void DrawSprite(string sheetName, Rectangle destination, Rectangle source);

        /// <summary>
        /// Draws a semi-transparent overlay.
        /// </summary>
        void DrawOverlay(Color color);

        /// <summary>
        /// Draws centered text at the specified vertical offset from center.
        /// </summary>
        void DrawCenteredText(string text, Color color, float yOffset);

        /// <summary>
        /// Draws a centered menu option label. The label position does not change between
        /// selected and unselected states; when selected, a "(" cursor is drawn to the left
        /// of the label without shifting it.
        /// </summary>
        void DrawMenuOption(string label, Color color, float yOffset, bool selected);

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
