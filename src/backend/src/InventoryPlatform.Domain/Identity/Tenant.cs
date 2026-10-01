using InventoryPlatform.Domain.Common;

namespace InventoryPlatform.Domain.Identity;

public class Tenant : Entity
{
    public string Name { get; set; } = default!;

    /// <summary>Short unique code the user types at login to say which company they belong to.</summary>
    public string Slug { get; set; } = default!;
    public bool IsActive { get; set; } = true;
}
