import { afterEach, describe, expect, it, vi } from "vitest";
import { createSeasonReview, finalizeSeasonReview, getSeasonReviewByCropCycle, updateSeasonReview } from "@/lib/api/client";

const review = { id: "review-1", organizationId: "org 1", cropCycleId: "cycle/1", reviewDate: "2026-08-22", findings: "F", lessonsLearned: "L", nextSeasonRecommendations: "R", status: 1, finalizedAt: null, createdAt: "2026-08-22T00:00:00Z", updatedAt: null };
const json = (body: unknown) => new Response(JSON.stringify(body), { status: 200, headers: { "Content-Type": "application/json" } });
const csrf = { requestToken: "token-1", headerName: "X-CSRF" };
const originalFetch = globalThis.fetch;

afterEach(() => { globalThis.fetch = originalFetch; });

describe("season review api client", () => {
  it("reads by encoded crop cycle without CSRF", async () => {
    const fetchMock = vi.fn().mockResolvedValue(json(review)); vi.stubGlobal("fetch", fetchMock);
    await getSeasonReviewByCropCycle("org 1", "cycle/1");
    expect(fetchMock).toHaveBeenCalledOnce();
    expect(fetchMock.mock.calls[0][0]).toBe("/api/v1/organizations/org%201/season-reviews/by-crop-cycle/cycle%2F1");
  });

  it.each([
    ["create", () => createSeasonReview("org-1", { cropCycleId: "cycle-1", reviewDate: "2026-08-22", findings: "F", lessonsLearned: "L", nextSeasonRecommendations: "R" }), "POST", "/api/v1/organizations/org-1/season-reviews"],
    ["update", () => updateSeasonReview("org-1", "review-1", { reviewDate: "2026-08-22", findings: "F", lessonsLearned: "L", nextSeasonRecommendations: "R" }), "PUT", "/api/v1/organizations/org-1/season-reviews/review-1"],
    ["finalize", () => finalizeSeasonReview("org-1", "review-1"), "PATCH", "/api/v1/organizations/org-1/season-reviews/review-1/finalize"],
  ])("uses CSRF for %s", async (_name, action, method, path) => {
    const fetchMock = vi.fn().mockResolvedValueOnce(json(csrf)).mockResolvedValueOnce(json(review)); vi.stubGlobal("fetch", fetchMock);
    await action();
    expect(fetchMock).toHaveBeenCalledTimes(2);
    expect(fetchMock.mock.calls[1][0]).toBe(path);
    const init = fetchMock.mock.calls[1][1] as RequestInit;
    expect(init.method).toBe(method);
    expect(new Headers(init.headers).get("X-CSRF")).toBe("token-1");
  });
});
