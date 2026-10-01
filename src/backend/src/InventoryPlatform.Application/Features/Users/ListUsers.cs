using InventoryPlatform.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryPlatform.Application.Features.Users;

public record UserDto(Guid Id, string Email, string FullName, bool IsActive);

public record ListUsersQuery : IRequest<IReadOnlyList<UserDto>>;

/// <summary>No explicit tenant condition: the global query filter on User limits the result to the caller's tenant.</summary>
public class ListUsersHandler(IAppDbContext db) : IRequestHandler<ListUsersQuery, IReadOnlyList<UserDto>>
{
    public async Task<IReadOnlyList<UserDto>> Handle(ListUsersQuery request, CancellationToken ct) =>
        await db.Users.OrderBy(u => u.Email)
            .Select(u => new UserDto(u.Id, u.Email, u.FullName, u.IsActive))
            .ToListAsync(ct);
}
