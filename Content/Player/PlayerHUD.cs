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
        private Texture2D iconsSpriteSheet;
        private Texture2D redPotionIcon;
        private Texture2D redMiniPotionIcon;

        private int heartFrameWidth;
        private int heartFrameHeight;
        private int totalHeartFrames = 5;

        private int coinFrameWidth;
        private int coinFrameHeight;

        public int MaxHealth { get; set; } = 5;
        public int CurrentHealth { get; set; } = 5;
        public int CoinCount { get; set; } = 0;

        // Damage animation
        private bool isDamageAnimating = false;
        private float damageAnimTimer = 0f;
        private float damageAnimDuration = 0.5f; // Total animation time
        private int damageAnimFrame = 0;

        // Ability icons
        private AbilityIcon attack1Icon;
        private AbilityIcon attack2Icon;
        private AbilityIcon sprintIcon;
        private AbilityIcon dashIcon;

        // Track unlock state
        public bool HasAttack2 { get; set; } = false;
        public bool HasDash { get; set; } = false;

        // Cooldown tracking
        private const float Attack2Cooldown = 3f;
        private const float DashCooldown = 2f;

        // Potion inventory (defaults set by SaveData or initial load)
        public int RedPotionCount { get; set; } = 0;
        public int RedMiniPotionCount { get; set; } = 0;

        public void LoadContent(ContentManager content)
        {
            font = content.Load<SpriteFont>("InstructionFont");
            heartSpriteSheet = content.Load<Texture2D>("Player/Heart");
            coinSpriteSheet = content.Load<Texture2D>("Interactables/Coin");
            iconsSpriteSheet = content.Load<Texture2D>("HUD/Icons");
            redPotionIcon = content.Load<Texture2D>("HUD/RedPotionIcon");
            redMiniPotionIcon = content.Load<Texture2D>("HUD/RedMiniPotionIcon");

            heartFrameWidth = heartSpriteSheet.Width / totalHeartFrames;
            heartFrameHeight = heartSpriteSheet.Height;

            int coinFrames = 8;
            coinFrameWidth = coinSpriteSheet.Width / coinFrames;
            coinFrameHeight = coinSpriteSheet.Height;

            // Create ability icons
            // Row 5 (0-indexed): Attack1, mini icon col 2, LMB, no cooldown
            attack1Icon = new AbilityIcon(iconsSpriteSheet, 5, 2, "LMB", 0f);

            // Row 4: Attack2, mini icon col 8, RMB, 3s cooldown
            attack2Icon = new AbilityIcon(iconsSpriteSheet, 4, 8, "RMB", Attack2Cooldown);
            attack2Icon.IsUnlocked = false;

            // Row 9: Sprint, mini icon col 9, L-Shift, no cooldown
            sprintIcon = new AbilityIcon(iconsSpriteSheet, 9, 9, "L-Shift", 0f);

            // Row 0: Dash, mini icon col 10, Space, 0.6s cooldown
            dashIcon = new AbilityIcon(iconsSpriteSheet, 0, 10, "Space", DashCooldown);
            dashIcon.IsUnlocked = false;
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

            // Update unlock states
            attack2Icon.IsUnlocked = HasAttack2;
            dashIcon.IsUnlocked = HasDash;

            // Update ability icons
            attack1Icon.Update(gameTime);
            attack2Icon.Update(gameTime);
            sprintIcon.Update(gameTime);
            dashIcon.Update(gameTime);
        }

        public void Draw(SpriteBatch spriteBatch, Viewport viewport)
        {
            int padding = 20;
            float scale = 3f;

            // Draw hearts (top left)
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

            // Draw coins (below hearts)
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

            // Draw potion inventory (row above ability icons)
            float potionScale = 2.5f;
            int potionSpacing = 10;
            float potionRowY = viewport.Height - padding - 16 * 3f - 20 - 55; // Above ability icons row

            float potionX = padding;

            // Red Potion (Q)
            DrawPotionSlot(spriteBatch, redPotionIcon, new Vector2(potionX, potionRowY), potionScale, RedPotionCount, "Q");
            potionX += redPotionIcon.Width * potionScale + potionSpacing;

            // Red Mini Potion (F)
            DrawPotionSlot(spriteBatch, redMiniPotionIcon, new Vector2(potionX, potionRowY), potionScale, RedMiniPotionCount, "F");

            // Draw ability icons (bottom left)
            float iconScale = 3f;
            int iconSize = 16;
            int iconSpacing = 10;
            float iconY = viewport.Height - padding - iconSize * iconScale - 20; // 20 extra for label

            float currentX = padding;

            // Attack1 (always shown)
            attack1Icon.Draw(spriteBatch, font, new Vector2(currentX, iconY), iconScale);
            currentX += iconSize * iconScale + iconSpacing;

            // Attack2 (only if unlocked)
            if (HasAttack2)
            {
                attack2Icon.Draw(spriteBatch, font, new Vector2(currentX, iconY), iconScale);
                currentX += iconSize * iconScale + iconSpacing;
            }

            // Sprint (always shown)
            sprintIcon.Draw(spriteBatch, font, new Vector2(currentX, iconY), iconScale);
            currentX += iconSize * iconScale + iconSpacing;

            // Dash (only if unlocked)
            if (HasDash)
            {
                dashIcon.Draw(spriteBatch, font, new Vector2(currentX, iconY), iconScale);
            }
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

        // Called when player uses Attack2
        public void TriggerAttack2Cooldown()
        {
            attack2Icon.StartCooldown();
        }

        // Called when player uses Dash
        public void TriggerDashCooldown()
        {
            dashIcon.StartCooldown();
        }

        // Set active states for abilities being held
        public void SetAttack1Active(bool active) => attack1Icon.IsActive = active;
        public void SetSprintActive(bool active) => sprintIcon.IsActive = active;

        // Draw a potion inventory slot
        private void DrawPotionSlot(SpriteBatch spriteBatch, Texture2D icon, Vector2 position, float scale, int count, string keyLabel)
        {
            Color iconColor = count > 0 ? Color.White : Color.Gray;

            // Draw potion icon
            spriteBatch.Draw(
                icon,
                position,
                null,
                iconColor,
                0f,
                Vector2.Zero,
                scale,
                SpriteEffects.None,
                0f
            );

            // Draw count
            string countText = $"x{count}";
            Vector2 countPos = new Vector2(
                position.X + icon.Width * scale - 5,
                position.Y + (icon.Height * scale / 2) - 8
            );

            // Count outline
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    if (x != 0 || y != 0)
                    {
                        spriteBatch.DrawString(font, countText, countPos + new Vector2(x, y), Color.Black, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
                    }
                }
            }
            spriteBatch.DrawString(font, countText, countPos, iconColor, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);

            // Draw key label below
            float labelScale = 0.5f;
            Vector2 labelSize = font.MeasureString(keyLabel);
            Vector2 labelPos = new Vector2(
                position.X + (icon.Width * scale / 2) - (labelSize.X * labelScale / 2),
                position.Y + icon.Height * scale - 5
            );

            // Label outline
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    if (x != 0 || y != 0)
                    {
                        spriteBatch.DrawString(font, keyLabel, labelPos + new Vector2(x, y), Color.Black, 0f, Vector2.Zero, labelScale, SpriteEffects.None, 0f);
                    }
                }
            }
            spriteBatch.DrawString(font, keyLabel, labelPos, iconColor, 0f, Vector2.Zero, labelScale, SpriteEffects.None, 0f);
        }

        // Add potions to inventory
        public void AddRedPotion(int amount = 1) => RedPotionCount += amount;
        public void AddRedMiniPotion(int amount = 1) => RedMiniPotionCount += amount;

        // Use potions (returns heal amount if successful, 0 if failed)
        public int UseRedPotion()
        {
            if (RedPotionCount > 0 && CurrentHealth < MaxHealth)
            {
                RedPotionCount--;
                return 2; // Heal amount applied after animation
            }
            return 0;
        }

        public int UseRedMiniPotion()
        {
            if (RedMiniPotionCount > 0 && CurrentHealth < MaxHealth)
            {
                RedMiniPotionCount--;
                return 1; // Heal amount applied after animation
            }
            return 0;
        }
    }
}
