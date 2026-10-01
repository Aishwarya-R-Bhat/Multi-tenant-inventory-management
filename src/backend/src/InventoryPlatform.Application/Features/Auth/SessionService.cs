using InventoryPlatform.Application.Abstractions;
using InventoryPlatform.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace InventoryPlatform.Application.Features.Auth;

/// <summary>Issues an access token plus a stored refresh token for a user. Shared by register, login and refresh.</summary>
public class SessionService(IAppDbContext db, ITokenService tokens)
{
    public async Task<AuthResponse> IssueAsync(User user, string tenantSlug, CancellationToken ct)
    {
        // No tenant is resolved yet during login/refresh, so the global filter is bypassed and TenantId is checked by hand.
        var permissions = await db.UserRoles.IgnoreQueryFilters()
            .Where(ur => ur.TenantId == user.TenantId && ur.UserId == user.Id)
            .SelectMany(ur => ur.Role.RolePermissions)
            .Select(rp => rp.Permission.Name)
            .Distinct()
            .ToListAsync(ct);

        var (accessToken, expiresAt) = tokens.CreateAccessToken(user, tenantSlug, permissions);
        var (raw, hash) = tokens.CreateRefreshToken();
        db.RefreshTokens.Add(new RefreshToken
        {
            TenantId = user.TenantId,
            UserId = user.Id,
            TokenHash = hash,
            ExpiresAt = tokens.RefreshTokenExpiry
        });
        await db.SaveChangesAsync(ct);
        return new AuthResponse(accessToken, expiresAt, raw);
    }
}
