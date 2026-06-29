namespace ConquerMapViewer.Core.Services;

public sealed class MapLoadingService
{
    private readonly IPackageReader _packageReader;
    private readonly IMapFileLoader _mapFileLoader;
    private readonly IPuzzleFileLoader _puzzleFileLoader;
    private readonly IOtherDataFileLoader _otherDataFileLoader;

    public MapLoadingService(
        IPackageReader packageReader,
        IMapFileLoader mapFileLoader,
        IPuzzleFileLoader puzzleFileLoader,
        IOtherDataFileLoader otherDataFileLoader)

    {
        _packageReader = packageReader;
        _mapFileLoader = mapFileLoader;
        _puzzleFileLoader = puzzleFileLoader;
        _otherDataFileLoader = otherDataFileLoader;
    }

    public (MapData MapData, Puzzle Puzzle, Pux Pux) LoadMap(string path, int tileSize)
    {
        bool isNewFormat = DetectNewFormat(path);
        var otherData = TryLoadOtherData(path);

        using var mapstream = _packageReader.LoadFile(path);
        var mapData = _mapFileLoader.Load(mapstream,
            isNewFormat,
            otherData);
        mapData.Cells.CellDepth = 32;
        mapData.Cells.CellWidth = 64;

        using var puzzlesteam = _packageReader.LoadFile(mapData.PuzzlePath);
        var puzzlefile = _puzzleFileLoader.Load(mapData.PuzzlePath, puzzlesteam);
        if (puzzlefile.Puzzle != null)
        {
            var puzzleTileSize = _puzzleFileLoader.GetTileSize(puzzlefile.Puzzle, _packageReader);

            if (puzzleTileSize != tileSize && puzzleTileSize != 0)
            {
                tileSize = puzzleTileSize;
            }

            puzzlefile.Puzzle.TileSize = tileSize;
        }
        if (puzzlefile.Pux != null)
        {
            var puxTileSize = _puzzleFileLoader.GetTileSize(puzzlefile.Pux, _packageReader);

            if (puxTileSize != tileSize && puxTileSize != 0)
            {
                tileSize = puxTileSize;
            }

            puzzlefile.Pux.TileSize = tileSize;
        }
        // Load backdrop puzzles for all layers
        LoadBackdropPuzzles(mapData, tileSize);

        return (mapData, puzzlefile.Puzzle, puzzlefile.Pux);
    }

    // ── Format detection (FUN_00d2070b) ──────────────────────────────────────
    // Copy 4 chars before last '.' → _stricmp "_new"
    private static bool DetectNewFormat(string path)
    {
        int dot = path.LastIndexOf('.');
        return dot >= 4 &&
               path.Substring(dot - 4, 4)
                   .Equals("_new", StringComparison.OrdinalIgnoreCase);
    }

    // ── OtherData side-channel (FUN_00d21d57 / FUN_005d260b) ─────────────────
    private MapOtherData? TryLoadOtherData(string mapPath)
    {
        try
        {
            var basePath = Path.ChangeExtension(mapPath, null); // "map/map/newplain_new"
            using var s = _packageReader.LoadFile(basePath + ".OtherData");
            return s is null ? null
                 : _otherDataFileLoader.Load(s);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[OtherData] {mapPath}.OtherData: {ex.Message}");
            return null;
        }
    }


    private void LoadBackdropPuzzles(MapData mapData, int tileSize)
    {
        foreach (var layer in mapData.Layers)
        {
            foreach (var backdrop in layer.Backdrops)
            {
                try
                {
                    var stream = _packageReader.LoadFile(backdrop.PuzzlePath);
                    var backdropPuzzle = _puzzleFileLoader.Load(backdrop.PuzzlePath, stream);
                    // Detect tile size from the backdrop's own ANI/DDS, 
                    // not from the parent map's PUX tile size
                    var backdropTileSize = _puzzleFileLoader.GetTileSize(
                        backdropPuzzle.Puzzle, _packageReader);

                    backdropPuzzle.Puzzle.TileSize = backdropTileSize > 0
                        ? backdropTileSize
                        : tileSize;  // fallback to map tile size

                    // layer.RateX / layer.RateY are the CSceneLayer parallax move rates.
                    // They are not stored on the Puzzle (Puzzle.RollSpeedX/Y is the scroll
                    // animation speed, an unrelated field). The rates are consumed directly
                    // by BackdropDrawingComponent via MapViewerService.InitializeDrawingComponents().

                    backdrop.Puzzle = backdropPuzzle.Puzzle;
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