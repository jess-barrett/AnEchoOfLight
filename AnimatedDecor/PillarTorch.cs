using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GameProject2
{
    public class PillarTorch
    {
        public Vector2 Position;
        private Texture2D tilesetTexture;
        private float scale;
        private int currentFrame = 0;
        private float animationTimer = 0f;
        private float frameDuration = 0.15f;

        // 3 rows of frames (top, middle, bottom)
        private Rectangle[][] frameRows; // [row][frame]
        private int frameCount;
        private int rowCount = 3; // 3 rows tall

        public PillarTorch(Texture2D tileset, Vector2 position, float scale)
        {
            this.tilesetTexture = tileset;
            this.Position = position;
            this.scale = scale;

            // Calculate frames from tileset
            // Columns 14-21 (indices 13-20 in 0-based), Rows 6-8 (indices 5-7)
            frameCount = 8; // 8 columns
            frameRows = new Rectangle[rowCount][];

            int tileSize = 16;
            int startCol = 13; // 14th column (0-based index)
            int startRow = 5; // 6th row (0-based index)

            for (int row = 0; row < rowCount; row++)
            {
                frameRows[row] = new Rectangle[frameCount];

                for (int i = 0; i < frameCount; i++)
                {
                    frameRows[row][i] = new Rectangle(
                        (startCol + i) * tileSize,
                        (startRow + row) * tileSize,
                        tileSize,
                        tileSize
                    );
                }
            }
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
            int tileSize = 16;
            Vector2 origin = new Vector2(8f, 8f); // Center origin (half of 16x16)

            // Draw all 3 rows (top to bottom)
            for (int row = 0; row < rowCount; row++)
            {
                Vector2 tilePosition = new Vector2(
                    Position.X,
                    Position.Y + (row * tileSize * scale)
                );

                // Adjust layer depth slightly for each row so they stack properly
                float rowLayerDepth = layerDepth - (row * 0.0001f);

                spriteBatch.Draw(
                    tilesetTexture,
                    tilePosition,
                    frameRows[row][currentFrame],
                    Color.White,
                    0f,
                    origin,
                    scale,
                    SpriteEffects.None,
                    rowLayerDepth
                );
            }
        }
    }
}