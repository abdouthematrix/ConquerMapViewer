namespace ConquerMapViewer.Core.Domain.Enums;

public enum MapCellAccessType : short
{
    Accessible = 0,
    Inaccessible = 1,
    Portal = 2,
    Terrain = 4,
    Scene = 8,
    Backdrop = 16,
    Effect = 32,
    Sound = 64
}