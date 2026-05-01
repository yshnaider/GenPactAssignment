namespace PlaywrightTests.pageModels;

using Microsoft.Playwright;
using Allure.NUnit.Attributes;
using PlaywrightTests.utils;


/// <summary>
/// Page object for the Wikipedia "Appearance" menu (text size, width, color theme).
/// Markup varies between Vector versions, so interactions are intentionally defensive.
/// </summary>
public class AppearanceMenu
{
    // Content section locators
    public readonly ILocator wrapper;
    public readonly ILocator text;
    public readonly ILocator width;
    public readonly ILocator color;

    public enum TextOptions
    {
        Small,
        Standard,
        Large
    }

    public enum WidthOptions
    {
        Standard,
        Wide
    }

    public enum ColorOptions
    {
        Automatic,
        Light,
        Dark
    }

    public record AppearanceSettings(
        TextOptions? Text = null,
        WidthOptions? Width = null,
        ColorOptions? Color = null
    );




    /// <summary>Creates appearance-menu locators relative to the provided <see cref="IPage"/>.</summary>
    public AppearanceMenu(IPage page)
    {
        Page = page;
        Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        wrapper = Page.Locator("#vector-appearance");
        text = wrapper.Locator(".vector-menu-content").Nth(0);

        // Locator creation is lazy; it won't fail until used.
        width = wrapper.Locator(".vector-menu-content").Nth(1);
        color = wrapper.Locator(".vector-menu-content").Nth(2);

    }

    /// <summary>
    /// Applies any non-null appearance settings by clicking the corresponding controls.
    /// </summary>
    [AllureStep("Set appearance options: {settings}")]
    public async Task SetAppearanceOption(AppearanceSettings settings)
    {
        await wrapper.ScrollIntoViewIfNeededAsync();
        await EnsureExpandedAsync();

        if (settings.Text is { } textOption)
        {
            await ClickOptionInSectionAsync(text, textOption.ToString());
        }

        if (settings.Width is { } widthOption)
        {
            await ClickOptionInSectionAsync(width, widthOption.ToString());
        }

        if (settings.Color is { } colorOption)
        {
            await ClickOptionInSectionAsync(color, colorOption.ToString());
        }
    }

    private async Task EnsureExpandedAsync()
    {
        // Vector appearance is a collapsible menu. On Wikipedia this is usually a dropdown
        // with a label/button that has aria-expanded.
        var content = wrapper.Locator(".vector-menu-content");
        if (await content.First.IsVisibleAsync())
        {
            return;
        }

        // Try a few likely toggles (Wikipedia Vector skin changes markup occasionally).
        var candidates = new[]
        {
            wrapper.Locator("[aria-expanded]"),
            wrapper.Locator(".vector-dropdown-label"),
            wrapper.GetByRole(AriaRole.Button),
            wrapper.GetByRole(AriaRole.Link),
        };

        foreach (var c in candidates)
        {
            if (await c.CountAsync() == 0) continue;

            var el = c.First;
            var expanded = await el.GetAttributeAsync("aria-expanded");
            if (string.Equals(expanded, "true", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            await el.ClickAsync();
            if (await content.First.IsVisibleAsync())
            {
                return;
            }
        }

        // Last resort: click wrapper itself and wait briefly.
        await UiActions.RunAsync(() => wrapper.ClickAsync());
        await content.First.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 5000 });
    }

    private static async Task ClickOptionInSectionAsync(ILocator section, string optionLabel)
    {
        // Try common patterns for Vector appearance controls.
        var byLabel = section.GetByLabel(optionLabel, new() { Exact = true });
        if (await byLabel.CountAsync() > 0)
        {
            await UiActions.RunAsync(() => byLabel.First.ClickAsync());
            return;
        }

        var byText = section.GetByText(optionLabel, new() { Exact = true });
        if (await byText.CountAsync() > 0)
        {
            await UiActions.RunAsync(() => byText.First.ClickAsync());
        }
    }

    /// <summary>Underlying Playwright page.</summary>
    public IPage Page { get; }
}
