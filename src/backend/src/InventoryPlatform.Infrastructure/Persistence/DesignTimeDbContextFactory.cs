using InventoryPlatform.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace InventoryPlatform.Infrastructure.Persistence;

/// <summary>Used only by `dotnet ef` to create migrations. It never connects to a database.</summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=localhost,1433;Database=InventoryPlatform;TrustServerCertificate=True")
            .Options;
        return new AppDbContext(options, new NoTenant());
    }

    private class NoTenant : ICurrentTenant
    {
        public Guid TenantId => Guid.Empty;
    }
}
