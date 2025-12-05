using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace GameProject2.Tilemaps
{
    public class TmxLoader
    {
        public static Tilemap Load(string tmxFilePath, ContentManager content)
        {
            XDocument doc = XDocument.Load(tmxFilePath);
            XElement mapElement = doc.Element("map");

            Tilemap tilemap = new Tilemap();

            tilemap.Width = int.Parse(mapElement.Attribute("width").Value);
            tilemap.Height = int.Parse(mapElement.Attribute("height").Value);
            tilemap.TileWidth = int.Parse(mapElement.Attribute("tilewidth").Value);
            tilemap.TileHeight = int.Parse(mapElement.Attribute("tileheight").Value);

            // Load all tilesets
            foreach (XElement tilesetElement in mapElement.Elements("tileset"))
            {
                int firstGid = int.Parse(tilesetElement.Attribute("firstgid").Value);
                string tilesetSource = tilesetElement.Attribute("source")?.Value;

                if (!string.IsNullOrEmpty(tilesetSource))
                {
                    string contentRoot = Path.GetDirectoryName(Path.GetDirectoryName(tmxFilePath));
                    string tilesetPath = Path.Combine(contentRoot, "Tilemaps", Path.GetFileName(tilesetSource));

                    XDocument tilesetDoc = XDocument.Load(tilesetPath);
                    ProcessTileset(tilesetDoc.Element("tileset"), tilemap, content, Path.GetDirectoryName(tilesetPath), firstGid);
                }
                else 
                {
                    ProcessTileset(tilesetElement, tilemap, content, Path.GetDirectoryName(tmxFilePath), firstGid);
                }
            }

            foreach (XElement layerElement in mapElement.Elements("layer"))
            {
                TileLayer layer = new TileLayer
                {
                    Name = layerElement.Attribute("name").Value,
                    Width = int.Parse(layerElement.Attribute("width").Value),
                    Height = int.Parse(layerElement.Attribute("height").Value)
                };

                XElement dataElement = layerElement.Element("data");
                string encoding = dataElement.Attribute("encoding")?.Value;

                if (encoding == "csv")
                {
                    string csvData = dataElement.Value.Trim();
                    var tileStrings = csvData.Split(',');
                    layer.Tiles = new int[tileStrings.Length];

                    // Tiled uses the top 3 bits for flip flags
                    const uint FLIPPED_HORIZONTALLY_FLAG = 0x80000000;
                    const uint FLIPPED_VERTICALLY_FLAG = 0x40000000;
                    const uint FLIPPED_DIAGONALLY_FLAG = 0x20000000;
                    const uint TILE_MASK = ~(FLIPPED_HORIZONTALLY_FLAG | FLIPPED_VERTICALLY_FLAG | FLIPPED_DIAGONALLY_FLAG);

                    for (int i = 0; i < tileStrings.Length; i++)
                    {
                        string tileStr = tileStrings[i].Trim();

                        if (string.IsNullOrEmpty(tileStr))
                        {
                            layer.Tiles[i] = 0;
                            continue;
                        }

                        try
                        {
                            uint gid = uint.Parse(tileStr);
                            uint tileId = gid & TILE_MASK;
                            layer.Tiles[i] = (int)tileId;
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"WARNING: Could not parse tile value '{tileStr}' at index {i} in layer '{layerElement.Attribute("name").Value}'. Error: {ex.Message}. Setting to 0.");
                            layer.Tiles[i] = 0;
                        }
                    }

                    int nonZeroCount = layer.Tiles.Count(t => t != 0);
                    System.Diagnostics.Debug.WriteLine($"Layer '{layer.Name}': {layer.Tiles.Length} total tiles, {nonZeroCount} non-zero");
                }
                else
                {
                    layer.Tiles = dataElement.Elements("tile")
                        .Select(t => int.Parse(t.Attribute("gid").Value))
                        .ToArray();

                    int nonZeroCount = layer.Tiles.Count(t => t != 0);
                    System.Diagnostics.Debug.WriteLine($"Layer '{layer.Name}': {layer.Tiles.Length} total tiles, {nonZeroCount} non-zero");
                }

                tilemap.Layers.Add(layer);
            }

            foreach (XElement objectGroupElement in mapElement.Elements("objectgroup"))
            {
                ObjectLayer objectLayer = new ObjectLayer
                {
                    Name = objectGroupElement.Attribute("name").Value
                };

                foreach (XElement objElement in objectGroupElement.Elements("object"))
                {
                    TiledObject obj = new TiledObject
                    {
                        Name = objElement.Attribute("name")?.Value ?? "",
                        Class = objElement.Attribute("class")?.Value ?? objElement.Attribute("type")?.Value ?? "",
                        X = float.Parse(objElement.Attribute("x").Value),
                        Y = float.Parse(objElement.Attribute("y").Value),
                        Width = float.Parse(objElement.Attribute("width")?.Value ?? "0"),
                        Height = float.Parse(objElement.Attribute("height")?.Value ?? "0")
                    };

                    XElement propertiesElement = objElement.Element("properties");
                    if (propertiesElement != null)
                    {
                        foreach (XElement propElement in propertiesElement.Elements("property"))
                        {
                            string propName = propElement.Attribute("name").Value;
                            string propValue = propElement.Attribute("value").Value;
                            obj.Properties[propName] = propValue;
                        }
                    }

                    objectLayer.Objects.Add(obj);
                }

                tilemap.ObjectLayers.Add(objectLayer);
            }

            return tilemap;
        }

        private static void ProcessTileset(XElement tilesetElement, Tilemap tilemap, ContentManager content, string baseDirectory, int firstGid)
        {
            TilesetInfo tilesetInfo = new TilesetInfo
            {
                FirstGid = firstGid,
                TileWidth = int.Parse(tilesetElement.Attribute("tilewidth").Value),
                TileHeight = int.Parse(tilesetElement.Attribute("tileheight").Value),
                Columns = int.Parse(tilesetElement.Attribute("columns").Value)
            };

            XElement imageElement = tilesetElement.Element("image");
            if (imageElement != null)
            {
                string imagePath = imageElement.Attribute("source").Value;
                string fileName = Path.GetFileNameWithoutExtension(imagePath);

                try
                {
                    tilesetInfo.Texture = content.Load<Texture2D>($"Tilemaps/{fileName}");
                    System.Diagnostics.Debug.WriteLine($"Loaded tileset: {fileName} (firstgid={firstGid})");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ERROR: Could not load tileset texture '{fileName}': {ex.Message}");
                }
            }

            tilemap.Tilesets.Add(tilesetInfo);

            // Keep the first tileset as the main one for backward compatibility
            if (tilemap.TilesetTexture == null)
            {
                tilemap.TilesetTexture = tilesetInfo.Texture;
                tilemap.TilesetColumns = tilesetInfo.Columns;
                tilemap.TilesetTileWidth = tilesetInfo.TileWidth;
                tilemap.TilesetTileHeight = tilesetInfo.TileHeight;
            }
        }
    }

    public class TilesetInfo
    {
        public int FirstGid { get; set; }
        public Texture2D Texture { get; set; }
        public int Columns { get; set; }
        public int TileWidth { get; set; }
        public int TileHeight { get; set; }
    }

    public class Tilemap
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public int TileWidth { get; set; }
        public int TileHeight { get; set; }

        public List<TileLayer> Layers { get; set; } = new List<TileLayer>();
        public List<ObjectLayer> ObjectLayers { get; set; } = new List<ObjectLayer>();

        // Multiple tilesets
        public List<TilesetInfo> Tilesets { get; set; } = new List<TilesetInfo>();

        // Legacy single tileset properties (for backward compatibility)
        public Texture2D TilesetTexture { get; set; }
        public int TilesetColumns { get; set; }
        public int TilesetTileWidth { get; set; }
        public int TilesetTileHeight { get; set; }
    }

    public class TileLayer
    {
        public string Name { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int[] Tiles { get; set; }
    }

    public class ObjectLayer
    {
        public string Name { get; set; }
        public List<TiledObject> Objects { get; set; } = new List<TiledObject>();
    }

    public class TiledObject
    {
        public string Name { get; set; }
        public string Class { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
        public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();
    }
}