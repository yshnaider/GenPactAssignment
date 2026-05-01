namespace PlaywrightTests.Wikipedia;

using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Playwright;

/// <summary>
/// Minimal MediaWiki API client used for Task 1 comparisons.
/// Fetches a section by title, extracts a DOM-comparable subset of text, and computes unique words.
/// </summary>
public static class WikipediaApi
{
    /// <summary>
    /// Fetches the "Debugging features" section HTML from the MediaWiki API and returns a parsed text/word set.
    /// </summary>
    public static async Task<WikipediaSectionTextResult> FetchDebuggingFeaturesTextAsync(
        IAPIRequestContext request,
        string pageTitle = "Playwright_(software)",
        CancellationToken cancellationToken = default)
    {
        using var sectionsDoc = await GetJsonWithPlaywrightAsync(
            request,
            $"https://en.wikipedia.org/w/api.php?action=parse&page={Uri.EscapeDataString(pageTitle)}&prop=sections&format=json",
            cancellationToken);

        var sectionIndex = FindSectionIndex(sectionsDoc, "Debugging features");

        using var textDoc = await GetJsonWithPlaywrightAsync(
            request,
            $"https://en.wikipedia.org/w/api.php?action=parse&page={Uri.EscapeDataString(pageTitle)}&section={Uri.EscapeDataString(sectionIndex)}&prop=text&format=json",
            cancellationToken);

        var html = textDoc.RootElement
            .GetProperty("parse")
            .GetProperty("text")
            .GetProperty("*")
            .GetString() ?? string.Empty;

        var plainText = ExtractPlainTextFromHtml(html);
        var uniqueWords = WordParsing.UniqueWords(plainText);

        return new WikipediaSectionTextResult(sectionIndex, html, plainText, uniqueWords);
    }

    private static async Task<JsonDocument> GetJsonWithPlaywrightAsync(
        IAPIRequestContext request,
        string url,
        CancellationToken cancellationToken)
    {
        // NOTE: Wikipedia may return 403 for requests without a User-Agent.
        // Set headers on the request context (recommended), but keep a safety net here too.
        var response = await request.GetAsync(url, new()
        {
            Headers = new Dictionary<string, string>
            {
                ["User-Agent"] = "GenPactAssignmentPlaywrightTests/1.0",
                ["Accept"] = "application/json",
            }
        });
        if (!response.Ok)
        {
            throw new HttpRequestException($"Request failed: {(int)response.Status} {response.StatusText}");
        }

        var body = await response.TextAsync();
        return JsonDocument.Parse(body);
    }

    private static string FindSectionIndex(JsonDocument doc, string sectionLineEquals)
    {
        if (!doc.RootElement.TryGetProperty("parse", out var parse) ||
            !parse.TryGetProperty("sections", out var sections) ||
            sections.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException("Unexpected Wikipedia sections payload.");
        }

        foreach (var s in sections.EnumerateArray())
        {
            if (!s.TryGetProperty("line", out var lineProp)) continue;
            if (!string.Equals(lineProp.GetString(), sectionLineEquals, StringComparison.OrdinalIgnoreCase)) continue;

            // MediaWiki returns section ids as strings (e.g., "5")
            if (s.TryGetProperty("index", out var indexProp))
            {
                var index = indexProp.GetString();
                if (!string.IsNullOrWhiteSpace(index))
                {
                    return index;
                }
            }
        }

        throw new InvalidOperationException($"Section '{sectionLineEquals}' not found in Wikipedia TOC.");
    }

    private static string ExtractPlainTextFromHtml(string html)
    {
        if (string.IsNullOrWhiteSpace(html)) return string.Empty;

        // Drop scripts/styles first.
        var cleaned = Regex.Replace(html, "<(script|style)[^>]*>[\\s\\S]*?</\\1>", " ", RegexOptions.IgnoreCase);

        // The API returns HTML for the whole section. To match what we scrape from the DOM,
        // extract just paragraph text plus the first unordered list (ignoring reference/metadata lists).
        var chunks = new List<string>();

        foreach (Match m in Regex.Matches(cleaned, "<p\\b[^>]*>[\\s\\S]*?</p>", RegexOptions.IgnoreCase))
        {
            chunks.Add(m.Value);
        }

        var firstUl = Regex.Match(cleaned, "<ul\\b[^>]*>[\\s\\S]*?</ul>", RegexOptions.IgnoreCase);
        if (firstUl.Success)
        {
            chunks.Add(firstUl.Value);
        }

        var joined = chunks.Count > 0 ? string.Join("\n", chunks) : cleaned;

        // Remove all tags.
        joined = Regex.Replace(joined, "<[^>]+>", " ");

        // Decode entities (&nbsp; etc).
        joined = WebUtility.HtmlDecode(joined);

        // Normalize whitespace but keep line breaks as separators.
        joined = Regex.Replace(joined, "[ \\t\\f\\v]+", " ");
        joined = Regex.Replace(joined, "\\s*\\n\\s*", "\n").Trim();

        return joined;
    }
}

/// <summary>Result of fetching and parsing a Wikipedia section via the MediaWiki API.</summary>
public sealed record WikipediaSectionTextResult(
    string SectionIndex,
    string Html,
    string PlainText,
    IReadOnlySet<string> UniqueWords);

