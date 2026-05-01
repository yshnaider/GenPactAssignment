namespace PlaywrightTests;

using Allure.NUnit;
using NUnit.Framework;
using PlaywrightTests.pageModels;
using PlaywrightTests.utils;
using PlaywrightTests.Wikipedia;

/// <summary>
/// Task 1: Compare unique words scraped from the Wikipedia UI vs Wikipedia API output.
/// </summary>
[TestFixture]
[Parallelizable(ParallelScope.Self)]
[AllureNUnit]
public class Task1Tests : PlaywrightTestBase
{
    /// <summary>
    /// Collects unique words from DOM and from API and asserts the sets match (case-insensitive).
    /// </summary>
    [Test]
    public async Task CompareUniqueWords()
    {
        var playwrightPage = new PlaywrightPage(Page);

        var domWords = await playwrightPage.UniqueWordsFromDomDebuggingFeaturesAsync();

        await using var request = await Playwright.APIRequest.NewContextAsync(new()
        {
            ExtraHTTPHeaders = new Dictionary<string, string>
            {
                ["User-Agent"] = "GenPactAssignmentPlaywrightTests/1.0",
                ["Accept"] = "application/json",
            }
        });
        var apiResult = await WikipediaApi.FetchDebuggingFeaturesTextAsync(request);
        var apiWords = apiResult.UniqueWords;

        var missingInApi = domWords.Except(apiWords, StringComparer.OrdinalIgnoreCase).OrderBy(x => x).Take(50).ToList();
        var extraInApi = apiWords.Except(domWords, StringComparer.OrdinalIgnoreCase).OrderBy(x => x).Take(50).ToList();

        AllureUtils.AssertThat(
            "Assert: DOM unique words match API unique words",
            domWords.SetEquals(apiWords),
            $"Unique words mismatch.\n" +
            $"DOM unique words: {domWords.Count}\n" +
            $"API unique words: {apiWords.Count}\n" +
            $"Missing in API (first 50): {string.Join(", ", missingInApi)}\n" +
            $"Extra in API (first 50): {string.Join(", ", extraInApi)}");
    }
}
