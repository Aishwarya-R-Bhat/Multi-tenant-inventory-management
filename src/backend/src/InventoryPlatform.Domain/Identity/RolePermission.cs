using InventoryPlatform.Domain.Common;

namespace InventoryPlatform.Domain.Identity;

public class RolePermission : ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid RoleId { get; set; }
    public int PermissionId { get; set; }
    public Role Role { get; set; } = default!;
    public Permission Permission { get; set; } = default!;
}
