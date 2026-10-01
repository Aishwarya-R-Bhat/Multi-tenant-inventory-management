import { fakeToken } from './fake-token';
import { decodeUser } from './jwt';

describe('decodeUser', () => {
  const base = { sub: 'u1', email: 'a@b.com', name: 'Asha', tenant_id: 't1', tenant_slug: 'acme' };

  it('reads identity claims', () => {
    const user = decodeUser(fakeToken(base));
    expect(user).toMatchObject({ id: 'u1', email: 'a@b.com', name: 'Asha', tenantId: 't1', tenantSlug: 'acme' });
  });

  it('treats a single permission claim as a list of one', () => {
    expect(decodeUser(fakeToken({ ...base, permission: 'Users.Read' }))?.permissions).toEqual(['Users.Read']);
  });

  it('reads several permission claims', () => {
    const permissions = decodeUser(fakeToken({ ...base, permission: ['Users.Read', 'Roles.Manage'] }))?.permissions;
    expect(permissions).toEqual(['Users.Read', 'Roles.Manage']);
  });

  it('returns null for a malformed token', () => {
    expect(decodeUser('not-a-token')).toBeNull();
  });
});
