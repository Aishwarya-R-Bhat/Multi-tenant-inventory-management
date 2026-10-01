using InventoryPlatform.Domain.Common;

namespace InventoryPlatform.Domain.Identity;

public class Role : Entity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = default!;
    public List<RolePermission> RolePermissions { get; set; } = [];

    public const string Admin = "Admin";
}
