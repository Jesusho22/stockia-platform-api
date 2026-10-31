using System.ComponentModel.DataAnnotations;

namespace DataBit.StockIA.Platform.Iam.Interfaces.REST.Resources;

public record UpdateUserProfileResource(
    [Required, MinLength(3), MaxLength(120)] string FullName,
    [Required, EmailAddress, MaxLength(255)] string Email);
