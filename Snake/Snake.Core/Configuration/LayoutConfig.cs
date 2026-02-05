using System;
using Microsoft.Xna.Framework;

namespace Snake.Core.Configuration
{
    /// <summary>
    /// Configuration for screen layout, sizing, and button positions.
    /// Call CalculateLayout() after screen dimensions are known.
    /// </summary>
    public class LayoutConfig
    {
        /// <summary>
        /// Whether to show touch controls. Set to false for desktop platforms.
        /// </summary>
        public bool ShowTouchControls { get; set; } = true;

        /// <summary>
        /// Whether running on a mobile platform (affects safe margins).
        /// </summary>
        public bool IsMobilePlatform { get; set; } = true;

        // Safe area margins (for notches, status bars, etc.)
        // These are adjusted in CalculateLayout based on IsMobilePlatform
        public int SafeMarginTop { get; set; } = 80;
        public int SafeMarginBottom { get; set; } = 100;
        public int SafeMarginX { get; set; } = 60;

        // Desktop margins (smaller, just for aesthetics)
        public int DesktopMarginTop { get; set; } = 20;
        public int DesktopMarginBottom { get; set; } = 20;
        public int DesktopMarginX { get; set; } = 20;

        // Score area
        public int ScoreAreaHeight { get; set; } = 40;
        public int ScoreYOffset { get; set; } = 35;

        // Cell sizing
        public int MinCellSize { get; set; } = 12;

        // Button sizing
        public int MinButtonSize { get; set; } = 120;
        public int ButtonMargin { get; set; } = 10;
        public int TouchPadding { get; set; } = 20;
        public int DPadOffset { get; set; } = 30;
        public int DPadGap { get; set; } = 5;

        // Cell rendering
        public int CellPadding { get; set; } = 1;
        public int ButtonBorderWidth { get; set; } = 2;

        // Computed values (set by CalculateLayout)
        public int ScreenWidth { get; private set; }
        public int ScreenHeight { get; private set; }
        public int CellSize { get; private set; }
        public int GridOffsetX { get; private set; }
        public int GridOffsetY { get; private set; }
        public int ButtonSize { get; private set; }

        // Button rectangles (computed)
        public Rectangle ButtonUp { get; private set; }
        public Rectangle ButtonDown { get; private set; }
        public Rectangle ButtonLeft { get; private set; }
        public Rectangle ButtonRight { get; private set; }
        public Rectangle ButtonPause { get; private set; }
        public Rectangle ButtonAction { get; private set; }

        /// <summary>
        /// Calculates all layout values based on screen dimensions.
        /// Must be called after graphics device is initialized.
        /// </summary>
        public void CalculateLayout(int screenWidth, int screenHeight, int gridWidth, int gridHeight)
        {
            ScreenWidth = screenWidth;
            ScreenHeight = screenHeight;

            // Use appropriate margins based on platform
            int marginTop = IsMobilePlatform ? SafeMarginTop : DesktopMarginTop;
            int marginBottom = IsMobilePlatform ? SafeMarginBottom : DesktopMarginBottom;
            int marginX = IsMobilePlatform ? SafeMarginX : DesktopMarginX;

            // Calculate safe area
            int safeHeight = screenHeight - marginTop - marginBottom;
            int safeWidth = screenWidth - marginX * 2;

            // Calculate cell size to fit grid in safe area (below score)
            int gridAreaHeight = safeHeight - ScoreAreaHeight;
            int cellByWidth = safeWidth / gridWidth;
            int cellByHeight = gridAreaHeight / gridHeight;
            CellSize = Math.Max(Math.Min(cellByWidth, cellByHeight), MinCellSize);

            // Position grid: centered horizontally, below score in safe area
            int totalGridWidth = gridWidth * CellSize;
            int totalGridHeight = gridHeight * CellSize;
            GridOffsetX = (screenWidth - totalGridWidth) / 2;
            GridOffsetY = marginTop + ScoreAreaHeight;

            // Calculate button size
            ButtonSize = Math.Max(MinButtonSize, screenHeight / 4);

            // D-pad in bottom-left
            int dpadCenterX = ButtonSize + DPadOffset;
            int dpadCenterY = screenHeight - ButtonSize - DPadOffset;

            ButtonUp = new Rectangle(
                dpadCenterX - ButtonSize / 2,
                dpadCenterY - ButtonSize - DPadGap,
                ButtonSize, ButtonSize);

            ButtonDown = new Rectangle(
                dpadCenterX - ButtonSize / 2,
                dpadCenterY + DPadGap,
                ButtonSize, ButtonSize);

            ButtonLeft = new Rectangle(
                dpadCenterX - ButtonSize - DPadGap,
                dpadCenterY - ButtonSize / 2,
                ButtonSize, ButtonSize);

            ButtonRight = new Rectangle(
                dpadCenterX + ButtonSize / 2 + DPadGap,
                dpadCenterY - ButtonSize / 2,
                ButtonSize, ButtonSize);

            // Pause button in bottom-right
            ButtonPause = new Rectangle(
                screenWidth - ButtonSize - DPadOffset,
                screenHeight - ButtonSize - DPadOffset,
                ButtonSize, ButtonSize);

            // Action button (start/restart) - centered on screen
            ButtonAction = new Rectangle(
                screenWidth / 2 - ButtonSize,
                screenHeight / 2 + 80,
                ButtonSize * 2, ButtonSize);
        }

        /// <summary>
        /// Gets an expanded hit area for a button (for easier touch detection).
        /// </summary>
        public Rectangle GetExpandedHitArea(Rectangle button)
        {
            return new Rectangle(
                button.X - TouchPadding,
                button.Y - TouchPadding,
                button.Width + TouchPadding * 2,
                button.Height + TouchPadding * 2);
        }

        /// <summary>
        /// Converts a grid position to screen coordinates.
        /// </summary>
        public Rectangle GetCellRectangle(Point gridPosition)
        {
            return new Rectangle(
                GridOffsetX + gridPosition.X * CellSize + CellPadding,
                GridOffsetY + gridPosition.Y * CellSize + CellPadding,
                CellSize - CellPadding * 2,
                CellSize - CellPadding * 2);
        }

        /// <summary>
        /// Gets the inner rectangle of a button (for fill, excluding border).
        /// </summary>
        public Rectangle GetButtonInnerRect(Rectangle button)
        {
            return new Rectangle(
                button.X + ButtonBorderWidth,
                button.Y + ButtonBorderWidth,
                button.Width - ButtonBorderWidth * 2,
                button.Height - ButtonBorderWidth * 2);
        }
    }
}
