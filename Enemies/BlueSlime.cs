using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace GameProject2.Enemies
{
    /// <summary>
    /// Blue Slime enemy - a small bouncing slime that chases the player
    /// Has 40 health, weaker than the skull
    ///
    /// Sprite sheet layout:
    /// Row 1 (8 frames): Jump animation - used for idle and chasing
    /// Row 2 (8 frames): Taking damage animation
    /// Row 3 (5 frames): Death animation
    /// </summary>
    public class BlueSlime : Enemy
    {
        private Texture2D spriteSheet;
        private int frameWidth;
        private int frameHeight;

        // Animation row indices
        private const int JUMP_ROW = 0;
        private const int HURT_ROW = 1;
        private const int DEATH_ROW = 2;

        // Frame counts per row
        private const int JUMP_FRAMES = 8;
        private const int HURT_FRAMES = 8;
        private const int DEATH_FRAMES = 5;

        // Animation timing
        private const int JUMP_FPS = 10;
        private const int HURT_FPS = 12;
        private const int DEATH_FPS = 8;

        // Slimes don't show death particles (they have their own death animation)
        public override bool ShowDeathParticles => false;

        public BlueSlime(Texture2D texture, Vector2 startPosition, float scale = 4f)
            : base(maxHealth: 40)
        {
            spriteSheet = texture;
            Position = startPosition;
            Speed = 60f; // Slower than skull
            Scale = scale;

            // Calculate frame dimensions (8 columns, 3 rows)
            frameWidth = texture.Width / 8;
            frameHeight = texture.Height / 3;

            // Hitbox for the slime - keep small relative to sprite
            HitboxWidth = (int)(16 * scale);
            HitboxHeight = (int)(12 * scale);

            // Create animations for each state
            CreateAnimations();

            Animation = IdleAnimation;
            WanderTarget = Position;

            // Slimes have shorter vision but still chase
            VisionRange = 400f;
            ChaseRange = 500f;
        }

        private void CreateAnimations()
        {
            // Jump/Idle animation (Row 1)
            IdleAnimation = CreateRowAnimation(JUMP_ROW, JUMP_FRAMES, JUMP_FPS);

            // Hurt animation (Row 2)
            HurtAnimation = CreateRowAnimation(HURT_ROW, HURT_FRAMES, HURT_FPS);

            // Death animation (Row 3)
            DeathAnimation = CreateRowAnimation(DEATH_ROW, DEATH_FRAMES, DEATH_FPS);
        }

        private SpriteAnimation CreateRowAnimation(int row, int frameCount, int fps)
        {
            // Create a custom animation that uses specific rows from the sprite sheet
            var animation = new SlimeAnimation(spriteSheet, frameWidth, frameHeight, row, frameCount, fps);
            animation.Scale = Scale;
            animation.Origin = new Vector2(frameWidth / 2f, frameHeight / 2f);
            animation.Position = Position;
            return animation;
        }

        protected override void OnStartHurtAnimation()
        {
            if (HurtAnimation != null)
            {
                Animation = HurtAnimation;
                Animation.IsLooping = false;
                Animation.setFrame(0);
                Animation.Position = Position;
            }
        }

        protected override void OnStartDeathAnimation()
        {
            if (DeathAnimation != null)
            {
                Animation = DeathAnimation;
                Animation.IsLooping = false;
                Animation.setFrame(0);
                Animation.Position = Position;
            }
        }

        public override void Update(GameTime gameTime, Player player, List<Rectangle> collisionBoxes)
        {
            if (IsDeathAnimationComplete)
                return;

            // Check if death animation finished
            if (State == EnemyState.Dying && Animation.CurrentFrameIndex >= Animation.FrameCount - 1)
            {
                IsDeathAnimationComplete = true;
                return;
            }

            base.Update(gameTime, player, collisionBoxes);
        }

        public override Rectangle Hitbox
        {
            get
            {
                // Center the hitbox on the slime, offset up to match sprite body
                return new Rectangle(
                    (int)(Position.X - HitboxWidth / 2),
                    (int)(Position.Y - HitboxHeight / 2 - 5 * Scale), // Offset up to match sprite
                    HitboxWidth,
                    HitboxHeight
                );
            }
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            // Use the SlimeAnimation's custom Draw method
            if (Animation is SlimeAnimation slimeAnim)
            {
                slimeAnim.Draw(spriteBatch);
            }
            else
            {
                base.Draw(spriteBatch);
            }
        }
    }

    /// <summary>
    /// Custom animation class for multi-row sprite sheets
    /// </summary>
    public class SlimeAnimation : SpriteAnimation
    {
        private int rowIndex;
        private int actualFrameWidth;
        private int actualFrameHeight;
        private Rectangle[] rowRectangles;

        public SlimeAnimation(Texture2D texture, int frameWidth, int frameHeight, int row, int frameCount, int fps)
            : base(texture, frameCount, fps)
        {
            rowIndex = row;
            actualFrameWidth = frameWidth;
            actualFrameHeight = frameHeight;

            // Override the rectangles to use the correct row
            rowRectangles = new Rectangle[frameCount];
            for (int i = 0; i < frameCount; i++)
            {
                rowRectangles[i] = new Rectangle(
                    i * frameWidth,
                    row * frameHeight,
                    frameWidth,
                    frameHeight
                );
            }
        }

        public new Rectangle CurrentFrameRectangle => rowRectangles[CurrentFrameIndex % rowRectangles.Length];

        public new void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(
                GetTexture,
                Position,
                CurrentFrameRectangle,
                Color,
                Rotation,
                Origin,
                Scale,
                SpriteEffect,
                LayerDepth
            );
        }
    }
}
