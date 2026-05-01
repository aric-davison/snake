using Microsoft.Xna.Framework;

namespace Snake.Core.Configuration
{
    /// <summary>
    /// Configuration for all visual elements including colors.
    /// </summary>
    public class VisualConfig
    {
        // Snake colors
        public Color SnakeHeadColor { get; set; } = Color.Green;
        public Color SnakeBodyColor { get; set; } = Color.LightGreen;

        // Food color
        public Color FoodColor { get; set; } = Color.Red;

        // Background and grid
        public Color BackgroundColor { get; set; } = new Color(0x88, 0x84, 0x7C);
        public Color GridLineColor { get; set; } = Color.Black;

        // Touch control buttons
        public Color ButtonFillColor { get; set; } = new Color(Color.White, 0.25f);
        public Color ButtonBorderColor { get; set; } = new Color(Color.White, 0.4f);
        public Color ButtonTextColor { get; set; } = new Color(Color.White, 0.7f);

        // Action button (start/restart)
        public Color ActionButtonFillColor { get; set; } = new Color(Color.Green, 0.5f);
        public Color ActionButtonBorderColor { get; set; } = new Color(Color.Green, 0.8f);

        // Overlays
        public Color PauseOverlayColor { get; set; } = new Color(Color.Black, 0.5f);
        public Color GameOverOverlayColor { get; set; } = new Color(Color.Black, 0.7f);

        // Text colors
        public Color TitleColor { get; set; } = Color.Green;
        public Color ScoreColor { get; set; } = Color.White;
        public Color GameOverTextColor { get; set; } = Color.Red;
        public Color PausedTextColor { get; set; } = Color.Yellow;
        public Color InstructionColor { get; set; } = Color.White;
        public Color HighlightColor { get; set; } = Color.Yellow;
    }
}
