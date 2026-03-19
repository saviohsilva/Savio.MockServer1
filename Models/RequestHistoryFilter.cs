namespace Savio.MockServer.Models;

public sealed class RequestHistoryFilter
{
    public int? MockEndpointId { get; set; }
    public string? Method { get; set; }
    public string? RouteContains { get; set; }
    public int? ResponseStatusCode { get; set; }
    public string? TextContains { get; set; }
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
    public bool IncludeMockEndpoint { get; set; } = true;
}
