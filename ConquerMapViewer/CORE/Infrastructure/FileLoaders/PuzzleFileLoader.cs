using ConquerMapViewer.Infrastructure.Extensions;
using ConquerMapViewer.Infrastructure.Graphics;

namespace ConquerMapViewer.Infrastructure.FileLoaders;

public sealed class PuzzleFileLoader : IPuzzleFileLoader
{
    private readonly string _conquerDirectory;

    public PuzzleFileLoader(string conquerDirectory)
    {
        _conquerDirectory = conquerDirectory;
    }

    public Puzzle Load(Stream stream)
    {
        using var reader = new BinaryReader(stream);

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
            puzzle.HorizontalRate = reader.ReadInt32();
            puzzle.VerticalRate = reader.ReadInt32();
        }

        return puzzle;
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
}
