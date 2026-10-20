using DataBit.StockIA.Platform.Iam.Application.Internal.OutboundServices;

namespace DataBit.StockIA.Platform.Iam.Infrastructure.Hashing.BCrypt.Services;

public class HashingService : IHashingService
{
    public string HashPassword(string password) => global::BCrypt.Net.BCrypt.HashPassword(password);

    public bool VerifyPassword(string password, string passwordHash) =>
        !string.IsNullOrEmpty(passwordHash) && global::BCrypt.Net.BCrypt.Verify(password, passwordHash);
}
