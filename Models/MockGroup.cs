namespace Savio.MockServer.Models;

public class MockGroup
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<MockEndpoint> MockEndpoints { get; set; } = new();
}
