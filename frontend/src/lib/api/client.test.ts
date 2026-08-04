import { beforeEach, describe, expect, it, vi } from "vitest";
import {
  addLandPlot,
  ApiError,
  bootstrapOwner,
  createLand,
  deleteLand,
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
  removeLandPlot,
  setLandActive,
  setLandPlotActive,
  updateLand,
  updateLandPlot,
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

  it("writes land records through CSRF-protected encoded routes", async () => {
    const landRequest = {
      code: "LHN-01",
      name: "Lahan Timur",
      tenureType: 1 as const,
      totalArea: 1,
      areaUnit: 2 as const,
      address: null,
      locationDescription: null,
      latitude: null,
      longitude: null,
      notes: null,
    };
    fetchMock
      .mockResolvedValueOnce(jsonResponse({ requestToken: "csrf-1", headerName: "X-CSRF" }))
      .mockResolvedValueOnce(jsonResponse({ id: "land-1" }, 201))
      .mockResolvedValueOnce(jsonResponse({ requestToken: "csrf-2", headerName: "X-CSRF" }))
      .mockResolvedValueOnce(jsonResponse({ id: "land-1" }))
      .mockResolvedValueOnce(jsonResponse({ requestToken: "csrf-3", headerName: "X-CSRF" }))
      .mockResolvedValueOnce(jsonResponse({ id: "land-1", isActive: false }));

    await createLand("org 1", landRequest);
    await updateLand("org 1", "land 1", {
      name: landRequest.name,
      tenureType: landRequest.tenureType,
      totalArea: landRequest.totalArea,
      areaUnit: landRequest.areaUnit,
      address: landRequest.address,
      locationDescription: landRequest.locationDescription,
      latitude: landRequest.latitude,
      longitude: landRequest.longitude,
      notes: landRequest.notes,
    });
    await setLandActive("org 1", "land 1", false);

    expect(fetchMock.mock.calls[1][0]).toBe("/api/v1/organizations/org%201/lands");
    expect((fetchMock.mock.calls[1][1] as RequestInit).method).toBe("POST");
    expect(fetchMock.mock.calls[3][0]).toBe("/api/v1/organizations/org%201/lands/land%201");
    expect((fetchMock.mock.calls[3][1] as RequestInit).method).toBe("PUT");
    expect(fetchMock.mock.calls[5][0]).toBe("/api/v1/organizations/org%201/lands/land%201/deactivate");
    expect((fetchMock.mock.calls[5][1] as RequestInit).method).toBe("PATCH");
    expect(new Headers((fetchMock.mock.calls[1][1] as RequestInit).headers).get("X-CSRF")).toBe("csrf-1");
  });

  it("writes plot records through CSRF-protected nested routes", async () => {
    const plotRequest = {
      code: "PTK-01",
      name: "Petak Utara",
      area: 2500,
      areaUnit: 1 as const,
      generalCondition: null,
      notes: null,
    };
    for (let index = 0; index < 4; index += 1) {
      fetchMock
        .mockResolvedValueOnce(jsonResponse({ requestToken: `csrf-${index}`, headerName: "X-CSRF" }))
        .mockResolvedValueOnce(jsonResponse({ id: "land-1", plots: [] }));
    }

    await addLandPlot("org 1", "land 1", plotRequest);
    await updateLandPlot("org 1", "land 1", "plot 1", {
      name: plotRequest.name,
      area: plotRequest.area,
      areaUnit: plotRequest.areaUnit,
      generalCondition: plotRequest.generalCondition,
      notes: plotRequest.notes,
    });
    await setLandPlotActive("org 1", "land 1", "plot 1", false);
    await removeLandPlot("org 1", "land 1", "plot 1");

    const resource = "/api/v1/organizations/org%201/lands/land%201/plots/plot%201";
    expect(fetchMock.mock.calls[1][0]).toBe("/api/v1/organizations/org%201/lands/land%201/plots");
    expect(fetchMock.mock.calls[3][0]).toBe(resource);
    expect(fetchMock.mock.calls[5][0]).toBe(`${resource}/deactivate`);
    expect(fetchMock.mock.calls[7][0]).toBe(resource);
    expect([1, 3, 5, 7].map((index) => (fetchMock.mock.calls[index][1] as RequestInit).method))
      .toEqual(["POST", "PUT", "PATCH", "DELETE"]);
  });

  it("deletes an unused land through a CSRF-protected encoded route", async () => {
    fetchMock
      .mockResolvedValueOnce(jsonResponse({ requestToken: "csrf-delete", headerName: "X-CSRF" }))
      .mockResolvedValueOnce(new Response(null, { status: 204 }));

    await expect(deleteLand("org 1", "land 1")).resolves.toBeUndefined();

    expect(fetchMock.mock.calls[1][0]).toBe("/api/v1/organizations/org%201/lands/land%201");
    expect((fetchMock.mock.calls[1][1] as RequestInit).method).toBe("DELETE");
    expect(new Headers((fetchMock.mock.calls[1][1] as RequestInit).headers).get("X-CSRF"))
      .toBe("csrf-delete");
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
