namespace PlaywrightTests;

using System.Text.RegularExpressions;
using Allure.NUnit;
using Microsoft.Playwright;
using NUnit.Framework;
using PlaywrightTests.pageModels;
using PlaywrightTests.utils;

/// <summary>
/// Task 2: Validate that external links in a specific section are navigational links
/// (have visible text and a non-empty href).
/// </summary>
[TestFixture]
[Parallelizable(ParallelScope.Self)]
[AllureNUnit]
public partial class Task2Tests : PlaywrightTestBase
{
    [GeneratedRegex(".+")]
    private static partial Regex NonEmptyRegex();

    /// <summary>
    /// Iterates through all links and asserts text and href are present.
    /// This test is allowed to fail to surface non-navigational anchors.
    /// </summary>
    [Test]
    public async Task CheckIfTextLinks()
    {
        var playwrightPage = new PlaywrightPage(Page);
        await playwrightPage.OpenExternalLinksMicrosoftDevelopmentToolsAsync();

        var links = playwrightPage.MicrosoftDevelopmentToolsLinks();
        var count = await links.CountAsync();
        AllureUtils.AssertThat(
            "Assert: tools section has at least one link",
            count > 0,
            "Expected at least one link in the tools section.");

        for (var i = 0; i < count; i++)
        {
            var link = links.Nth(i);
            var text = (await link.TextContentAsync())?.Trim() ?? string.Empty;

            AllureUtils.AssertThat(
                $"Assert: link[{i}] has visible text",
                !string.IsNullOrWhiteSpace(text),
                $"Found a non-text link at index {i}.");

            var href = (await link.GetAttributeAsync("href"))?.Trim() ?? string.Empty;
            AllureUtils.AssertThat(
                $"Assert: link[{i}] has href",
                !string.IsNullOrWhiteSpace(href),
                $"Expected link[{i}] to have a non-empty href attribute. " +
                $"text='{text}', href='{href}'");
        }
    }
}
