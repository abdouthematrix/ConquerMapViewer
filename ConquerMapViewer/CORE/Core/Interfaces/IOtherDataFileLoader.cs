namespace ConquerMapViewer.Core.Interfaces;

public interface IOtherDataFileLoader
{
    MapOtherData? Load(Stream s, int layerCount);
}
