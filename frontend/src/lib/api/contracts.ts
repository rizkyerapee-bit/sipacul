export type OrganizationRole =
  | 1
  | 2
  | 3
  | 4
  | "Owner"
  | "Admin"
  | "Finance"
  | "Operator";

export type AntiforgeryTokenResponse = {
  requestToken: string;
  headerName: string;
};

export type CurrentUserMembership = {
  membershipId: string;
  organizationId: string;
  role: OrganizationRole;
  permissions: string[];
};

export type CurrentUser = {
  userId: string;
  email: string;
  emailConfirmed: boolean;
  lastLoginAt: string | null;
  memberships: CurrentUserMembership[];
};

export type LoginRequest = {
  email: string;
  password: string;
  rememberMe: boolean;
};

export type BootstrapStatus = {
  isConfigured: boolean;
  isInitialized: boolean;
  canBootstrap: boolean;
};

export type BootstrapOwnerRequest = {
  organizationCode: string;
  organizationName: string;
  organizationLegalName: string | null;
  organizationTimeZone: string;
  email: string;
  password: string;
};

export type BootstrapOwnerResponse = {
  userId: string;
  email: string;
  organizationId: string;
  organizationCode: string;
  organizationName: string;
  membershipId: string;
  role: OrganizationRole;
  createdAt: string;
};

export type Organization = {
  id: string;
  code: string;
  name: string;
  legalName: string | null;
  timeZone: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string | null;
};

export type ApiProblem = {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  code?: string;
  errors?: string[];
  [key: string]: unknown;
};