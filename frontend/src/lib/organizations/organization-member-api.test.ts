import { beforeEach, describe, expect, it, vi } from "vitest";
import {
  changeOrganizationMemberRole,
  createOrganizationMember,
  getOrganizationMember,
  getOrganizationMembers,
  setOrganizationMembershipActive,
} from "@/lib/api/client";

const fetchMock = vi.fn<typeof fetch>();

function jsonResponse(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { "content-type": "application/json" },
  });
}

const member = {
  membershipId: "membership 1",
  userId: "user-1",
  email: "operator@example.com",
  emailConfirmed: true,
  userIsActive: true,
  role: 4,
  status: 1,
  joinedAt: "2026-09-04T00:00:00Z",
  suspendedAt: null,
};

describe("organization member API client", () => {
  beforeEach(() => {
    fetchMock.mockReset();
    vi.stubGlobal("fetch", fetchMock);
  });

  it("reads member collections and details from encoded organization routes", async () => {
    fetchMock
      .mockResolvedValueOnce(jsonResponse([member]))
      .mockResolvedValueOnce(jsonResponse(member));

    await getOrganizationMembers("org 1");
    await getOrganizationMember("org 1", "membership/1");

    expect(fetchMock.mock.calls.map((call) => call[0])).toEqual([
      "/api/v1/organizations/org%201/members",
      "/api/v1/organizations/org%201/members/membership%2F1",
    ]);
    expect(fetchMock.mock.calls.map((call) =>
      (call[1] as RequestInit).method,
    )).toEqual([undefined, undefined]);
  });

  it("writes the supported non-Owner lifecycle through CSRF-protected routes", async () => {
    for (let index = 0; index < 4; index += 1) {
      fetchMock
        .mockResolvedValueOnce(jsonResponse({
          requestToken: `member-csrf-${index}`,
          headerName: "X-CSRF",
        }))
        .mockResolvedValueOnce(jsonResponse(member, index === 0 ? 201 : 200));
    }

    await createOrganizationMember("org 1", {
      email: "operator@example.com",
      initialPassword: "StrongPass12!",
      role: 4,
    });
    await changeOrganizationMemberRole("org 1", "membership 1", { role: 3 });
    await setOrganizationMembershipActive("org 1", "membership 1", false);
    await setOrganizationMembershipActive("org 1", "membership 1", true);

    const memberPath = "/api/v1/organizations/org%201/members/membership%201";
    expect([1, 3, 5, 7].map((index) => fetchMock.mock.calls[index][0]))
      .toEqual([
        "/api/v1/organizations/org%201/members",
        `${memberPath}/role`,
        `${memberPath}/suspend`,
        `${memberPath}/activate`,
      ]);
    expect([1, 3, 5, 7].map((index) =>
      (fetchMock.mock.calls[index][1] as RequestInit).method,
    )).toEqual(["POST", "PATCH", "PATCH", "PATCH"]);
    expect((fetchMock.mock.calls[1][1] as RequestInit).body).toBe(JSON.stringify({
      email: "operator@example.com",
      initialPassword: "StrongPass12!",
      role: 4,
    }));
    expect((fetchMock.mock.calls[3][1] as RequestInit).body)
      .toBe(JSON.stringify({ role: 3 }));
    expect(new Headers(
      (fetchMock.mock.calls[7][1] as RequestInit).headers,
    ).get("X-CSRF")).toBe("member-csrf-3");
  });
});
