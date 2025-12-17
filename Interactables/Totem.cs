using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GameProject2
{
    public enum TotemState
    {
        Waiting,    // Static on frame 0, waiting for trigger
        Idle,       // Showing top 2 rows, animating 16 frames (plays once, pauses on last frame)
        Dropping,   // Playing drop animation (rows 7-8, specific columns)
        Dropped     // Finished dropping, static final frame
    }

    public class Totem
    {
        public Vector2 Position { get; private set; }
        public TotemState State { get; private set; } = TotemState.Waiting;
        public string TotemId { get; set; }
        public string AbilityName { get; set; } // e.g., "Dash", "Attack2"

        private Texture2D texture;
        private float scale;
        private int tileSize = 16;

        // Animation state
        private int currentFrame = 0;
        private float animationTimer = 0f;
        private float frameDuration = 0.1f;
        private bool idleAnimationComplete = false;

        // Idle animation: top 2 rows, 16 frames (plays once)
        private int idleFrameCount = 16;

        // Drop animation: rows 7-8, columns 4,5,6,7, then 9,10,11,12,13,14,15,16 (12 frames)
        private int[] dropFrameColumns = { 3, 4, 5, 6, 8, 9, 10, 11, 12, 13, 14, 15 }; // 0-indexed
        private int dropFrameCount = 12;

        public Rectangle Hitbox
        {
            get
            {
                // 2 tiles tall
                return new Rectangle(
                    (int)(Position.X),
                    (int)(Position.Y),
                    (int)(tileSize * scale),
                    (int)(tileSize * 2 * scale)
                );
            }
        }

        public Totem(Texture2D totemTexture, Vector2 position, float scale, string totemId, string abilityName)
        {
            this.texture = totemTexture;
            this.Position = position;
            this.scale = scale;
            this.TotemId = totemId;
            this.AbilityName = abilityName;
        }

        public void SetDropped()
        {
            State = TotemState.Dropped;
            currentFrame = dropFrameCount - 1;
        }

        // Called when player crosses the trigger - starts the idle animation
        public void TriggerActivation()
        {
            if (State == TotemState.Waiting)
            {
                State = TotemState.Idle;
                currentFrame = 0;
                animationTimer = 0f;
                AudioManager.PlayTotemActivateSound(0.5f);
            }
        }

        // Called after unlock overlay closes - starts the drop animation
        public void TriggerDrop()
        {
            if (State == TotemState.Idle)
            {
                State = TotemState.Dropping;
                currentFrame = 0;
                animationTimer = 0f;
            }
        }

        public void Update(GameTime gameTime)
        {
            // Waiting state - static on frame 0, no updates needed
            if (State == TotemState.Waiting || State == TotemState.Dropped)
                return;

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            animationTimer += dt;

            if (animationTimer >= frameDuration)
            {
                animationTimer = 0f;

                switch (State)
                {
                    case TotemState.Idle:
                        // Play once and pause on final frame
                        if (!idleAnimationComplete)
                        {
                            currentFrame++;
                            if (currentFrame >= idleFrameCount)
                            {
                                currentFrame = idleFrameCount - 1;
                                idleAnimationComplete = true;
                            }
                        }
                        break;

                    case TotemState.Dropping:
                        currentFrame++;
                        if (currentFrame >= dropFrameCount)
                        {
                            currentFrame = dropFrameCount - 1;
                            State = TotemState.Dropped;
                        }
                        break;
                }
            }
        }

        public void Draw(SpriteBatch spriteBatch, float layerDepth)
        {
            if (State == TotemState.Waiting || State == TotemState.Idle)
            {
                // Draw top tile (row 0)
                Rectangle sourceTop = new Rectangle(
                    currentFrame * tileSize,
                    0,
                    tileSize,
                    tileSize
                );

                // Draw bottom tile (row 1)
                Rectangle sourceBottom = new Rectangle(
                    currentFrame * tileSize,
                    tileSize,
                    tileSize,
                    tileSize
                );

                // Draw top part
                spriteBatch.Draw(
                    texture,
                    Position,
                    sourceTop,
                    Color.White,
                    0f,
                    Vector2.Zero,
                    scale,
                    SpriteEffects.None,
                    layerDepth
                );

                // Draw bottom part
                spriteBatch.Draw(
                    texture,
                    Position + new Vector2(0, tileSize * scale),
                    sourceBottom,
                    Color.White,
                    0f,
                    Vector2.Zero,
                    scale,
                    SpriteEffects.None,
                    layerDepth + 0.001f
                );
            }
            else
            {
                // Dropping or Dropped state - use rows 7-8 (indices 6-7)
                int col = dropFrameColumns[currentFrame];

                // Draw top tile (row 6, 0-indexed)
                Rectangle sourceTop = new Rectangle(
                    col * tileSize,
                    6 * tileSize,
                    tileSize,
                    tileSize
                );

                // Draw bottom tile (row 7, 0-indexed)
                Rectangle sourceBottom = new Rectangle(
                    col * tileSize,
                    7 * tileSize,
                    tileSize,
                    tileSize
                );

                // Draw top part
                spriteBatch.Draw(
                    texture,
                    Position,
                    sourceTop,
                    Color.White,
                    0f,
                    Vector2.Zero,
                    scale,
                    SpriteEffects.None,
                    layerDepth
                );

                // Draw bottom part
                spriteBatch.Draw(
                    texture,
                    Position + new Vector2(0, tileSize * scale),
                    sourceBottom,
                    Color.White,
                    0f,
                    Vector2.Zero,
                    scale,
                    SpriteEffects.None,
                    layerDepth + 0.001f
                );
            }
        }
    }
}
