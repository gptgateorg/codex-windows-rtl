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
    private readonly RuntimeSessionStore _sessions;
    private readonly HashSet<string> _registeredTargets = new(StringComparer.Ordinal);

    public RuntimeRtlService(FontSettingsService? fonts = null, RuntimeSessionStore? sessions = null)
    {
        _fonts = fonts ?? new FontSettingsService();
        _sessions = sessions ?? new RuntimeSessionStore();
    }

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
        var arguments = BuildLaunchArguments(port, useExistingProfile);

        progress?.Report("در حال اجرای نسخه رسمی ChatGPT…");
        var process = StartProcess(installation, arguments);
        _sessions.Write(new RuntimeSession(process.Id, port, DateTimeOffset.Now));

        try
        {
            await WaitForTargetsAsync(port, process, cancellationToken);
            progress?.Report("در حال فعال‌کردن RTL در رابط ChatGPT…");
            var injected = await EnsureInjectedAsync(port, cancellationToken);

            if (injected == 0)
            {
                throw new InvalidOperationException("پنجره ChatGPT باز شد، اما تزریق RTL تأیید نشد.");
            }

            progress?.Report($"RTL در {injected} پنجره فعال شد.");
            return new RuntimeLaunchResult(process, port, injected);
        }
        catch
        {
            _sessions.Clear();
            if (!process.HasExited)
            {
                process.Kill(true);
            }
            throw;
        }
    }

    public async Task<int> EnsureInjectedAsync(int port, CancellationToken cancellationToken = default)
    {
        var targets = await Client.GetFromJsonAsync<CdpTarget[]>(
            $"http://127.0.0.1:{port}/json/list",
            cancellationToken) ?? [];
        var script = RuntimeRtlPayload.CreateScript(_fonts.GetActiveFont());
        var injected = 0;

        foreach (var target in targets.Where(target => target.Type is "page" or "webview"))
        {
            if (string.IsNullOrWhiteSpace(target.WebSocketDebuggerUrl))
            {
                continue;
            }

            var registerForNewDocuments = _registeredTargets.Add(target.Id);
            try
            {
                if (await InjectTargetAsync(
                    target.WebSocketDebuggerUrl,
                    script,
                    registerForNewDocuments,
                    cancellationToken))
                {
                    injected++;
                }
            }
            catch
            {
                if (registerForNewDocuments)
                {
                    _registeredTargets.Remove(target.Id);
                }
                throw;
            }
        }

        return injected;
    }

    public static string BuildLaunchArguments(int port, bool useExistingProfile)
    {
        var arguments = new List<string>();
        if (!useExistingProfile)
        {
            arguments.Add($"--user-data-dir=\"{AppPaths.RuntimeUserDataDirectory}\"");
        }
        arguments.Add($"--remote-debugging-port={port}");
        return string.Join(' ', arguments);
    }

    private static Process StartProcess(CodexInstallation installation, string arguments)
    {
        if (installation.IsStorePackage)
        {
            if (string.IsNullOrWhiteSpace(installation.AppUserModelId))
            {
                throw new InvalidOperationException("شناسه اجرای نسخه Microsoft Store پیدا نشد.");
            }

            return StoreApplicationActivator.Activate(installation.AppUserModelId, arguments);
        }

        return Process.Start(new ProcessStartInfo
        {
            FileName = installation.ExecutablePath,
            WorkingDirectory = installation.RootDirectory,
            Arguments = arguments,
            UseShellExecute = false
        }) ?? throw new InvalidOperationException("ChatGPT اجرا نشد.");
    }

    private static int ReservePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static async Task WaitForTargetsAsync(int port, Process process, CancellationToken cancellationToken)
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
                    return;
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

    private static async Task<bool> InjectTargetAsync(
        string webSocketUrl,
        string script,
        bool registerForNewDocuments,
        CancellationToken cancellationToken)
    {
        if (registerForNewDocuments)
        {
            var registration = await SendCommandAsync(
                webSocketUrl,
                1,
                "Page.addScriptToEvaluateOnNewDocument",
                new { source = BuildDocumentStartupScript(script) },
                cancellationToken);
            _ = registration;
        }

        var response = await SendCommandAsync(
            webSocketUrl,
            2,
            "Runtime.evaluate",
            new
            {
                expression = script,
                awaitPromise = true,
                returnByValue = true
            },
            cancellationToken);

        return !response.TryGetProperty("error", out _)
            && response.TryGetProperty("result", out var commandResult)
            && commandResult.TryGetProperty("result", out var evaluation)
            && evaluation.TryGetProperty("value", out var value)
            && value.ValueKind == JsonValueKind.True;
    }

    public static string BuildDocumentStartupScript(string script) =>
        "(() => { const apply = () => { " + script +
        "; }; if (document.readyState === 'loading') { " +
        "document.addEventListener('DOMContentLoaded', apply, { once: true }); " +
        "} else { apply(); } })();";

    private static async Task<JsonElement> SendCommandAsync(
        string webSocketUrl,
        int id,
        string method,
        object parameters,
        CancellationToken cancellationToken)
    {
        using var socket = new ClientWebSocket();
        await socket.ConnectAsync(new Uri(webSocketUrl), cancellationToken);
        var request = JsonSerializer.SerializeToUtf8Bytes(new { id, method, @params = parameters });
        await socket.SendAsync(request, WebSocketMessageType.Text, true, cancellationToken);

        while (true)
        {
            var buffer = new byte[64 * 1024];
            using var response = new MemoryStream();
            WebSocketReceiveResult result;
            do
            {
                result = await socket.ReceiveAsync(buffer, cancellationToken);
                response.Write(buffer, 0, result.Count);
            } while (!result.EndOfMessage);

            using var json = JsonDocument.Parse(response.ToArray());
            if (json.RootElement.TryGetProperty("id", out var responseId)
                && responseId.GetInt32() == id)
            {
                return json.RootElement.Clone();
            }
        }
    }

    private sealed record CdpTarget(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("webSocketDebuggerUrl")] string? WebSocketDebuggerUrl);
}
