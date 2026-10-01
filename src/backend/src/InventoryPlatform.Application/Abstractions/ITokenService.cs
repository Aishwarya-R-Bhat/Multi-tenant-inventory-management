using InventoryPlatform.Domain.Identity;

namespace InventoryPlatform.Application.Abstractions;

public interface ITokenService
{
    (string Token, DateTime ExpiresAt) CreateAccessToken(User user, string tenantSlug, IReadOnlyCollection<string> permissions);

    /// <summary>Returns a random raw token for the client and its hash for storage.</summary>
    (string Raw, string Hash) CreateRefreshToken();
    string HashRefreshToken(string raw);
    DateTime RefreshTokenExpiry { get; }
}
