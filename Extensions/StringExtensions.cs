namespace HangScheduler.Api.Extensions;

public static class StringExtensions
{
    /// <summary>
    /// Combines the specified separator.
    /// </summary>
    /// <param name="paths">The paths.</param>
    /// <param name="separator">The separator.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException">paths</exception>
    public static string? Combine(this IEnumerable<string?> paths, char separator)
    {
        return paths == null
            ? throw new ArgumentNullException(nameof(paths))
            : string.Join(separator, paths.Where(p => !string.IsNullOrEmpty(p)));
    }

    /// <summary>
    /// Combines the trim.
    /// </summary>
    /// <param name="paths">The paths.</param>
    /// <param name="separator">The separator.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static string? CombineTrim(this IEnumerable<string?> paths, char separator)
    {
        ArgumentNullException.ThrowIfNull(paths);

        var pathList = paths.Where(p => !string.IsNullOrEmpty(p)).ToList();

        for (var i = 0; i < pathList.Count - 1; i++)
        {
            // Trim trailing separator from the current path if it exists
            if (pathList[i]!.EndsWith(separator))
            {
                pathList[i] = pathList[i]!.TrimEnd(separator);
            }
        }

        // Join paths with the separator
        return string.Join(separator, pathList);
    }


    /// <summary>
    ///     Extracts a substring starting at a specified index and pads with spaces if the desired length is not met.
    /// </summary>
    /// <param name="source">The source string to extract from.</param>
    /// <param name="startIndex">The starting index from which to extract the substring.</param>
    /// <param name="length">The desired length of the output string.</param>
    /// <returns>A substring of the specified length, padded with spaces if necessary.</returns>
    public static string ExtractAndPadWithSpaces(this string source, int startIndex, int length)
    {
        if (startIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(startIndex),
                $"Parameter startIndex must be non-negative (was {startIndex}).");
        }

        if (length < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(length),
                $"Parameter length must be non-negative (was {length}).");
        }

        // Calculate how many characters can be extracted
        var availableChars = Math.Max(0, Math.Min(source.Length - startIndex, length));

        // Get the substring and pad with spaces if necessary
        return (availableChars > 0 ? source.Substring(startIndex, availableChars) : string.Empty)
            .PadRight(length);
    }

    public static string NormalizePath(this string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            throw new ArgumentNullException(nameof(path));
        }

        // Replace all slashes with the current environment's directory separator
        return path.Replace('\\', Path.DirectorySeparatorChar)
            .Replace('/', Path.DirectorySeparatorChar);
    }
}