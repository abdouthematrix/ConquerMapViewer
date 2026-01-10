using System.Text.RegularExpressions;

namespace ConquerMapViewer.Infrastructure.Animation;

/// <summary>
/// Animation index (.ani file) containing texture frame paths
/// </summary>
public sealed class AnimationIndex
{
    public Dictionary<string, List<string>> Animations { get; set; } =
        new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets frames for specified animation name
    /// </summary>
    public List<string> GetFrames(string animationName)
    {
        if (Animations.TryGetValue(animationName, out var frames))
            return frames;
        return new List<string>();
    }

    /// <summary>
    /// Gets single frame (for non-animated textures)
    /// </summary>
    public string? GetFrame(string animationName, int frameIndex = 0)
    {
        var frames = GetFrames(animationName);
        if (frameIndex >= 0 && frameIndex < frames.Count)
            return frames[frameIndex];
        return null;
    }

    /// <summary>
    /// Gets the total number of animations
    /// </summary>
    public int AnimationCount => Animations.Count;

    /// <summary>
    /// Checks if an animation exists
    /// </summary>
    public bool HasAnimation(string animationName) =>
        Animations.ContainsKey(animationName);
}

/// <summary>
/// Parses .ani animation index files in INI format.
/// Format:
/// [Puzzle0]
/// FrameAmount=1
/// Frame0=data/map/puzzle/room/arena/arena000.dds
/// [Puzzle1]
/// FrameAmount=1
/// Frame0=data/map/puzzle/room/arena/arena001.dds
/// </summary>
public sealed class AniParser
{
    private static readonly Regex SectionRegex = new(@"^\[(.+)\]$", RegexOptions.Compiled);
    private static readonly Regex FrameRegex = new(@"^Frame(\d+)=(.+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex FrameAmountRegex = new(@"^FrameAmount=(\d+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Parses animation index from stream
    /// </summary>
    public AnimationIndex Parse(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var index = new AnimationIndex();
        string? currentSection = null;
        List<string>? currentFrames = null;

        using var reader = new StreamReader(stream, Encoding.ASCII);

        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine()?.Trim();

            if (string.IsNullOrWhiteSpace(line))
                continue;

            // Skip comments
            if (line.StartsWith(';') || line.StartsWith('#'))
                continue;

            // Check for section header [SectionName]
            var sectionMatch = SectionRegex.Match(line);
            if (sectionMatch.Success)
            {
                // Save previous section if it has frames
                if (currentSection != null && currentFrames != null && currentFrames.Count > 0)
                {
                    index.Animations[currentSection] = currentFrames;
                }

                // Start new section
                currentSection = sectionMatch.Groups[1].Value;
                currentFrames = new List<string>();
                continue;
            }

            if (currentFrames == null)
                continue; // Ignore data outside of sections

            // Check for FrameAmount (optional, used for pre-allocation)
            var frameAmountMatch = FrameAmountRegex.Match(line);
            if (frameAmountMatch.Success)
            {
                var expectedCount = int.Parse(frameAmountMatch.Groups[1].Value);
                currentFrames.Capacity = expectedCount;
                continue;
            }

            // Check for frame entry Frame0=path
            var frameMatch = FrameRegex.Match(line);
            if (frameMatch.Success)
            {
                var frameIndex = int.Parse(frameMatch.Groups[1].Value);
                var framePath = frameMatch.Groups[2].Value.Trim();

                // Ensure list is large enough (fill gaps with empty strings if needed)
                while (currentFrames.Count <= frameIndex)
                    currentFrames.Add(string.Empty);

                currentFrames[frameIndex] = framePath;
            }
        }

        // Save last section
        if (currentSection != null && currentFrames != null && currentFrames.Count > 0)
        {
            index.Animations[currentSection] = currentFrames;
        }

        return index;
    }

    /// <summary>
    /// Parses animation index from file path
    /// </summary>
    public AnimationIndex ParseFile(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Animation file not found: {filePath}", filePath);

        using var stream = File.OpenRead(filePath);
        return Parse(stream);
    }

    /// <summary>
    /// Parses animation index from string content
    /// </summary>
    public AnimationIndex ParseString(string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        using var stream = new MemoryStream(Encoding.ASCII.GetBytes(content));
        return Parse(stream);
    }
}

/// <summary>
/// Extension methods for AnimationIndex
/// </summary>
public static class AnimationIndexExtensions
{
    /// <summary>
    /// Gets all animation names
    /// </summary>
    public static IEnumerable<string> GetAnimationNames(this AnimationIndex index)
    {
        return index.Animations.Keys;
    }

    /// <summary>
    /// Tries to get frames for an animation
    /// </summary>
    public static bool TryGetFrames(this AnimationIndex index, string animationName, out List<string> frames)
    {
        return index.Animations.TryGetValue(animationName, out frames);
    }

    /// <summary>
    /// Gets frame count for a specific animation
    /// </summary>
    public static int GetFrameCount(this AnimationIndex index, string animationName)
    {
        if (index.Animations.TryGetValue(animationName, out var frames))
            return frames.Count;
        return 0;
    }
}