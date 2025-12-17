using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GameProject2.Content.Player
{
    public class AbilityIcon
    {
        private Texture2D iconSheet;
        private int iconRow;           // Which row in the sprite sheet (0-indexed)
        private int miniIconColumn;    // Column in row 11 for the mini/cooldown icon
        private string keyLabel;       // Text label (e.g., "LMB", "Space")

        private int frameWidth = 16;   // Width of each frame
        private int frameHeight = 16;  // Height of each frame
        private int framesPerRow = 16; // 16 frames for sheen animation
        private int miniIconRow = 11;  // Row 11 is the mini icons row

        // Animation state
        private int currentFrame = 0;
        private float animationTimer = 0f;
        private float frameDuration = 0.05f; // Fast sheen animation

        // Cooldown state
        private float cooldownDuration;
        private float cooldownTimer = 0f;
        public bool IsOnCooldown => cooldownTimer > 0f;
        public float CooldownProgress => cooldownDuration > 0 ? cooldownTimer / cooldownDuration : 0f;

        // Unlock state
        public bool IsUnlocked { get; set; } = true;

        // Active state (for abilities being held like sprint/attack)
        public bool IsActive { get; set; } = false;

        public AbilityIcon(Texture2D sheet, int row, int miniColumn, string label, float cooldown = 0f)
        {
            iconSheet = sheet;
            iconRow = row;
            miniIconColumn = miniColumn;
            keyLabel = label;
            cooldownDuration = cooldown;
        }

        public void StartCooldown()
        {
            if (cooldownDuration > 0)
            {
                cooldownTimer = cooldownDuration;
            }
        }

        public void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Update cooldown
            if (cooldownTimer > 0)
            {
                cooldownTimer -= dt;
                if (cooldownTimer < 0) cooldownTimer = 0;
            }

            // Update sheen animation (only when not on cooldown)
            if (!IsOnCooldown)
            {
                animationTimer += dt;
                if (animationTimer >= frameDuration)
                {
                    animationTimer = 0f;
                    currentFrame = (currentFrame + 1) % framesPerRow;
                }
            }
        }

        public void Draw(SpriteBatch spriteBatch, SpriteFont font, Vector2 position, float scale)
        {
            if (!IsUnlocked)
                return;

            Rectangle sourceRect;
            Color drawColor = Color.White;

            if (IsOnCooldown)
            {
                // Draw mini/cooldown icon from row 11
                sourceRect = new Rectangle(
                    miniIconColumn * frameWidth,
                    miniIconRow * frameHeight,
                    frameWidth,
                    frameHeight
                );
                drawColor = Color.Gray;
            }
            else if (IsActive)
            {
                // Draw mini icon when ability is actively being used
                sourceRect = new Rectangle(
                    miniIconColumn * frameWidth,
                    miniIconRow * frameHeight,
                    frameWidth,
                    frameHeight
                );
                drawColor = Color.White;
            }
            else
            {
                // Draw animated sheen icon
                sourceRect = new Rectangle(
                    currentFrame * frameWidth,
                    iconRow * frameHeight,
                    frameWidth,
                    frameHeight
                );
            }

            // Draw the icon
            spriteBatch.Draw(
                iconSheet,
                position,
                sourceRect,
                drawColor,
                0f,
                Vector2.Zero,
                scale,
                SpriteEffects.None,
                0f
            );

            // Draw key label below the icon
            Vector2 labelSize = font.MeasureString(keyLabel);
            float labelScale = 0.5f;
            Vector2 labelPos = new Vector2(
                position.X + (frameWidth * scale / 2) - (labelSize.X * labelScale / 2),
                position.Y + frameHeight * scale + 2
            );

            // Draw label outline
            Color outlineColor = Color.Black;
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    if (x != 0 || y != 0)
                    {
                        spriteBatch.DrawString(
                            font,
                            keyLabel,
                            labelPos + new Vector2(x, y),
                            outlineColor,
                            0f,
                            Vector2.Zero,
                            labelScale,
                            SpriteEffects.None,
                            0f
                        );
                    }
                }
            }

            // Draw label text
            Color labelColor = IsOnCooldown ? Color.Gray : Color.White;
            spriteBatch.DrawString(
                font,
                keyLabel,
                labelPos,
                labelColor,
                0f,
                Vector2.Zero,
                labelScale,
                SpriteEffects.None,
                0f
            );

            // Draw cooldown overlay if on cooldown
            if (IsOnCooldown && cooldownDuration > 0)
            {
                // Draw remaining time
                string timeText = cooldownTimer.ToString("0.0");
                Vector2 timeSize = font.MeasureString(timeText);
                float timeScale = 0.6f;
                Vector2 timePos = new Vector2(
                    position.X + (frameWidth * scale / 2) - (timeSize.X * timeScale / 2),
                    position.Y + (frameHeight * scale / 2) - (timeSize.Y * timeScale / 2)
                );

                spriteBatch.DrawString(
                    font,
                    timeText,
                    timePos,
                    Color.White,
                    0f,
                    Vector2.Zero,
                    timeScale,
                    SpriteEffects.None,
                    0f
                );
            }
        }
    }
}
