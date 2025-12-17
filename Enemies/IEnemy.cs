using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace GameProject2.Enemies
{
    /// <summary>
    /// Interface defining common behavior for all enemies
    /// </summary>
    public interface IEnemy
    {
        // Position and physics
        Vector2 Position { get; set; }
        Rectangle Hitbox { get; }
        RotatedRectangle RotatedHitbox { get; }

        // Health system
        int MaxHealth { get; }
        int CurrentHealth { get; }
        bool IsDead { get; }
        bool IsDeathAnimationComplete { get; }

        // Visual effects
        bool ShowDeathParticles { get; }

        // Combat
        void TakeDamage(int amount);

        // Animation
        SpriteAnimation Animation { get; }

        // Update and Draw
        void Update(GameTime gameTime, Player player, List<Rectangle> collisionBoxes);
        void Draw(SpriteBatch spriteBatch);
        void DrawHealthBar(SpriteBatch spriteBatch, SpriteFont font);
    }
}
