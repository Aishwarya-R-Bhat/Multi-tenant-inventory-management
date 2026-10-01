namespace InventoryPlatform.Domain.Identity;

/// <summary>Global (not per tenant) catalogue of permissions. Tenants assign them to their own roles.</summary>
public class Permission
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
}
