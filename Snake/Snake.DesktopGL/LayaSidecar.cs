using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Snake.Core.Agent;

/// <summary>
/// Runs the Laya agent (agent/laya_player.py) as a child process and exchanges JSON lines with
/// it over stdin/stdout. The agent's stderr is left attached to this console for its logs.
/// </summary>
internal sealed class LayaSidecar : IAgentLink
{
    private readonly Process m_process;
    private readonly ConcurrentQueue<string> m_inbox = new ConcurrentQueue<string>();
    private volatile bool m_connected = true;

    public LayaSidecar(string python, string script, IEnumerable<string> args)
    {
        var startInfo = new ProcessStartInfo(python)
        {
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
        };
        startInfo.ArgumentList.Add("-u");
        startInfo.ArgumentList.Add(script);
        foreach (string arg in args)
        {
            startInfo.ArgumentList.Add(arg);
        }

        m_process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Could not start '{python}'.");
        m_process.StandardInput.AutoFlush = true;

        var reader = new Thread(ReadLoop) { IsBackground = true, Name = "laya-stdout" };
        reader.Start();
    }

    public bool IsConnected => m_connected;

    public void Send(string jsonLine)
    {
        if (!m_connected) return;
        try
        {
            m_process.StandardInput.WriteLine(jsonLine);
        }
        catch (IOException)
        {
            m_connected = false;
        }
    }

    public bool TryReceive(out string jsonLine)
    {
        return m_inbox.TryDequeue(out jsonLine);
    }

    private void ReadLoop()
    {
        try
        {
            string line;
            while ((line = m_process.StandardOutput.ReadLine()) != null)
            {
                m_inbox.Enqueue(line);
            }
        }
        catch (IOException)
        {
        }
        finally
        {
            m_connected = false;
        }
    }

    public void Dispose()
    {
        // Closing stdin tells the agent to print its session summary and exit.
        try
        {
            m_process.StandardInput.Close();
        }
        catch (IOException)
        {
        }

        if (!m_process.WaitForExit(3000))
        {
            try
            {
                m_process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException)
            {
            }
        }
        m_process.Dispose();
    }
}
