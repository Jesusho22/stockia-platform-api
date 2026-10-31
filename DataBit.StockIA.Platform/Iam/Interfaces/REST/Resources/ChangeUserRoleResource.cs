using System.ComponentModel.DataAnnotations;

namespace DataBit.StockIA.Platform.Iam.Interfaces.REST.Resources;

public record ChangeUserRoleResource([Required] string Role);
