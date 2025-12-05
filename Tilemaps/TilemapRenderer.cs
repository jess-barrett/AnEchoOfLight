using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GameProject2.Tilemaps
{
    public class TilemapRenderer
    {
        public static void DrawLayer(SpriteBatch spriteBatch, Tilemap tilemap, TileLayer layer, float layerDepth, float scale)
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
                        Color.White,
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