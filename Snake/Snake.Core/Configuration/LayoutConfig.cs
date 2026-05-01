using System;
using Microsoft.Xna.Framework;

namespace Snake.Core.Configuration
{
    /// <summary>
    /// Configuration for screen layout, sizing, and button positions.
    /// The game renders to a fixed virtual canvas (VirtualWidth x VirtualHeight) and
    /// the canvas is upscaled with point sampling to fit the physical screen.
    /// Cell-space coordinates (grid, HUD, overlays) are in virtual pixels.
    /// Touch button rectangles remain in physical screen pixels so finger targets
    /// stay a sensible physical size on mobile.
    /// </summary>
    public class LayoutConfig
    {
        public bool ShowTouchControls { get; set; } = true;
        public bool IsMobilePlatform { get; set; } = true;

        // Mobile safe margins (touch buttons in physical space)
        public int SafeMarginTop { get; set; } = 80;
        public int SafeMarginBottom { get; set; } = 100;
        public int SafeMarginX { get; set; } = 60;

        // Desktop margins (touch buttons in physical space)
        public int DesktopMarginTop { get; set; } = 20;
        public int DesktopMarginBottom { get; set; } = 20;
        public int DesktopMarginX { get; set; } = 20;

        // Touch button sizing (physical space)
        public int MinButtonSize { get; set; } = 120;
        public int ButtonMargin { get; set; } = 10;
        public int TouchPadding { get; set; } = 20;
        public int DPadOffset { get; set; } = 30;
        public int DPadGap { get; set; } = 5;
        public int ButtonBorderWidth { get; set; } = 2;

        // Virtual canvas settings (cell-space)
        public int VirtualWidth { get; set; } = 256;
        public int VirtualHeight { get; set; } = 180;
        public int CellSize { get; set; } = 16;
        public int HudHeight { get; set; } = 16;
        public int CellPadding { get; set; } = 0;

        // Computed: physical screen
        public int ScreenWidth { get; private set; }
        public int ScreenHeight { get; private set; }

        // Computed: where the upscaled virtual canvas sits in physical space
        public int CanvasScale { get; private set; }
        public Rectangle CanvasDestRect { get; private set; }

        // Computed: virtual-space grid placement
        public int GridOffsetX { get; private set; }
        public int GridOffsetY { get; private set; }

        // Computed: physical-space touch button placement
        public int ButtonSize { get; private set; }
        public Rectangle ButtonUp { get; private set; }
        public Rectangle ButtonDown { get; private set; }
        public Rectangle ButtonLeft { get; private set; }
        public Rectangle ButtonRight { get; private set; }
        public Rectangle ButtonPause { get; private set; }
        public Rectangle ButtonAction { get; private set; }

        public void CalculateLayout(int screenWidth, int screenHeight, int gridWidth, int gridHeight)
        {
            ScreenWidth = screenWidth;
            ScreenHeight = screenHeight;

            // --- Virtual-space layout ---
            int gridPixelsWide = gridWidth * CellSize;
            int gridPixelsTall = gridHeight * CellSize;
            GridOffsetX = (VirtualWidth - gridPixelsWide) / 2;
            GridOffsetY = HudHeight + (VirtualHeight - HudHeight - gridPixelsTall) / 2;

            // --- Canvas-to-screen upscale (integer scale, letterboxed) ---
            int scaleX = screenWidth / VirtualWidth;
            int scaleY = screenHeight / VirtualHeight;
            CanvasScale = Math.Max(1, Math.Min(scaleX, scaleY));
            int destW = VirtualWidth * CanvasScale;
            int destH = VirtualHeight * CanvasScale;
            CanvasDestRect = new Rectangle(
                (screenWidth - destW) / 2,
                (screenHeight - destH) / 2,
                destW,
                destH);

            // --- Physical-space touch button layout (unchanged behavior) ---
            ButtonSize = Math.Max(MinButtonSize, screenHeight / 4);

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

            ButtonPause = new Rectangle(
                screenWidth - ButtonSize - DPadOffset,
                screenHeight - ButtonSize - DPadOffset,
                ButtonSize, ButtonSize);

            ButtonAction = new Rectangle(
                screenWidth / 2 - ButtonSize,
                screenHeight / 2 + 80,
                ButtonSize * 2, ButtonSize);
        }

        public Rectangle GetExpandedHitArea(Rectangle button)
        {
            return new Rectangle(
                button.X - TouchPadding,
                button.Y - TouchPadding,
                button.Width + TouchPadding * 2,
                button.Height + TouchPadding * 2);
        }

        public Rectangle GetCellRectangle(Point gridPosition)
        {
            return new Rectangle(
                GridOffsetX + gridPosition.X * CellSize + CellPadding,
                GridOffsetY + gridPosition.Y * CellSize + CellPadding,
                CellSize - CellPadding * 2,
                CellSize - CellPadding * 2);
        }

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
