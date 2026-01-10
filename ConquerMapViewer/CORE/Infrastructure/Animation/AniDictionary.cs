namespace ConquerMapViewer.Infrastructure.Animation;

/// <summary>
/// Manages and caches animation index files from the Conquer directory
/// </summary>
public sealed class AniDictionary : IAniDictionary
{
    private readonly Dictionary<string, AnimationIndex> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _conquerDirectory;
    private readonly AniParser _parser = new();
    private readonly object _lock = new();

    public AniDictionary(string conquerDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(conquerDirectory);

        if (!Directory.Exists(conquerDirectory))
            throw new DirectoryNotFoundException($"Conquer directory not found: {conquerDirectory}");

        _conquerDirectory = conquerDirectory;
    }

    /// <summary>
    /// Loads and caches an animation index file
    /// </summary>
    /// <param name="aniPath">Relative path to .ani file (e.g., "data/map/puzzle.ani")</param>
    public void Add(string aniPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(aniPath);

        // Thread-safe check and add
        lock (_lock)
        {
            if (_cache.ContainsKey(aniPath))
                return;

            var fullPath = Path.Combine(_conquerDirectory, aniPath);

            if (!File.Exists(fullPath))
            {
                // Cache empty index to avoid repeated file system checks
                _cache[aniPath] = new AnimationIndex();
                return;
            }

            try
            {
                var index = _parser.ParseFile(fullPath);
                _cache[aniPath] = index;
            }
            catch (Exception ex)
            {
                // Log error and cache empty index
                Console.WriteLine($"Failed to parse {aniPath}: {ex.Message}");
                _cache[aniPath] = new AnimationIndex();
            }
        }
    }

    /// <summary>
    /// Gets frames for a specific animation in a specific .ani file
    /// </summary>
    public IReadOnlyList<string> this[string aniPath, string animationName]
    {
        get
        {
            if (string.IsNullOrWhiteSpace(aniPath) || string.IsNullOrWhiteSpace(animationName))
                return Array.Empty<string>();

            // Ensure file is loaded
            if (!_cache.ContainsKey(aniPath))
                Add(aniPath);

            if (_cache.TryGetValue(aniPath, out var index))
            {
                var frames = index.GetFrames(animationName);
                return frames.AsReadOnly();
            }

            return Array.Empty<string>();
        }
    }

    /// <summary>
    /// Gets a specific frame from an animation
    /// </summary>
    public string? GetFrame(string aniPath, string animationName, int frameIndex = 0)
    {
        if (string.IsNullOrWhiteSpace(aniPath) || string.IsNullOrWhiteSpace(animationName))
            return null;

        // Ensure file is loaded
        if (!_cache.ContainsKey(aniPath))
            Add(aniPath);

        if (_cache.TryGetValue(aniPath, out var index))
        {
            return index.GetFrame(animationName, frameIndex);
        }

        return null;
    }

    /// <summary>
    /// Checks if an animation index is loaded
    /// </summary>
    public bool IsLoaded(string aniPath)
    {
        return _cache.ContainsKey(aniPath);
    }

    /// <summary>
    /// Gets the number of cached animation indices
    /// </summary>
    public int CachedCount => _cache.Count;

    /// <summary>
    /// Clears the entire cache
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _cache.Clear();
        }
    }

    /// <summary>
    /// Removes a specific animation index from cache
    /// </summary>
    public bool Remove(string aniPath)
    {
        lock (_lock)
        {
            return _cache.Remove(aniPath);
        }
    }

    /// <summary>
    /// Pre-loads multiple animation index files
    /// </summary>
    public void AddRange(IEnumerable<string> aniPaths)
    {
        ArgumentNullException.ThrowIfNull(aniPaths);

        foreach (var aniPath in aniPaths)
        {
            Add(aniPath);
        }
    }

    /// <summary>
    /// Gets all loaded animation index paths
    /// </summary>
    public IReadOnlyCollection<string> GetLoadedPaths()
    {
        return _cache.Keys.ToList().AsReadOnly();
    }
}

/// <summary>
/// Extension methods for IAniDictionary
/// </summary>
public static class AniDictionaryExtensions
{
    /// <summary>
    /// Tries to get frames for an animation
    /// </summary>
    public static bool TryGetFrames(this IAniDictionary dictionary, string aniPath, string animationName, out IReadOnlyList<string> frames)
    {
        frames = dictionary[aniPath, animationName];
        return frames.Count > 0;
    }

    /// <summary>
    /// Gets the frame count for a specific animation
    /// </summary>
    public static int GetFrameCount(this IAniDictionary dictionary, string aniPath, string animationName)
    {
        var frames = dictionary[aniPath, animationName];
        return frames.Count;
    }

    /// <summary>
    /// Checks if a specific animation exists in an .ani file
    /// </summary>
    public static bool HasAnimation(this IAniDictionary dictionary, string aniPath, string animationName)
    {
        var frames = dictionary[aniPath, animationName];
        return frames.Count > 0;
    }
}