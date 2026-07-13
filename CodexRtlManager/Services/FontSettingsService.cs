using System.Collections;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Text.Json;

namespace CodexRtlManager.Services;

public sealed class FontSettingsService
{
    private const int MaxFontBytes = 5 * 1024 * 1024;
    private const string DefaultFontResource = "resources/arad-regular.ttf";
    private readonly string _dataDirectory;
    private readonly string _settingsPath;

    public FontSettingsService(string? dataDirectory = null)
    {
        _dataDirectory = dataDirectory ?? AppPaths.DataDirectory;
        _settingsPath = Path.Combine(_dataDirectory, "font-settings.json");
        Directory.CreateDirectory(_dataDirectory);
    }

    public string ActiveFontName => ReadSettings()?.DisplayName ?? "Arad";

    public RuntimeFont GetActiveFont()
    {
        var settings = ReadSettings();
        if (settings is not null)
        {
            var path = GetStoredFontPath(settings.StoredFileName);
            if (path is not null && File.Exists(path))
            {
                var bytes = File.ReadAllBytes(path);
                var type = GetFontType(bytes);
                if (bytes.Length <= MaxFontBytes && type is not null)
                {
                    return RuntimeFont.FromBytes(settings.DisplayName, type.MimeType, type.Format, bytes);
                }
            }
        }

        return RuntimeFont.FromBytes("Arad", "font/ttf", "truetype", ReadEmbeddedArad());
    }

    public string Import(string sourcePath)
    {
        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException("فایل فونت پیدا نشد.", sourcePath);
        }

        var bytes = File.ReadAllBytes(sourcePath);
        if (bytes.Length == 0 || bytes.Length > MaxFontBytes)
        {
            throw new InvalidOperationException("حجم فونت باید کمتر از ۵ مگابایت باشد.");
        }

        var type = GetFontType(bytes)
            ?? throw new InvalidOperationException("فقط فایل معتبر TTF یا OTF پشتیبانی می‌شود.");
        var storedFileName = $"custom-font{type.Extension}";
        var destination = Path.Combine(_dataDirectory, storedFileName);
        foreach (var oldPath in new[] { "custom-font.ttf", "custom-font.otf" }
                     .Select(fileName => Path.Combine(_dataDirectory, fileName))
                     .Where(path => !string.Equals(path, destination, StringComparison.OrdinalIgnoreCase) && File.Exists(path)))
        {
            File.Delete(oldPath);
        }
        File.WriteAllBytes(destination, bytes);

        var displayName = Path.GetFileNameWithoutExtension(sourcePath);
        var json = JsonSerializer.Serialize(new FontSettings(displayName, storedFileName));
        File.WriteAllText(_settingsPath, json);
        return displayName;
    }

    public void ResetToArad()
    {
        foreach (var fileName in new[] { "custom-font.ttf", "custom-font.otf" })
        {
            var fontPath = Path.Combine(_dataDirectory, fileName);
            if (File.Exists(fontPath))
            {
                File.Delete(fontPath);
            }
        }

        if (File.Exists(_settingsPath))
        {
            File.Delete(_settingsPath);
        }
    }

    private FontSettings? ReadSettings()
    {
        try
        {
            return File.Exists(_settingsPath)
                ? JsonSerializer.Deserialize<FontSettings>(File.ReadAllText(_settingsPath))
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private string? GetStoredFontPath(string storedFileName) =>
        storedFileName is "custom-font.ttf" or "custom-font.otf"
            ? Path.Combine(_dataDirectory, storedFileName)
            : null;

    private static byte[] ReadEmbeddedArad()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly.GetManifestResourceNames()
            .Single(name => name.EndsWith(".g.resources", StringComparison.OrdinalIgnoreCase));
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException("فونت پیش‌فرض Arad پیدا نشد.");
        using var reader = new ResourceReader(stream);
        foreach (DictionaryEntry entry in reader)
        {
            if (!string.Equals(entry.Key?.ToString(), DefaultFontResource, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            using var fontStream = (Stream)entry.Value!;
            using var memory = new MemoryStream();
            fontStream.CopyTo(memory);
            return memory.ToArray();
        }

        throw new InvalidOperationException("فونت پیش‌فرض Arad پیدا نشد.");
    }

    private static FontType? GetFontType(byte[] bytes)
    {
        if (bytes.Length < 4)
        {
            return null;
        }

        if (bytes[0] == 0x00 && bytes[1] == 0x01 && bytes[2] == 0x00 && bytes[3] == 0x00)
        {
            return new FontType(".ttf", "font/ttf", "truetype");
        }

        if (bytes[0] == (byte)'O' && bytes[1] == (byte)'T' && bytes[2] == (byte)'T' && bytes[3] == (byte)'O')
        {
            return new FontType(".otf", "font/otf", "opentype");
        }

        return null;
    }

    private sealed record FontSettings(string DisplayName, string StoredFileName);
    private sealed record FontType(string Extension, string MimeType, string Format);
}
