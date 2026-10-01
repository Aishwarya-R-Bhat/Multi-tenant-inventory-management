export interface AuthResponse {
  accessToken: string;
  accessTokenExpiresAt: string;
  refreshToken: string;
}

export interface LoginRequest {
  tenantSlug: string;
  email: string;
  password: string;
}

export interface RegisterTenantRequest {
  companyName: string;
  slug: string;
  adminEmail: string;
  adminFullName: string;
  password: string;
}

export interface CurrentUser {
  id: string;
  email: string;
  name: string;
  tenantId: string;
  tenantSlug: string;
  permissions: string[];
}
