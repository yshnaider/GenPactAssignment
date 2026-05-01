namespace PlaywrightTests.pageModels;

using Microsoft.Playwright;
using PlaywrightTests.Wikipedia;
using Allure.NUnit.Attributes;
using PlaywrightTests.utils;


/// <summary>
/// Page object for the Wikipedia "Playwright (software)" page.
/// Encapsulates section scraping and navigation to the External links subsection used in tasks.
/// </summary>
public class PlaywrightPage
{
    public readonly ContentsList _contentsList;
    private ILocator? _externalLinksToolsContainer;

    /// <summary>Creates a page object around an already-open <see cref="IPage"/>.</summary>
    public PlaywrightPage(IPage page)
    {
        Page = page;
        Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        _contentsList = new ContentsList(page);
    }

    /// <summary>Underlying Playwright page.</summary>
    public IPage Page { get; }

    /// <summary>
    /// Reads visible text lines from a named section by taking paragraphs and the first list directly after the heading.
    /// Kept intentionally narrow to match the scope of the API parsing used for comparisons.
    /// </summary>
    [AllureStep("Read section lines: {0}")]
    public async Task<List<string>> ReadSectionLines(string sectionText)
    {
        // Use a hash set or list to store the results
        List<string> lines = new List<string>();

        // Define the base locator for the section
        var section = Page.Locator(".mw-heading.mw-heading3").Filter(new() { HasText = sectionText });

        // 1. Get all paragraphs - AllTextContentsAsync returns an IReadOnlyList<string>
        // We use AddRange to add each paragraph as its own line in our list
        var paragraphs = await section.Locator("+ p").AllTextContentsAsync();
        lines.AddRange(paragraphs);

        // 2. Get all list items (li) inside any ul in this section
        var listItems = await section.Locator("~ ul").First.AllTextContentsAsync();
        lines.AddRange(listItems);

        // 3. Clean up the data (optional but recommended)
        // Removes empty strings or whitespace-only entries
        return lines
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => line.Trim())
            .ToList();
    }

    /// <summary>
    /// Collects unique words from the DOM for the "Debugging features" section.
    /// </summary>
    [AllureStep("Collect unique words from DOM: Debugging features")]
    public async Task<IReadOnlySet<string>> UniqueWordsFromDomDebuggingFeaturesAsync()
    {
        var lines = await ReadSectionLines("Debugging features");
        return WordParsing.UniqueWords(lines);
    }

    /// <summary>
    /// Navigates to the "External links" section and expands the "Microsoft development tools" subsection
    /// so link assertions can query a stable container.
    /// </summary>
    [AllureStep("Open External links → Microsoft development tools")]
    public async Task OpenExternalLinksMicrosoftDevelopmentToolsAsync()
    {
        await ClickExternalLinksTocAsync();

        await ExpandMicrosoftDevelopmentToolsSectionAsync();

        // Corrected CSS: lowercase 'b' and using '*' for safer partial matching
        _externalLinksToolsContainer = Page.Locator("[aria-labelledby*='Microsoft_development_tools']").First;
    }

    [AllureStep("Click TOC: External links")]
    private async Task ClickExternalLinksTocAsync()
        => await _contentsList.externalLinks.ClickAsync();

    [AllureStep("Click: expand Microsoft development tools section")]
    private async Task ExpandMicrosoftDevelopmentToolsSectionAsync()
        => await Page.Locator("[aria-labelledby*='Microsoft_development_tools']").First.Locator("button").First.ClickAsync();

    /// <summary>
    /// Returns all anchor tags in the expanded "Microsoft development tools" container.
    /// Call <see cref="OpenExternalLinksMicrosoftDevelopmentToolsAsync"/> first.
    /// </summary>
    public ILocator MicrosoftDevelopmentToolsLinks()
    {
        if (_externalLinksToolsContainer is null)
        {
            throw new InvalidOperationException(
                "Microsoft development tools section is not opened. Call OpenExternalLinksMicrosoftDevelopmentToolsAsync() first.");
        }

        // Collect ALL anchors in the section (including those missing href/text)
        // so tests can detect non-text links or missing href.
        return _externalLinksToolsContainer.Locator("a");
    }

}
