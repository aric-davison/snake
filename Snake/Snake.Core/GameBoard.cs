using Snake.Core.Configuration;

namespace Snake.Core
{
    /// <summary>
    /// Represents the game board (playing field) for the Snake game.
    /// Defines the dimensions and boundaries of the play area.
    /// </summary>
    public class GameBoard
    {
        private readonly GameConfig m_config;

        public int GridWidth => m_config.GridWidth;
        public int GridHeight => m_config.GridHeight;

        public GameBoard(GameConfig config)
        {
            m_config = config;
        }
    }
}
