using System.IO;

namespace CodexRtlManager.Services;

public static class AppPaths
{
    public static readonly string DataDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "CodexRtlManager");

    public static readonly string RuntimeUserDataDirectory = Path.Combine(DataDirectory, "RuntimeUserData");
    public static readonly string SettingsPath = Path.Combine(DataDirectory, "settings.json");

    public static void EnsureCreated()
    {
        Directory.CreateDirectory(DataDirectory);
        Directory.CreateDirectory(RuntimeUserDataDirectory);
    }
}
