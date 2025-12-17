using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GameProject2
{
    public class SpriteManager
    {
        protected Texture2D Texture;
        public Vector2 Position = Vector2.Zero;
        public Color Color = Color.White;
        public Vector2 Origin;
        public float Rotation = 0f;
        public float Scale = 1f;
        public SpriteEffects SpriteEffect;
        public float LayerDepth;
        public float LayerDepthOffset = 0f;
        protected Rectangle[] Rectangles;
        protected int FrameIndex = 0;

        // Add public properties for hitbox calculations
        public Texture2D GetTexture => Texture;
        public int FrameCount => Rectangles.Length;
        public int FrameWidth => Rectangles.Length > 0 ? Rectangles[0].Width : 0;
        public int FrameHeight => Rectangles.Length > 0 ? Rectangles[0].Height : 0;
        public int CurrentFrameIndex => FrameIndex;

        public SpriteManager(Texture2D Texture, int frames)
        {
            this.Texture = Texture;
            int width = Texture.Width / frames;
            Rectangles = new Rectangle[frames];
            for (int i = 0; i < frames; i++)
                Rectangles[i] = new Rectangle(i * width, 0, width, Texture.Height);
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(Texture, Position, Rectangles[FrameIndex], Color, Rotation, Origin, Scale, SpriteEffect, LayerDepth);
        }
    }

    public class SpriteAnimation : SpriteManager
    {
        private float timeElapsed;
        public bool IsLooping = true;
        private float timeToUpdate;

        // Per-frame duration multipliers (optional)
        private float[] frameDurationMultipliers;

        public int FramesPerSecond { set { timeToUpdate = (1f / value); } }
        public Rectangle CurrentFrameRectangle => Rectangles[FrameIndex];

        public SpriteAnimation(Texture2D Texture, int frames, int fps) : base(Texture, frames)
        {
            FramesPerSecond = fps;
        }

        /// <summary>
        /// Set custom duration multipliers for specific frames.
        /// A multiplier of 2 means that frame plays twice as long.
        /// </summary>
        public void SetFrameDurations(float[] multipliers)
        {
            frameDurationMultipliers = multipliers;
        }

        public void Update(GameTime gameTime)
        {
            timeElapsed += (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Get duration for current frame (use multiplier if set)
            float currentFrameDuration = timeToUpdate;
            if (frameDurationMultipliers != null && FrameIndex < frameDurationMultipliers.Length)
            {
                currentFrameDuration *= frameDurationMultipliers[FrameIndex];
            }

            if (timeElapsed > currentFrameDuration)
            {
                timeElapsed -= currentFrameDuration;
                if (FrameIndex < Rectangles.Length - 1)
                    FrameIndex++;
                else if (IsLooping)
                    FrameIndex = 0;
            }
        }

        public void setFrame(int frame)
        {
            FrameIndex = frame;
            timeElapsed = 0f;
        }
    }
}