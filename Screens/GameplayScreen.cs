using Comora;
using GameProject2.Content.Player;
using GameProject2.Graphics3D;
using GameProject2.SaveSystem;
using GameProject2.StateManagement;
using GameProject2.Tilemaps;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GameProject2.Screens
{
    public class GameplayScreen : GameScreen
    {
        private ContentManager _content;
        private SpriteBatch _spriteBatch;

        private Player player;
        private PlayerHUD hud;

        private Camera camera;
        private ParticleSystem particleSystem;
        private Texture2D pixelTexture;

        private string currentRoom = "StartingRoom";
        private string lastRoomName = null;

        private string lastCheckpointRoom = "StartingRoom";
        private string lastCheckpointName = "CP-StartingRoom";
        private bool showCheckpointMessage = false;
        private float checkpointMessageTimer = 0f;
        private float checkpointMessageDuration = 2f;

        private bool showDemoMessage = false;
        private float demoMessageTimer = 0f;
        private float demoMessageDuration = 5f;

        private Texture2D vaseTexture;
        private Dictionary<string, HashSet<string>> destroyedVasesPerRoom = new Dictionary<string, HashSet<string>>();
        private Texture2D coinTexture;
        private Texture2D buttonTexture;
        private List<Vase> vases = new List<Vase>();
        private List<Coin> coins = new List<Coin>();
        private List<Button> buttons = new List<Button>();

        private Texture2D torchTilesetTexture;
        private List<PillarTorch> pillarTorches = new List<PillarTorch>();

        private Texture2D potionTexture;
        private List<Potion> potions = new List<Potion>();

        private Texture2D chestTexture;
        private List<Chest> chests = new List<Chest>();
        private Dictionary<string, HashSet<string>> openedChestsPerRoom = new Dictionary<string, HashSet<string>>();

        // For spawning items from chests
        private bool spawnItemsNextFrame = false;
        private List<string> itemsToSpawn = new List<string>();
        private Vector2 spawnPosition;

        private Trophy trophy;
        private bool showTrophyPrompt = false;
        private Vector2 trophyPromptPosition;
        private KeyboardState previousKeyboardState;
        private Matrix view3D;
        private Matrix projection3D;

        private Texture2D skullSheet;
        private List<Skull> enemies = new List<Skull>();

        private Tilemap tilemap;

        private List<Rectangle> collisionBoxes = new List<Rectangle>();

        private SpriteFont instructionFont;
        private float instructionTimer = 8f;
        private string instructionText = "WASD to move | SHIFT to sprint | SPACE to attack";

        private bool showButtonPrompt = false;
        private Vector2 buttonPromptPosition;

        private bool isDying = false;
        private float deathFadeTimer = 0f;
        private float deathFadeDuration = 3f;
        private float deathFadeAlpha = 0f;
        private Texture2D fadeTexture;


        public GameplayScreen()
        {
            TransitionOnTime = TimeSpan.FromSeconds(1.0);
            TransitionOffTime = TimeSpan.FromSeconds(0.5);
        }

        public override void Activate()
        {
            if (_content == null)
                _content = new ContentManager(ScreenManager.Game.Services, "Content");

            _spriteBatch = new SpriteBatch(ScreenManager.GraphicsDevice);

            fadeTexture = new Texture2D(ScreenManager.GraphicsDevice, 1, 1);
            fadeTexture.SetData(new[] { Color.Black });

            AudioManager.PlayGameplayMusicWithIntro();

            string tmxPath = Path.Combine(_content.RootDirectory, "Rooms", "StartingRoom.tmx");
            tilemap = TmxLoader.Load(tmxPath, _content);

            float tilemapScale = 4f;
            collisionBoxes.Clear();

            foreach (var objectLayer in tilemap.ObjectLayers)
            {
                if (objectLayer.Name == "Collision")
                {
                    foreach (var obj in objectLayer.Objects)
                    {
                        Rectangle collisionRect = new Rectangle(
                            (int)(obj.X * tilemapScale),
                            (int)(obj.Y * tilemapScale),
                            (int)(obj.Width * tilemapScale),
                            (int)(obj.Height * tilemapScale)
                        );
                        collisionBoxes.Add(collisionRect);
                    }
                }
            }

            player = new Player();
            hud = new PlayerHUD();
            hud.LoadContent(_content);

            camera = new Camera(ScreenManager.GraphicsDevice);
            particleSystem = new ParticleSystem(ScreenManager.GraphicsDevice);

            instructionFont = _content.Load<SpriteFont>("InstructionFont");

            torchTilesetTexture = _content.Load<Texture2D>("Tilemaps/Set 4.8");

            skullSheet = _content.Load<Texture2D>("skull");

            // load animations
            var idleDown = _content.Load<Texture2D>("Player/Sprites/IDLE/idle_down");
            var idleUp = _content.Load<Texture2D>("Player/Sprites/IDLE/idle_up");
            var idleLeft = _content.Load<Texture2D>("Player/Sprites/IDLE/idle_left");
            var idleRight = _content.Load<Texture2D>("Player/Sprites/IDLE/idle_right");

            var walkDown = _content.Load<Texture2D>("Player/Sprites/WALK/walk_down");
            var walkUp = _content.Load<Texture2D>("Player/Sprites/WALK/walk_up");
            var walkLeft = _content.Load<Texture2D>("Player/Sprites/WALK/walk_left");
            var walkRight = _content.Load<Texture2D>("Player/Sprites/WALK/walk_right");

            var runDown = _content.Load<Texture2D>("Player/Sprites/RUN/run_down");
            var runUp = _content.Load<Texture2D>("Player/Sprites/RUN/run_up");
            var runLeft = _content.Load<Texture2D>("Player/Sprites/RUN/run_left");
            var runRight = _content.Load<Texture2D>("Player/Sprites/RUN/run_right");

            var attack1Down = _content.Load<Texture2D>("Player/Sprites/ATTACK 1/attack1_down");
            var attack1Up = _content.Load<Texture2D>("Player/Sprites/ATTACK 1/attack1_up");
            var attack1Left = _content.Load<Texture2D>("Player/Sprites/ATTACK 1/attack1_left");
            var attack1Right = _content.Load<Texture2D>("Player/Sprites/ATTACK 1/attack1_right");

            var hurtDown = _content.Load<Texture2D>("Player/Sprites/HURT/hurt_down");
            var hurtUp = _content.Load<Texture2D>("Player/Sprites/HURT/hurt_up");
            var hurtLeft = _content.Load<Texture2D>("Player/Sprites/HURT/hurt_left");
            var hurtRight = _content.Load<Texture2D>("Player/Sprites/HURT/hurt_right");

            var deathDown = _content.Load<Texture2D>("Player/Sprites/DEATH/death_down");
            var deathUp = _content.Load<Texture2D>("Player/Sprites/DEATH/death_up");
            var deathLeft = _content.Load<Texture2D>("Player/Sprites/DEATH/death_left");
            var deathRight = _content.Load<Texture2D>("Player/Sprites/DEATH/death_right");

            List<SpriteAnimation[]> animations = new List<SpriteAnimation[]>();

            player.idleAnimations[0] = new SpriteAnimation(idleDown, 8, 8);
            player.idleAnimations[1] = new SpriteAnimation(idleUp, 8, 8);
            player.idleAnimations[2] = new SpriteAnimation(idleLeft, 8, 8);
            player.idleAnimations[3] = new SpriteAnimation(idleRight, 8, 8);
            animations.Add(player.idleAnimations);

            player.walkAnimations[0] = new SpriteAnimation(walkDown, 8, 8);
            player.walkAnimations[1] = new SpriteAnimation(walkUp, 8, 8);
            player.walkAnimations[2] = new SpriteAnimation(walkLeft, 8, 8);
            player.walkAnimations[3] = new SpriteAnimation(walkRight, 8, 8);
            animations.Add(player.walkAnimations);

            player.runAnimations[0] = new SpriteAnimation(runDown, 8, 12);
            player.runAnimations[1] = new SpriteAnimation(runUp, 8, 12);
            player.runAnimations[2] = new SpriteAnimation(runLeft, 8, 12);
            player.runAnimations[3] = new SpriteAnimation(runRight, 8, 12);
            animations.Add(player.runAnimations);

            player.attack1Animations[0] = new SpriteAnimation(attack1Down, 8, 24);
            player.attack1Animations[1] = new SpriteAnimation(attack1Up, 8, 24);
            player.attack1Animations[2] = new SpriteAnimation(attack1Left, 8, 24);
            player.attack1Animations[3] = new SpriteAnimation(attack1Right, 8, 24);
            animations.Add(player.attack1Animations);

            player.hurtAnimations[0] = new SpriteAnimation(hurtDown, 4, 8);
            player.hurtAnimations[1] = new SpriteAnimation(hurtUp, 4, 8);
            player.hurtAnimations[2] = new SpriteAnimation(hurtLeft, 4, 8);
            player.hurtAnimations[3] = new SpriteAnimation(hurtRight, 4, 8);
            player.hurtAnimations[3] = new SpriteAnimation(hurtRight, 4, 8);
            animations.Add(player.hurtAnimations);

            player.deathAnimations[0] = new SpriteAnimation(deathDown, 7, 7); // Adjust frame count based on your sprite sheets
            player.deathAnimations[1] = new SpriteAnimation(deathUp, 7, 7);
            player.deathAnimations[2] = new SpriteAnimation(deathLeft, 7, 7);
            player.deathAnimations[3] = new SpriteAnimation(deathRight, 7, 7);
            animations.Add(player.deathAnimations);

            player.Animation = player.idleAnimations[0];

            foreach (var animSet in animations)
            {
                foreach (var anim in animSet)
                {
                    anim.Scale = 4f;
                    int frameWidth = anim.GetTexture.Width / anim.FrameCount;
                    int frameHeight = anim.GetTexture.Height;
                    anim.Origin = new Vector2(frameWidth / 2f, frameHeight / 2f);
                }
            }

            vaseTexture = _content.Load<Texture2D>("Interactables/Vase");
            coinTexture = _content.Load<Texture2D>("Interactables/Coin");
            buttonTexture = _content.Load<Texture2D>("Interactables/Button");
            potionTexture = _content.Load<Texture2D>("Interactables/Set 2.4");
            chestTexture = _content.Load<Texture2D>("Interactables/Chest");

            foreach (var objectLayer in tilemap.ObjectLayers)
            {
                if (objectLayer.Name == "Objects")
                {
                    foreach (var obj in objectLayer.Objects)
                    {
                        if (obj.Class == "Vase")
                        {
                            Vector2 vasePos = new Vector2(obj.X * tilemapScale, obj.Y * tilemapScale);

                            string vaseId = $"{obj.X}_{obj.Y}";

                            if (!destroyedVasesPerRoom.ContainsKey(currentRoom))
                            {
                                destroyedVasesPerRoom[currentRoom] = new HashSet<string>();
                            }

                            if (!destroyedVasesPerRoom[currentRoom].Contains(vaseId))
                            {
                                Vase vase = new Vase(vaseTexture, vasePos, 16, 8);
                                vase.VaseId = vaseId;
                                vases.Add(vase);
                                System.Diagnostics.Debug.WriteLine($"Initial spawn vase {vaseId} in room {currentRoom}");
                            }
                        }
                        else if (obj.Class == "Skull")
                        {
                            Vector2 skullPos = new Vector2(obj.X * tilemapScale, obj.Y * tilemapScale);
                            Skull skull = new Skull(skullSheet, 10, 10, skullPos); // Pass position directly
                            enemies.Add(skull);
                        }
                        else if (obj.Class == "Button")
                        {
                            Vector2 buttonPos = new Vector2(obj.X * tilemapScale, obj.Y * tilemapScale);
                            Button button = new Button(buttonTexture, buttonPos, 2f, obj.Name);

                            if (obj.Name == lastCheckpointName && currentRoom == lastCheckpointRoom)
                            {
                                button.IsCurrentCheckpoint = true;
                                button.Press();
                            }

                            buttons.Add(button);
                        }
                        else if (obj.Class == "PillarTorch")
                        {
                            Vector2 torchPos = new Vector2(obj.X * tilemapScale, obj.Y * tilemapScale);
                            PillarTorch torch = new PillarTorch(torchTilesetTexture, torchPos, tilemapScale);
                            pillarTorches.Add(torch);
                        }
                        else if (obj.Class == "Potion")
                        {
                            Vector2 potionPos = new Vector2(obj.X * tilemapScale, obj.Y * tilemapScale);

                            string potionTypeStr = obj.Name ?? "RedMini";

                            if (System.Enum.TryParse(potionTypeStr, out PotionType potionType))
                            {
                                Potion potion = new Potion(potionTexture, potionPos, potionType, 3f, true);
                                potions.Add(potion);
                            }
                        }
                        else if (obj.Class == "Chest")
                        {
                            Vector2 chestPos = new Vector2(obj.X * tilemapScale, obj.Y * tilemapScale);

                            string chestId = $"{obj.X}_{obj.Y}";

                            System.Diagnostics.Debug.WriteLine($"Loading chest at {chestPos}, Properties count: {obj.Properties.Count}");

                            List<string> items = new List<string>();
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
                                    items.Add("Coin");
                                    System.Diagnostics.Debug.WriteLine($"  Added item: Coin");
                                }
                            }

                            System.Diagnostics.Debug.WriteLine($"Chest created with {items.Count} items");

                            Chest chest = new Chest(chestTexture, chestPos, 4f, items, chestId);

                            if (!openedChestsPerRoom.ContainsKey(currentRoom))
                            {
                                openedChestsPerRoom[currentRoom] = new HashSet<string>();
                            }

                            if (openedChestsPerRoom[currentRoom].Contains(chestId))
                            {
                                chest.SetOpened();
                            }

                            chests.Add(chest);
                        }
                    }
                }
            }

            // Setup 3D projection
            projection3D = Matrix.CreatePerspectiveFieldOfView(
                MathHelper.PiOver4,
                ScreenManager.GraphicsDevice.Viewport.AspectRatio,
                0.1f,
                1000f
            );

            // Look for trophy spawn point in tilemap
            foreach (var objectLayer in tilemap.ObjectLayers)
            {
                foreach (var obj in objectLayer.Objects)
                {
                    if (obj.Class == "Trophy")
                    {
                        tilemapScale = 4f;
                        Vector2 trophyPos = new Vector2(obj.X * tilemapScale, obj.Y * tilemapScale);
                        trophy = new Trophy(ScreenManager.GraphicsDevice, trophyPos);
                        break;
                    }
                }
            }

            LoadGame();
        }

        public override void Unload()
        {
            SaveGame();
            _content.Unload();
        }

        public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
        {
            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);

            // Don't process input if another screen has focus
            if (otherScreenHasFocus)
            {
                previousKeyboardState = Keyboard.GetState();
                return;
            }

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            showTrophyPrompt = false;

            if (instructionTimer > 0)
                instructionTimer -= dt;

            if (showDemoMessage)
            {
                demoMessageTimer -= dt;
                if (demoMessageTimer <= 0)
                {
                    showDemoMessage = false;
                }
            }

            // update all skulls
            foreach (var skull in enemies)
                skull.Update(gameTime, player, collisionBoxes);

            // Check for collisions between player and skulls
            RotatedRectangle playerHitbox = player.RotatedHitbox;
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                var skull = enemies[i];
                if (playerHitbox.Intersects(skull.RotatedHitbox))
                {
                    if (player.State != PlayerState.Attack1 && player.State != PlayerState.Hurt && player.State != PlayerState.Death)
                    {
                        hud.TakeDamage();

                        // Check if player died
                        if (hud.CurrentHealth <= 0)
                        {
                            player.State = PlayerState.Death;
                            player.Animation = player.deathAnimations[(int)player.Direction];
                            player.Animation.IsLooping = false;
                            player.Animation.setFrame(0);
                            isDying = true;
                            deathFadeTimer = 0f;
                            AudioManager.PlayDeathSound(0.25f);
                        }
                        else
                        {
                            player.State = PlayerState.Hurt;
                            player.Animation = player.hurtAnimations[(int)player.Direction];
                            player.Animation.IsLooping = false;
                            player.Animation.setFrame(0);
                            AudioManager.PlayTakeDamageSound(0.5f);
                        }

                        particleSystem.CreateSkullDeathEffect(skull.Position);
                        enemies.RemoveAt(i);
                    }
                }
            }

            // Update vases
            foreach (var vase in vases)
                vase.Update(gameTime);

            // Update coins
            foreach (var coin in coins)
                coin.Update(gameTime);

            foreach (var torch in pillarTorches)
                torch.Update(gameTime);

            // Update potions
            foreach (var potion in potions)
                potion.Update(gameTime);

            // Update chests
            foreach (var chest in chests)
                chest.Update(gameTime);

            // Handle spawning items from opened chests
            if (spawnItemsNextFrame)
            {
                SpawnItemsFromChest(itemsToSpawn, spawnPosition);
                spawnItemsNextFrame = false;
                itemsToSpawn.Clear();
            }

            // Check player hitting vases
            if (player.State == PlayerState.Attack1)
            {
                Rectangle attackHitbox = player.Hitbox;
                int verticalRange = 40;
                int horizontalRange = 100;

                switch (player.Direction)
                {
                    case Direction.Up:
                        attackHitbox.Y -= verticalRange;
                        attackHitbox.Height += verticalRange;
                        break;
                    case Direction.Down:
                        attackHitbox.Height += verticalRange;
                        break;
                    case Direction.Left:
                        attackHitbox.X -= horizontalRange;
                        attackHitbox.Width += horizontalRange;
                        break;
                    case Direction.Right:
                        attackHitbox.Width += horizontalRange;
                        break;
                }

                for (int i = vases.Count - 1; i >= 0; i--)
                {
                    if (!vases[i].IsDestroyed && attackHitbox.Intersects(vases[i].Hitbox))
                    {
                        vases[i].IsDestroyed = true;
                        coins.Add(new Coin(coinTexture, vases[i].Position, 8, 8, false));

                        // Mark vase as destroyed
                        if (!destroyedVasesPerRoom.ContainsKey(currentRoom))
                        {
                            destroyedVasesPerRoom[currentRoom] = new HashSet<string>();
                        }
                        destroyedVasesPerRoom[currentRoom].Add(vases[i].VaseId);

                        vases.RemoveAt(i);
                    }
                }
            }

            KeyboardState currentKeyboardState = Keyboard.GetState();

            showButtonPrompt = false;

            // Check player collecting coins
            for (int i = coins.Count - 1; i >= 0; i--)
            {
                if (player.Hitbox.Intersects(coins[i].Hitbox))
                {
                    // If coin requires interaction, show prompt and wait for E
                    if (coins[i].RequiresInteraction)
                    {
                        showButtonPrompt = true;
                        buttonPromptPosition = new Vector2(coins[i].Position.X, coins[i].Position.Y - 30);

                        if (currentKeyboardState.IsKeyDown(Keys.E) && previousKeyboardState.IsKeyUp(Keys.E))
                        {
                            coins[i].IsCollected = true;
                            hud.AddCoin();
                            coins.RemoveAt(i);
                        }
                    }
                    else
                    {
                        // Auto-collect coins from vases
                        coins[i].IsCollected = true;
                        hud.AddCoin();
                        coins.RemoveAt(i);
                    }
                }
            }

            // Check player collecting potions
            for (int i = potions.Count - 1; i >= 0; i--)
            {
                if (player.Hitbox.Intersects(potions[i].Hitbox))
                {
                    if (potions[i].RequiresInteraction)
                    {
                        showButtonPrompt = true;
                        buttonPromptPosition = new Vector2(potions[i].Position.X, potions[i].Position.Y - 30);

                        if (currentKeyboardState.IsKeyDown(Keys.E) && previousKeyboardState.IsKeyUp(Keys.E))
                        {
                            potions[i].IsCollected = true;
                            potions[i].ApplyEffect(hud);
                            potions.RemoveAt(i);
                            previousKeyboardState = currentKeyboardState;
                            return;
                        }
                    }
                    else
                    {
                        // Auto-collect potions that don't require interaction
                        potions[i].IsCollected = true;
                        potions[i].ApplyEffect(hud);
                        potions.RemoveAt(i);
                    }
                }
            }

            // Check door collisions for room transitions
            float tilemapScale = 4f;
            Rectangle currentPlayerHitbox = player.Hitbox;

            // Update checkpoint message timer
            if (showCheckpointMessage)
            {
                checkpointMessageTimer -= dt;
                if (checkpointMessageTimer <= 0)
                {
                    showCheckpointMessage = false;
                }
            }

            // Update buttons
            foreach (var button in buttons)
            {
                button.Update(gameTime);
            }

            currentKeyboardState = Keyboard.GetState();

            // Check button interactions
            foreach (var button in buttons)
            {
                if (currentPlayerHitbox.Intersects(button.Hitbox))
                {
                    // Show prompt if button not already pressed
                    if (!button.IsPressed)
                    {
                        showButtonPrompt = true;
                        buttonPromptPosition = new Vector2(button.Position.X, button.Position.Y - 50);
                    }

                    // Press button with E
                    if (!button.IsPressed && currentKeyboardState.IsKeyDown(Keys.E) && previousKeyboardState.IsKeyUp(Keys.E))
                    {
                        // Press the button
                        button.Press();

                        // Reset all other buttons in this room
                        foreach (var otherButton in buttons)
                        {
                            if (otherButton != button)
                            {
                                otherButton.Reset();
                            }
                        }

                        // Mark this as the current checkpoint
                        button.IsCurrentCheckpoint = true;
                        lastCheckpointRoom = currentRoom;
                        lastCheckpointName = button.CheckpointName;

                        // Show message
                        showCheckpointMessage = true;
                        checkpointMessageTimer = checkpointMessageDuration;

                        System.Diagnostics.Debug.WriteLine($"Checkpoint activated: {lastCheckpointName} in {lastCheckpointRoom}");
                    }
                }
            }

            // Check chest interactions
            foreach (var chest in chests)
            {
                if (currentPlayerHitbox.Intersects(chest.Hitbox))
                {
                    // Show prompt if chest not opened
                    if (chest.State == ChestState.Closed)
                    {
                        showButtonPrompt = true;
                        buttonPromptPosition = new Vector2(chest.Position.X, chest.Position.Y - 50);
                    }

                    // Open chest with E
                    if (chest.State == ChestState.Closed && currentKeyboardState.IsKeyDown(Keys.E) && previousKeyboardState.IsKeyUp(Keys.E))
                    {
                        chest.Open();

                        // Mark chest as opened in this room
                        if (!openedChestsPerRoom.ContainsKey(currentRoom))
                        {
                            openedChestsPerRoom[currentRoom] = new HashSet<string>();
                        }
                        openedChestsPerRoom[currentRoom].Add(chest.ChestId);

                        // Debug: Check what items are in the chest
                        System.Diagnostics.Debug.WriteLine($"Opening chest with {chest.Items.Count} items:");
                        foreach (var item in chest.Items)
                        {
                            System.Diagnostics.Debug.WriteLine($"  - {item}");
                        }

                        // Schedule items to spawn next frame (after chest animation starts)
                        spawnItemsNextFrame = true;
                        itemsToSpawn = new List<string>(chest.Items);
                        spawnPosition = chest.Position;

                        System.Diagnostics.Debug.WriteLine($"Scheduled to spawn items at position {spawnPosition}");
                    }
                }
            }

            foreach (var objectLayer in tilemap.ObjectLayers)
            {
                foreach (var obj in objectLayer.Objects)
                {
                    if (obj.Class == "Door")
                    {
                        Rectangle doorRect = new Rectangle(
                            (int)(obj.X * tilemapScale),
                            (int)(obj.Y * tilemapScale),
                            (int)(obj.Width * tilemapScale),
                            (int)(obj.Height * tilemapScale)
                        );

                        if (currentPlayerHitbox.Intersects(doorRect))
                        {
                            string nextRoom = obj.Name;
                            LoadRoom(nextRoom, $"From{currentRoom}");
                            return;
                        }
                    }
                    else if (obj.Class == "Trophy")
                    {
                        Rectangle trophyRect = new Rectangle(
                            (int)(obj.X * tilemapScale),
                            (int)(obj.Y * tilemapScale),
                            (int)(obj.Width * tilemapScale),
                            (int)(obj.Height * tilemapScale)
                        );

                        if (currentPlayerHitbox.Intersects(trophyRect))
                        {
                            showTrophyPrompt = true;
                            trophyPromptPosition = new Vector2(obj.X * tilemapScale, obj.Y * tilemapScale - 50);

                            if (currentKeyboardState.IsKeyDown(Keys.E) && previousKeyboardState.IsKeyUp(Keys.E))
                            {
                                System.Diagnostics.Debug.WriteLine($"E pressed - opening trophy screen");
                                showTrophyPrompt = false;
                                ScreenManager.AddScreen(new TrophyScreen(), ControllingPlayer);
                                previousKeyboardState = currentKeyboardState; // Update immediately
                                return;
                            }
                        }
                    }
                    else if (obj.Class == "Checkpoint")
                    {
                        Rectangle checkpointRect = new Rectangle(
                            (int)(obj.X * tilemapScale),
                            (int)(obj.Y * tilemapScale),
                            (int)(obj.Width * tilemapScale),
                            (int)(obj.Height * tilemapScale)
                        );

                        if (currentPlayerHitbox.Intersects(checkpointRect))
                        {
                            lastCheckpointRoom = currentRoom;
                            lastCheckpointName = obj.Name;
                        }
                    }
                }
            }

            particleSystem.Update(gameTime);
            player.Update(gameTime, enemies, particleSystem, collisionBoxes);
            hud.Update(gameTime);

            if (trophy != null)
                trophy.Update(gameTime);

            if (isDying)
            {
                deathFadeTimer += dt;
                deathFadeAlpha = MathHelper.Clamp(deathFadeTimer / deathFadeDuration, 0f, 1f);

                // When fade is complete, respawn player
                if (deathFadeTimer >= deathFadeDuration)
                {
                    RespawnPlayer();
                    isDying = false;
                    deathFadeTimer = 0f;
                    deathFadeAlpha = 0f;
                }

                return;
            }

            // Update 3D view to match camera
            view3D = Matrix.CreateLookAt(
                new Vector3(camera.Position.X, 10f, camera.Position.Y + 15f),
                new Vector3(camera.Position.X, 0f, camera.Position.Y),
                Vector3.Up
            );

            float tilemapWidth = tilemap.Width * tilemap.TileWidth * tilemapScale;
            float tilemapHeight = tilemap.Height * tilemap.TileHeight * tilemapScale;
            float halfScreenWidth = ScreenManager.GraphicsDevice.Viewport.Width / 2f;
            float halfScreenHeight = ScreenManager.GraphicsDevice.Viewport.Height / 2f;

            camera.Position = new Vector2(
                MathHelper.Clamp(player.Position.X, halfScreenWidth, tilemapWidth - halfScreenWidth),
                MathHelper.Clamp(player.Position.Y, halfScreenHeight, tilemapHeight - halfScreenHeight)
            );

            previousKeyboardState = currentKeyboardState;

            camera.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            ScreenManager.GraphicsDevice.Clear(Color.CornflowerBlue);

            _spriteBatch.Begin(
                camera,
                SpriteSortMode.BackToFront,
                BlendState.AlphaBlend,
                SamplerState.PointClamp,
                DepthStencilState.Default,
                RasterizerState.CullNone
            );

            // Draw ONLY the floor/ground layers first
            float baseDepth = 0.99f;
            float depthStep = 0.01f;

            // Draw layers that should be BEHIND entities
            for (int i = 0; i < tilemap.Layers.Count; i++)
            {
                var layer = tilemap.Layers[i];

                // Only draw floor/wall layers behind everything
                if (layer.Name == "Walls & Floor" || layer.Name.Contains("Floor"))
                {
                    float layerDepth = baseDepth - (i * depthStep);
                    TilemapRenderer.DrawLayer(_spriteBatch, tilemap, layer, layerDepth, 4f);
                }
            }

            // Build and draw sprite list (player, enemies, vases, coins)
            var drawList = new List<SpriteAnimation>();
            if (player.Animation != null)
                drawList.Add(player.Animation);
            drawList.AddRange(enemies.Select(e => e.Animation));
            drawList.AddRange(vases.Select(v => v.Animation));

            drawList = drawList
                .OrderBy(anim => anim.Position.Y + anim.FrameHeight / 2f)
                .ToList();

            foreach (var anim in drawList)
            {
                float yPosition = anim.Position.Y + anim.FrameHeight / 2f + anim.LayerDepthOffset;
                float normalizedY = yPosition / 2000f;
                float layerDepth = MathHelper.Clamp(0.9f - (normalizedY * 0.8f), 0.1f, 0.9f);

                _spriteBatch.Draw(
                    anim.GetTexture,
                    anim.Position,
                    anim.CurrentFrameRectangle,
                    anim.Color,
                    anim.Rotation,
                    anim.Origin,
                    anim.Scale,
                    anim.SpriteEffect,
                    layerDepth
                );
            }

            // Draw buttons
            foreach (var button in buttons)
            {
                float layerDepth = 0.95f;
                button.Draw(_spriteBatch, layerDepth);
            }

            // Draw potions
            foreach (var potion in potions)
            {
                float yPosition = potion.Position.Y;
                float normalizedY = yPosition / 2000f;
                float layerDepth = MathHelper.Clamp(0.9f - (normalizedY * 0.8f), 0.1f, 0.9f);
                potion.Draw(_spriteBatch, layerDepth);
            }

            // Draw pillar torches
            foreach (var torch in pillarTorches)
            {
                // The torch is 3 tiles tall (48 pixels at scale 1, or 48*4 = 192 at scale 4)
                float torchHeight = 16 * 2 * 4f; // 3 tiles * 16 pixels * scale 4
                float yPosition = torch.Position.Y + torchHeight; // Bottom of torch
                float normalizedY = yPosition / 2000f;
                float layerDepth = MathHelper.Clamp(0.9f - (normalizedY * 0.8f), 0.1f, 0.9f);
                torch.Draw(_spriteBatch, layerDepth);
            }

            // Draw chests
            foreach (var chest in chests)
            {
                float yPosition = chest.Position.Y;
                float normalizedY = yPosition / 2000f;
                float layerDepth = MathHelper.Clamp(0.9f - (normalizedY * 0.8f), 0.1f, 0.9f);
                chest.Draw(_spriteBatch, layerDepth);
            }

            // Draw layers that should be ABOVE entities with Y-based depth sorting
            for (int i = 0; i < tilemap.Layers.Count; i++)
            {
                var layer = tilemap.Layers[i];

                // Draw decoration layers that need depth sorting
                if (layer.Name == "W&F Decor" || layer.Name.Contains("Decor") || layer.Name.Contains("Above"))
                {
                    DrawLayerWithDepthSorting(_spriteBatch, tilemap, layer, 4f);
                }
            }

            // Draw other overlay layers (like waterfall) on top of everything
            for (int i = 0; i < tilemap.Layers.Count; i++)
            {
                var layer = tilemap.Layers[i];

                if (layer.Name == "Waterfall" || layer.Name.Contains("Overlay"))
                {
                    float layerDepth = 0.05f; // Very front
                    TilemapRenderer.DrawLayer(_spriteBatch, tilemap, layer, layerDepth, 4f);
                }
            }

            particleSystem.Draw(_spriteBatch, 0.05f);

            // Draw trophy interaction prompt
            if (showTrophyPrompt)
            {
                string promptText = "E";
                Vector2 textSize = instructionFont.MeasureString(promptText);

                _spriteBatch.DrawString(
                    instructionFont,
                    promptText,
                    trophyPromptPosition,
                    Color.White,
                    0f,
                    textSize / 2f,
                    2f,
                    SpriteEffects.None,
                    0.01f
                );
            }

            // Draw button interaction prompt
            if (showButtonPrompt)
            {
                string promptText = "E";
                Vector2 textSize = instructionFont.MeasureString(promptText);
                float promptScale = 1f;

                // Center the text above the item, moved higher
                Vector2 promptPosition = new Vector2(
                    buttonPromptPosition.X - (textSize.X * promptScale) / 2f,
                    buttonPromptPosition.Y - (textSize.Y * promptScale) / 2f - 10f // Added -20f to move it higher
                );

                // Draw thicker black outline
                Color outlineColor = Color.Black;
                for (int x = -2; x <= 2; x++) // Changed from -1 to 1, to -2 to 2 for thicker outline
                {
                    for (int y = -2; y <= 2; y++)
                    {
                        if (x != 0 || y != 0)
                        {
                            _spriteBatch.DrawString(
                                instructionFont,
                                promptText,
                                promptPosition + new Vector2(x, y),
                                outlineColor,
                                0f,
                                Vector2.Zero,
                                promptScale,
                                SpriteEffects.None,
                                0.01f
                            );
                        }
                    }
                }

                // Draw main white text
                _spriteBatch.DrawString(
                    instructionFont,
                    promptText,
                    promptPosition,
                    Color.White,
                    0f,
                    Vector2.Zero,
                    promptScale,
                    SpriteEffects.None,
                    0.01f
                );
            }

            _spriteBatch.End();

            // Draw UI elements in screen space (no camera)
            _spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.PointClamp,
                DepthStencilState.Default,
                RasterizerState.CullNone
            );

            // Draw instruction text
            if (instructionTimer > 0)
            {
                float alpha = MathHelper.Clamp(instructionTimer, 0f, 1f);
                Vector2 textSize = instructionFont.MeasureString(instructionText);
                Vector2 textPosition = new Vector2(
                    (ScreenManager.GraphicsDevice.Viewport.Width - textSize.X) / 2,
                    30
                );
                _spriteBatch.DrawString(
                    instructionFont,
                    instructionText,
                    textPosition,
                    Color.Black * alpha
                );
            }

            // Draw checkpoint saved message
            if (showCheckpointMessage)
            {
                string message = "Checkpoint Saved";
                Vector2 messageSize = instructionFont.MeasureString(message);
                float scale = 2f;

                // Calculate centered position accounting for scale
                Vector2 messagePosition = new Vector2(
                    (ScreenManager.GraphicsDevice.Viewport.Width - (messageSize.X * scale)) / 2,
                    ScreenManager.GraphicsDevice.Viewport.Height / 2 - 100
                );

                // Draw with outline for visibility
                Color outlineColor = Color.Black;
                Color textColor = Color.White;

                // Draw outline
                for (int x = -1; x <= 1; x++)
                {
                    for (int y = -1; y <= 1; y++)
                    {
                        if (x != 0 || y != 0)
                        {
                            _spriteBatch.DrawString(
                                instructionFont,
                                message,
                                messagePosition + new Vector2(x * 2, y * 2),
                                outlineColor,
                                0f,
                                Vector2.Zero,
                                scale,
                                SpriteEffects.None,
                                0f
                            );
                        }
                    }
                }

                // Draw main text
                _spriteBatch.DrawString(
                    instructionFont,
                    message,
                    messagePosition,
                    textColor,
                    0f,
                    Vector2.Zero,
                    scale,
                    SpriteEffects.None,
                    0f
                );
            }

            // Draw HUD
            hud.Draw(_spriteBatch, ScreenManager.GraphicsDevice.Viewport);

            // Draw death fade overlay
            if (isDying && deathFadeAlpha > 0)
            {
                _spriteBatch.Draw(
                    fadeTexture,
                    new Rectangle(0, 0,
                        ScreenManager.GraphicsDevice.Viewport.Width,
                        ScreenManager.GraphicsDevice.Viewport.Height),
                    Color.Black * deathFadeAlpha
                );
            }

            _spriteBatch.End();
        }

        private void DrawRotatedHitbox(SpriteBatch spriteBatch, RotatedRectangle rotRect, Color color)
        {
            if (pixelTexture == null)
            {
                pixelTexture = new Texture2D(ScreenManager.GraphicsDevice, 1, 1);
                pixelTexture.SetData(new[] { Color.White });
            }

            Vector2[] corners = rotRect.GetCorners();

            // Draw lines between corners
            for (int i = 0; i < 4; i++)
            {
                Vector2 start = corners[i];
                Vector2 end = corners[(i + 1) % 4];
                DrawLine(spriteBatch, start, end, color * 0.5f, 2f);
            }
        }

        private void DrawLine(SpriteBatch spriteBatch, Vector2 start, Vector2 end, Color color, float thickness)
        {
            Vector2 edge = end - start;
            float angle = (float)Math.Atan2(edge.Y, edge.X);

            spriteBatch.Draw(pixelTexture,
                new Rectangle((int)start.X, (int)start.Y, (int)edge.Length(), (int)thickness),
                null, color, angle, Vector2.Zero, SpriteEffects.None, 0);
        }

        public void SaveGame()
        {
            var destroyedVasesForSave = new Dictionary<string, List<string>>();
            foreach (var kvp in destroyedVasesPerRoom)
            {
                destroyedVasesForSave[kvp.Key] = new List<string>(kvp.Value);
            }

            var openedChestsForSave = new Dictionary<string, List<string>>();
            foreach (var kvp in openedChestsPerRoom)
            {
                openedChestsForSave[kvp.Key] = new List<string>(kvp.Value);
            }

            SaveData data = new SaveData
            {
                CoinCount = hud.CoinCount,
                CurrentHealth = hud.CurrentHealth,
                MaxHealth = hud.MaxHealth,
                CheckpointRoom = lastCheckpointRoom,
                CheckpointName = lastCheckpointName,
                CurrentRoom = currentRoom,
                MusicVolume = AudioManager.MusicVolume,
                SfxVolume = AudioManager.SFXVolume,
                DestroyedVases = destroyedVasesForSave,
                OpenedChests = openedChestsForSave
            };

            SaveData.Save(data);
            System.Diagnostics.Debug.WriteLine("Game saved!");
        }

        public void LoadGame()
        {
            SaveData data = SaveData.Load();

            if (data != null)
            {
                hud.CoinCount = data.CoinCount;
                hud.CurrentHealth = data.CurrentHealth;
                hud.MaxHealth = data.MaxHealth;

                lastCheckpointRoom = data.CheckpointRoom ?? "StartingRoom";
                lastCheckpointName = data.CheckpointName ?? "InitialSpawn";

                // Load destroyed vases
                if (data.DestroyedVases != null)
                {
                    destroyedVasesPerRoom.Clear();
                    foreach (var kvp in data.DestroyedVases)
                    {
                        destroyedVasesPerRoom[kvp.Key] = new HashSet<string>(kvp.Value);
                    }
                }

                // Load opened chests
                if (data.OpenedChests != null)
                {
                    openedChestsPerRoom.Clear();
                    foreach (var kvp in data.OpenedChests)
                    {
                        openedChestsPerRoom[kvp.Key] = new HashSet<string>(kvp.Value);
                    }
                }

                // Always load the checkpoint room to ensure proper spawning
                if (!string.IsNullOrEmpty(data.CheckpointRoom))
                {
                    LoadRoom(data.CheckpointRoom, data.CheckpointName);
                }

                AudioManager.MusicVolume = data.MusicVolume;
                AudioManager.SFXVolume = data.SfxVolume;

                System.Diagnostics.Debug.WriteLine("Game loaded!");
            }
        }

        private void LoadRoom(string roomName, string spawnPointName = null)
        {
            // Save current state before changing rooms
            if (!isDying)
            {
                SaveGame();
            }

            // Clear current room entities
            enemies.Clear();
            vases.Clear();
            coins.Clear();
            buttons.Clear();
            potions.Clear();
            pillarTorches.Clear();
            chests.Clear();
            collisionBoxes.Clear();

            // Load new tilemap
            string tmxPath = Path.Combine(_content.RootDirectory, "Rooms", $"{roomName}.tmx");
            tilemap = TmxLoader.Load(tmxPath, _content);

            // Extract collision rectangles and spawn objects from new room
            float tilemapScale = 4f;
            foreach (var objectLayer in tilemap.ObjectLayers)
            {
                if (objectLayer.Name == "Collision")
                {
                    foreach (var obj in objectLayer.Objects)
                    {
                        Rectangle collisionRect = new Rectangle(
                            (int)(obj.X * tilemapScale),
                            (int)(obj.Y * tilemapScale),
                            (int)(obj.Width * tilemapScale),
                            (int)(obj.Height * tilemapScale)
                        );
                        collisionBoxes.Add(collisionRect);
                    }
                }

                // Spawn objects in new room
                if (objectLayer.Name == "Objects")
                {
                    foreach (var obj in objectLayer.Objects)
                    {
                        if (obj.Class == "Vase")
                        {
                            Vector2 vasePos = new Vector2(obj.X * tilemapScale, obj.Y * tilemapScale);

                            // Create unique ID for this vase (room + position)
                            string vaseId = $"{obj.X}_{obj.Y}";

                            // IMPORTANT: Use roomName (the room we're loading) not currentRoom (the old room)
                            if (!destroyedVasesPerRoom.ContainsKey(roomName))
                            {
                                destroyedVasesPerRoom[roomName] = new HashSet<string>();
                            }

                            if (!destroyedVasesPerRoom[roomName].Contains(vaseId))
                            {
                                Vase vase = new Vase(vaseTexture, vasePos, 16, 8);
                                vase.VaseId = vaseId;
                                vases.Add(vase);
                                System.Diagnostics.Debug.WriteLine($"Spawned vase {vaseId} in room {roomName}");
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"Skipped destroyed vase {vaseId} in room {roomName}");
                            }
                        }
                        else if (obj.Class == "Trophy")
                        {
                            Vector2 trophyPos = new Vector2(obj.X * tilemapScale, obj.Y * tilemapScale);
                            trophy = new Trophy(ScreenManager.GraphicsDevice, trophyPos);
                        }
                        else if (obj.Class == "Skull")
                        {
                            Vector2 skullPos = new Vector2(obj.X * tilemapScale, obj.Y * tilemapScale);
                            Skull skull = new Skull(skullSheet, 10, 10, skullPos);
                            enemies.Add(skull);
                        }
                        else if (obj.Class == "Button")
                        {
                            Vector2 buttonPos = new Vector2(obj.X * tilemapScale, obj.Y * tilemapScale);
                            Button button = new Button(buttonTexture, buttonPos, 2f, obj.Name);

                            if (obj.Name == lastCheckpointName && roomName == lastCheckpointRoom)
                            {
                                button.IsCurrentCheckpoint = true;
                                button.Press();
                            }

                            buttons.Add(button);
                        }
                        else if (obj.Class == "PillarTorch")
                        {
                            Vector2 torchPos = new Vector2(obj.X * tilemapScale, obj.Y * tilemapScale);
                            PillarTorch torch = new PillarTorch(torchTilesetTexture, torchPos, tilemapScale);
                            pillarTorches.Add(torch);
                        }

                        else if (obj.Class == "Potion")
                        {
                            Vector2 potionPos = new Vector2(obj.X * tilemapScale, obj.Y * tilemapScale);

                            string potionTypeStr = obj.Name ?? "RedMini";

                            if (System.Enum.TryParse(potionTypeStr, out PotionType potionType))
                            {
                                Potion potion = new Potion(potionTexture, potionPos, potionType, 3f, true);
                                potions.Add(potion);
                            }
                        }
                        else if (obj.Class == "Chest")
                        {
                            Vector2 chestPos = new Vector2(obj.X * tilemapScale, obj.Y * tilemapScale);

                            string chestId = $"{obj.X}_{obj.Y}";

                            List<string> items = new List<string>();
                            foreach (var prop in obj.Properties)
                            {
                                string itemType = prop.Key;
                                string itemValue = prop.Value;

                                if (itemType == "Potion")
                                {
                                    items.Add($"Potion.{itemValue}");
                                }
                                else if (itemType == "Coin")
                                {
                                    items.Add("Coin");
                                }
                            }

                            Chest chest = new Chest(chestTexture, chestPos, 4f, items, chestId);

                            if (!openedChestsPerRoom.ContainsKey(roomName))
                            {
                                openedChestsPerRoom[roomName] = new HashSet<string>();
                            }

                            if (openedChestsPerRoom[roomName].Contains(chestId))
                            {
                                chest.SetOpened();
                            }

                            chests.Add(chest);
                        }
                    }
                }
            }

            // Position player at appropriate spawn point
            bool foundSpawn = false;

            // If spawnPointName is provided, look for that specific spawn (could be Checkpoint or SpawnPoint)
            if (!string.IsNullOrEmpty(spawnPointName))
            {
                foreach (var objectLayer in tilemap.ObjectLayers)
                {
                    foreach (var obj in objectLayer.Objects)
                    {
                        // Check both Checkpoint and SpawnPoint classes
                        if ((obj.Class == "SpawnPoint" || obj.Class == "Checkpoint") && obj.Name == spawnPointName)
                        {
                            player.SetX(obj.X * tilemapScale);
                            player.SetY(obj.Y * tilemapScale);
                            foundSpawn = true;
                            break;
                        }
                    }
                    if (foundSpawn) break;
                }
            }

            // If no specific spawn found and this is initial load, look for InitialSpawn
            if (!foundSpawn && lastRoomName == null)
            {
                foreach (var objectLayer in tilemap.ObjectLayers)
                {
                    foreach (var obj in objectLayer.Objects)
                    {
                        if ((obj.Class == "SpawnPoint" || obj.Class == "Checkpoint") && obj.Name == "InitialSpawn")
                        {
                            player.SetX(obj.X * tilemapScale);
                            player.SetY(obj.Y * tilemapScale);
                            foundSpawn = true;
                            break;
                        }
                    }
                    if (foundSpawn) break;
                }
            }

            // If still no spawn point, place in center of room
            if (!foundSpawn)
            {
                float centerX = (tilemap.Width * tilemap.TileWidth * tilemapScale) / 2f;
                float centerY = (tilemap.Height * tilemap.TileHeight * tilemapScale) / 2f;
                player.SetX(centerX);
                player.SetY(centerY);
            }

            lastRoomName = currentRoom;
            currentRoom = roomName;
        }

        private void RespawnPlayer()
        {
            player.State = PlayerState.Idle;
            player.Animation = player.idleAnimations[(int)player.Direction];

            hud.CurrentHealth = hud.MaxHealth;

            LoadRoom(lastCheckpointRoom, lastCheckpointName);

            System.Diagnostics.Debug.WriteLine($"Player respawned at {lastCheckpointName} in {lastCheckpointRoom}");
        }

        private void DrawLayerWithDepthSorting(SpriteBatch spriteBatch, Tilemap tilemap, TileLayer layer, float scale)
        {
            for (int y = 0; y < layer.Height; y++)
            {
                for (int x = 0; x < layer.Width; x++)
                {
                    int index = y * layer.Width + x;
                    int gid = layer.Tiles[index];

                    if (gid == 0) continue;

                    // Find the correct tileset for this gid
                    TilesetInfo tileset = null;
                    int localTileId = gid;

                    for (int i = tilemap.Tilesets.Count - 1; i >= 0; i--)
                    {
                        if (gid >= tilemap.Tilesets[i].FirstGid)
                        {
                            tileset = tilemap.Tilesets[i];
                            localTileId = gid - tileset.FirstGid;
                            break;
                        }
                    }

                    if (tileset == null || tileset.Texture == null) continue;

                    int tileX = localTileId % tileset.Columns;
                    int tileY = localTileId / tileset.Columns;

                    Rectangle sourceRect = new Rectangle(
                        tileX * tileset.TileWidth,
                        tileY * tileset.TileHeight,
                        tileset.TileWidth,
                        tileset.TileHeight
                    );

                    Vector2 position = new Vector2(x * tilemap.TileWidth * scale, y * tilemap.TileHeight * scale);

                    // Calculate depth based on Y position (bottom of tile)
                    float yPosition = position.Y + (tilemap.TileHeight * scale);
                    float normalizedY = yPosition / 2000f;
                    float layerDepth = MathHelper.Clamp(0.9f - (normalizedY * 0.8f), 0.1f, 0.9f);

                    spriteBatch.Draw(
                        tileset.Texture,
                        position,
                        sourceRect,
                        Color.White,
                        0f,
                        Vector2.Zero,
                        scale,
                        SpriteEffects.None,
                        layerDepth
                    );
                }
            }
        }

        private void SpawnItemsFromChest(List<string> items, Vector2 chestPosition)
        {
            System.Diagnostics.Debug.WriteLine($"SpawnItemsFromChest called with {items.Count} items at {chestPosition}");

            float tilemapScale = 4f;

            foreach (var item in items)
            {
                System.Diagnostics.Debug.WriteLine($"Processing item: {item}");

                // Add some random spread so items don't all spawn in exact same spot
                Random rng = new Random();
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

                    if (System.Enum.TryParse(potionTypeStr, out PotionType potionType))
                    {
                        Potion potion = new Potion(potionTexture, itemPos, potionType, 3f, true); // true = requires E to pick up
                        potions.Add(potion);
                        System.Diagnostics.Debug.WriteLine($"Successfully spawned {potionType} potion at {itemPos}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to parse potion type: {potionTypeStr}");
                    }
                }
                else if (item == "Coin")
                {
                    Coin coin = new Coin(coinTexture, itemPos, 8, 8, true);
                    coins.Add(coin);
                    System.Diagnostics.Debug.WriteLine($"Spawned coin at {itemPos}");
                }
            }
        }
    }
}
