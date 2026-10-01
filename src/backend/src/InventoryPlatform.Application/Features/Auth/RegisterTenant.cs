using FluentValidation;
using InventoryPlatform.Application.Abstractions;
using InventoryPlatform.Application.Common;
using InventoryPlatform.Domain.Identity;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryPlatform.Application.Features.Auth;

public record RegisterTenantCommand(string CompanyName, string Slug, string AdminEmail, string AdminFullName, string Password)
    : IRequest<AuthResponse>;

public class RegisterTenantValidator : AbstractValidator<RegisterTenantCommand>
{
    public RegisterTenantValidator()
    {
        RuleFor(x => x.CompanyName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(50).Matches("^[a-z0-9-]+$")
            .WithMessage("Slug may contain only lowercase letters, digits and hyphens.");
        RuleFor(x => x.AdminEmail).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.AdminFullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
    }
}

public class RegisterTenantHandler(IAppDbContext db, IPasswordService passwords, SessionService sessions)
    : IRequestHandler<RegisterTenantCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(RegisterTenantCommand request, CancellationToken ct)
    {
        if (await db.Tenants.AnyAsync(t => t.Slug == request.Slug, ct))
            throw new ConflictException($"The company code '{request.Slug}' is already taken.");

        var tenant = new Tenant { Name = request.CompanyName, Slug = request.Slug };
        db.Tenants.Add(tenant);

        // Every new tenant starts with an Admin role that holds all permissions.
        var allPermissions = await db.Permissions.ToListAsync(ct);
        var admin = new Role { TenantId = tenant.Id, Name = Role.Admin };
        admin.RolePermissions = allPermissions
            .Select(p => new RolePermission { TenantId = tenant.Id, Role = admin, PermissionId = p.Id }).ToList();
        db.Roles.Add(admin);

        var user = new User
        {
            TenantId = tenant.Id,
            Email = request.AdminEmail.Trim().ToLowerInvariant(),
            FullName = request.AdminFullName,
            PasswordHash = passwords.Hash(request.Password)
        };
        user.UserRoles.Add(new UserRole { TenantId = tenant.Id, User = user, Role = admin });
        db.Users.Add(user);

        await db.SaveChangesAsync(ct);
        return await sessions.IssueAsync(user, tenant.Slug, ct);
    }
}
