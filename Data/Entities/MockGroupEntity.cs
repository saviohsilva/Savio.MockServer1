using System.ComponentModel.DataAnnotations;

namespace Savio.MockServer.Data.Entities;

public class MockGroupEntity
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Proprietário
    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    // Navegação
    public ICollection<MockEndpointEntity> MockEndpoints { get; set; } = new List<MockEndpointEntity>();
}
