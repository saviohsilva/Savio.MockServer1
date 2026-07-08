namespace Savio.MockServer.Models;

/// <summary>
/// Projeção leve do histórico de requisições para exibição em lista.
/// Carrega apenas os campos necessários para a tela de listagem,
/// evitando trazer colunas grandes (Request/Response body, base64, JSON, etc.).
/// </summary>
public sealed class RequestHistoryListItem
{
    public int Id { get; set; }
    public string Method { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public int ResponseStatusCode { get; set; }
    public int DelayMs { get; set; }
    public DateTime RequestedAt { get; set; }
    public string? ClientIp { get; set; }
    public int? MockEndpointId { get; set; }
    public string? MockEndpointDescription { get; set; }
}
