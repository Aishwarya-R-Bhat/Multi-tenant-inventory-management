using InventoryPlatform.Domain.Common;

namespace InventoryPlatform.Domain.Identity;

public class UserRole : ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
    public User User { get; set; } = default!;
    public Role Role { get; set; } = default!;
}
