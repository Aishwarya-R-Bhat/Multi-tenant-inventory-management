namespace InventoryPlatform.Application.Abstractions;

public interface ICurrentTenant
{
    /// <summary>Tenant of the caller, or Guid.Empty when unauthenticated (queries then match nothing).</summary>
    Guid TenantId { get; }
}
