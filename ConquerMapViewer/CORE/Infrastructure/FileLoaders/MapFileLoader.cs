using SharpDX.MediaFoundation;

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
        Terrain = 1,
        TerrainPart = 2,
        MAP_SCENE = 3,
        Cover = 4,
        Role = 5,
        Hero = 6,
        Player = 7,
        Puzzle = 8,
        Simple3D = 9,
        Item2D = 11,
        Npc3D = 12,
        Obj3D = 13,
        Trace3D = 14,
        Region2D = 16,
        MagicMapItem3D = 17,
        Item3D = 18,
        Effect3DNew = 19,
        TerrainSectionCover = 24
    }

    public MapData Load(Stream stream, bool isNewFormat, MapOtherData? otherData)
    {
        using var reader = new BinaryReader(stream);

        var mapData = new MapData
        {
            //DMapHeader = ReadAsciiString(reader, 8),
            DMapVersion = reader.ReadUInt32(),
            DMapData = reader.ReadUInt32(),
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
            ApplyOtherData(mapData, otherData, isNewFormat);

        return mapData;
    }

    // ── Cells ─────────────────────────────────────────────────────────────────
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

                checksum += (ulong)((int)cell.Access * (cell.Surface + y + 1) +
                                    (cell.Height + 2) * (x + 1 + cell.Surface));
            }

            var fileChecksum = reader.ReadUInt32();
            if (fileChecksum != checksum)
                Debug.WriteLine("[Dmap] [LoadDataMap] Checksum doesn't match");
        }
    }

    // ── Portals ───────────────────────────────────────────────────────────────
    private static void LoadPortals(BinaryReader reader, MapData mapData)
    {
        var portalCount = reader.ReadInt32();
        for (var i = 0; i < portalCount; i++)
        {
            var portal = new MapPortal
            {
                Location = ReadPoint(reader),
                PortalIndex = reader.ReadInt32()
            };
            mapData.Portals.Add(portal);
            TrySetAccess(mapData, portal.Location.X, portal.Location.Y, MapCellAccessType.Portal);
        }
    }

    // ── Objects ───────────────────────────────────────────────────────────────
    // NOTE: the old isNewFormat TerrainHandles block has been removed.
    // The dword that was misread as a handle-count is actually the object count.
    private static void LoadObjects(BinaryReader reader, MapData mapData, bool isNewFormat)
    {
        var objectCount = reader.ReadInt32();
        for (var i = 0; i < objectCount; i++)
        {
            var objectType = (MapObjectType)reader.ReadInt32();
            if (objectType == 0)            
                objectType = (MapObjectType)reader.ReadInt32();
            
            switch (objectType)
            {
                case MapObjectType.Scene://MAP_TERRAIN
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
                case MapObjectType.TerrainObject://MAP_COVER
                    {
                        var terrain = ReadMapTerrainObject(reader, isNewFormat);
                        if (!string.IsNullOrEmpty(terrain.AniPath))
                        {
                            mapData.TerrainObjects.Add(terrain);
                            TrySetAccess(mapData, (int)terrain.Location.X, (int)terrain.Location.Y, MapCellAccessType.Terrain);
                        }
                        break;
                    }
                case MapObjectType.Effect://MAP_3DEFFECT
                    {
                        var effect = ReadEffect(reader, isNewFormat);
                        mapData.Effects.Add(effect);
                        var cell = mapData.Cells.World2Cell(effect.Location.X, effect.Location.Y);
                        TrySetAccess(mapData, (int)cell.X, (int)cell.Y, MapCellAccessType.Effect);
                        break;
                    }
                case MapObjectType.Sound://MAP_SOUND
                    {
                        var sound = new MapSound
                        {
                            SoundPath = ReadAsciiString(reader, 260),
                            Location = ReadPoint(reader),
                            Range = reader.ReadInt32(),
                            Volume = reader.ReadInt32(),                            
                            Interval = 100
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

    // ── Layers ────────────────────────────────────────────────────────────────
    private static void LoadLayers(BinaryReader reader, MapData mapData, bool isNewFormat)
    {
        //new- format: flat object list comes first(all objects in one synthetic layer)
        if (isNewFormat)
        {
            var count = reader.ReadInt32();
            var layer = new MapLayer
            {
                Backdrops = new List<MapBackdrop>(),
            };
            for (var i = 0; i < count; i++)
                ReadLayerObject(reader, mapData, layer, isNewFormat);
            mapData.Layers.Add(layer);
        }

        // classic layered block — present in both formats
        var layerCount = reader.ReadInt32();        
        for (var i = 0; i < layerCount; i++)
        {
            var layer = new MapLayer
            {
                Backdrops = new List<MapBackdrop>(),
            };
            layer.Index = reader.ReadInt32();
            layer.Type = reader.ReadInt32();
            switch (layer.Type)
            {
                case 4://LAYER_SCENE
                    {
                        layer.RateX = reader.ReadInt32();
                        layer.RateY = reader.ReadInt32();

                        if (isNewFormat)
                        {
                            layer.NewA = reader.ReadInt32();
                            layer.NewB = reader.ReadInt32();
                            layer.NewC = reader.ReadInt32();
                        }

                        var objectCount = reader.ReadInt32();
                        for (var j = 0; j < objectCount; j++)
                            ReadLayerObject(reader, mapData, layer, isNewFormat);

                        mapData.Layers.Add(layer);
                        break;
                    }
                default:
                    throw new NotSupportedException($"Unknown layer type: {layer.Type}");

            }
        }
    }

    private static void ReadLayerObject(BinaryReader reader, MapData mapData, MapLayer layer, bool isNewFormat)
    {
        var objectType = (MapObjectType)reader.ReadInt32();
        switch (objectType)
        {
            case MapObjectType.MAP_SCENE:
                //layer.TerrainObjects.Add(ReadMapTerrainObject(reader, isNewFormat));
                mapData.TerrainObjects.Add(ReadMapTerrainObject(reader, isNewFormat));
                break;
            case MapObjectType.Backdrop://MAP_PUZZLE
                layer.Backdrops.Add(new MapBackdrop
                {
                    PuzzlePath = ReadAsciiString(reader, 260)
                });
                break;
            
            case MapObjectType.TerrainObject:
            case MapObjectType.TerrainSectionCover:
                mapData.TerrainObjects.Add(ReadMapTerrainObject(reader, isNewFormat));
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

    // ── Terrain object / effect helpers ───────────────────────────────────────
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
            c.ShowWay = (ushort)(r.ReadInt32() & 0xFFFF);
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

    // ── String / point / size helpers ─────────────────────────────────────────
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

    private static void TrySetAccess(MapData map, int x, int y, MapCellAccessType t)
    {
        try { map.Cells[x, y].Access = t; }
        catch (IndexOutOfRangeException)
        { Debug.WriteLine($"[Dmap] Cell ({x},{y}) out of bounds for {t}"); }
    }

    // ── OtherData  FUN_00d21d57 ───────────────────────────────────────────────
    // FIX 1: loop bound now covers the union of map.Layers and OtherData indices
    //        so PicSizes are applied even when layerCount < OtherData layer count.
    //
    // FIX 2: for new-format maps, Layers[0] is the synthetic flat layer whose
    //        TerrainObjects are addressed by OtherData with indices starting at
    //        map.TerrainObjects.Count (the global object base offset).
    //        ApplyPicSize's (uint) guard safely ignores negative relative indices.
    private static void ApplyOtherData(MapData map, MapOtherData od, bool isNewFormat)
    {
        // Layers[0] in a new-format map is the synthetic flat layer.
        var flatLayer = isNewFormat && map.Layers.Count > 0 ? map.Layers[0] : null;
        int flatOffset = map.TerrainObjects.Count;

        int maxI = new[]
        {
            map.Layers.Count,
            od.SceneLayers.Count      > 0 ? od.SceneLayers.Keys.Max()      + 1 : 0,
            od.TerrainLayers.Count    > 0 ? od.TerrainLayers.Keys.Max()    + 1 : 0,
            od.InteractiveLayers.Count > 0 ? od.InteractiveLayers.Keys.Max() + 1 : 0,
        }.Max();

        for (int i = 0; i < maxI; i++)
        {
            var layer = i < map.Layers.Count ? map.Layers[i] : null;

            if (layer != null && od.SceneLayers.TryGetValue(i, out var sl))
            {
                layer.Alpha = sl.Alpha; layer.Light = sl.Light;
                layer.ColorR = sl.Red; layer.ColorG = sl.Green;
                layer.ColorB = sl.Blue; layer.PuzzleAlpha = sl.PuzzleAlpha;
                layer.PuzzleLight = sl.PuzzleLight; layer.PuzzleColorR = sl.PuzzleRed;
                layer.PuzzleColorG = sl.PuzzleGreen; layer.PuzzleColorB = sl.PuzzleBlue;
            }

            if (od.TerrainLayers.TryGetValue(i, out var tl))
            {
                if (layer != null)
                {
                    layer.Alpha = tl.Alpha; layer.Light = tl.Light;
                    layer.ColorR = tl.Red; layer.ColorG = tl.Green; layer.ColorB = tl.Blue;
                }
                foreach (var (idx, w, h) in tl.PicSizes)
                {
                    ApplyPicSize(map.TerrainObjects, idx, w, h, interactive: false);
                    if (flatLayer != null)
                        ApplyPicSize(map.TerrainObjects, idx - flatOffset, w, h, interactive: false);
                }
            }

            if (od.InteractiveLayers.TryGetValue(i, out var il))
            {
                foreach (var (idx, w, h) in il.PicSizes)
                {
                    ApplyPicSize(map.TerrainObjects, idx, w, h, interactive: true);
                    if (flatLayer != null)
                        ApplyPicSize(map.TerrainObjects, idx - flatOffset, w, h, interactive: true);
                }
            }
        }
    }

    private static void ApplyPicSize(List<MapTerrainObject> list, int idx, int w, int h, bool interactive)
    {
        if ((uint)idx >= (uint)list.Count) return;
        list[idx].PicWidth = w;
        list[idx].PicHeight = h;
        list[idx].Interactive = interactive;
    }
}