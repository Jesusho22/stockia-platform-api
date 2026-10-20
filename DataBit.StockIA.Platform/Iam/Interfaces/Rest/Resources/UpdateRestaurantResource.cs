using System.ComponentModel.DataAnnotations;

namespace DataBit.StockIA.Platform.Iam.Interfaces.Rest.Resources;

public record UpdateRestaurantResource(
    [Required, MinLength(2), MaxLength(150)] string Name,
    [MaxLength(255)] string? Address,
    [MaxLength(30), RegularExpression("^\\+?[\\d\\s-]{6,20}$", ErrorMessage = "Enter a valid phone number.")] string? Phone);
