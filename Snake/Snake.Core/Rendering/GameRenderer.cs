using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Snake.Core.Configuration;

namespace Snake.Core.Rendering
{
    /// <summary>
    /// Handles all rendering for the game.
    /// Uses configuration classes for colors and layout.
    /// </summary>
    public class GameRenderer : IGameRenderer
    {
        private readonly VisualConfig m_visuals;
        private readonly LayoutConfig m_layout;

        private SpriteBatch m_spriteBatch;
        private Texture2D m_pixelTexture;
        private SpriteFont m_font;

        public bool HasFont => m_font != null;

        public GameRenderer(VisualConfig visuals, LayoutConfig layout)
        {
            m_visuals = visuals;
            m_layout = layout;
        }

        public void LoadContent(GraphicsDevice device, ContentManager content)
        {
            m_spriteBatch = new SpriteBatch(device);

            // Create a 1x1 white pixel texture for drawing rectangles
            m_pixelTexture = new Texture2D(device, 1, 1);
            m_pixelTexture.SetData(new[] { Color.White });

            // Load font
            try
            {
                m_font = content.Load<SpriteFont>("Fonts/Hud");
            }
            catch
            {
                m_font = null;
            }
        }

        public void BeginFrame()
        {
            m_spriteBatch.Begin();
        }

        public void EndFrame()
        {
            m_spriteBatch.End();
        }

        public void DrawGrid(int gridWidth, int gridHeight)
        {
            Color lineColor = m_visuals.GridLineColor;

            // Draw vertical lines
            for (int x = 0; x <= gridWidth; x++)
            {
                Rectangle line = new Rectangle(
                    m_layout.GridOffsetX + x * m_layout.CellSize,
                    m_layout.GridOffsetY,
                    1,
                    gridHeight * m_layout.CellSize);
                m_spriteBatch.Draw(m_pixelTexture, line, lineColor);
            }

            // Draw horizontal lines
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
            // Draw head
            DrawCell(snake.Head, m_visuals.SnakeHeadColor);

            // Draw body segments
            foreach (var segment in snake.Body)
            {
                DrawCell(segment, m_visuals.SnakeBodyColor);
            }
        }

        public void DrawFood(Food food)
        {
            DrawCell(food.Position, m_visuals.FoodColor);
        }

        public void DrawApples(int sessionApples, int totalBalance)
        {
            if (m_font == null) return;

            string text = $"Apples: {sessionApples}    Total: {totalBalance}";
            Vector2 textSize = m_font.MeasureString(text);
            Vector2 position = new Vector2(
                m_layout.ScreenWidth / 2 - textSize.X / 2,
                m_layout.GridOffsetY - m_layout.ScoreYOffset);

            m_spriteBatch.DrawString(m_font, text, position, m_visuals.ScoreColor);
        }

        public void DrawTouchControls()
        {
            // Skip drawing touch controls on desktop
            if (!m_layout.ShowTouchControls)
                return;

            // Draw D-pad buttons
            DrawButton(m_layout.ButtonUp, m_visuals.ButtonFillColor, m_visuals.ButtonBorderColor, "U");
            DrawButton(m_layout.ButtonDown, m_visuals.ButtonFillColor, m_visuals.ButtonBorderColor, "D");
            DrawButton(m_layout.ButtonLeft, m_visuals.ButtonFillColor, m_visuals.ButtonBorderColor, "L");
            DrawButton(m_layout.ButtonRight, m_visuals.ButtonFillColor, m_visuals.ButtonBorderColor, "R");

            // Draw pause button
            DrawButton(m_layout.ButtonPause, m_visuals.ButtonFillColor, m_visuals.ButtonBorderColor, "||");
        }

        public void DrawOverlay(Color color)
        {
            Rectangle overlay = new Rectangle(0, 0, m_layout.ScreenWidth, m_layout.ScreenHeight);
            m_spriteBatch.Draw(m_pixelTexture, overlay, color);
        }

        public void DrawCenteredText(string text, Color color, float yOffset)
        {
            if (m_font == null) return;

            Vector2 textSize = m_font.MeasureString(text);
            Vector2 position = new Vector2(
                m_layout.ScreenWidth / 2 - textSize.X / 2,
                m_layout.ScreenHeight / 2 + yOffset);

            m_spriteBatch.DrawString(m_font, text, position, color);
        }

        public void DrawActionButton(string label)
        {
            // Draw border
            m_spriteBatch.Draw(m_pixelTexture, m_layout.ButtonAction, m_visuals.ActionButtonBorderColor);

            // Draw fill
            Rectangle innerRect = m_layout.GetButtonInnerRect(m_layout.ButtonAction);
            m_spriteBatch.Draw(m_pixelTexture, innerRect, m_visuals.ActionButtonFillColor);

            // Draw label
            if (m_font != null)
            {
                Vector2 labelSize = m_font.MeasureString(label);
                Vector2 labelPos = new Vector2(
                    m_layout.ButtonAction.X + (m_layout.ButtonAction.Width - labelSize.X) / 2,
                    m_layout.ButtonAction.Y + (m_layout.ButtonAction.Height - labelSize.Y) / 2);
                m_spriteBatch.DrawString(m_font, label, labelPos, m_visuals.ScoreColor);
            }
        }

        private void DrawCell(Point position, Color color)
        {
            Rectangle cellRect = m_layout.GetCellRectangle(position);
            m_spriteBatch.Draw(m_pixelTexture, cellRect, color);
        }

        private void DrawButton(Rectangle rect, Color fillColor, Color borderColor, string label)
        {
            // Draw border
            m_spriteBatch.Draw(m_pixelTexture, rect, borderColor);

            // Draw fill (slightly smaller)
            Rectangle innerRect = m_layout.GetButtonInnerRect(rect);
            m_spriteBatch.Draw(m_pixelTexture, innerRect, fillColor);

            // Draw label
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
