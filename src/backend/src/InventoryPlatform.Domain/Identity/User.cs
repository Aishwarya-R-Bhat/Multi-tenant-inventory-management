using InventoryPlatform.Domain.Common;

namespace InventoryPlatform.Domain.Identity;

public class User : Entity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Email { get; set; } = default!;
    public string PasswordHash { get; set; } = default!;
    public string FullName { get; set; } = default!;
    public bool IsActive { get; set; } = true;
    public List<UserRole> UserRoles { get; set; } = [];
}
