import { beforeEach, describe, expect, it, vi } from "vitest";
import {
  addCultivationSopStep,
  createCultivationSop,
  getCultivationSop,
  getCultivationSops,
  moveCultivationSopStep,
  removeCultivationSopStep,
  setCultivationSopActive,
  updateCultivationSop,
  updateCultivationSopStep,
} from "@/lib/api/client";

const fetchMock = vi.fn<typeof fetch>();

function jsonResponse(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { "content-type": "application/json" },
  });
}

describe("cultivation SOP API client", () => {
  beforeEach(() => {
    fetchMock.mockReset();
    vi.stubGlobal("fetch", fetchMock);
  });

  it("reads collections, commodity filters, and details with encoded identifiers", async () => {
    fetchMock
      .mockResolvedValueOnce(jsonResponse([]))
      .mockResolvedValueOnce(jsonResponse([]))
      .mockResolvedValueOnce(jsonResponse({ id: "sop 1", steps: [] }));

    await getCultivationSops("org 1");
    await getCultivationSops("org 1", "commodity 1");
    await getCultivationSop("org 1", "sop 1");

    expect(fetchMock.mock.calls.map((call) => call[0])).toEqual([
      "/api/v1/organizations/org%201/cultivation-sops",
      "/api/v1/organizations/org%201/cultivation-sops?commodityId=commodity%201",
      "/api/v1/organizations/org%201/cultivation-sops/sop%201",
    ]);
    expect(fetchMock.mock.calls.map((call) =>
      (call[1] as RequestInit).method,
    )).toEqual([undefined, undefined, undefined]);
  });

  it("writes the complete SOP lifecycle through CSRF-protected encoded routes", async () => {
    const createRequest = {
      commodityId: "commodity 1",
      name: "SOP Cabai",
      description: null,
    };
    const stepRequest = {
      name: "Persiapan lahan",
      description: null,
      plannedDayOffset: -7,
      estimatedDurationDays: 2,
      isRequired: true,
    };

    for (let index = 0; index < 8; index += 1) {
      fetchMock
        .mockResolvedValueOnce(jsonResponse({
          requestToken: `sop-csrf-${index}`,
          headerName: "X-CSRF",
        }))
        .mockResolvedValueOnce(jsonResponse({
          id: "sop 1",
          ...createRequest,
          steps: [],
        }, index === 0 ? 201 : 200));
    }

    await createCultivationSop("org 1", createRequest);
    await updateCultivationSop("org 1", "sop 1", {
      name: "SOP Cabai Revisi",
      description: "Versi lapangan",
    });
    await setCultivationSopActive("org 1", "sop 1", false);
    await addCultivationSopStep("org 1", "sop 1", stepRequest);
    await updateCultivationSopStep("org 1", "sop 1", "step 1", {
      ...stepRequest,
      estimatedDurationDays: 3,
    });
    await moveCultivationSopStep("org 1", "sop 1", "step 1", {
      newSequence: 2,
    });
    await removeCultivationSopStep("org 1", "sop 1", "step 1");
    await setCultivationSopActive("org 1", "sop 1", true);

    const sopPath = "/api/v1/organizations/org%201/cultivation-sops/sop%201";
    expect([1, 3, 5, 7, 9, 11, 13, 15]
      .map((index) => fetchMock.mock.calls[index][0])).toEqual([
        "/api/v1/organizations/org%201/cultivation-sops",
        sopPath,
        `${sopPath}/deactivate`,
        `${sopPath}/steps`,
        `${sopPath}/steps/step%201`,
        `${sopPath}/steps/step%201/move`,
        `${sopPath}/steps/step%201`,
        `${sopPath}/activate`,
      ]);
    expect([1, 3, 5, 7, 9, 11, 13, 15]
      .map((index) =>
        (fetchMock.mock.calls[index][1] as RequestInit).method,
      )).toEqual([
        "POST", "PUT", "PATCH", "POST", "PUT", "PATCH", "DELETE", "PATCH",
      ]);
    expect(new Headers(
      (fetchMock.mock.calls[15][1] as RequestInit).headers,
    ).get("X-CSRF")).toBe("sop-csrf-7");
    expect((fetchMock.mock.calls[11][1] as RequestInit).body)
      .toBe(JSON.stringify({ newSequence: 2 }));
  });
});