using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace GameProject2.Enemies
{
    /// <summary>
    /// Skull enemy - a floating skull that chases the player
    /// Has 100 health and a bobbing animation
    /// </summary>
    public class Skull : Enemy
    {
        // Offset for each frame of the bobbing animation
        private int[] frameYOffsets = new int[10]
        {
            0, 5, 10, 10, 5, 0, -5, -10, -10, -5
        };

        public override Rectangle Hitbox
        {
            get
            {
                int currentFrame = Animation.CurrentFrameIndex;
                int yOffset = frameYOffsets[currentFrame % frameYOffsets.Length];
                return new Rectangle(
                    (int)(Position.X - HitboxWidth / 2),
                    (int)(Position.Y - HitboxHeight / 2 + yOffset),
                    HitboxWidth,
                    HitboxHeight
                );
            }
        }

        public Skull(Texture2D texture, int frames, int fps, Vector2 startPosition)
            : base(maxHealth: 100)
        {
            Position = startPosition;
            Speed = 80f;
            Scale = 1f;
            HitboxWidth = 64;
            HitboxHeight = 64;

            // Create the single animation (skull only has idle/move animation)
            IdleAnimation = new SpriteAnimation(texture, frames, fps);
            IdleAnimation.Scale = Scale;
            IdleAnimation.Origin = new Vector2((texture.Width / (float)frames) / 2f, texture.Height / 2f);
            IdleAnimation.LayerDepthOffset = -100f;
            IdleAnimation.Position = Position;

            // Skull uses the same animation for everything (no hurt/death anims in original)
            // We'll use the same animation but it will be removed on death
            HurtAnimation = IdleAnimation;
            DeathAnimation = IdleAnimation;

            Animation = IdleAnimation;
            WanderTarget = Position;
        }

        protected override void OnStartHurtAnimation()
        {
            // Skull doesn't have a separate hurt animation, so just flash or continue
            // Animation stays the same
        }

        protected override void OnStartDeathAnimation()
        {
            // Skull dies immediately (no death animation), mark as complete
            IsDeathAnimationComplete = true;
        }

        public override void Update(GameTime gameTime, Player player, List<Rectangle> collisionBoxes)
        {
            // Skip update if death animation is complete
            if (IsDeathAnimationComplete)
                return;

            base.Update(gameTime, player, collisionBoxes);
        }
    }
}
