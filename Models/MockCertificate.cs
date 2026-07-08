namespace Savio.MockServer.Models;

public class MockCertificate
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Thumbprint { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public bool HasPassword { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
}
