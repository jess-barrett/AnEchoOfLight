using GameProject2.SaveSystem;
using Microsoft.Xna.Framework;

namespace GameProject2.Managers
{
    public static class CheckpointManager
    {
        public static string LastCheckpointRoom { get; private set; } = "StartingRoom";
        public static string LastCheckpointName { get; private set; } = "CP-StartingRoom";

        // Message display state
        public static bool ShowCheckpointMessage { get; private set; }
        private static float _messageTimer;
        private const float MessageDuration = 2f;

        public static void Initialize(string room = "StartingRoom", string checkpoint = "CP-StartingRoom")
        {
            LastCheckpointRoom = room;
            LastCheckpointName = checkpoint;
            ShowCheckpointMessage = false;
            _messageTimer = 0f;
        }

        public static void SetCheckpoint(string room, string checkpointName)
        {
            LastCheckpointRoom = room;
            LastCheckpointName = checkpointName;
            ShowCheckpointMessage = true;
            _messageTimer = MessageDuration;

            AudioManager.PlayCheckpointSound();

            System.Diagnostics.Debug.WriteLine($"Checkpoint activated: {checkpointName} in {room}");
        }

        // Silent update for passive checkpoint zones (no message shown)
        public static void SetCheckpointSilent(string room, string checkpointName)
        {
            LastCheckpointRoom = room;
            LastCheckpointName = checkpointName;
        }

        public static void Update(float deltaTime)
        {
            if (ShowCheckpointMessage)
            {
                _messageTimer -= deltaTime;
                if (_messageTimer <= 0)
                {
                    ShowCheckpointMessage = false;
                }
            }
        }

        // Load checkpoint data from save
        public static void LoadFromSave(SaveData data)
        {
            if (data != null)
            {
                LastCheckpointRoom = data.CheckpointRoom ?? "StartingRoom";
                LastCheckpointName = data.CheckpointName ?? "InitialSpawn";
            }
        }

        // Get spawn position for respawn (uses RoomManager)
        public static Vector2 GetRespawnPosition()
        {
            var spawnPos = RoomManager.FindSpawnPoint(LastCheckpointName);
            return spawnPos ?? RoomManager.GetRoomCenter();
        }
    }
}
