using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GameProject2
{
    public class Vase
    {
        public SpriteAnimation Animation;
        public Vector2 Position;
        public string VaseId { get; set; }
        public string VaseName { get; set; }  // Name from Tiled (e.g., "ApproachVase" for respawning vases)
        public bool IsDestroyed = false;
        private float scale = 4f;

        // Drop counts (set from Tiled properties)
        public int CoinDropCount { get; set; } = 0; // Default to 0, set via Tiled properties
        public int PotionRedDropCount { get; set; } = 0;
        public int PotionRedMiniDropCount { get; set; } = 0;

        public Rectangle Hitbox
        {
            get
            {
                int hitboxWidth = 32;
                int hitboxHeight = 48;
                return new Rectangle(
                    (int)(Position.X - hitboxWidth / 2),
                    (int)(Position.Y - hitboxHeight / 2),
                    hitboxWidth,
                    hitboxHeight
                );
            }
        }

        public Vase(Texture2D texture, Vector2 position, int frames, int fps)
        {
            Position = position;
            Animation = new SpriteAnimation(texture, frames, fps);
            Animation.Scale = scale;
            Animation.Origin = new Vector2((texture.Width / (float)frames) / 2f, texture.Height / 2f);
            Animation.Position = Position;
        }

        public void Update(GameTime gameTime)
        {
            Animation.Position = Position;
            Animation.Update(gameTime);
        }
    }
}