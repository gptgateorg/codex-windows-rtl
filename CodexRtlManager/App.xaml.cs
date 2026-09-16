using System.Windows;
using CodexRtlManager.Services;

namespace CodexRtlManager;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        if (e.Args.Contains(AutoRtlRegistrationService.BackgroundArgument, StringComparer.OrdinalIgnoreCase))
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            _ = RunBackgroundAsync();
            return;
        }

        ShutdownMode = ShutdownMode.OnMainWindowClose;
        var window = new MainWindow();
        MainWindow = window;
        window.Show();
    }

    private async Task RunBackgroundAsync()
    {
        using var mutex = new Mutex(false, AutoRtlRegistrationService.BackgroundMutexName);
        var ownsMutex = false;

        try
        {
            try
            {
                ownsMutex = mutex.WaitOne(0);
            }
            catch (AbandonedMutexException)
            {
                ownsMutex = true;
            }

            if (!ownsMutex)
            {
                Shutdown(0);
                return;
            }

            using var stopEvent = new EventWaitHandle(
                false,
                EventResetMode.ManualReset,
                AutoRtlRegistrationService.BackgroundStopEventName);
            using var cancellation = new CancellationTokenSource();
            var monitorTask = new BackgroundRtlHost().RunAsync(cancellation.Token);
            var stopTask = WaitForSignalAsync(stopEvent);

            await Task.WhenAny(monitorTask, stopTask);
            cancellation.Cancel();

            try
            {
                await monitorTask;
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown after persistent RTL is disabled.
            }
        }
        finally
        {
            if (ownsMutex)
            {
                mutex.ReleaseMutex();
            }
            Shutdown(0);
        }
    }

    private static Task WaitForSignalAsync(WaitHandle waitHandle)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        RegisteredWaitHandle? registration = null;
        registration = ThreadPool.RegisterWaitForSingleObject(
            waitHandle,
            (_, _) =>
            {
                completion.TrySetResult();
                registration?.Unregister(null);
            },
            null,
            Timeout.Infinite,
            true);
        return completion.Task;
    }
}
