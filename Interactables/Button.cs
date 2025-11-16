using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GameProject2
{
    public class Button
    {
        public Vector2 Position;
        private Texture2D texture;
        private int currentFrame = 0;
        private int frameWidth;
        private int frameHeight;
        private float scale;

        public bool IsPressed { get; private set; } = false;
        public bool IsCurrentCheckpoint { get; set; } = false;

        private float animationTimer = 0f;
        private float animationDuration = 0.2f;

        public string CheckpointName { get; set; }

        public Rectangle Hitbox
        {
            get
            {
                return new Rectangle(
                    (int)(Position.X - (frameWidth * scale) / 2),
                    (int)(Position.Y - (frameHeight * scale) / 2),
                    (int)(frameWidth * scale),
                    (int)(frameHeight * scale)
                );
            }
        }

        public Button(Texture2D buttonTexture, Vector2 position, float scale, string checkpointName)
        {
            this.texture = buttonTexture;
            this.Position = position;
            this.scale = scale;
            this.CheckpointName = checkpointName;

            frameWidth = texture.Width / 3;
            frameHeight = texture.Height;
        }

        public void Press()
        {
            if (!IsPressed)
            {
                IsPressed = true;
                animationTimer = 0f;
            }
        }

        public void Reset()
        {
            IsPressed = false;
            IsCurrentCheckpoint = false;
            currentFrame = 0;
            animationTimer = 0f;
        }

        public void Update(GameTime gameTime)
        {
            if (IsPressed && currentFrame < 2)
            {
                animationTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;

                if (animationTimer >= animationDuration / 2f && currentFrame == 0)
                {
                    currentFrame = 1;
                }
                else if (animationTimer >= animationDuration)
                {
                    currentFrame = 2;
                }
            }

            if (IsCurrentCheckpoint)
            {
                currentFrame = 2;
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
                new Vector2(frameWidth / 2f, frameHeight / 2f),
                scale,
                SpriteEffects.None,
                layerDepth
            );
        }
    }
}