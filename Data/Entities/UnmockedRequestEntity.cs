using System.ComponentModel.DataAnnotations;

namespace Savio.MockServer.Data.Entities;

public class UnmockedRequestEntity
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(10)]
    public string Method { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(500)]
    public string Route { get; set; } = string.Empty;
    
    public string? RequestHeadersJson { get; set; }
    
    // Texto (json/raw) quando aplicável
    public string? RequestBody { get; set; }
    
    // Multipart/form-data ou x-www-form-urlencoded normalizado em JSON
    public string? RequestFormJson { get; set; }
    
    // Conteúdo binário do request quando aplicável (base64)
    public string? RequestBodyBase64 { get; set; }
    public string? RequestBodyContentType { get; set; }
    public string? RequestBodyFileName { get; set; }
    
    public DateTime FirstSeenAt { get; set; } = DateTime.UtcNow;
    
    public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;
    
    public int HitCount { get; set; } = 1;
    
    [MaxLength(50)]
    public string? LastClientIp { get; set; }
    
    public bool MockCreated { get; set; } = false;
    
    public DateTime? MockCreatedAt { get; set; }
}
