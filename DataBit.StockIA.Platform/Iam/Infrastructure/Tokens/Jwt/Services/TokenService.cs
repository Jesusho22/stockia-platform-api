using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using DataBit.StockIA.Platform.Iam.Application.Internal.OutboundServices;
using DataBit.StockIA.Platform.Iam.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.Iam.Infrastructure.Tokens.Jwt.Configuration;
using DataBit.StockIA.Platform.Shared.Domain.Model.ValueObjects;
using DataBit.StockIA.Platform.Shared.Infrastructure.Security;

namespace DataBit.StockIA.Platform.Iam.Infrastructure.Tokens.Jwt.Services;

/// <summary>
///     Issues and validates JWTs. Claims: NameIdentifier (user id), Email,
///     restaurant_id and Role ("ADMIN" / "EMPLOYEE") — the contract with
///     Shared's CurrentUserAccessor.
/// </summary>
public class TokenService(IOptions<TokenSettings> tokenSettings) : ITokenService
{
    private readonly TokenSettings _settings = tokenSettings.Value;

    private byte[] Key
    {
        get
        {
            var secret = Environment.ExpandEnvironmentVariables(_settings.Secret);
            if (secret.Length < 32 || secret.Contains('%'))
                throw new InvalidOperationException("TokenSettings:Secret (STOCKIA_JWT_SECRET) must have at least 32 characters.");
            return Encoding.UTF8.GetBytes(secret);
        }
    }

    public string GenerateToken(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity([
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(CurrentUserAccessor.RestaurantIdClaimType, user.RestaurantId.ToString()),
                new Claim(ClaimTypes.Role, user.Role.ToCode())
            ]),
            Expires = DateTime.UtcNow.AddDays(_settings.ExpirationDays),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Key), SecurityAlgorithms.HmacSha256Signature)
        };
        return tokenHandler.WriteToken(tokenHandler.CreateToken(descriptor));
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        if (string.IsNullOrEmpty(token)) return null;
        try
        {
            return new JwtSecurityTokenHandler().ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Key),
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero
            }, out _);
        }
        catch (Exception)
        {
            return null;
        }
    }
}
