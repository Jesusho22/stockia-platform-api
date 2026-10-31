using System.ComponentModel.DataAnnotations;

namespace DataBit.StockIA.Platform.Iam.Interfaces.REST.Resources;

public record SignInResource([Required, EmailAddress] string Email, [Required] string Password);
