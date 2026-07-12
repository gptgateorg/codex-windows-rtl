using System.Diagnostics;
using System.IO;
using System.Text.Json;
using CodexRtlManager.Models;

namespace CodexRtlManager.Services;

public sealed class CodexLocator
{
    public async Task<CodexInstallation?> FindAsync(CancellationToken cancellationToken = default)
    {
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var candidates = new[]
        {
            Path.Combine(local, "Programs", "Codex"),
            Path.Combine(local, "Codex")
        };

        foreach (var root in candidates)
        {
            var found = FromRoot(root, false, "Desktop", null, null);
            if (found is not null)
            {
                return found;
            }
        }

        return await FindStorePackageAsync(cancellationToken);
    }

    private static CodexInstallation? FromRoot(
        string root,
        bool store,
        string version,
        string? preferredExecutable,
        string? appUserModelId)
    {
        if (!Directory.Exists(root))
        {
            return null;
        }

        var asarCandidates = new[]
        {
            Path.Combine(root, "resources", "app.asar"),
            Path.Combine(root, "app", "resources", "app.asar")
        };

        var asar = asarCandidates.FirstOrDefault(File.Exists);
        if (asar is null)
        {
            return null;
        }

        var appRoot = Directory.GetParent(Path.GetDirectoryName(asar)!)?.FullName ?? root;
        var explicitExecutable = string.IsNullOrWhiteSpace(preferredExecutable)
            ? null
            : Path.Combine(root, preferredExecutable.Replace('/', Path.DirectorySeparatorChar));
        var exe = explicitExecutable is not null && File.Exists(explicitExecutable)
            ? explicitExecutable
            : FindMainExecutable(appRoot);

        return exe is null ? null : new CodexInstallation(version, appRoot, asar, exe, store, appUserModelId);
    }

    private static async Task<CodexInstallation?> FindStorePackageAsync(CancellationToken cancellationToken)
    {
        const string command = "$p=Get-AppxPackage OpenAI.Codex | Select-Object -First 1; if($p){$a=(Get-AppxPackageManifest -Package $p).Package.Applications.Application | Select-Object -First 1; [pscustomobject]@{Version=$p.Version.ToString();InstallLocation=$p.InstallLocation;Executable=$a.Executable;AppUserModelId=($p.PackageFamilyName+'!'+$a.Id)}|ConvertTo-Json -Compress}";
        var start = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -NonInteractive -Command \"{command}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = Process.Start(start) ?? throw new InvalidOperationException("PowerShell اجرا نشد.");
        var stdout = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderr = await process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"تشخیص نسخه Microsoft Store ناموفق بود: {stderr.Trim()}");
        }

        if (string.IsNullOrWhiteSpace(stdout))
        {
            return null;
        }

        using var json = JsonDocument.Parse(stdout);
        var root = json.RootElement;
        var location = root.GetProperty("InstallLocation").GetString();
        var version = root.GetProperty("Version").GetString() ?? "Store";
        var executable = root.TryGetProperty("Executable", out var executableProperty) ? executableProperty.GetString() : null;
        var appUserModelId = root.TryGetProperty("AppUserModelId", out var idProperty) ? idProperty.GetString() : null;
        return string.IsNullOrWhiteSpace(location)
            ? null
            : FromRoot(location, true, version, executable, appUserModelId);
    }

    private static string? FindMainExecutable(string appRoot)
    {
        var candidates = Directory.EnumerateFiles(appRoot, "*.exe", SearchOption.TopDirectoryOnly).ToArray();
        return candidates.FirstOrDefault(path => string.Equals(Path.GetFileName(path), "ChatGPT.exe", StringComparison.OrdinalIgnoreCase))
            ?? candidates.FirstOrDefault(path => string.Equals(Path.GetFileName(path), "Codex.exe", StringComparison.OrdinalIgnoreCase))
            ?? candidates.FirstOrDefault(path => !IsHelperExecutable(Path.GetFileName(path)));
    }

    private static bool IsHelperExecutable(string name)
    {
        return name.Contains("uninstall", StringComparison.OrdinalIgnoreCase)
            || name.Contains("helper", StringComparison.OrdinalIgnoreCase)
            || name.Contains("service", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("chrome_", StringComparison.OrdinalIgnoreCase);
    }
}
