using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace GameProject2
{
    public enum SkullState
    {
        Idle,
        Chasing,
        Wandering
    }

    public class Skull
    {
        public SpriteAnimation Animation;
        public Vector2 Position;
        private float speed = 80f;
        private float scale = 1f;

        public SkullState State = SkullState.Idle;
        private float visionRange = 500f;
        private float chaseRange = 600f; // Slightly larger than vision to prevent constant state flipping

        // Wandering behavior
        private Vector2 wanderTarget;
        private float wanderTimer = 0f;
        private float wanderInterval = 2f;
        private Random random = new Random();

        // Offset for each frame of the bobbing animation
        private int[] frameYOffsets = new int[10]
        {
            0,
            5,
            10,
            10,
            5,
            0,
            -5,
            -10,
            -10,
            -5
        };

        public Rectangle Hitbox
        {
            get
            {
                int hitboxWidth = 64;
                int hitboxHeight = 64;
                int currentFrame = Animation.CurrentFrameIndex;
                int yOffset = frameYOffsets[currentFrame];
                return new Rectangle(
                    (int)(Position.X - hitboxWidth / 2),
                    (int)(Position.Y - hitboxHeight / 2 + yOffset),
                    hitboxWidth,
                    hitboxHeight
                );
            }
        }

        public RotatedRectangle RotatedHitbox
        {
            get
            {
                return new RotatedRectangle(Hitbox, 0);
            }
        }

        public Skull(Texture2D texture, int frames, int fps, Vector2 startPosition)
        {
            Animation = new SpriteAnimation(texture, frames, fps);
            Animation.Scale = scale;
            Animation.Origin = new Vector2((texture.Width / (float)frames) / 2f, texture.Height / 2f);
            Animation.LayerDepthOffset = -100f;

            Position = startPosition;
            Animation.Position = Position;

            // Initialize wander target
            wanderTarget = Position;
        }

        public void Update(GameTime gameTime, Player player, List<Rectangle> collisionBoxes)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Calculate distance to player
            float distanceToPlayer = Vector2.Distance(Position, player.Position);

            // Determine if skull can see the player (line of sight check)
            bool canSeePlayer = distanceToPlayer <= visionRange && HasLineOfSight(player.Position, collisionBoxes);

            // State machine
            switch (State)
            {
                case SkullState.Idle:
                    if (canSeePlayer)
                    {
                        State = SkullState.Chasing;
                    }
                    else
                    {
                        // Occasionally start wandering
                        wanderTimer += dt;
                        if (wanderTimer >= wanderInterval)
                        {
                            State = SkullState.Wandering;
                            wanderTimer = 0f;
                        }
                    }
                    break;

                case SkullState.Chasing:
                    if (!canSeePlayer && distanceToPlayer > chaseRange)
                    {
                        State = SkullState.Idle;
                    }
                    else
                    {
                        MoveTowards(player.Position, dt, collisionBoxes);
                    }
                    break;

                case SkullState.Wandering:
                    wanderTimer += dt;

                    if (canSeePlayer)
                    {
                        State = SkullState.Chasing;
                        wanderTimer = 0f;
                    }
                    else if (Vector2.Distance(Position, wanderTarget) < 20f || wanderTimer >= wanderInterval * 3f)
                    {
                        // Reached wander target or took too long, go back to idle
                        State = SkullState.Idle;
                        wanderTimer = 0f;
                    }
                    else
                    {
                        MoveTowards(wanderTarget, dt, collisionBoxes);
                    }
                    break;
            }

            Animation.Position = Position;
            Animation.Update(gameTime);
        }

        private void MoveTowards(Vector2 target, float dt, List<Rectangle> collisionBoxes)
        {
            Vector2 direction = target - Position;
            if (direction != Vector2.Zero)
            {
                direction.Normalize();

                Vector2 newPosition = Position + direction * speed * dt;

                // Create hitbox at new position for collision check
                Rectangle newHitbox = new Rectangle(
                    (int)(newPosition.X - 32),
                    (int)(newPosition.Y - 32),
                    64,
                    64
                );

                // Check collision with walls
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
                else if (State == SkullState.Wandering)
                {
                    // Hit a wall while wandering, pick new target
                    SetRandomWanderTarget();
                }
            }
        }

        private bool HasLineOfSight(Vector2 targetPosition, List<Rectangle> collisionBoxes)
        {
            // Simple raycast - check if any walls intersect the line between skull and player
            Vector2 direction = targetPosition - Position;
            float distance = direction.Length();

            if (distance == 0) return true;

            direction.Normalize();

            // Check points along the line
            int steps = (int)(distance / 10f); // Check every 10 pixels
            for (int i = 0; i < steps; i++)
            {
                Vector2 checkPoint = Position + direction * (i * 10f);
                Point point = new Point((int)checkPoint.X, (int)checkPoint.Y);

                foreach (var wall in collisionBoxes)
                {
                    if (wall.Contains(point))
                    {
                        return false; // Wall blocking line of sight
                    }
                }
            }

            return true;
        }

        public void SetRandomWanderTarget()
        {
            // Pick a random point within a small radius
            float angle = (float)(random.NextDouble() * Math.PI * 2);
            float distance = (float)(random.NextDouble() * 100 + 50); // 50-150 pixels away

            wanderTarget = Position + new Vector2(
                (float)Math.Cos(angle) * distance,
                (float)Math.Sin(angle) * distance
            );
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            Animation.Draw(spriteBatch);
        }
    }
}