import { describe, expect, it } from "vitest";
import type {
  AssignableOrganizationRole,
  OrganizationMember,
} from "@/lib/api/contracts";
import {
  filterOrganizationMembers,
  getOrganizationMemberAccountLabel,
  getOrganizationMemberRoleLabel,
  getOrganizationMemberStatusLabel,
  isOrganizationMembershipActive,
  isOrganizationOwner,
  organizationMemberDraft,
  toCreateOrganizationMemberRequest,
  toUpdateOrganizationMemberRoleRequest,
  validateOrganizationMemberDraft,
  type OrganizationMemberDraft,
} from "@/lib/organizations/organization-member-management";

function member(overrides: Partial<OrganizationMember> = {}): OrganizationMember {
  return {
    membershipId: "membership-1",
    userId: "user-1",
    email: "operator@example.com",
    emailConfirmed: true,
    userIsActive: true,
    role: 4,
    status: 1,
    joinedAt: "2026-09-04T00:00:00Z",
    suspendedAt: null,
    ...overrides,
  };
}

const validDraft: OrganizationMemberDraft = {
  email: " Admin@Example.com ",
  initialPassword: "StrongPass12!",
  confirmInitialPassword: "StrongPass12!",
  role: 2,
};

describe("organization member management helpers", () => {
  it("creates a safe Operator draft without persisting a password", () => {
    expect(organizationMemberDraft()).toEqual({
      email: "",
      initialPassword: "",
      confirmInitialPassword: "",
      role: 4,
    });
  });

  it("accepts an existing account without an initial password", () => {
    expect(validateOrganizationMemberDraft({
      ...validDraft,
      initialPassword: "",
      confirmInitialPassword: "",
    })).toEqual([]);
  });

  it("validates email, assignable roles, and confirmation", () => {
    expect(validateOrganizationMemberDraft({
      email: "email-tidak-valid",
      initialPassword: "StrongPass12!",
      confirmInitialPassword: "berbeda",
      role: 1 as unknown as AssignableOrganizationRole,
    })).toEqual(expect.arrayContaining([
      "Format email anggota tidak valid.",
      "Peran anggota tidak didukung.",
      "Konfirmasi password awal tidak cocok.",
    ]));
  });

  it("mirrors the current backend password policy for a new account", () => {
    expect(validateOrganizationMemberDraft({
      ...validDraft,
      initialPassword: "short",
      confirmInitialPassword: "short",
    })).toEqual(expect.arrayContaining([
      "Password awal minimal 12 karakter.",
      "Password awal harus memuat huruf besar.",
      "Password awal harus memuat angka.",
      "Password awal harus memuat simbol.",
    ]));
  });

  it("normalizes create and change-role request payloads", () => {
    expect(toCreateOrganizationMemberRequest(validDraft)).toEqual({
      email: "admin@example.com",
      initialPassword: "StrongPass12!",
      role: 2,
    });
    expect(toCreateOrganizationMemberRequest({
      ...validDraft,
      initialPassword: "",
      confirmInitialPassword: "",
    }).initialPassword).toBeNull();
    expect(toUpdateOrganizationMemberRoleRequest(3)).toEqual({ role: 3 });
  });

  it("maps numeric and string role/status contracts", () => {
    expect(getOrganizationMemberRoleLabel(1)).toBe("Owner");
    expect(getOrganizationMemberRoleLabel("Finance")).toBe("Finance");
    expect(getOrganizationMemberStatusLabel(1)).toBe("Aktif");
    expect(getOrganizationMemberStatusLabel("Suspended")).toBe("Ditangguhkan");
    expect(isOrganizationMembershipActive("Active")).toBe(true);
    expect(isOrganizationMembershipActive(2)).toBe(false);
  });

  it("keeps Owner protected and presents account-level attention", () => {
    expect(isOrganizationOwner(member({ role: "Owner" }))).toBe(true);
    expect(isOrganizationOwner(member({ role: 2 }))).toBe(false);
    expect(getOrganizationMemberAccountLabel(member())).toBe("Akun pengguna aktif");
    expect(getOrganizationMemberAccountLabel(member({ emailConfirmed: false })))
      .toBe("Email belum dikonfirmasi");
    expect(getOrganizationMemberAccountLabel(member({ userIsActive: false })))
      .toBe("Akun pengguna nonaktif");
  });

  it("filters and sorts members without mutating backend order", () => {
    const source = [
      member({
        membershipId: "membership-operator",
        email: "zeta@example.com",
        role: "Operator",
      }),
      member({
        membershipId: "membership-finance",
        email: "finance@example.com",
        role: 3,
        status: "Suspended",
        suspendedAt: "2026-09-04T01:00:00Z",
      }),
      member({
        membershipId: "membership-owner",
        email: "owner@example.com",
        role: 1,
      }),
    ];

    expect(filterOrganizationMembers(source, "finance", "all", "suspended")
      .map((item) => item.membershipId)).toEqual(["membership-finance"]);
    expect(filterOrganizationMembers(source, "", "all", "all")
      .map((item) => item.membershipId)).toEqual([
        "membership-owner",
        "membership-finance",
        "membership-operator",
      ]);
    expect(source.map((item) => item.membershipId)).toEqual([
      "membership-operator",
      "membership-finance",
      "membership-owner",
    ]);
  });
});
