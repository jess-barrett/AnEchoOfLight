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
        private static List<Totem> _totems = new List<Totem>();
        private static Trophy _trophy;

        // Public read-only accessors
        public static IReadOnlyList<IEnemy> Enemies => _enemies;
        public static IReadOnlyList<Vase> Vases => _vases;
        public static IReadOnlyList<Coin> Coins => _coins;
        public static IReadOnlyList<Chest> Chests => _chests;
        public static IReadOnlyList<Potion> Potions => _potions;
        public static IReadOnlyList<Button> Buttons => _buttons;
        public static IReadOnlyList<PillarTorch> PillarTorches => _pillarTorches;
        public static IReadOnlyList<Totem> Totems => _totems;
        public static Trophy Trophy => _trophy;

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
            _totems.Clear();
            _trophy = null;
        }

        // Unified spawning from tilemap (eliminates duplication between Activate and LoadRoom)
        public static void SpawnFromTilemap(
            Tilemap tilemap,
            string roomName,
            float tilemapScale,
            Dictionary<string, HashSet<string>> destroyedVases,
            Dictionary<string, HashSet<string>> openedChests,
            HashSet<string> activatedTotems,
            string lastCheckpointName,
            string lastCheckpointRoom,
            GraphicsDevice graphicsDevice)
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
                            SpawnVase(pos, obj, roomName, destroyedVases);
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
            Dictionary<string, HashSet<string>> destroyedVases)
        {
            string vaseId = $"{obj.X}_{obj.Y}";

            if (!destroyedVases.ContainsKey(roomName))
                destroyedVases[roomName] = new HashSet<string>();

            if (!destroyedVases[roomName].Contains(vaseId))
            {
                var vase = new Vase(_vaseTexture, pos, 16, 8);
                vase.VaseId = vaseId;
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
            foreach (var prop in obj.Properties)
            {
                string itemType = prop.Key;
                string itemValue = prop.Value;

                System.Diagnostics.Debug.WriteLine($"  Property: {itemType} = {itemValue}");

                if (itemType == "Potion")
                {
                    items.Add($"Potion.{itemValue}");
                    System.Diagnostics.Debug.WriteLine($"  Added item: Potion.{itemValue}");
                }
                else if (itemType == "Coin")
                {
                    // Parse coin count from value (e.g., "3" means 3 coins)
                    int coinCount = 1;
                    if (int.TryParse(itemValue, out int parsed))
                    {
                        coinCount = parsed;
                    }

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

            // Remove enemies that have finished their death animation
            _enemies.RemoveAll(e => e.IsDeathAnimationComplete);

            foreach (var vase in _vases)
                vase.Update(gameTime);

            foreach (var coin in _coins)
                coin.Update(gameTime);

            foreach (var torch in _pillarTorches)
                torch.Update(gameTime);

            foreach (var potion in _potions)
                potion.Update(gameTime);

            foreach (var chest in _chests)
                chest.Update(gameTime);

            foreach (var button in _buttons)
                button.Update(gameTime);

            foreach (var totem in _totems)
                totem.Update(gameTime);

            _trophy?.Update(gameTime);
        }

        // Remove methods
        public static void RemoveEnemy(IEnemy enemy) => _enemies.Remove(enemy);
        public static void RemoveVase(Vase vase) => _vases.Remove(vase);
        public static void RemoveCoin(Coin coin) => _coins.Remove(coin);
        public static void RemovePotion(Potion potion) => _potions.Remove(potion);

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
