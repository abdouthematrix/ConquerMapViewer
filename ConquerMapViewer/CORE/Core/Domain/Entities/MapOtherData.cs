namespace ConquerMapViewer.Core.Domain.Entities;

// ─────────────────────────────────────────────────────────────────────────────
// MapOtherData  (FUN_00d21d57 / ".OtherData" named sub-stream)
//
// Accessed via FUN_005d260b(ctx, path, ".OtherData") — a named sub-stream
// inside the .dmap container, NOT inline in the sequential byte stream.
// Key-value store: FUN_01209496(section, key, defaultValue) reads int by key.
//
// Section naming:
//   SceneLayers       → "SceneLayer{i}"          (FUN_00d223b9 / scenelayer.cpp)
//   TerrainLayers     → "TerrainLayer{i}"         (FUN_00d22686 / terrainlayer.cpp)
//   InteractiveLayers → "InteractiveLayer{i}"     (FUN_00d21e87)
// ─────────────────────────────────────────────────────────────────────────────
public sealed class MapOtherData
{
    public Dictionary<int, SceneLayerData> SceneLayers { get; } = new();
    public Dictionary<int, TerrainLayerData> TerrainLayers { get; } = new();
    public Dictionary<int, InteractiveLayerData> InteractiveLayers { get; } = new();

    // ── SceneLayer  (FUN_00d223b9) ─────────────────────────────────────────
    // Keys + defaults from FUN_01209496 call sites:
    //   Alpha(0xFF) Light(0x80) Red(0xFF) Green(0xFF) Blue(0xFF)
    //   PuzzleAlpha(0xFF) PuzzleLight(0x80)
    //   PuzzleRed(0xFF) PuzzleGreen(0xFF) PuzzleBlue(0xFF)
    public sealed class SceneLayerData
    {
        public int Alpha { get; set; } = 0xFF;
        public int Light { get; set; } = 0x80;
        public int Red { get; set; } = 0xFF;
        public int Green { get; set; } = 0xFF;
        public int Blue { get; set; } = 0xFF;
        public int PuzzleAlpha { get; set; } = 0xFF;
        public int PuzzleLight { get; set; } = 0x80;
        public int PuzzleRed { get; set; } = 0xFF;
        public int PuzzleGreen { get; set; } = 0xFF;
        public int PuzzleBlue { get; set; } = 0xFF;
    }

    // ── TerrainLayer  (FUN_00d22686) ─────────────────────────────────────────
    // Same colour keys as SceneLayer + object refs + PicSize entries.
    // Engine re-triggers .pul/.pux load mid-section via FUN_00d4775f.
    public sealed class TerrainLayerData
    {
        public int Alpha { get; set; } = 0xFF;
        public int Light { get; set; } = 0x80;
        public int Red { get; set; } = 0xFF;
        public int Green { get; set; } = 0xFF;
        public int Blue { get; set; } = 0xFF;

        /// <summary>Object indices from MapObjIndex%d keys.</summary>
        public List<int> ObjRefs { get; } = new();

        /// <summary>(objectIndex, width, height) from TerrainLayerPicSize%d.</summary>
        public List<(int Idx, int W, int H)> PicSizes { get; } = new();
    }

    // ── InteractiveLayer  (FUN_00d21e87) ─────────────────────────────────────
    // Header → count + dims. ObjRefs + PicSizes same structure as TerrainLayer.
    // Engine: *(obj+0x48)=w, *(obj+0x4c)=h, *(obj+0x76)=0
    public sealed class InteractiveLayerData
    {
        public int Width { get; set; }
        public int Height { get; set; }

        public List<int> ObjRefs { get; } = new();
        public List<(int Idx, int W, int H)> PicSizes { get; } = new();
    }
}