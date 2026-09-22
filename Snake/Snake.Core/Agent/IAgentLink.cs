using System;

namespace Snake.Core.Agent
{
    /// <summary>
    /// A line-based connection to an external agent process. Each message is one JSON object
    /// per line. Implementations live in the platform projects (e.g. a child process on desktop),
    /// keeping Snake.Core free of process and I/O concerns.
    /// </summary>
    public interface IAgentLink : IDisposable
    {
        /// <summary>
        /// True while the agent can still receive messages.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Sends one message to the agent. Called from the game loop, so it must return quickly.
        /// </summary>
        void Send(string jsonLine);

        /// <summary>
        /// Returns the next message from the agent, if one has arrived.
        /// </summary>
        bool TryReceive(out string jsonLine);
    }
}
