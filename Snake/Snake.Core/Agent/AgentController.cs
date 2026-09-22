using Snake.Core.Configuration;
using Snake.Core.Engine;
using Snake.Core.Input;

namespace Snake.Core.Agent
{
    /// <summary>
    /// Lets an external agent play the game by writing into the frame's InputState, the same
    /// way the keyboard does. Keyboard input still works and takes priority.
    ///
    /// While Playing, the board is sent to the agent after every snake move, and its reply
    /// steers the next move. If the snake is due to move before the reply arrives, the frame
    /// is held, so a slow agent slows the game down rather than acting on a stale board.
    /// On the Menu and Game Over screens it selects the first option (Play / Play Again)
    /// after a short delay, unless the player has started navigating the menu.
    /// </summary>
    public class AgentController
    {
        private const double MenuStartDelaySec = 1.0;
        private const double GameOverRestartDelaySec = 2.0;
        private const double DecisionTimeoutSec = 5.0;

        private readonly IAgentLink m_link;
        private readonly GameEngine m_engine;
        private readonly GameConfig m_config;

        private bool m_ready;
        private int m_nextId;

        // Request awaiting a reply, and the board position (run, step) it was made for.
        private int m_pendingId = -1;
        private (int Run, int Step) m_pendingFor;
        private double m_pendingAge;

        // Latest reply, and the position it applies to.
        private (int Run, int Step) m_decidedFor = (-1, -1);
        private Direction? m_decision;

        private GameState? m_lastState;
        private double m_screenTime;
        private bool m_playerNavigated;

        /// <summary>
        /// Short status line, e.g. "LAYA: UP 97%", shown in the window title.
        /// </summary>
        public string Status { get; private set; } = "LAYA: LOADING";

        public AgentController(IAgentLink link, GameEngine engine, GameConfig config)
        {
            m_link = link;
            m_engine = engine;
            m_config = config;
        }

        /// <summary>
        /// Call once per frame, before the current state's Update.
        /// </summary>
        /// <returns>False if the game should skip this frame's state update to wait for the agent.</returns>
        public bool Update(GameState state, InputState input, double deltaTime)
        {
            ReadReplies();

            if (state != m_lastState)
            {
                OnStateChanged(m_lastState, state);
                m_lastState = state;
            }

            if (!m_link.IsConnected)
            {
                Status = "LAYA: OFFLINE";
                return true;
            }
            if (!m_ready)
            {
                return true;
            }

            switch (state)
            {
                case GameState.Playing:
                    return UpdatePlaying(input, deltaTime);
                case GameState.Menu:
                    AutoSelectFirstOption(input, deltaTime, MenuStartDelaySec);
                    break;
                case GameState.GameOver:
                    AutoSelectFirstOption(input, deltaTime, GameOverRestartDelaySec);
                    break;
            }
            return true;
        }

        private bool UpdatePlaying(InputState input, double deltaTime)
        {
            var position = (m_engine.RunNumber, m_engine.StepCount);

            if (m_decidedFor != position && m_pendingId < 0)
            {
                m_pendingId = m_nextId++;
                m_pendingFor = position;
                m_pendingAge = 0;
                m_link.Send(AgentProtocol.Observe(m_pendingId, m_engine, m_config));
            }

            if (m_decidedFor == position && m_decision.HasValue && !input.RequestedDirection.HasValue)
            {
                input.RequestedDirection = m_decision;
            }

            if (m_pendingId >= 0 && m_pendingFor == position)
            {
                m_pendingAge += deltaTime;
                if (m_pendingAge > DecisionTimeoutSec)
                {
                    // Stop waiting; the snake keeps its heading and the late reply is ignored.
                    m_decidedFor = position;
                    m_decision = null;
                    m_pendingId = -1;
                    Status = "LAYA: TIMED OUT";
                    return true;
                }
                return !m_engine.IsStepDue(deltaTime);
            }

            return true;
        }

        private void AutoSelectFirstOption(InputState input, double deltaTime, double delay)
        {
            if (input.DirectionPressed.HasValue || input.ActionPressed)
            {
                m_playerNavigated = true;
            }

            m_screenTime += deltaTime;
            if (!m_playerNavigated && m_screenTime >= delay)
            {
                input.ActionPressed = true;
                m_screenTime = 0;
            }
        }

        private void OnStateChanged(GameState? from, GameState to)
        {
            m_screenTime = 0;
            m_playerNavigated = false;

            if (from == GameState.Playing && to == GameState.GameOver && m_link.IsConnected)
            {
                m_link.Send(AgentProtocol.GameOver(m_engine));
            }
        }

        private void ReadReplies()
        {
            while (m_link.TryReceive(out string line))
            {
                var reply = AgentProtocol.Parse(line);
                switch (reply.Type)
                {
                    case "ready":
                        m_ready = true;
                        Status = "LAYA: READY";
                        break;

                    case "decision" when reply.Id >= 0 && reply.Id == m_pendingId:
                        m_decidedFor = m_pendingFor;
                        m_decision = reply.Direction;
                        m_pendingId = -1;
                        if (reply.Direction.HasValue)
                        {
                            Status = $"LAYA: {reply.Direction.Value.ToString().ToUpperInvariant()} {reply.Confidence * 100:0}%";
                        }
                        break;

                    case "error" when reply.Id >= 0 && reply.Id == m_pendingId:
                        m_decidedFor = m_pendingFor;
                        m_decision = null;
                        m_pendingId = -1;
                        Status = "LAYA: ERROR";
                        break;
                }
            }
        }
    }
}
