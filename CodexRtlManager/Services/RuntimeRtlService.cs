using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CodexRtlManager.Models;

namespace CodexRtlManager.Services;

public sealed record RuntimeLaunchResult(Process Process, int DebugPort, int InjectedTargets);

public sealed class RuntimeRtlService
{
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(3) };
    private readonly FontSettingsService _fonts;

    public RuntimeRtlService(FontSettingsService? fonts = null) =>
        _fonts = fonts ?? new FontSettingsService();

    public async Task<RuntimeLaunchResult> LaunchAndInjectAsync(
        CodexInstallation installation,
        bool useExistingProfile = false,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (!useExistingProfile)
        {
            Directory.CreateDirectory(AppPaths.RuntimeUserDataDirectory);
        }
        var port = ReservePort();
        var startInfo = new ProcessStartInfo
        {
            FileName = installation.ExecutablePath,
            WorkingDirectory = installation.RootDirectory,
            UseShellExecute = true
        };
        if (!useExistingProfile)
        {
            startInfo.ArgumentList.Add($"--user-data-dir={AppPaths.RuntimeUserDataDirectory}");
        }
        startInfo.ArgumentList.Add($"--remote-debugging-port={port}");

        progress?.Report("در حال اجرای نسخه رسمی ChatGPT…");
        var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("ChatGPT اجرا نشد.");

        try
        {
            var targets = await WaitForTargetsAsync(port, process, cancellationToken);
            progress?.Report("در حال فعال‌کردن RTL در رابط ChatGPT…");
            var injected = 0;
            var script = RuntimeRtlPayload.CreateScript(_fonts.GetActiveFont());
            foreach (var target in targets.Where(target => target.Type is "page" or "webview"))
            {
                if (string.IsNullOrWhiteSpace(target.WebSocketDebuggerUrl))
                {
                    continue;
                }
                if (await InjectTargetAsync(target.WebSocketDebuggerUrl, script, cancellationToken))
                {
                    injected++;
                }
            }

            if (injected == 0)
            {
                throw new InvalidOperationException("پنجره ChatGPT باز شد، اما تزریق RTL تأیید نشد.");
            }

            progress?.Report($"RTL در {injected} پنجره فعال شد.");
            return new RuntimeLaunchResult(process, port, injected);
        }
        catch
        {
            if (!process.HasExited)
            {
                process.Kill(true);
            }
            throw;
        }
    }

    private static int ReservePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static async Task<CdpTarget[]> WaitForTargetsAsync(int port, Process process, CancellationToken cancellationToken)
    {
        Exception? lastError = null;
        for (var attempt = 0; attempt < 40; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (process.HasExited)
            {
                throw new InvalidOperationException($"ChatGPT پیش از ساخت پنجره بسته شد. کد خروج: {process.ExitCode}");
            }

            try
            {
                var targets = await Client.GetFromJsonAsync<CdpTarget[]>($"http://127.0.0.1:{port}/json/list", cancellationToken);
                if (targets is { Length: > 0 })
                {
                    return targets;
                }
            }
            catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
            {
                lastError = exception;
            }
            await Task.Delay(500, cancellationToken);
        }

        throw new TimeoutException($"رابط تزریق ChatGPT در زمان مقرر آماده نشد. {lastError?.Message}");
    }

    private static async Task<bool> InjectTargetAsync(string webSocketUrl, string script, CancellationToken cancellationToken)
    {
        using var socket = new ClientWebSocket();
        await socket.ConnectAsync(new Uri(webSocketUrl), cancellationToken);
        var request = JsonSerializer.SerializeToUtf8Bytes(new
        {
            id = 1,
            method = "Runtime.evaluate",
            @params = new
            {
                expression = script,
                awaitPromise = true,
                returnByValue = true
            }
        });
        await socket.SendAsync(request, WebSocketMessageType.Text, true, cancellationToken);

        var buffer = new byte[64 * 1024];
        using var response = new MemoryStream();
        WebSocketReceiveResult result;
        do
        {
            result = await socket.ReceiveAsync(buffer, cancellationToken);
            response.Write(buffer, 0, result.Count);
        } while (!result.EndOfMessage);

        using var json = JsonDocument.Parse(response.ToArray());
        return !json.RootElement.TryGetProperty("error", out _)
            && json.RootElement.TryGetProperty("result", out var commandResult)
            && commandResult.TryGetProperty("result", out var evaluation)
            && evaluation.TryGetProperty("value", out var value)
            && value.ValueKind == JsonValueKind.True;
    }

    private sealed record CdpTarget(
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("webSocketDebuggerUrl")] string? WebSocketDebuggerUrl);
}
