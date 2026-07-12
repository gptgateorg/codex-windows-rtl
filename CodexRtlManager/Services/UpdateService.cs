using System.Net.Http;
using System.Net.Http.Json;
using System.IO;
using System.Text.Json;

namespace CodexRtlManager.Services;

public sealed record UpdateSettings(string? ManifestUrl);
public sealed record UpdateManifest(string Version, string DownloadUrl, string Sha256, string[] SupportedCodexVersions);
public sealed record UpdateCheckResult(bool Configured, bool Available, string Message, UpdateManifest? Manifest);

public sealed class UpdateService
{
    private const string DefaultManifestUrl = "https://raw.githubusercontent.com/Hamham83/gptgate-rtl/main/update.json";
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(15) };

    public async Task<UpdateCheckResult> CheckAsync(Version currentVersion, CancellationToken cancellationToken = default)
    {
        var settings = await LoadSettingsAsync(cancellationToken);
        var manifestUrl = string.IsNullOrWhiteSpace(settings?.ManifestUrl)
            ? DefaultManifestUrl
            : settings.ManifestUrl;

        if (!Uri.TryCreate(manifestUrl, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidDataException("نشانی بروزرسانی باید HTTPS باشد.");
        }

        var manifest = await Client.GetFromJsonAsync<UpdateManifest>(uri, cancellationToken)
            ?? throw new InvalidDataException("فایل بروزرسانی معتبر نیست.");
        if (!Version.TryParse(manifest.Version, out var remoteVersion))
        {
            throw new InvalidDataException("شماره نسخه بروزرسانی معتبر نیست.");
        }

        return remoteVersion > currentVersion
            ? new UpdateCheckResult(true, true, $"نسخه {remoteVersion} آماده است.", manifest)
            : new UpdateCheckResult(true, false, "برنامه به‌روز است.", manifest);
    }

    private static async Task<UpdateSettings?> LoadSettingsAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(AppPaths.SettingsPath))
        {
            return null;
        }
        await using var stream = File.OpenRead(AppPaths.SettingsPath);
        return await JsonSerializer.DeserializeAsync<UpdateSettings>(stream, cancellationToken: cancellationToken);
    }
}
