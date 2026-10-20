using System.ComponentModel.DataAnnotations;

namespace DataBit.StockIA.Platform.Iam.Interfaces.Rest.Resources;

public record SignInResource([Required, EmailAddress] string Email, [Required] string Password);
