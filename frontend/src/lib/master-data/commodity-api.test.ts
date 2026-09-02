import { beforeEach, describe, expect, it, vi } from "vitest";
import {
  createCommodity,
  createCommodityCategory,
  getCommodities,
  getCommodityCategories,
  setCommodityActive,
  setCommodityCategoryActive,
  updateCommodity,
  updateCommodityCategory,
} from "@/lib/api/client";

const fetchMock = vi.fn<typeof fetch>();

function jsonResponse(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { "Content-Type": "application/json" },
  });
}

const csrfResponse = {
  requestToken: "csrf-value",
  headerName: "X-CSRF-TOKEN",
};

describe("master commodity API client", () => {
  beforeEach(() => {
    fetchMock.mockReset();
    vi.stubGlobal("fetch", fetchMock);
  });

  it("reads categories and commodities from encoded organization routes", async () => {
    fetchMock
      .mockResolvedValueOnce(jsonResponse([]))
      .mockResolvedValueOnce(jsonResponse([]));

    await getCommodityCategories("org 1");
    await getCommodities("org 1");

    expect(fetchMock.mock.calls[0][0]).toBe(
      "/api/v1/organizations/org%201/commodity-categories",
    );
    expect(fetchMock.mock.calls[1][0]).toBe(
      "/api/v1/organizations/org%201/commodities",
    );
  });

  it("creates and updates categories through CSRF-protected routes", async () => {
    const category = {
      id: "category-1",
      organizationId: "org 1",
      name: "Tanaman Pangan",
      description: null,
      isActive: true,
      createdAt: "2026-08-31T00:00:00Z",
      updatedAt: null,
    };

    fetchMock
      .mockResolvedValueOnce(jsonResponse(csrfResponse))
      .mockResolvedValueOnce(jsonResponse(category, 201))
      .mockResolvedValueOnce(jsonResponse(csrfResponse))
      .mockResolvedValueOnce(jsonResponse(category));

    await createCommodityCategory("org 1", {
      name: "Tanaman Pangan",
      description: null,
    });
    await updateCommodityCategory("org 1", "category/1", {
      name: "Tanaman Pangan",
      description: "Kategori utama",
    });

    expect(fetchMock.mock.calls[1][0]).toBe(
      "/api/v1/organizations/org%201/commodity-categories",
    );
    expect((fetchMock.mock.calls[1][1] as RequestInit).method).toBe("POST");
    expect(fetchMock.mock.calls[3][0]).toBe(
      "/api/v1/organizations/org%201/commodity-categories/category%2F1",
    );
    expect((fetchMock.mock.calls[3][1] as RequestInit).method).toBe("PUT");
  });

  it("creates and updates commodities with the backend contract", async () => {
    const commodity = {
      id: "commodity-1",
      organizationId: "org 1",
      code: "CABAI",
      name: "Cabai",
      commodityCategoryId: "category-1",
      scientificName: null,
      description: null,
      isActive: true,
      createdAt: "2026-08-31T00:00:00Z",
      updatedAt: null,
    };

    fetchMock
      .mockResolvedValueOnce(jsonResponse(csrfResponse))
      .mockResolvedValueOnce(jsonResponse(commodity, 201))
      .mockResolvedValueOnce(jsonResponse(csrfResponse))
      .mockResolvedValueOnce(jsonResponse(commodity));

    await createCommodity("org 1", {
      code: "CABAI",
      name: "Cabai",
      commodityCategoryId: "category-1",
      scientificName: null,
      description: null,
    });
    await updateCommodity("org 1", "commodity/1", {
      name: "Cabai Merah",
      commodityCategoryId: "category-1",
      scientificName: null,
      description: null,
    });

    expect(fetchMock.mock.calls[1][0]).toBe(
      "/api/v1/organizations/org%201/commodities",
    );
    expect((fetchMock.mock.calls[1][1] as RequestInit).method).toBe("POST");
    expect(fetchMock.mock.calls[3][0]).toBe(
      "/api/v1/organizations/org%201/commodities/commodity%2F1",
    );
    expect((fetchMock.mock.calls[3][1] as RequestInit).method).toBe("PUT");
  });

  it("toggles category and commodity activation through PATCH", async () => {
    const category = {
      id: "category-1",
      organizationId: "org 1",
      name: "Tanaman Pangan",
      description: null,
      isActive: false,
      createdAt: "2026-08-31T00:00:00Z",
      updatedAt: null,
    };
    const commodity = {
      id: "commodity-1",
      organizationId: "org 1",
      code: "CABAI",
      name: "Cabai",
      commodityCategoryId: "category-1",
      scientificName: null,
      description: null,
      isActive: true,
      createdAt: "2026-08-31T00:00:00Z",
      updatedAt: null,
    };

    fetchMock
      .mockResolvedValueOnce(jsonResponse(csrfResponse))
      .mockResolvedValueOnce(jsonResponse(category))
      .mockResolvedValueOnce(jsonResponse(csrfResponse))
      .mockResolvedValueOnce(jsonResponse(commodity));

    await setCommodityCategoryActive("org 1", "category 1", false);
    await setCommodityActive("org 1", "commodity 1", true);

    expect(fetchMock.mock.calls[1][0]).toBe(
      "/api/v1/organizations/org%201/commodity-categories/category%201/deactivate",
    );
    expect((fetchMock.mock.calls[1][1] as RequestInit).method).toBe("PATCH");
    expect(fetchMock.mock.calls[3][0]).toBe(
      "/api/v1/organizations/org%201/commodities/commodity%201/activate",
    );
    expect((fetchMock.mock.calls[3][1] as RequestInit).method).toBe("PATCH");
  });
});
