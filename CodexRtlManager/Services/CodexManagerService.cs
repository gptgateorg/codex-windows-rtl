using System.Diagnostics;
using CodexRtlManager.Models;

namespace CodexRtlManager.Services;

public sealed class CodexManagerService
{
    private readonly CodexLocator _locator = new();
    private readonly FontSettingsService _fonts = new();
    private readonly AutoRtlRegistrationService _autoRtl = new();
    private readonly RuntimeRtlService _runtimeRtl;

    public CodexManagerService()
    {
        AppPaths.EnsureCreated();
        _runtimeRtl = new RuntimeRtlService(_fonts);
    }

    public string ActiveFontName => _fonts.ActiveFontName;

    public bool IsPersistentRtlEnabled => _autoRtl.IsEnabled;

    public void EnablePersistentRtl() => _autoRtl.Enable();

    public void DisablePersistentRtl() => _autoRtl.Disable();

    public string ImportFont(string path) => _fonts.Import(path);

    public void ResetFont() => _fonts.ResetToArad();

    public async Task<ManagerSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var installation = await _locator.FindAsync(cancellationToken);
            if (installation is null)
            {
                return new ManagerSnapshot(
                    RuntimeState.NotFound,
                    "ChatGPT Desktop پیدا نشد",
                    "نسخه رسمی ChatGPT را نصب کنید، سپس «بررسی دوباره» را بزنید.",
                    null,
                    false);
            }

            return new ManagerSnapshot(
                RuntimeState.Ready,
                "آماده اجرای RTL",
                $"نسخه {installation.Version} بدون تغییر فایل‌های برنامه اجرا می‌شود.",
                installation,
                true);
        }
        catch (Exception exception)
        {
            return new ManagerSnapshot(RuntimeState.Error, "بررسی ناموفق بود", exception.Message, null, false);
        }
    }

    public bool IsChatGptRunning() =>
        Process.GetProcessesByName("ChatGPT").Length > 0 ||
        Process.GetProcessesByName("Codex").Length > 0;

    public async Task<RuntimeLaunchResult> LaunchAsync(
        bool useExistingProfile = false,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var installation = await _locator.FindAsync(cancellationToken)
            ?? throw new InvalidOperationException("ChatGPT Desktop پیدا نشد.");
        return await _runtimeRtl.LaunchAndInjectAsync(installation, useExistingProfile, progress, cancellationToken);
    }

    public async Task<RuntimeLaunchResult> RestartCurrentChatGptWithRtlAsync(
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default,
        int gracefulCloseAttempts = 20)
    {
        var installation = await _locator.FindAsync(cancellationToken)
            ?? throw new InvalidOperationException("ChatGPT Desktop پیدا نشد.");

        progress?.Report("در حال بستن پنجره‌های ChatGPT…");
        foreach (var process in FindChatGptProcesses().Where(process => process.MainWindowHandle != IntPtr.Zero))
        {
            process.CloseMainWindow();
        }

        for (var attempt = 0; attempt < gracefulCloseAttempts && FindChatGptProcesses().Any(); attempt++)
        {
            await Task.Delay(250, cancellationToken);
        }

        foreach (var process in FindChatGptProcesses())
        {
            try
            {
                process.Kill(true);
                await process.WaitForExitAsync(cancellationToken);
            }
            catch (InvalidOperationException)
            {
                // The process exited between discovery and termination.
            }
        }

        return await _runtimeRtl.LaunchAndInjectAsync(installation, true, progress, cancellationToken);
    }

    public Task<int> EnsureRtlAsync(int debugPort, CancellationToken cancellationToken = default) =>
        _runtimeRtl.EnsureInjectedAsync(debugPort, cancellationToken);

    private static Process[] FindChatGptProcesses() =>
        Process.GetProcessesByName("ChatGPT")
            .Concat(Process.GetProcessesByName("Codex"))
            .GroupBy(process => process.Id)
            .Select(group => group.First())
            .ToArray();
}
