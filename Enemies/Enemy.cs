using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace GameProject2.Enemies
{
    /// <summary>
    /// Common enemy states shared by all enemy types
    /// </summary>
    public enum EnemyState
    {
        Idle,
        Chasing,
        Wandering,
        TakingDamage,
        Dying
    }

    /// <summary>
    /// Abstract base class for all enemies providing common functionality
    /// </summary>
    public abstract class Enemy : IEnemy
    {
        // Position and movement
        public Vector2 Position { get; set; }
        protected float Speed = 80f;
        protected float Scale = 1f;

        // State machine
        public EnemyState State { get; protected set; } = EnemyState.Idle;

        // AI parameters
        protected float VisionRange = 500f;
        protected float ChaseRange = 600f;

        // Wandering behavior
        protected Vector2 WanderTarget;
        protected float WanderTimer = 0f;
        protected float WanderInterval = 2f;
        protected Random Random = new Random();

        // Health system
        public int MaxHealth { get; protected set; }
        public int CurrentHealth { get; protected set; }
        public bool IsDead => CurrentHealth <= 0;
        public bool IsDeathAnimationComplete { get; protected set; } = false;

        // Visual effects
        public virtual bool ShowDeathParticles => true;

        // Damage state
        protected float DamageTimer = 0f;
        protected float DamageDuration = 0.3f;

        // Damage flash effect
        protected float DamageFlashTimer = 0f;
        protected float DamageFlashDuration = 0.15f;
        protected bool ShowDamageFlash = false;
        protected Color NormalColor = Color.White;
        protected Color DamageFlashColor = Color.Red;

        // Animation
        public SpriteAnimation Animation { get; protected set; }
        protected SpriteAnimation IdleAnimation;
        protected SpriteAnimation HurtAnimation;
        protected SpriteAnimation DeathAnimation;

        // Hitbox dimensions (override in subclasses)
        protected int HitboxWidth = 64;
        protected int HitboxHeight = 64;

        public virtual Rectangle Hitbox
        {
            get
            {
                return new Rectangle(
                    (int)(Position.X - HitboxWidth / 2),
                    (int)(Position.Y - HitboxHeight / 2),
                    HitboxWidth,
                    HitboxHeight
                );
            }
        }

        public virtual RotatedRectangle RotatedHitbox
        {
            get
            {
                return new RotatedRectangle(Hitbox, 0);
            }
        }

        protected Enemy(int maxHealth)
        {
            MaxHealth = maxHealth;
            CurrentHealth = maxHealth;
            WanderTarget = Position;
        }

        public virtual void TakeDamage(int amount)
        {
            if (IsDead || State == EnemyState.TakingDamage || State == EnemyState.Dying)
                return;

            CurrentHealth -= amount;

            // Trigger damage flash
            DamageFlashTimer = DamageFlashDuration;
            ShowDamageFlash = true;

            if (CurrentHealth <= 0)
            {
                CurrentHealth = 0;
                State = EnemyState.Dying;
                OnStartDeathAnimation();
            }
            else
            {
                State = EnemyState.TakingDamage;
                DamageTimer = DamageDuration;
                OnStartHurtAnimation();
            }
        }

        protected virtual void OnStartHurtAnimation()
        {
            if (HurtAnimation != null)
            {
                Animation = HurtAnimation;
                Animation.IsLooping = false;
                Animation.setFrame(0);
                Animation.Position = Position; // Sync position immediately
            }
        }

        protected virtual void OnStartDeathAnimation()
        {
            if (DeathAnimation != null)
            {
                Animation = DeathAnimation;
                Animation.IsLooping = false;
                Animation.setFrame(0);
                Animation.Position = Position; // Sync position immediately
            }
        }

        public virtual void Update(GameTime gameTime, Player player, List<Rectangle> collisionBoxes)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Update damage flash
            if (ShowDamageFlash)
            {
                DamageFlashTimer -= dt;
                if (DamageFlashTimer <= 0)
                {
                    ShowDamageFlash = false;
                    Animation.Color = NormalColor;
                }
                else
                {
                    // Flash between red and white
                    float flashProgress = DamageFlashTimer / DamageFlashDuration;
                    Animation.Color = Color.Lerp(NormalColor, DamageFlashColor, flashProgress);
                }
            }

            // Handle death state
            if (State == EnemyState.Dying)
            {
                Animation.Update(gameTime);
                Animation.Position = Position;

                if (Animation.CurrentFrameIndex >= Animation.FrameCount - 1)
                {
                    IsDeathAnimationComplete = true;
                }
                return;
            }

            // Handle taking damage state
            if (State == EnemyState.TakingDamage)
            {
                DamageTimer -= dt;
                Animation.Update(gameTime);
                Animation.Position = Position;

                if (DamageTimer <= 0 || Animation.CurrentFrameIndex >= Animation.FrameCount - 1)
                {
                    State = EnemyState.Idle;
                    Animation = IdleAnimation;
                    Animation.IsLooping = true;
                    Animation.Position = Position; // Sync position when switching back
                }
                return;
            }

            // Normal AI behavior
            UpdateAI(gameTime, player, collisionBoxes);

            Animation.Position = Position;
            Animation.Update(gameTime);
        }

        protected virtual void UpdateAI(GameTime gameTime, Player player, List<Rectangle> collisionBoxes)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            float distanceToPlayer = Vector2.Distance(Position, player.Position);
            bool canSeePlayer = distanceToPlayer <= VisionRange && HasLineOfSight(player.Position, collisionBoxes);

            switch (State)
            {
                case EnemyState.Idle:
                    if (canSeePlayer)
                    {
                        State = EnemyState.Chasing;
                    }
                    else
                    {
                        WanderTimer += dt;
                        if (WanderTimer >= WanderInterval)
                        {
                            State = EnemyState.Wandering;
                            WanderTimer = 0f;
                        }
                    }
                    break;

                case EnemyState.Chasing:
                    if (!canSeePlayer && distanceToPlayer > ChaseRange)
                    {
                        State = EnemyState.Idle;
                    }
                    else
                    {
                        MoveTowards(player.Position, dt, collisionBoxes);
                    }
                    break;

                case EnemyState.Wandering:
                    WanderTimer += dt;

                    if (canSeePlayer)
                    {
                        State = EnemyState.Chasing;
                        WanderTimer = 0f;
                    }
                    else if (Vector2.Distance(Position, WanderTarget) < 20f || WanderTimer >= WanderInterval * 3f)
                    {
                        State = EnemyState.Idle;
                        WanderTimer = 0f;
                    }
                    else
                    {
                        MoveTowards(WanderTarget, dt, collisionBoxes);
                    }
                    break;
            }
        }

        protected virtual void MoveTowards(Vector2 target, float dt, List<Rectangle> collisionBoxes)
        {
            Vector2 direction = target - Position;
            if (direction != Vector2.Zero)
            {
                direction.Normalize();

                Vector2 newPosition = Position + direction * Speed * dt;

                Rectangle newHitbox = new Rectangle(
                    (int)(newPosition.X - HitboxWidth / 2),
                    (int)(newPosition.Y - HitboxHeight / 2),
                    HitboxWidth,
                    HitboxHeight
                );

                bool collided = false;
                foreach (var wall in collisionBoxes)
                {
                    if (newHitbox.Intersects(wall))
                    {
                        collided = true;
                        break;
                    }
                }

                if (!collided)
                {
                    Position = newPosition;
                }
                else if (State == EnemyState.Wandering)
                {
                    SetRandomWanderTarget();
                }
            }
        }

        protected bool HasLineOfSight(Vector2 targetPosition, List<Rectangle> collisionBoxes)
        {
            Vector2 direction = targetPosition - Position;
            float distance = direction.Length();

            if (distance == 0) return true;

            direction.Normalize();

            int steps = (int)(distance / 10f);
            for (int i = 0; i < steps; i++)
            {
                Vector2 checkPoint = Position + direction * (i * 10f);
                Point point = new Point((int)checkPoint.X, (int)checkPoint.Y);

                foreach (var wall in collisionBoxes)
                {
                    if (wall.Contains(point))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public void SetRandomWanderTarget()
        {
            float angle = (float)(Random.NextDouble() * Math.PI * 2);
            float distance = (float)(Random.NextDouble() * 100 + 50);

            WanderTarget = Position + new Vector2(
                (float)Math.Cos(angle) * distance,
                (float)Math.Sin(angle) * distance
            );
        }

        public virtual void Draw(SpriteBatch spriteBatch)
        {
            Animation.Draw(spriteBatch);
        }

        public virtual void DrawHealthBar(SpriteBatch spriteBatch, SpriteFont font)
        {
            // Only show health bar when enemy is aggro (chasing) or taking damage/dying
            if (IsDead) return;
            if (State != EnemyState.Chasing && State != EnemyState.TakingDamage && State != EnemyState.Dying) return;

            string healthText = $"{CurrentHealth}/{MaxHealth}";
            float textScale = 0.6f;
            Vector2 textSize = font.MeasureString(healthText) * textScale;
            Vector2 textPosition = new Vector2(
                Position.X - textSize.X / 2f,
                Position.Y - HitboxHeight / 2f - 20
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
