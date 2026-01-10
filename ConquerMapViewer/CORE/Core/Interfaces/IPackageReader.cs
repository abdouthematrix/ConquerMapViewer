namespace ConquerMapViewer.Core.Interfaces;

public interface IPackageReader : IDisposable
{
    void AddPackage(string fileName);
    Stream LoadFile(string fileName);
}
