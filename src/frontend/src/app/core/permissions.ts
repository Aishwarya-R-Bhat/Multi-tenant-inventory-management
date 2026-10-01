/** Must match InventoryPlatform.Domain.Identity.Permissions on the backend. */
export const Permissions = {
  UsersRead: 'Users.Read',
  UsersManage: 'Users.Manage',
  RolesManage: 'Roles.Manage',
} as const;
