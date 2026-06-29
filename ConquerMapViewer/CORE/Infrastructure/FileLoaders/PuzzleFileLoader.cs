using SharpDX.Direct3D9;
using SharpDX.Win32;

namespace ConquerMapViewer.Infrastructure.FileLoaders;

public sealed class PuzzleFileLoader : IPuzzleFileLoader
{
    private readonly string _conquerDirectory;

    public PuzzleFileLoader(string conquerDirectory)
    {
        _conquerDirectory = conquerDirectory;
    }

    public (Puzzle Puzzle, Pux Pux) Load(string path, Stream stream)
    {

        using var reader = new BinaryReader(stream);

        bool isPul = Path.GetExtension(path)
    .Equals(".pul", StringComparison.OrdinalIgnoreCase);

        if (isPul)
        {
            var puzzle = new Puzzle
            {
                PuzzleType = reader.ReadASCIIString(8),
                AniPath = reader.ReadASCIIString(256),
                HorizontalTiles = reader.ReadInt32(),
                VerticalTiles = reader.ReadInt32()
            };

            puzzle.Tiles = new short[puzzle.HorizontalTiles, puzzle.VerticalTiles];

            for (var y = 0; y < puzzle.VerticalTiles; y++)
            {
                for (var x = 0; x < puzzle.HorizontalTiles; x++)
                {
                    puzzle.Tiles[x, y] = reader.ReadInt16();
                }
            }

            if (puzzle.PuzzleType == "PUZZLE2")
            {
                puzzle.RollSpeedX = reader.ReadInt32();
                puzzle.RollSpeedY = reader.ReadInt32();
            }

            return (puzzle, null);
        }
        else
        {
            string magic = reader.ReadASCIIString(16);
            if (magic != "TqTerrain")
                throw new InvalidDataException($"[Pux] Expected TqTerrain, got '{magic}'");

            var pux = new Pux
            {
                Version = reader.ReadUInt32(),
                Width = reader.ReadInt32(),
                Height = reader.ReadInt32(),
            };

            pux.TerrainGroups = ReadGroups(reader, "TerrainGroups");
            pux.PuzzleUnitGroups = ReadGroups(reader, "PuzzleUnitGroups");

            // ── TileUnits — flat Width×Height grid ────────────────────────────────
            uint unitCount = reader.ReadUInt32();
            int expected = pux.Width * pux.Height;
            if ((int)unitCount != expected)
                Debug.WriteLine($"[Pux] unit_count {unitCount} ≠ {pux.Width}×{pux.Height}={expected}");

            for (int i = 0; i < unitCount; i++)
            {
                var unit = new PuxTileUnit();
                byte sub = reader.ReadByte();
                for (int j = 0; j < sub; j++)
                    unit.Assignments.Add(new PuxTileAssignment
                    {
                        TileId = reader.ReadUInt16(),
                        Extra = reader.ReadUInt32(),
                    });
                pux.TileUnits.Add(unit);
            }

            // ── ColorEntries ──────────────────────────────────────────────────────
            uint colorCount = reader.ReadUInt32();
            for (int i = 0; i < colorCount; i++)
                pux.ColorEntries.Add(new PuxColorEntry
                {
                    X = reader.ReadUInt16(),
                    Y = reader.ReadUInt16(),
                    R = reader.ReadByte(),
                    G = reader.ReadByte(),
                    B = reader.ReadByte(),
                    A = reader.ReadByte(),
                });
            return (null, pux);
        }
    }

    // ── Group section (TerrainGroups + PuzzleUnitGroups share this layout) ────
    // [4] version  [2] count  × count: Name1(GBK) + Name2(ASCII) + Name3(ASCII) + 5×int32
    private static List<PuxGroup> ReadGroups(BinaryReader r, string label)
    {
        uint version = r.ReadUInt32();
        ushort count = r.ReadUInt16();
        var list = new List<PuxGroup>(count);

        for (int i = 0; i < count; i++)
            list.Add(new PuxGroup
            {
                // Name1 — Chinese display name (GBK). Not used for rendering.
                Name1 = ReadName(r, System.Text.Encoding.ASCII, label, i, 1),
                // Name2 — animation file path ("ANI\ZF.ANI"). Register with IAniDictionary.
                Name2 = ReadName(r, System.Text.Encoding.ASCII, label, i, 2),
                // Name3 — animation frame key ("Puzzle280"). Used as lookup key in GetFrames.
                Name3 = ReadName(r, System.Text.Encoding.ASCII, label, i, 3),
                Field1 = r.ReadInt32(),   // animation property (observed: 5)
                Field2 = r.ReadInt32(),   // animation property (observed: 6)
                Field3 = r.ReadInt32(),   // tile h-block count (observed: 4)
                Field4 = r.ReadInt32(),   // tile v-block count (observed: 1)
                Field5 = r.ReadInt32(),   // z-order           (observed: -1)
            });

        Debug.WriteLine($"[Pux] {label}: version={version} count={count}");
        return list;
    }

    // [2] len  [len] bytes  — engine asserts len ≤ 1023
    private static string ReadName(BinaryReader r, System.Text.Encoding enc,
        string label, int gi, int ni)
    {
        ushort len = r.ReadUInt16();
        if (len > 1023)
            throw new InvalidDataException(
                $"[Pux] {label}[{gi}].Name{ni} length {len} > {1023}");
        return enc.GetString(r.ReadBytes(len));
    }



    public int GetTileSize(Puzzle puzzle, IPackageReader packageReader)
    {
        var aniPath = Path.Combine(_conquerDirectory, puzzle.AniPath);
        if (!File.Exists(aniPath))
            return 0;

        using var aniStream = new FileStream(aniPath, FileMode.Open);
        var ani = new AniParser().Parse(aniStream);

        var tile = puzzle.Tiles[0, 0];
        if (tile == -1)
            return 0;

        var aniPuzzleKey = $"Puzzle{tile}";
        var frames = ani.GetFrames(aniPuzzleKey);
        if (frames.Count == 0)
            return 0;

        using var textureStream = packageReader.LoadFile(frames[0]);
        var extension = Path.GetExtension(frames[0]).ToLowerInvariant();

        if (extension == ".dds")
        {
            return DDSHelper.GetWidth(textureStream);
        }

        // For non-DDS files, we need a GraphicsDevice which we'll get from the game control
        return 0; // Will be detected later when GraphicsDevice is available
    }

    public int GetTileSize(Pux pux, IPackageReader packageReader)
    {

        foreach (var g in pux.TerrainGroups)
        {
            if (string.IsNullOrEmpty(g.Name2)) continue;

            var aniPath = Path.Combine(_conquerDirectory, g.Name2);
            if (!File.Exists(aniPath))
                return 0;
            using var aniStream = new FileStream(aniPath, FileMode.Open);
            var ani = new AniParser().Parse(aniStream);

            var aniPuzzleKey = g.Name3;
            var frames = ani.GetFrames(aniPuzzleKey);
            if (frames.Count == 0)
                return 0;

            using var textureStream = packageReader.LoadFile(frames[0]);
            var extension = Path.GetExtension(frames[0]).ToLowerInvariant();

            if (extension == ".dds")
            {
                return DDSHelper.GetWidth(textureStream);
            }
        }
        return 0;

    }
}