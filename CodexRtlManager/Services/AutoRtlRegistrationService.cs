using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace CodexRtlManager.Services;

public sealed class AutoRtlRegistrationService
{
    public const string BackgroundArgument = "--background";
    public const string BackgroundMutexName = "Local\\GptGateRTL.Background";
    public const string BackgroundStopEventName = "Local\\GptGateRTL.StopBackground";

    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RunValueName = "GptGateRTL";
    private readonly RuntimeSessionStore _sessions = new();

    public bool IsEnabled
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
            return key?.GetValue(RunValueName) is string value && !string.IsNullOrWhiteSpace(value);
        }
    }

    public void Enable()
    {
        var executable = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(executable) || !File.Exists(executable))
        {
            throw new InvalidOperationException("مسیر فایل اجرایی GptGate RTL پیدا نشد.");
        }

        using (var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, true))
        {
            key.SetValue(RunValueName, BuildRunCommand(executable), RegistryValueKind.String);
        }

        StartBackground(executable);
    }

    public void Disable()
    {
        using (var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true))
        {
            key?.DeleteValue(RunValueName, false);
        }

        SignalBackgroundToStop();
        _sessions.Clear();
    }

    public static string BuildRunCommand(string executablePath) =>
        $"\"{executablePath.Replace("\"", "\\\"")}\" {BackgroundArgument}";

    private static void StartBackground(string executable)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = executable,
            Arguments = BackgroundArgument,
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        });
    }

    private static void SignalBackgroundToStop()
    {
        try
        {
            using var stopEvent = EventWaitHandle.OpenExisting(BackgroundStopEventName);
            stopEvent.Set();
        }
        catch (WaitHandleCannotBeOpenedException)
        {
            // Background mode is not currently running.
        }
    }
}
