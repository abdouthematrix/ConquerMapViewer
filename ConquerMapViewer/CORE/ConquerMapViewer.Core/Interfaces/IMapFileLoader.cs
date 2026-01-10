namespace ConquerMapViewer.Core.Interfaces;

public interface IMapFileLoader
{
    MapData Load(Stream stream);
}
