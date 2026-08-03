import { beforeEach, describe, expect, it, vi } from "vitest";
import {
  ApiError,
  bootstrapOwner,
  getBootstrapStatus,
  getCropCycleProfitability,
  getCropCycles,
  getCultivationActivities,
  getCurrentUser,
  getHarvestBatches,
  getLands,
  getOrganization,
  login,
  logout,
} from "@/lib/api/client";

const fetchMock = vi.fn<typeof fetch>();

function jsonResponse(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { "content-type": "application/json" },
  });
}

describe("SiPacul API client", () => {
  beforeEach(() => {
    fetchMock.mockReset();
    vi.stubGlobal("fetch", fetchMock);
  });

  it("reads bootstrap status without credentials in source code", async () => {
    fetchMock.mockResolvedValueOnce(jsonResponse({ isConfigured: true, isInitialized: false, canBootstrap: true }));

    await expect(getBootstrapStatus()).resolves.toMatchObject({ canBootstrap: true });
    expect(fetchMock).toHaveBeenCalledWith("/api/v1/bootstrap/status", expect.objectContaining({ credentials: "include", cache: "no-store" }));
  });

  it("gets the current authenticated user", async () => {
    fetchMock.mockResolvedValueOnce(jsonResponse({ userId: "user-1", email: "owner@example.com", memberships: [] }));

    await expect(getCurrentUser()).resolves.toMatchObject({ email: "owner@example.com" });
  });

  it("gets an organization by the membership organization identifier", async () => {
    fetchMock.mockResolvedValueOnce(jsonResponse({ id: "org 1", code: "ORG", name: "Farm" }));

    await getOrganization("org 1");
    expect(fetchMock.mock.calls[0][0]).toBe("/api/v1/organizations/org%201");
  });

  it("reads organization dashboard collections with encoded identifiers", async () => {
    fetchMock
      .mockResolvedValueOnce(jsonResponse([]))
      .mockResolvedValueOnce(jsonResponse([]));

    await getLands("org 1");
    await getCropCycles("org 1");

    expect(fetchMock.mock.calls[0][0]).toBe("/api/v1/organizations/org%201/lands");
    expect(fetchMock.mock.calls[1][0]).toBe("/api/v1/organizations/org%201/crop-cycles");
  });

  it("reads selected-cycle dashboard sources with encoded identifiers", async () => {
    fetchMock
      .mockResolvedValueOnce(jsonResponse([]))
      .mockResolvedValueOnce(jsonResponse([]))
      .mockResolvedValueOnce(jsonResponse({ cropCycleId: "cycle 1" }));

    await getCultivationActivities("org 1", "cycle 1");
    await getHarvestBatches("org 1", "cycle 1");
    await getCropCycleProfitability("org 1", "cycle 1");

    const basePath = "/api/v1/organizations/org%201/crop-cycles/cycle%201";
    expect(fetchMock.mock.calls[0][0]).toBe(`${basePath}/activities`);
    expect(fetchMock.mock.calls[1][0]).toBe(`${basePath}/harvest-batches`);
    expect(fetchMock.mock.calls[2][0]).toBe(`${basePath}/profitability`);
  });

  it("keeps dashboard reads cookie-based and uncached", async () => {
    fetchMock.mockResolvedValueOnce(jsonResponse([]));

    await getLands("org-1");

    expect(fetchMock.mock.calls[0][1]).toEqual(expect.objectContaining({
      credentials: "include",
      cache: "no-store",
    }));
  });

  it("obtains a CSRF token before login", async () => {
    fetchMock
      .mockResolvedValueOnce(jsonResponse({ requestToken: "csrf-value", headerName: "X-CSRF-TOKEN" }))
      .mockResolvedValueOnce(jsonResponse({ userId: "user-1", email: "owner@example.com", memberships: [] }));

    await login({ email: "owner@example.com", password: "secret", rememberMe: true });

    expect(fetchMock).toHaveBeenCalledTimes(2);
    const loginInit = fetchMock.mock.calls[1][1] as RequestInit;
    const headers = new Headers(loginInit.headers);
    expect(headers.get("X-CSRF-TOKEN")).toBe("csrf-value");
    expect(loginInit.body).toBe(JSON.stringify({ email: "owner@example.com", password: "secret", rememberMe: true }));
  });

  it("sends bootstrap authorization and CSRF headers together", async () => {
    fetchMock
      .mockResolvedValueOnce(jsonResponse({ requestToken: "csrf-value", headerName: "X-CSRF-TOKEN" }))
      .mockResolvedValueOnce(jsonResponse({ userId: "user-1", organizationId: "org-1" }, 201));

    await bootstrapOwner("bootstrap-secret", {
      organizationCode: "FARM",
      organizationName: "Farm",
      organizationLegalName: null,
      organizationTimeZone: "Asia/Jakarta",
      email: "owner@example.com",
      password: "secret",
    });

    const requestInit = fetchMock.mock.calls[1][1] as RequestInit;
    const headers = new Headers(requestInit.headers);
    expect(headers.get("X-CSRF-TOKEN")).toBe("csrf-value");
    expect(Array.from(headers.values())).toContain("bootstrap-secret");
  });

  it("supports a no-content logout response", async () => {
    fetchMock
      .mockResolvedValueOnce(jsonResponse({ requestToken: "csrf-value", headerName: "X-CSRF-TOKEN" }))
      .mockResolvedValueOnce(new Response(null, { status: 204 }));

    await expect(logout()).resolves.toBeUndefined();
  });

  it("exposes RFC problem details through ApiError", async () => {
    fetchMock.mockResolvedValueOnce(jsonResponse({ title: "Authentication failed", detail: "Email or password is invalid.", code: "Authentication.InvalidCredentials" }, 401));

    const error = await getCurrentUser().catch((caught) => caught);
    expect(error).toBeInstanceOf(ApiError);
    expect(error).toMatchObject({ status: 401, message: "Email or password is invalid." });
    expect((error as ApiError).problem?.code).toBe("Authentication.InvalidCredentials");
  });

  it("creates a safe fallback for an empty authorization response", async () => {
    fetchMock.mockResolvedValueOnce(new Response(null, { status: 403 }));

    await expect(getCurrentUser()).rejects.toMatchObject({ status: 403, message: "Permintaan gagal dengan status 403." });
  });
});
