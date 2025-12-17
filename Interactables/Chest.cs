using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace GameProject2
{
    public enum ChestState
    {
        Closed,
        Opening,
        Open
    }

    public class Chest
    {
        public Vector2 Position;
        public ChestState State { get; private set; } = ChestState.Closed;
        public List<string> Items { get; private set; } = new List<string>();
        public string ChestId { get; set; }

        private Texture2D texture;
        private int currentFrame = 0;
        private float animationTimer = 0f;
        private float frameDuration = 0.1f;
        private int frameCount = 8;
        private float scale;
        private int tileSize = 16;

        public bool HasBeenOpened { get; private set; } = false;

        public Rectangle Hitbox
        {
            get
            {
                return new Rectangle(
                    (int)(Position.X - (tileSize * scale) / 2),
                    (int)(Position.Y - (tileSize * scale) / 2),
                    (int)(tileSize * scale),
                    (int)(tileSize * scale)
                );
            }
        }

        public Chest(Texture2D chestTexture, Vector2 position, float scale, List<string> items, string chestId)
        {
            this.texture = chestTexture;
            this.Position = position;
            this.scale = scale;
            this.Items = items;
            this.ChestId = chestId;
        }

        public void Open()
        {
            if (State == ChestState.Closed && !HasBeenOpened)
            {
                State = ChestState.Opening;
                currentFrame = 0;
                animationTimer = 0f;
                HasBeenOpened = true;

                AudioManager.PlayChestOpenSound(0.5f);
            }
        }

        public void SetOpened()
        {
            State = ChestState.Open;
            HasBeenOpened = true;
            currentFrame = frameCount - 1; // Last frame of opening animation
        }

        public void Update(GameTime gameTime)
        {
            animationTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (animationTimer >= frameDuration)
            {
                animationTimer = 0f;

                if (State == ChestState.Closed)
                {
                    // Animate idle/closed state
                    currentFrame = (currentFrame + 1) % frameCount;
                }
                else if (State == ChestState.Opening)
                {
                    currentFrame++;

                    if (currentFrame >= frameCount)
                    {
                        currentFrame = frameCount - 1; // Stay on last frame
                        State = ChestState.Open;
                    }
                }
            }
        }

        public void Draw(SpriteBatch spriteBatch, float layerDepth)
        {
            int row;
            switch (State)
            {
                case ChestState.Closed:
                    row = 2; // Bottom row (idle/closed animation)
                    break;
                case ChestState.Opening:
                    row = 0; // Top row (opening animation)
                    break;
                case ChestState.Open:
                    row = 0; // Top row (stay on last frame)
                    break;
                default:
                    row = 2;
                    break;
            }

            Rectangle sourceRect = new Rectangle(
                currentFrame * tileSize,
                row * tileSize,
                tileSize,
                tileSize
            );

            spriteBatch.Draw(
                texture,
                Position,
                sourceRect,
                Color.White,
                0f,
                new Vector2(tileSize / 2f, tileSize / 2f),
                scale,
                SpriteEffects.None,
                layerDepth
            );
        }
    }
}