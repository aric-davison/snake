using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using Snake.Core;
using Snake.Core.Agent;

internal class Program
{
    /// <summary>
    /// The main entry point for the application.
    /// This creates an instance of your game and calls it's Run() method
    /// </summary>
    /// <param name="args">Command-line arguments passed to the application.
    /// Pass --laya to have the Laya agent play; see agent/README.md.</param>
    private static void Main(string[] args)
    {
        using IAgentLink agent = StartLayaAgent(args);
        using var game = new SnakeGame(agent);
        game.Run();
    }

    /// <summary>
    /// With --laya, starts agent/laya_player.py to play the game. Arguments after --laya are
    /// passed on to it, except these two, which are read here:
    ///   --laya-python PATH   Python interpreter with laya installed (default: $LAYA_PYTHON, then python3)
    ///   --laya-agent PATH    path to laya_player.py (default: found by searching up from the executable)
    /// </summary>
    private static IAgentLink StartLayaAgent(string[] args)
    {
        int start = Array.IndexOf(args, "--laya");
        if (start < 0) return null;

        string python = Environment.GetEnvironmentVariable("LAYA_PYTHON") ?? "python3";
        string script = null;
        var forwarded = new List<string>();
        for (int i = start + 1; i < args.Length; i++)
        {
            if (args[i] == "--laya-python" && i + 1 < args.Length) python = args[++i];
            else if (args[i] == "--laya-agent" && i + 1 < args.Length) script = args[++i];
            else forwarded.Add(args[i]);
        }

        script ??= FindAgentScript();
        if (script == null)
        {
            Console.Error.WriteLine("[snake] --laya: could not find agent/laya_player.py; pass --laya-agent PATH.");
            return null;
        }

        try
        {
            Console.Error.WriteLine($"[snake] Starting Laya agent: {python} {script}");
            return new LayaSidecar(python, script, forwarded);
        }
        catch (Exception e) when (e is Win32Exception or InvalidOperationException)
        {
            Console.Error.WriteLine($"[snake] Could not start the Laya agent with '{python}': {e.Message}");
            return null;
        }
    }

    private static string FindAgentScript()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
        {
            string candidate = Path.Combine(dir.FullName, "agent", "laya_player.py");
            if (File.Exists(candidate)) return candidate;
        }
        return null;
    }
}
