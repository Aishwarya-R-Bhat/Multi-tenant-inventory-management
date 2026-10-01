using FluentValidation;
using InventoryPlatform.Application.Abstractions;
using InventoryPlatform.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryPlatform.Application.Features.Auth;

public record LoginCommand(string TenantSlug, string Email, string Password) : IRequest<AuthResponse>;

public class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator()
    {
        RuleFor(x => x.TenantSlug).NotEmpty();
        RuleFor(x => x.Email).NotEmpty();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public class LoginHandler(IAppDbContext db, IPasswordService passwords, SessionService sessions)
    : IRequestHandler<LoginCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(LoginCommand request, CancellationToken ct)
    {
        const string failure = "Invalid company code, email or password.";

        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Slug == request.TenantSlug && t.IsActive, ct)
            ?? throw new UnauthorizedException(failure);

        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.TenantId == tenant.Id && u.Email == email && u.IsActive, ct);

        if (user is null || !passwords.Verify(user.PasswordHash, request.Password))
            throw new UnauthorizedException(failure);

        return await sessions.IssueAsync(user, tenant.Slug, ct);
    }
}
