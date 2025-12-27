using Comora;
using GameProject2.Content.Player;
using GameProject2.Enemies;
using GameProject2.Graphics3D;
using GameProject2.Managers;
using GameProject2.SaveSystem;
using GameProject2.StateManagement;
using GameProject2.Tilemaps;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GameProject2.Screens
{
    public class GameplayScreen : GameScreen
    {
        private ContentManager _content;
        private SpriteBatch _spriteBatch;

        private Player player;
        private PlayerHUD hud;
        private BossHealthBar bossHealthBar;
        private Action onDashUsedHandler;
        private Action onAttack2UsedHandler;

        private Camera camera;
        private ParticleSystem particleSystem;
        private Texture2D pixelTexture;

        // Per-room persistent state (owned here, passed to managers)
        private Dictionary<string, HashSet<string>> destroyedVasesPerRoom = new Dictionary<string, HashSet<string>>();
        private Dictionary<string, HashSet<string>> openedChestsPerRoom = new Dictionary<string, HashSet<string>>();
        private Dictionary<string, HashSet<string>> destroyedCratesPerRoom = new Dictionary<string, HashSet<string>>();
        private HashSet<string> activatedTotems = new HashSet<string>();
        private HashSet<string> completedGauntlets = new HashSet<string>();

        // Unlock overlay state
        private bool showUnlockOverlay = false;
        private string unlockTitle = "";
        private string unlockDescription = "";
        private Totem pendingTotemDrop = null;

        // Victory overlay state
        private bool showVictoryOverlay = false;
        private bool bossDefeated = false;
        private bool bossDeathAnimationComplete = false;
        private bool victoryShown = false;
        private float victoryDelayTimer = 0f;
        private const float VictoryDelayDuration = 1.0f;

        // Light rays fade effect
        private float lightRaysFadeProgress = 0f;
        private bool lightRaysFadeStarted = false;
        private bool lightRaysFadeComplete = false;
        private const float LightRaysFadeDuration = 3.0f;

        // Persistent boss defeated state (saved to file)
        private bool orcKingDefeated = false;
        private bool orcKingDefeatedFromSave = false; // True only if loaded from save file

        private KeyboardState previousKeyboardState;
        private Matrix view3D;
        private Matrix projection3D;

        private SpriteFont instructionFont;
        private float instructionTimer = 8f;
        private string instructionText = "WASD to move | SHIFT to sprint | LEFT CLICK to attack";

        // Death state
        private bool isDying = false;
        private float deathFadeTimer = 0f;
        private float deathFadeDuration = 3f;
        private float deathFadeAlpha = 0f;
        private Texture2D fadeTexture;

        // Demo message (kept for compatibility)
        private bool showDemoMessage = false;
        private float demoMessageTimer = 0f;
        private float demoMessageDuration = 5f;

        // Darkness overlay for MazeRoom
        private Texture2D darknessTexture;
        private float darknessRadius = 40f; // Radius of light around player (very tight)

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

            // Create darkness texture for MazeRoom
            CreateDarknessTexture();

            AudioManager.PlayGameplayMusicWithIntro();

            // Clear any stale state from previous game sessions
            System.Diagnostics.Debug.WriteLine("GameplayScreen.Activate: Clearing stale state...");
            EntityManager.ClearAll();
            GauntletManager.ClearAll();
            destroyedVasesPerRoom.Clear();
            openedChestsPerRoom.Clear();
            activatedTotems.Clear();
            completedGauntlets.Clear();

            // Initialize managers
            System.Diagnostics.Debug.WriteLine("GameplayScreen.Activate: Loading EntityManager content...");
            EntityManager.LoadContent(_content);
            System.Diagnostics.Debug.WriteLine("GameplayScreen.Activate: Loading StartingRoom...");
            RoomManager.LoadRoom("StartingRoom", _content);
            System.Diagnostics.Debug.WriteLine($"GameplayScreen.Activate: CurrentTilemap is {(RoomManager.CurrentTilemap != null ? "loaded" : "NULL")}");
            CheckpointManager.Initialize();

            // Create player and load animations
            player = new Player();
            LoadPlayerAnimations();

            // Set initial player position immediately to avoid flicker
            var initialSpawn = RoomManager.FindSpawnPoint("InitialSpawn") ?? RoomManager.GetRoomCenter();
            player.SetX(initialSpawn.X);
            player.SetY(initialSpawn.Y);

            hud = new PlayerHUD();
            hud.LoadContent(_content);

            // Subscribe to player ability events for HUD cooldown triggers
            onDashUsedHandler = () => hud.TriggerDashCooldown();
            onAttack2UsedHandler = () => hud.TriggerAttack2Cooldown();
            player.OnDashUsed += onDashUsedHandler;
            player.OnAttack2Used += onAttack2UsedHandler;

            // Subscribe to potion usage events
            player.OnRedPotionUsed += HandleRedPotionUsed;
            player.OnRedMiniPotionUsed += HandleRedMiniPotionUsed;
            player.OnHealAnimationComplete += HandleHealAnimationComplete;

            // Subscribe to water falling event
            player.OnFellInWater += HandleFellInWater;

            camera = new Camera(ScreenManager.GraphicsDevice);
            particleSystem = new ParticleSystem(ScreenManager.GraphicsDevice);

            instructionFont = _content.Load<SpriteFont>("InstructionFont");

            bossHealthBar = new BossHealthBar(ScreenManager.GraphicsDevice, instructionFont, "Orc King");

            // Spawn entities for starting room
            EntityManager.SpawnFromTilemap(
                RoomManager.CurrentTilemap,
                RoomManager.CurrentRoom,
                RoomManager.TilemapScale,
                destroyedVasesPerRoom,
                openedChestsPerRoom,
                destroyedCratesPerRoom,
                activatedTotems,
                CheckpointManager.LastCheckpointName,
                CheckpointManager.LastCheckpointRoom,
                ScreenManager.GraphicsDevice,
                orcKingDefeated);

            // Spawn gauntlet entities for starting room
            GauntletManager.SpawnFromTilemap(
                RoomManager.CurrentTilemap,
                RoomManager.CurrentRoom,
                RoomManager.TilemapScale,
                completedGauntlets,
                orcKingDefeated);

            // Setup 3D projection
            projection3D = Matrix.CreatePerspectiveFieldOfView(
                MathHelper.PiOver4,
                ScreenManager.GraphicsDevice.Viewport.AspectRatio,
                0.1f,
                1000f
            );

            // Subscribe to interaction events
            InteractionSystem.OnCheckpointActivated += HandleCheckpointActivated;
            InteractionSystem.OnTrophyInteracted += HandleTrophyInteracted;

            // Load saved game (will override spawn position if save exists)
            LoadGame();
        }

        private void LoadPlayerAnimations()
        {
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

            var attack2Down = _content.Load<Texture2D>("Player/Sprites/ATTACK 2/attack2_down");
            var attack2Up = _content.Load<Texture2D>("Player/Sprites/ATTACK 2/attack2_up");
            var attack2Left = _content.Load<Texture2D>("Player/Sprites/ATTACK 2/attack2_left");
            var attack2Right = _content.Load<Texture2D>("Player/Sprites/ATTACK 2/attack2_right");

            var dashDown = _content.Load<Texture2D>("Player/Sprites/DASH/dash_down");
            var dashUp = _content.Load<Texture2D>("Player/Sprites/DASH/dash_up");
            var dashLeft = _content.Load<Texture2D>("Player/Sprites/DASH/dash_left");
            var dashRight = _content.Load<Texture2D>("Player/Sprites/DASH/dash_right");

            var healDown = _content.Load<Texture2D>("Player/Sprites/HEAL/heal_down");
            var healUp = _content.Load<Texture2D>("Player/Sprites/HEAL/heal_up");
            var healLeft = _content.Load<Texture2D>("Player/Sprites/HEAL/heal_left");
            var healRight = _content.Load<Texture2D>("Player/Sprites/HEAL/heal_right");

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
            animations.Add(player.hurtAnimations);

            player.deathAnimations[0] = new SpriteAnimation(deathDown, 7, 7);
            player.deathAnimations[1] = new SpriteAnimation(deathUp, 7, 7);
            player.deathAnimations[2] = new SpriteAnimation(deathLeft, 7, 7);
            player.deathAnimations[3] = new SpriteAnimation(deathRight, 7, 7);
            animations.Add(player.deathAnimations);

            player.attack2Animations[0] = new SpriteAnimation(attack2Down, 8, 12);
            player.attack2Animations[1] = new SpriteAnimation(attack2Up, 8, 12);
            player.attack2Animations[2] = new SpriteAnimation(attack2Left, 8, 12);
            player.attack2Animations[3] = new SpriteAnimation(attack2Right, 8, 12);
            // Wind-up on frame 0: hold 3x longer, then normal speed for rest
            float[] attack2FrameDurations = { 3f, 1f, 1f, 1f, 1f, 1f, 1f, 1f };
            foreach (var anim in player.attack2Animations)
            {
                anim.SetFrameDurations(attack2FrameDurations);
            }
            animations.Add(player.attack2Animations);

            player.dashAnimations[0] = new SpriteAnimation(dashDown, 7, 20);
            player.dashAnimations[1] = new SpriteAnimation(dashUp, 7, 20);
            player.dashAnimations[2] = new SpriteAnimation(dashLeft, 7, 20);
            player.dashAnimations[3] = new SpriteAnimation(dashRight, 7, 20);
            animations.Add(player.dashAnimations);

            player.healAnimations[0] = new SpriteAnimation(healDown, 12, 10);
            player.healAnimations[1] = new SpriteAnimation(healUp, 12, 10);
            player.healAnimations[2] = new SpriteAnimation(healLeft, 12, 10);
            player.healAnimations[3] = new SpriteAnimation(healRight, 12, 10);
            animations.Add(player.healAnimations);

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
        }

        private void HandleCheckpointActivated(string room, string checkpointName)
        {
            CheckpointManager.SetCheckpoint(room, checkpointName);
        }

        private void HandleTrophyInteracted()
        {
            ScreenManager.AddScreen(new TrophyScreen(), ControllingPlayer);
        }

        private void HandleRedPotionUsed()
        {
            int healAmount = hud.UseRedPotion();
            if (healAmount > 0)
            {
                // If animation can't start (e.g., already healing), refund the potion
                if (player.TriggerHealAnimation(healAmount))
                {
                    AudioManager.PlayHealSound(0.5f);
                }
                else
                {
                    hud.AddRedPotion();
                }
            }
        }

        private void HandleRedMiniPotionUsed()
        {
            int healAmount = hud.UseRedMiniPotion();
            if (healAmount > 0)
            {
                // If animation can't start (e.g., already healing), refund the potion
                if (player.TriggerHealAnimation(healAmount))
                {
                    AudioManager.PlayHealSound(0.5f);
                }
                else
                {
                    hud.AddRedMiniPotion();
                }
            }
        }

        private void HandleHealAnimationComplete(int healAmount)
        {
            hud.Heal(healAmount);
        }

        private void HandleFellInWater()
        {
            // Take 1 heart of damage
            hud.TakeDamage();
            AudioManager.PlayPlayerTakeDamageSound(0.5f);

            if (hud.CurrentHealth <= 0)
            {
                // Player died from water
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
                // Respawn at room's spawn point
                var spawnPos = RoomManager.FindSpawnPoint($"From{RoomManager.LastRoom}")
                    ?? RoomManager.FindSpawnPoint("InitialSpawn")
                    ?? RoomManager.GetRoomCenter();

                player.SetX(spawnPos.X);
                player.SetY(spawnPos.Y);
                player.State = PlayerState.Idle;
                player.Animation = player.idleAnimations[(int)player.Direction];
            }
        }

        public override void Unload()
        {
            // Unsubscribe from events
            InteractionSystem.OnCheckpointActivated -= HandleCheckpointActivated;
            InteractionSystem.OnTrophyInteracted -= HandleTrophyInteracted;
            InteractionSystem.ClearEvents();

            // Unsubscribe from player events
            player.OnDashUsed -= onDashUsedHandler;
            player.OnAttack2Used -= onAttack2UsedHandler;
            player.OnRedPotionUsed -= HandleRedPotionUsed;
            player.OnRedMiniPotionUsed -= HandleRedMiniPotionUsed;
            player.OnHealAnimationComplete -= HandleHealAnimationComplete;
            player.OnFellInWater -= HandleFellInWater;

            SaveGame();
            _content.Unload();
        }

        public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
        {
            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);

            if (otherScreenHasFocus)
            {
                previousKeyboardState = Keyboard.GetState();
                return;
            }

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            KeyboardState currentKeyboardState = Keyboard.GetState();

            if (instructionTimer > 0)
                instructionTimer -= dt;

            if (showDemoMessage)
            {
                demoMessageTimer -= dt;
                if (demoMessageTimer <= 0)
                    showDemoMessage = false;
            }

            // Handle unlock overlay (pauses gameplay)
            if (showUnlockOverlay)
            {
                if (currentKeyboardState.IsKeyDown(Keys.E) && previousKeyboardState.IsKeyUp(Keys.E))
                {
                    showUnlockOverlay = false;

                    // Trigger the totem drop animation
                    if (pendingTotemDrop != null)
                    {
                        pendingTotemDrop.TriggerDrop();
                        pendingTotemDrop = null;
                    }
                }

                previousKeyboardState = currentKeyboardState;
                return; // Pause all other updates
            }

            // Handle victory overlay (pauses gameplay)
            if (showVictoryOverlay)
            {
                if (currentKeyboardState.IsKeyDown(Keys.E) && previousKeyboardState.IsKeyUp(Keys.E))
                {
                    showVictoryOverlay = false;
                    AudioManager.PlayGameplayMusic();

                    // Open the boss room doors
                    foreach (var door in GauntletManager.Doors)
                    {
                        door.Open();
                    }
                    if (GauntletManager.Doors.Count > 0)
                    {
                        AudioManager.PlayWoodDoorOpenSound(0.5f);
                    }
                }

                previousKeyboardState = currentKeyboardState;
                return; // Pause all other updates
            }

            // Update managers
            CheckpointManager.Update(dt);

            // Filter collision boxes (removes boss door collider when boss is defeated)
            var collisionBoxes = GauntletManager.FilterCollisionBoxes(RoomManager.CollisionBoxes, orcKingDefeated);

            EntityManager.Update(gameTime, player, collisionBoxes);
            GauntletManager.Update(gameTime, player, collisionBoxes);

            // Boss fight music handling
            if (EntityManager.OrcBoss != null && !EntityManager.OrcBoss.IsDead)
            {
                if (!AudioManager.IsBossFightMusicPlaying())
                {
                    AudioManager.PlayBossFightMusic();
                }
            }
            else if (AudioManager.IsBossFightMusicPlaying())
            {
                // Boss died, stop music and wait for death animation
                AudioManager.StopMusic();
                bossDefeated = true;
                orcKingDefeated = true;
                SaveGame();
            }

            // Wait for boss death animation to complete, then start light rays fade
            if (bossDefeated && !victoryShown)
            {
                if (EntityManager.OrcBoss != null && EntityManager.OrcBoss.IsDeathAnimationComplete)
                {
                    if (!bossDeathAnimationComplete)
                    {
                        bossDeathAnimationComplete = true;
                        victoryDelayTimer = 0f;
                    }
                    else if (!lightRaysFadeStarted)
                    {
                        victoryDelayTimer += dt;
                        if (victoryDelayTimer >= VictoryDelayDuration)
                        {
                            // Start the light rays fade
                            lightRaysFadeStarted = true;
                            lightRaysFadeProgress = 0f;
                        }
                    }
                    else if (!lightRaysFadeComplete)
                    {
                        // Update light rays fade
                        lightRaysFadeProgress += dt / LightRaysFadeDuration;
                        if (lightRaysFadeProgress >= 1f)
                        {
                            lightRaysFadeProgress = 1f;
                            lightRaysFadeComplete = true;
                        }
                    }
                    else
                    {
                        // Light rays complete, show victory immediately
                        AudioManager.PlayBossFightVictorySound(0.2f);
                        showVictoryOverlay = true;
                        victoryShown = true;
                    }
                }
            }

            // Check if gauntlet was just completed
            if (GauntletManager.IsGauntletCompleted() && !completedGauntlets.Contains(RoomManager.CurrentRoom))
            {
                completedGauntlets.Add(RoomManager.CurrentRoom);
                SaveGame();
            }

            // Handle interactions (coins, potions, buttons, chests, trophy)
            InteractionSystem.Update(
                gameTime,
                player,
                hud,
                currentKeyboardState,
                previousKeyboardState,
                RoomManager.CurrentRoom,
                destroyedVasesPerRoom,
                openedChestsPerRoom);

            // Check for totem trigger interaction (after InteractionSystem so ShowPrompt isn't overwritten)
            CheckTotemTrigger(currentKeyboardState, previousKeyboardState);

            // Combat: check player vs enemies
            HandleEnemyCollisions();

            // Combat: player attacking vases
            HandlePlayerAttackingVases();
            HandlePlayerAttackingCrates();

            // Check door transitions
            CheckDoorTransitions();

            // Check checkpoint zones (passive checkpoint areas in tilemap)
            CheckCheckpointZones();

            // Update particles, player, HUD
            particleSystem.Update(gameTime);

            // Rebuild collision boxes with current crate state (after crates may have been destroyed)
            var playerCollisionBoxes = new List<Rectangle>(GauntletManager.FilterCollisionBoxes(RoomManager.CollisionBoxes, orcKingDefeated));
            playerCollisionBoxes.AddRange(EntityManager.GetCrateCollisionBoxes());

            player.Update(gameTime, EntityManager.GetEnemiesMutable(), particleSystem, playerCollisionBoxes, RoomManager.WaterBoxes);

            // Sync HUD ability unlock states with player
            hud.HasAttack2 = player.HasAttack2;
            hud.HasDash = player.HasDash;

            // Sync active states for held abilities
            hud.SetAttack1Active(player.State == PlayerState.Attack1);
            hud.SetSprintActive(player.State == PlayerState.Run);

            hud.Update(gameTime);

            if (EntityManager.Trophy != null)
                EntityManager.Trophy.Update(gameTime);

            // Handle death sequence
            if (isDying)
            {
                deathFadeTimer += dt;
                deathFadeAlpha = MathHelper.Clamp(deathFadeTimer / deathFadeDuration, 0f, 1f);

                if (deathFadeTimer >= deathFadeDuration)
                {
                    RespawnPlayer();
                    isDying = false;
                    deathFadeTimer = 0f;
                    deathFadeAlpha = 0f;
                }

                previousKeyboardState = currentKeyboardState;
                return;
            }

            // Update 3D view to match camera
            view3D = Matrix.CreateLookAt(
                new Vector3(camera.Position.X, 10f, camera.Position.Y + 15f),
                new Vector3(camera.Position.X, 0f, camera.Position.Y),
                Vector3.Up
            );

            // Update camera position (clamped to tilemap bounds)
            Vector2 tilemapSize = RoomManager.GetTilemapSize();
            float halfScreenWidth = ScreenManager.GraphicsDevice.Viewport.Width / 2f;
            float halfScreenHeight = ScreenManager.GraphicsDevice.Viewport.Height / 2f;

            camera.Position = new Vector2(
                MathHelper.Clamp(player.Position.X, halfScreenWidth, tilemapSize.X - halfScreenWidth),
                MathHelper.Clamp(player.Position.Y, halfScreenHeight, tilemapSize.Y - halfScreenHeight)
            );

            previousKeyboardState = currentKeyboardState;
            camera.Update(gameTime);
        }

        private void HandleEnemyCollisions()
        {
            RotatedRectangle playerHitbox = player.RotatedHitbox;

            foreach (var enemy in EntityManager.Enemies)
            {
                // Skip dead or dying enemies
                if (enemy.IsDead)
                    continue;

                // Special handling for OrcBoss - only takes damage from boss attacks, not contact
                if (enemy is OrcBoss boss)
                {
                    HandleBossAttackDamage(boss);
                    continue;
                }

                if (playerHitbox.Intersects(enemy.RotatedHitbox))
                {
                    if (player.State != PlayerState.Attack1 && player.State != PlayerState.Attack2 &&
                        player.State != PlayerState.Hurt && player.State != PlayerState.Death &&
                        player.State != PlayerState.Dash)
                    {
                        hud.TakeDamage();

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
                            AudioManager.PlayPlayerTakeDamageSound(0.5f);
                        }

                        // Enemy dies on contact with player (suicide attack)
                        enemy.TakeDamage(enemy.MaxHealth);
                        if (enemy.ShowDeathParticles)
                        {
                            particleSystem.CreateSkullDeathEffect(enemy.Position);
                        }
                        break; // Only handle one collision per frame
                    }
                }
            }
        }

        private void HandleBossAttackDamage(OrcBoss boss)
        {
            // Only deal damage if boss is in attack frames and hasn't already hit this attack
            if (!boss.IsAttackingAndInDamageFrames())
                return;

            // Check if player can take damage
            if (player.State == PlayerState.Hurt || player.State == PlayerState.Death || player.State == PlayerState.Dash)
                return;

            Rectangle attackHitbox = boss.GetCurrentAttackHitbox();

            if (attackHitbox.Intersects(player.Hitbox))
            {
                boss.MarkDamageDealt();
                hud.TakeDamage();

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
                    AudioManager.PlayPlayerTakeDamageSound(0.5f);
                }
            }
        }

        private void HandlePlayerAttackingVases()
        {
            if (player.State != PlayerState.Attack1) return;

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

            foreach (var vase in EntityManager.GetVasesMutable())
            {
                if (!vase.IsDestroyed && attackHitbox.Intersects(vase.Hitbox))
                {
                    vase.IsDestroyed = true;
                    InteractionSystem.HandleVaseDestroyed(vase, RoomManager.CurrentRoom, destroyedVasesPerRoom);
                }
            }
        }

        // Track crates hit this attack to prevent multi-hit
        private HashSet<Crate> cratesHitThisAttack = new HashSet<Crate>();

        private void HandlePlayerAttackingCrates()
        {
            // Crates can ONLY be broken by Attack2
            if (player.State != PlayerState.Attack2)
            {
                cratesHitThisAttack.Clear();
                return;
            }

            // Attack2 has wider range
            Rectangle attackHitbox = player.Hitbox;
            int verticalRange = 60;
            int horizontalRange = 140;

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

            // Only check during active attack frames (2-5)
            int currentFrame = player.Animation.CurrentFrameIndex;
            if (currentFrame < 2 || currentFrame > 5) return;

            foreach (var crate in EntityManager.Crates)
            {
                if (crate.IsDestroyed || cratesHitThisAttack.Contains(crate))
                    continue;

                if (attackHitbox.Intersects(crate.Hitbox))
                {
                    cratesHitThisAttack.Add(crate);
                    bool destroyed = crate.TakeDamage(1);

                    if (destroyed)
                    {
                        // Large particle effect for destruction
                        particleSystem.CreateWoodSplinterEffect(crate.Position, true);
                        AudioManager.PlaySwingSwordSound(0.2f, 0.5f); // Higher pitch for breaking sound

                        // Track destroyed crate for save system
                        if (!destroyedCratesPerRoom.ContainsKey(RoomManager.CurrentRoom))
                            destroyedCratesPerRoom[RoomManager.CurrentRoom] = new HashSet<string>();
                        destroyedCratesPerRoom[RoomManager.CurrentRoom].Add(crate.CrateId);

                        SaveGame();
                    }
                    else
                    {
                        // Small particle effect for hit
                        particleSystem.CreateWoodSplinterEffect(crate.Position, false);
                    }
                }
            }
        }

        private void CheckDoorTransitions()
        {
            var door = RoomManager.FindDoorAtPosition(player.Hitbox);
            if (door != null)
            {
                string nextRoom = door.Name;
                LoadRoom(nextRoom, $"From{RoomManager.CurrentRoom}");
            }
        }

        private void CheckCheckpointZones()
        {
            var checkpoint = RoomManager.FindCheckpointAtPosition(player.Hitbox);
            if (checkpoint != null)
            {
                // Passive checkpoint update (for checkpoint zones, not buttons)
                if (CheckpointManager.LastCheckpointName != checkpoint.Name ||
                    CheckpointManager.LastCheckpointRoom != RoomManager.CurrentRoom)
                {
                    // Only update if different from current (don't show message for passive checkpoints)
                    CheckpointManager.SetCheckpointSilent(RoomManager.CurrentRoom, checkpoint.Name);
                }
            }
        }

        private void CheckTotemTrigger(KeyboardState current, KeyboardState previous)
        {
            // Check if player crosses the entrance trigger (starts idle animation)
            var triggerObj = RoomManager.FindTotemTriggerAtPosition(player.Hitbox);
            if (triggerObj != null)
            {
                // Find a non-activated totem and trigger its animation
                foreach (var totem in EntityManager.Totems)
                {
                    if (!activatedTotems.Contains(totem.TotemId) && totem.State == TotemState.Waiting)
                    {
                        totem.TriggerActivation();
                    }
                }
            }

            // Check if player is near a totem to interact with E
            foreach (var totem in EntityManager.Totems)
            {
                if (activatedTotems.Contains(totem.TotemId)) continue;
                if (totem.State != TotemState.Idle) continue; // Must have been activated by trigger first

                // Check if player is near this totem
                Rectangle totemInteractZone = new Rectangle(
                    (int)(totem.Position.X - 40),
                    (int)(totem.Position.Y - 40),
                    (int)(16 * RoomManager.TilemapScale + 80),
                    (int)(32 * RoomManager.TilemapScale + 80)
                );

                if (!player.Hitbox.Intersects(totemInteractZone)) continue;

                // Show E prompt above the totem
                InteractionSystem.ShowPrompt = true;
                InteractionSystem.PromptPosition = new Vector2(
                    totem.Position.X + 8,
                    totem.Position.Y - 20);

                // Check for E key press
                if (current.IsKeyDown(Keys.E) && previous.IsKeyUp(Keys.E))
                {
                    // Determine what ability this totem unlocks
                    string abilityName = totem.AbilityName;

                    if (abilityName.Contains("Dash") || abilityName.ToLower().Contains("dash"))
                    {
                        player.HasDash = true;
                        unlockTitle = "You have unlocked DASH";
                        unlockDescription = "Press SPACE to make a quick dash in the\ndirection you are facing.\n\nPress E to close.";
                    }
                    else if (abilityName.Contains("Attack2") || abilityName.ToLower().Contains("attack"))
                    {
                        player.HasAttack2 = true;
                        unlockTitle = "You have unlocked HEAVY ATTACK";
                        unlockDescription = "Right Click to perform a powerful sweeping attack\nwith greater range.\n\nPress E to close.";
                    }

                    // Mark as activated
                    activatedTotems.Add(totem.TotemId);
                    pendingTotemDrop = totem;
                    showUnlockOverlay = true;
                    AudioManager.PlayTotemAbilityUnlockSound(0.8f);

                    // Save immediately
                    SaveGame();
                }

                break; // Only interact with one totem at a time
            }
        }

        private void LoadRoom(string roomName, string spawnPointName = null)
        {
            if (!isDying)
                SaveGame();

            EntityManager.ClearAll();
            GauntletManager.ClearAll();
            RoomManager.LoadRoom(roomName, _content);

            // Handle room-specific music
            if (roomName == "DashGauntlet")
            {
                // Stop music when entering DashGauntlet (will start when gauntlet triggers)
                AudioManager.StopMusic();
            }
            else if (AudioManager.IsDashGauntletMusicPlaying())
            {
                // Resume normal gameplay music when leaving DashGauntlet
                AudioManager.PlayGameplayMusic();
            }

            EntityManager.SpawnFromTilemap(
                RoomManager.CurrentTilemap,
                RoomManager.CurrentRoom,
                RoomManager.TilemapScale,
                destroyedVasesPerRoom,
                openedChestsPerRoom,
                destroyedCratesPerRoom,
                activatedTotems,
                CheckpointManager.LastCheckpointName,
                CheckpointManager.LastCheckpointRoom,
                ScreenManager.GraphicsDevice,
                orcKingDefeated);

            // Spawn gauntlet entities (spawner totems, doors)
            GauntletManager.SpawnFromTilemap(
                RoomManager.CurrentTilemap,
                RoomManager.CurrentRoom,
                RoomManager.TilemapScale,
                completedGauntlets,
                orcKingDefeated);

            // Position player at spawn point
            var spawnPos = RoomManager.FindSpawnPoint(spawnPointName)
                ?? RoomManager.FindSpawnPoint("InitialSpawn")
                ?? RoomManager.GetRoomCenter();

            player.SetX(spawnPos.X);
            player.SetY(spawnPos.Y);
        }

        private void RespawnPlayer()
        {
            player.State = PlayerState.Idle;
            player.Animation = player.idleAnimations[(int)player.Direction];

            hud.CurrentHealth = hud.MaxHealth;

            // Stop boss fight music before loading room to prevent false "boss defeated" trigger
            // (LoadRoom clears EntityManager, which would set OrcBoss to null while music is still playing)
            if (AudioManager.IsBossFightMusicPlaying())
            {
                AudioManager.StopMusic();
            }

            LoadRoom(CheckpointManager.LastCheckpointRoom, CheckpointManager.LastCheckpointName);

            System.Diagnostics.Debug.WriteLine($"Player respawned at {CheckpointManager.LastCheckpointName} in {CheckpointManager.LastCheckpointRoom}");
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

            var destroyedCratesForSave = new Dictionary<string, List<string>>();
            foreach (var kvp in destroyedCratesPerRoom)
            {
                destroyedCratesForSave[kvp.Key] = new List<string>(kvp.Value);
            }

            SaveData data = new SaveData
            {
                CoinCount = hud.CoinCount,
                CurrentHealth = hud.CurrentHealth,
                MaxHealth = hud.MaxHealth,
                CheckpointRoom = CheckpointManager.LastCheckpointRoom,
                CheckpointName = CheckpointManager.LastCheckpointName,
                CurrentRoom = RoomManager.CurrentRoom,
                MusicVolume = AudioManager.MusicVolume,
                SfxVolume = AudioManager.SFXVolume,
                DestroyedVases = destroyedVasesForSave,
                OpenedChests = openedChestsForSave,
                DestroyedCrates = destroyedCratesForSave,
                HasAttack2 = player.HasAttack2,
                HasDash = player.HasDash,
                ActivatedTotems = new List<string>(activatedTotems),
                CompletedGauntlets = new List<string>(completedGauntlets),
                RedPotionCount = hud.RedPotionCount,
                RedMiniPotionCount = hud.RedMiniPotionCount,
                OrcKingDefeated = orcKingDefeated
            };

            SaveData.Save(data);
            System.Diagnostics.Debug.WriteLine("Game saved!");
        }

        public void LoadGame()
        {
            System.Diagnostics.Debug.WriteLine("GameplayScreen.LoadGame: Attempting to load save...");
            SaveData data = SaveData.Load();

            System.Diagnostics.Debug.WriteLine($"GameplayScreen.LoadGame: SaveData is {(data != null ? "found" : "NULL (no save file)")}");

            if (data != null)
            {
                System.Diagnostics.Debug.WriteLine($"GameplayScreen.LoadGame: Loading checkpoint room '{data.CheckpointRoom}'");
                hud.CoinCount = data.CoinCount;
                hud.CurrentHealth = data.CurrentHealth;
                hud.MaxHealth = data.MaxHealth;
                hud.RedPotionCount = data.RedPotionCount;
                hud.RedMiniPotionCount = data.RedMiniPotionCount;

                CheckpointManager.LoadFromSave(data);

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

                // Load destroyed crates
                if (data.DestroyedCrates != null)
                {
                    destroyedCratesPerRoom.Clear();
                    foreach (var kvp in data.DestroyedCrates)
                    {
                        destroyedCratesPerRoom[kvp.Key] = new HashSet<string>(kvp.Value);
                    }
                }

                // Load the checkpoint room
                if (!string.IsNullOrEmpty(data.CheckpointRoom))
                {
                    LoadRoom(data.CheckpointRoom, data.CheckpointName);
                }

                AudioManager.MusicVolume = data.MusicVolume;
                AudioManager.SFXVolume = data.SfxVolume;

                // Load ability unlocks
                player.HasAttack2 = data.HasAttack2;
                player.HasDash = data.HasDash;

                // Load activated totems
                if (data.ActivatedTotems != null)
                {
                    activatedTotems.Clear();
                    foreach (var totemId in data.ActivatedTotems)
                    {
                        activatedTotems.Add(totemId);
                    }
                }

                // Load completed gauntlets
                if (data.CompletedGauntlets != null)
                {
                    completedGauntlets.Clear();
                    foreach (var roomName in data.CompletedGauntlets)
                    {
                        completedGauntlets.Add(roomName);
                    }
                }

                // Load boss defeated state
                orcKingDefeated = data.OrcKingDefeated;
                orcKingDefeatedFromSave = data.OrcKingDefeated;

                System.Diagnostics.Debug.WriteLine("Game loaded!");
            }
            else
            {
                // New game - set initial potion inventory
                hud.RedPotionCount = 1;
                hud.RedMiniPotionCount = 2;
                System.Diagnostics.Debug.WriteLine("No save file found - starting new game with initial potions");
            }
        }

        public override void Draw(GameTime gameTime)
        {
            ScreenManager.GraphicsDevice.Clear(Color.CornflowerBlue);

            // Cover the first frames with black to avoid spawn position flicker
            if (TransitionAlpha < 0.5f)
            {
                _spriteBatch.Begin();
                _spriteBatch.Draw(fadeTexture, new Rectangle(0, 0,
                    ScreenManager.GraphicsDevice.Viewport.Width,
                    ScreenManager.GraphicsDevice.Viewport.Height),
                    Color.Black);
                _spriteBatch.End();
                return;
            }

            var tilemap = RoomManager.CurrentTilemap;
            if (tilemap == null)
            {
                System.Diagnostics.Debug.WriteLine("WARNING: CurrentTilemap is null in Draw!");
                return;
            }

            _spriteBatch.Begin(
                camera,
                SpriteSortMode.BackToFront,
                BlendState.AlphaBlend,
                SamplerState.PointClamp,
                DepthStencilState.Default,
                RasterizerState.CullNone
            );

            // Draw background/floor layers first (behind entities)
            float baseDepth = 0.99f;
            float depthStep = 0.01f;

            for (int i = 0; i < tilemap.Layers.Count; i++)
            {
                var layer = tilemap.Layers[i];
                // Draw base layers: Walls & Floor, Floor, Walls, Background, BackgroundDetails
                if (layer.Name == "Walls & Floor" ||
                    layer.Name == "Walls" ||
                    layer.Name == "Background" ||
                    layer.Name == "BackgroundDetails" ||
                    layer.Name.Contains("Floor"))
                {
                    float layerDepth = baseDepth - (i * depthStep);
                    TilemapRenderer.DrawLayer(_spriteBatch, tilemap, layer, layerDepth, RoomManager.TilemapScale);
                }
            }

            // Draw player animation
            if (player.Animation != null)
            {
                var anim = player.Animation;
                float yPosition = anim.Position.Y + anim.FrameHeight / 2f + anim.LayerDepthOffset;
                float normalizedY = yPosition / 2000f;
                float layerDepth = MathHelper.Clamp(0.49f - (normalizedY * 0.39f), 0.1f, 0.49f);

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

            // Draw enemies (each enemy handles its own drawing to support custom animations)
            foreach (var enemy in EntityManager.Enemies)
            {
                float yPosition = enemy.Position.Y + enemy.Animation.LayerDepthOffset;
                float normalizedY = yPosition / 2000f;
                float layerDepth = MathHelper.Clamp(0.49f - (normalizedY * 0.39f), 0.1f, 0.49f);
                enemy.Animation.LayerDepth = layerDepth;
                enemy.Draw(_spriteBatch);
                enemy.DrawHealthBar(_spriteBatch, instructionFont);
            }

            // Draw gauntlet spawner totems
            // Use slightly lower layer depth to draw in front of tilemap tiles
            foreach (var totem in GauntletManager.SpawnerTotems)
            {
                float yPosition = totem.Position.Y;
                float normalizedY = yPosition / 2000f;
                float layerDepth = MathHelper.Clamp(0.48f - (normalizedY * 0.39f), 0.09f, 0.48f);
                totem.Draw(_spriteBatch, layerDepth);
                totem.DrawHealthBar(_spriteBatch, instructionFont);
            }

            // Draw wooden double doors
            // Use slightly lower layer depth to draw in front of tilemap tiles
            foreach (var door in GauntletManager.Doors)
            {
                float yPosition = door.Position.Y;
                float normalizedY = yPosition / 2000f;
                float layerDepth = MathHelper.Clamp(0.48f - (normalizedY * 0.39f), 0.09f, 0.48f);
                door.Draw(_spriteBatch, layerDepth);
            }

            // Draw vases
            foreach (var vase in EntityManager.Vases)
            {
                var anim = vase.Animation;
                float yPosition = anim.Position.Y + anim.FrameHeight / 2f + anim.LayerDepthOffset;
                float normalizedY = yPosition / 2000f;
                float layerDepth = MathHelper.Clamp(0.49f - (normalizedY * 0.39f), 0.1f, 0.49f);

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

            // Draw crates (Y-sorted so bottom crates appear in front)
            foreach (var crate in EntityManager.Crates)
            {
                if (!crate.IsDestroyed)
                {
                    float yPosition = crate.Position.Y + crate.Hitbox.Height / 2f;
                    float normalizedY = yPosition / 2000f;
                    float layerDepth = MathHelper.Clamp(0.49f - (normalizedY * 0.39f), 0.1f, 0.49f);
                    crate.Draw(_spriteBatch, layerDepth);
                }
            }

            // Draw buttons
            foreach (var button in EntityManager.Buttons)
            {
                float layerDepth = 0.95f;
                button.Draw(_spriteBatch, layerDepth);
            }

            // Draw potions (entity depth range 0.1-0.49)
            foreach (var potion in EntityManager.Potions)
            {
                float yPosition = potion.Position.Y;
                float normalizedY = yPosition / 2000f;
                float layerDepth = MathHelper.Clamp(0.49f - (normalizedY * 0.39f), 0.1f, 0.49f);
                potion.Draw(_spriteBatch, layerDepth);
            }

            // Draw pillar torches (entity depth range 0.1-0.49)
            foreach (var torch in EntityManager.PillarTorches)
            {
                float torchHeight = 16 * 2 * RoomManager.TilemapScale;
                float yPosition = torch.Position.Y + torchHeight;
                float normalizedY = yPosition / 2000f;
                float layerDepth = MathHelper.Clamp(0.49f - (normalizedY * 0.39f), 0.1f, 0.49f);
                torch.Draw(_spriteBatch, layerDepth);
            }

            // Draw wall torches (entity depth range 0.1-0.49)
            foreach (var torch in EntityManager.WallTorches)
            {
                float yPosition = torch.Position.Y;
                float normalizedY = yPosition / 2000f;
                float layerDepth = MathHelper.Clamp(0.49f - (normalizedY * 0.39f), 0.1f, 0.49f);
                torch.Draw(_spriteBatch, layerDepth);
            }

            // Draw chests (entity depth range 0.1-0.49)
            foreach (var chest in EntityManager.Chests)
            {
                float yPosition = chest.Position.Y;
                float normalizedY = yPosition / 2000f;
                float layerDepth = MathHelper.Clamp(0.49f - (normalizedY * 0.39f), 0.1f, 0.49f);
                chest.Draw(_spriteBatch, layerDepth);
            }

            // Draw coins (entity depth range 0.1-0.49)
            foreach (var coin in EntityManager.Coins)
            {
                float yPosition = coin.Position.Y;
                float normalizedY = yPosition / 2000f;
                float layerDepth = MathHelper.Clamp(0.49f - (normalizedY * 0.39f), 0.1f, 0.49f);
                coin.Draw(_spriteBatch, layerDepth);
            }

            // Draw totems (entity depth range 0.1-0.49)
            foreach (var totem in EntityManager.Totems)
            {
                float yPosition = totem.Position.Y + 32 * RoomManager.TilemapScale; // Bottom of 2-tile totem
                float normalizedY = yPosition / 2000f;
                float layerDepth = MathHelper.Clamp(0.49f - (normalizedY * 0.39f), 0.1f, 0.49f);
                totem.Draw(_spriteBatch, layerDepth);
            }

            // Draw decoration layers with depth sorting
            for (int i = 0; i < tilemap.Layers.Count; i++)
            {
                var layer = tilemap.Layers[i];
                if (layer.Name == "W&F Decor" || layer.Name.Contains("Decor") || layer.Name.Contains("Above"))
                {
                    DrawLayerWithDepthSorting(_spriteBatch, tilemap, layer, RoomManager.TilemapScale);
                }
            }

            // Draw overlay layers
            for (int i = 0; i < tilemap.Layers.Count; i++)
            {
                var layer = tilemap.Layers[i];
                if (layer.Name == "Waterfall" || layer.Name.Contains("Overlay"))
                {
                    float layerDepth = 0.05f;
                    TilemapRenderer.DrawLayer(_spriteBatch, tilemap, layer, layerDepth, RoomManager.TilemapScale);
                }
            }

            // Draw LightRays layer (with diagonal fade during boss defeat, or fully visible if loaded from save)
            if (lightRaysFadeStarted || orcKingDefeatedFromSave)
            {
                for (int i = 0; i < tilemap.Layers.Count; i++)
                {
                    var layer = tilemap.Layers[i];
                    if (layer.Name == "LightRays")
                    {
                        float layerDepth = 0.04f; // In front of other overlays
                        // If boss was already defeated from save, show fully; otherwise use fade progress
                        float progress = orcKingDefeatedFromSave ? 1f : lightRaysFadeProgress;
                        TilemapRenderer.DrawLayerWithDiagonalFade(_spriteBatch, tilemap, layer, layerDepth, RoomManager.TilemapScale, progress);
                    }
                }
            }

            particleSystem.Draw(_spriteBatch, 0.05f);

            // Draw trophy interaction prompt
            if (InteractionSystem.ShowTrophyPrompt)
            {
                DrawPrompt("E", InteractionSystem.TrophyPromptPosition, 2f);
            }

            // Draw button/interaction prompt
            if (InteractionSystem.ShowPrompt)
            {
                System.Diagnostics.Debug.WriteLine($"Drawing E prompt at {InteractionSystem.PromptPosition}");
                DrawPrompt("E", InteractionSystem.PromptPosition, 1f);
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

            // Draw darkness overlay for MazeRoom
            if (RoomManager.CurrentRoom == "MazeRoom")
            {
                var vp = ScreenManager.GraphicsDevice.Viewport;
                // Calculate player's actual screen position based on camera offset
                // When camera is clamped at edges, player moves away from center
                Vector2 screenCenter = new Vector2(vp.Width / 2f, vp.Height / 2f);
                Vector2 playerScreenPos = screenCenter + (player.Position - camera.Position);
                DrawDarknessOverlay(playerScreenPos);
            }

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
            if (CheckpointManager.ShowCheckpointMessage)
            {
                DrawCheckpointMessage();
            }

            // Draw HUD
            hud.Draw(_spriteBatch, ScreenManager.GraphicsDevice.Viewport);

            // Draw boss health bar if boss exists and is alive
            if (EntityManager.OrcBoss != null && !EntityManager.OrcBoss.IsDeathAnimationComplete)
            {
                bossHealthBar.Draw(_spriteBatch, ScreenManager.GraphicsDevice.Viewport, EntityManager.OrcBoss);
            }

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

            // Draw unlock overlay
            if (showUnlockOverlay)
            {
                DrawUnlockOverlay();
            }

            // Draw victory overlay
            if (showVictoryOverlay)
            {
                DrawVictoryOverlay();
            }

            _spriteBatch.End();
        }

        private void DrawUnlockOverlay()
        {
            var viewport = ScreenManager.GraphicsDevice.Viewport;

            // Draw semi-transparent background
            _spriteBatch.Draw(
                fadeTexture,
                new Rectangle(0, 0, viewport.Width, viewport.Height),
                Color.Black * 0.7f
            );

            // Draw title
            float titleScale = 2.5f;
            Vector2 titleSize = instructionFont.MeasureString(unlockTitle) * titleScale;
            Vector2 titlePos = new Vector2(
                (viewport.Width - titleSize.X) / 2,
                viewport.Height / 3
            );

            // Title outline
            for (int x = -2; x <= 2; x++)
            {
                for (int y = -2; y <= 2; y++)
                {
                    if (x != 0 || y != 0)
                    {
                        _spriteBatch.DrawString(
                            instructionFont,
                            unlockTitle,
                            titlePos + new Vector2(x, y),
                            Color.Black,
                            0f,
                            Vector2.Zero,
                            titleScale,
                            SpriteEffects.None,
                            0f
                        );
                    }
                }
            }

            _spriteBatch.DrawString(
                instructionFont,
                unlockTitle,
                titlePos,
                Color.Gold,
                0f,
                Vector2.Zero,
                titleScale,
                SpriteEffects.None,
                0f
            );

            // Draw description
            float descScale = 1.5f;
            string[] lines = unlockDescription.Split('\n');
            float lineHeight = instructionFont.LineSpacing * descScale;
            float startY = titlePos.Y + titleSize.Y + 40;

            foreach (string line in lines)
            {
                Vector2 lineSize = instructionFont.MeasureString(line) * descScale;
                Vector2 linePos = new Vector2(
                    (viewport.Width - lineSize.X) / 2,
                    startY
                );

                // Description outline
                for (int x = -1; x <= 1; x++)
                {
                    for (int y = -1; y <= 1; y++)
                    {
                        if (x != 0 || y != 0)
                        {
                            _spriteBatch.DrawString(
                                instructionFont,
                                line,
                                linePos + new Vector2(x, y),
                                Color.Black,
                                0f,
                                Vector2.Zero,
                                descScale,
                                SpriteEffects.None,
                                0f
                            );
                        }
                    }
                }

                _spriteBatch.DrawString(
                    instructionFont,
                    line,
                    linePos,
                    Color.White,
                    0f,
                    Vector2.Zero,
                    descScale,
                    SpriteEffects.None,
                    0f
                );

                startY += lineHeight;
            }
        }

        private void DrawVictoryOverlay()
        {
            var viewport = ScreenManager.GraphicsDevice.Viewport;

            // Draw semi-transparent background
            _spriteBatch.Draw(
                fadeTexture,
                new Rectangle(0, 0, viewport.Width, viewport.Height),
                Color.Black * 0.8f
            );

            // Draw "YOU WIN!" title
            string victoryTitle = "YOU WIN!";
            float titleScale = 3.5f;
            Vector2 titleSize = instructionFont.MeasureString(victoryTitle) * titleScale;
            Vector2 titlePos = new Vector2(
                (viewport.Width - titleSize.X) / 2,
                viewport.Height / 4
            );

            // Title outline
            for (int x = -3; x <= 3; x++)
            {
                for (int y = -3; y <= 3; y++)
                {
                    if (x != 0 || y != 0)
                    {
                        _spriteBatch.DrawString(
                            instructionFont,
                            victoryTitle,
                            titlePos + new Vector2(x, y),
                            Color.Black,
                            0f,
                            Vector2.Zero,
                            titleScale,
                            SpriteEffects.None,
                            0f
                        );
                    }
                }
            }

            _spriteBatch.DrawString(
                instructionFont,
                victoryTitle,
                titlePos,
                Color.Gold,
                0f,
                Vector2.Zero,
                titleScale,
                SpriteEffects.None,
                0f
            );

            // Draw description lines
            string[] descLines = new string[]
            {
                "You have slain the Orc King",
                "and have found the light!",
                "",
                "Press E to Dismiss"
            };

            float descScale = 1.8f;
            float lineHeight = instructionFont.LineSpacing * descScale;
            float startY = titlePos.Y + titleSize.Y + 50;

            foreach (string line in descLines)
            {
                if (string.IsNullOrEmpty(line))
                {
                    startY += lineHeight * 0.5f;
                    continue;
                }

                Vector2 lineSize = instructionFont.MeasureString(line) * descScale;
                Vector2 linePos = new Vector2(
                    (viewport.Width - lineSize.X) / 2,
                    startY
                );

                // Use different color for the dismiss instruction
                Color lineColor = line.Contains("Press E") ? Color.Yellow : Color.White;

                // Description outline
                for (int x = -2; x <= 2; x++)
                {
                    for (int y = -2; y <= 2; y++)
                    {
                        if (x != 0 || y != 0)
                        {
                            _spriteBatch.DrawString(
                                instructionFont,
                                line,
                                linePos + new Vector2(x, y),
                                Color.Black,
                                0f,
                                Vector2.Zero,
                                descScale,
                                SpriteEffects.None,
                                0f
                            );
                        }
                    }
                }

                _spriteBatch.DrawString(
                    instructionFont,
                    line,
                    linePos,
                    lineColor,
                    0f,
                    Vector2.Zero,
                    descScale,
                    SpriteEffects.None,
                    0f
                );

                startY += lineHeight;
            }
        }

        private void DrawPrompt(string text, Vector2 position, float scale)
        {
            Vector2 textSize = instructionFont.MeasureString(text);

            Vector2 promptPosition = new Vector2(
                position.X - (textSize.X * scale) / 2f,
                position.Y - (textSize.Y * scale) / 2f - 10f
            );

            // Draw outline
            for (int x = -2; x <= 2; x++)
            {
                for (int y = -2; y <= 2; y++)
                {
                    if (x != 0 || y != 0)
                    {
                        _spriteBatch.DrawString(
                            instructionFont, text,
                            promptPosition + new Vector2(x, y),
                            Color.Black, 0f, Vector2.Zero, scale,
                            SpriteEffects.None, 0.002f
                        );
                    }
                }
            }

            // Draw main text
            _spriteBatch.DrawString(
                instructionFont, text, promptPosition,
                Color.White, 0f, Vector2.Zero, scale,
                SpriteEffects.None, 0.001f
            );
        }

        private void DrawCheckpointMessage()
        {
            string message = "Checkpoint Saved";
            Vector2 messageSize = instructionFont.MeasureString(message);
            float scale = 2f;

            Vector2 messagePosition = new Vector2(
                (ScreenManager.GraphicsDevice.Viewport.Width - (messageSize.X * scale)) / 2,
                ScreenManager.GraphicsDevice.Viewport.Height / 2 - 100
            );

            // Draw outline
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    if (x != 0 || y != 0)
                    {
                        _spriteBatch.DrawString(
                            instructionFont, message,
                            messagePosition + new Vector2(x * 2, y * 2),
                            Color.Black, 0f, Vector2.Zero, scale,
                            SpriteEffects.None, 0f
                        );
                    }
                }
            }

            _spriteBatch.DrawString(
                instructionFont, message, messagePosition,
                Color.White, 0f, Vector2.Zero, scale,
                SpriteEffects.None, 0f
            );
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

                    float yPosition = position.Y + (tilemap.TileHeight * scale);
                    float normalizedY = yPosition / 2000f;
                    // Decor uses depth range 0.5-0.9 (behind entities which use 0.1-0.49)
                    float layerDepth = MathHelper.Clamp(0.9f - (normalizedY * 0.4f), 0.5f, 0.9f);

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

        private void DrawRotatedHitbox(SpriteBatch spriteBatch, RotatedRectangle rotRect, Color color)
        {
            if (pixelTexture == null)
            {
                pixelTexture = new Texture2D(ScreenManager.GraphicsDevice, 1, 1);
                pixelTexture.SetData(new[] { Color.White });
            }

            Vector2[] corners = rotRect.GetCorners();

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

        private void CreateDarknessTexture()
        {
            // Create a texture large enough to cover the screen with a radial gradient
            int size = 1024; // Large enough for most screens
            darknessTexture = new Texture2D(ScreenManager.GraphicsDevice, size, size);

            Color[] data = new Color[size * size];
            Vector2 center = new Vector2(size / 2f, size / 2f);
            float maxRadius = size / 2f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    float normalizedDistance = distance / maxRadius;

                    // Create smooth falloff from center (transparent) to edge (opaque black)
                    float alpha = MathHelper.Clamp(normalizedDistance, 0f, 1f);
                    // Apply smoothstep for nicer falloff
                    alpha = alpha * alpha * (3f - 2f * alpha);

                    data[y * size + x] = new Color(0, 0, 0, alpha);
                }
            }

            darknessTexture.SetData(data);
        }

        private void DrawDarknessOverlay(Vector2 playerScreenPos)
        {
            var viewport = ScreenManager.GraphicsDevice.Viewport;

            // Calculate the size of the light circle on screen
            float lightSize = darknessRadius * 2 * RoomManager.TilemapScale;

            // Draw darkness around the player
            // We draw 4 rectangles around the light circle to cover the screen

            // First, draw the radial gradient centered on player
            Rectangle lightRect = new Rectangle(
                (int)(playerScreenPos.X - lightSize),
                (int)(playerScreenPos.Y - lightSize),
                (int)(lightSize * 2),
                (int)(lightSize * 2)
            );

            _spriteBatch.Draw(
                darknessTexture,
                lightRect,
                Color.White
            );

            // Now fill in the rest of the screen with solid black
            // Top
            if (lightRect.Top > 0)
            {
                _spriteBatch.Draw(fadeTexture,
                    new Rectangle(0, 0, viewport.Width, lightRect.Top),
                    Color.Black);
            }
            // Bottom
            if (lightRect.Bottom < viewport.Height)
            {
                _spriteBatch.Draw(fadeTexture,
                    new Rectangle(0, lightRect.Bottom, viewport.Width, viewport.Height - lightRect.Bottom),
                    Color.Black);
            }
            // Left
            if (lightRect.Left > 0)
            {
                _spriteBatch.Draw(fadeTexture,
                    new Rectangle(0, lightRect.Top, lightRect.Left, lightRect.Height),
                    Color.Black);
            }
            // Right
            if (lightRect.Right < viewport.Width)
            {
                _spriteBatch.Draw(fadeTexture,
                    new Rectangle(lightRect.Right, lightRect.Top, viewport.Width - lightRect.Right, lightRect.Height),
                    Color.Black);
            }
        }
    }
}
