namespace PlaywrightTests.pageModels;

using Microsoft.Playwright;
using Allure.NUnit.Attributes;
using PlaywrightTests.utils;


/// <summary>
/// Page object for the Wikipedia table-of-contents panel (Vector skin).
/// Exposes commonly used TOC entries and helpers to expand/collapse grouped items.
/// </summary>
public class ContentsList
{
    // Content section locators
    public readonly ILocator contentsWrapper;
    public readonly ILocator top;
    public readonly ILocator playwrightTest;
    public readonly ILocator history;
    public readonly ILocator usageAndExamples;

    // Subsection list
    public readonly ILocator configuration;
    public readonly ILocator debuggingFeatures;

    public readonly ILocator reporters;
    public readonly ILocator usageTrends;
    public readonly ILocator references;
    public readonly ILocator furtherReading;
    public readonly ILocator externalLinks;




    /// <summary>Creates TOC locators relative to the provided <see cref="IPage"/>.</summary>
    public ContentsList(IPage page)
    {
        Page = page;
        Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        contentsWrapper = Page.Locator("#mw-panel-toc-list");

        // Locator creation is lazy; it won't fail until used.
        top = contentsWrapper.Locator("#toc-mw-content-text");
        playwrightTest = contentsWrapper.Locator("toc-@playwright/test");
        history = contentsWrapper.Locator("#toc-History");

        // Wrapper of a list
        usageAndExamples = contentsWrapper.Locator("#toc-Usage_and_examples");

        // list
        configuration = contentsWrapper.Locator("#toc-Configuration");
        debuggingFeatures = contentsWrapper.Locator("#toc-Debugging_features");
        reporters = contentsWrapper.Locator("#toc-Reporters");
        usageTrends = contentsWrapper.Locator("#toc-Usage_Trends");

        references = contentsWrapper.Locator("#toc-References");
        furtherReading = contentsWrapper.Locator("#toc-Further_reading");
        externalLinks = contentsWrapper.Locator("#toc-External_links");
    }

    public IPage Page { get; }

    /// <summary>Expands the "Usage and examples" TOC group if it is currently collapsed.</summary>
    [AllureStep("Expand TOC group: Usage and examples")]
    public async Task ExpandUsagesAndExamples()
    {
        bool isExpanded = await usageAndExamples.GetAttributeAsync("aria-expanded") == "true";
        if (isExpanded)
        {
            return;
        }
        await UiActions.RunAsync(() => usageAndExamples.ClickAsync());
    }

    /// <summary>Collapses the "Usage and examples" TOC group if it is currently expanded.</summary>
    [AllureStep("Collapse TOC group: Usage and examples")]
    public async Task CollapseUsagesAndExamples()
    {
        bool isExpanded = await usageAndExamples.GetAttributeAsync("aria-expanded") == "true";
        if (!isExpanded)
        {
            return;
        }
        await UiActions.RunAsync(() => usageAndExamples.ClickAsync());
    }
}
