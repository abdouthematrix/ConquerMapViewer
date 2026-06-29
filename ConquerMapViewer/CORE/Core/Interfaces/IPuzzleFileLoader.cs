namespace ConquerMapViewer.Core.Interfaces;

public interface IPuzzleFileLoader
{
    (Puzzle Puzzle, Pux Pux) Load(string path, Stream stream);
    int GetTileSize(Puzzle puzzle, IPackageReader packageReader);
    int GetTileSize(Pux pux, IPackageReader packageReader);
}
