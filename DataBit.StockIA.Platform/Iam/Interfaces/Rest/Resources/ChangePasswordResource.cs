using System.ComponentModel.DataAnnotations;

namespace DataBit.StockIA.Platform.Iam.Interfaces.Rest.Resources;

public record ChangePasswordResource(
    [Required] string CurrentPassword,
    [Required, MinLength(8), MaxLength(72), RegularExpression("^(?=.*[A-Za-z])(?=.*\\d).+$", ErrorMessage = "The password needs letters and numbers.")] string NewPassword);
