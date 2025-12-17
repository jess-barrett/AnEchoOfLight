using GameProject2.Enemies;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GameProject2.Content.Player
{
    public class BossHealthBar
    {
        private Texture2D pixelTexture;
        private SpriteFont font;

        private const int BarWidth = 400;
        private const int BarHeight = 20;
        private const int BorderThickness = 3;

        private Color borderColor = Color.Black;
        private Color backgroundColor = new Color(40, 40, 40);
        private Color healthColorFull = Color.Red;
        private Color healthColorLow = Color.DarkRed;

        private string bossName;

        public BossHealthBar(GraphicsDevice graphicsDevice, SpriteFont font, string bossName = "ORC BOSS")
        {
            this.font = font;
            this.bossName = bossName;

            // Create a 1x1 white pixel texture for drawing rectangles
            pixelTexture = new Texture2D(graphicsDevice, 1, 1);
            pixelTexture.SetData(new[] { Color.White });
        }

        public void Draw(SpriteBatch spriteBatch, Viewport viewport, OrcBoss boss)
        {
            if (boss == null || boss.IsDeathAnimationComplete) return;

            float healthPercent = (float)boss.CurrentHealth / boss.MaxHealth;

            // Position at top center of screen (moved down for better visibility)
            int barX = (viewport.Width - BarWidth) / 2;
            int barY = 70;

            // Draw boss name
            Vector2 nameSize = font.MeasureString(bossName);
            float nameScale = 1.2f;
            Vector2 namePos = new Vector2(
                (viewport.Width - nameSize.X * nameScale) / 2,
                barY - nameSize.Y * nameScale - 5
            );

            // Name outline
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    if (x != 0 || y != 0)
                    {
                        spriteBatch.DrawString(
                            font,
                            bossName,
                            namePos + new Vector2(x * 2, y * 2),
                            Color.Black,
                            0f,
                            Vector2.Zero,
                            nameScale,
                            SpriteEffects.None,
                            0f
                        );
                    }
                }
            }

            spriteBatch.DrawString(
                font,
                bossName,
                namePos,
                Color.White,
                0f,
                Vector2.Zero,
                nameScale,
                SpriteEffects.None,
                0f
            );

            // Draw border
            spriteBatch.Draw(
                pixelTexture,
                new Rectangle(
                    barX - BorderThickness,
                    barY - BorderThickness,
                    BarWidth + BorderThickness * 2,
                    BarHeight + BorderThickness * 2
                ),
                borderColor
            );

            // Draw background
            spriteBatch.Draw(
                pixelTexture,
                new Rectangle(barX, barY, BarWidth, BarHeight),
                backgroundColor
            );

            // Draw health bar
            int healthWidth = (int)(BarWidth * healthPercent);
            Color healthColor = Color.Lerp(healthColorLow, healthColorFull, healthPercent);

            spriteBatch.Draw(
                pixelTexture,
                new Rectangle(barX, barY, healthWidth, BarHeight),
                healthColor
            );

            // Draw health text
            string healthText = $"{boss.CurrentHealth} / {boss.MaxHealth}";
            Vector2 textSize = font.MeasureString(healthText);
            float textScale = 0.7f;
            Vector2 textPos = new Vector2(
                (viewport.Width - textSize.X * textScale) / 2,
                barY + (BarHeight - textSize.Y * textScale) / 2
            );

            // Text outline
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    if (x != 0 || y != 0)
                    {
                        spriteBatch.DrawString(
                            font,
                            healthText,
                            textPos + new Vector2(x, y),
                            Color.Black,
                            0f,
                            Vector2.Zero,
                            textScale,
                            SpriteEffects.None,
                            0f
                        );
                    }
                }
            }

            spriteBatch.DrawString(
                font,
                healthText,
                textPos,
                Color.White,
                0f,
                Vector2.Zero,
                textScale,
                SpriteEffects.None,
                0f
            );
        }
    }
}
