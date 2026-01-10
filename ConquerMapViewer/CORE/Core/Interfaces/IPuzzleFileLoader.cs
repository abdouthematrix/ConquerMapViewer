namespace ConquerMapViewer.Core.Interfaces;

public interface IPuzzleFileLoader
{
    Puzzle Load(Stream stream);
    int GetTileSize(Puzzle puzzle, IPackageReader packageReader);
}
