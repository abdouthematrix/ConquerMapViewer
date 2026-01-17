using System.Diagnostics;

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
            ulong checksum = 0;
            for (var x = 0; x < mapData.Bounds.Width; x++)
            {
                var cell = new MapCell
                {
                    Access = (MapCellAccessType)reader.ReadInt16(),
                    Surface = reader.ReadInt16(),
                    Height = reader.ReadInt16()
                };
                mapData.Cells[x, y] = cell;

                // Calculate checksum
                checksum += (ulong)((int)cell.Access * (cell.Surface + y + 1) +
                                   (cell.Height + 2) * (x + 1 + cell.Surface));
            }

            var fileChecksum = reader.ReadUInt32();
            if (fileChecksum != checksum)
                Debug.WriteLine("[Dmap] [LoadDataMap] Checksum doesn't match");
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
            try
            {
                mapData.Cells[(int)portal.Location.X, (int)portal.Location.Y].Access = MapCellAccessType.Portal;
            }
            catch (IndexOutOfRangeException)
            {
                Debug.WriteLine("[Dmap] [LoadPortals] Portal location is out of bounds");
            }
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
                    var scene = new MapScene
                    {
                        ScenePath = ReadAsciiString(reader, 260),
                        Location = ReadPoint(reader)
                    };
                    mapData.Scenes.Add(scene);
                    try
                    {
                        mapData.Cells[(int)scene.Location.X, (int)scene.Location.Y].Access = MapCellAccessType.Scene;
                    }
                    catch (IndexOutOfRangeException)
                    {
                        Debug.WriteLine("[Dmap] [LoadObjects] Scene location is out of bounds");
                    }
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
                    try
                    {
                        mapData.Cells[(int)terrain.Location.X, (int)terrain.Location.Y].Access = MapCellAccessType.Terrain;
                    }
                    catch (IndexOutOfRangeException)
                    {
                        Debug.WriteLine("[Dmap] [LoadObjects] Terrain object location is out of bounds");
                    }
                    break;
                case MapObjectType.Effect:
                    var effect = new Map3DEffect
                    {
                        Effect = ReadAsciiString(reader, 64),
                        Location = ReadPoint(reader)
                    };
                    mapData.Effects.Add(effect);
                    try
                    {
                        mapData.Cells[(int)effect.Location.X, (int)effect.Location.Y].Access = MapCellAccessType.Effect;
                    }
                    catch (IndexOutOfRangeException)
                    {
                        Debug.WriteLine("[Dmap] [LoadObjects] Effect location is out of bounds");
                    }
                    break;

                case MapObjectType.Sound:
                    var sound = new MapSound
                    {
                        SoundPath = ReadAsciiString(reader, 260),
                        Location = ReadPoint(reader),
                        Volume = reader.ReadInt32(),
                        Range = reader.ReadInt32()
                    };
                    mapData.Sounds.Add(sound);
                    try
                    {
                        mapData.Cells[(int)sound.Location.X, (int)sound.Location.Y].Access = MapCellAccessType.Sound;
                    }
                    catch (IndexOutOfRangeException)
                    {
                        Debug.WriteLine("[Dmap] [LoadObjects] Sound location is out of bounds");
                    }
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