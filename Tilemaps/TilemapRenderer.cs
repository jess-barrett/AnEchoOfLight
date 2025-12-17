using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace GameProject2.Tilemaps
{
    public class TilemapRenderer
    {
        public static void DrawLayer(SpriteBatch spriteBatch, Tilemap tilemap, TileLayer layer, float layerDepth, float scale)
        {
            DrawLayer(spriteBatch, tilemap, layer, layerDepth, scale, Color.White);
        }

        public static void DrawLayer(SpriteBatch spriteBatch, Tilemap tilemap, TileLayer layer, float layerDepth, float scale, Color tint)
        {
            for (int y = 0; y < layer.Height; y++)
            {
                for (int x = 0; x < layer.Width; x++)
                {
                    int index = y * layer.Width + x;
                    int gid = layer.Tiles[index];

                    if (gid == 0) continue;

                    // Find the correct tileset for this gid
                    TilesetInfo tileset = null;
                    int localTileId = gid;

                    for (int i = tilemap.Tilesets.Count - 1; i >= 0; i--)
                    {
                        if (gid >= tilemap.Tilesets[i].FirstGid)
                        {
                            tileset = tilemap.Tilesets[i];
                            localTileId = gid - tileset.FirstGid;
                            break;
                        }
                    }

                    if (tileset == null || tileset.Texture == null) continue;

                    int tileX = localTileId % tileset.Columns;
                    int tileY = localTileId / tileset.Columns;

                    Rectangle sourceRect = new Rectangle(
                        tileX * tileset.TileWidth,
                        tileY * tileset.TileHeight,
                        tileset.TileWidth,
                        tileset.TileHeight
                    );

                    Vector2 position = new Vector2(x * tilemap.TileWidth * scale, y * tilemap.TileHeight * scale);

                    spriteBatch.Draw(
                        tileset.Texture,
                        position,
                        sourceRect,
                        tint,
                        0f,
                        Vector2.Zero,
                        scale,
                        SpriteEffects.None,
                        layerDepth
                    );
                }
            }
        }

        /// <summary>
        /// Draw a layer with a diagonal fade effect from top-right to bottom-left.
        /// Progress 0 = fully transparent, 1 = fully visible.
        /// </summary>
        public static void DrawLayerWithDiagonalFade(SpriteBatch spriteBatch, Tilemap tilemap, TileLayer layer, float layerDepth, float scale, float progress)
        {
            if (progress <= 0) return;

            for (int y = 0; y < layer.Height; y++)
            {
                for (int x = 0; x < layer.Width; x++)
                {
                    int index = y * layer.Width + x;
                    int gid = layer.Tiles[index];

                    if (gid == 0) continue;

                    // Find the correct tileset for this gid
                    TilesetInfo tileset = null;
                    int localTileId = gid;

                    for (int i = tilemap.Tilesets.Count - 1; i >= 0; i--)
                    {
                        if (gid >= tilemap.Tilesets[i].FirstGid)
                        {
                            tileset = tilemap.Tilesets[i];
                            localTileId = gid - tileset.FirstGid;
                            break;
                        }
                    }

                    if (tileset == null || tileset.Texture == null) continue;

                    // Calculate diagonal progress for this tile (top-right to bottom-left)
                    // Normalize x and y to 0-1 range
                    float normalizedX = (float)x / layer.Width;
                    float normalizedY = (float)y / layer.Height;

                    // Diagonal from top-right: tiles with high x and low y appear first
                    // Use (1 - normalizedX) + normalizedY to get diagonal from top-right
                    float tileProgress = (1f - normalizedX) + normalizedY; // Range 0-2
                    tileProgress /= 2f; // Normalize to 0-1

                    // Calculate tile opacity based on overall progress
                    // Tiles "ahead" in the diagonal appear first
                    float tileOpacity = MathHelper.Clamp((progress * 2f) - tileProgress, 0f, 1f);

                    if (tileOpacity <= 0) continue;

                    int tileX = localTileId % tileset.Columns;
                    int tileY = localTileId / tileset.Columns;

                    Rectangle sourceRect = new Rectangle(
                        tileX * tileset.TileWidth,
                        tileY * tileset.TileHeight,
                        tileset.TileWidth,
                        tileset.TileHeight
                    );

                    Vector2 position = new Vector2(x * tilemap.TileWidth * scale, y * tilemap.TileHeight * scale);

                    spriteBatch.Draw(
                        tileset.Texture,
                        position,
                        sourceRect,
                        Color.White * tileOpacity,
                        0f,
                        Vector2.Zero,
                        scale,
                        SpriteEffects.None,
                        layerDepth
                    );
                }
            }
        }
    }
}