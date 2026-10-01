/** Builds an unsigned JWT for tests. */
export function fakeToken(claims: Record<string, unknown>): string {
  const encode = (o: unknown) => btoa(JSON.stringify(o)).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
  return `${encode({ alg: 'none' })}.${encode(claims)}.sig`;
}
