import type {
  CurrentUserMembership,
  OrganizationRole,
} from "@/lib/api/contracts";

const STORAGE_KEY = "sipacul.selectedOrganizationId";

const roleLabels: Record<string, string> = {
  "1": "Owner",
  "2": "Admin",
  "3": "Finance",
  "4": "Operator",
  Owner: "Owner",
  Admin: "Admin",
  Finance: "Finance",
  Operator: "Operator",
};

export function getRoleLabel(role: OrganizationRole): string {
  return roleLabels[String(role)] ?? "Anggota";
}

export function hasPermission(
  membership: CurrentUserMembership,
  permission: string,
): boolean {
  return membership.permissions.includes(permission);
}

export function setSelectedOrganizationId(
  organizationId: string | null,
): void {
  if (organizationId) {
    localStorage.setItem(STORAGE_KEY, organizationId);
    return;
  }

  localStorage.removeItem(STORAGE_KEY);
}

export function resolveSelectedMembership(
  memberships: CurrentUserMembership[],
): CurrentUserMembership | null {
  if (memberships.length === 0) {
    setSelectedOrganizationId(null);
    return null;
  }

  const storedId = localStorage.getItem(STORAGE_KEY);
  const storedMembership = memberships.find(
    (membership) => membership.organizationId === storedId,
  );

  if (storedMembership) {
    return storedMembership;
  }

  const fallback = memberships[0];
  setSelectedOrganizationId(fallback.organizationId);
  return fallback;
}