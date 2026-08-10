import { describe, expect, it } from "vitest";
import type { CropCycle, HarvestBatch } from "@/lib/api/contracts";
import {
  filterHarvestBatches,
  formatHarvestQuantity,
  harvestDraftFrom,
  parseHarvestNumber,
  requiredHarvestUnit,
  summarizeHarvest,
  validateHarvestDraft,
} from "@/lib/harvest/harvest-management";

const cycle: CropCycle = {
  id: "cycle-1",
  organizationId: "org-1",
  code: "SB-01",
  name: "Nanas Hamparan Timur",
  commodityId: "commodity-1",
  cultivationSopId: null,
  landId: "land-1",
  landPlotId: "plot-1",
  plantedArea: 1,
  areaUnit: 2,
  plantedAreaInSquareMeters: 10000,
  plannedStartDate: "2026-01-10",
  expectedHarvestDate: "2027-06-10",
  actualStartDate: "2026-01-12",
  actualHarvestDate: null,
  status: 2,
  cancellationReason: null,
  notes: null,
  createdAt: "2026-01-01T00:00:00Z",
  updatedAt: null,
};

const batch: HarvestBatch = {
  id: "harvest-1",
  organizationId: "org-1",
  cropCycleId: cycle.id,
  code: "PNN-001",
  harvestDate: "2027-05-20",
  grossQuantity: 1250,
  rejectedQuantity: 50,
  netQuantity: 1200,
  quantityUnit: 1,
  qualityGrade: "Grade A",
  storageLocation: "Gudang Timur",
  notes: "Buah matang seragam.",
  status: 2,
  confirmedAt: "2027-05-20T08:00:00Z",
  cancellationReason: null,
  confirmedSoldQuantity: 300,
  availableQuantity: 900,
  createdAt: "2027-05-20T07:00:00Z",
  updatedAt: null,
};

describe("harvest management helpers", () => {
  it("accepts a complete harvest batch and Indonesian decimals", () => {
    expect(validateHarvestDraft(harvestDraftFrom(batch), true, cycle)).toEqual([]);
    expect(parseHarvestNumber("12,5")).toBe(12.5);
    expect(formatHarvestQuantity(1200, 1)).toContain("1.200");
  });

  it("rejects invalid codes, dates, and quantities", () => {
    const errors = validateHarvestDraft({
      ...harvestDraftFrom(batch),
      code: " PANEN 01",
      harvestDate: "2025-12-01",
      grossQuantity: "100",
      rejectedQuantity: "125",
    }, true, cycle);

    expect(errors).toEqual(expect.arrayContaining([
      expect.stringContaining("Kode batch"),
      expect.stringContaining("tanggal mulai aktual"),
      expect.stringContaining("melebihi hasil kotor"),
    ]));
  });

  it("filters by status, unit, and harvest metadata", () => {
    const draft = {
      ...batch,
      id: "harvest-2",
      code: "PNN-002",
      qualityGrade: "Grade B",
      status: 1 as const,
    };

    expect(filterHarvestBatches([batch, draft], "gudang", 2, 1))
      .toEqual([batch]);
  });

  it("summarizes confirmed net and available quantities", () => {
    const second = {
      ...batch,
      id: "harvest-2",
      netQuantity: 500,
      rejectedQuantity: 25,
      confirmedSoldQuantity: 100,
      availableQuantity: 400,
    };

    expect(summarizeHarvest([batch, second])).toMatchObject({
      batchCount: 2,
      confirmedCount: 2,
      netQuantity: 1700,
      availableQuantity: 1300,
      rejectedQuantity: 75,
      unit: 1,
      hasMixedUnits: false,
    });
  });

  it("does not add quantities with different units", () => {
    expect(summarizeHarvest([
      batch,
      { ...batch, id: "harvest-2", quantityUnit: 2 },
    ])).toMatchObject({
      unit: null,
      hasMixedUnits: true,
      netQuantity: 0,
      availableQuantity: 0,
    });
  });

  it("uses one active cycle unit and ignores cancelled batches or the edited batch", () => {
    const cancelled = {
      ...batch,
      id: "harvest-cancelled",
      status: 3 as const,
      quantityUnit: 6 as const,
    };
    const sibling = { ...batch, id: "harvest-sibling" };

    expect(requiredHarvestUnit([batch, cancelled])).toBe(1);
    expect(requiredHarvestUnit([batch, sibling], batch.id)).toBe(1);
    expect(requiredHarvestUnit([batch], batch.id)).toBeNull();
  });
});
