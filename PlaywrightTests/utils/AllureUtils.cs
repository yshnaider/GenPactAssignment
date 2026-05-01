namespace PlaywrightTests.utils;

using Microsoft.Playwright;
using NUnit.Framework;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;

/// <summary>
/// Small helpers for creating Allure steps and attaching artifacts from Playwright/NUnit tests.
/// </summary>
public static class AllureUtils
{
    /// <summary>
    /// Runs an NUnit boolean assertion inside an Allure step (so the assertion appears as a step in the report).
    /// </summary>
    [Allure.NUnit.Attributes.AllureStep("{name}")]
    public static void AssertThat(string name, bool condition, string? message = null)
        => Assert.That(condition, message ?? string.Empty);

    public static void AttachText(string name, string text, string fileName = "details.txt")
        => AttachBytes(name, Encoding.UTF8.GetBytes(text ?? string.Empty), "text/plain", fileName);

    // AttachJson intentionally omitted for now (not used in this project).

    public static async Task AttachScreenshotAsync(IPage page, string name = "screenshot", string fileName = "screenshot.png")
    {
        var bytes = await page.ScreenshotAsync(new() { FullPage = true });
        AttachBytes(name, bytes, "image/png", fileName);
    }

    public static async Task AttachPageHtmlAsync(IPage page, string name = "page.html", string fileName = "page.html")
    {
        var html = await page.ContentAsync();
        AttachBytes(name, Encoding.UTF8.GetBytes(html ?? string.Empty), "text/html", fileName);
    }

    public static async Task AttachPageArtifactsOnFailureAsync(IPage page, string prefix = "failure")
    {
        // Use from TearDown when a test fails.
        await AttachScreenshotAsync(page, $"{prefix}-screenshot", $"{prefix}-screenshot.png");
        await AttachPageHtmlAsync(page, $"{prefix}-html", $"{prefix}.html");
        AttachText($"{prefix}-url", page.Url, $"{prefix}-url.txt");
    }

    public static void AttachVideo(string videoPath, string name = "video", string fileName = "video.webm")
    {
        if (string.IsNullOrWhiteSpace(videoPath) || !File.Exists(videoPath))
        {
            return;
        }

        try
        {
            // Copy video into allure-results first; attaching from outside the results directory
            // is unreliable in some Allure.NUnit / Allure.Net.Commons versions.
            var bytes = File.ReadAllBytes(videoPath);
            var tmp = WriteToAllureResultsDir(bytes, "webm");
            if (tmp is null) return;
            TryAddAttachmentFromPath(name, "video/webm", tmp);
        }
        catch
        {
            // best-effort
        }
    }

    // Note: Allure.NUnit 2.14.0 doesn't expose an attachment attribute in this package set,
    // so attachments are added via lifecycle reflection helpers.

    private static void AttachBytes(string name, byte[] content, string mimeType, string fileName)
    {
        // Prefer attaching via file path; it's more reliable across Allure/NUnit integration states.
        var ext = Path.GetExtension(fileName);
        if (ext.StartsWith(".", StringComparison.Ordinal)) ext = ext[1..];
        if (string.IsNullOrWhiteSpace(ext)) ext = "bin";

        var tmp = WriteToAllureResultsDir(content, ext);
        if (tmp is null) return;
        TryAddAttachmentFromPath(name, mimeType, tmp);
    }

    private static void TryAddAttachmentFromPath(string name, string mimeType, string path)
    {
        try
        {
            var lifecycle = GetAllureLifecycleInstance();
            if (lifecycle is null) return;

            // Ensure we have an active step context; otherwise Allure may not link attachments
            // to the finished test (common when test is marked "broken").
            var attachmentStepUuid = TryStartAttachmentStep(lifecycle);
            InvokeAddAttachment(lifecycle, name, mimeType, path, Path.GetExtension(path));

            if (attachmentStepUuid is not null)
            {
                try
                {
                    InvokeInstanceMethod(lifecycle, "StopStep", new[] { typeof(string) }, new object[] { attachmentStepUuid });
                }
                catch { /* best-effort */ }
            }
        }
        catch
        {
            // no-op
        }
    }

    private static void InvokeAddAttachment(object lifecycle, string name, string mimeType, string path, string fileExt)
    {
        // Normalize ext to "png" rather than ".png"
        var ext = fileExt ?? string.Empty;
        if (ext.StartsWith(".", StringComparison.Ordinal)) ext = ext[1..];

        // Try common overloads across Allure.Commons versions:
        // - AddAttachment(string, string, string)
        // - AddAttachment(string, string, string, string)
        // - AddAttachment(string, string, byte[], string)
        try
        {
            InvokeInstanceMethod(
                lifecycle,
                "AddAttachment",
                new[] { typeof(string), typeof(string), typeof(string), typeof(string) },
                new object[] { name, mimeType, path, ext });
            return;
        }
        catch { /* try next */ }

        try
        {
            InvokeInstanceMethod(
                lifecycle,
                "AddAttachment",
                new[] { typeof(string), typeof(string), typeof(string) },
                new object[] { name, mimeType, path });
            return;
        }
        catch { /* try next */ }

        try
        {
            var bytes = File.ReadAllBytes(path);
            InvokeInstanceMethod(
                lifecycle,
                "AddAttachment",
                new[] { typeof(string), typeof(string), typeof(byte[]), typeof(string) },
                new object[] { name, mimeType, bytes, ext });
        }
        catch
        {
            // best-effort
        }
    }

    private static object? GetAllureLifecycleInstance()
    {
        // Allure packages differ between versions (Allure.Commons vs Allure.Net.Commons).
        var t =
            Type.GetType("Allure.Commons.AllureLifecycle, Allure.Commons") ??
            Type.GetType("Allure.Net.Commons.AllureLifecycle, Allure.Net.Commons");
        if (t is null) return null;
        var p = t.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
        return p?.GetValue(null);
    }

    private static string? GetResultsDirectory(object lifecycle)
    {
        try
        {
            var p = lifecycle.GetType().GetProperty("ResultsDirectory", BindingFlags.Public | BindingFlags.Instance);
            var v = p?.GetValue(lifecycle) as string;
            if (!string.IsNullOrWhiteSpace(v)) return v;
        }
        catch { /* ignore */ }

        try
        {
            var p = lifecycle.GetType().GetProperty("resultsDirectory", BindingFlags.Public | BindingFlags.Instance);
            var v = p?.GetValue(lifecycle) as string;
            if (!string.IsNullOrWhiteSpace(v)) return v;
        }
        catch { /* ignore */ }

        try
        {
            var cfg = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.WorkDirectory, "../../../allure-results"));
            return cfg;
        }
        catch
        {
            return null;
        }
    }

    private static string? WriteToAllureResultsDir(byte[] content, string ext)
    {
        try
        {
            var lifecycle = GetAllureLifecycleInstance();
            if (lifecycle is null) return null;

            var dir = GetResultsDirectory(lifecycle);
            if (string.IsNullOrWhiteSpace(dir)) return null;

            Directory.CreateDirectory(dir);
            var file = Path.Combine(dir, $"{Guid.NewGuid():N}.{ext}");
            File.WriteAllBytes(file, content);
            return file;
        }
        catch
        {
            return null;
        }
    }

    private static string? TryStartAttachmentStep(object lifecycle)
    {
        try
        {
            // Get AllureStorage (private field "storage") and find root step uuid.
            var storageField = lifecycle.GetType().GetField("storage", BindingFlags.NonPublic | BindingFlags.Instance);
            var storage = storageField?.GetValue(lifecycle);
            if (storage is null) return null;

            var getRoot = storage.GetType().GetMethod("GetRootStep", BindingFlags.Public | BindingFlags.Instance);
            var rootUuid = getRoot?.Invoke(storage, Array.Empty<object>()) as string;
            if (string.IsNullOrWhiteSpace(rootUuid)) return null;

            var stepResultType = Type.GetType("Allure.Commons.StepResult, Allure.Commons");
            if (stepResultType is null) return null;

            var stepResult = Activator.CreateInstance(stepResultType);
            stepResultType.GetProperty("name")?.SetValue(stepResult, "Attachments");
            stepResultType.GetProperty("Name")?.SetValue(stepResult, "Attachments");

            var stepUuid = Guid.NewGuid().ToString("N");
            InvokeInstanceMethod(
                lifecycle,
                "StartStep",
                new[] { typeof(string), typeof(string), stepResultType },
                new object[] { rootUuid!, stepUuid, stepResult! });

            return stepUuid;
        }
        catch
        {
            return null;
        }
    }

    private static void InvokeInstanceMethod(object instance, string methodName, Type[] signature, object[] args)
    {
        var m = instance.GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance, binder: null, types: signature, modifiers: null);
        if (m is null)
        {
            throw new MissingMethodException(instance.GetType().FullName, methodName);
        }
        m.Invoke(instance, args);
    }
}

