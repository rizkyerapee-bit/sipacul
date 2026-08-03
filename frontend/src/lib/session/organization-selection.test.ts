import { describe, expect, it } from "vitest";
import type { CurrentUserMembership } from "@/lib/api/contracts";
import { getRoleLabel, hasPermission, resolveSelectedMembership, setSelectedOrganizationId } from "@/lib/session/organization-selection";

const memberships: CurrentUserMembership[] = [
  { membershipId: "member-1", organizationId: "org-1", role: 1, permissions: ["lands.read", "finance.read"] },
  { membershipId: "member-2", organizationId: "org-2", role: "Operator", permissions: ["cultivation.read"] },
];

describe("organization selection", () => {
  it("selects the first membership when no preference exists", () => {
    expect(resolveSelectedMembership(memberships)?.organizationId).toBe("org-1");
    expect(localStorage.getItem("sipacul.selectedOrganizationId")).toBe("org-1");
  });

  it("restores a stored membership", () => {
    setSelectedOrganizationId("org-2");
    expect(resolveSelectedMembership(memberships)?.membershipId).toBe("member-2");
  });

  it("replaces a stale preference with a valid membership", () => {
    setSelectedOrganizationId("org-missing");
    expect(resolveSelectedMembership(memberships)?.organizationId).toBe("org-1");
  });

  it("clears the preference when the user has no memberships", () => {
    setSelectedOrganizationId("org-1");
    expect(resolveSelectedMembership([])).toBeNull();
    expect(localStorage.getItem("sipacul.selectedOrganizationId")).toBeNull();
  });

  it("clears an explicit selection", () => {
    setSelectedOrganizationId("org-1");
    setSelectedOrganizationId(null);
    expect(localStorage.getItem("sipacul.selectedOrganizationId")).toBeNull();
  });

  it("maps numeric and string role contracts", () => {
    expect(getRoleLabel(1)).toBe("Owner");
    expect(getRoleLabel("Finance")).toBe("Finance");
  });

  it("checks exact organization permissions", () => {
    expect(hasPermission(memberships[0], "lands.read")).toBe(true);
    expect(hasPermission(memberships[0], "lands.write")).toBe(false);
  });
});