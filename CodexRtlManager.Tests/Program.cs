using CodexRtlManager.Services;
using CodexRtlManager.Models;
using System.Collections;
using System.Resources;

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

var tests = new (string Name, Action Run)[]
{
    ("payload returns a successful CDP value", () => AssertContains(RuntimeRtlPayload.Script, "; true")),
    ("payload detects Persian and Arabic text", () => AssertContains(RuntimeRtlPayload.Script, "\\u0590-\\u08FF")),
    ("payload preserves code direction", () => AssertContains(RuntimeRtlPayload.Script, "pre, code, kbd, samp")),
    ("payload ignores editable fields", () => AssertContains(RuntimeRtlPayload.Script, "[contenteditable=\"true\"]")),
    ("payload watches streamed messages", () => AssertContains(RuntimeRtlPayload.Script, "MutationObserver")),
    ("legacy file patching is absent", AssertLegacyPatcherIsAbsent),
    ("open-source Arad typography weights are embedded", AssertAradFontsAreEmbedded)
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
