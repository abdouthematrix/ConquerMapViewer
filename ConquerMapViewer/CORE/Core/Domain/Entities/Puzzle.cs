namespace ConquerMapViewer.Core.Domain.Entities;
public sealed class Puzzle
{
    public string PuzzleType { get; set; } = string.Empty;
    public string AniPath { get; set; } = string.Empty;
    public int HorizontalTiles { get; set; }
    public int VerticalTiles { get; set; }
    public short[,] Tiles { get; set; } = new short[0, 0];
    public int? HorizontalRate { get; set; }
    public int? VerticalRate { get; set; }
    public int TileSize { get; set; }
    public int Width => HorizontalTiles * TileSize;
    public int Height => VerticalTiles * TileSize;
}
