using GameProject2.Enemies;
using GameProject2.Graphics3D;
using GameProject2.Tilemaps;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GameProject2.Managers
{
    public static class EntityManager
    {
        // Entity collections - now uses IEnemy interface for all enemies
        private static List<IEnemy> _enemies = new List<IEnemy>();
        private static List<Vase> _vases = new List<Vase>();
        private static List<Coin> _coins = new List<Coin>();
        private static List<Chest> _chests = new List<Chest>();
        private static List<Potion> _potions = new List<Potion>();
        private static List<Button> _buttons = new List<Button>();
        private static List<PillarTorch> _pillarTorches = new List<PillarTorch>();
        private static List<WallTorch> _wallTorches = new List<WallTorch>();
        private static List<Totem> _totems = new List<Totem>();
        private static List<Crate> _crates = new List<Crate>();
        private static Trophy _trophy;
        private static OrcBoss _orcBoss;

        // Public read-only accessors
        public static IReadOnlyList<IEnemy> Enemies => _enemies;
        public static IReadOnlyList<Vase> Vases => _vases;
        public static IReadOnlyList<Coin> Coins => _coins;
        public static IReadOnlyList<Chest> Chests => _chests;
        public static IReadOnlyList<Potion> Potions => _potions;
        public static IReadOnlyList<Button> Buttons => _buttons;
        public static IReadOnlyList<PillarTorch> PillarTorches => _pillarTorches;
        public static IReadOnlyList<WallTorch> WallTorches => _wallTorches;
        public static IReadOnlyList<Totem> Totems => _totems;
        public static IReadOnlyList<Crate> Crates => _crates;
        public static Trophy Trophy => _trophy;
        public static OrcBoss OrcBoss => _orcBoss;

        // Texture cache (loaded once, reused)
        private static Texture2D _vaseTexture;
        private static Texture2D _coinTexture;
        private static Texture2D _buttonTexture;
        private static Texture2D _potionTexture;
        private static Texture2D _chestTexture;
        private static Texture2D _torchTilesetTexture;
        private static Texture2D _skullSheet;
        private static Texture2D _totemTexture;
        private static Texture2D _blueSlimeSheet;
        private static Texture2D _woodenDoubleDoorTexture;
        private static Texture2D _wallTorchTexture;

        // Orc boss textures
        private static Texture2D _orcIdleTexture;
        private static Texture2D _orcWalkTexture;
        private static Texture2D _orcRunTexture;
        private static Texture2D _orcAttackTexture;
        private static Texture2D _orcWalkAttackTexture;
        private static Texture2D _orcRunAttackTexture;
        private static Texture2D _orcHurtTexture;
        private static Texture2D _orcDeathTexture;
        private static Texture2D _cratesTexture;

        // Load all entity textures
        public static void LoadContent(ContentManager content)
        {
            _vaseTexture = content.Load<Texture2D>("Interactables/Vase");
            _coinTexture = content.Load<Texture2D>("Interactables/Coin");
            _buttonTexture = content.Load<Texture2D>("Interactables/Button");
            _potionTexture = content.Load<Texture2D>("Interactables/Set 2.4");
            _chestTexture = content.Load<Texture2D>("Interactables/Chest");
            _torchTilesetTexture = content.Load<Texture2D>("Tilemaps/Set 4.8");
            _skullSheet = content.Load<Texture2D>("skull");
            _totemTexture = content.Load<Texture2D>("Interactables/Totems");
            _blueSlimeSheet = content.Load<Texture2D>("Enemies/Blue Slime");
            _woodenDoubleDoorTexture = content.Load<Texture2D>("Interactables/WoodenDoubleDoor");
            _wallTorchTexture = content.Load<Texture2D>("Torches");

            // Load Orc boss textures
            _orcIdleTexture = content.Load<Texture2D>("Orc/orc_idle");
            _orcWalkTexture = content.Load<Texture2D>("Orc/orc_walk");
            _orcRunTexture = content.Load<Texture2D>("Orc/orc_run");
            _orcAttackTexture = content.Load<Texture2D>("Orc/orc_attack");
            _orcWalkAttackTexture = content.Load<Texture2D>("Orc/orc_walk_attack");
            _orcRunAttackTexture = content.Load<Texture2D>("Orc/orc_run_attack");
            _orcHurtTexture = content.Load<Texture2D>("Orc/orc_hurt");
            _orcDeathTexture = content.Load<Texture2D>("Orc/orc_death");
            _cratesTexture = content.Load<Texture2D>("Interactables/Crates");

            // Initialize GauntletManager with textures
            GauntletManager.LoadContent(_totemTexture, _woodenDoubleDoorTexture);
        }

        // Clear all entities (called before loading new room)
        public static void ClearAll()
        {
            _enemies.Clear();
            _vases.Clear();
            _coins.Clear();
            _chests.Clear();
            _potions.Clear();
            _buttons.Clear();
            _pillarTorches.Clear();
            _wallTorches.Clear();
            _totems.Clear();
            _crates.Clear();
            _trophy = null;
            _orcBoss = null;
        }

        // Unified spawning from tilemap (eliminates duplication between Activate and LoadRoom)
        public static void SpawnFromTilemap(
            Tilemap tilemap,
            string roomName,
            float tilemapScale,
            Dictionary<string, HashSet<string>> destroyedVases,
            Dictionary<string, HashSet<string>> openedChests,
            Dictionary<string, HashSet<string>> destroyedCrates,
            HashSet<string> activatedTotems,
            string lastCheckpointName,
            string lastCheckpointRoom,
            GraphicsDevice graphicsDevice,
            bool orcKingDefeated = false)
        {
            foreach (var objectLayer in tilemap.ObjectLayers)
            {
                if (objectLayer.Name != "Objects") continue;

                foreach (var obj in objectLayer.Objects)
                {
                    Vector2 pos = new Vector2(obj.X * tilemapScale, obj.Y * tilemapScale);

                    switch (obj.Class)
                    {
                        case "Vase":
                            SpawnVase(pos, obj, roomName, destroyedVases, orcKingDefeated);
                            break;
                        case "Skull":
                            SpawnSkull(pos);
                            break;
                        case "BlueSlime":
                            SpawnBlueSlime(pos);
                            break;
                        case "Enemy":
                            SpawnEnemy(pos, obj.Name);
                            break;
                        case "Button":
                            SpawnButton(pos, obj, roomName, lastCheckpointName, lastCheckpointRoom);
                            break;
                        case "PillarTorch":
                            SpawnPillarTorch(pos, tilemapScale);
                            break;
                        case "Potion":
                            SpawnPotion(pos, obj);
                            break;
                        case "Chest":
                            SpawnChest(pos, obj, roomName, openedChests);
                            break;
                        case "Trophy":
                            SpawnTrophy(pos, graphicsDevice);
                            break;
                        case "Totem":
                            // Only spawn ability totems here (Dash, Attack2, etc.)
                            // Enemy spawner totems (Skull, BlueSlime) are handled by GauntletManager
                            if (!IsEnemySpawnerTotem(obj.Name))
                            {
                                SpawnTotem(pos, obj, roomName, activatedTotems, tilemapScale);
                            }
                            break;
                        case "WallTorch":
                            SpawnWallTorch(pos, obj, tilemapScale);
                            break;
                        case "OrcBoss":
                            SpawnOrcBoss(pos, obj, orcKingDefeated);
                            break;
                        case "Crate":
                            SpawnCrate(pos, obj, roomName, destroyedCrates);
                            break;
                    }
                }
            }
        }

        // Check if a totem name is an enemy spawner type (handled by GauntletManager)
        private static bool IsEnemySpawnerTotem(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            return name == "Skull" || name == "BlueSlime";
            // Add more enemy types here as they're created
        }

        // Individual spawn methods
        private static void SpawnVase(Vector2 pos, TiledObject obj, string roomName,
            Dictionary<string, HashSet<string>> destroyedVases, bool orcKingDefeated = false)
        {
            // Skip BossVase if the Orc King has been defeated
            if (obj.Name == "BossVase" && orcKingDefeated)
            {
                System.Diagnostics.Debug.WriteLine($"Skipped BossVase (boss defeated) in room {roomName}");
                return;
            }

            string vaseId = $"{obj.X}_{obj.Y}";

            if (!destroyedVases.ContainsKey(roomName))
                destroyedVases[roomName] = new HashSet<string>();

            if (!destroyedVases[roomName].Contains(vaseId))
            {
                var vase = new Vase(_vaseTexture, pos, 16, 8);
                vase.VaseId = vaseId;
                vase.VaseName = obj.Name;  // Store name for persistence check (e.g., "ApproachVase")

                // Read drop counts from Tiled properties
                if (obj.Properties.TryGetValue("Coin", out string coinStr))
                {
                    if (int.TryParse(coinStr, out int coinCount))
                        vase.CoinDropCount = coinCount;
                }

                if (obj.Properties.TryGetValue("PotionRed", out string redStr))
                {
                    if (int.TryParse(redStr, out int redCount))
                        vase.PotionRedDropCount = redCount;
                }

                if (obj.Properties.TryGetValue("PotionRedMini", out string redMiniStr))
                {
                    if (int.TryParse(redMiniStr, out int redMiniCount))
                        vase.PotionRedMiniDropCount = redMiniCount;
                }

                _vases.Add(vase);
                System.Diagnostics.Debug.WriteLine($"Spawned vase {vaseId} in room {roomName}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Skipped destroyed vase {vaseId} in room {roomName}");
            }
        }

        private static Skull SpawnSkull(Vector2 pos)
        {
            var skull = new Skull(_skullSheet, 10, 10, pos);
            _enemies.Add(skull);
            return skull;
        }

        private static BlueSlime SpawnBlueSlime(Vector2 pos)
        {
            var slime = new BlueSlime(_blueSlimeSheet, pos, 4f);
            _enemies.Add(slime);
            return slime;
        }

        // Public spawn methods for GauntletManager - return the enemy for tracking
        public static IEnemy SpawnSkullPublic(Vector2 pos) => SpawnSkull(pos);
        public static IEnemy SpawnBlueSlimePublic(Vector2 pos) => SpawnBlueSlime(pos);

        /// <summary>
        /// Spawns an enemy based on the name from Tiled.
        /// Use Class="Enemy" and Name="Skull", "BlueSlime", etc.
        /// </summary>
        private static void SpawnEnemy(Vector2 pos, string enemyName)
        {
            switch (enemyName)
            {
                case "Skull":
                    SpawnSkull(pos);
                    break;
                case "BlueSlime":
                    SpawnBlueSlime(pos);
                    break;
                default:
                    System.Diagnostics.Debug.WriteLine($"Unknown enemy type: {enemyName}");
                    break;
            }
        }

        private static void SpawnButton(Vector2 pos, TiledObject obj, string roomName,
            string lastCheckpointName, string lastCheckpointRoom)
        {
            var button = new Button(_buttonTexture, pos, 2f, obj.Name);

            if (obj.Name == lastCheckpointName && roomName == lastCheckpointRoom)
            {
                button.IsCurrentCheckpoint = true;
                button.Press();
            }

            _buttons.Add(button);
        }

        private static void SpawnPillarTorch(Vector2 pos, float scale)
        {
            _pillarTorches.Add(new PillarTorch(_torchTilesetTexture, pos, scale));
        }

        private static void SpawnWallTorch(Vector2 pos, TiledObject obj, float scale)
        {
            TorchMount mount = TorchMount.Front;

            switch (obj.Name)
            {
                case "TorchLeft":
                    mount = TorchMount.Left;
                    break;
                case "TorchFront":
                    mount = TorchMount.Front;
                    break;
                case "TorchRight":
                    mount = TorchMount.Right;
                    break;
            }

            TorchColor color = TorchColor.Red;
            if (obj.Properties.TryGetValue("Color", out string colorValue))
            {
                switch (colorValue)
                {
                    case "Red":
                        color = TorchColor.Red;
                        break;
                    case "Blue":
                        color = TorchColor.Blue;
                        break;
                    case "Orange":
                        color = TorchColor.Orange;
                        break;
                    case "Green":
                        color = TorchColor.Green;
                        break;
                }
            }

            _wallTorches.Add(new WallTorch(_wallTorchTexture, pos, scale, mount, color));
        }

        private static void SpawnPotion(Vector2 pos, TiledObject obj)
        {
            string potionTypeStr = obj.Name ?? "RedMini";

            if (Enum.TryParse(potionTypeStr, out PotionType potionType))
            {
                _potions.Add(new Potion(_potionTexture, pos, potionType, 3f, true));
            }
        }

        private static void SpawnChest(Vector2 pos, TiledObject obj, string roomName,
            Dictionary<string, HashSet<string>> openedChests)
        {
            string chestId = $"{obj.X}_{obj.Y}";

            System.Diagnostics.Debug.WriteLine($"Loading chest at {pos}, Properties count: {obj.Properties.Count}");

            var items = new List<string>();

            // Check for PotionRed (int count)
            if (obj.Properties.TryGetValue("PotionRed", out string redPotionStr))
            {
                if (int.TryParse(redPotionStr, out int redCount))
                {
                    for (int i = 0; i < redCount; i++)
                    {
                        items.Add("Potion.Red");
                    }
                    System.Diagnostics.Debug.WriteLine($"  Added {redCount} Red potions");
                }
            }

            // Check for PotionRedMini (int count)
            if (obj.Properties.TryGetValue("PotionRedMini", out string redMiniStr))
            {
                if (int.TryParse(redMiniStr, out int redMiniCount))
                {
                    for (int i = 0; i < redMiniCount; i++)
                    {
                        items.Add("Potion.RedMini");
                    }
                    System.Diagnostics.Debug.WriteLine($"  Added {redMiniCount} RedMini potions");
                }
            }

            // Check for Coin (int count)
            if (obj.Properties.TryGetValue("Coin", out string coinStr))
            {
                if (int.TryParse(coinStr, out int coinCount))
                {
                    for (int i = 0; i < coinCount; i++)
                    {
                        items.Add("Coin");
                    }
                    System.Diagnostics.Debug.WriteLine($"  Added {coinCount} coins");
                }
            }

            System.Diagnostics.Debug.WriteLine($"Chest created with {items.Count} items");

            var chest = new Chest(_chestTexture, pos, 4f, items, chestId);

            if (!openedChests.ContainsKey(roomName))
                openedChests[roomName] = new HashSet<string>();

            if (openedChests[roomName].Contains(chestId))
                chest.SetOpened();

            _chests.Add(chest);
        }

        private static void SpawnTrophy(Vector2 pos, GraphicsDevice graphicsDevice)
        {
            _trophy = new Trophy(graphicsDevice, pos);
        }

        private static void SpawnOrcBoss(Vector2 pos, TiledObject obj, bool alreadyDefeated)
        {
            // Get max health from properties, default to 1500
            int maxHealth = 1500;
            if (obj.Properties.TryGetValue("Health", out string healthStr))
            {
                int.TryParse(healthStr, out maxHealth);
            }

            _orcBoss = new OrcBoss(
                _orcIdleTexture,
                _orcWalkTexture,
                _orcRunTexture,
                _orcAttackTexture,
                _orcWalkAttackTexture,
                _orcRunAttackTexture,
                _orcHurtTexture,
                _orcDeathTexture,
                pos,
                maxHealth
            );

            // If boss was already defeated, set it as dead on last frame
            if (alreadyDefeated)
            {
                _orcBoss.SetAsDefeated();
                System.Diagnostics.Debug.WriteLine($"Spawned defeated OrcBoss at {pos} (already dead)");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Spawned OrcBoss at {pos} with {maxHealth} HP");
            }

            // Also add to enemies list so player attacks can hit it
            _enemies.Add(_orcBoss);
        }

        private static void SpawnCrate(Vector2 pos, TiledObject obj, string roomName, Dictionary<string, HashSet<string>> destroyedCrates)
        {
            // Determine crate type from Name property
            CrateType crateType = CrateType.CrateCluster1; // default
            int health = 3; // default health - takes 3 Attack2 hits to destroy

            switch (obj.Name)
            {
                case "TallCrate":
                    crateType = CrateType.TallCrate;
                    break;
                case "CrateCluster1":
                    crateType = CrateType.CrateCluster1;
                    break;
                case "CrateCluster2":
                    crateType = CrateType.CrateCluster2;
                    break;
            }

            // Override health from properties if specified
            if (obj.Properties.TryGetValue("Health", out string healthStr))
            {
                int.TryParse(healthStr, out health);
            }

            string crateId = $"{obj.X}_{obj.Y}";

            // Check if this crate was already destroyed
            if (!destroyedCrates.ContainsKey(roomName))
                destroyedCrates[roomName] = new HashSet<string>();

            if (destroyedCrates[roomName].Contains(crateId))
            {
                System.Diagnostics.Debug.WriteLine($"Skipped destroyed crate {crateId} in room {roomName}");
                return; // Don't spawn destroyed crates
            }

            var crate = new Crate(_cratesTexture, pos, crateType, RoomManager.TilemapScale, health);
            crate.CrateId = crateId;
            _crates.Add(crate);

            System.Diagnostics.Debug.WriteLine($"Spawned Crate '{obj.Name}' at {pos} with {health} HP");
        }

        private static void SpawnTotem(Vector2 pos, TiledObject obj, string roomName,
            HashSet<string> activatedTotems, float scale)
        {
            string totemId = $"{roomName}_{obj.X}_{obj.Y}";
            string abilityName = obj.Name ?? ""; // e.g., "Dash", "Attack2"

            var totem = new Totem(_totemTexture, pos, scale, totemId, abilityName);

            // Check if this totem was already activated
            if (activatedTotems.Contains(totemId))
            {
                totem.SetDropped();
            }

            _totems.Add(totem);
        }

        // Spawn dynamic items (from destroyed vases, opened chests)
        public static void SpawnCoin(Vector2 position, bool requiresInteraction)
        {
            _coins.Add(new Coin(_coinTexture, position, 8, 8, requiresInteraction));
        }

        public static void SpawnPotion(Vector2 position, PotionType type, bool requiresInteraction)
        {
            _potions.Add(new Potion(_potionTexture, position, type, 3f, requiresInteraction));
        }

        // Update all entities
        public static void Update(GameTime gameTime, Player player, List<Rectangle> collisionBoxes)
        {
            // Update enemies and remove dead ones whose death animation is complete
            foreach (var enemy in _enemies.ToList())
            {
                enemy.Update(gameTime, player, collisionBoxes);
            }

            // Remove enemies that have finished their death animation (except OrcBoss - stays on last frame)
            _enemies.RemoveAll(e => e.IsDeathAnimationComplete && e != _orcBoss);

            foreach (var vase in _vases)
                vase.Update(gameTime);

            foreach (var coin in _coins)
                coin.Update(gameTime);

            foreach (var torch in _pillarTorches)
                torch.Update(gameTime);

            foreach (var torch in _wallTorches)
                torch.Update(gameTime);

            foreach (var potion in _potions)
                potion.Update(gameTime);

            foreach (var chest in _chests)
                chest.Update(gameTime);

            foreach (var button in _buttons)
                button.Update(gameTime);

            foreach (var totem in _totems)
                totem.Update(gameTime);

            foreach (var crate in _crates)
                crate.Update(gameTime);

            _trophy?.Update(gameTime);
        }

        // Remove methods
        public static void RemoveEnemy(IEnemy enemy) => _enemies.Remove(enemy);
        public static void RemoveVase(Vase vase) => _vases.Remove(vase);
        public static void RemoveCoin(Coin coin) => _coins.Remove(coin);
        public static void RemovePotion(Potion potion) => _potions.Remove(potion);
        public static void RemoveCrate(Crate crate) => _crates.Remove(crate);

        // Get crate collision boxes for player movement blocking
        public static List<Rectangle> GetCrateCollisionBoxes()
        {
            var boxes = new List<Rectangle>();
            foreach (var crate in _crates)
            {
                if (!crate.IsDestroyed)
                {
                    boxes.Add(crate.Hitbox);
                }
            }
            System.Diagnostics.Debug.WriteLine($"GetCrateCollisionBoxes: {boxes.Count} boxes from {_crates.Count} crates");
            return boxes;
        }

        // Get mutable lists for direct modification
        // Returns the actual list for player attack damage system
        public static List<IEnemy> GetEnemiesMutable() => _enemies;

        // These return copies for safe iteration with removal via EntityManager.Remove*() methods
        public static List<Vase> GetVasesMutable() => new List<Vase>(_vases);
        public static List<Coin> GetCoinsMutable() => new List<Coin>(_coins);
        public static List<Potion> GetPotionsMutable() => new List<Potion>(_potions);
        public static List<Chest> GetChestsMutable() => new List<Chest>(_chests);
    }
}
