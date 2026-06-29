namespace ConquerMapViewer.Rendering.Drawing;

// ─────────────────────────────────────────────────────────────────────────────
// PuxDrawingComponent
//
// PUX terrain renderer.  PUX is multi-layer PUL:
//
//   PUL:  Tiles[x, y] → short tileId → frame = "Puzzle{tileId}"
//   PUX:  TileUnits[y * Width + x] → list of (tileId, extra)
//           → aniPath = TerrainGroups[tileId].Name2   ("ANI\ZF.ANI")
//           → frameKey = TerrainGroups[tileId].Name3  ("Puzzle{N}")
//
// TerrainGroups:
//   Name1 = Chinese display name   — GBK, ignored for rendering
//   Name2 = animation file path    — identical for all groups in practice
//   Name3 = animation frame key    — "Puzzle0" … "Puzzle286"
//   Field1-5 = tile animation properties (5,6,4,1,-1) — not positions
//
// TileUnits: flat array, index = y * Width + x.  Each cell has 0..N layers.
//   sub_count > 1 = stacked terrain (blend transitions between tile types).
//   extra = 32-bit per-layer coverage/blend bitmask — controls which
//   sub-pixels of the tile are visible for smooth terrain edge blending.
//   Ignored here (full tile rendered); revisit when blend precision is needed.
//
// PuzzleUnitGroups: empty in all observed files — irrelevant for rendering.
// ColorEntries:     cell-level RGBA tint — zero in observed files but supported.
// ─────────────────────────────────────────────────────────────────────────────

public sealed class PuxDrawingComponent : BaseDrawingComponent, IDisposable
{
    private record struct ScreenTile(Vector2 Location, Texture2D Texture, Color Tint);

    private readonly Pux _pux;
    private readonly IAniDictionary _ani;
    private readonly TextureCache _cache;
    private readonly List<ScreenTile> _tiles = new();
    private readonly Dictionary<(int, int), Color> _colorMap;

    public int TileSize { get; set; }

    private const int EXTRA = 2;

    // ── Construction ──────────────────────────────────────────────────────────
    public PuxDrawingComponent(Pux pux, IAniDictionary ani, TextureCache cache)
    {
        _pux = pux;
        TileSize = pux.TileSize;
        _ani = ani;
        _cache = cache;

        // ColorEntries: per-cell RGBA tint (usually empty)
        _colorMap = pux.ColorEntries.ToDictionary(
            e => ((int)e.X, (int)e.Y),
            e => new Color(e.R, e.G, e.B, e.A));

        // Register every unique animation path from TerrainGroups.Name2.
        // In practice all groups share one path (ANI\ZF.ANI) — deduplicate.
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var g in pux.TerrainGroups)
            if (!string.IsNullOrEmpty(g.Name2) && seen.Add(g.Name2))
                _ani.Add(g.Name2);
    }

    // ── Screen update ─────────────────────────────────────────────────────────
    // Identical flow to PuzzleDrawingComponent — just multi-layer per cell.
    public override void UpdateScreen(Rectangle sr)
    {
        _tiles.Clear();
        if (!Enabled || TileSize == 0) return;

        int numX = Math.Min(sr.Width / TileSize + EXTRA, _pux.Width);
        int numY = Math.Min(sr.Height / TileSize + EXTRA, _pux.Height);
        int sx = sr.X / TileSize;
        int sy = sr.Y / TileSize;
        int offX = -(sr.X % TileSize);
        int offY = -(sr.Y % TileSize);

        for (int x = sx; x < sx + numX; x++)
            for (int y = sy; y < sy + numY; y++)
            {
                if (x < 0 || x >= _pux.Width ||
                    y < 0 || y >= _pux.Height) continue;

                var drawPos = new Vector2(
                    offX + (x - sx) * TileSize,
                    offY + (y - sy) * TileSize);

                var tint = _colorMap.GetValueOrDefault((x, y), Color.White);

                // TileUnits is a flat row-major array: index = y * Width + x
                var unit = _pux.TileUnits[y * _pux.Width + x];

                // Draw all stacked layers bottom-to-top.
                // extra = coverage bitmask (partial tile blend) — ignored for now.
                foreach (var assignment in unit.Assignments)
                    LoadLayer(assignment, drawPos, tint);
            }
    }

    // ── Per-layer tile load ───────────────────────────────────────────────────
    private void LoadLayer(PuxTileAssignment assignment, Vector2 location, Color tint)
    {
        if (!assignment.HasTile) return;
        if (assignment.TileId >= _pux.TerrainGroups.Count) return;

        var g = _pux.TerrainGroups[assignment.TileId];
        // Name2 = animation path ("ANI\ZF.ANI")
        // Name3 = frame key     ("Puzzle280")
        if (string.IsNullOrEmpty(g.Name2) || string.IsNullOrEmpty(g.Name3)) return;
        if (!_ani.TryGetFrames(g.Name2, g.Name3, out var frames) || frames.Count == 0) return;

        try
        {
            var tex = _cache.GetOrLoad(frames[0]);
            _tiles.Add(new ScreenTile(location, tex, tint));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Pux] layer load fail ({g.Name2}/{g.Name3}): {ex.Message}");
        }
    }

    // ── Draw ─────────────────────────────────────────────────────────────────
    public override void Draw(SpriteBatch sb, Matrix transform)
    {
        if (!Enabled) return;
        sb.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied,
                 null, null, null, null, transform);
        foreach (var t in _tiles)
            sb.Draw(t.Texture, t.Location, t.Tint);
        sb.End();
    }

    // ── Dispose ───────────────────────────────────────────────────────────────
    private bool _disposed;
    public void Dispose()
    {
        if (!_disposed) { _tiles.Clear(); _disposed = true; }
        GC.SuppressFinalize(this);
    }
}