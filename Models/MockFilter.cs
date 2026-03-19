namespace Savio.MockServer.Models;

public sealed class MockFilter
{
    public string? Method { get; set; }
    public string? RouteContains { get; set; }
    public bool? IsActive { get; set; }
    public string? DescriptionContains { get; set; }
    public int? MockGroupId { get; set; }
    public string? UserId { get; set; }

    public void Clear()
    {
        Method = null;
        RouteContains = null;
        IsActive = null;
        DescriptionContains = null;
        MockGroupId = null;
    }
}
