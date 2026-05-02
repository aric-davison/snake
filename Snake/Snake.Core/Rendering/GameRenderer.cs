using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Snake.Core.Configuration;

namespace Snake.Core.Rendering
{
    /// <summary>
    /// Handles all rendering for the game.
    /// Draws into a fixed-size virtual canvas (LayoutConfig.VirtualWidth x VirtualHeight)
    /// then upscales to the back buffer with point sampling for the retro pixel-perfect look.
    /// </summary>
    public class GameRenderer : IGameRenderer
    {
        // Snake sheet (Sprites/snake.png) - 32x32, 8x8 tiles in a 4x4 grid:
        //   Row 0: head_up    head_right  head_down   head_left
        //   Row 1: body_horiz body_vert   corner_UL   corner_UR
        //   Row 2: corner_DL  corner_DR   apple       golden_apple
        //   Row 3: tail_up    tail_right  tail_down   tail_left
        private const string SnakeSheet = "snake";
        private const string GrassSheet = "grass";
        private static readonly Rectangle SrcGrass = new Rectangle(0, 0, 16, 16);

        private const string MenuTileSheet = "menu_tile";
        private const string SlotsCabinetSheet = "slots_cabinet";
        private const string SlotsTrimSheet = "slots_trim";
        private const string SlotsWindowSheet = "slots_window";
        private const string SlotsSymbolsSheet = "slots_symbols";
        private static readonly Rectangle SrcHeadUp    = new Rectangle( 0,  0, 8, 8);
        private static readonly Rectangle SrcHeadRight = new Rectangle( 8,  0, 8, 8);
        private static readonly Rectangle SrcHeadDown  = new Rectangle(16,  0, 8, 8);
        private static readonly Rectangle SrcHeadLeft  = new Rectangle(24,  0, 8, 8);
        private static readonly Rectangle SrcBodyHoriz = new Rectangle( 0,  8, 8, 8);
        private static readonly Rectangle SrcBodyVert  = new Rectangle( 8,  8, 8, 8);
        private static readonly Rectangle SrcCornerUL  = new Rectangle(16,  8, 8, 8);
        private static readonly Rectangle SrcCornerUR  = new Rectangle(24,  8, 8, 8);
        private static readonly Rectangle SrcCornerDL  = new Rectangle( 0, 16, 8, 8);
        private static readonly Rectangle SrcCornerDR  = new Rectangle( 8, 16, 8, 8);
        private static readonly Rectangle SrcApple        = new Rectangle(16, 16, 8, 8);
        private static readonly Rectangle SrcGoldenApple  = new Rectangle(24, 16, 8, 8);
        private static readonly Rectangle SrcTailUp    = new Rectangle( 0, 24, 8, 8);
        private static readonly Rectangle SrcTailRight = new Rectangle( 8, 24, 8, 8);
        private static readonly Rectangle SrcTailDown  = new Rectangle(16, 24, 8, 8);
        private static readonly Rectangle SrcTailLeft  = new Rectangle(24, 24, 8, 8);

        private readonly VisualConfig m_visuals;
        private readonly LayoutConfig m_layout;
        private readonly Dictionary<string, Texture2D> m_sprites = new Dictionary<string, Texture2D>();

        private GraphicsDevice m_graphicsDevice;
        private SpriteBatch m_spriteBatch;
        private Texture2D m_pixelTexture;
        private SpriteFont m_font;
        private SpriteFont m_smallFont;
        private RenderTarget2D m_canvas;

        public bool HasFont => m_font != null;

        public GameRenderer(VisualConfig visuals, LayoutConfig layout)
        {
            m_visuals = visuals;
            m_layout = layout;
        }

        public void LoadContent(GraphicsDevice device, ContentManager content)
        {
            m_graphicsDevice = device;
            m_spriteBatch = new SpriteBatch(device);

            m_pixelTexture = new Texture2D(device, 1, 1);
            m_pixelTexture.SetData(new[] { Color.White });

            m_canvas = new RenderTarget2D(
                device,
                m_layout.VirtualWidth,
                m_layout.VirtualHeight,
                false,
                SurfaceFormat.Color,
                DepthFormat.None);

            try
            {
                m_font = content.Load<SpriteFont>("Fonts/Hud");
            }
            catch
            {
                m_font = null;
            }

            try
            {
                m_smallFont = content.Load<SpriteFont>("Fonts/HudSmall");
            }
            catch
            {
                m_smallFont = null;
            }

            TryLoadSheet(content, SnakeSheet, "Sprites/snake");
            TryLoadSheet(content, GrassSheet, "Sprites/grass");
            TryLoadSheet(content, MenuTileSheet, "Sprites/menu_tile");
            TryLoadSheet(content, SlotsCabinetSheet, "Sprites/slots_outer_cabinet");
            TryLoadSheet(content, SlotsTrimSheet, "Sprites/slots_cabinet_trim");
            TryLoadSheet(content, SlotsWindowSheet, "Sprites/slots_window");
            TryLoadSheet(content, SlotsSymbolsSheet, "Sprites/slots_symbols");
        }

        public const string SlotsCabinet = SlotsCabinetSheet;
        public const string SlotsTrim = SlotsTrimSheet;
        public const string SlotsWindow = SlotsWindowSheet;
        public const string SlotsSymbols = SlotsSymbolsSheet;

        private void TryLoadSheet(ContentManager content, string name, string asset)
        {
            try
            {
                m_sprites[name] = content.Load<Texture2D>(asset);
            }
            catch
            {
                // Sheet unavailable; renderer falls back to colored rectangles for affected tiles.
            }
        }

        public void BeginFrame()
        {
            m_graphicsDevice.SetRenderTarget(m_canvas);
            m_graphicsDevice.Clear(m_visuals.BackgroundColor);
            m_spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        }

        public void EndFrame()
        {
            m_spriteBatch.End();

            // Composite the virtual canvas onto the back buffer (letterboxed, integer scale).
            m_graphicsDevice.SetRenderTarget(null);
            m_graphicsDevice.Clear(m_visuals.BackgroundColor);
            m_spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            m_spriteBatch.Draw(m_canvas, m_layout.CanvasDestRect, Color.White);
            m_spriteBatch.End();
        }

        public void DrawGrid(int gridWidth, int gridHeight)
        {
            if (m_sprites.ContainsKey(GrassSheet))
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    for (int x = 0; x < gridWidth; x++)
                    {
                        Rectangle dest = m_layout.GetCellRectangle(new Point(x, y));
                        DrawSprite(GrassSheet, dest, SrcGrass);
                    }
                }
            }

            Color lineColor = m_visuals.GridLineColor;

            for (int x = 0; x <= gridWidth; x++)
            {
                Rectangle line = new Rectangle(
                    m_layout.GridOffsetX + x * m_layout.CellSize,
                    m_layout.GridOffsetY,
                    1,
                    gridHeight * m_layout.CellSize);
                m_spriteBatch.Draw(m_pixelTexture, line, lineColor);
            }

            for (int y = 0; y <= gridHeight; y++)
            {
                Rectangle line = new Rectangle(
                    m_layout.GridOffsetX,
                    m_layout.GridOffsetY + y * m_layout.CellSize,
                    gridWidth * m_layout.CellSize,
                    1);
                m_spriteBatch.Draw(m_pixelTexture, line, lineColor);
            }
        }

        public void DrawSnake(Snake snake)
        {
            if (!m_sprites.ContainsKey(SnakeSheet))
            {
                // Fallback: solid-colored cells when the sprite sheet failed to load.
                DrawCell(snake.Head, m_visuals.SnakeHeadColor);
                foreach (var seg in snake.Body)
                {
                    DrawCell(seg, m_visuals.SnakeBodyColor);
                }
                return;
            }

            var segments = new List<Point>(snake.AllSegments);
            int count = segments.Count;
            for (int i = 0; i < count; i++)
            {
                Rectangle src = PickSnakeTile(segments, i, count, snake.CurrentDirection);
                Rectangle dest = m_layout.GetCellRectangle(segments[i]);
                DrawSprite(SnakeSheet, dest, src);
            }
        }

        public void DrawFood(Food food)
        {
            if (!m_sprites.ContainsKey(SnakeSheet))
            {
                DrawCell(food.Position, m_visuals.FoodColor);
                return;
            }

            Rectangle dest = m_layout.GetCellRectangle(food.Position);
            DrawSprite(SnakeSheet, dest, SrcApple);
        }

        public void DrawAppleAt(Point position, Color tint)
        {
            if (!m_sprites.TryGetValue(SnakeSheet, out var sheet))
            {
                DrawCell(position, tint);
                return;
            }

            Rectangle dest = m_layout.GetCellRectangle(position);
            m_spriteBatch.Draw(sheet, dest, SrcApple, tint);
        }

        public void DrawGoldenAppleAt(Point position)
        {
            if (!m_sprites.TryGetValue(SnakeSheet, out var sheet))
            {
                DrawCell(position, Color.Gold);
                return;
            }

            Rectangle dest = m_layout.GetCellRectangle(position);
            m_spriteBatch.Draw(sheet, dest, SrcGoldenApple, Color.White);
        }

        public void DrawApples(int sessionApples, int totalBalance)
        {
            if (m_font == null) return;

            // Center the text, but pin the left edge to a min margin so wide values can't
            // clip the leading "A" off the canvas. Right side may trail off-screen instead.
            const float MinLeftMargin = 4f;
            string text = $"Apples: {sessionApples}    Total: {totalBalance}";
            Vector2 textSize = m_font.MeasureString(text);
            float centeredX = m_layout.VirtualWidth / 2f - textSize.X / 2f;
            Vector2 position = new Vector2(
                Math.Max(centeredX, MinLeftMargin),
                (m_layout.HudHeight - textSize.Y) / 2);

            m_spriteBatch.DrawString(m_font, text, position, m_visuals.ScoreColor);
        }

        public void DrawTouchControls()
        {
            if (!m_layout.ShowTouchControls)
                return;

            DrawButton(m_layout.ButtonUp, m_visuals.ButtonFillColor, m_visuals.ButtonBorderColor, "U");
            DrawButton(m_layout.ButtonDown, m_visuals.ButtonFillColor, m_visuals.ButtonBorderColor, "D");
            DrawButton(m_layout.ButtonLeft, m_visuals.ButtonFillColor, m_visuals.ButtonBorderColor, "L");
            DrawButton(m_layout.ButtonRight, m_visuals.ButtonFillColor, m_visuals.ButtonBorderColor, "R");
            DrawButton(m_layout.ButtonPause, m_visuals.ButtonFillColor, m_visuals.ButtonBorderColor, "||");
        }

        public void DrawSprite(string sheetName, Rectangle destination, Rectangle source)
        {
            if (m_sprites.TryGetValue(sheetName, out var sheet))
            {
                m_spriteBatch.Draw(sheet, destination, source, Color.White);
            }
        }

        public void DrawOverlay(Color color)
        {
            Rectangle overlay = new Rectangle(0, 0, m_layout.VirtualWidth, m_layout.VirtualHeight);
            m_spriteBatch.Draw(m_pixelTexture, overlay, color);
        }

        public void DrawFilledRect(Rectangle rect, Color color)
        {
            m_spriteBatch.Draw(m_pixelTexture, rect, color);
        }

        public void DrawCenteredText(string text, Color color, float yOffset)
        {
            if (m_font == null) return;

            Vector2 textSize = m_font.MeasureString(text);
            Vector2 position = new Vector2(
                m_layout.VirtualWidth / 2 - textSize.X / 2,
                m_layout.VirtualHeight / 2 + yOffset);

            m_spriteBatch.DrawString(m_font, text, position, color);
        }

        public void DrawCenteredMultilineText(string text, Color color, float yOffset)
        {
            if (m_font == null) return;

            string[] lines = text.Split('\n');
            float lineHeight = m_font.LineSpacing;
            float startY = m_layout.VirtualHeight / 2f + yOffset;

            for (int i = 0; i < lines.Length; i++)
            {
                Vector2 size = m_font.MeasureString(lines[i]);
                float x = m_layout.VirtualWidth / 2f - size.X / 2f;
                float y = startY + i * lineHeight;
                m_spriteBatch.DrawString(m_font, lines[i], new Vector2(x, y), color);
            }
        }

        public void DrawMenuBackground()
        {
            if (!m_sprites.TryGetValue(MenuTileSheet, out var sheet))
                return;

            Rectangle dest = new Rectangle(0, 0, m_layout.VirtualWidth, m_layout.VirtualHeight);
            m_spriteBatch.Draw(sheet, dest, Color.White);
        }

        public void DrawTiledRegion(string sheetName, Rectangle region, int tileWidth, int tileHeight)
        {
            if (!m_sprites.TryGetValue(sheetName, out var sheet))
                return;

            for (int y = region.Y; y < region.Y + region.Height; y += tileHeight)
            {
                for (int x = region.X; x < region.X + region.Width; x += tileWidth)
                {
                    int w = System.Math.Min(tileWidth, region.X + region.Width - x);
                    int h = System.Math.Min(tileHeight, region.Y + region.Height - y);
                    Rectangle dest = new Rectangle(x, y, w, h);
                    Rectangle src = new Rectangle(0, 0, w, h);
                    m_spriteBatch.Draw(sheet, dest, src, Color.White);
                }
            }
        }

        public int VirtualWidth => m_layout.VirtualWidth;
        public int VirtualHeight => m_layout.VirtualHeight;

        public void DrawNineSlice(string sheetName, Rectangle dest, int tileSize)
        {
            if (!m_sprites.TryGetValue(sheetName, out var sheet))
                return;

            int t = tileSize;
            int innerX = dest.X + t;
            int innerY = dest.Y + t;
            int innerWidth = dest.Width - 2 * t;
            int innerHeight = dest.Height - 2 * t;
            int rightX = dest.Right - t;
            int bottomY = dest.Bottom - t;

            // Source tiles in the 3x3 sheet.
            Rectangle srcTL = new Rectangle(0,     0,     t, t);
            Rectangle srcTM = new Rectangle(t,     0,     t, t);
            Rectangle srcTR = new Rectangle(2 * t, 0,     t, t);
            Rectangle srcML = new Rectangle(0,     t,     t, t);
            Rectangle srcMM = new Rectangle(t,     t,     t, t);
            Rectangle srcMR = new Rectangle(2 * t, t,     t, t);
            Rectangle srcBL = new Rectangle(0,     2 * t, t, t);
            Rectangle srcBM = new Rectangle(t,     2 * t, t, t);
            Rectangle srcBR = new Rectangle(2 * t, 2 * t, t, t);

            // Corners.
            m_spriteBatch.Draw(sheet, new Rectangle(dest.X,  dest.Y,  t, t), srcTL, Color.White);
            m_spriteBatch.Draw(sheet, new Rectangle(rightX,  dest.Y,  t, t), srcTR, Color.White);
            m_spriteBatch.Draw(sheet, new Rectangle(dest.X,  bottomY, t, t), srcBL, Color.White);
            m_spriteBatch.Draw(sheet, new Rectangle(rightX,  bottomY, t, t), srcBR, Color.White);

            // Top + bottom edges (tile horizontally, clip last tile if needed).
            for (int x = innerX; x < innerX + innerWidth; x += t)
            {
                int w = System.Math.Min(t, innerX + innerWidth - x);
                Rectangle topDest = new Rectangle(x, dest.Y, w, t);
                Rectangle botDest = new Rectangle(x, bottomY, w, t);
                Rectangle topSrc = new Rectangle(srcTM.X, srcTM.Y, w, t);
                Rectangle botSrc = new Rectangle(srcBM.X, srcBM.Y, w, t);
                m_spriteBatch.Draw(sheet, topDest, topSrc, Color.White);
                m_spriteBatch.Draw(sheet, botDest, botSrc, Color.White);
            }

            // Left + right edges (tile vertically, clip last tile if needed).
            for (int y = innerY; y < innerY + innerHeight; y += t)
            {
                int h = System.Math.Min(t, innerY + innerHeight - y);
                Rectangle leftDest  = new Rectangle(dest.X, y, t, h);
                Rectangle rightDest = new Rectangle(rightX, y, t, h);
                Rectangle leftSrc  = new Rectangle(srcML.X, srcML.Y, t, h);
                Rectangle rightSrc = new Rectangle(srcMR.X, srcMR.Y, t, h);
                m_spriteBatch.Draw(sheet, leftDest,  leftSrc,  Color.White);
                m_spriteBatch.Draw(sheet, rightDest, rightSrc, Color.White);
            }

            // Middle (tile both ways).
            for (int y = innerY; y < innerY + innerHeight; y += t)
            {
                int h = System.Math.Min(t, innerY + innerHeight - y);
                for (int x = innerX; x < innerX + innerWidth; x += t)
                {
                    int w = System.Math.Min(t, innerX + innerWidth - x);
                    Rectangle midDest = new Rectangle(x, y, w, h);
                    Rectangle midSrc  = new Rectangle(srcMM.X, srcMM.Y, w, h);
                    m_spriteBatch.Draw(sheet, midDest, midSrc, Color.White);
                }
            }
        }

        public void DrawVerticalThreeSlice(
            string sheetName,
            Rectangle dest,
            Rectangle topSrc,
            Rectangle middleSrc,
            Rectangle bottomSrc,
            int capDestHeight,
            int middleTileDestHeight)
        {
            if (!m_sprites.TryGetValue(sheetName, out var sheet))
                return;

            // Top cap.
            m_spriteBatch.Draw(
                sheet,
                new Rectangle(dest.X, dest.Y, dest.Width, capDestHeight),
                topSrc, Color.White);

            // Bottom cap.
            m_spriteBatch.Draw(
                sheet,
                new Rectangle(dest.X, dest.Bottom - capDestHeight, dest.Width, capDestHeight),
                bottomSrc, Color.White);

            // Tiled middle: stack copies of middleSrc between the caps. Last tile is
            // height-clipped (with proportional source clip) if the gap isn't a clean multiple.
            int innerY = dest.Y + capDestHeight;
            int innerEndY = dest.Bottom - capDestHeight;
            for (int y = innerY; y < innerEndY; y += middleTileDestHeight)
            {
                int h = System.Math.Min(middleTileDestHeight, innerEndY - y);
                int srcH = h == middleTileDestHeight
                    ? middleSrc.Height
                    : System.Math.Max(1, (int)((float)h / middleTileDestHeight * middleSrc.Height));
                m_spriteBatch.Draw(
                    sheet,
                    new Rectangle(dest.X, y, dest.Width, h),
                    new Rectangle(middleSrc.X, middleSrc.Y, middleSrc.Width, srcH),
                    Color.White);
            }
        }

        public void DrawTextCenteredAt(string text, int centerX, int y, Color color)
        {
            if (m_font == null) return;
            Vector2 size = m_font.MeasureString(text);
            Vector2 pos = new Vector2(centerX - size.X / 2, y);
            m_spriteBatch.DrawString(m_font, text, pos, color);
        }

        public void DrawSmallTextCenteredAt(string text, int centerX, int y, Color color)
        {
            SpriteFont font = m_smallFont ?? m_font;
            if (font == null) return;
            Vector2 size = font.MeasureString(text);
            Vector2 pos = new Vector2(centerX - size.X / 2, y);
            m_spriteBatch.DrawString(font, text, pos, color);
        }

        public void DrawSmallTextAt(string text, int x, int y, Color color, bool mirror)
        {
            SpriteFont font = m_smallFont ?? m_font;
            if (font == null) return;
            SpriteEffects effects = mirror ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            m_spriteBatch.DrawString(
                font, text, new Vector2(x, y), color,
                0f, Vector2.Zero, 1f, effects, 0f);
        }

        public Vector2 MeasureSmallText(string text)
        {
            SpriteFont font = m_smallFont ?? m_font;
            return font == null ? Vector2.Zero : font.MeasureString(text);
        }

        public void DrawMenuOption(string label, Color color, float yOffset, bool selected)
        {
            if (m_font == null) return;

            Vector2 textSize = m_font.MeasureString(label);
            float textX = m_layout.VirtualWidth / 2 - textSize.X / 2;
            float y = m_layout.VirtualHeight / 2 + yOffset;

            m_spriteBatch.DrawString(m_font, label, new Vector2(textX, y), color);

            if (selected)
            {
                const string cursor = "(";
                Vector2 cursorSize = m_font.MeasureString(cursor);
                float cursorX = textX - cursorSize.X - 2;
                m_spriteBatch.DrawString(m_font, cursor, new Vector2(cursorX, y), color);
            }
        }

        public void DrawSlider(string label, int level, int max, Color labelColor, Color indicatorColor, float yOffset, bool selected)
        {
            if (m_font == null) return;

            // Build "label   - - - - -" with the indicator slot already showing '('.
            var sb = new System.Text.StringBuilder();
            sb.Append(label);
            sb.Append("   ");
            for (int i = 0; i < max; i++)
            {
                if (i > 0) sb.Append(' ');
                sb.Append(i == level ? '(' : '-');
            }
            string row = sb.ToString();

            Vector2 rowSize = m_font.MeasureString(row);
            float rowX = m_layout.VirtualWidth / 2 - rowSize.X / 2;
            float y = m_layout.VirtualHeight / 2 + yOffset;

            // Pass 1: full row in labelColor.
            m_spriteBatch.DrawString(m_font, row, new Vector2(rowX, y), labelColor);

            // Pass 2: overdraw the indicator '(' in indicatorColor at its slot position.
            int indicatorCharIndex = label.Length + 3 + (level * 2);
            string prefix = row.Substring(0, indicatorCharIndex);
            float indicatorX = rowX + m_font.MeasureString(prefix).X;
            m_spriteBatch.DrawString(m_font, "(", new Vector2(indicatorX, y), indicatorColor);

            if (selected)
            {
                const string cursor = "(";
                Vector2 cursorSize = m_font.MeasureString(cursor);
                float cursorX = rowX - cursorSize.X - 2;
                m_spriteBatch.DrawString(m_font, cursor, new Vector2(cursorX, y), labelColor);
            }
        }

        public void DrawActionButton(string label)
        {
            m_spriteBatch.Draw(m_pixelTexture, m_layout.ButtonAction, m_visuals.ActionButtonBorderColor);

            Rectangle innerRect = m_layout.GetButtonInnerRect(m_layout.ButtonAction);
            m_spriteBatch.Draw(m_pixelTexture, innerRect, m_visuals.ActionButtonFillColor);

            if (m_font != null)
            {
                Vector2 labelSize = m_font.MeasureString(label);
                Vector2 labelPos = new Vector2(
                    m_layout.ButtonAction.X + (m_layout.ButtonAction.Width - labelSize.X) / 2,
                    m_layout.ButtonAction.Y + (m_layout.ButtonAction.Height - labelSize.Y) / 2);
                m_spriteBatch.DrawString(m_font, label, labelPos, m_visuals.ScoreColor);
            }
        }

        private static Rectangle PickSnakeTile(List<Point> segments, int i, int count, Direction headDirection)
        {
            // Head: face the snake's current direction.
            if (i == 0)
            {
                return headDirection switch
                {
                    Direction.Up => SrcHeadUp,
                    Direction.Down => SrcHeadDown,
                    Direction.Left => SrcHeadLeft,
                    _ => SrcHeadRight
                };
            }

            // Tail: sprite is named for the direction the body attaches (toward the head),
            // so tail_right means the next segment is to the right of the tail.
            if (i == count - 1)
            {
                Point tail = segments[i];
                Point prev = segments[i - 1];
                int dx = prev.X - tail.X;
                int dy = prev.Y - tail.Y;
                if (dx > 0) return SrcTailRight;
                if (dx < 0) return SrcTailLeft;
                if (dy > 0) return SrcTailDown;
                return SrcTailUp;
            }

            // Body: examine neighbors. If both neighbors are along the same axis it's
            // a straight segment, otherwise it's a corner whose orientation is the union
            // of the two neighbor directions.
            Point cur = segments[i];
            Point pSeg = segments[i - 1];
            Point nSeg = segments[i + 1];

            int prevDx = pSeg.X - cur.X;
            int prevDy = pSeg.Y - cur.Y;
            int nextDx = nSeg.X - cur.X;
            int nextDy = nSeg.Y - cur.Y;

            bool prevHoriz = prevDx != 0;
            bool nextHoriz = nextDx != 0;

            if (prevHoriz && nextHoriz) return SrcBodyHoriz;
            if (!prevHoriz && !nextHoriz) return SrcBodyVert;

            bool hasUp = prevDy < 0 || nextDy < 0;
            bool hasLeft = prevDx < 0 || nextDx < 0;
            bool hasRight = prevDx > 0 || nextDx > 0;

            if (hasUp && hasLeft) return SrcCornerUL;
            if (hasUp && hasRight) return SrcCornerUR;
            if (hasLeft) return SrcCornerDL;
            return SrcCornerDR;
        }

        private void DrawCell(Point position, Color color)
        {
            Rectangle cellRect = m_layout.GetCellRectangle(position);
            m_spriteBatch.Draw(m_pixelTexture, cellRect, color);
        }

        private void DrawButton(Rectangle rect, Color fillColor, Color borderColor, string label)
        {
            m_spriteBatch.Draw(m_pixelTexture, rect, borderColor);

            Rectangle innerRect = m_layout.GetButtonInnerRect(rect);
            m_spriteBatch.Draw(m_pixelTexture, innerRect, fillColor);

            if (m_font != null && !string.IsNullOrEmpty(label))
            {
                Vector2 labelSize = m_font.MeasureString(label);
                Vector2 labelPos = new Vector2(
                    rect.X + (rect.Width - labelSize.X) / 2,
                    rect.Y + (rect.Height - labelSize.Y) / 2);
                m_spriteBatch.DrawString(m_font, label, labelPos, m_visuals.ButtonTextColor);
            }
        }
    }
}
