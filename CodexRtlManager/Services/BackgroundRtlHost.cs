using System.Diagnostics;
using System.IO;

namespace CodexRtlManager.Services;

public sealed class BackgroundRtlHost
{
    private readonly CodexManagerService _manager = new();
    private readonly RuntimeSessionStore _sessions = new();

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        WriteLog("Background RTL monitor started.");

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var session = _sessions.Read();
                if (session is not null && IsProcessRunning(session))
                {
                    await _manager.EnsureRtlAsync(session.DebugPort, cancellationToken);
                    await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
                    continue;
                }

                if (session is not null)
                {
                    _sessions.Clear();
                }

                if (_manager.IsChatGptRunning())
                {
                    await Task.Delay(700, cancellationToken);
                    if (_manager.IsChatGptRunning() && _sessions.Read() is null)
                    {
                        WriteLog("Unmanaged ChatGPT session detected; restarting with RTL.");
                        await _manager.RestartCurrentChatGptWithRtlAsync(
                            cancellationToken: cancellationToken,
                            gracefulCloseAttempts: 4);
                    }
                    continue;
                }

                await Task.Delay(800, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                WriteLog($"{exception.GetType().Name}: {exception.Message}");
                await Task.Delay(TimeSpan.FromSeconds(8), cancellationToken);
            }
        }

        WriteLog("Background RTL monitor stopped.");
    }

    private static bool IsProcessRunning(RuntimeSession session)
    {
        try
        {
            using var process = Process.GetProcessById(session.ProcessId);
            var expectedName = process.ProcessName is "ChatGPT" or "Codex";
            var startDifference = (new DateTimeOffset(process.StartTime) - session.StartedAt).Duration();
            return !process.HasExited && expectedName && startDifference < TimeSpan.FromSeconds(30);
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static void WriteLog(string message)
    {
        try
        {
            AppPaths.EnsureCreated();
            File.AppendAllText(
                AppPaths.BackgroundLogPath,
                $"{DateTimeOffset.Now:O} {message}{Environment.NewLine}");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // The monitor must stay alive even when diagnostics cannot be written.
        }
    }
}
