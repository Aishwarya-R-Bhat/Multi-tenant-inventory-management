using InventoryPlatform.Application.Abstractions;
using InventoryPlatform.Infrastructure.Security;

namespace InventoryPlatform.Api.Infrastructure;

/// <summary>Reads the tenant from the validated JWT. Anonymous callers get Guid.Empty, which matches no rows.</summary>
public class CurrentTenant(IHttpContextAccessor accessor) : ICurrentTenant
{
    public Guid TenantId =>
        Guid.TryParse(accessor.HttpContext?.User.FindFirst(JwtTokenService.TenantIdClaim)?.Value, out var id) ? id : Guid.Empty;
}
