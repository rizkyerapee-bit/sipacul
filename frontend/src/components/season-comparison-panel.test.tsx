import { renderToStaticMarkup } from "react-dom/server";
import { describe, expect, it, vi } from "vitest";
import type { SeasonEvaluation } from "@/lib/api/contracts";
import { buildSeasonComparison } from "@/lib/evaluations/season-comparison";
import { SeasonComparisonPanel } from "./season-comparison-panel";

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

function render(selectedCount: number, seasons: SeasonEvaluation[] = []): string {
  const comparison = buildSeasonComparison(seasons, seasons.map((item) => item.cropCycleId));
  return renderToStaticMarkup(
    <SeasonComparisonPanel comparison={comparison} selectedCount={selectedCount} onClear={vi.fn()} />,
  );
}

describe("SeasonComparisonPanel", () => {
  it("guides an empty selection without showing a clear action", () => {
    const html = render(0);
    expect(html).toContain("Pilih musim dari panel detail");
    expect(html).not.toContain("Hapus pilihan");
  });

  it("keeps one selected season pending and exposes the clear action", () => {
    const html = render(1, [season("one")]);
    expect(html).toContain("1 musim dipilih");
    expect(html).toContain("Pilih minimal dua dan maksimal empat musim selesai");
    expect(html).toContain("Hapus pilihan");
  });

  it("renders the oldest season as baseline with a neutral delta", () => {
    const html = render(2, [
      season("newer", { actualHarvestDate: "2026-08-01", netProfit: 7_000_000 }),
      season("older", { actualHarvestDate: "2026-05-01", netProfit: 6_000_000 }),
    ]);
    expect(html.indexOf("Musim older")).toBeLessThan(html.indexOf("Musim newer"));
    expect(html).toContain("Nilai baseline");
    expect(html).toContain("dari baseline");
  });

  it("warns when commodity and plot contexts differ", () => {
    const html = render(2, [
      season("one"),
      season("two", {
        actualHarvestDate: "2026-08-01",
        commodityId: "commodity-2",
        commodityName: "Nanas",
        landPlotId: "plot-2",
        landPlotName: "Petak Selatan",
      }),
    ]);
    expect(html).toContain("Konteks pilihan berbeda");
    expect(html).toContain("Komoditas tidak sama");
    expect(html).toContain("Petak tidak sama");
  });

  it("keeps missing facts visibly unavailable", () => {
    const html = render(2, [
      season("one", { sopCompliancePercentage: null }),
      season("two", { actualHarvestDate: "2026-08-01", sopCompliancePercentage: 90 }),
    ]);
    expect(html).toContain("Belum tersedia");
    expect(html).toContain("Delta tidak tersedia");
  });
});
