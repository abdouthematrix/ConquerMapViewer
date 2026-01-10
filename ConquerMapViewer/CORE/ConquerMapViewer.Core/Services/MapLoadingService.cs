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

        return (mapData, puzzle);
    }
}
