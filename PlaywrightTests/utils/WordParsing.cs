namespace PlaywrightTests.Wikipedia;

using System.Text.RegularExpressions;

/// <summary>
/// Text parsing helpers used to compute a stable set of unique words for UI/API comparisons.
/// </summary>
public static class WordParsing
{
    // Letters/digits + internal apostrophes/hyphens; lowercased for stable comparison.
    private static readonly Regex WordRegex = new(
        @"\b[\p{L}\p{Nd}]+(?:['’-][\p{L}\p{Nd}]+)*\b",
        RegexOptions.Compiled);
    private static readonly Regex DigitsOnlyRegex = new(@"^\p{Nd}+$", RegexOptions.Compiled);

    /// <summary>
    /// Extracts unique words from a single text blob (case-insensitive) while skipping purely numeric tokens.
    /// </summary>
    public static HashSet<string> UniqueWords(string text)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(text)) return set;

        foreach (Match m in WordRegex.Matches(text))
        {
            var w = m.Value.Trim();
            if (w.Length == 0) continue;
            if (DigitsOnlyRegex.IsMatch(w)) continue;
            set.Add(w);
        }

        return set;
    }

    /// <summary>Extracts unique words from multiple lines by unioning per-line results.</summary>
    public static HashSet<string> UniqueWords(IEnumerable<string> lines)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in lines)
        {
            foreach (var w in UniqueWords(line))
            {
                set.Add(w);
            }
        }

        return set;
    }
}

