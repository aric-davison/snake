using System;
using System.IO;
using System.Text.Json;

namespace Snake.Core.Persistence
{
    /// <summary>
    /// Encapsulates JSON serialization of PlayerData to and from a local save file.
    /// Falls back to defaults when the file is missing or malformed.
    /// </summary>
    public class SaveManager
    {
        private readonly string m_filePath;

        public SaveManager()
        {
            var folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SnakeGame");
            Directory.CreateDirectory(folder);
            m_filePath = Path.Combine(folder, "save.json");
        }

        public PlayerData Load()
        {
            if (!File.Exists(m_filePath))
            {
                return CreateDefault();
            }

            try
            {
                var json = File.ReadAllText(m_filePath);
                var data = JsonSerializer.Deserialize<PlayerData>(json);
                return data ?? CreateDefault();
            }
            catch (Exception)
            {
                return CreateDefault();
            }
        }

        public void Save(PlayerData data)
        {
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(m_filePath, json);
        }

        private PlayerData CreateDefault()
        {
            return new PlayerData();
        }
    }
}
