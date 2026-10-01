using System.Linq.Expressions;
using System.Reflection;
using InventoryPlatform.Application.Abstractions;
using InventoryPlatform.Domain.Common;
using InventoryPlatform.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace InventoryPlatform.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options, ICurrentTenant currentTenant)
    : DbContext(options), IAppDbContext
{
    public const string IdentitySchema = "identity";

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    // Read by the global query filters. EF re-evaluates it per context instance, so each request sees its own tenant.
    private Guid CurrentTenantId => currentTenant.TenantId;

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.HasDefaultSchema(IdentitySchema);

        b.Entity<Tenant>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.Slug).HasMaxLength(50);
            e.HasIndex(x => x.Slug).IsUnique();
        });

        b.Entity<User>(e =>
        {
            e.Property(x => x.Email).HasMaxLength(256);
            e.Property(x => x.FullName).HasMaxLength(200);
            e.HasIndex(x => new { x.TenantId, x.Email }).IsUnique();
        });

        b.Entity<Role>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(100);
            e.HasIndex(x => new { x.TenantId, x.Name }).IsUnique();
        });

        b.Entity<Permission>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(100);
            e.HasIndex(x => x.Name).IsUnique();
            e.HasData(SeedPermissions());
        });

        b.Entity<UserRole>(e =>
        {
            e.HasKey(x => new { x.UserId, x.RoleId });
            e.HasOne(x => x.User).WithMany(u => u.UserRoles).HasForeignKey(x => x.UserId);
            e.HasOne(x => x.Role).WithMany().HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<RolePermission>(e =>
        {
            e.HasKey(x => new { x.RoleId, x.PermissionId });
            e.HasOne(x => x.Role).WithMany(r => r.RolePermissions).HasForeignKey(x => x.RoleId);
            e.HasOne(x => x.Permission).WithMany().HasForeignKey(x => x.PermissionId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<RefreshToken>(e =>
        {
            e.Property(x => x.TokenHash).HasMaxLength(64);
            e.HasIndex(x => x.TokenHash).IsUnique();
            e.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        // Every tenant-owned entity gets the same filter and a foreign key to Tenants.
        foreach (var type in b.Model.GetEntityTypes().Where(t => typeof(ITenantEntity).IsAssignableFrom(t.ClrType)))
        {
            typeof(AppDbContext)
                .GetMethod(nameof(ConfigureTenantEntity), BindingFlags.NonPublic | BindingFlags.Instance)!
                .MakeGenericMethod(type.ClrType)
                .Invoke(this, [b]);
        }
    }

    private void ConfigureTenantEntity<T>(ModelBuilder b) where T : class, ITenantEntity
    {
        Expression<Func<T, bool>> filter = e => e.TenantId == CurrentTenantId;
        b.Entity<T>().HasQueryFilter(filter);
        b.Entity<T>().HasOne<Tenant>().WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
    }

    private static Permission[] SeedPermissions() =>
        Domain.Identity.Permissions.All.Select((name, i) => new Permission { Id = i + 1, Name = name }).ToArray();

    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var tenantId = currentTenant.TenantId;

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is Entity entity)
            {
                if (entry.State == EntityState.Added) entity.CreatedAt = now;
                else if (entry.State == EntityState.Modified) entity.UpdatedAt = now;
            }

            if (entry.Entity is ITenantEntity owned && tenantId != Guid.Empty)
            {
                // Signed-in callers can only write rows for their own tenant.
                if (entry.State == EntityState.Added && owned.TenantId == Guid.Empty) owned.TenantId = tenantId;
                if (entry.State is EntityState.Added or EntityState.Modified && owned.TenantId != tenantId)
                    throw new InvalidOperationException("Cross-tenant write blocked.");
            }
        }
        return base.SaveChangesAsync(ct);
    }
}
