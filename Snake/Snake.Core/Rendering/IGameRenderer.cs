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
        /// Draws an apple sprite at the given grid position with a color tint. Used for
        /// Fortune-spawned bonus apples.
        /// </summary>
        void DrawAppleAt(Microsoft.Xna.Framework.Point position, Microsoft.Xna.Framework.Color tint);

        /// <summary>
        /// Draws the dedicated golden apple sprite at the given grid position.
        /// </summary>
        void DrawGoldenAppleAt(Microsoft.Xna.Framework.Point position);

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

        /// <summary>Draws a solid filled rectangle. Used for tuning layouts and masking.</summary>
        void DrawFilledRect(Rectangle rect, Color color);

        /// <summary>
        /// Draws centered text at the specified vertical offset from center.
        /// </summary>
        void DrawCenteredText(string text, Color color, float yOffset);

        /// <summary>
        /// Draws multi-line centered text. Each line (split on '\n') is centered horizontally
        /// on its own; lines stack downward starting at yOffset. Use for descriptions that
        /// don't fit on a single line.
        /// </summary>
        void DrawCenteredMultilineText(string text, Color color, float yOffset);

        /// <summary>
        /// Draws text horizontally centered on the given centerX, with its top edge at y.
        /// Used for laying out rows of labels/buttons at fixed x positions.
        /// </summary>
        void DrawTextCenteredAt(string text, int centerX, int y, Color color);

        /// <summary>
        /// Same as DrawTextCenteredAt but uses the small (HudSmall) font. Falls back to the
        /// main font if the small font failed to load.
        /// </summary>
        void DrawSmallTextCenteredAt(string text, int centerX, int y, Color color);

        /// <summary>
        /// Draws small-font text with its top-left at (x, y). When mirror is true the text
        /// is drawn with horizontal flip - used for repurposing arrow glyphs as their
        /// opposite direction.
        /// </summary>
        void DrawSmallTextAt(string text, int x, int y, Color color, bool mirror);

        /// <summary>Returns the rendered size of text in the small font.</summary>
        Vector2 MeasureSmallText(string text);

        /// <summary>
        /// Draws a centered menu option label. The label position does not change between
        /// selected and unselected states; when selected, a "(" cursor is drawn to the left
        /// of the label without shifting it.
        /// </summary>
        void DrawMenuOption(string label, Color color, float yOffset, bool selected);

        /// <summary>
        /// Draws a horizontal level slider as: "label   - - - - -" with the dash at index
        /// `level` overdrawn by "(" in indicatorColor. When selected, a "(" cursor is drawn
        /// to the left of the row.
        /// </summary>
        void DrawSlider(string label, int level, int max, Color labelColor, Color indicatorColor, float yOffset, bool selected);

        /// <summary>
        /// Tiles the menu_tile sprite across the entire virtual canvas as the menu background.
        /// </summary>
        void DrawMenuBackground();

        /// <summary>
        /// Tiles a sprite sheet (assumed to be a single tileWidth x tileHeight tile) across
        /// the given region. Edges are clipped to fit. Used for slot machine background fill.
        /// </summary>
        void DrawTiledRegion(string sheetName, Rectangle region, int tileWidth, int tileHeight);

        /// <summary>
        /// Composes a 9-slice frame from a 3x3 grid of tileSize x tileSize tiles in the
        /// source sheet (TL TM TR / ML MM MR / BL BM BR). Corners are drawn once; edges
        /// and the middle are tiled to fill the destination rect. dest.Width and dest.Height
        /// should each be at least 2*tileSize; sizes that aren't multiples of tileSize are
        /// handled via clipped tiles on the trailing edge.
        /// </summary>
        void DrawNineSlice(string sheetName, Rectangle dest, int tileSize);

        /// <summary>
        /// Composes a vertical three-slice (top cap / tiled middle / bottom cap) from the
        /// given source rects on a sheet, into the destination rect. Top and bottom caps
        /// are stretched horizontally to dest.Width and given capDestHeight. The middle
        /// source rect is tiled in middleTileDestHeight chunks to fill the gap between caps.
        /// </summary>
        void DrawVerticalThreeSlice(
            string sheetName,
            Rectangle dest,
            Rectangle topSrc,
            Rectangle middleSrc,
            Rectangle bottomSrc,
            int capDestHeight,
            int middleTileDestHeight);

        /// <summary>Width of the virtual canvas in pixels.</summary>
        int VirtualWidth { get; }

        /// <summary>Height of the virtual canvas in pixels.</summary>
        int VirtualHeight { get; }

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
