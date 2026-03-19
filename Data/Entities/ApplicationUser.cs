using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Savio.MockServer.Data.Entities;

public class ApplicationUser : IdentityUser
{
    [Required]
    [MaxLength(100)]
    public string Alias { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Método preferido de MFA: "Authenticator" (TOTP) ou "Email"
    /// </summary>
    [MaxLength(20)]
    public string MfaMethod { get; set; } = "Authenticator";
}
