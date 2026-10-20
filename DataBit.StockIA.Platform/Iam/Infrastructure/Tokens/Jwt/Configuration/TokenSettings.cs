namespace DataBit.StockIA.Platform.Iam.Infrastructure.Tokens.Jwt.Configuration;

public class TokenSettings
{
    /// <summary>HMAC-SHA256 key, at least 32 characters. Set STOCKIA_JWT_SECRET in production.</summary>
    public string Secret { get; set; } = string.Empty;

    public int ExpirationDays { get; set; } = 7;
}
