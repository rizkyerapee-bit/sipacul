import { describe, expect, it } from "vitest";
import type { SeasonEvaluation } from "@/lib/api/contracts";
import {
  attentionValueLabel,
  formatSeasonDate,
  formatSeasonPercentage,
  formatVarianceDays,
  summarizeSeasonPage,
} from "@/lib/evaluations/season-history";

function season(overrides: Partial<SeasonEvaluation> = {}): SeasonEvaluation {
  return {
    organizationId: "org-1",
    cropCycleId: "cycle-1",
    cropCycleCode: "CAB-01",
    cropCycleName: "Cabai Musim Pertama",
    landId: "land-1",
    landCode: "LHN-01",
    landName: "Lahan Utama",
    landPlotId: "plot-1",
    landPlotCode: "PTK-01",
    landPlotName: "Petak Utara",
    commodityId: "commodity-1",
    commodityCode: "CAB",
    commodityName: "Cabai",
    cropCycleStatus: 3,
    plannedStartDate: "2027-01-01",
    expectedHarvestDate: "2027-05-01",
    actualStartDate: "2027-01-03",
    actualHarvestDate: "2027-05-04",
    startVarianceDays: 2,
    harvestVarianceDays: 3,
    totalActivityCount: 10,
    completedActivityCount: 10,
    cancelledActivityCount: 0,
    pendingActivityCount: 0,
    issueActivityCount: 0,
    activityCompletionPercentage: 100,
    sopLinkedActivityCount: 8,
    sopCompliantActivityCount: 8,
    sopDeviatedActivityCount: 0,
    sopNotEvaluatedActivityCount: 0,
    sopCompliancePercentage: 100,
    confirmedHarvestBatchCount: 2,
    recognizedRevenue: 15_000_000,
    collectedRevenue: 14_000_000,
    outstandingReceivable: 1_000_000,
    totalCultivationCost: 9_000_000,
    netProfit: 6_000_000,
    profitMarginPercentage: 40,
    profitabilityOutcome: 3,
    capitalFundingGap: 0,
    isReadyForReview: true,
    requiresAttention: true,
    criticalAttentionCount: 0,
    warningAttentionCount: 1,
    informationAttentionCount: 0,
    attentions: [{ code: 11, severity: 2, value: 1_000_000 }],
    generatedAt: "2027-05-05T00:00:00Z",
    ...overrides,
  };
}

describe("season history presentation", () => {
  it("formats API percentages without treating them as ratios", () => {
    expect(formatSeasonPercentage(62.5)).toBe("62,5%");
    expect(formatSeasonPercentage(null)).toBe("Belum tersedia");
  });

  it("formats date-only values without UTC day shifts", () => {
    expect(formatSeasonDate("2027-05-04")).toBe("4 Mei 2027");
    expect(formatSeasonDate(null)).toBe("Belum tercatat");
  });

  it("explains positive, zero, negative, and missing variances", () => {
    expect(formatVarianceDays(3)).toBe("3 hari terlambat");
    expect(formatVarianceDays(0)).toBe("Sesuai rencana");
    expect(formatVarianceDays(-2)).toBe("2 hari lebih awal");
    expect(formatVarianceDays(null)).toBe("Belum tersedia");
  });

  it("uses units defined by each attention code", () => {
    expect(attentionValueLabel({ code: 3, severity: 2, value: 4 })).toBe("4 hari");
    expect(attentionValueLabel({ code: 8, severity: 2, value: 2 })).toBe("2 aktivitas");
    expect(attentionValueLabel({ code: 13, severity: 3, value: 250_000 })).toContain("250.000");
    expect(attentionValueLabel({ code: 12, severity: 1, value: null })).toBeNull();
  });

  it("summarizes only the seasons present on the current page", () => {
    expect(summarizeSeasonPage([
      season(),
      season({
        cropCycleId: "cycle-2",
        isReadyForReview: false,
        requiresAttention: true,
        criticalAttentionCount: 2,
        outstandingReceivable: 500_000,
      }),
      season({
        cropCycleId: "cycle-3",
        requiresAttention: false,
        criticalAttentionCount: 0,
        outstandingReceivable: 0,
      }),
    ])).toEqual({
      visibleSeasonCount: 3,
      reviewReadyCount: 2,
      requiresAttentionCount: 2,
      criticalAttentionCount: 2,
      outstandingReceivable: 1_500_000,
    });
  });
});
