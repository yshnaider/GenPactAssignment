namespace PlaywrightTests;

using System;
using Microsoft.Playwright;
using NUnit.Framework;
using PlaywrightTests.utils;
using System.IO;

/// <summary>
/// Shared Playwright lifecycle for NUnit test fixtures.
/// Creates a fresh browser context + page per test and performs best-effort artifact capture on failures.
/// </summary>
public abstract class PlaywrightTestBase
{
    /// <summary>Playwright driver instance for the current test.</summary>
    protected IPlaywright Playwright { get; private set; } = null!;
    /// <summary>Launched browser instance for the current test.</summary>
    protected IBrowser Browser { get; private set; } = null!;
    /// <summary>Isolated browser context for the current test (video recording enabled).</summary>
    protected IBrowserContext Context { get; private set; } = null!;
    /// <summary>Page used by the current test.</summary>
    protected IPage Page { get; private set; } = null!;

    /// <summary>
    /// Initial navigation target. Override in a derived fixture when a different entry point is needed.
    /// </summary>
    protected virtual string BaseUrl => "https://en.wikipedia.org/wiki/Playwright_(software)#Debugging_features";

    /// <summary>
    /// Creates Playwright, launches a headed Chromium browser, enables video recording,
    /// navigates to <see cref="BaseUrl"/>, and waits for network idle.
    /// </summary>
    [SetUp]
    public async Task GlobalSetUpAsync()
    {
        Playwright = await Microsoft.Playwright.Playwright.CreateAsync();

        var isCi = string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase);

        Browser = await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = isCi,
            SlowMo = isCi ? 0 : 200,
        });

        var videoDir = Path.Combine(TestContext.CurrentContext.WorkDirectory, "playwright-videos");
        Directory.CreateDirectory(videoDir);

        Context = await Browser.NewContextAsync(new()
        {
            RecordVideoDir = videoDir,
            RecordVideoSize = new() { Width = 1280, Height = 720 }
        });

        Page = await Context.NewPageAsync();
        await Page.GotoAsync(BaseUrl);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    /// <summary>
    /// On non-passed outcomes, attaches page artifacts (HTML, URL, screenshot) and attempts to attach the recorded video.
    /// Video file paths are only available after the page is closed.
    /// </summary>
    [TearDown]
    public async Task GlobalTearDownAsync()
    {
        var status = TestContext.CurrentContext.Result.Outcome.Status;
        var shouldAttach = status != NUnit.Framework.Interfaces.TestStatus.Passed;
        var prefix = GetType().Name;

        if (shouldAttach)
        {
            await AllureUtils.AttachPageArtifactsOnFailureAsync(Page, prefix);
        }

        // Close page first to ensure the video is finalized on disk.
        try
        {
            await Page.CloseAsync();
        }
        catch
        {
            // best-effort
        }

        if (shouldAttach && Page.Video is not null)
        {
            try
            {
                var path = await Page.Video.PathAsync();
                AllureUtils.AttachVideo(path, $"{prefix}-video", $"{prefix}.webm");
            }
            catch
            {
                // best-effort
            }
        }

        await Context.CloseAsync();
        await Browser.CloseAsync();
        Playwright.Dispose();
    }
}

