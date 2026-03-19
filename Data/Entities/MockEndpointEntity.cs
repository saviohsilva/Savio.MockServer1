using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Savio.MockServer.Data.Entities;

public class MockEndpointEntity
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(500)]
    public string Route { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(10)]
    public string Method { get; set; } = "GET";
    
    public int StatusCode { get; set; } = 200;
    
    public string? HeadersJson { get; set; }
    
    public string? ResponseBodyJson { get; set; }
    
    public string? ResponseBodyRaw { get; set; }
    
    // Response binário (persistido como blob)
    public int? ResponseBinaryBlobId { get; set; }

    // Response binário (legado base64)
    public string? ResponseBodyBase64 { get; set; }
    public string? ResponseBodyContentType { get; set; }
    public string? ResponseBodyFileName { get; set; }
    
    // Response multipart/mixed em JSON (configuração)
    public string? ResponseMultipartJson { get; set; }
    
    public int DelayMs { get; set; }
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public int CallCount { get; set; }
    
    public DateTime? LastCalledAt { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Agrupamento
    public int? MockGroupId { get; set; }
    public MockGroupEntity? MockGroup { get; set; }

    // Proprietário
    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    // Navegação
    public ICollection<RequestHistoryEntity> RequestHistory { get; set; } = new List<RequestHistoryEntity>();
    
    // Helpers
    public Dictionary<string, string> GetHeaders()
    {
        if (string.IsNullOrEmpty(HeadersJson))
            return new Dictionary<string, string>();
            
        return JsonSerializer.Deserialize<Dictionary<string, string>>(HeadersJson) 
            ?? new Dictionary<string, string>();
    }
    
    public void SetHeaders(Dictionary<string, string> headers)
    {
        HeadersJson = JsonSerializer.Serialize(headers);
    }
}
