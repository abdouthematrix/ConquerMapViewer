namespace ConquerMapViewer.Core.Domain.Entities;

// ─────────────────────────────────────────────────────────────────────────────
// PuxFile  (FUN_00d1fbd7 / aoxpuzzle.cpp)
// ─────────────────────────────────────────────────────────────────────────────
public sealed class Pux
{
    /// <summary>File version — default 1000.</summary>
    public uint Version { get; set; }
    /// <summary>Grid column count — FUN_00d13f21(Width, Height).</summary>
    public int Width { get; set; }
    /// <summary>Grid row count.</summary>
    public int Height { get; set; }

    /// <summary>FUN_00d233b5 — stored at parent+0x48/0x4C.</summary>
    public List<PuxGroup> TerrainGroups { get; set; } = new();
    /// <summary>FUN_00d20ffb — identical layout; stored at parent+0x54/0x58.</summary>
    public List<PuxGroup> PuzzleUnitGroups { get; set; } = new();
    /// <summary>count must equal PuzzleUnitGroups.Count (engine asserts).</summary>
    public List<PuxTileUnit> TileUnits { get; } = new();
    /// <summary>Per-cell colour overrides — written to *(cell+0x14).</summary>
    public List<PuxColorEntry> ColorEntries { get; } = new();
    public int TileSize { get; set; }
    // pixel dimensions — matches Puzzle.Width / Puzzle.Height convention
    public int PixelWidth => Width * TileSize;
    public int PixelHeight => Height * TileSize;
}

// ─────────────────────────────────────────────────────────────────────────────
// PuxGroup  (FUN_00d233b5 / FUN_00d20ffb — identical binary layout)
//
// [2+n] Name1   uint16 len + ASCII bytes  (max 1023)
// [2+n] Name2
// [2+n] Name3
// [4]   Field1  int32  group+0x48
// [4]   Field2  int32  group+0x4C
// [4]   Field3  int32  group+0x50
// [4]   Field4  int32  group+0x54
// [4]   Field5  int32  group+0x58
// ─────────────────────────────────────────────────────────────────────────────
public sealed class PuxGroup
{
    public string Name1 { get; set; } = string.Empty;
    public string Name2 { get; set; } = string.Empty;
    public string Name3 { get; set; } = string.Empty;
    public int Field1 { get; set; }
    public int Field2 { get; set; }
    public int Field3 { get; set; }
    public int Field4 { get; set; }
    public int Field5 { get; set; }
}

// ─────────────────────────────────────────────────────────────────────────────
// PuxTileUnit  — one per puzzle unit
// [1] sub_count  uint8
// × sub_count: PuxTileAssignment
// ─────────────────────────────────────────────────────────────────────────────
public sealed class PuxTileUnit
{
    public List<PuxTileAssignment> Assignments { get; } = new();
}

// [2] TileId  uint16  0xFFFF = no tile
// [4] Extra   uint32
public sealed class PuxTileAssignment
{
    public ushort TileId { get; set; } = 0xFFFF;
    public uint Extra { get; set; }
    public bool HasTile => TileId != 0xFFFF;
}

// ─────────────────────────────────────────────────────────────────────────────
// PuxColorEntry  — per-cell colour override
// [2] X  uint16  [2] Y  uint16
// [1] R  [1] G  [1] B  [1] A   (written to *(cell+0x14) as uint32)
// ─────────────────────────────────────────────────────────────────────────────
public sealed class PuxColorEntry
{
    public ushort X { get; set; }
    public ushort Y { get; set; }
    public byte R { get; set; }
    public byte G { get; set; }
    public byte B { get; set; }
    public byte A { get; set; }
}
