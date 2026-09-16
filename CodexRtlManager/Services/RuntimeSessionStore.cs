using System.IO;
using System.Text.Json;

namespace CodexRtlManager.Services;

public sealed record RuntimeSession(int ProcessId, int DebugPort, DateTimeOffset StartedAt);

public sealed class RuntimeSessionStore
{
    private readonly string _path;

    public RuntimeSessionStore(string? path = null) =>
        _path = path ?? AppPaths.RuntimeSessionPath;

    public RuntimeSession? Read()
    {
        try
        {
            return File.Exists(_path)
                ? JsonSerializer.Deserialize<RuntimeSession>(File.ReadAllText(_path))
                : null;
        }
        catch (Exception exception) when (exception is IOException or JsonException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    public void Write(RuntimeSession session)
    {
        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(_path, JsonSerializer.Serialize(session));
    }

    public void Clear()
    {
        if (File.Exists(_path))
        {
            File.Delete(_path);
        }
    }
}
