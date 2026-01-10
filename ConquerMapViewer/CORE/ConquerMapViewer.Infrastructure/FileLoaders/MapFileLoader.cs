namespace ConquerMapViewer.Infrastructure.FileLoaders;

public sealed class MapFileLoader : IMapFileLoader
{
    private enum MapObjectType
    {
        Scene = 1,
        TerrainObject = 4,
        Backdrop = 8,
        Effect = 10,
        Sound = 15
    }

    public MapData Load(Stream stream)
    {
        using var reader = new BinaryReader(stream);

        var mapData = new MapData
        {
            DMapHeader = ReadAsciiString(reader, 8),
            PuzzlePath = ReadAsciiString(reader, 260),
            Bounds = ReadSize(reader)
        };

        mapData.Cells = new MapCellCollection(mapData.Bounds)
        {
            CollectionSize = mapData.Bounds
        };

        LoadCells(reader, mapData);
        LoadPortals(reader, mapData);
        LoadObjects(reader, mapData);
        LoadLayers(reader, mapData);

        return mapData;
    }

    private static void LoadCells(BinaryReader reader, MapData mapData)
    {
        for (var y = 0; y < mapData.Bounds.Height; y++)
        {
            for (var x = 0; x < mapData.Bounds.Width; x++)
            {
                mapData.Cells[x, y] = new MapCell
                {
                    Access = (MapCellAccessType)reader.ReadInt16(),
                    Surface = reader.ReadInt16(),
                    Height = reader.ReadInt16()
                };
            }
            reader.ReadInt32();
        }
    }

    private static void LoadPortals(BinaryReader reader, MapData mapData)
    {
        var portalCount = reader.ReadInt32();
        for (var i = 0; i < portalCount; i++)
        {
            var portal = new MapPortal
            {
                Location = ReadPoint(reader),
                PortalType = reader.ReadInt32()
            };
            mapData.Portals.Add(portal);
            mapData.Cells[(int)portal.Location.X, (int)portal.Location.Y].Access = MapCellAccessType.Portal;
        }
    }

    private static void LoadObjects(BinaryReader reader, MapData mapData)
    {
        var objectCount = reader.ReadInt32();
        for (var i = 0; i < objectCount; i++)
        {
            var objectType = (MapObjectType)reader.ReadInt32();
            switch (objectType)
            {
                case MapObjectType.Scene:
                    mapData.Scenes.Add(new MapScene
                    {
                        ScenePath = ReadAsciiString(reader, 260),
                        Location = ReadPoint(reader)
                    });
                    break;

                case MapObjectType.TerrainObject:
                    var terrain = new MapTerrainObject
                    {
                        AniPath = ReadAsciiString(reader, 260),
                        AniName = ReadAsciiString(reader, 128),
                        Location = ReadPoint(reader),
                        Size = ReadSize(reader),
                        ImageOffset = ReadPoint(reader),
                        Interval = reader.ReadInt32()
                    };
                    mapData.TerrainObjects.Add(terrain);
                    mapData.Cells[(int)terrain.Location.X, (int)terrain.Location.Y].Access = MapCellAccessType.Terrain;
                    break;

                case MapObjectType.Effect:
                    mapData.Effects.Add(new Map3DEffect
                    {
                        Effect = ReadAsciiString(reader, 64),
                        Location = ReadPoint(reader)
                    });
                    break;

                case MapObjectType.Sound:
                    mapData.Sounds.Add(new MapSound
                    {
                        SoundPath = ReadAsciiString(reader, 260),
                        Location = ReadPoint(reader),
                        Volume = reader.ReadInt32(),
                        Range = reader.ReadInt32()
                    });
                    break;

                default:
                    throw new NotSupportedException($"Unknown object type: {objectType}");
            }
        }
    }

    private static void LoadLayers(BinaryReader reader, MapData mapData)
    {
        var layerCount = reader.ReadInt32();
        for (var i = 0; i < layerCount; i++)
        {
            var layer = new MapLayer
            {
                index = reader.ReadInt32(),
                layertype = reader.ReadInt32(),
                xInt = reader.ReadInt32(),
                yInt = reader.ReadInt32(),
                Backdrops = new List<MapBackdrop>(),
                TerrainObjects = new List<MapTerrainObject>()
            };

            var objectCount = reader.ReadInt32();
            for (var j = 0; j < objectCount; j++)
            {
                var objectType = (MapObjectType)reader.ReadInt32();
                switch (objectType)
                {
                    case MapObjectType.Backdrop:
                        layer.Backdrops.Add(new MapBackdrop
                        {
                            PuzzlePath = ReadAsciiString(reader, 260)
                        });
                        break;

                    case MapObjectType.TerrainObject:
                        layer.TerrainObjects.Add(new MapTerrainObject
                        {
                            AniPath = ReadAsciiString(reader, 260),
                            AniName = ReadAsciiString(reader, 128),
                            Location = ReadPoint(reader),
                            Size = ReadSize(reader),
                            ImageOffset = ReadPoint(reader),
                            Interval = reader.ReadInt32()
                        });
                        break;

                    default:
                        throw new NotSupportedException($"Unknown layer object type: {objectType}");
                }
            }

            mapData.Layers.Add(layer);
        }
    }

    private static string ReadAsciiString(BinaryReader reader, int length)
    {
        var bytes = reader.ReadBytes(length);
        var nullIndex = Array.IndexOf(bytes, (byte)0);
        return Encoding.ASCII.GetString(bytes, 0, nullIndex >= 0 ? nullIndex : length);
    }

    private static MapPoint ReadPoint(BinaryReader reader) =>
        new(reader.ReadInt32(), reader.ReadInt32());

    private static MapSize ReadSize(BinaryReader reader) =>
        new(reader.ReadInt32(), reader.ReadInt32());
}
