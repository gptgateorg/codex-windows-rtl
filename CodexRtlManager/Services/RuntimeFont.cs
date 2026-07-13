namespace CodexRtlManager.Services;

public sealed record RuntimeFont(string DisplayName, string MimeType, string Format, string Base64Data)
{
    public static RuntimeFont FromBytes(string displayName, string mimeType, string format, byte[] bytes) =>
        new(displayName, mimeType, format, Convert.ToBase64String(bytes));
}
