namespace InventoryPlatform.Domain.Identity;

/// <summary>Permission names, in the form Area.Action. Seeded into identity.Permissions.</summary>
public static class Permissions
{
    public const string UsersRead = "Users.Read";
    public const string UsersManage = "Users.Manage";
    public const string RolesManage = "Roles.Manage";

    public static readonly IReadOnlyList<string> All = [UsersRead, UsersManage, RolesManage];
}
