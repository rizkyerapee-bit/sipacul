import { describe, expect, it } from "vitest";
import type {
  CapitalContribution,
  CropCycle,
  CropCycleProfitability,
  ProfitSharingSettlement,
} from "@/lib/api/contracts";
import {
  capitalDraftFrom,
  contributionDateWindow,
  filterCapitalContributions,
  formatRatio,
  parseCapitalAmount,
  profitPools,
  settlementDraftFrom,
  settlementReadiness,
  summarizeCapital,
  validateCapitalDraft,
  validateSettlementDraft,
} from "@/lib/finance/profit-sharing-management";

const cycle: CropCycle = {
  id: "cycle-1",
  organizationId: "org-1",
  code: "CAB-01",
  name: "Cabai Musim 1",
  commodityId: "commodity-1",
  cultivationSopId: null,
  landId: "land-1",
  landPlotId: "plot-1",
  plantedArea: 1,
  areaUnit: 2,
  plantedAreaInSquareMeters: 10_000,
  plannedStartDate: "2026-08-01",
  expectedHarvestDate: "2026-12-01",
  actualStartDate: "2026-08-01",
  actualHarvestDate: "2026-12-10",
  status: 3,
  cancellationReason: null,
  notes: null,
  createdAt: "2026-07-01T00:00:00Z",
  updatedAt: null,
};

function contribution(
  id: string,
  role: 1 | 2,
  amount: number,
  status: 1 | 2 | 3 = 2,
): CapitalContribution {
  return {
    id,
    organizationId: "org-1",
    cropCycleId: "cycle-1",
    code: `MOD-${id}`,
    contributionDate: "2026-08-01",
    contributorCode: role === 1 ? "INV-01" : "MIT-01",
    contributorName: role === 1 ? "Investor Utama" : "Mitra Tani",
    contributorRole: role,
    amount,
    paymentMethod: 2,
    referenceNumber: null,
    notes: null,
    status,
    isConfirmedCapital: status === 2,
    isInvestorCapital: role === 1,
    isPartnerCapital: role === 2,
    confirmedAt: status === 2 ? "2026-08-01T00:00:00Z" : null,
    cancellationReason: null,
    createdAt: "2026-08-01T00:00:00Z",
    updatedAt: null,
  };
}

const profitability: CropCycleProfitability = {
  organizationId: "org-1",
  cropCycleId: "cycle-1",
  cropCycleCode: "CAB-01",
  cropCycleName: "Cabai Musim 1",
  commodityIdSnapshot: "commodity-1",
  commodityCodeSnapshot: "CAB",
  commodityNameSnapshot: "Cabai",
  recognizedRevenue: 15_000_000,
  collectedRevenue: 15_000_000,
  outstandingReceivable: 0,
  activityResourceCost: 2_000_000,
  manualExpenseCost: 7_000_000,
  totalCultivationCost: 9_000_000,
  netProfit: 6_000_000,
  profitMarginPercentage: 40,
  outcome: 3,
  confirmedInvestorCapital: 6_000_000,
  confirmedPartnerCapital: 3_000_000,
  totalConfirmedCapital: 9_000_000,
  capitalFundingGap: 0,
  capitalFundingExcess: 0,
  availableHarvestQuantity: 0,
  harvestQuantityUnit: 1,
  generatedAt: "2026-12-20T00:00:00Z",
};

describe("profit-sharing management", () => {
  it("parses monetary input and formats capital ratios", () => {
    expect(parseCapitalAmount("1 250 000,50")).toBe(1_250_000.5);
    expect(parseCapitalAmount("0")).toBe(0);
    expect(parseCapitalAmount("1.234")).toBeNull();
    expect(formatRatio(1 / 3)).toBe("33,33%");
  });

  it("uses the backend contribution date window and clamps a new draft", () => {
    expect(contributionDateWindow(cycle)).toEqual({
      minimum: "2025-08-01",
      maximum: "2027-12-10",
    });
    expect(capitalDraftFrom(null, cycle, "2028-01-01").contributionDate)
      .toBe("2027-12-10");
  });

  it("validates a capital contribution before sending it", () => {
    const draft = capitalDraftFrom(null, cycle, "2026-08-11");
    expect(validateCapitalDraft(draft, cycle, true)).toContain("Kode setoran modal wajib diisi.");

    expect(validateCapitalDraft({
      ...draft,
      code: "MOD-001",
      contributorCode: "INV-01",
      contributorName: "Investor Utama",
      amount: "6000000",
    }, cycle, true)).toEqual([]);
  });

  it("summarizes only confirmed capital by contributor role", () => {
    expect(summarizeCapital([
      contribution("1", 1, 6_000_000),
      contribution("2", 2, 3_000_000),
      contribution("3", 1, 500_000, 1),
      contribution("4", 2, 250_000, 3),
    ])).toMatchObject({
      investor: 6_000_000,
      partner: 3_000_000,
      total: 9_000_000,
      draft: 500_000,
      draftCount: 1,
    });
  });

  it("filters capital by status, role, and identity", () => {
    const items = [
      contribution("1", 1, 6_000_000),
      contribution("2", 2, 3_000_000),
      contribution("3", 1, 500_000, 1),
    ];
    expect(filterCapitalContributions(items, "mitra", "all", 2).map((item) => item.id))
      .toEqual(["2"]);
    expect(filterCapitalContributions(items, "", 1, "all").map((item) => item.id))
      .toEqual(["3"]);
  });

  it("uses SiPacul one-third management and two-thirds capital pools", () => {
    expect(profitPools(profitability)).toEqual({
      management: 2_000_000,
      capital: 4_000_000,
    });
    expect(profitPools({ ...profitability, outcome: 1, netProfit: -2_000_000 }))
      .toEqual({ management: 0, capital: 0 });
  });

  it("prefills the managing partner from confirmed partner capital", () => {
    expect(settlementDraftFrom(null, [contribution("2", 2, 3_000_000)], "2026-12-20"))
      .toMatchObject({
        settlementDate: "2026-12-20",
        managingPartnerCode: "MIT-01",
        managingPartnerName: "Mitra Tani",
      });
  });

  it("validates settlement identity and notes", () => {
    expect(validateSettlementDraft({
      code: "BH-001",
      settlementDate: "2026-12-20",
      managingPartnerCode: "MIT-01",
      managingPartnerName: "Mitra Tani",
      notes: "Pembagian musim pertama",
    }, true)).toEqual([]);
    expect(validateSettlementDraft({
      code: "",
      settlementDate: "",
      managingPartnerCode: "",
      managingPartnerName: "",
      notes: "",
    }, true)).toHaveLength(4);
  });

  it("reports readiness from terminal cycle, receivable, capital, drafts, and active settlement", () => {
    const contributions = [
      contribution("1", 1, 6_000_000),
      contribution("2", 2, 3_000_000),
    ];
    expect(settlementReadiness(cycle, profitability, contributions, [])
      .every((item) => item.ready)).toBe(true);

    const active = {
      id: "settlement-1",
      status: 2,
      isActive: true,
      settlementDate: "2026-12-20",
    } as ProfitSharingSettlement;
    const blocked = settlementReadiness(
      { ...cycle, status: 2 },
      { ...profitability, outstandingReceivable: 100_000 },
      [...contributions, contribution("3", 1, 1, 1)],
      [active],
    );
    expect(blocked.filter((item) => !item.ready).map((item) => item.key))
      .toEqual(["cycle", "receivable", "draft-capital", "active"]);
  });
});
