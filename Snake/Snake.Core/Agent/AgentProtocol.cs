using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Snake.Core.Configuration;
using Snake.Core.Engine;

namespace Snake.Core.Agent
{
    /// <summary>
    /// JSON messages exchanged with the agent (see agent/README.md for the full protocol).
    /// Game to agent: "observe" (one board position) and "game_over".
    /// Agent to game: "ready", "decision" and "error".
    /// </summary>
    public static class AgentProtocol
    {
        public static string Observe(int id, GameEngine engine, GameConfig config)
        {
            var apples = new List<object>
            {
                new { x = engine.Food.Position.X, y = engine.Food.Position.Y, kind = "apple", value = engine.AppleValue }
            };
            if (engine.BonusApple.HasValue)
            {
                var p = engine.BonusApple.Value;
                apples.Add(new { x = p.X, y = p.Y, kind = "bonus", value = engine.AppleValue });
            }
            if (engine.GoldenApple.Active)
            {
                var p = engine.GoldenApple.Position;
                apples.Add(new
                {
                    x = p.X, y = p.Y, kind = "golden", value = GameEngine.GoldenAppleReward,
                    time_left = engine.GoldenApple.TimeRemaining
                });
            }

            return JsonSerializer.Serialize(new
            {
                type = "observe",
                id,
                run = engine.RunNumber,
                step = engine.StepCount,
                width = config.GridWidth,
                height = config.GridHeight,
                snake = engine.Snake.AllSegments.Select(p => new[] { p.X, p.Y }),
                direction = engine.Snake.CurrentDirection.ToString(),
                growing = engine.Snake.IsGrowing,
                apples,
                apples_collected = engine.SessionApples,
                step_interval = config.UpdateInterval
            });
        }

        public static string GameOver(GameEngine engine)
        {
            return JsonSerializer.Serialize(new
            {
                type = "game_over",
                run = engine.RunNumber,
                steps = engine.StepCount,
                apples = engine.SessionApples,
                length = engine.Snake.AllSegments.Count()
            });
        }

        /// <summary>
        /// A parsed message from the agent. Unknown or malformed lines parse as type "invalid".
        /// </summary>
        public readonly struct Reply
        {
            public string Type { get; init; }
            public int Id { get; init; }
            public Direction? Direction { get; init; }
            public float Confidence { get; init; }
            public string Message { get; init; }
        }

        public static Reply Parse(string line)
        {
            try
            {
                using var doc = JsonDocument.Parse(line);
                var root = doc.RootElement;
                string type = root.TryGetProperty("type", out var t) ? t.GetString() : null;

                Direction? direction = null;
                if (root.TryGetProperty("direction", out var d) &&
                    Enum.TryParse(d.GetString(), ignoreCase: true, out Direction parsed))
                {
                    direction = parsed;
                }

                return new Reply
                {
                    Type = type ?? "invalid",
                    Id = root.TryGetProperty("id", out var id) && id.ValueKind == JsonValueKind.Number ? id.GetInt32() : -1,
                    Direction = direction,
                    Confidence = root.TryGetProperty("confidence", out var c) && c.ValueKind == JsonValueKind.Number ? c.GetSingle() : 0f,
                    Message = root.TryGetProperty("message", out var m) ? m.ToString() : null
                };
            }
            catch (JsonException)
            {
                return new Reply { Type = "invalid", Id = -1, Message = line };
            }
        }
    }
}
