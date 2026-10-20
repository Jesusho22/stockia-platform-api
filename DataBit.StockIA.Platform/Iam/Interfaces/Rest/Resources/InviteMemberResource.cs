using System.ComponentModel.DataAnnotations;

namespace DataBit.StockIA.Platform.Iam.Interfaces.Rest.Resources;

/// <summary>TemporaryPassword is optional; the configured invitation password is used when it is missing.</summary>
public record InviteMemberResource(
    [Required, MinLength(3), MaxLength(120)] string FullName,
    [Required, EmailAddress, MaxLength(255)] string Email,
    [Required] string Role,
    [MinLength(8), MaxLength(72)] string? TemporaryPassword = null);
