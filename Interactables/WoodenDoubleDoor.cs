using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GameProject2
{
    public enum DoorState
    {
        Closed,
        Opening,
        Open
    }

    public class WoodenDoubleDoor
    {
        public Vector2 Position { get; private set; }
        public DoorState State { get; private set; } = DoorState.Closed;
        public string DoorId { get; set; }
        public bool IsOpen => State == DoorState.Open;

        private Texture2D texture;
        private float scale;
        private int frameWidth = 32;
        private int frameHeight = 32;

        // Animation state
        private int currentFrame = 0;
        private float animationTimer = 0f;
        private float frameDuration = 0.1f;
        private int frameCount = 8;

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

        public WoodenDoubleDoor(Texture2D doorTexture, Vector2 position, float scale, string doorId)
        {
            this.texture = doorTexture;
            this.Position = position;
            this.scale = scale;
            this.DoorId = doorId;
            this.currentFrame = 0; // Explicitly set to frame 0 (closed door)
            this.State = DoorState.Closed;
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
            if (State != DoorState.Opening)
                return;

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
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

        public void Draw(SpriteBatch spriteBatch, float layerDepth)
        {
            Rectangle sourceRect = new Rectangle(
                currentFrame * frameWidth,
                0,
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
