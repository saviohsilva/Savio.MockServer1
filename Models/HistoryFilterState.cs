namespace Savio.MockServer.Models;

public sealed class HistoryFilterState
{
    public string? UserId { get; set; }
    public int? MockEndpointId { get; set; }
    public string? Method { get; set; }
    public string? RouteContains { get; set; }
    public int? StatusCode { get; set; }
    public string? TextContains { get; set; }

    public DateTime? FromUtc { get; private set; }
    public DateTime? ToUtc { get; private set; }

    public bool IsDateRangeValid { get; private set; } = true;

    public void SetDateRange(DateTime? fromUtc, DateTime? toUtc, bool isValid)
    {
        IsDateRangeValid = isValid;
        FromUtc = fromUtc;
        ToUtc = toUtc;
    }

    public void Clear()
    {
        UserId = null;
        MockEndpointId = null;
        Method = null;
        RouteContains = null;
        StatusCode = null;
        TextContains = null;
        FromUtc = null;
        ToUtc = null;
        IsDateRangeValid = true;
    }

    public RequestHistoryFilter ToFilter()
    {
        return new RequestHistoryFilter
        {
            UserId = UserId,
            MockEndpointId = MockEndpointId,
            Method = string.IsNullOrWhiteSpace(Method) ? null : Method,
            RouteContains = string.IsNullOrWhiteSpace(RouteContains) ? null : RouteContains,
            ResponseStatusCode = StatusCode,
            TextContains = string.IsNullOrWhiteSpace(TextContains) ? null : TextContains,
            FromUtc = IsDateRangeValid ? FromUtc : null,
            ToUtc = IsDateRangeValid ? ToUtc : null,
            IncludeMockEndpoint = true
        };
    }
}
