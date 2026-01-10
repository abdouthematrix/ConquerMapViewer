namespace ConquerMapViewer.Core.Interfaces;

/// <summary>
/// Interface for managing animation index files
/// </summary>
public interface IAniDictionary
{
    /// <summary>
    /// Loads and caches an animation index file
    /// </summary>
    void Add(string aniPath);

    /// <summary>
    /// Gets frames for a specific animation in a specific .ani file
    /// </summary>
    IReadOnlyList<string> this[string aniPath, string animationName] { get; }

    /// <summary>
    /// Gets a specific frame from an animation
    /// </summary>
    string? GetFrame(string aniPath, string animationName, int frameIndex = 0);

    /// <summary>
    /// Checks if an animation index is loaded
    /// </summary>
    bool IsLoaded(string aniPath);

    /// <summary>
    /// Clears the cache
    /// </summary>
    void Clear();
}
