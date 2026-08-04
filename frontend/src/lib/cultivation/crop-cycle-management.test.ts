import { describe, expect, it } from "vitest";
import type {
  Commodity,
  CropCycle,
  CultivationSop,
  Land,
} from "@/lib/api/contracts";
import {
  cropCycleDraftFrom,
  filterCropCycles,
  formatDateOnly,
  getPlannedDurationDays,
  validateCropCycleDraft,
} from "@/lib/cultivation/crop-cycle-management";

const commodity: Commodity = {
  id: "commodity-1",
  organizationId: "org-1",
  code: "CABAI",
  name: "Cabai Merah",
  commodityCategoryId: "category-1",
  scientificName: null,
  description: null,
  isActive: true,
  createdAt: "2026-08-04T00:00:00Z",
  updatedAt: null,
};

const sop: CultivationSop = {
  id: "sop-1",
  organizationId: "org-1",
  commodityId: commodity.id,
  name: "SOP Cabai Standar",
  description: null,
  isActive: true,
  createdAt: "2026-08-04T00:00:00Z",
  updatedAt: null,
  steps: [],
};

const land: Land = {
  id: "land-1",
  organizationId: "org-1",
  code: "LHN-01",
  name: "Lahan Timur",
  tenureType: 1,
  totalArea: 1,
  areaUnit: 2,
  totalAreaInSquareMeters: 10_000,
  allocatedPlotAreaInSquareMeters: 5_000,
  address: null,
  locationDescription: null,
  latitude: null,
  longitude: null,
  notes: null,
  isActive: true,
  createdAt: "2026-08-04T00:00:00Z",
  updatedAt: null,
  plots: [{
    id: "plot-1",
    landId: "land-1",
    code: "PTK-A",
    name: "Petak A",
    area: 0.5,
    areaUnit: 2,
    generalCondition: null,
    notes: null,
    isActive: true,
    createdAt: "2026-08-04T00:00:00Z",
    updatedAt: null,
  }],
};

const cycle: CropCycle = {
  id: "cycle-1",
  organizationId: "org-1",
  code: "SB-2026-01",
  name: "Cabai Musim Kemarau",
  commodityId: commodity.id,
  cultivationSopId: sop.id,
  landId: land.id,
  landPlotId: land.plots[0].id,
  plantedArea: 0.4,
  areaUnit: 2,
  plantedAreaInSquareMeters: 4_000,
  plannedStartDate: "2026-08-10",
  expectedHarvestDate: "2026-11-08",
  actualStartDate: null,
  actualHarvestDate: null,
  status: 1,
  cancellationReason: null,
  notes: null,
  createdAt: "2026-08-04T00:00:00Z",
  updatedAt: null,
};

describe("crop-cycle management helpers", () => {
  it("filters cycles by related land, commodity text, and status", () => {
    const result = filterCropCycles(
      [cycle, { ...cycle, id: "cycle-2", status: 3 }],
      [commodity],
      [land],
      "cabai merah",
      1,
      land.id,
    );

    expect(result.map((item) => item.id)).toEqual(["cycle-1"]);
  });

  it("creates an editable draft and calculates the planned duration", () => {
    expect(cropCycleDraftFrom(cycle)).toMatchObject({
      code: "SB-2026-01",
      landPlotId: "plot-1",
      plantedArea: "0.4",
    });
    expect(getPlannedDurationDays(cycle)).toBe(90);
    expect(formatDateOnly("2026-08-10")).toContain("2026");
  });

  it("accepts a complete plan within the selected plot capacity", () => {
    expect(validateCropCycleDraft(
      cropCycleDraftFrom(cycle),
      true,
      [commodity],
      [sop],
      [land],
    )).toEqual([]);
  });

  it("rejects an invalid code, mismatched SOP, excessive area, and date order", () => {
    const otherSop = { ...sop, id: "sop-2", commodityId: "commodity-2" };
    const draft = {
      ...cropCycleDraftFrom(cycle),
      code: "SB 01",
      cultivationSopId: otherSop.id,
      plantedArea: "0.75",
      expectedHarvestDate: "2026-08-09",
    };

    const errors = validateCropCycleDraft(
      draft,
      true,
      [commodity],
      [otherSop],
      [land],
    );

    expect(errors).toEqual(expect.arrayContaining([
      expect.stringContaining("Kode siklus"),
      expect.stringContaining("SOP budidaya harus sesuai"),
      expect.stringContaining("Luas tanam"),
      expect.stringContaining("Perkiraan panen harus setelah"),
    ]));
  });

  it("rejects inactive references for a new cycle", () => {
    const errors = validateCropCycleDraft(
      cropCycleDraftFrom(cycle),
      true,
      [{ ...commodity, isActive: false }],
      [{ ...sop, isActive: false }],
      [{ ...land, isActive: false }],
    );

    expect(errors).toEqual(expect.arrayContaining([
      expect.stringContaining("Komoditas"),
      expect.stringContaining("SOP budidaya"),
      expect.stringContaining("Lahan"),
    ]));
  });
});
