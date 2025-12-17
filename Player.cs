using GameProject2.Enemies;
using GameProject2.Managers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace GameProject2
{
    public enum PlayerState
    {
        Idle,
        Walk,
        Run,
        Attack1,
        Attack2,
        Heal,
        Hurt,
        Dash,
        Death
    }

    public class Player
    {
        private Vector2 position = new Vector2(500, 300);

        private int walkSpeed = 300;
        private int runSpeed = 500;

        public Direction Direction = Direction.Down;

        private bool isMoving = false;

        private float footstepTimer = 0f;
        private float walkFootstepInterval = 0.5f;
        private float runFootstepInterval = 0.3f;

        public PlayerState State { get; set; } = PlayerState.Idle;

        public SpriteAnimation Animation;

        public SpriteAnimation[] idleAnimations = new SpriteAnimation[4];
        public SpriteAnimation[] walkAnimations = new SpriteAnimation[4];
        public SpriteAnimation[] runAnimations = new SpriteAnimation[4];
        public SpriteAnimation[] attack1Animations = new SpriteAnimation[4];
        public SpriteAnimation[] attack2Animations = new SpriteAnimation[4];
        public SpriteAnimation[] dashAnimations = new SpriteAnimation[4];
        public SpriteAnimation[] healAnimations = new SpriteAnimation[4];
        public SpriteAnimation[] hurtAnimations = new SpriteAnimation[4];
        public SpriteAnimation[] deathAnimations = new SpriteAnimation[4];

        // Ability unlock flags
        public bool HasAttack2 { get; set; } = false;
        public bool HasDash { get; set; } = false;

        // Events for HUD cooldown triggers
        public event Action OnDashUsed;
        public event Action OnAttack2Used;

        // Events for potion usage
        public event Action OnRedPotionUsed;
        public event Action OnRedMiniPotionUsed;

        // Previous keyboard state for single-press detection
        private KeyboardState previousKbState;

        // Dash state - Hollow Knight style: fast burst, abrupt stop
        private float dashSpeed = 1100f;
        private float dashDuration = 0.2f;
        private float dashTimer = 0f;
        private Vector2 dashDirection;
        private float dashCooldown = 2f;
        private float dashCooldownTimer = 0f;

        public Vector2 Position => position;

        public RotatedRectangle RotatedHitbox
        {
            get
            {
                if (Animation == null) return new RotatedRectangle(Rectangle.Empty, 0);

                int hitboxWidth = 48;
                int hitboxHeight = 112;
                float rotation = 0f;

                if (State == PlayerState.Run)
                {
                    switch (Direction)
                    {
                        case Direction.Left:
                            hitboxWidth = 48;
                            hitboxHeight = 112;
                            rotation = MathHelper.ToRadians(-15);
                            break;

                        case Direction.Right:
                            hitboxWidth = 48;
                            hitboxHeight = 112;
                            rotation = MathHelper.ToRadians(15);
                            break;

                        case Direction.Up:
                        case Direction.Down:
                            hitboxWidth = 48;
                            hitboxHeight = 96;
                            break;
                    }
                }

                Rectangle rect = new Rectangle(
                    (int)(position.X - hitboxWidth / 2),
                    (int)(position.Y - hitboxHeight / 2),
                    hitboxWidth,
                    hitboxHeight
                );

                return new RotatedRectangle(rect, rotation);
            }
        }
        public Rectangle Hitbox => RotatedHitbox.Rectangle;

        public void SetX(float newX) => position.X = newX;
        public void SetY(float newY) => position.Y = newY;

        // Pending heal amount (applied when animation completes)
        private int pendingHealAmount = 0;

        // Event fired when heal animation completes
        public event Action<int> OnHealAnimationComplete;

        // Trigger heal animation (called when player uses a potion)
        public bool TriggerHealAnimation(int healAmount)
        {
            System.Diagnostics.Debug.WriteLine($"TriggerHealAnimation called with healAmount={healAmount}, current State={State}");
            if (State != PlayerState.Death && State != PlayerState.Hurt && State != PlayerState.Heal)
            {
                State = PlayerState.Heal;
                Animation = healAnimations[(int)Direction];
                Animation.IsLooping = false;
                Animation.setFrame(0);
                Animation.Position = position;
                pendingHealAmount = healAmount;
                System.Diagnostics.Debug.WriteLine($"Heal animation started, pendingHealAmount={pendingHealAmount}");
                return true;
            }
            System.Diagnostics.Debug.WriteLine("TriggerHealAnimation blocked due to state");
            return false;
        }

        // Damage values for attacks
        public const int Attack1Damage = 25;
        public const int Attack2Damage = 40;

        // Track enemies that have been hit this attack to prevent multi-hit
        private HashSet<IEnemy> enemiesHitThisAttack = new HashSet<IEnemy>();

        public void Update(GameTime gameTime, List<IEnemy> enemies, ParticleSystem particleSystem, List<Rectangle> collisionBoxes)
        {
            KeyboardState kbState = Keyboard.GetState();
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Update dash cooldown
            if (dashCooldownTimer > 0)
                dashCooldownTimer -= dt;

            // === DEATH LOGIC ===
            if (State == PlayerState.Death)
            {
                Animation.Position = position;
                Animation.Update(gameTime);

                if (Animation.CurrentFrameIndex == Animation.FrameCount - 1)
                {
                    // Death animation complete - could trigger game over here
                    // For now, just freeze on last frame
                }

                return;
            }

            // === HURT LOGIC ===
            if (State == PlayerState.Hurt)
            {
                Animation.Position = position;
                Animation.Update(gameTime);

                // End hurt state when animation finishes
                if (Animation.CurrentFrameIndex == Animation.FrameCount - 1)
                {
                    State = PlayerState.Idle;
                }

                // Freeze movement while hurt
                return;
            }

            // === HEAL LOGIC ===
            if (State == PlayerState.Heal)
            {
                Animation.Position = position;
                Animation.Update(gameTime);

                // End heal state when animation finishes
                if (Animation.CurrentFrameIndex == Animation.FrameCount - 1)
                {
                    State = PlayerState.Idle;
                    // Apply heal when animation completes
                    if (pendingHealAmount > 0)
                    {
                        OnHealAnimationComplete?.Invoke(pendingHealAmount);
                        pendingHealAmount = 0;
                    }
                }

                // Freeze movement while healing
                return;
            }

            // === DASH LOGIC ===
            if (State == PlayerState.Dash)
            {
                dashTimer -= dt;

                if (dashTimer > 0)
                {
                    // Move in dash direction
                    Vector2 newPosition = position + dashDirection * dashSpeed * dt;

                    Rectangle newHitbox = new Rectangle(
                        (int)(newPosition.X - 24),
                        (int)(newPosition.Y - 56),
                        48,
                        112
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
                        position = newPosition;
                    }
                }

                Animation.Position = position;
                Animation.Update(gameTime);

                // End dash when timer expires
                if (dashTimer <= 0)
                {
                    State = PlayerState.Idle;
                    dashCooldownTimer = dashCooldown;
                }

                return;
            }

            // === ATTACK1 LOGIC ===
            if (State == PlayerState.Attack1)
            {
                Animation.Update(gameTime);

                int attackStartFrame = 1;
                int attackEndFrame = 3;

                if (Animation.CurrentFrameIndex >= attackStartFrame && Animation.CurrentFrameIndex <= attackEndFrame)
                {
                    Rectangle attackHitbox = Hitbox;
                    int verticalRange = 40;
                    int horizontalRange = 100;
                    switch (Direction)
                    {
                        case Direction.Up: attackHitbox.Y -= verticalRange; break;
                        case Direction.Down: attackHitbox.Y += verticalRange; break;
                        case Direction.Left: attackHitbox.X -= horizontalRange; break;
                        case Direction.Right: attackHitbox.X += horizontalRange; break;
                    }

                    foreach (var enemy in enemies)
                    {
                        // Skip if already hit this attack or if enemy is dying/dead
                        if (enemiesHitThisAttack.Contains(enemy) || enemy.IsDead)
                            continue;

                        if (attackHitbox.Intersects(enemy.Hitbox))
                        {
                            enemy.TakeDamage(Attack1Damage);
                            enemiesHitThisAttack.Add(enemy);
                            AudioManager.PlayAttackLandingSound(0.15f);

                            // Create death particle effect only for enemies that want it
                            if (enemy.IsDead && enemy.ShowDeathParticles)
                            {
                                particleSystem.CreateSkullDeathEffect(enemy.Position);
                            }
                        }
                    }

                    // Attack spawner totems in gauntlet
                    GauntletManager.HandlePlayerAttackOnTotems(attackHitbox, Attack1Damage);
                }

                // End attack when animation finishes
                if (Animation.CurrentFrameIndex == Animation.FrameCount - 1)
                {
                    State = PlayerState.Idle;
                    enemiesHitThisAttack.Clear();
                }

                return;
            }

            // === ATTACK2 LOGIC ===
            if (State == PlayerState.Attack2)
            {
                Animation.Update(gameTime);

                int attackStartFrame = 2;
                int attackEndFrame = 5;

                if (Animation.CurrentFrameIndex >= attackStartFrame && Animation.CurrentFrameIndex <= attackEndFrame)
                {
                    // Attack2 has wider range
                    Rectangle attackHitbox = Hitbox;
                    int verticalRange = 60;
                    int horizontalRange = 140;
                    switch (Direction)
                    {
                        case Direction.Up: attackHitbox.Y -= verticalRange; break;
                        case Direction.Down: attackHitbox.Y += verticalRange; break;
                        case Direction.Left: attackHitbox.X -= horizontalRange; break;
                        case Direction.Right: attackHitbox.X += horizontalRange; break;
                    }

                    foreach (var enemy in enemies)
                    {
                        // Skip if already hit this attack or if enemy is dying/dead
                        if (enemiesHitThisAttack.Contains(enemy) || enemy.IsDead)
                            continue;

                        if (attackHitbox.Intersects(enemy.Hitbox))
                        {
                            enemy.TakeDamage(Attack2Damage);
                            enemiesHitThisAttack.Add(enemy);
                            AudioManager.PlayAttackLandingSound(0.15f);

                            // Create death particle effect only for enemies that want it
                            if (enemy.IsDead && enemy.ShowDeathParticles)
                            {
                                particleSystem.CreateSkullDeathEffect(enemy.Position);
                            }
                        }
                    }

                    // Attack spawner totems in gauntlet
                    GauntletManager.HandlePlayerAttackOnTotems(attackHitbox, Attack2Damage);
                }

                // End attack when animation finishes
                if (Animation.CurrentFrameIndex == Animation.FrameCount - 1)
                {
                    State = PlayerState.Idle;
                    enemiesHitThisAttack.Clear();
                }

                return;
            }

            // === DASH TRIGGER (Space) - requires unlock ===
            // Check dash FIRST before movement input so WASD doesn't change direction mid-dash-trigger
            if (HasDash && kbState.IsKeyDown(Keys.Space) && dashCooldownTimer <= 0)
            {
                State = PlayerState.Dash;
                Animation = dashAnimations[(int)Direction];
                Animation.IsLooping = false;
                Animation.setFrame(0);
                Animation.Position = position;
                dashTimer = dashDuration;

                // Set dash direction based on current direction
                switch (Direction)
                {
                    case Direction.Up: dashDirection = new Vector2(0, -1); break;
                    case Direction.Down: dashDirection = new Vector2(0, 1); break;
                    case Direction.Left: dashDirection = new Vector2(-1, 0); break;
                    case Direction.Right: dashDirection = new Vector2(1, 0); break;
                }

                OnDashUsed?.Invoke();
                return;
            }

            // === MOVEMENT INPUT ===
            isMoving = false;

            if (kbState.IsKeyDown(Keys.D)) { Direction = Direction.Right; isMoving = true; }
            if (kbState.IsKeyDown(Keys.A)) { Direction = Direction.Left; isMoving = true; }
            if (kbState.IsKeyDown(Keys.W)) { Direction = Direction.Up; isMoving = true; }
            if (kbState.IsKeyDown(Keys.S)) { Direction = Direction.Down; isMoving = true; }

            // === POTION TRIGGERS ===
            // Red Potion (Q) - single press
            if (kbState.IsKeyDown(Keys.Q) && previousKbState.IsKeyUp(Keys.Q))
            {
                System.Diagnostics.Debug.WriteLine("Q pressed - firing OnRedPotionUsed");
                OnRedPotionUsed?.Invoke();
                // Return to prevent other actions from overriding heal state
                if (State == PlayerState.Heal) return;
            }

            // Red Mini Potion (F) - single press
            if (kbState.IsKeyDown(Keys.F) && previousKbState.IsKeyUp(Keys.F))
            {
                System.Diagnostics.Debug.WriteLine("F pressed - firing OnRedMiniPotionUsed");
                OnRedMiniPotionUsed?.Invoke();
                // Return to prevent other actions from overriding heal state
                if (State == PlayerState.Heal) return;
            }

            // === ATTACK1 TRIGGER (Left Click) ===
            MouseState mouseState = Mouse.GetState();
            if (mouseState.LeftButton == ButtonState.Pressed)
            {
                State = PlayerState.Attack1;
                Animation = attack1Animations[(int)Direction];
                Animation.IsLooping = false;
                Animation.setFrame(0);
                Animation.Position = position;
                enemiesHitThisAttack.Clear();
                GauntletManager.ClearAttackHitTracking(); // Clear totem hit tracking
                AudioManager.PlaySwingSwordSound(0.25f);
                return;
            }

            // === ATTACK2 TRIGGER (Right Click) - requires unlock ===
            if (HasAttack2 && mouseState.RightButton == ButtonState.Pressed)
            {
                State = PlayerState.Attack2;
                Animation = attack2Animations[(int)Direction];
                Animation.IsLooping = false;
                Animation.setFrame(0);
                Animation.Position = position;
                enemiesHitThisAttack.Clear();
                GauntletManager.ClearAttackHitTracking(); // Clear totem hit tracking
                AudioManager.PlaySwingSwordSound(0.35f);
                OnAttack2Used?.Invoke();
                return;
            }

            // === MOVEMENT LOGIC ===
            if (isMoving)
            {
                State = kbState.IsKeyDown(Keys.LeftShift) ? PlayerState.Run : PlayerState.Walk;
                int speed = State == PlayerState.Walk ? walkSpeed : runSpeed;

                Vector2 newPosition = position;

                switch (Direction)
                {
                    case Direction.Down: newPosition.Y += speed * dt; break;
                    case Direction.Up: newPosition.Y -= speed * dt; break;
                    case Direction.Left: newPosition.X -= speed * dt; break;
                    case Direction.Right: newPosition.X += speed * dt; break;
                }

                Rectangle newHitbox = new Rectangle(
                    (int)(newPosition.X - 24),
                    (int)(newPosition.Y - 56),
                    48,
                    112
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
                    position = newPosition;
                }

                Animation = State == PlayerState.Walk
                    ? walkAnimations[(int)Direction]
                    : runAnimations[(int)Direction];

                // Footsteps
                footstepTimer += dt;
                float interval = State == PlayerState.Walk ? walkFootstepInterval : runFootstepInterval;
                if (footstepTimer >= interval)
                {
                    footstepTimer = 0f;
                    float pitchVariation = (float)(new Random().NextDouble() * 0.2 - 0.1);
                    float volume = State == PlayerState.Walk ? 0.6f : 0.8f;
                    AudioManager.PlayFootstep(volume, pitchVariation);
                }
            }
            else
            {
                State = PlayerState.Idle;
                Animation = idleAnimations[(int)Direction];
                footstepTimer = 0f;
            }

            Animation.Position = position;
            Animation.Update(gameTime);

            // Store previous keyboard state
            previousKbState = kbState;
        }
    }
}
