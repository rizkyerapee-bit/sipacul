import { describe, expect, it } from "vitest";
import type { SeasonEvaluation } from "@/lib/api/contracts";
import {
  buildSeasonComparison,
  maximumComparedSeasons,
  updateComparisonSelection,
} from "@/lib/evaluations/season-comparison";

function season(id: string, overrides: Partial<SeasonEvaluation> = {}): SeasonEvaluation {
  return {
    organizationId: "org-1",
    cropCycleId: id,
    cropCycleCode: id.toUpperCase(),
    cropCycleName: `Musim ${id}`,
    landId: "land-1",
    landCode: "LHN-1",
    landName: "Lahan Utama",
    landPlotId: "plot-1",
    landPlotCode: "PTK-1",
    landPlotName: "Petak Utara",
    commodityId: "commodity-1",
    commodityCode: "CAB",
    commodityName: "Cabai",
    cropCycleStatus: 3,
    plannedStartDate: "2026-01-01",
    expectedHarvestDate: "2026-05-01",
    actualStartDate: "2026-01-01",
    actualHarvestDate: "2026-05-01",
    startVarianceDays: 0,
    harvestVarianceDays: 2,
    totalActivityCount: 10,
    completedActivityCount: 10,
    cancelledActivityCount: 0,
    pendingActivityCount: 0,
    issueActivityCount: 2,
    activityCompletionPercentage: 100,
    sopLinkedActivityCount: 8,
    sopCompliantActivityCount: 8,
    sopDeviatedActivityCount: 0,
    sopNotEvaluatedActivityCount: 0,
    sopCompliancePercentage: 100,
    confirmedHarvestBatchCount: 1,
    recognizedRevenue: 15_000_000,
    collectedRevenue: 14_000_000,
    outstandingReceivable: 1_000_000,
    totalCultivationCost: 9_000_000,
    netProfit: 6_000_000,
    profitMarginPercentage: 40,
    profitabilityOutcome: 3,
    capitalFundingGap: 0,
    isReadyForReview: true,
    requiresAttention: false,
    criticalAttentionCount: 0,
    warningAttentionCount: 0,
    informationAttentionCount: 0,
    attentions: [],
    generatedAt: "2026-05-02T00:00:00Z",
    ...overrides,
  };
}

describe("season comparison selection", () => {
  it("selects only terminal visible seasons and toggles an existing choice", () => {
    const visible = [season("one"), season("active", { isReadyForReview: false })];
    expect(updateComparisonSelection([], "active", visible).selectedIds).toEqual([]);
    expect(updateComparisonSelection([], "one", visible).selectedIds).toEqual(["one"]);
    expect(updateComparisonSelection(["one"], "one", visible).selectedIds).toEqual([]);
  });

  it("normalizes stale choices and enforces four seasons", () => {
    const visible = ["one", "two", "three", "four", "five"].map((id) => season(id));
    const selected = ["stale", "one", "two", "three", "four"];
    const result = updateComparisonSelection(selected, "five", visible);
    expect(result.selectedIds).toHaveLength(maximumComparedSeasons);
    expect(result.selectedIds).not.toContain("stale");
    expect(result.limitReached).toBe(true);
  });
});

describe("season comparison model", () => {
  it("requires between two and four eligible seasons", () => {
    const seasons = [season("one"), season("two")];
    expect(buildSeasonComparison(seasons, ["one"])).toBeNull();
    expect(buildSeasonComparison(seasons, ["one", "two"])).not.toBeNull();
  });

  it("orders the oldest season as baseline and calculates neutral deltas", () => {
    const newer = season("newer", {
      actualHarvestDate: "2026-08-01",
      netProfit: 7_500_000,
      issueActivityCount: 1,
    });
    const older = season("older", {
      actualHarvestDate: "2026-05-01",
      netProfit: 6_000_000,
      issueActivityCount: 2,
    });
    const comparison = buildSeasonComparison([newer, older], ["newer", "older"]);
    expect(comparison?.columns.map((column) => column.cropCycleId)).toEqual(["older", "newer"]);
    const profit = comparison?.rows.find((row) => row.key === "netProfit");
    expect(profit?.values[0]).toMatchObject({ deltaFromBaseline: 0, direction: "unchanged" });
    expect(profit?.values[1]).toMatchObject({ deltaFromBaseline: 1_500_000, direction: "increase" });
    const issues = comparison?.rows.find((row) => row.key === "issueActivityCount");
    expect(issues?.values[1]).toMatchObject({ deltaFromBaseline: -1, direction: "decrease" });
  });

  it("keeps missing percentages unavailable and exposes context differences", () => {
    const comparison = buildSeasonComparison([
      season("one", { sopCompliancePercentage: null }),
      season("two", {
        actualHarvestDate: "2026-08-01",
        commodityId: "commodity-2",
        landPlotId: "plot-2",
        sopCompliancePercentage: 90,
      }),
    ], ["one", "two"]);
    const compliance = comparison?.rows.find((row) => row.key === "sopCompliancePercentage");
    expect(compliance?.values[1]).toEqual({
      value: 90,
      deltaFromBaseline: null,
      direction: "unavailable",
    });
    expect(comparison).toMatchObject({ sameCommodity: false, sameLandPlot: false });
  });
});
