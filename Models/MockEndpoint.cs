namespace Savio.MockServer.Models;

public class MockEndpoint
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Route { get; set; } = string.Empty;
    public string Method { get; set; } = "GET";
    public int StatusCode { get; set; } = 200;
    public Dictionary<string, string> Headers { get; set; } = [];
    public string ResponseBodyJson { get; set; } = string.Empty;
    public string ResponseBodyRaw { get; set; } = string.Empty;

    // Response binário (novo: referência a blob persistido)
    public int? ResponseBinaryBlobId { get; set; }

    // Response binário (legado: base64)
    public string ResponseBodyBase64 { get; set; } = string.Empty;
    public string ResponseBodyContentType { get; set; } = string.Empty;
    public string ResponseBodyFileName { get; set; } = string.Empty;

    // Response multipart (JSON de configuração)
    public string ResponseMultipartJson { get; set; } = string.Empty;

    public int DelayMs { get; set; } = 0;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastCalledAt { get; set; }
    public int CallCount { get; set; } = 0;

    public int? MockGroupId { get; set; }
    public string? MockGroupName { get; set; }
    public string? MockGroupColor { get; set; }

    public string FileName { get; set; } = string.Empty;
}
