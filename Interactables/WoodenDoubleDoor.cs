using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GameProject2
{
    public enum DoorState
    {
        Closed,
        Opening,
        Open,
        WaitingToClose,
        Closing
    }

    public class WoodenDoubleDoor
    {
        public Vector2 Position { get; private set; }
        public DoorState State { get; private set; } = DoorState.Closed;
        public string DoorId { get; set; }
        public bool IsOpen => State == DoorState.Open || State == DoorState.WaitingToClose;

        private Texture2D texture;
        private float scale;
        private int frameWidth = 32;
        private int frameHeight = 32;

        // Animation state
        private int currentFrame = 0;
        private float animationTimer = 0f;
        private float frameDuration = 0.1f;
        private int frameCount = 8;

        // StartOpen feature - door starts open and closes after delay
        public bool StartOpen { get; set; } = false;
        private float closeDelay = 1.5f; // Seconds to wait before closing
        private float closeDelayTimer = 0f;

        // Collision rectangle (provided from Tiled)
        public Rectangle CollisionRect { get; set; }

        public Rectangle Hitbox
        {
            get
            {
                return new Rectangle(
                    (int)Position.X,
                    (int)Position.Y,
                    (int)(frameWidth * scale),
                    (int)(frameHeight * scale)
                );
            }
        }

        public WoodenDoubleDoor(Texture2D doorTexture, Vector2 position, float scale, string doorId, bool startOpen = false)
        {
            this.texture = doorTexture;
            this.Position = position;
            this.scale = scale;
            this.DoorId = doorId;
            this.StartOpen = startOpen;

            if (startOpen)
            {
                // Start in open state, waiting to close
                this.State = DoorState.WaitingToClose;
                this.currentFrame = frameCount - 1; // Last frame (fully open)
                this.closeDelayTimer = closeDelay;
            }
            else
            {
                this.currentFrame = 0; // Frame 0 (closed door)
                this.State = DoorState.Closed;
            }
        }

        public void SetOpen()
        {
            State = DoorState.Open;
            currentFrame = frameCount - 1;
        }

        public void Open()
        {
            if (State == DoorState.Closed)
            {
                State = DoorState.Opening;
                currentFrame = 0;
                animationTimer = 0f;
            }
        }

        public void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Handle waiting to close (StartOpen delay)
            if (State == DoorState.WaitingToClose)
            {
                closeDelayTimer -= dt;
                if (closeDelayTimer <= 0)
                {
                    State = DoorState.Closing;
                    currentFrame = 0; // Start closing animation from frame 0
                    animationTimer = 0f;
                }
                return;
            }

            // Handle closing animation (row 1)
            if (State == DoorState.Closing)
            {
                animationTimer += dt;
                if (animationTimer >= frameDuration)
                {
                    animationTimer = 0f;
                    currentFrame++;

                    if (currentFrame >= frameCount)
                    {
                        currentFrame = 0; // Reset to closed frame
                        State = DoorState.Closed;
                    }
                }
                return;
            }

            // Handle opening animation (row 0)
            if (State == DoorState.Opening)
            {
                animationTimer += dt;
                if (animationTimer >= frameDuration)
                {
                    animationTimer = 0f;
                    currentFrame++;

                    if (currentFrame >= frameCount)
                    {
                        currentFrame = frameCount - 1;
                        State = DoorState.Open;
                    }
                }
            }
        }

        public void Draw(SpriteBatch spriteBatch, float layerDepth)
        {
            // Row 0 = opening animation, Row 1 = closing animation
            int row = (State == DoorState.Closing) ? 1 : 0;

            // For WaitingToClose and Open states, show last frame of row 0 (fully open)
            int frameToShow = currentFrame;
            if (State == DoorState.WaitingToClose || State == DoorState.Open)
            {
                frameToShow = frameCount - 1;
                row = 0;
            }
            else if (State == DoorState.Closed)
            {
                frameToShow = 0;
                row = 0;
            }

            Rectangle sourceRect = new Rectangle(
                frameToShow * frameWidth,
                row * frameHeight,
                frameWidth,
                frameHeight
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
    }
}
