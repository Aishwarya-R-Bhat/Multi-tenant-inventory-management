using InventoryPlatform.Api.Infrastructure;
using InventoryPlatform.Application.Features.Users;
using InventoryPlatform.Domain.Identity;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InventoryPlatform.Api.Controllers;

[ApiController]
[Route("api/v1/users")]
public class UsersController(ISender sender) : ControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.UsersRead)]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> List(CancellationToken ct) =>
        Ok(await sender.Send(new ListUsersQuery(), ct));
}
