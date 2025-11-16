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
        public SpriteAnimation[] hurtAnimations = new SpriteAnimation[4];
        public SpriteAnimation[] deathAnimations = new SpriteAnimation[4];

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

        public void Update(GameTime gameTime, List<Skull> skulls, ParticleSystem particleSystem, List<Rectangle> collisionBoxes)
        {
            KeyboardState kbState = Keyboard.GetState();
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

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

            // === ATTACK LOGIC ===
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

                    foreach (var skull in skulls.ToArray())
                    {
                        if (attackHitbox.Intersects(skull.Hitbox))
                        {
                            particleSystem.CreateSkullDeathEffect(skull.Position);
                            skulls.Remove(skull);
                        }
                    }
                }

                // End attack when animation finishes
                if (Animation.CurrentFrameIndex == Animation.FrameCount - 1)
                {
                    State = PlayerState.Idle;
                }

                return;
            }

            // === MOVEMENT INPUT ===
            isMoving = false;

            if (kbState.IsKeyDown(Keys.D)) { Direction = Direction.Right; isMoving = true; }
            if (kbState.IsKeyDown(Keys.A)) { Direction = Direction.Left; isMoving = true; }
            if (kbState.IsKeyDown(Keys.W)) { Direction = Direction.Up; isMoving = true; }
            if (kbState.IsKeyDown(Keys.S)) { Direction = Direction.Down; isMoving = true; }

            // === ATTACK TRIGGER ===
            if (kbState.IsKeyDown(Keys.Space))
            {
                State = PlayerState.Attack1;
                Animation = attack1Animations[(int)Direction];
                Animation.IsLooping = false;
                Animation.setFrame(0);
                Animation.Position = position;
                AudioManager.PlaySwingSwordSound(0.25f);
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
        }
    }
}
