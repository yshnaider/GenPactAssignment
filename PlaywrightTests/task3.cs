namespace PlaywrightTests;

using Allure.NUnit;
using NUnit.Framework;
using PlaywrightTests.pageModels;
using PlaywrightTests.utils;

/// <summary>
/// Task 3: Toggle Wikipedia appearance to Dark and verify dark/night styling is applied.
/// </summary>
[TestFixture]
[Parallelizable(ParallelScope.Self)]
[AllureNUnit]
public class Task3Tests : PlaywrightTestBase
{
    /// <summary>
    /// Sets Dark appearance and asserts the page reflects dark/night mode via CSS class changes.
    /// </summary>
    [Test]
    public async Task VerifyDarkMode()
    {
        var playwrightPage = new PlaywrightPage(Page);

        // Set appearance -> Color: Dark
        var appearance = new AppearanceMenu(playwrightPage.Page);
        await appearance.SetAppearanceOption(new AppearanceMenu.AppearanceSettings(Color: AppearanceMenu.ColorOptions.Dark));

        // Verify: Wikipedia toggles a dark-mode class on the root element (implementation may vary slightly).
        var htmlClass = await playwrightPage.Page.Locator("html").GetAttributeAsync("class") ?? string.Empty;
        var bodyClass = await playwrightPage.Page.Locator("body").GetAttributeAsync("class") ?? string.Empty;

        AllureUtils.AssertThat(
            "Assert: dark/night mode is enabled",
            htmlClass.Contains("dark", StringComparison.OrdinalIgnoreCase) ||
            bodyClass.Contains("dark", StringComparison.OrdinalIgnoreCase) ||
            htmlClass.Contains("night", StringComparison.OrdinalIgnoreCase) ||
            bodyClass.Contains("night", StringComparison.OrdinalIgnoreCase),
            $"Expected dark-mode/night-mode class after enabling Dark appearance. html.class='{htmlClass}', body.class='{bodyClass}'");
    }
}
