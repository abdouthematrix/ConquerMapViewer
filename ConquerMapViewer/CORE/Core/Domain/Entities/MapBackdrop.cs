namespace ConquerMapViewer.Core.Domain.Entities;

public sealed class MapBackdrop
{
    public string PuzzlePath { get; set; } = string.Empty;
    public Puzzle? Puzzle { get; set; }
}
