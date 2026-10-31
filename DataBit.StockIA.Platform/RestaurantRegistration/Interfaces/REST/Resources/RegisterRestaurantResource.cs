using System.ComponentModel.DataAnnotations;

namespace DataBit.StockIA.Platform.RestaurantRegistration.Interfaces.REST.Resources;

/// <summary>Same rules as the Web Application form: password with 8+ characters, letters and numbers.</summary>
public record RegisterRestaurantResource(
    [Required, MinLength(3), MaxLength(120)] string FullName,
    [Required, MinLength(2), MaxLength(150)] string RestaurantName,
    [Required, EmailAddress, MaxLength(255)] string Email,
    [Required, MinLength(8), MaxLength(72), RegularExpression("^(?=.*[A-Za-z])(?=.*\\d).+$", ErrorMessage = "The password needs letters and numbers.")] string Password);

/// <summary>The registered owner, already signed in (same shape as the IAM sign-in response).</summary>
public record RegisteredOwnerResource(
    int Id,
    string FullName,
    string Email,
    string Role,
    int RestaurantId,
    string RestaurantName,
    string RestaurantAddress,
    string RestaurantPhone,
    string Token);
