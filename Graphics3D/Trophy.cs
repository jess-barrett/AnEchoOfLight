using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace GameProject2.Graphics3D
{
    public class Trophy
    {
        private VertexBuffer vertices;
        private IndexBuffer indices;
        private BasicEffect effect;
        private GraphicsDevice graphicsDevice;
        private int triangleCount;

        private float rotation = 0f;
        public Vector2 WorldPosition { get; set; } // Position in the 2D world

        public Trophy(GraphicsDevice graphicsDevice, Vector2 worldPosition)
        {
            this.graphicsDevice = graphicsDevice;
            this.WorldPosition = worldPosition;
            InitializeVertices();
            InitializeIndices();
            InitializeEffect();
        }

        private void InitializeVertices()
        {
            var vertexList = new List<VertexPositionColor>();

            // BASE - Wide circular base
            float baseRadius = 1.5f;
            int segments = 8;

            // Base center point (to fill the donut hole)
            vertexList.Add(new VertexPositionColor
            {
                Position = new Vector3(0, 0, 0),
                Color = Color.Goldenrod
            });

            // Base bottom circle
            for (int i = 0; i < segments; i++)
            {
                float angle = (float)i / segments * MathHelper.TwoPi;
                vertexList.Add(new VertexPositionColor
                {
                    Position = new Vector3((float)Math.Cos(angle) * baseRadius, 0, (float)Math.Sin(angle) * baseRadius),
                    Color = Color.Goldenrod
                });
            }

            // Base top circle (smaller)
            for (int i = 0; i < segments; i++)
            {
                float angle = (float)i / segments * MathHelper.TwoPi;
                vertexList.Add(new VertexPositionColor
                {
                    Position = new Vector3((float)Math.Cos(angle) * 1.2f, 0.5f, (float)Math.Sin(angle) * 1.2f),
                    Color = Color.Gold
                });
            }

            // STEM - Narrow cylinder (extended down to connect to base)
            for (int i = 0; i < segments; i++)
            {
                float angle = (float)i / segments * MathHelper.TwoPi;
                vertexList.Add(new VertexPositionColor
                {
                    Position = new Vector3((float)Math.Cos(angle) * 0.3f, 0.5f, (float)Math.Sin(angle) * 0.3f), // Connect to base top
                    Color = Color.Gold
                });
            }

            for (int i = 0; i < segments; i++)
            {
                float angle = (float)i / segments * MathHelper.TwoPi;
                vertexList.Add(new VertexPositionColor
                {
                    Position = new Vector3((float)Math.Cos(angle) * 0.3f, 3.0f, (float)Math.Sin(angle) * 0.3f), // Extended higher
                    Color = Color.Yellow
                });
            }

            // CUP BOTTOM - Wider
            for (int i = 0; i < segments; i++)
            {
                float angle = (float)i / segments * MathHelper.TwoPi;
                vertexList.Add(new VertexPositionColor
                {
                    Position = new Vector3((float)Math.Cos(angle) * 1.2f, 3.0f, (float)Math.Sin(angle) * 1.2f),
                    Color = Color.Yellow
                });
            }

            // CUP TOP - Flared rim
            for (int i = 0; i < segments; i++)
            {
                float angle = (float)i / segments * MathHelper.TwoPi;
                vertexList.Add(new VertexPositionColor
                {
                    Position = new Vector3((float)Math.Cos(angle) * 1.4f, 5.0f, (float)Math.Sin(angle) * 1.4f),
                    Color = Color.Gold
                });
            }

            // Top cap center
            vertexList.Add(new VertexPositionColor
            {
                Position = new Vector3(0, 5.3f, 0),
                Color = Color.Gold
            });

            vertices = new VertexBuffer(
                graphicsDevice,
                typeof(VertexPositionColor),
                vertexList.Count,
                BufferUsage.None
            );
            vertices.SetData(vertexList.ToArray());
        }

        private void InitializeIndices()
        {
            var indexList = new List<short>();
            int segments = 8;

            // Base bottom - fan from center (fills the donut hole)
            int baseRing = 1; // Start at index 1 (after center point)
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                indexList.AddRange(new short[] {
            0, // Center point
            (short)(baseRing + i),
            (short)(baseRing + next)
        });
            }

            // Base sides (1-8 to 9-16)
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                indexList.AddRange(new short[] {
            (short)(baseRing + i), (short)(baseRing + segments + i), (short)(baseRing + next),
            (short)(baseRing + next), (short)(baseRing + segments + i), (short)(baseRing + segments + next)
        });
            }

            // Stem sides (17-24 to 25-32)
            int stemBase = segments * 2 + 1; // +1 for center point
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                indexList.AddRange(new short[] {
            (short)(stemBase + i), (short)(stemBase + segments + i), (short)(stemBase + next),
            (short)(stemBase + next), (short)(stemBase + segments + i), (short)(stemBase + segments + next)
        });
            }

            // Cup sides (33-40 to 41-48)
            int cupBase = segments * 4 + 1;
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                indexList.AddRange(new short[] {
            (short)(cupBase + i), (short)(cupBase + segments + i), (short)(cupBase + next),
            (short)(cupBase + next), (short)(cupBase + segments + i), (short)(cupBase + segments + next)
        });
            }

            // Top cap (fan from center)
            int topRing = segments * 5 + 1;
            int center = segments * 6 + 1;
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                indexList.AddRange(new short[] {
            (short)center, (short)(topRing + i), (short)(topRing + next)
        });
            }

            triangleCount = indexList.Count / 3;

            indices = new IndexBuffer(
                graphicsDevice,
                IndexElementSize.SixteenBits,
                indexList.Count,
                BufferUsage.None
            );
            indices.SetData(indexList.ToArray());
        }

        private void InitializeEffect()
        {
            effect = new BasicEffect(graphicsDevice);
            effect.VertexColorEnabled = true;
            effect.LightingEnabled = true;
            effect.EnableDefaultLighting();
        }

        public void Update(GameTime gameTime)
        {
            // Spin the trophy
            rotation += (float)gameTime.ElapsedGameTime.TotalSeconds * 1.5f;
        }

        public void Draw(Matrix view, Matrix projection)
        {
            // Smaller scale
            effect.World = Matrix.CreateScale(5f) * // Reduced from 10f
                           Matrix.CreateRotationY(rotation) *
                           Matrix.CreateTranslation(WorldPosition.X, 5f, WorldPosition.Y);
            effect.View = view;
            effect.Projection = projection;

            effect.CurrentTechnique.Passes[0].Apply();
            graphicsDevice.SetVertexBuffer(vertices);
            graphicsDevice.Indices = indices;
            graphicsDevice.DrawIndexedPrimitives(
                PrimitiveType.TriangleList,
                0,
                0,
                triangleCount
            );
        }
    }
}