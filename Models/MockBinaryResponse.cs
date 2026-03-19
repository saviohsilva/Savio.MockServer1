namespace Savio.MockServer.Models;

public sealed class MockBinaryResponse
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";

    /// <summary>
    /// Conteúdo do response em base64.
    /// </summary>
    public string Base64 { get; set; } = string.Empty;
}
