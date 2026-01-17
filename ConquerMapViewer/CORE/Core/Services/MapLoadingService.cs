namespace ConquerMapViewer.Core.Services;

public sealed class MapLoadingService
{
    private readonly IPackageReader _packageReader;
    private readonly IMapFileLoader _mapFileLoader;
    private readonly IPuzzleFileLoader _puzzleFileLoader;

    public MapLoadingService(
        IPackageReader packageReader,
        IMapFileLoader mapFileLoader,
        IPuzzleFileLoader puzzleFileLoader)
    {
        _packageReader = packageReader;
        _mapFileLoader = mapFileLoader;
        _puzzleFileLoader = puzzleFileLoader;
    }

    public (MapData MapData, Puzzle Puzzle) LoadMap(string path, int tileSize)
    {
        var mapData = _mapFileLoader.Load(_packageReader.LoadFile(path));
        mapData.Cells.CellDepth = 32;
        mapData.Cells.CellWidth = 64;

        var puzzle = _puzzleFileLoader.Load(_packageReader.LoadFile(mapData.PuzzlePath));
        var puzzleTileSize = _puzzleFileLoader.GetTileSize(puzzle, _packageReader);

        if (puzzleTileSize != tileSize && puzzleTileSize != 0)
        {
            tileSize = puzzleTileSize;
        }

        puzzle.TileSize = tileSize;

        // Load backdrop puzzles for all layers
        LoadBackdropPuzzles(mapData, tileSize);

        return (mapData, puzzle);
    }

    private void LoadBackdropPuzzles(MapData mapData, int tileSize)
    {
        foreach (var layer in mapData.Layers)
        {
            foreach (var backdrop in layer.Backdrops)
            {
                try
                {
                    var backdropPuzzle = _puzzleFileLoader.Load(_packageReader.LoadFile(backdrop.PuzzlePath));
                    backdropPuzzle.TileSize = tileSize;
                    backdropPuzzle.HorizontalRate = layer.xInt;
                    backdropPuzzle.VerticalRate = layer.yInt;

                    // Store the loaded puzzle in the backdrop object
                    backdrop.Puzzle = backdropPuzzle;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to load backdrop puzzle: {backdrop.PuzzlePath}, Error: {ex.Message}");
                    // Continue loading other backdrops even if one fails
                }
            }
        }
    }
}