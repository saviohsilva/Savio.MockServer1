using System.ComponentModel.DataAnnotations;

namespace Savio.MockServer.Data.Entities;

public class RequestHistoryEntity
{
    [Key]
    public int Id { get; set; }
    
    public int MockEndpointId { get; set; }
    
    [Required]
    [MaxLength(10)]
    public string Method { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(500)]
    public string Route { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? QueryString { get; set; }

    public string? RequestHeadersJson { get; set; }
    
    // Texto (json/raw) quando aplicável
    public string? RequestBody { get; set; }
    
    // Multipart/form-data ou x-www-form-urlencoded normalizado em JSON
    public string? RequestFormJson { get; set; }
    
    // Conteúdo binário de request quando aplicável (base64)
    public string? RequestBodyBase64 { get; set; }
    public string? RequestBodyContentType { get; set; }
    public string? RequestBodyFileName { get; set; }
    
    public int ResponseStatusCode { get; set; }
    
    public string? ResponseHeadersJson { get; set; }
    
    // Texto (json/raw) quando aplicável
    public string? ResponseBody { get; set; }
    
    // Conteúdo binário de response quando aplicável (base64)
    public string? ResponseBodyBase64 { get; set; }
    public string? ResponseBodyContentType { get; set; }
    public string? ResponseBodyFileName { get; set; }
    
    // Referência a blob para responses muito grandes
    public int? ResponseBinaryBlobId { get; set; }
    
    public int DelayMs { get; set; }
    
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    
    [MaxLength(50)]
    public string? ClientIp { get; set; }
    
    // Navegação
    public MockEndpointEntity MockEndpoint { get; set; } = null!;
}
