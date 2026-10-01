using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using InventoryPlatform.Application.Abstractions;
using InventoryPlatform.Domain.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace InventoryPlatform.Infrastructure.Security;

public class JwtTokenService(IOptions<JwtOptions> options) : ITokenService
{
    public const string TenantIdClaim = "tenant_id";
    public const string TenantSlugClaim = "tenant_slug";
    public const string PermissionClaim = "permission";

    private readonly JwtOptions _options = options.Value;

    public DateTime RefreshTokenExpiry => DateTime.UtcNow.AddDays(_options.RefreshTokenDays);

    public (string Token, DateTime ExpiresAt) CreateAccessToken(User user, string tenantSlug, IReadOnlyCollection<string> permissions)
    {
        var expires = DateTime.UtcNow.AddMinutes(_options.AccessTokenMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new("name", user.FullName),
            new(TenantIdClaim, user.TenantId.ToString()),
            new(TenantSlugClaim, tenantSlug)
        };
        claims.AddRange(permissions.Select(p => new Claim(PermissionClaim, p)));

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            Expires = expires,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key)), SecurityAlgorithms.HmacSha256)
        };
        return (new JsonWebTokenHandler().CreateToken(descriptor), expires);
    }

    public (string Raw, string Hash) CreateRefreshToken()
    {
        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        return (raw, HashRefreshToken(raw));
    }

    public string HashRefreshToken(string raw) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
}
