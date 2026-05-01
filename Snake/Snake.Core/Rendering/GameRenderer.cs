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
        // Snake sheet (Sprites/snake.png) — 32x32, 8x8 tiles in a 4x4 grid:
        //   Row 0: head_up    head_right  head_down   head_left
        //   Row 1: body_horiz body_vert   corner_UL   corner_UR
        //   Row 2: corner_DL  corner_DR   apple       (empty)
        //   Row 3: tail_up    tail_right  tail_down   tail_left
        private const string SnakeSheet = "snake";
        private const string GrassSheet = "grass";
        private static readonly Rectangle SrcGrass = new Rectangle(0, 0, 16, 16);

        private const string MenuTileSheet = "menu_tile";
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
        private static readonly Rectangle SrcApple     = new Rectangle(16, 16, 8, 8);
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

            TryLoadSheet(content, SnakeSheet, "Sprites/snake");
            TryLoadSheet(content, GrassSheet, "Sprites/grass");
            TryLoadSheet(content, MenuTileSheet, "Sprites/menu_tile");
        }

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

        public void DrawApples(int sessionApples, int totalBalance)
        {
            if (m_font == null) return;

            string text = $"Apples: {sessionApples}    Total: {totalBalance}";
            Vector2 textSize = m_font.MeasureString(text);
            Vector2 position = new Vector2(
                m_layout.VirtualWidth / 2 - textSize.X / 2,
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

        public void DrawCenteredText(string text, Color color, float yOffset)
        {
            if (m_font == null) return;

            Vector2 textSize = m_font.MeasureString(text);
            Vector2 position = new Vector2(
                m_layout.VirtualWidth / 2 - textSize.X / 2,
                m_layout.VirtualHeight / 2 + yOffset);

            m_spriteBatch.DrawString(m_font, text, position, color);
        }

        public void DrawMenuBackground()
        {
            if (!m_sprites.TryGetValue(MenuTileSheet, out var sheet))
                return;

            Rectangle dest = new Rectangle(0, 0, m_layout.VirtualWidth, m_layout.VirtualHeight);
            m_spriteBatch.Draw(sheet, dest, Color.White);
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
