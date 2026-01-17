namespace ConquerMapViewer.Core.Interfaces;

public interface ISceneFileLoader
{
    Scene Load(Stream stream);
}