using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using GameProject2.StateManagement;
using GameProject2.Graphics3D;

namespace GameProject2.Screens
{
    public class TrophyScreen : GameScreen
    {
        private Trophy trophy;
        private Matrix view;
        private Matrix projection;
        private float cameraAngle = 0f;

        public TrophyScreen()
        {
            TransitionOnTime = TimeSpan.FromSeconds(0.5);
            TransitionOffTime = TimeSpan.FromSeconds(0.5);
        }

        public override void Activate()
        {
            base.Activate();

            System.Diagnostics.Debug.WriteLine("Trophy screen activated");

            // Create trophy at origin with large scale
            trophy = new Trophy(ScreenManager.GraphicsDevice, Vector2.Zero);

            // Setup projection
            projection = Matrix.CreatePerspectiveFieldOfView(
                MathHelper.PiOver4,
                ScreenManager.GraphicsDevice.Viewport.AspectRatio,
                0.1f,
                1000f
            );
        }

        public override void HandleInput(GameTime gameTime, InputState input)
        {
            PlayerIndex playerIndex;

            if (input.IsNewKeyPress(Keys.Space, ControllingPlayer, out playerIndex) ||
                input.IsNewKeyPress(Keys.Escape, ControllingPlayer, out playerIndex))
            {
                System.Diagnostics.Debug.WriteLine("Exiting trophy screen");
                ExitScreen();
            }
        }

        public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
        {
            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);

            if (IsActive)
            {
                trophy.Update(gameTime);

                // Slowly rotate camera around the trophy
                cameraAngle += (float)gameTime.ElapsedGameTime.TotalSeconds * 0.3f;

                float cameraDistance = 40f;
                Vector3 cameraPosition = new Vector3(
                    (float)Math.Sin(cameraAngle) * cameraDistance,
                    28f, // Even higher
                    (float)Math.Cos(cameraAngle) * cameraDistance
                );

                view = Matrix.CreateLookAt(
                    cameraPosition,
                    new Vector3(0, 18f, 0),
                    Vector3.Up
                );
            }
        }

        public override void Draw(GameTime gameTime)
        {
            var graphics = ScreenManager.GraphicsDevice;

            graphics.Clear(Color.DarkSlateGray);

            // Setup 3D rendering
            var rasterizerState = new RasterizerState();
            rasterizerState.CullMode = CullMode.None;
            graphics.RasterizerState = rasterizerState;
            graphics.DepthStencilState = DepthStencilState.Default;
            graphics.BlendState = BlendState.Opaque;

            // Draw trophy
            trophy.Draw(view, projection);

            // Draw text
            var spriteBatch = ScreenManager.SpriteBatch;
            var font = ScreenManager.Font;

            spriteBatch.Begin();

            // Title text
            string titleText = "You found the Secret Trophy!";
            Vector2 titleSize = font.MeasureString(titleText);
            Vector2 titlePosition = new Vector2(
                (graphics.Viewport.Width - titleSize.X) / 2,
                graphics.Viewport.Height - 120
            );
            spriteBatch.DrawString(font, titleText, titlePosition, Color.White);

            // Instruction text
            string instructionText = "Press SPACE to Return";
            Vector2 instructionSize = font.MeasureString(instructionText);
            Vector2 instructionPosition = new Vector2(
                (graphics.Viewport.Width - instructionSize.X) / 2,
                graphics.Viewport.Height - 60
            );
            spriteBatch.DrawString(font, instructionText, instructionPosition, Color.White);

            spriteBatch.End();

            // Reset graphics state
            graphics.DepthStencilState = DepthStencilState.None;
        }
    }
}
