using GameProject2.Enemies;
using GameProject2.Tilemaps;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;

namespace GameProject2.Managers
{
    public enum GauntletState
    {
        Inactive,       // Player hasn't triggered the gauntlet
        Active,         // Gauntlet is running, totems spawning enemies
        Completed       // All totems destroyed, door opens
    }

    public static class GauntletManager
    {
        // Gauntlet state
        public static GauntletState State { get; private set; } = GauntletState.Inactive;
        public static string CurrentGauntletRoom { get; private set; } = null;

        // Entities managed by gauntlet
        private static List<EnemySpawnerTotem> _spawnerTotems = new List<EnemySpawnerTotem>();
        private static List<WoodenDoubleDoor> _doors = new List<WoodenDoubleDoor>();

        // Collision rectangles for doors (to be ignored when open)
        private static Dictionary<string, Rectangle> _doorColliders = new Dictionary<string, Rectangle>();

        // Starter trigger rectangle
        private static Rectangle? _starterTrigger = null;
        private static bool _triggerActivated = false;

        // Track totems hit this attack to prevent multi-hit
        private static HashSet<EnemySpawnerTotem> _totemsHitThisAttack = new HashSet<EnemySpawnerTotem>();

        // Reference to collision boxes for spawn validation
        private static List<Rectangle> _collisionBoxes = new List<Rectangle>();

        // Door open delay
        private static float _doorOpenDelayTimer = 0f;
        private static float _doorOpenDelay = 3f;
        private static bool _waitingToOpenDoors = false;

        // Texture cache
        private static Texture2D _totemTexture;
        private static Texture2D _doorTexture;

        public static IReadOnlyList<EnemySpawnerTotem> SpawnerTotems => _spawnerTotems;
        public static IReadOnlyList<WoodenDoubleDoor> Doors => _doors;

        public static void LoadContent(Texture2D totemTexture, Texture2D doorTexture)
        {
            _totemTexture = totemTexture;
            _doorTexture = doorTexture;
        }

        public static void ClearAll()
        {
            _spawnerTotems.Clear();
            _doors.Clear();
            _doorColliders.Clear();
            _starterTrigger = null;
            _triggerActivated = false;
            _totemsHitThisAttack.Clear();
            _collisionBoxes.Clear();
            _doorOpenDelayTimer = 0f;
            _waitingToOpenDoors = false;
            State = GauntletState.Inactive;
            CurrentGauntletRoom = null;
        }

        public static void SpawnFromTilemap(
            Tilemap tilemap,
            string roomName,
            float tilemapScale,
            HashSet<string> completedGauntlets,
            bool orcKingDefeated = false)
        {
            CurrentGauntletRoom = roomName;
            bool gauntletAlreadyCompleted = completedGauntlets.Contains(roomName);
            bool doorsStartOpen = gauntletAlreadyCompleted || orcKingDefeated;

            foreach (var objectLayer in tilemap.ObjectLayers)
            {
                foreach (var obj in objectLayer.Objects)
                {
                    Vector2 pos = new Vector2(obj.X * tilemapScale, obj.Y * tilemapScale);

                    // Check for gauntlet starter trigger
                    if (obj.Name == "GauntletStarterCollider" || obj.Class == "GauntletStarter")
                    {
                        _starterTrigger = new Rectangle(
                            (int)(obj.X * tilemapScale),
                            (int)(obj.Y * tilemapScale),
                            (int)(obj.Width * tilemapScale),
                            (int)(obj.Height * tilemapScale)
                        );

                        if (gauntletAlreadyCompleted)
                        {
                            _triggerActivated = true;
                            State = GauntletState.Completed;
                        }
                    }

                    // Check for spawner totems (Class=Totem, Name=enemy type like "Skull", "BlueSlime")
                    // Only spawn if the totem name is an enemy type (not ability like "Dash")
                    if (obj.Class == "Totem" && IsEnemyType(obj.Name))
                    {
                        string totemId = $"{roomName}_{obj.X}_{obj.Y}";
                        var totem = new EnemySpawnerTotem(_totemTexture, pos, tilemapScale, totemId, obj.Name);

                        if (gauntletAlreadyCompleted)
                        {
                            // Show as destroyed if gauntlet already completed
                            totem.TakeDamage(totem.MaxHealth);
                        }

                        _spawnerTotems.Add(totem);
                    }

                    // Check for wooden double doors
                    if (obj.Class == "WoodenDoubleDoor")
                    {
                        string doorId = $"{roomName}_{obj.X}_{obj.Y}";

                        // Check for StartOpen property (door starts open then closes after delay)
                        bool startOpen = false;
                        if (obj.Properties.TryGetValue("StartOpen", out string startOpenStr))
                        {
                            bool.TryParse(startOpenStr, out startOpen);
                        }

                        // Check for CloseDelay property (custom delay before door closes)
                        float closeDelay = -1f;
                        if (obj.Properties.TryGetValue("CloseDelay", out string closeDelayStr))
                        {
                            float.TryParse(closeDelayStr, out closeDelay);
                        }

                        var door = new WoodenDoubleDoor(_doorTexture, pos, tilemapScale, doorId, startOpen, closeDelay);

                        if (doorsStartOpen)
                        {
                            door.SetOpen();
                        }

                        _doors.Add(door);
                    }

                    // Check for door colliders on Collision layer
                    if (objectLayer.Name == "Collision" && obj.Name == "WoodenDoubleDoorCollider")
                    {
                        string colliderId = $"{roomName}_{obj.X}_{obj.Y}";
                        Rectangle colliderRect = new Rectangle(
                            (int)(obj.X * tilemapScale),
                            (int)(obj.Y * tilemapScale),
                            (int)(obj.Width * tilemapScale),
                            (int)(obj.Height * tilemapScale)
                        );
                        _doorColliders[colliderId] = colliderRect;
                        System.Diagnostics.Debug.WriteLine($"Added door collider: {colliderRect}");
                    }
                }
            }

            // If gauntlet was already completed, set state
            if (gauntletAlreadyCompleted)
            {
                State = GauntletState.Completed;
            }
        }

        private static bool IsEnemyType(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;

            // List of valid enemy types
            return name == "Skull" || name == "BlueSlime";
            // Add more enemy types here as they're created
        }

        public static void Update(GameTime gameTime, Player player, List<Rectangle> collisionBoxes)
        {
            // Store collision boxes reference for spawn validation
            _collisionBoxes = collisionBoxes;

            // Check if player triggers the gauntlet
            if (State == GauntletState.Inactive && _starterTrigger.HasValue && !_triggerActivated)
            {
                if (player.Hitbox.Intersects(_starterTrigger.Value))
                {
                    StartGauntlet();
                }
            }

            // Update spawner totems
            bool anySpawnedThisFrame = false;
            foreach (var totem in _spawnerTotems)
            {
                totem.Update(gameTime);

                // Handle enemy spawning
                if (totem.ShouldSpawnEnemyNow())
                {
                    SpawnEnemyFromTotem(totem, collisionBoxes, ref anySpawnedThisFrame);
                }
            }

            // Update doors
            foreach (var door in _doors)
            {
                door.Update(gameTime);
            }

            // Check if all totems are destroyed
            if (State == GauntletState.Active)
            {
                bool allDestroyed = _spawnerTotems.All(t => t.IsDestroyed);
                if (allDestroyed)
                {
                    CompleteGauntlet();
                }
            }

            // Handle door open delay
            if (_waitingToOpenDoors)
            {
                _doorOpenDelayTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (_doorOpenDelayTimer <= 0)
                {
                    _waitingToOpenDoors = false;
                    OpenDoors();
                }
            }
        }

        private static void StartGauntlet()
        {
            State = GauntletState.Active;
            _triggerActivated = true;

            // Activate all spawner totems
            foreach (var totem in _spawnerTotems)
            {
                totem.Activate();
            }

            // Play activate sound once for all totems
            if (_spawnerTotems.Count > 0)
            {
                AudioManager.PlayTotemActivateSound(0.5f);
            }

            // Play gauntlet-specific music
            if (CurrentGauntletRoom == "DashGauntlet")
            {
                AudioManager.PlayDashGauntletMusic();
            }

            System.Diagnostics.Debug.WriteLine("Gauntlet started!");
        }

        private static void CompleteGauntlet()
        {
            State = GauntletState.Completed;

            // Stop the gauntlet music
            AudioManager.StopMusic();

            // Play gauntlet defeated sound
            AudioManager.PlayGauntletDefeatedSound(0.6f);

            // Start door open delay
            _doorOpenDelayTimer = _doorOpenDelay;
            _waitingToOpenDoors = true;

            System.Diagnostics.Debug.WriteLine("Gauntlet completed! Doors will open in 3 seconds...");
        }

        private static void OpenDoors()
        {
            // Open all doors with sound
            foreach (var door in _doors)
            {
                door.Open();
            }

            // Play door open sound once (not per door)
            if (_doors.Count > 0)
            {
                AudioManager.PlayWoodDoorOpenSound(0.5f);
            }

            System.Diagnostics.Debug.WriteLine("Doors opened!");
        }

        private static void SpawnEnemyFromTotem(EnemySpawnerTotem totem, List<Rectangle> collisionBoxes, ref bool playedSoundThisFrame)
        {
            // Get spawn position with wall collision check
            Vector2? spawnPos = totem.GetSpawnPosition(collisionBoxes);

            if (!spawnPos.HasValue)
            {
                // No valid spawn position found, skip this spawn
                System.Diagnostics.Debug.WriteLine($"Could not find valid spawn position for {totem.EnemyType}");
                totem.OnEnemySpawned(); // Still mark as spawned to continue the sequence
                return;
            }

            IEnemy enemy = null;

            switch (totem.EnemyType)
            {
                case "Skull":
                    enemy = EntityManager.SpawnSkullPublic(spawnPos.Value);
                    break;
                case "BlueSlime":
                    enemy = EntityManager.SpawnBlueSlimePublic(spawnPos.Value);
                    break;
            }

            if (enemy != null)
            {
                totem.RegisterSpawnedEnemy(enemy);
                // Only play spawn sound once per frame, even if multiple totems spawn
                if (!playedSoundThisFrame)
                {
                    AudioManager.PlayTotemSpawnEnemySound(0.4f);
                    playedSoundThisFrame = true;
                }
            }

            totem.OnEnemySpawned();

            System.Diagnostics.Debug.WriteLine($"Spawned {totem.EnemyType} from totem at {spawnPos.Value}");
        }

        // Get collision boxes, removing door colliders when boss is defeated
        public static List<Rectangle> FilterCollisionBoxes(List<Rectangle> originalBoxes, bool bossDefeated = false)
        {
            // If no door colliders registered or boss not defeated, return original boxes
            if (_doorColliders.Count == 0 || !bossDefeated)
                return originalBoxes;

            // Boss is defeated - remove the door colliders so player can exit
            var result = new List<Rectangle>(originalBoxes);

            foreach (var colliderRect in _doorColliders.Values)
            {
                result.RemoveAll(r => r == colliderRect ||
                    (r.X == colliderRect.X && r.Y == colliderRect.Y &&
                     r.Width == colliderRect.Width && r.Height == colliderRect.Height));
            }

            return result;
        }

        /// <summary>
        /// Handle player attacks on totems. Call this once per attack, not per frame.
        /// </summary>
        public static void HandlePlayerAttackOnTotems(Rectangle attackHitbox, int damage)
        {
            if (State != GauntletState.Active) return;

            foreach (var totem in _spawnerTotems)
            {
                // Skip if already hit this attack or destroyed
                if (_totemsHitThisAttack.Contains(totem) || totem.IsDestroyed)
                    continue;

                if (attackHitbox.Intersects(totem.Hitbox))
                {
                    totem.TakeDamage(damage);
                    _totemsHitThisAttack.Add(totem);
                    AudioManager.PlayAttackLandingSound(0.15f);
                }
            }
        }

        /// <summary>
        /// Clear the hit tracking set - call this when a new attack starts
        /// </summary>
        public static void ClearAttackHitTracking()
        {
            _totemsHitThisAttack.Clear();
        }

        public static bool IsGauntletRoom()
        {
            return _spawnerTotems.Count > 0;
        }

        public static bool IsGauntletCompleted()
        {
            return State == GauntletState.Completed;
        }
    }
}
