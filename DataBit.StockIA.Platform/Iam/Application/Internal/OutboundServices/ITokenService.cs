using System.Security.Claims;
using DataBit.StockIA.Platform.Iam.Domain.Model.Aggregates;

namespace DataBit.StockIA.Platform.Iam.Application.Internal.OutboundServices;

public interface ITokenService
{
    string GenerateToken(User user);
    ClaimsPrincipal? ValidateToken(string token);
}
