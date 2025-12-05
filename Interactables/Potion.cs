using GameProject2.Content.Player;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GameProject2
{
    public enum PotionType
    {
        YellowMini,
        RedMini,      // +1 heart
        BlueMini,
        Red,          // +2 hearts
        Pink,
        Green,
        White,
        Orange,
        Blue
    }

    public class Potion
    {
        public Vector2 Position;
        public PotionType Type;
        public bool IsCollected { get; set; } = false;
        public bool RequiresInteraction { get; set; } = false;

        private Texture2D texture;
        private int currentFrame = 0;
        private float animationTimer = 0f;
        private float frameDuration = 0.1f;
        private int frameCount = 16;
        private float scale;
        private int tileSize = 16;

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

        public Potion(Texture2D potionTexture, Vector2 position, PotionType type, float scale, bool requiresInteraction = false)
        {
            this.texture = potionTexture;
            this.Position = position;
            this.Type = type;
            this.scale = scale;
            this.RequiresInteraction = requiresInteraction;
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
            int row = (int)Type;

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

        // Apply potion effect
        public void ApplyEffect(PlayerHUD hud)
        {
            switch (Type)
            {
                case PotionType.RedMini:
                    hud.Heal(1); // +1 heart
                    break;

                case PotionType.Red:
                    hud.Heal(2); // +2 hearts
                    break;

                case PotionType.YellowMini:
                    // TODO: Define effect (maybe speed boost?)
                    break;

                case PotionType.BlueMini:
                    // TODO: Define effect (maybe stamina/mana?)
                    break;

                case PotionType.Pink:
                    // TODO: Define effect
                    break;

                case PotionType.Green:
                    // TODO: Define effect
                    break;

                case PotionType.White:
                    // TODO: Define effect
                    break;

                case PotionType.Orange:
                    // TODO: Define effect
                    break;

                case PotionType.Blue:
                    // TODO: Define effect
                    break;
            }
        }
    }
}