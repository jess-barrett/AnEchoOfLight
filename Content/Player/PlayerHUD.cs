using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace GameProject2.Content.Player
{
    public class PlayerHUD
    {
        private SpriteFont font;
        private Texture2D heartSpriteSheet;
        private Texture2D coinSpriteSheet;

        private int heartFrameWidth;
        private int heartFrameHeight;
        private int totalHeartFrames = 5;

        private int coinFrameWidth;
        private int coinFrameHeight;

        public int MaxHealth { get; set; } = 3;
        public int CurrentHealth { get; set; } = 3;
        public int CoinCount { get; set; } = 0;

        // Damage animation
        private bool isDamageAnimating = false;
        private float damageAnimTimer = 0f;
        private float damageAnimDuration = 0.5f; // Total animation time
        private int damageAnimFrame = 0;

        public void LoadContent(ContentManager content)
        {
            font = content.Load<SpriteFont>("InstructionFont");
            heartSpriteSheet = content.Load<Texture2D>("Player/Heart");
            coinSpriteSheet = content.Load<Texture2D>("Interactables/Coin");

            heartFrameWidth = heartSpriteSheet.Width / totalHeartFrames;
            heartFrameHeight = heartSpriteSheet.Height;

            int coinFrames = 8;
            coinFrameWidth = coinSpriteSheet.Width / coinFrames;
            coinFrameHeight = coinSpriteSheet.Height;
        }

        public void Update(GameTime gameTime)
        {
            if (isDamageAnimating)
            {
                float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
                damageAnimTimer += dt;

                float frameTime = damageAnimDuration / 4f;
                damageAnimFrame = 1 + (int)(damageAnimTimer / frameTime);

                if (damageAnimTimer >= damageAnimDuration)
                {
                    isDamageAnimating = false;
                    damageAnimTimer = 0f;
                    damageAnimFrame = 0;
                }
            }
        }

        public void Draw(SpriteBatch spriteBatch, Viewport viewport)
        {
            int padding = 20;
            float scale = 3f;

            for (int i = 0; i < MaxHealth; i++)
            {
                Vector2 heartPosition = new Vector2(padding + i * (heartFrameWidth * scale + 10), padding);

                int frameIndex;

                if (i == CurrentHealth && isDamageAnimating)
                {
                    frameIndex = damageAnimFrame;
                }
                else
                {
                    frameIndex = i < CurrentHealth ? 0 : 4;
                }

                Rectangle sourceRect = new Rectangle(
                    frameIndex * heartFrameWidth,
                    0,
                    heartFrameWidth,
                    heartFrameHeight
                );

                spriteBatch.Draw(
                    heartSpriteSheet,
                    heartPosition,
                    sourceRect,
                    Color.White,
                    0f,
                    Vector2.Zero,
                    scale,
                    SpriteEffects.None,
                    0f
                );
            }

            Vector2 coinPosition = new Vector2(padding, padding + heartFrameHeight * scale + 20);

            int cropBottom = 4;
            Rectangle coinSourceRect = new Rectangle(
                0,
                0,
                coinFrameWidth,
                coinFrameHeight - cropBottom
            );

            spriteBatch.Draw(
                coinSpriteSheet,
                coinPosition + new Vector2(0, 6),
                coinSourceRect,
                Color.White,
                0f,
                Vector2.Zero,
                scale,
                SpriteEffects.None,
                0f
            );

            Vector2 textPosition = coinPosition + new Vector2(coinFrameWidth * scale + 10, 5);
            spriteBatch.DrawString(font, $"x {CoinCount}", textPosition, Color.White);
        }

        public void TakeDamage(int amount = 1)
        {
            CurrentHealth = MathHelper.Max(0, CurrentHealth - amount);

            isDamageAnimating = true;
            damageAnimTimer = 0f;
        }

        public void Heal(int amount)
        {
            CurrentHealth = Math.Min(CurrentHealth + amount, MaxHealth);
            System.Diagnostics.Debug.WriteLine($"Healed {amount} hearts. Current health: {CurrentHealth}/{MaxHealth}");
        }

        public void AddCoin(int amount = 1)
        {
            CoinCount += amount;
        }
    }
}