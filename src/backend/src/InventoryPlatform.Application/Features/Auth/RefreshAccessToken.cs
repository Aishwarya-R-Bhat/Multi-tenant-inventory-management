using FluentValidation;
using InventoryPlatform.Application.Abstractions;
using InventoryPlatform.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryPlatform.Application.Features.Auth;

public record RefreshAccessTokenCommand(string RefreshToken) : IRequest<AuthResponse>;

public class RefreshAccessTokenValidator : AbstractValidator<RefreshAccessTokenCommand>
{
    public RefreshAccessTokenValidator() => RuleFor(x => x.RefreshToken).NotEmpty();
}

public class RefreshAccessTokenHandler(IAppDbContext db, ITokenService tokens, SessionService sessions)
    : IRequestHandler<RefreshAccessTokenCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(RefreshAccessTokenCommand request, CancellationToken ct)
    {
        var hash = tokens.HashRefreshToken(request.RefreshToken);
        var stored = await db.RefreshTokens.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
        if (stored is null || !stored.IsActive(DateTime.UtcNow))
            throw new UnauthorizedException("Invalid refresh token.");

        var user = await db.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == stored.UserId && u.TenantId == stored.TenantId && u.IsActive, ct)
            ?? throw new UnauthorizedException("Invalid refresh token.");
        var tenant = await db.Tenants.FirstAsync(t => t.Id == user.TenantId, ct);

        // Rotation: each refresh token works once.
        stored.RevokedAt = DateTime.UtcNow;
        return await sessions.IssueAsync(user, tenant.Slug, ct);
    }
}
