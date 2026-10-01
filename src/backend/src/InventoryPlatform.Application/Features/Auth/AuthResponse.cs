namespace InventoryPlatform.Application.Features.Auth;

public record AuthResponse(string AccessToken, DateTime AccessTokenExpiresAt, string RefreshToken);
