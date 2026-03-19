using System.ComponentModel.DataAnnotations;

namespace Savio.MockServer.Data.Entities;

public sealed class MockBinaryBlobEntity
{
    [Key]
    public int Id { get; set; }

    [MaxLength(260)]
    public string? FileName { get; set; }

    [MaxLength(200)]
    public string? ContentType { get; set; }

    public byte[] Bytes { get; set; } = Array.Empty<byte>();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
