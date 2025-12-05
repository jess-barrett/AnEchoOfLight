using GameProject2.Content.Player;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace GameProject2.Managers
{
    public static class InteractionSystem
    {
        // Interaction state for UI prompts
        public static bool ShowPrompt { get; set; }
        public static Vector2 PromptPosition { get; set; }
        public static bool ShowTrophyPrompt { get; private set; }
        public static Vector2 TrophyPromptPosition { get; private set; }

        // Pending chest spawn (deferred to next frame)
        private static bool _pendingChestSpawn;
        private static List<string> _pendingItems = new List<string>();
        private static Vector2 _pendingSpawnPosition;

        // Events for GameplayScreen to handle
        public static event Action<string, string> OnCheckpointActivated;  // room, checkpointName
        public static event Action OnTrophyInteracted;
        public static event Action<string, string> OnVaseDestroyed;        // roomName, vaseId

        public static void Update(
            GameTime gameTime,
            Player player,
            PlayerHUD hud,
            KeyboardState currentKeyboard,
            KeyboardState previousKeyboard,
            string currentRoom,
            Dictionary<string, HashSet<string>> destroyedVases,
            Dictionary<string, HashSet<string>> openedChests)
        {
            ShowPrompt = false;
            ShowTrophyPrompt = false;

            Rectangle playerHitbox = player.Hitbox;

            // Handle pending chest spawns from previous frame
            if (_pendingChestSpawn)
            {
                SpawnChestItems(_pendingItems, _pendingSpawnPosition);
                _pendingChestSpawn = false;
                _pendingItems.Clear();
            }

            // Check coin collection
            CheckCoinInteraction(player, hud, currentKeyboard, previousKeyboard);

            // Check potion collection
            CheckPotionInteraction(player, hud, currentKeyboard, previousKeyboard);

            // Check button (checkpoint) interaction
            CheckButtonInteraction(playerHitbox, currentRoom, currentKeyboard, previousKeyboard);

            // Check chest interaction
            CheckChestInteraction(playerHitbox, currentRoom, openedChests, currentKeyboard, previousKeyboard);

            // Check trophy interaction
            CheckTrophyInteraction(playerHitbox, currentKeyboard, previousKeyboard);
        }

        private static void CheckCoinInteraction(Player player, PlayerHUD hud,
            KeyboardState current, KeyboardState previous)
        {
            foreach (var coin in EntityManager.GetCoinsMutable())
            {
                if (!player.Hitbox.Intersects(coin.Hitbox)) continue;

                if (coin.RequiresInteraction)
                {
                    ShowPrompt = true;
                    PromptPosition = new Vector2(coin.Position.X, coin.Position.Y - 30);

                    if (current.IsKeyDown(Keys.E) && previous.IsKeyUp(Keys.E))
                    {
                        coin.IsCollected = true;
                        hud.AddCoin();
                        EntityManager.RemoveCoin(coin);
                    }
                }
                else
                {
                    // Auto-collect
                    coin.IsCollected = true;
                    hud.AddCoin();
                    EntityManager.RemoveCoin(coin);
                }
            }
        }

        private static void CheckPotionInteraction(Player player, PlayerHUD hud,
            KeyboardState current, KeyboardState previous)
        {
            foreach (var potion in EntityManager.GetPotionsMutable())
            {
                if (!player.Hitbox.Intersects(potion.Hitbox)) continue;

                if (potion.RequiresInteraction)
                {
                    ShowPrompt = true;
                    PromptPosition = new Vector2(potion.Position.X, potion.Position.Y - 30);

                    if (current.IsKeyDown(Keys.E) && previous.IsKeyUp(Keys.E))
                    {
                        potion.IsCollected = true;
                        potion.ApplyEffect(hud);
                        EntityManager.RemovePotion(potion);

                        // Trigger heal animation for health potions
                        if (potion.Type == PotionType.RedMini || potion.Type == PotionType.Red)
                        {
                            player.TriggerHealAnimation();
                        }
                    }
                }
                else
                {
                    potion.IsCollected = true;
                    potion.ApplyEffect(hud);
                    EntityManager.RemovePotion(potion);

                    // Trigger heal animation for health potions
                    if (potion.Type == PotionType.RedMini || potion.Type == PotionType.Red)
                    {
                        player.TriggerHealAnimation();
                    }
                }
            }
        }

        private static void CheckButtonInteraction(Rectangle playerHitbox, string currentRoom,
            KeyboardState current, KeyboardState previous)
        {
            foreach (var button in EntityManager.Buttons)
            {
                if (!playerHitbox.Intersects(button.Hitbox)) continue;

                if (!button.IsPressed)
                {
                    ShowPrompt = true;
                    PromptPosition = new Vector2(button.Position.X, button.Position.Y - 50);
                }

                if (!button.IsPressed && current.IsKeyDown(Keys.E) && previous.IsKeyUp(Keys.E))
                {
                    button.Press();

                    // Reset other buttons
                    foreach (var other in EntityManager.Buttons)
                    {
                        if (other != button)
                            other.Reset();
                    }

                    button.IsCurrentCheckpoint = true;
                    OnCheckpointActivated?.Invoke(currentRoom, button.CheckpointName);
                }
            }
        }

        private static void CheckChestInteraction(Rectangle playerHitbox, string currentRoom,
            Dictionary<string, HashSet<string>> openedChests,
            KeyboardState current, KeyboardState previous)
        {
            foreach (var chest in EntityManager.Chests)
            {
                if (!playerHitbox.Intersects(chest.Hitbox)) continue;

                if (chest.State == ChestState.Closed)
                {
                    ShowPrompt = true;
                    PromptPosition = new Vector2(chest.Position.X, chest.Position.Y - 50);
                }

                if (chest.State == ChestState.Closed && current.IsKeyDown(Keys.E) && previous.IsKeyUp(Keys.E))
                {
                    chest.Open();

                    if (!openedChests.ContainsKey(currentRoom))
                        openedChests[currentRoom] = new HashSet<string>();
                    openedChests[currentRoom].Add(chest.ChestId);

                    // Debug: Check what items are in the chest
                    System.Diagnostics.Debug.WriteLine($"Opening chest with {chest.Items.Count} items:");
                    foreach (var item in chest.Items)
                    {
                        System.Diagnostics.Debug.WriteLine($"  - {item}");
                    }

                    // Schedule item spawn for next frame (after chest animation starts)
                    _pendingChestSpawn = true;
                    _pendingItems = new List<string>(chest.Items);
                    _pendingSpawnPosition = chest.Position;

                    System.Diagnostics.Debug.WriteLine($"Scheduled to spawn items at position {_pendingSpawnPosition}");
                }
            }
        }

        private static void CheckTrophyInteraction(Rectangle playerHitbox,
            KeyboardState current, KeyboardState previous)
        {
            var trophyObj = RoomManager.FindTrophyAtPosition(playerHitbox);
            if (trophyObj != null)
            {
                ShowTrophyPrompt = true;
                TrophyPromptPosition = new Vector2(
                    trophyObj.X * RoomManager.TilemapScale,
                    trophyObj.Y * RoomManager.TilemapScale - 50);

                if (current.IsKeyDown(Keys.E) && previous.IsKeyUp(Keys.E))
                {
                    OnTrophyInteracted?.Invoke();
                }
            }
        }

        private static void SpawnChestItems(List<string> items, Vector2 chestPosition)
        {
            System.Diagnostics.Debug.WriteLine($"SpawnChestItems called with {items.Count} items at {chestPosition}");

            Random rng = new Random();
            foreach (var item in items)
            {
                System.Diagnostics.Debug.WriteLine($"Processing item: {item}");

                // Add some random spread so items don't all spawn in exact same spot
                float offsetX = (float)(rng.NextDouble() * 40 - 20); // -20 to +20
                float offsetY = (float)(rng.NextDouble() * 20 + 30); // +30 to +50 (downward)

                Vector2 itemPos = new Vector2(
                    chestPosition.X + offsetX,
                    chestPosition.Y + offsetY
                );

                if (item.StartsWith("Potion."))
                {
                    string potionTypeStr = item.Substring(7); // Remove "Potion." prefix
                    System.Diagnostics.Debug.WriteLine($"Attempting to spawn potion: {potionTypeStr}");

                    if (Enum.TryParse(potionTypeStr, out PotionType potionType))
                    {
                        EntityManager.SpawnPotion(itemPos, potionType, true);
                        System.Diagnostics.Debug.WriteLine($"Successfully spawned {potionType} potion at {itemPos}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to parse potion type: {potionTypeStr}");
                    }
                }
                else if (item == "Coin")
                {
                    EntityManager.SpawnCoin(itemPos, true);
                    System.Diagnostics.Debug.WriteLine($"Spawned coin at {itemPos}");
                }
            }
        }

        // For vase destruction (called by combat system in GameplayScreen)
        public static void HandleVaseDestroyed(Vase vase, string currentRoom,
            Dictionary<string, HashSet<string>> destroyedVases)
        {
            if (!destroyedVases.ContainsKey(currentRoom))
                destroyedVases[currentRoom] = new HashSet<string>();

            destroyedVases[currentRoom].Add(vase.VaseId);
            EntityManager.SpawnCoin(vase.Position, false); // Auto-collect coin
            EntityManager.RemoveVase(vase);
            OnVaseDestroyed?.Invoke(currentRoom, vase.VaseId);
        }

        // Clear event subscriptions (call in GameplayScreen.Unload)
        public static void ClearEvents()
        {
            OnCheckpointActivated = null;
            OnTrophyInteracted = null;
            OnVaseDestroyed = null;
        }
    }
}
