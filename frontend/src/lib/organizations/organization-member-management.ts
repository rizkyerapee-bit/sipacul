import type {
  AssignableOrganizationRole,
  CreateOrganizationMemberRequest,
  OrganizationMember,
  OrganizationMembershipStatus,
  OrganizationRole,
  UpdateOrganizationMemberRoleRequest,
} from "@/lib/api/contracts";

export type OrganizationMemberRoleFilter =
  | "all"
  | "owner"
  | "admin"
  | "finance"
  | "operator";

export type OrganizationMemberStatusFilter = "all" | "active" | "suspended";

export type OrganizationMemberDraft = {
  email: string;
  initialPassword: string;
  confirmInitialPassword: string;
  role: AssignableOrganizationRole;
};

export const assignableOrganizationMemberRoles: ReadonlyArray<{
  value: AssignableOrganizationRole;
  label: string;
  description: string;
}> = [
  {
    value: 2,
    label: "Admin",
    description: "Mengelola anggota, master data, operasional, dan keuangan.",
  },
  {
    value: 3,
    label: "Finance",
    description: "Mengelola penjualan, pembayaran, biaya, dan bagi hasil.",
  },
  {
    value: 4,
    label: "Operator",
    description: "Mengelola kegiatan budidaya, panen, dan penjualan lapangan.",
  },
];

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

const statusLabels: Record<string, string> = {
  "1": "Aktif",
  "2": "Ditangguhkan",
  Active: "Aktif",
  Suspended: "Ditangguhkan",
};

function roleNumber(role: OrganizationRole): number {
  const normalized = String(role).toLocaleLowerCase("en-US");
  if (normalized === "1" || normalized === "owner") return 1;
  if (normalized === "2" || normalized === "admin") return 2;
  if (normalized === "3" || normalized === "finance") return 3;
  if (normalized === "4" || normalized === "operator") return 4;
  return Number.MAX_SAFE_INTEGER;
}

function statusNumber(status: OrganizationMembershipStatus): number {
  const normalized = String(status).toLocaleLowerCase("en-US");
  if (normalized === "1" || normalized === "active") return 1;
  if (normalized === "2" || normalized === "suspended") return 2;
  return Number.MAX_SAFE_INTEGER;
}

function roleFilterKey(role: OrganizationRole): Exclude<
  OrganizationMemberRoleFilter,
  "all"
> | null {
  const number = roleNumber(role);
  if (number === 1) return "owner";
  if (number === 2) return "admin";
  if (number === 3) return "finance";
  if (number === 4) return "operator";
  return null;
}

export function organizationMemberDraft(): OrganizationMemberDraft {
  return {
    email: "",
    initialPassword: "",
    confirmInitialPassword: "",
    role: 4,
  };
}

export function validateOrganizationMemberDraft(
  draft: OrganizationMemberDraft,
): string[] {
  const errors: string[] = [];
  const email = draft.email.trim();
  const password = draft.initialPassword;

  if (!email) {
    errors.push("Email anggota wajib diisi.");
  } else if (email.length > 256) {
    errors.push("Email anggota maksimal 256 karakter.");
  } else if (!/^[^\s@]+@[^\s@]+$/.test(email)) {
    errors.push("Format email anggota tidak valid.");
  }

  if (![2, 3, 4].includes(draft.role)) {
    errors.push("Peran anggota tidak didukung.");
  }

  if (password.length > 0) {
    if (password.length < 12) {
      errors.push("Password awal minimal 12 karakter.");
    }
    if (password.length > 1024) {
      errors.push("Password awal maksimal 1024 karakter.");
    }
    if (!/[A-Z]/.test(password)) {
      errors.push("Password awal harus memuat huruf besar.");
    }
    if (!/[a-z]/.test(password)) {
      errors.push("Password awal harus memuat huruf kecil.");
    }
    if (!/\d/.test(password)) {
      errors.push("Password awal harus memuat angka.");
    }
    if (!/[^A-Za-z0-9]/.test(password)) {
      errors.push("Password awal harus memuat simbol.");
    }
  }

  if (password !== draft.confirmInitialPassword) {
    errors.push("Konfirmasi password awal tidak cocok.");
  }

  return errors;
}

export function toCreateOrganizationMemberRequest(
  draft: OrganizationMemberDraft,
): CreateOrganizationMemberRequest {
  return {
    email: draft.email.trim().toLowerCase(),
    initialPassword: draft.initialPassword || null,
    role: draft.role,
  };
}

export function toUpdateOrganizationMemberRoleRequest(
  role: AssignableOrganizationRole,
): UpdateOrganizationMemberRoleRequest {
  return { role };
}

export function getOrganizationMemberRoleLabel(role: OrganizationRole): string {
  return roleLabels[String(role)] ?? "Anggota";
}

export function getOrganizationMemberStatusLabel(
  status: OrganizationMembershipStatus,
): string {
  return statusLabels[String(status)] ?? "Tidak diketahui";
}

export function getOrganizationMemberAccountLabel(
  member: OrganizationMember,
): string {
  if (!member.userIsActive) return "Akun pengguna nonaktif";
  if (!member.emailConfirmed) return "Email belum dikonfirmasi";
  return "Akun pengguna aktif";
}

export function isOrganizationOwner(member: OrganizationMember): boolean {
  return roleNumber(member.role) === 1;
}

export function isOrganizationMembershipActive(
  status: OrganizationMembershipStatus,
): boolean {
  return statusNumber(status) === 1;
}

export function filterOrganizationMembers(
  members: OrganizationMember[],
  query: string,
  role: OrganizationMemberRoleFilter,
  status: OrganizationMemberStatusFilter,
): OrganizationMember[] {
  const normalizedQuery = query.trim().toLocaleLowerCase("id-ID");

  return members
    .filter((member) => role === "all" || roleFilterKey(member.role) === role)
    .filter((member) => {
      if (status === "all") return true;
      return isOrganizationMembershipActive(member.status) === (status === "active");
    })
    .filter((member) => {
      if (!normalizedQuery) return true;

      const searchable = [
        member.email,
        getOrganizationMemberRoleLabel(member.role),
        getOrganizationMemberStatusLabel(member.status),
        getOrganizationMemberAccountLabel(member),
      ].join(" ").toLocaleLowerCase("id-ID");

      return searchable.includes(normalizedQuery);
    })
    .sort((left, right) =>
      roleNumber(left.role) - roleNumber(right.role) ||
      left.email.localeCompare(right.email, "id-ID") ||
      left.membershipId.localeCompare(right.membershipId),
    );
}
