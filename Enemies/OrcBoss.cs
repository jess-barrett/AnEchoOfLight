using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace GameProject2.Enemies
{
    #region Enums and Data Structures

    /// <summary>
    /// High-level boss states. These control decision-making, not animation.
    /// </summary>
    public enum BossState
    {
        Idle,           // Waiting, observing player
        Advancing,      // Moving toward player (walk or run based on distance)
        Attacking,      // Executing an attack pattern
        Recovering,     // Post-attack vulnerability window
        Staggered,      // Hit during vulnerable window, longer stun
        Hurt,           // Taking damage, brief interruption
        Dying,          // Death sequence
        Dead            // Completely finished
    }

    /// <summary>
    /// Boss phases that modify behavior, speed, and attack patterns.
    /// </summary>
    public enum BossPhase
    {
        Phase1,     // 100-60% HP: Methodical, slower attacks, more recovery
        Phase2,     // 60-30% HP: Faster, combo attacks, less recovery
        Phase3      // 30-0% HP: Enraged, relentless aggression, new moves
    }

    /// <summary>
    /// Animation states - separate from decision logic.
    /// </summary>
    public enum BossAnimation
    {
        Idle,
        Walk,
        Run,
        Attack,
        WalkAttack,
        RunAttack,
        Hurt,
        Death
    }

    /// <summary>
    /// Defines an attack with timing windows and properties.
    /// </summary>
    public class AttackData
    {
        public string Name;
        public BossAnimation Animation;
        public float WindupDuration;      // Telegraph/tell before damage
        public float ActiveDuration;      // Damage frames active
        public float RecoveryDuration;    // Punish window after attack
        public int DamageAmount;
        public float Range;               // How close player must be
        public bool CanBeInterrupted;     // Can hurt interrupt this attack?
        public int[] DamageFrames;        // Which frames deal damage

        public float TotalDuration => WindupDuration + ActiveDuration + RecoveryDuration;
    }

    /// <summary>
    /// Tracks the current attack execution state.
    /// </summary>
    public class AttackExecution
    {
        public AttackData Attack;
        public float Timer;
        public bool HasDealtDamage;
        public AttackPhase Phase;

        public enum AttackPhase { Windup, Active, Recovery, Complete }

        public void Update(float dt)
        {
            Timer += dt;

            if (Timer < Attack.WindupDuration)
                Phase = AttackPhase.Windup;
            else if (Timer < Attack.WindupDuration + Attack.ActiveDuration)
                Phase = AttackPhase.Active;
            else if (Timer < Attack.TotalDuration)
                Phase = AttackPhase.Recovery;
            else
                Phase = AttackPhase.Complete;
        }

        public bool IsInDamageWindow => Phase == AttackPhase.Active && !HasDealtDamage;
        public bool IsComplete => Phase == AttackPhase.Complete;
        public bool IsRecovering => Phase == AttackPhase.Recovery;
    }

    #endregion

    public class OrcBoss : IEnemy
    {
        #region Constants - Animation Frame Counts

        private const int FrameWidth = 64;
        private const int FrameHeight = 64;

        private static readonly Dictionary<BossAnimation, int> FrameCounts = new()
        {
            { BossAnimation.Idle, 4 },
            { BossAnimation.Walk, 6 },
            { BossAnimation.Run, 8 },
            { BossAnimation.Attack, 8 },
            { BossAnimation.WalkAttack, 6 },
            { BossAnimation.RunAttack, 8 },
            { BossAnimation.Hurt, 6 },
            { BossAnimation.Death, 8 }
        };

        private static readonly Dictionary<BossAnimation, float> FrameDurations = new()
        {
            { BossAnimation.Idle, 0.15f },
            { BossAnimation.Walk, 0.12f },
            { BossAnimation.Run, 0.08f },
            { BossAnimation.Attack, 0.1f },
            { BossAnimation.WalkAttack, 0.1f },
            { BossAnimation.RunAttack, 0.08f },
            { BossAnimation.Hurt, 0.1f },
            { BossAnimation.Death, 0.15f }
        };

        #endregion

        #region Phase Configuration

        // Health thresholds for phase transitions (percentage)
        private const float Phase2Threshold = 0.60f;  // 60% HP
        private const float Phase3Threshold = 0.30f;  // 30% HP

        // Phase-specific modifiers
        private static readonly Dictionary<BossPhase, float> SpeedMultipliers = new()
        {
            { BossPhase.Phase1, 1.0f },
            { BossPhase.Phase2, 1.25f },
            { BossPhase.Phase3, 1.5f }
        };

        private static readonly Dictionary<BossPhase, float> RecoveryMultipliers = new()
        {
            { BossPhase.Phase1, 1.0f },   // Full recovery windows
            { BossPhase.Phase2, 0.75f },  // 25% shorter recovery
            { BossPhase.Phase3, 0.5f }    // 50% shorter recovery
        };

        private static readonly Dictionary<BossPhase, float> AggressionMultipliers = new()
        {
            { BossPhase.Phase1, 1.0f },
            { BossPhase.Phase2, 0.8f },   // 20% less idle time
            { BossPhase.Phase3, 0.5f }    // 50% less idle time
        };

        #endregion

        #region Attack Patterns

        private readonly List<AttackData> _phase1Attacks;
        private readonly List<AttackData> _phase2Attacks;
        private readonly List<AttackData> _phase3Attacks;

        private void InitializeAttackPatterns()
        {
            // Phase 1: Slow, telegraphed attacks with long recovery
            _phase1Attacks.Add(new AttackData
            {
                Name = "Heavy Swing",
                Animation = BossAnimation.Attack,
                WindupDuration = 0.5f,      // Clear telegraph
                ActiveDuration = 0.3f,
                RecoveryDuration = 0.8f,    // Long punish window
                DamageAmount = 2,
                Range = 150f,
                CanBeInterrupted = false,
                DamageFrames = new[] { 0, 1, 2, 3, 4, 5 }
            });

            _phase1Attacks.Add(new AttackData
            {
                Name = "Lunging Strike",
                Animation = BossAnimation.WalkAttack,
                WindupDuration = 0.4f,
                ActiveDuration = 0.4f,
                RecoveryDuration = 0.6f,
                DamageAmount = 1,
                Range = 200f,
                CanBeInterrupted = false,
                DamageFrames = new[] { 0, 1, 2, 3, 4 }
            });

            // Phase 2: Faster attacks, shorter windows
            _phase2Attacks.Add(new AttackData
            {
                Name = "Quick Slash",
                Animation = BossAnimation.Attack,
                WindupDuration = 0.3f,
                ActiveDuration = 0.25f,
                RecoveryDuration = 0.4f,
                DamageAmount = 1,
                Range = 140f,
                CanBeInterrupted = false,
                DamageFrames = new[] { 0, 1, 2, 3, 4, 5 }
            });

            _phase2Attacks.Add(new AttackData
            {
                Name = "Charging Strike",
                Animation = BossAnimation.RunAttack,
                WindupDuration = 0.2f,
                ActiveDuration = 0.5f,
                RecoveryDuration = 0.5f,
                DamageAmount = 2,
                Range = 250f,
                CanBeInterrupted = false,
                DamageFrames = new[] { 0, 1, 2, 3, 4, 5, 6 }
            });

            _phase2Attacks.Add(new AttackData
            {
                Name = "Double Swing",
                Animation = BossAnimation.Attack,
                WindupDuration = 0.25f,
                ActiveDuration = 0.4f,
                RecoveryDuration = 0.35f,
                DamageAmount = 1,
                Range = 150f,
                CanBeInterrupted = false,
                DamageFrames = new[] { 0, 1, 2, 3, 4, 5, 6 }
            });

            // Phase 3: Enraged - relentless aggression
            _phase3Attacks.Add(new AttackData
            {
                Name = "Fury Swipe",
                Animation = BossAnimation.Attack,
                WindupDuration = 0.15f,     // Minimal telegraph
                ActiveDuration = 0.3f,
                RecoveryDuration = 0.25f,   // Very short recovery
                DamageAmount = 2,
                Range = 160f,
                CanBeInterrupted = false,
                DamageFrames = new[] { 0, 1, 2, 3, 4, 5, 6 }
            });

            _phase3Attacks.Add(new AttackData
            {
                Name = "Berserk Rush",
                Animation = BossAnimation.RunAttack,
                WindupDuration = 0.1f,
                ActiveDuration = 0.6f,
                RecoveryDuration = 0.3f,
                DamageAmount = 3,
                Range = 300f,
                CanBeInterrupted = false,
                DamageFrames = new[] { 0, 1, 2, 3, 4, 5, 6, 7 }
            });

            _phase3Attacks.Add(new AttackData
            {
                Name = "Rage Combo",
                Animation = BossAnimation.WalkAttack,
                WindupDuration = 0.2f,
                ActiveDuration = 0.5f,
                RecoveryDuration = 0.2f,
                DamageAmount = 1,
                Range = 180f,
                CanBeInterrupted = false,
                DamageFrames = new[] { 0, 1, 2, 3, 4, 5 }
            });
        }

        #endregion

        #region Core Properties

        public Vector2 Position { get; set; }
        public int MaxHealth { get; private set; }
        public int CurrentHealth { get; private set; }
        public bool IsDead => State == BossState.Dead;
        public bool IsDeathAnimationComplete { get; private set; }
        public bool ShowDeathParticles => false;

        // State
        public BossState State { get; private set; } = BossState.Idle;
        public BossPhase Phase { get; private set; } = BossPhase.Phase1;

        // For IEnemy interface (legacy compatibility)
        public SpriteAnimation Animation { get; private set; }

        public Rectangle Hitbox => new Rectangle(
            (int)(Position.X - _hitboxWidth / 2),
            (int)(Position.Y - _hitboxHeight / 2 + 20),
            _hitboxWidth,
            _hitboxHeight
        );

        public RotatedRectangle RotatedHitbox => new RotatedRectangle(Hitbox, 0);

        #endregion

        #region Private Fields

        // Textures
        private readonly Dictionary<BossAnimation, Texture2D> _textures = new();

        // Movement
        private const float BaseWalkSpeed = 80f;
        private const float BaseRunSpeed = 180f;
        private const float RunDistanceThreshold = 250f;
        private readonly float _scale = 5f;
        private readonly int _hitboxWidth = 100;
        private readonly int _hitboxHeight = 120;

        // Direction
        private Direction _facing = Direction.Down;

        // Animation state (separate from logic state)
        private BossAnimation _currentAnimation = BossAnimation.Idle;
        private int _currentFrame;
        private float _frameTimer;
        private bool _animationLooping = true;

        // Combat
        private AttackExecution _currentAttack;
        private float _attackCooldown;
        private const float BaseAttackCooldown = 1.5f;
        private readonly Random _rng = new();

        // Decision timers
        private float _idleTimer;
        private float _idleDuration;
        private const float BaseIdleDuration = 1.0f;

        // Stagger/Hurt
        private float _staggerTimer;
        private const float StaggerDuration = 1.0f;
        private float _hurtTimer;
        private const float HurtDuration = 0.3f;
        private int _staggerAccumulator;      // Damage accumulated during vulnerable window
        private const int StaggerThreshold = 60;  // Damage needed to trigger stagger

        // Consecutive hit tracking - triggers retreat
        private int _consecutiveHits;
        private float _consecutiveHitTimer;
        private const float ConsecutiveHitWindow = 1.5f;  // Hits within this window count as consecutive
        private const int HitsBeforeRetreat = 2;          // Retreat after this many hits

        // Retreat state
        private bool _isRetreating;
        private float _retreatTimer;
        private const float RetreatDuration = 0.4f;
        private const float RetreatSpeed = 350f;
        private Vector2 _retreatDirection;

        // Damage flash
        private float _damageFlashTimer;
        private const float DamageFlashDuration = 0.15f;

        // Aggro
        private const float AggroRange = 500f;

        // Phase transition
        private bool _isTransitioningPhase;
        private float _phaseTransitionTimer;
        private const float PhaseTransitionDuration = 1.5f;

        // Reference to player for retreat direction
        private Vector2 _lastPlayerPosition;

        #endregion

        #region Constructor

        public OrcBoss(
            Texture2D idle,
            Texture2D walk,
            Texture2D run,
            Texture2D attack,
            Texture2D walkAttack,
            Texture2D runAttack,
            Texture2D hurt,
            Texture2D death,
            Vector2 startPosition,
            int maxHealth = 1500)
        {
            // Store textures
            _textures[BossAnimation.Idle] = idle;
            _textures[BossAnimation.Walk] = walk;
            _textures[BossAnimation.Run] = run;
            _textures[BossAnimation.Attack] = attack;
            _textures[BossAnimation.WalkAttack] = walkAttack;
            _textures[BossAnimation.RunAttack] = runAttack;
            _textures[BossAnimation.Hurt] = hurt;
            _textures[BossAnimation.Death] = death;

            Position = startPosition;
            MaxHealth = maxHealth;
            CurrentHealth = maxHealth;

            // Initialize attack pattern lists
            _phase1Attacks = new List<AttackData>();
            _phase2Attacks = new List<AttackData>();
            _phase3Attacks = new List<AttackData>();
            InitializeAttackPatterns();

            // Legacy animation for IEnemy interface
            Animation = new SpriteAnimation(idle, FrameCounts[BossAnimation.Idle], 10);
            Animation.Scale = _scale;
            Animation.Position = Position;

            SetAnimation(BossAnimation.Idle, true);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Set the boss as already defeated (for loading saved state).
        /// Places boss on last frame of death animation.
        /// </summary>
        public void SetAsDefeated()
        {
            State = BossState.Dead;
            CurrentHealth = 0;
            IsDeathAnimationComplete = true;
            _currentAttack = null;
            _isRetreating = false;
            SetAnimation(BossAnimation.Death, false);
            // Set to last frame
            _currentFrame = FrameCounts[BossAnimation.Death] - 1;
        }

        public void TakeDamage(int amount)
        {
            if (State == BossState.Dead || State == BossState.Dying)
                return;

            // Don't take damage while retreating (brief invulnerability)
            if (_isRetreating)
                return;

            CurrentHealth = Math.Max(0, CurrentHealth - amount);
            _damageFlashTimer = DamageFlashDuration;

            // Track consecutive hits for retreat mechanic
            _consecutiveHits++;
            _consecutiveHitTimer = ConsecutiveHitWindow;

            // Check for phase transition
            CheckPhaseTransition();

            // Accumulate stagger damage if in recovery
            if (State == BossState.Recovering || (_currentAttack?.IsRecovering ?? false))
            {
                _staggerAccumulator += amount;
                if (_staggerAccumulator >= StaggerThreshold)
                {
                    EnterStaggered();
                    return;
                }
            }

            // Check for death
            if (CurrentHealth <= 0)
            {
                EnterDying();
                return;
            }

            // Trigger retreat after consecutive hits (boss jumps back to reset)
            if (_consecutiveHits >= HitsBeforeRetreat)
            {
                EnterRetreat();
                return;
            }

            // Only interrupt if in interruptible state or attack allows it
            if (CanBeInterrupted())
            {
                EnterHurt();
            }
        }

        public void Update(GameTime gameTime, Player player, List<Rectangle> collisionBoxes)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Track player position for retreat direction
            _lastPlayerPosition = player.Position;

            // Update timers
            UpdateTimers(dt);

            // Update consecutive hit timer - reset counter if window expires
            if (_consecutiveHitTimer > 0)
            {
                _consecutiveHitTimer -= dt;
                if (_consecutiveHitTimer <= 0)
                {
                    _consecutiveHits = 0;
                }
            }

            // Handle retreat movement (takes priority over state machine)
            if (_isRetreating)
            {
                UpdateRetreat(dt, collisionBoxes);
                UpdateAnimation(dt);
                Animation.Position = Position;
                return;
            }

            // Update animation (always runs)
            UpdateAnimation(dt);

            // State machine
            switch (State)
            {
                case BossState.Idle:
                    UpdateIdle(dt, player, collisionBoxes);
                    break;

                case BossState.Advancing:
                    UpdateAdvancing(dt, player, collisionBoxes);
                    break;

                case BossState.Attacking:
                    UpdateAttacking(dt, player, collisionBoxes);
                    break;

                case BossState.Recovering:
                    UpdateRecovering(dt, player);
                    break;

                case BossState.Staggered:
                    UpdateStaggered(dt);
                    break;

                case BossState.Hurt:
                    UpdateHurt(dt);
                    break;

                case BossState.Dying:
                    UpdateDying(dt);
                    break;

                case BossState.Dead:
                    // Do nothing
                    break;
            }

            // Handle phase transition overlay
            if (_isTransitioningPhase)
            {
                UpdatePhaseTransition(dt);
            }

            // Update facing direction when not locked in animation
            if (State != BossState.Attacking && State != BossState.Staggered &&
                State != BossState.Hurt && State != BossState.Dying &&
                State != BossState.Dead && !_isRetreating)
            {
                UpdateFacing(player.Position);
            }

            // Update legacy animation position
            Animation.Position = Position;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (!_textures.TryGetValue(_currentAnimation, out var texture))
                return;

            int row = (int)_facing;
            int frameCount = FrameCounts[_currentAnimation];
            int frame = Math.Min(_currentFrame, frameCount - 1);

            Rectangle sourceRect = new Rectangle(
                frame * FrameWidth,
                row * FrameHeight,
                FrameWidth,
                FrameHeight
            );

            Color drawColor = _damageFlashTimer > 0 ? Color.Red : Color.White;

            // Enraged visual effect in Phase 3 (but not when dying/dead)
            if (Phase == BossPhase.Phase3 && State != BossState.Dying && State != BossState.Dead)
            {
                float pulse = (float)(Math.Sin(Environment.TickCount / 100.0) * 0.2 + 0.8);
                drawColor = new Color(
                    (int)(255 * pulse),
                    (int)(200 * pulse),
                    (int)(200 * pulse)
                );
                if (_damageFlashTimer > 0)
                    drawColor = Color.Red;
            }

            spriteBatch.Draw(
                texture,
                Position,
                sourceRect,
                drawColor,
                0f,
                new Vector2(FrameWidth / 2f, FrameHeight / 2f),
                _scale,
                SpriteEffects.None,
                0.4f
            );
        }

        public void DrawHealthBar(SpriteBatch spriteBatch, SpriteFont font)
        {
            // Boss health bar is drawn on HUD instead
        }

        /// <summary>
        /// Check if boss is in damage-dealing frames. Used by GameplayScreen.
        /// </summary>
        public bool IsAttackingAndInDamageFrames()
        {
            return _currentAttack != null &&
                   _currentAttack.IsInDamageWindow &&
                   Array.Exists(_currentAttack.Attack.DamageFrames, f => f == _currentFrame);
        }

        /// <summary>
        /// Get the attack hitbox for collision detection.
        /// </summary>
        public Rectangle GetCurrentAttackHitbox()
        {
            if (_currentAttack == null) return Rectangle.Empty;

            int attackWidth = 120;
            int attackHeight = 100;
            int offsetX = 0;
            int offsetY = 0;

            switch (_facing)
            {
                case Direction.Down:
                    offsetY = 70;
                    break;
                case Direction.Up:
                    offsetY = -70;
                    break;
                case Direction.Left:
                    offsetX = -70;
                    break;
                case Direction.Right:
                    offsetX = 70;
                    break;
            }

            return new Rectangle(
                (int)(Position.X - attackWidth / 2 + offsetX),
                (int)(Position.Y - attackHeight / 2 + offsetY),
                attackWidth,
                attackHeight
            );
        }

        /// <summary>
        /// Mark that damage was dealt this attack (prevents multi-hit).
        /// </summary>
        public void MarkDamageDealt()
        {
            if (_currentAttack != null)
                _currentAttack.HasDealtDamage = true;
        }

        /// <summary>
        /// Get current attack damage amount.
        /// </summary>
        public int GetCurrentAttackDamage()
        {
            return _currentAttack?.Attack.DamageAmount ?? 1;
        }

        #endregion

        #region State Updates

        private void UpdateIdle(float dt, Player player, List<Rectangle> collisionBoxes)
        {
            float distanceToPlayer = Vector2.Distance(Position, player.Position);

            // Check if player in range
            if (distanceToPlayer > AggroRange)
            {
                // Just idle, player too far
                return;
            }

            // Idle timer - boss doesn't immediately attack
            _idleTimer += dt;
            float adjustedIdleDuration = _idleDuration * AggressionMultipliers[Phase];

            if (_idleTimer >= adjustedIdleDuration)
            {
                // Decision time: attack or advance?
                if (CanAttack(distanceToPlayer))
                {
                    StartAttack(distanceToPlayer);
                }
                else
                {
                    EnterAdvancing();
                }
            }
        }

        private void UpdateAdvancing(float dt, Player player, List<Rectangle> collisionBoxes)
        {
            float distanceToPlayer = Vector2.Distance(Position, player.Position);

            // Check if we can attack now
            if (_attackCooldown <= 0 && CanAttack(distanceToPlayer))
            {
                StartAttack(distanceToPlayer);
                return;
            }

            // Move toward player
            bool shouldRun = distanceToPlayer > RunDistanceThreshold;
            float speed = (shouldRun ? BaseRunSpeed : BaseWalkSpeed) * SpeedMultipliers[Phase];

            // Set appropriate animation
            BossAnimation moveAnim = shouldRun ? BossAnimation.Run : BossAnimation.Walk;
            if (_currentAnimation != moveAnim)
            {
                SetAnimation(moveAnim, true);
            }

            MoveToward(player.Position, speed * dt, collisionBoxes);

            // If player moves out of aggro range, return to idle
            if (distanceToPlayer > AggroRange)
            {
                EnterIdle();
            }
        }

        private void UpdateAttacking(float dt, Player player, List<Rectangle> collisionBoxes)
        {
            if (_currentAttack == null)
            {
                EnterRecovering();
                return;
            }

            _currentAttack.Update(dt);

            // Move during walk/run attacks
            if (_currentAttack.Attack.Animation == BossAnimation.WalkAttack ||
                _currentAttack.Attack.Animation == BossAnimation.RunAttack)
            {
                float speed = _currentAttack.Attack.Animation == BossAnimation.RunAttack
                    ? BaseRunSpeed * 0.7f
                    : BaseWalkSpeed * 0.5f;
                speed *= SpeedMultipliers[Phase];

                MoveToward(player.Position, speed * dt, collisionBoxes);
            }

            // Transition when attack complete
            if (_currentAttack.IsComplete)
            {
                EnterRecovering();
            }
        }

        private void UpdateRecovering(float dt, Player player)
        {
            // Recovery is a punish window - boss is vulnerable
            // Timer managed via _currentAttack recovery duration or separate timer

            // This state is exited when animation finishes
            // Animation callback or timer check

            if (_staggerTimer <= 0)
            {
                _staggerAccumulator = 0;  // Reset stagger accumulator
                EnterIdle();
            }
        }

        private void UpdateStaggered(float dt)
        {
            // Boss is stunned, cannot act
            if (_staggerTimer <= 0)
            {
                _staggerAccumulator = 0;
                EnterIdle();
            }
        }

        private void UpdateHurt(float dt)
        {
            if (_hurtTimer <= 0)
            {
                // Return to appropriate state based on context
                EnterIdle();
            }
        }

        private void UpdateDying(float dt)
        {
            // Wait for death animation to complete
            int deathFrames = FrameCounts[BossAnimation.Death];
            if (_currentFrame >= deathFrames - 1 && !_animationLooping)
            {
                IsDeathAnimationComplete = true;
                State = BossState.Dead;
            }
        }

        private void UpdatePhaseTransition(float dt)
        {
            _phaseTransitionTimer -= dt;
            if (_phaseTransitionTimer <= 0)
            {
                _isTransitioningPhase = false;
            }
        }

        #endregion

        #region State Transitions

        private void EnterIdle()
        {
            State = BossState.Idle;
            _idleTimer = 0f;
            _idleDuration = BaseIdleDuration * (0.8f + (float)_rng.NextDouble() * 0.4f);
            _currentAttack = null;
            SetAnimation(BossAnimation.Idle, true);
            AudioManager.PlayOrcIdleSound(0.5f);
        }

        private void EnterAdvancing()
        {
            State = BossState.Advancing;
            // Animation set in UpdateAdvancing based on distance
            AudioManager.PlayOrcPursueSound(0.5f);
        }

        private void StartAttack(float distanceToPlayer)
        {
            State = BossState.Attacking;

            // Select attack based on phase and distance
            AttackData attack = SelectAttack(distanceToPlayer);
            if (attack == null)
            {
                EnterAdvancing();
                return;
            }

            _currentAttack = new AttackExecution
            {
                Attack = attack,
                Timer = 0f,
                HasDealtDamage = false,
                Phase = AttackExecution.AttackPhase.Windup
            };

            SetAnimation(attack.Animation, false);
            _attackCooldown = BaseAttackCooldown * RecoveryMultipliers[Phase];
            AudioManager.PlayOrcAttackSound(0.6f);
        }

        private void EnterRecovering()
        {
            State = BossState.Recovering;
            float recoveryTime = _currentAttack?.Attack.RecoveryDuration ?? 0.5f;
            recoveryTime *= RecoveryMultipliers[Phase];
            _staggerTimer = recoveryTime;
            _staggerAccumulator = 0;

            // Stay on last frame of attack animation during recovery
            _animationLooping = false;
        }

        private void EnterStaggered()
        {
            State = BossState.Staggered;
            _staggerTimer = StaggerDuration;
            _staggerAccumulator = 0;
            _currentAttack = null;
            SetAnimation(BossAnimation.Hurt, false);
            AudioManager.PlayOrcHurtSound(0.6f);
        }

        private void EnterHurt()
        {
            State = BossState.Hurt;
            _hurtTimer = HurtDuration;
            _currentAttack = null;
            SetAnimation(BossAnimation.Hurt, false);
            // Note: Hurt sound only plays during Staggered state, not regular hurt
        }

        private void EnterDying()
        {
            State = BossState.Dying;
            CurrentHealth = 0;
            _currentAttack = null;
            _isRetreating = false;
            SetAnimation(BossAnimation.Death, false);
            AudioManager.PlayOrcDeathSound(0.7f);
        }

        private void EnterRetreat()
        {
            _isRetreating = true;
            _retreatTimer = RetreatDuration;
            _consecutiveHits = 0;
            _consecutiveHitTimer = 0;
            _currentAttack = null;

            // Calculate retreat direction (away from player)
            Vector2 awayFromPlayer = Position - _lastPlayerPosition;
            if (awayFromPlayer.LengthSquared() > 0)
            {
                awayFromPlayer.Normalize();
                _retreatDirection = awayFromPlayer;
            }
            else
            {
                // Default to backing up in facing direction
                _retreatDirection = _facing switch
                {
                    Direction.Up => new Vector2(0, 1),
                    Direction.Down => new Vector2(0, -1),
                    Direction.Left => new Vector2(1, 0),
                    Direction.Right => new Vector2(-1, 0),
                    _ => new Vector2(0, -1)
                };
            }

            // Play hurt animation during retreat
            SetAnimation(BossAnimation.Hurt, false);
            AudioManager.PlayOrcJumpBackSound(0.6f);
        }

        private void UpdateRetreat(float dt, List<Rectangle> collisionBoxes)
        {
            _retreatTimer -= dt;

            // Move away from player
            Vector2 movement = _retreatDirection * RetreatSpeed * dt;
            Vector2 newPosition = Position + movement;

            // Check collision
            Rectangle newHitbox = new Rectangle(
                (int)(newPosition.X - _hitboxWidth / 2),
                (int)(newPosition.Y - _hitboxHeight / 2 + 20),
                _hitboxWidth,
                _hitboxHeight
            );

            bool canMove = true;
            foreach (var wall in collisionBoxes)
            {
                if (newHitbox.Intersects(wall))
                {
                    canMove = false;
                    break;
                }
            }

            if (canMove)
            {
                Position = newPosition;
            }

            // End retreat
            if (_retreatTimer <= 0)
            {
                _isRetreating = false;
                // Immediately start an attack after retreating (counterattack)
                State = BossState.Idle;
                _idleTimer = _idleDuration * 0.5f; // Shortened idle before counterattack
                SetAnimation(BossAnimation.Idle, true);
            }
        }

        #endregion

        #region Attack Selection

        private AttackData SelectAttack(float distanceToPlayer)
        {
            List<AttackData> availableAttacks = Phase switch
            {
                BossPhase.Phase1 => _phase1Attacks,
                BossPhase.Phase2 => _phase2Attacks,
                BossPhase.Phase3 => _phase3Attacks,
                _ => _phase1Attacks
            };

            // Filter by range
            var inRangeAttacks = availableAttacks.FindAll(a => distanceToPlayer <= a.Range);

            if (inRangeAttacks.Count == 0)
                return null;

            // Weighted random selection (could be expanded for smarter selection)
            return inRangeAttacks[_rng.Next(inRangeAttacks.Count)];
        }

        private bool CanAttack(float distanceToPlayer)
        {
            if (_attackCooldown > 0)
                return false;

            List<AttackData> attacks = Phase switch
            {
                BossPhase.Phase1 => _phase1Attacks,
                BossPhase.Phase2 => _phase2Attacks,
                BossPhase.Phase3 => _phase3Attacks,
                _ => _phase1Attacks
            };

            return attacks.Exists(a => distanceToPlayer <= a.Range);
        }

        #endregion

        #region Phase Management

        private void CheckPhaseTransition()
        {
            float healthPercent = (float)CurrentHealth / MaxHealth;
            BossPhase newPhase = Phase;

            if (healthPercent <= Phase3Threshold && Phase != BossPhase.Phase3)
            {
                newPhase = BossPhase.Phase3;
            }
            else if (healthPercent <= Phase2Threshold && Phase == BossPhase.Phase1)
            {
                newPhase = BossPhase.Phase2;
            }

            if (newPhase != Phase)
            {
                TransitionToPhase(newPhase);
            }
        }

        private void TransitionToPhase(BossPhase newPhase)
        {
            BossPhase previousPhase = Phase;
            Phase = newPhase;
            _isTransitioningPhase = true;
            _phaseTransitionTimer = PhaseTransitionDuration;

            // Brief invulnerability/stagger during transition
            State = BossState.Staggered;
            _staggerTimer = PhaseTransitionDuration;
            _currentAttack = null;

            SetAnimation(BossAnimation.Hurt, false);

            // Play phase change sound
            if (previousPhase == BossPhase.Phase1 && newPhase == BossPhase.Phase2)
            {
                AudioManager.PlayOrcPhaseChange1Sound(0.7f);
            }
            else if (newPhase == BossPhase.Phase3)
            {
                AudioManager.PlayOrcPhaseChange2Sound(0.7f);
            }

            System.Diagnostics.Debug.WriteLine($"Boss entered {newPhase}!");
        }

        #endregion

        #region Animation

        private void SetAnimation(BossAnimation anim, bool loop)
        {
            if (_currentAnimation == anim && _animationLooping == loop)
                return;

            _currentAnimation = anim;
            _currentFrame = 0;
            _frameTimer = 0f;
            _animationLooping = loop;
        }

        private void UpdateAnimation(float dt)
        {
            float frameDuration = FrameDurations[_currentAnimation];

            _frameTimer += dt;
            if (_frameTimer >= frameDuration)
            {
                _frameTimer -= frameDuration;
                _currentFrame++;

                int maxFrames = FrameCounts[_currentAnimation];
                if (_currentFrame >= maxFrames)
                {
                    if (_animationLooping)
                    {
                        _currentFrame = 0;
                    }
                    else
                    {
                        _currentFrame = maxFrames - 1;
                    }
                }
            }
        }

        #endregion

        #region Helpers

        private void UpdateTimers(float dt)
        {
            if (_attackCooldown > 0)
                _attackCooldown -= dt;
            if (_staggerTimer > 0)
                _staggerTimer -= dt;
            if (_hurtTimer > 0)
                _hurtTimer -= dt;
            if (_damageFlashTimer > 0)
                _damageFlashTimer -= dt;
        }

        private bool CanBeInterrupted()
        {
            // Boss can only be interrupted during idle or advancing
            // Attacks are NEVER interrupted - player must dodge or get hit
            if (State == BossState.Idle || State == BossState.Advancing)
                return true;

            // Attacks cannot be interrupted - punish bad timing
            return false;
        }

        private void UpdateFacing(Vector2 target)
        {
            Vector2 direction = target - Position;

            if (Math.Abs(direction.X) > Math.Abs(direction.Y))
            {
                _facing = direction.X > 0 ? Direction.Right : Direction.Left;
            }
            else
            {
                _facing = direction.Y > 0 ? Direction.Down : Direction.Up;
            }
        }

        private void MoveToward(Vector2 target, float amount, List<Rectangle> collisionBoxes)
        {
            Vector2 direction = target - Position;
            if (direction.LengthSquared() < 1f)
                return;

            direction.Normalize();
            Vector2 newPosition = Position + direction * amount;

            Rectangle newHitbox = new Rectangle(
                (int)(newPosition.X - _hitboxWidth / 2),
                (int)(newPosition.Y - _hitboxHeight / 2 + 20),
                _hitboxWidth,
                _hitboxHeight
            );

            foreach (var wall in collisionBoxes)
            {
                if (newHitbox.Intersects(wall))
                    return;  // Don't move if collision
            }

            Position = newPosition;
        }

        #endregion
    }
}
