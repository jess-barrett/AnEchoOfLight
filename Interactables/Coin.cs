using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GameProject2
{
    public class Coin
    {
        public Vector2 Position;
        public bool IsCollected { get; set; } = false;
        public bool RequiresInteraction { get; set; } = false; // NEW

        private Texture2D texture;
        private int currentFrame = 0;
        private float animationTimer = 0f;
        private float frameDuration = 0.1f;
        private int frameCount;
        private int frameWidth;
        private int frameHeight;
        private float scale;

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

        public Coin(Texture2D coinTexture, Vector2 position, int frameCount, int fps, bool requiresInteraction = false)
        {
            this.texture = coinTexture;
            this.Position = position;
            this.frameCount = frameCount;
            this.frameDuration = 1f / fps;
            this.RequiresInteraction = requiresInteraction;

            frameWidth = texture.Width / frameCount;
            frameHeight = texture.Height;
            scale = 4f;
        }

        public void Update(GameTime gameTime)
        {
            animationTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (animationTimer >= frameDuration)
            {
                animationTimer = 0f;
                currentFrame = (currentFrame + 1) % frameCount;
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