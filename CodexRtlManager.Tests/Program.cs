using CodexRtlManager.Services;
using CodexRtlManager.Models;
using CodexRtlManager.Dialogs;
using System.Collections;
using System.Resources;
using System.Windows;

if (args.Length == 2 && args[0] == "--runtime")
{
    var executable = Path.GetFullPath(args[1]);
    if (!File.Exists(executable))
    {
        Console.Error.WriteLine($"ChatGPT executable not found: {executable}");
        return 2;
    }

    var root = Path.GetDirectoryName(executable)!;
    var installation = new CodexInstallation(
        "integration-test",
        root,
        Path.Combine(root, "resources", "app.asar"),
        executable,
        true);
    var result = await new RuntimeRtlService().LaunchAndInjectAsync(installation, false);
    Console.WriteLine($"PASS runtime injection reached {result.InjectedTargets} target(s) on loopback port {result.DebugPort}.");
    if (!result.Process.HasExited)
    {
        result.Process.Kill(true);
        await result.Process.WaitForExitAsync();
    }
    return 0;
}

var payload = RuntimeRtlPayload.CreateScript(
    new RuntimeFont("Test Font", "font/ttf", "truetype", "AAEAAA=="));

var tests = new (string Name, Action Run)[]
{
    ("payload returns a successful CDP value", () => AssertContains(payload, "; true")),
    ("payload detects Persian and Arabic text", () => AssertContains(payload, "\\u0590-\\u08FF")),
    ("payload preserves code direction", () => AssertContains(payload, "pre, code, kbd, samp")),
    ("payload ignores editable fields", () => AssertContains(payload, "[contenteditable=\"true\"]")),
    ("payload watches streamed messages", () => AssertContains(payload, "MutationObserver")),
    ("payload embeds the selected font", () =>
    {
        AssertContains(payload, "data:${fontMime};base64,${fontBase64}");
        AssertContains(payload, "AAEAAA==");
    }),
    ("legacy file patching is absent", AssertLegacyPatcherIsAbsent),
    ("open-source Arad typography weights are embedded", AssertAradFontsAreEmbedded),
    ("Arad is the default runtime font", AssertAradIsDefaultRuntimeFont),
    ("custom TTF selection persists and resets", AssertCustomFontPersists),
    ("restart dialog only proceeds after explicit confirmation", AssertRestartConfirmationDecision),
    ("custom title bar toggles between normal and maximized", AssertWindowStateToggle)
};

var failed = 0;
foreach (var test in tests)
{
    try
    {
        test.Run();
        Console.WriteLine($"PASS {test.Name}");
    }
    catch (Exception exception)
    {
        failed++;
        Console.Error.WriteLine($"FAIL {test.Name}: {exception.Message}");
    }
}

Console.WriteLine($"{tests.Length - failed}/{tests.Length} checks passed.");
return failed == 0 ? 0 : 1;

static void AssertContains(string value, string expected)
{
    if (!value.Contains(expected, StringComparison.Ordinal))
    {
        throw new InvalidOperationException($"Expected payload to contain: {expected}");
    }
}

static void AssertLegacyPatcherIsAbsent()
{
    var assembly = typeof(RuntimeRtlPayload).Assembly;
    if (assembly.GetType("CodexRtlManager.Services.AsarArchive") is not null)
    {
        throw new InvalidOperationException("Legacy ASAR patcher is still compiled.");
    }
}

static void AssertAradFontsAreEmbedded()
{
    var assembly = typeof(RuntimeRtlPayload).Assembly;
    var resourceName = assembly.GetManifestResourceNames()
        .Single(name => name.EndsWith(".g.resources", StringComparison.OrdinalIgnoreCase));
    using var stream = assembly.GetManifestResourceStream(resourceName)
        ?? throw new InvalidOperationException("WPF resources were not found.");
    using var reader = new ResourceReader(stream);
    var keys = reader.Cast<DictionaryEntry>()
        .Select(entry => entry.Key?.ToString() ?? string.Empty)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    foreach (var weight in new[] { "regular", "semibold", "bold", "extrabold" })
    {
        var expected = $"resources/arad-{weight}.ttf";
        if (!keys.Contains(expected))
        {
            throw new InvalidOperationException($"Missing embedded font: {expected}");
        }
    }

    if (!keys.Contains("resources/arad-ofl.txt"))
    {
        throw new InvalidOperationException("Arad OFL license is not embedded.");
    }
}

static void AssertAradIsDefaultRuntimeFont()
{
    var directory = Path.Combine(Path.GetTempPath(), $"gptgate-font-test-{Guid.NewGuid():N}");
    try
    {
        var font = new FontSettingsService(directory).GetActiveFont();
        if (font.DisplayName != "Arad" || font.MimeType != "font/ttf" || font.Base64Data.Length == 0)
        {
            throw new InvalidOperationException("Arad was not loaded as the default runtime font.");
        }
    }
    finally
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, true);
        }
    }
}

static void AssertCustomFontPersists()
{
    var directory = Path.Combine(Path.GetTempPath(), $"gptgate-font-test-{Guid.NewGuid():N}");
    Directory.CreateDirectory(directory);
    var source = Path.Combine(directory, "My Persian Font.ttf");
    File.WriteAllBytes(source, [0x00, 0x01, 0x00, 0x00, 0x47, 0x50, 0x54]);

    try
    {
        var settingsDirectory = Path.Combine(directory, "settings");
        var service = new FontSettingsService(settingsDirectory);
        var name = service.Import(source);
        var restored = new FontSettingsService(settingsDirectory).GetActiveFont();
        if (name != "My Persian Font" || restored.DisplayName != name || restored.MimeType != "font/ttf")
        {
            throw new InvalidOperationException("The selected custom font was not restored.");
        }

        service.ResetToArad();
        if (service.ActiveFontName != "Arad")
        {
            throw new InvalidOperationException("Reset did not restore Arad.");
        }
    }
    finally
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, true);
        }
    }
}

static void AssertRestartConfirmationDecision()
{
    if (!RestartConfirmationDecision.ShouldProceed(true)
        || RestartConfirmationDecision.ShouldProceed(false)
        || RestartConfirmationDecision.ShouldProceed(null))
    {
        throw new InvalidOperationException("Restart confirmation did not fail closed.");
    }
}

static void AssertWindowStateToggle()
{
    if (WindowStatePolicy.Toggle(WindowState.Normal) != WindowState.Maximized
        || WindowStatePolicy.Toggle(WindowState.Maximized) != WindowState.Normal
        || WindowStatePolicy.Toggle(WindowState.Minimized) != WindowState.Maximized)
    {
        throw new InvalidOperationException("Window state toggle did not preserve native title-bar behavior.");
    }
}
