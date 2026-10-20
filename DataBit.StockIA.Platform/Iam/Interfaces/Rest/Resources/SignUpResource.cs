using System.ComponentModel.DataAnnotations;

namespace DataBit.StockIA.Platform.Iam.Interfaces.Rest.Resources;

/// <summary>Same rules as the Web Application form: password with 8+ characters, letters and numbers.</summary>
public record SignUpResource(
    [Required, MinLength(3), MaxLength(120)] string FullName,
    [Required, MinLength(2), MaxLength(150)] string RestaurantName,
    [Required, EmailAddress, MaxLength(255)] string Email,
    [Required, MinLength(8), MaxLength(72), RegularExpression("^(?=.*[A-Za-z])(?=.*\\d).+$", ErrorMessage = "The password needs letters and numbers.")] string Password);
