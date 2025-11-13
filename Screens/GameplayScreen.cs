using Comora;
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

        private string currentRoom = "StartingRoom";
        private string lastRoomName = null;

        private Texture2D vaseTexture;
        private Texture2D coinTexture;
        private List<Vase> vases = new List<Vase>();
        private List<Coin> coins = new List<Coin>();

        private Trophy trophy;
        private bool showTrophyPrompt = false;
        private Vector2 trophyPromptPosition;
        private KeyboardState previousKeyboardState;
        private Matrix view3D;
        private Matrix projection3D;

        private Texture2D skullSheet;
        private List<Skull> enemies = new List<Skull>();
        private float spawnTimer = 0f;
        private float spawnInterval = 2f;

        private Tilemap tilemap;

        private List<Rectangle> collisionBoxes = new List<Rectangle>();

        private SpriteFont instructionFont;
        private float instructionTimer = 5f;
        private string instructionText = "WASD to move | SHIFT to sprint | SPACE to attack";

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

            foreach (var objectLayer in tilemap.ObjectLayers)
            {
                if (objectLayer.Name == "Objects")
                {
                    foreach (var obj in objectLayer.Objects)
                    {
                        if (obj.Class == "Vase")
                        {
                            Vector2 vasePos = new Vector2(obj.X * tilemapScale, obj.Y * tilemapScale);
                            vases.Add(new Vase(vaseTexture, vasePos, 16, 8));
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

            // spawn timer
            if (currentRoom != "TrophyRoom")
            {
                spawnTimer += dt;
                if (spawnTimer >= spawnInterval)
                {
                    spawnTimer = 0f;
                    enemies.Add(new Skull(skullSheet, 10, 10,
                        ScreenManager.GraphicsDevice.Viewport.Width,
                        ScreenManager.GraphicsDevice.Viewport.Height));
                }
            }

            // update all skulls
            foreach (var skull in enemies)
                skull.Update(gameTime, player);

            // Check for collisions between player and skulls
            RotatedRectangle playerHitbox = player.RotatedHitbox;
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                var skull = enemies[i];
                if (playerHitbox.Intersects(skull.RotatedHitbox))
                {
                    if (player.State != PlayerState.Attack1 && player.State != PlayerState.Hurt)
                    {
                        player.State = PlayerState.Hurt;
                        player.Animation = player.hurtAnimations[(int)player.Direction];
                        player.Animation.IsLooping = false;
                        player.Animation.setFrame(0);
                        hud.TakeDamage();
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
                        coins.Add(new Coin(coinTexture, vases[i].Position, 8, 8));
                        vases.RemoveAt(i);
                    }
                }
            }

            // Check player collecting coins
            for (int i = coins.Count - 1; i >= 0; i--)
            {
                if (player.Hitbox.Intersects(coins[i].Hitbox))
                {
                    coins[i].IsCollected = true;
                    hud.AddCoin();
                    coins.RemoveAt(i);
                }
            }

            // Check door collisions for room transitions
            float tilemapScale = 4f;
            Rectangle currentPlayerHitbox = player.Hitbox;

            KeyboardState currentKeyboardState = Keyboard.GetState();

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
                }
            }

            particleSystem.Update(gameTime);
            player.Update(gameTime, enemies, particleSystem, collisionBoxes);
            hud.Update(gameTime);

            if (trophy != null)
                trophy.Update(gameTime);

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

            // Draw tilemap layers
            float baseDepth = 0.99f;
            float depthStep = 0.01f;
            for (int i = 0; i < tilemap.Layers.Count; i++)
            {
                float layerDepth = baseDepth - (i * depthStep);
                TilemapRenderer.DrawLayer(_spriteBatch, tilemap, tilemap.Layers[i], layerDepth, 4f);
            }

            // Build and draw sprite list
            var drawList = new List<SpriteAnimation>();
            if (player.Animation != null)
                drawList.Add(player.Animation);
            drawList.AddRange(enemies.Select(e => e.Animation));
            drawList.AddRange(vases.Select(v => v.Animation));
            drawList.AddRange(coins.Select(c => c.Animation));

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

            particleSystem.Draw(_spriteBatch, 0.05f);

            // Draw trophy interaction prompt IN WORLD SPACE
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

            // Draw HUD
            hud.Draw(_spriteBatch, ScreenManager.GraphicsDevice.Viewport);

            _spriteBatch.End();
        }

        private Texture2D pixelTexture;

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
            SaveData data = new SaveData
            {
                CoinCount = hud.CoinCount,
                CurrentHealth = hud.CurrentHealth,
                MaxHealth = hud.MaxHealth,
                PlayerX = player.Position.X,
                PlayerY = player.Position.Y,
                CurrentRoom = currentRoom,
                MusicVolume = AudioManager.MusicVolume,
                SfxVolume = AudioManager.SFXVolume
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

                // Load the saved room if it's different from current
                if (!string.IsNullOrEmpty(data.CurrentRoom) && data.CurrentRoom != currentRoom)
                {
                    LoadRoom(data.CurrentRoom);
                }

                player.SetX(data.PlayerX);
                player.SetY(data.PlayerY);
                AudioManager.MusicVolume = data.MusicVolume;
                AudioManager.SFXVolume = data.SfxVolume;

                System.Diagnostics.Debug.WriteLine("Game loaded!");
            }
        }

        private void LoadRoom(string roomName, string spawnPointName = null)
        {
            // Save current state before changing rooms
            SaveGame();

            // Clear current room entities
            enemies.Clear();
            vases.Clear();
            coins.Clear();
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

                // Spawn vases in new room
                if (objectLayer.Name == "Objects")
                {
                    foreach (var obj in objectLayer.Objects)
                    {
                        if (obj.Class == "Vase")
                        {
                            Vector2 vasePos = new Vector2(obj.X * tilemapScale, obj.Y * tilemapScale);
                            vases.Add(new Vase(vaseTexture, vasePos, 16, 8));
                        }
                        else if (obj.Class == "Trophy")
                        {
                            Vector2 trophyPos = new Vector2(obj.X * tilemapScale, obj.Y * tilemapScale);
                            trophy = new Trophy(ScreenManager.GraphicsDevice, trophyPos);
                        }
                    }
                }
            }

            // Position player at appropriate spawn point
            bool foundSpawn = false;

            // If spawnPointName is provided, look for that specific spawn
            if (!string.IsNullOrEmpty(spawnPointName))
            {
                foreach (var objectLayer in tilemap.ObjectLayers)
                {
                    foreach (var obj in objectLayer.Objects)
                    {
                        if (obj.Class == "SpawnPoint" && obj.Name == spawnPointName)
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
                        if (obj.Class == "SpawnPoint" && obj.Name == "InitialSpawn")
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
    }
}
