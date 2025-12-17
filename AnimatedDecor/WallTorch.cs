using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GameProject2
{
    public enum TorchMount
    {
        Left,
        Front,
        Right
    }

    public enum TorchColor
    {
        Red = 0,
        Blue = 1,
        Orange = 2,
        Green = 3
    }

    public class WallTorch
    {
        public Vector2 Position;
        private Texture2D texture;
        private float scale;
        private int currentFrame = 0;
        private float animationTimer = 0f;
        private float frameDuration = 0.1f;

        private const int TileSize = 16;
        private const int FrameCount = 8;

        private TorchMount mount;
        private TorchColor color;
        private Rectangle[] frames;

        public WallTorch(Texture2D torchTexture, Vector2 position, float scale, TorchMount mount, TorchColor color = TorchColor.Red)
        {
            this.texture = torchTexture;
            this.Position = position;
            this.scale = scale;
            this.mount = mount;
            this.color = color;

            // Calculate frame rectangles based on mount and color
            frames = new Rectangle[FrameCount];
            CalculateFrames();
        }

        private void CalculateFrames()
        {
            int startCol;
            int row;

            switch (mount)
            {
                case TorchMount.Front:
                    // Front: rows 0-3 (one per color), cols 8-15
                    startCol = 8;
                    row = (int)color;
                    break;
                case TorchMount.Left:
                    // Left: rows 4-7 (one per color), cols 0-7
                    startCol = 0;
                    row = 4 + (int)color;
                    break;
                case TorchMount.Right:
                    // Right: rows 4-7 (one per color), cols 8-15
                    startCol = 8;
                    row = 4 + (int)color;
                    break;
                default:
                    startCol = 8;
                    row = 0;
                    break;
            }

            for (int i = 0; i < FrameCount; i++)
            {
                frames[i] = new Rectangle(
                    (startCol + i) * TileSize,
                    row * TileSize,
                    TileSize,
                    TileSize
                );
            }
        }

        public void Update(GameTime gameTime)
        {
            animationTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (animationTimer >= frameDuration)
            {
                animationTimer = 0f;
                currentFrame = (currentFrame + 1) % FrameCount;
            }
        }

        public void Draw(SpriteBatch spriteBatch, float layerDepth)
        {
            Vector2 origin = new Vector2(TileSize / 2f, TileSize / 2f);

            spriteBatch.Draw(
                texture,
                Position,
                frames[currentFrame],
                Color.White,
                0f,
                origin,
                scale,
                SpriteEffects.None,
                layerDepth
            );
        }
    }
}
