using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
namespace GameProject2.SaveSystem
{
    [Serializable]
    public class SaveData
    {
        public int CoinCount { get; set; }
        public int CurrentHealth { get; set; }
        public int MaxHealth { get; set; }
        public string CheckpointRoom { get; set; } = "StartingRoom";
        public string CheckpointName { get; set; } = "InitialSpawn";

        public string CurrentRoom { get; set; } = "StartingRoom";
        public float MusicVolume { get; set; }
        public float SfxVolume { get; set; }
        public Dictionary<string, List<string>> DestroyedVases { get; set; } = new Dictionary<string, List<string>>();
        public Dictionary<string, List<string>> OpenedChests { get; set; } = new Dictionary<string, List<string>>();
        public Dictionary<string, List<string>> DestroyedCrates { get; set; } = new Dictionary<string, List<string>>();

        // Ability unlocks
        public bool HasAttack2 { get; set; } = false;
        public bool HasDash { get; set; } = false;

        // Potion inventory
        public int RedPotionCount { get; set; } = 1;
        public int RedMiniPotionCount { get; set; } = 2;

        // Activated totems
        public List<string> ActivatedTotems { get; set; } = new List<string>();

        // Completed gauntlets (room names)
        public List<string> CompletedGauntlets { get; set; } = new List<string>();

        // Boss defeated state
        public bool OrcKingDefeated { get; set; } = false;

        public static void Save(SaveData data, string filename = "savegame.json")
        {
            try
            {
                string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(filename, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Save failed: {ex.Message}");
            }
        }

        public static SaveData Load(string filename = "savegame.json")
        {
            try
            {
                if (File.Exists(filename))
                {
                    string json = File.ReadAllText(filename);
                    return JsonSerializer.Deserialize<SaveData>(json);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Load failed: {ex.Message}");
            }
            return null;
        }

        public static bool SaveExists(string filename = "savegame.json")
        {
            return File.Exists(filename);
        }
    }
}