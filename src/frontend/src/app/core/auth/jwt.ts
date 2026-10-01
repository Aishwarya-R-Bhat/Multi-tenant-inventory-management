import { CurrentUser } from './auth.models';

/** Reads the claims of a JWT. The signature is not checked here, the API does that on every request. */
export function decodeUser(token: string): CurrentUser | null {
  try {
    const payload = token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/');
    const json = decodeURIComponent(
      atob(payload)
        .split('')
        .map((c) => '%' + c.charCodeAt(0).toString(16).padStart(2, '0'))
        .join(''),
    );
    const claims = JSON.parse(json);
    const permission = claims['permission'];
    return {
      id: claims['sub'],
      email: claims['email'],
      name: claims['name'],
      tenantId: claims['tenant_id'],
      tenantSlug: claims['tenant_slug'],
      // A single permission arrives as a string, several as an array.
      permissions: permission === undefined ? [] : Array.isArray(permission) ? permission : [permission],
    };
  } catch {
    return null;
  }
}
