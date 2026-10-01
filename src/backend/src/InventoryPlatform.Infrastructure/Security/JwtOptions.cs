namespace InventoryPlatform.Infrastructure.Security;

public class JwtOptions
{
    public const string Section = "Jwt";

    public string Issuer { get; set; } = "inventory-platform";
    public string Audience { get; set; } = "inventory-platform";

    /// <summary>Signing key, at least 32 characters. Comes from user secrets / Key Vault, never from source control.</summary>
    public string Key { get; set; } = default!;
    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 7;
}
