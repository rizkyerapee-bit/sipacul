# SiPacul Authentication, Authorization, and SaaS Security Design

Version: 1.0
Date: 2026-08-02
Status: Approved implementation baseline

## 1. Purpose

This document defines the security baseline for SiPacul as a
self-hosted SaaS agribusiness application.

The design must ensure that:

- every human action is associated with an authenticated account;
- a user can belong to one or more organizations;
- roles are assigned per organization, never globally;
- an organization identifier in a URL is always authorized against
  the current user membership;
- data remains organization scoped at both authorization and query
  levels;
- password storage and sign-in security use framework components
  rather than custom cryptography;
- browser credentials are not stored in localStorage or
  sessionStorage;
- audit fields contain the stable user identifier;
- the application can later support subscriptions, invitations,
  mobile clients, and external integrations without replacing the
  core membership model.

## 2. Security boundary

SiPacul has two separate concepts:

1. Global identity
   - one account represents one human;
   - email and password belong to the global account;
   - the same account can join several organizations.

2. Organization membership
   - one row connects a user to one organization;
   - role and membership status are organization specific;
   - authorization is evaluated for the organization selected by the
     route.

A user account is not a tenant. An Organization is the tenant.

## 3. Authentication decision

### 3.1 Framework identity

SiPacul will use ASP.NET Core Identity for account storage, password
hashing, password verification, security stamps, lockout, email
confirmation, password reset, and recovery token generation.

SiPacul will not implement a custom password hashing algorithm or a
custom token format.

### 3.2 Browser authentication

The primary web application will use an encrypted authentication
cookie with:

- HttpOnly enabled;
- Secure enabled outside local development;
- SameSite configured for the deployed frontend topology;
- a short idle lifetime with sliding renewal;
- antiforgery validation on state-changing browser requests.

Authentication credentials must not be stored in localStorage or
sessionStorage.

### 3.3 Non-browser clients

Bearer tokens are deferred until a mobile application or external API
integration exists. The browser MVP does not require a custom JWT and
refresh-token subsystem.

## 4. Organization membership

The first implementation uses:

- OrganizationRole.Owner
- OrganizationRole.Admin
- OrganizationRole.Finance
- OrganizationRole.Operator

Membership status uses:

- Active
- Suspended

An active membership is required before an authenticated user can
access an organization route.

The membership aggregate stores:

- OrganizationId
- UserId
- Role
- Status
- JoinedAt
- SuspendedAt
- standard audit and soft-delete fields

A unique database constraint will later enforce one membership per
OrganizationId and UserId.

## 5. Role responsibilities

### Owner

The Owner has every organization permission. Only an Owner can assign
the Owner role. Future ownership transfer and subscription controls
also belong to this role.

### Admin

The Admin manages organization settings, members, master data,
operations, sales, finance, and settlements. An Admin cannot assign
the Owner role.

### Finance

The Finance role can read operational context and manage sales,
payments, expenses, capital, profitability, and profit-sharing
settlements. It cannot manage organization membership or edit land
and cultivation operations.

### Operator

The Operator manages operational master data usage, cultivation,
harvest, and sales entry. It cannot access finance administration,
capital, profitability, profit sharing, membership management, or
audit reporting.

## 6. Permission model

Permissions use stable lowercase identifiers:

- organizations.read
- organizations.manage
- members.read
- members.manage
- members.assign-owner
- master-data.read
- master-data.write
- lands.read
- lands.write
- cultivation.read
- cultivation.write
- harvest.read
- harvest.write
- sales.read
- sales.write
- finance.read
- finance.write
- profit-sharing.read
- profit-sharing.write
- profit-sharing.finalize
- profit-sharing.void
- audit.read

Endpoint authorization will use policies generated from these
permission identifiers.

Roles are only permission bundles. Application code checks a
permission, not a role name, except for owner-safety rules.

## 7. Authorization flow

For a protected organization route:

1. Authentication establishes the current UserId.
2. The route supplies OrganizationId.
3. An authorization handler loads the active membership for that
   UserId and OrganizationId.
4. The handler resolves the membership role to permissions.
5. The requested permission must be granted.
6. The application repository still applies OrganizationId scoping.

Authorization and query scoping are both required. Neither one
replaces the other.

A fallback authorization policy will require authentication for all
endpoints except explicitly anonymous authentication and health
endpoints.

## 8. Owner safety rules

Application services must enforce:

- an organization always has at least one active Owner;
- the final active Owner cannot be suspended;
- the final active Owner cannot be removed;
- the final active Owner cannot be demoted;
- only an Owner can assign the Owner role;
- a user cannot elevate their own role without the required
  permission.

These rules require a transaction because they depend on the current
number of active owners.

## 9. Audit identity

CreatedBy, UpdatedBy, and DeletedBy will store the stable user Guid as
a string.

They must not store email because email can change.

A scoped current-user service will expose:

- UserId
- Email
- IsAuthenticated

A SaveChanges interceptor or DbContext audit hook will populate audit
fields consistently.

System and migration actions may use a documented system identifier.

## 10. HTTP behavior

- unauthenticated request: 401 Unauthorized;
- authenticated but unauthorized request: 403 Forbidden;
- valid membership but organization-scoped record absent: 404 Not
  Found;
- duplicate account or membership: 409 Conflict;
- invalid credentials: generic 401 response without revealing whether
  an email exists;
- suspended account or membership: generic forbidden response;
- validation failure: 400 Bad Request.

## 11. Initial account and bootstrap

Production must not contain a hard-coded default password.

The first Owner will be created using a one-time bootstrap command or
deployment secret. The bootstrap process must:

- fail when an Owner already exists;
- require an explicit email and strong password;
- avoid printing the password;
- mark the account and membership through an auditable operation;
- be disabled after successful initialization.

## 12. Delivery sequence

### Sprint 19A-1A

- security architecture document;
- OrganizationRole;
- OrganizationMembershipStatus;
- OrganizationMembership aggregate;
- permission constants;
- role-permission catalog;
- domain and application tests.

### Sprint 19A-1B

- ASP.NET Core Identity account type;
- Identity-enabled DbContext;
- OrganizationMembership EF configuration;
- uniqueness and foreign keys;
- migration and PostgreSQL verification.

### Sprint 19A-2

- cookie authentication;
- account registration/bootstrap;
- login, logout, current-user, lockout, and password hashing;
- antiforgery baseline.

### Sprint 19A-3

- organization membership repository and service;
- owner-safety transaction;
- permission policy provider and authorization handler;
- organization route protection.

### Sprint 19A-4

- current-user abstraction;
- automatic audit identity;
- endpoint permission mapping;
- anonymous endpoint allow-list.

### Sprint 19A-5

- member invitation and role-management API;
- email confirmation and password reset;
- security event logging.

### Sprint 19A-6

- authentication and authorization E2E;
- cross-organization denial;
- suspended-membership denial;
- owner-safety verification;
- cookie and antiforgery verification;
- PostgreSQL cleanup.

## 13. Deferred items

The following are intentionally deferred:

- social login;
- SSO and external identity providers;
- mobile bearer tokens;
- external OAuth or OpenID Connect server;
- platform-level super administrator;
- subscription billing permissions;
- per-field permissions;
- organization-specific custom roles;
- passkeys and hardware security keys.

The permission identifiers and membership model are designed so these
features can be added without replacing the organization security
boundary.

## 14. Reference baseline

The design follows:

- Microsoft ASP.NET Core Identity guidance for securing web API
  backends;
- Microsoft ASP.NET Core policy-based authorization guidance;
- Microsoft guidance that ASP.NET Core does not provide a complete
  built-in multi-tenant authentication model;
- OWASP Password Storage guidance;
- OWASP Session Management guidance, including avoiding credential
  storage in browser Web Storage.
