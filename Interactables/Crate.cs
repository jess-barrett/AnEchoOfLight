using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GameProject2
{
    public enum CrateType
    {
        TallCrate,      // 2-high crate (row 8-9, col 15) - 16x32
        CrateCluster1,  // Cluster at row 9, col 12 - 16x16
        CrateCluster2   // Cluster at row 9, col 11 - 16x16
    }

    public class Crate
    {
        private Texture2D texture;
        public Vector2 Position { get; private set; }
        public string CrateId { get; set; }
        public bool IsDestroyed { get; private set; } = false;
        public CrateType Type { get; private set; }

        private float scale;
        private Rectangle sourceRect;
        private int hitboxWidth;
        private int hitboxHeight;

        // Health system - crates take multiple hits
        public int MaxHealth { get; private set; }
        public int CurrentHealth { get; private set; }

        // Damage flash effect
        private float damageFlashTimer = 0f;
        private float damageFlashDuration = 0.15f;
        private bool showDamageFlash = false;

        private const int TileSize = 16;

        public Rectangle Hitbox
        {
            get
            {
                return new Rectangle(
                    (int)(Position.X - hitboxWidth / 2),
                    (int)(Position.Y - hitboxHeight / 2),
                    hitboxWidth,
                    hitboxHeight
                );
            }
        }

        public Crate(Texture2D texture, Vector2 position, CrateType type, float scale = 4f, int health = 2)
        {
            this.texture = texture;
            this.Position = position;
            this.Type = type;
            this.scale = scale;
            this.MaxHealth = health;
            this.CurrentHealth = health;

            // Set source rectangle and hitbox based on crate type
            switch (type)
            {
                case CrateType.TallCrate:
                    // Row 8-9, col 15 - 2-high stacked crate
                    sourceRect = new Rectangle(15 * TileSize, 8 * TileSize, TileSize, TileSize * 2);
                    hitboxWidth = (int)(TileSize * scale);
                    hitboxHeight = (int)(TileSize * 2 * scale);
                    break;

                case CrateType.CrateCluster1:
                    // Row 9, col 11
                    sourceRect = new Rectangle(11 * TileSize, 9 * TileSize, TileSize, TileSize);
                    hitboxWidth = (int)(TileSize * scale);
                    hitboxHeight = (int)(TileSize * scale);
                    break;

                case CrateType.CrateCluster2:
                    // Row 9, col 12
                    sourceRect = new Rectangle(12 * TileSize, 9 * TileSize, TileSize, TileSize);
                    hitboxWidth = (int)(TileSize * scale);
                    hitboxHeight = (int)(TileSize * scale);
                    break;
            }
        }

        public bool TakeDamage(int amount = 1)
        {
            if (IsDestroyed) return false;

            CurrentHealth -= amount;
            damageFlashTimer = damageFlashDuration;
            showDamageFlash = true;

            if (CurrentHealth <= 0)
            {
                IsDestroyed = true;
                return true; // Crate was destroyed
            }

            return false; // Crate was hit but not destroyed
        }

        public void Update(GameTime gameTime)
        {
            if (IsDestroyed) return;

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Update damage flash
            if (showDamageFlash)
            {
                damageFlashTimer -= dt;
                if (damageFlashTimer <= 0)
                {
                    showDamageFlash = false;
                }
            }
        }

        public void Draw(SpriteBatch spriteBatch, float layerDepth)
        {
            if (IsDestroyed) return;

            Color drawColor = showDamageFlash ? Color.Red : Color.White;

            // Calculate origin based on sprite size
            Vector2 origin = new Vector2(sourceRect.Width / 2f, sourceRect.Height / 2f);

            spriteBatch.Draw(
                texture,
                Position,
                sourceRect,
                drawColor,
                0f,
                origin,
                scale,
                SpriteEffects.None,
                layerDepth
            );
        }
    }
}
