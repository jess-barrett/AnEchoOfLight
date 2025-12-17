using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace GameProject2
{
    public class ParticleSystem
    {
        private List<Particle> particles = new List<Particle>();
        private Texture2D particleTexture;
        private Random random = new Random();

        public ParticleSystem(GraphicsDevice graphicsDevice)
        {
            particleTexture = new Texture2D(graphicsDevice, 4, 4);
            Color[] data = new Color[16];
            for (int i = 0; i < data.Length; i++)
                data[i] = Color.White;
            particleTexture.SetData(data);
        }

        public void CreateSkullDeathEffect(Vector2 position)
        {
            // Create 20-30 particles
            int particleCount = random.Next(20, 31);

            for (int i = 0; i < particleCount; i++)
            {
                // Random direction
                float angle = (float)(random.NextDouble() * MathHelper.TwoPi);
                float speed = (float)(random.NextDouble() * 200 + 100);
                Vector2 velocity = new Vector2(
                    (float)Math.Cos(angle) * speed,
                    (float)Math.Sin(angle) * speed
                );

                // Random color (red, orange, yellow for fire effect)
                Color[] colors = { Color.White, Color.WhiteSmoke, Color.SlateGray, Color.Crimson };
                Color color = colors[random.Next(colors.Length)];

                // Random lifetime and scale
                float lifetime = (float)(random.NextDouble() * 0.5 + 0.3);
                float scale = (float)(random.NextDouble() * 2 + 1);
                particles.Add(new Particle(position, velocity, color, lifetime, scale));
            }
        }

        /// <summary>
        /// Creates a small wood splinter effect when crate is hit
        /// </summary>
        public void CreateWoodSplinterEffect(Vector2 position, bool large = false)
        {
            // Small effect for hits, larger for destruction
            int particleCount = large ? random.Next(15, 25) : random.Next(5, 10);
            float speedMultiplier = large ? 1.5f : 1f;
            float scaleMultiplier = large ? 1.3f : 1f;

            for (int i = 0; i < particleCount; i++)
            {
                // Random direction with slight upward bias
                float angle = (float)(random.NextDouble() * MathHelper.TwoPi);
                float speed = (float)(random.NextDouble() * 120 + 60) * speedMultiplier;
                Vector2 velocity = new Vector2(
                    (float)Math.Cos(angle) * speed,
                    (float)Math.Sin(angle) * speed - 30 // Slight upward bias
                );

                // Wood colors - browns and tans
                Color[] colors = {
                    new Color(139, 90, 43),   // SaddleBrown
                    new Color(160, 120, 70),  // Light brown
                    new Color(101, 67, 33),   // Dark brown
                    new Color(205, 170, 125), // Tan
                    new Color(120, 80, 40)    // Medium brown
                };
                Color color = colors[random.Next(colors.Length)];

                // Random lifetime and scale
                float lifetime = (float)(random.NextDouble() * 0.4 + 0.2);
                float scale = (float)(random.NextDouble() * 1.5 + 0.5) * scaleMultiplier;
                particles.Add(new Particle(position, velocity, color, lifetime, scale));
            }
        }

        public void Update(GameTime gameTime)
        {
            for (int i = particles.Count - 1; i >= 0; i--)
            {
                particles[i].Update(gameTime);

                if (!particles[i].IsAlive)
                    particles.RemoveAt(i);
            }
        }

        public void Draw(SpriteBatch spriteBatch, float baseLayerDepth)
        {
            foreach (var particle in particles)
            {
                spriteBatch.Draw(
                    particleTexture,
                    particle.Position,
                    null,
                    particle.Color * particle.Alpha,
                    particle.Rotation,
                    new Vector2(2, 2),
                    particle.Scale,
                    SpriteEffects.None,
                    baseLayerDepth - 0.01f
                );
            }
        }
    }
}