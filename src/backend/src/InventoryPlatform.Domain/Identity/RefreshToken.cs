using InventoryPlatform.Domain.Common;

namespace InventoryPlatform.Domain.Identity;

public class RefreshToken : Entity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }

    /// <summary>SHA-256 of the token. The raw token is only ever shown to the client.</summary>
    public string TokenHash { get; set; } = default!;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public bool IsActive(DateTime now) => RevokedAt is null && ExpiresAt > now;
}
