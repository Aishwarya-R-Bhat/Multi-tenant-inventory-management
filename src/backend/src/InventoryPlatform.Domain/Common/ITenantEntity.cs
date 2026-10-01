namespace InventoryPlatform.Domain.Common;

/// <summary>Marks an entity as owned by a tenant. Every query on it is filtered by TenantId.</summary>
public interface ITenantEntity
{
    Guid TenantId { get; set; }
}
