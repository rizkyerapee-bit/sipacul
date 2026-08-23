import { describe, expect, it } from "vitest";
import { ApiError } from "@/lib/api/client";
import { getSeasonReviewViewState, seasonReviewErrorMessage, seasonReviewStatusLabels } from "@/lib/evaluations/season-review";

describe("season review presentation", () => {
  it("labels draft and finalized states", () => {
    expect(seasonReviewStatusLabels[1]).toBe("Draf");
    expect(seasonReviewStatusLabels[2]).toBe("Final");
  });

  it("translates known API errors", () => {
    const error = new ApiError(409, "Conflict", {
      type: "about:blank",
      title: "Conflict",
      status: 409,
      detail: "Conflict",
      code: "SeasonReviews.AlreadyExists",
    });
    expect(seasonReviewErrorMessage(error)).toBe("Evaluasi untuk musim ini sudah tersedia.");
  });

  it("keeps useful unknown errors", () => {
    expect(seasonReviewErrorMessage(new Error("Jaringan terputus"))).toBe("Jaringan terputus");
  });
});

describe("season review view state", () => {
  it.each([
    [false, true, false, null, "unavailable"],
    [true, false, false, null, "unavailable"],
    [true, true, true, null, "loading"],
    [true, true, false, null, "empty"],
    [true, true, false, 1, "draft"],
    [true, true, false, 2, "final"],
  ] as const)("resolves permissions and lifecycle", (canRead, ready, loading, status, expected) => {
    expect(getSeasonReviewViewState(canRead, ready, loading, status)).toBe(expected);
  });
});
