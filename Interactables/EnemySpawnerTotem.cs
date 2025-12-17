using GameProject2.Enemies;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace GameProject2
{
    public enum SpawnerTotemState
    {
        Inactive,       // Waiting for gauntlet to start
        Rising,         // Playing rise animation (16x32 sprite)
        Idle,           // Standing still, not spawning (shows last rise frame)
        Spawning,       // Playing glow animation while spawning enemies
        Destroyed       // Broken (rows 6-7, column 1)
    }

    public class EnemySpawnerTotem
    {
        public Vector2 Position { get; private set; }
        public SpawnerTotemState State { get; private set; } = SpawnerTotemState.Inactive;
        public string TotemId { get; set; }
        public string EnemyType { get; set; } // "Skull", "BlueSlime", etc.

        // Health system
        public int MaxHealth { get; private set; } = 500;
        public int CurrentHealth { get; private set; } = 500;
        public bool IsDestroyed => CurrentHealth <= 0;

        private Texture2D texture;
        private float scale;

        // Sprite is 16x32 (width x height)
        private int spriteWidth = 16;
        private int spriteHeight = 32;

        // Random for spawn positioning
        private static Random _random = new Random();

        // Animation state
        private int currentFrame = 0;
        private float animationTimer = 0f;
        private float frameDuration = 0.1f;

        // Rise animation: columns 0-7 (8 frames, 16x32 each)
        private int riseFrameCount = 8;

        // Glow/Spawning animation: columns 8-15 (8 frames, 16x32 each)
        private int glowFrameCount = 8;
        private int glowFrameOffset = 8; // Column offset for glow frames

        // Broken frame: column 1, row offset for broken sprite
        private int brokenFrameColumn = 1;
        private int brokenRowOffset = 3; // Row 3 in 32-pixel rows = rows 6-7 in 16-pixel rows

        // Spawning configuration
        private float spawnTimer = 0f;
        private float spawnInterval = 10f; // Try to spawn every 10 seconds
        private int maxSkullsPerTotem = 2;
        private int maxSlimesPerTotem = 3;

        // Track spawned enemies from this totem
        private List<IEnemy> spawnedEnemies = new List<IEnemy>();

        // Spawning state
        private bool isSpawningEnemies = false;
        private int enemiesToSpawn = 0;
        private float enemySpawnDelay = 0.5f; // Delay between each enemy spawn
        private float enemySpawnTimer = 0f;

        // Glow delay before spawning starts
        private float glowDelayDuration = 3f; // Glow for 3 seconds before spawning
        private float glowDelayTimer = 0f;
        private bool glowDelayComplete = false;

        // Donut spawn radius (inner = minimum distance, outer = maximum distance)
        private float spawnInnerRadius = 70f;
        private float spawnOuterRadius = 120f;

        // Damage flash
        private float damageFlashTimer = 0f;
        private float damageFlashDuration = 0.15f;
        private bool showDamageFlash = false;

        public Rectangle Hitbox
        {
            get
            {
                int width = (int)(spriteWidth * scale);
                int height = (int)(spriteHeight * scale);
                return new Rectangle(
                    (int)(Position.X),
                    (int)(Position.Y),
                    width,
                    height
                );
            }
        }

        public EnemySpawnerTotem(Texture2D totemTexture, Vector2 position, float scale, string totemId, string enemyType)
        {
            this.texture = totemTexture;
            this.Position = position;
            this.scale = scale;
            this.TotemId = totemId;
            this.EnemyType = enemyType;
        }

        public void Activate()
        {
            if (State == SpawnerTotemState.Inactive)
            {
                State = SpawnerTotemState.Rising;
                currentFrame = 0;
                animationTimer = 0f;
                // Sound is played once by GauntletManager, not per-totem
            }
        }

        public void TakeDamage(int amount)
        {
            if (IsDestroyed || (State != SpawnerTotemState.Idle && State != SpawnerTotemState.Spawning))
                return;

            CurrentHealth -= amount;
            damageFlashTimer = damageFlashDuration;
            showDamageFlash = true;

            if (CurrentHealth <= 0)
            {
                CurrentHealth = 0;
                State = SpawnerTotemState.Destroyed;
                AudioManager.PlayTotemDestroySound(0.5f);
            }
        }

        /// <summary>
        /// Gets the max enemies this totem can have alive at once
        /// </summary>
        public int GetMaxEnemies()
        {
            return EnemyType == "Skull" ? maxSkullsPerTotem : maxSlimesPerTotem;
        }

        /// <summary>
        /// Counts how many spawned enemies are still alive
        /// </summary>
        public int GetAliveEnemyCount()
        {
            // Clean up dead enemies from our tracking list
            spawnedEnemies.RemoveAll(e => e.IsDead || e.IsDeathAnimationComplete);
            return spawnedEnemies.Count;
        }

        /// <summary>
        /// Registers an enemy as spawned by this totem
        /// </summary>
        public void RegisterSpawnedEnemy(IEnemy enemy)
        {
            spawnedEnemies.Add(enemy);
        }

        /// <summary>
        /// Gets a random spawn position in a donut shape around the totem center.
        /// Returns null if no valid position found (all positions in walls).
        /// </summary>
        public Vector2? GetSpawnPosition(List<Rectangle> collisionBoxes)
        {
            // Get totem center
            Vector2 center = Position + new Vector2(spriteWidth * scale / 2f, spriteHeight * scale / 2f);

            // Try multiple times to find a valid position
            for (int attempt = 0; attempt < 20; attempt++)
            {
                // Random angle
                float angle = (float)(_random.NextDouble() * Math.PI * 2);

                // Random distance between inner and outer radius (donut shape)
                float distance = spawnInnerRadius + (float)(_random.NextDouble() * (spawnOuterRadius - spawnInnerRadius));

                // Calculate offset
                float offsetX = (float)Math.Cos(angle) * distance;
                float offsetY = (float)Math.Sin(angle) * distance;

                Vector2 spawnPos = center + new Vector2(offsetX, offsetY);

                // Check collision with walls
                Rectangle spawnHitbox = new Rectangle(
                    (int)(spawnPos.X - 24),
                    (int)(spawnPos.Y - 24),
                    48,
                    48
                );

                bool collides = false;
                foreach (var wall in collisionBoxes)
                {
                    if (spawnHitbox.Intersects(wall))
                    {
                        collides = true;
                        break;
                    }
                }

                if (!collides)
                {
                    return spawnPos;
                }
            }

            // No valid position found after 20 attempts
            return null;
        }

        /// <summary>
        /// Check if it's time to try spawning and how many enemies to spawn
        /// </summary>
        public bool ShouldTrySpawning()
        {
            if (State != SpawnerTotemState.Idle || IsDestroyed)
                return false;

            int currentAlive = GetAliveEnemyCount();
            int maxAllowed = GetMaxEnemies();

            return currentAlive < maxAllowed;
        }

        /// <summary>
        /// Start the spawning sequence - first glows for 3 seconds, then spawns
        /// </summary>
        public void StartSpawning()
        {
            if (State != SpawnerTotemState.Idle)
                return;

            int currentAlive = GetAliveEnemyCount();
            int maxAllowed = GetMaxEnemies();
            enemiesToSpawn = maxAllowed - currentAlive;

            if (enemiesToSpawn > 0)
            {
                State = SpawnerTotemState.Spawning;
                currentFrame = 0; // Start at glow frame 0 (column 8)
                animationTimer = 0f;
                glowDelayTimer = 0f;
                glowDelayComplete = false;
                isSpawningEnemies = false; // Don't spawn yet, wait for glow delay
            }
        }

        /// <summary>
        /// Called by GauntletManager to check if we need to spawn an enemy this frame
        /// </summary>
        public bool ShouldSpawnEnemyNow()
        {
            if (State != SpawnerTotemState.Spawning || !isSpawningEnemies || !glowDelayComplete)
                return false;

            return enemySpawnTimer <= 0 && enemiesToSpawn > 0;
        }

        /// <summary>
        /// Called after an enemy is spawned
        /// </summary>
        public void OnEnemySpawned()
        {
            enemiesToSpawn--;
            enemySpawnTimer = enemySpawnDelay;

            if (enemiesToSpawn <= 0)
            {
                // Done spawning, return to idle
                isSpawningEnemies = false;
                State = SpawnerTotemState.Idle;
                currentFrame = riseFrameCount - 1; // Show last rise frame
            }
        }

        public void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Update damage flash
            if (showDamageFlash)
            {
                damageFlashTimer -= dt;
                if (damageFlashTimer <= 0)
                {
                    showDamageFlash = false;
                }
            }

            if (State == SpawnerTotemState.Inactive || State == SpawnerTotemState.Destroyed)
                return;

            animationTimer += dt;

            if (State == SpawnerTotemState.Rising)
            {
                if (animationTimer >= frameDuration)
                {
                    animationTimer = 0f;
                    currentFrame++;

                    if (currentFrame >= riseFrameCount)
                    {
                        // Transition to idle state, then immediately start spawning
                        State = SpawnerTotemState.Idle;
                        currentFrame = riseFrameCount - 1; // Stay on last frame
                        spawnTimer = spawnInterval; // Set to interval so first spawn happens immediately
                    }
                }
            }
            else if (State == SpawnerTotemState.Idle)
            {
                // Count up spawn timer
                spawnTimer += dt;
                if (spawnTimer >= spawnInterval && ShouldTrySpawning())
                {
                    spawnTimer = 0f;
                    StartSpawning();
                }
            }
            else if (State == SpawnerTotemState.Spawning)
            {
                // Glow animation loops while spawning
                if (animationTimer >= frameDuration)
                {
                    animationTimer = 0f;
                    currentFrame = (currentFrame + 1) % glowFrameCount;
                }

                // Handle glow delay before spawning starts
                if (!glowDelayComplete)
                {
                    glowDelayTimer += dt;
                    if (glowDelayTimer >= glowDelayDuration)
                    {
                        glowDelayComplete = true;
                        isSpawningEnemies = true;
                        enemySpawnTimer = 0f; // Start spawning immediately after delay
                    }
                }
                else
                {
                    // Update spawn timer for staggered enemy spawns
                    if (enemySpawnTimer > 0)
                    {
                        enemySpawnTimer -= dt;
                    }
                }
            }
        }

        public void Draw(SpriteBatch spriteBatch, float layerDepth)
        {
            Color drawColor = showDamageFlash ? Color.Red : Color.White;

            if (State == SpawnerTotemState.Inactive)
            {
                // Draw first frame (dormant totem) - same as what tilemap shows
                // This ensures consistent layering
                DrawFrame(spriteBatch, 0, layerDepth, Color.White);
            }
            else if (State == SpawnerTotemState.Rising)
            {
                // Draw rise animation frame
                DrawFrame(spriteBatch, currentFrame, layerDepth, drawColor);
            }
            else if (State == SpawnerTotemState.Idle)
            {
                // Draw last frame of rise animation (standing totem)
                DrawFrame(spriteBatch, riseFrameCount - 1, layerDepth, drawColor);
            }
            else if (State == SpawnerTotemState.Spawning)
            {
                // Draw glow animation frame (columns 8-15)
                DrawFrame(spriteBatch, currentFrame + glowFrameOffset, layerDepth, drawColor);
            }
            else if (State == SpawnerTotemState.Destroyed)
            {
                // Draw broken frame
                DrawBrokenFrame(spriteBatch, layerDepth);
            }
        }

        private void DrawFrame(SpriteBatch spriteBatch, int column, float layerDepth, Color color)
        {
            // Source rectangle for 16x32 sprite
            Rectangle sourceRect = new Rectangle(
                column * spriteWidth,
                0,
                spriteWidth,
                spriteHeight
            );

            spriteBatch.Draw(
                texture,
                Position,
                sourceRect,
                color,
                0f,
                Vector2.Zero,
                scale,
                SpriteEffects.None,
                layerDepth
            );
        }

        private void DrawBrokenFrame(SpriteBatch spriteBatch, float layerDepth)
        {
            // Broken sprite is at column 1, starting at row 6 (in 16px rows) = row 3 in 32px rows
            Rectangle sourceRect = new Rectangle(
                brokenFrameColumn * spriteWidth,
                brokenRowOffset * spriteHeight,
                spriteWidth,
                spriteHeight
            );

            spriteBatch.Draw(
                texture,
                Position,
                sourceRect,
                Color.White,
                0f,
                Vector2.Zero,
                scale,
                SpriteEffects.None,
                layerDepth
            );
        }

        public void DrawHealthBar(SpriteBatch spriteBatch, SpriteFont font)
        {
            if (IsDestroyed || (State != SpawnerTotemState.Idle && State != SpawnerTotemState.Spawning))
                return;

            string healthText = $"{CurrentHealth}/{MaxHealth}";
            float textScale = 0.5f;
            Vector2 textSize = font.MeasureString(healthText) * textScale;
            Vector2 textPosition = new Vector2(
                Position.X + (spriteWidth * scale / 2f) - textSize.X / 2f,
                Position.Y - 15
            );

            // Draw outline
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    if (x != 0 || y != 0)
                    {
                        spriteBatch.DrawString(
                            font,
                            healthText,
                            textPosition + new Vector2(x, y),
                            Color.Black,
                            0f,
                            Vector2.Zero,
                            textScale,
                            SpriteEffects.None,
                            0.001f
                        );
                    }
                }
            }

            // Draw health text
            Color healthColor = CurrentHealth > MaxHealth / 2 ? Color.LimeGreen :
                               CurrentHealth > MaxHealth / 4 ? Color.Yellow : Color.Red;

            spriteBatch.DrawString(
                font,
                healthText,
                textPosition,
                healthColor,
                0f,
                Vector2.Zero,
                textScale,
                SpriteEffects.None,
                0.0005f
            );
        }
    }
}
