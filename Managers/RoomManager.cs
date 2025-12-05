using GameProject2.Tilemaps;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using System.Collections.Generic;
using System.IO;

namespace GameProject2.Managers
{
    public static class RoomManager
    {
        public static Tilemap CurrentTilemap { get; private set; }
        public static string CurrentRoom { get; private set; } = "StartingRoom";
        public static string LastRoom { get; private set; }
        public static List<Rectangle> CollisionBoxes { get; private set; } = new List<Rectangle>();

        public const float TilemapScale = 4f;

        // Load a room's tilemap and extract collision
        public static void LoadRoom(string roomName, ContentManager content)
        {
            LastRoom = CurrentRoom;
            CurrentRoom = roomName;

            System.Diagnostics.Debug.WriteLine($"RoomManager: ContentManager.RootDirectory = '{content.RootDirectory}'");
            string tmxPath = Path.Combine(content.RootDirectory, "Rooms", $"{roomName}.tmx");
            System.Diagnostics.Debug.WriteLine($"RoomManager: Loading room '{roomName}' from path: {tmxPath}");
            System.Diagnostics.Debug.WriteLine($"RoomManager: File exists: {File.Exists(tmxPath)}");

            CurrentTilemap = TmxLoader.Load(tmxPath, content);

            System.Diagnostics.Debug.WriteLine($"RoomManager: Tilemap loaded - Layers: {CurrentTilemap?.Layers?.Count ?? 0}, ObjectLayers: {CurrentTilemap?.ObjectLayers?.Count ?? 0}");
            if (CurrentTilemap?.Layers != null)
            {
                foreach (var layer in CurrentTilemap.Layers)
                {
                    System.Diagnostics.Debug.WriteLine($"  Layer: '{layer.Name}'");
                }
            }

            ExtractCollisionBoxes();
            System.Diagnostics.Debug.WriteLine($"RoomManager: Extracted {CollisionBoxes.Count} collision boxes");
        }

        private static void ExtractCollisionBoxes()
        {
            CollisionBoxes.Clear();

            foreach (var objectLayer in CurrentTilemap.ObjectLayers)
            {
                if (objectLayer.Name == "Collision")
                {
                    foreach (var obj in objectLayer.Objects)
                    {
                        CollisionBoxes.Add(new Rectangle(
                            (int)(obj.X * TilemapScale),
                            (int)(obj.Y * TilemapScale),
                            (int)(obj.Width * TilemapScale),
                            (int)(obj.Height * TilemapScale)
                        ));
                    }
                }
            }
        }

        // Find spawn point position in current room
        public static Vector2? FindSpawnPoint(string spawnPointName)
        {
            if (string.IsNullOrEmpty(spawnPointName))
                return null;

            foreach (var objectLayer in CurrentTilemap.ObjectLayers)
            {
                foreach (var obj in objectLayer.Objects)
                {
                    if ((obj.Class == "SpawnPoint" || obj.Class == "Checkpoint")
                        && obj.Name == spawnPointName)
                    {
                        return new Vector2(obj.X * TilemapScale, obj.Y * TilemapScale);
                    }
                }
            }
            return null;
        }

        // Get room center (fallback spawn)
        public static Vector2 GetRoomCenter()
        {
            return new Vector2(
                (CurrentTilemap.Width * CurrentTilemap.TileWidth * TilemapScale) / 2f,
                (CurrentTilemap.Height * CurrentTilemap.TileHeight * TilemapScale) / 2f
            );
        }

        // Get tilemap dimensions in world units
        public static Vector2 GetTilemapSize()
        {
            return new Vector2(
                CurrentTilemap.Width * CurrentTilemap.TileWidth * TilemapScale,
                CurrentTilemap.Height * CurrentTilemap.TileHeight * TilemapScale
            );
        }

        // Find door object at player position (for room transitions)
        public static TiledObject FindDoorAtPosition(Rectangle playerHitbox)
        {
            foreach (var objectLayer in CurrentTilemap.ObjectLayers)
            {
                foreach (var obj in objectLayer.Objects)
                {
                    if (obj.Class == "Door")
                    {
                        Rectangle doorRect = new Rectangle(
                            (int)(obj.X * TilemapScale),
                            (int)(obj.Y * TilemapScale),
                            (int)(obj.Width * TilemapScale),
                            (int)(obj.Height * TilemapScale)
                        );

                        if (playerHitbox.Intersects(doorRect))
                            return obj;
                    }
                }
            }
            return null;
        }

        // Find trophy interaction zone
        public static TiledObject FindTrophyAtPosition(Rectangle playerHitbox)
        {
            foreach (var objectLayer in CurrentTilemap.ObjectLayers)
            {
                foreach (var obj in objectLayer.Objects)
                {
                    if (obj.Class == "Trophy")
                    {
                        Rectangle trophyRect = new Rectangle(
                            (int)(obj.X * TilemapScale),
                            (int)(obj.Y * TilemapScale),
                            (int)(obj.Width * TilemapScale),
                            (int)(obj.Height * TilemapScale)
                        );

                        if (playerHitbox.Intersects(trophyRect))
                            return obj;
                    }
                }
            }
            return null;
        }

        // Find checkpoint at player position
        public static TiledObject FindCheckpointAtPosition(Rectangle playerHitbox)
        {
            foreach (var objectLayer in CurrentTilemap.ObjectLayers)
            {
                foreach (var obj in objectLayer.Objects)
                {
                    if (obj.Class == "Checkpoint")
                    {
                        Rectangle checkpointRect = new Rectangle(
                            (int)(obj.X * TilemapScale),
                            (int)(obj.Y * TilemapScale),
                            (int)(obj.Width * TilemapScale),
                            (int)(obj.Height * TilemapScale)
                        );

                        if (playerHitbox.Intersects(checkpointRect))
                            return obj;
                    }
                }
            }
            return null;
        }

        // Find totem trigger at player position
        public static TiledObject FindTotemTriggerAtPosition(Rectangle playerHitbox)
        {
            foreach (var objectLayer in CurrentTilemap.ObjectLayers)
            {
                foreach (var obj in objectLayer.Objects)
                {
                    if (obj.Class == "TotemTrigger")
                    {
                        Rectangle triggerRect = new Rectangle(
                            (int)(obj.X * TilemapScale),
                            (int)(obj.Y * TilemapScale),
                            (int)(obj.Width * TilemapScale),
                            (int)(obj.Height * TilemapScale)
                        );

                        if (playerHitbox.Intersects(triggerRect))
                            return obj;
                    }
                }
            }
            return null;
        }
    }
}
