namespace ConquerMapViewer.Infrastructure.FileLoaders;

public sealed class MapFileLoader : IMapFileLoader
{
    private enum MapObjectType
    {
        Scene = 1,
        TerrainObject = 4,
        Backdrop = 8,
        Effect = 10,
        Sound = 15,

        None = 0,
        Terrain = 1,   // MAP_TERRAIN
        TerrainPart = 2,   // MAP_TERRAIN_PART
        MAP_SCENE = 3,   // MAP_SCENE
        Cover = 4,   // MAP_COVER
        Role = 5,
        Hero = 6,
        Player = 7,
        Puzzle = 8,   // MAP_PUZZLE
        Simple3D = 9,   // MAP_3DSIMPLE
        //Effect3D = 10,  // MAP_3DEFFECT
        Item2D = 11,  // MAP_2DITEM
        Npc3D = 12,  // MAP_3DNPC
        Obj3D = 13,  // MAP_3DOBJ
        Trace3D = 14,  // MAP_3DTRACE
        //Sound = 15,  // MAP_SOUND
        Region2D = 16,  // MAP_2DREGION
        MagicMapItem3D = 17,  // MAP_3DMAGICMAPITEM
        Item3D = 18,  // MAP_3DITEM
        Effect3DNew = 19,  // MAP_3DEFFECTNEW  ← C3DMapEffectNew
        TerrainSectionCover = 24

    }

    public MapData Load(Stream stream, bool isNewFormat, MapOtherData? otherData)
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
        LoadObjects(reader, mapData, isNewFormat);
        LoadLayers(reader, mapData, isNewFormat);
        if (otherData != null)
            ApplyOtherData(mapData, otherData);
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
            TrySetAccess(mapData, portal.Location.X, portal.Location.Y, MapCellAccessType.Portal);
        }
    }  

    private static void LoadObjects(BinaryReader reader, MapData mapData, bool isNewFormat)
    {
        var objectCount = reader.ReadInt32();
        for (var i = 0; i < objectCount; i++)
        {
            var objectType = (MapObjectType)reader.ReadInt32();
            switch (objectType)
            {
                case MapObjectType.Scene:
                    {
                        var scene = new MapScene
                        {
                            ScenePath = ReadAsciiString(reader, 260),
                            Location = ReadPoint(reader)
                        };
                        mapData.Scenes.Add(scene);
                        TrySetAccess(mapData, (int)scene.Location.X, (int)scene.Location.Y, MapCellAccessType.Scene);
                        break;
                    }
                case MapObjectType.TerrainSectionCover:
                    {
                        var terrain = ReadMapTerrainObject(reader, isNewFormat);
                        mapData.TerrainObjects.Add(terrain);
                        TrySetAccess(mapData, (int)terrain.Location.X, (int)terrain.Location.Y, MapCellAccessType.Terrain);
                        break;
                    }
                case MapObjectType.TerrainObject:
                    {
                        var terrain = ReadMapTerrainObject(reader, isNewFormat);
                        mapData.TerrainObjects.Add(terrain);
                        TrySetAccess(mapData, (int)terrain.Location.X, (int)terrain.Location.Y, MapCellAccessType.Terrain);
                        break;
                    }
                case MapObjectType.Effect:
                    {
                        var effect = ReadEffect(reader, isNewFormat);
                        mapData.Effects.Add(effect);
                        var cell = mapData.Cells.World2Cell(effect.Location.X, effect.Location.Y);
                        TrySetAccess(mapData, (int)cell.X, (int)cell.Y, MapCellAccessType.Effect);
                        break;
                    }

                case MapObjectType.Sound:
                    {
                        var sound = new MapSound
                        {
                            SoundPath = ReadAsciiString(reader, 260),
                            Location = ReadPoint(reader),
                            Volume = reader.ReadInt32(),
                            Range = reader.ReadInt32(),
                            Interval = 100//reader.ReadInt32(),
                        };
                        mapData.Sounds.Add(sound);
                        var cell = mapData.Cells.World2Cell(sound.Location.X, sound.Location.Y);
                        TrySetAccess(mapData, (int)cell.X, (int)cell.Y, MapCellAccessType.Effect);
                        break;
                    }
                default:
                    throw new NotSupportedException($"Unknown object type: {objectType}");
            }
        }
    }

    private static void LoadLayers(BinaryReader reader, MapData mapData, bool isNewFormat)
    {
        var count = reader.ReadInt32();

        if (!isNewFormat)
        {
            for (var i = 0; i < count; i++)
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
                    ReadLayerObject(reader, mapData, layer, isNewFormat);

                mapData.Layers.Add(layer);
            }
        }
        else
        {
            var layer = new MapLayer
            {
                Backdrops = new List<MapBackdrop>(),
                TerrainObjects = new List<MapTerrainObject>()
            };

            for (var i = 0; i < count; i++)
                ReadLayerObject(reader, mapData, layer, isNewFormat);

            mapData.Layers.Add(layer);
        }
    }

    private static void ReadLayerObject(BinaryReader reader, MapData mapData, MapLayer layer, bool isNewFormat)
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
            case MapObjectType.TerrainSectionCover:
                layer.TerrainObjects.Add(ReadMapTerrainObject(reader, isNewFormat));
                break;
            case MapObjectType.Effect3DNew:
            case MapObjectType.Effect:
                {
                    var effect = ReadEffect(reader, isNewFormat);
                    mapData.Effects.Add(effect);
                    var cell = mapData.Cells.World2Cell(effect.Location.X, effect.Location.Y);
                    TrySetAccess(mapData, (int)cell.X, (int)cell.Y, MapCellAccessType.Effect);
                    break;
                }
            case MapObjectType.Scene:
                {
                    var scene = new MapScene
                    {
                        ScenePath = ReadAsciiString(reader, 260),
                        Location = ReadPoint(reader)
                    };
                    mapData.Scenes.Add(scene);
                    TrySetAccess(mapData, (int)scene.Location.X, (int)scene.Location.Y, MapCellAccessType.Scene);
                    break;
                }
            case MapObjectType.Sound:
                {
                    var sound = new MapSound
                    {
                        SoundPath = ReadAsciiString(reader, 260),
                        Location = ReadPoint(reader),
                        Volume = reader.ReadInt32(),
                        Range = reader.ReadInt32(),
                        Interval = 100
                    };
                    mapData.Sounds.Add(sound);
                    break;
                }
            default:
                throw new NotSupportedException($"Unknown layer object type: {objectType}");
        }
    }

    private static MapTerrainObject ReadMapTerrainObject(BinaryReader r, bool isNewFormat)
    {
        var c = new MapTerrainObject
        {
            AniPath = r.ReadASCIIString(260),
            AniName = r.ReadASCIIString(128),
            Location = r.ReadPoint(),
            Size = r.ReadSize(),
            ImageOffset = r.ReadPoint(),
        };
        if (isNewFormat)
        {
            c.Interval = (int)((uint)r.ReadInt32() & 0x0FFF_FFFF);
            c.SubFlags = (ushort)(r.ReadInt32() & 0xFFFF);
        }
        else
            c.Interval = r.ReadInt32();
        return c;
    }

    private static Map3DEffect ReadEffect(BinaryReader reader, bool isNewFormat)
    {
        var c = new Map3DEffectNEW
        {
            Effect = ReadAsciiString(reader, 64),
            Location = ReadPoint(reader)
        };
        if (isNewFormat)
        {
            c.AnglePad = reader.ReadSingle();
            c.Vertical = reader.ReadSingle();
            c.Horizontal = reader.ReadSingle();
            c.ScaleX = reader.ReadSingle();
            c.ScaleY = reader.ReadSingle();
            c.ScaleZ = reader.ReadSingle();
        }
        return c;
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

    // ── Helper ────────────────────────────────────────────────────────────────
    private static void TrySetAccess(MapData map, int x, int y, MapCellAccessType t)
    {
        try { map.Cells[x, y].Access = t; }
        catch (IndexOutOfRangeException)
        { Debug.WriteLine($"[Dmap] Cell ({x},{y}) out of bounds for {t}"); }
    }

    // ── OtherData  FUN_00d21d57 ───────────────────────────────────────────────
    private static void ApplyOtherData(MapData map, MapOtherData od)
    {
        for (int i = 0; i < map.Layers.Count; i++)
        {
            var layer = map.Layers[i];

            if (od.SceneLayers.TryGetValue(i, out var sl))
            {
                layer.Alpha = sl.Alpha; layer.Light = sl.Light;
                layer.ColorR = sl.Red; layer.ColorG = sl.Green;
                layer.ColorB = sl.Blue; layer.PuzzleAlpha = sl.PuzzleAlpha;
                layer.PuzzleLight = sl.PuzzleLight; layer.PuzzleColorR = sl.PuzzleRed;
                layer.PuzzleColorG = sl.PuzzleGreen; layer.PuzzleColorB = sl.PuzzleBlue;
            }

            if (od.TerrainLayers.TryGetValue(i, out var tl))
            {
                layer.Alpha = tl.Alpha; layer.Light = tl.Light;
                layer.ColorR = tl.Red; layer.ColorG = tl.Green; layer.ColorB = tl.Blue;
                foreach (var (idx, w, h) in tl.PicSizes)
                    ApplyPicSize(map.TerrainObjects, idx, w, h, interactive: false);
            }

            if (od.InteractiveLayers.TryGetValue(i, out var il))
                foreach (var (idx, w, h) in il.PicSizes)
                    ApplyPicSize(map.TerrainObjects, idx, w, h, interactive: true);
        }
    }

    private static void ApplyPicSize(List<MapTerrainObject> list, int idx, int w, int h, bool interactive)
    {
        if ((uint)idx >= (uint)list.Count) return;
        list[idx].PicWidth = w; list[idx].PicHeight = h; list[idx].Interactive = interactive;
    }

}