using InventoryPlatform.Application.Features.Auth;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryPlatform.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1/auth")]
public class AuthController(ISender sender) : ControllerBase
{
    /// <summary>Creates a new company (tenant) with its first admin user and signs them in.</summary>
    [HttpPost("register-tenant")]
    public async Task<ActionResult<AuthResponse>> RegisterTenant(RegisterTenantCommand command, CancellationToken ct) =>
        Ok(await sender.Send(command, ct));

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginCommand command, CancellationToken ct) =>
        Ok(await sender.Send(command, ct));

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh(RefreshAccessTokenCommand command, CancellationToken ct) =>
        Ok(await sender.Send(command, ct));
}
