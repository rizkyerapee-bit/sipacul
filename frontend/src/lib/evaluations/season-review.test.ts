import { describe, expect, it } from "vitest";
import { ApiError } from "@/lib/api/client";
import { seasonReviewErrorMessage, seasonReviewStatusLabels } from "@/lib/evaluations/season-review";

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
