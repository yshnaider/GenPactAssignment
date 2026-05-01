namespace PlaywrightTests.utils;

/// <summary>
/// Thin wrapper around async UI actions.
/// Kept Allure-free to avoid serializing Playwright/delegate objects into the report pipeline.
/// </summary>
public static class UiActions
{
    /// <summary>Runs an async action.</summary>
    public static async Task RunAsync(Func<Task> action) => await action();
    /// <summary>Runs an async action with a return value.</summary>
    public static async Task<T> RunAsync<T>(Func<Task<T>> action) => await action();
}

