import { describe, expect, it } from "vitest";
import type { Land } from "@/lib/api/contracts";
import {
  filterLands,
  formatSquareMeters,
  getAllocationPercentage,
  getAvailableArea,
  type LandDraft,
  type PlotDraft,
  validateLandDraft,
  validatePlotDraft,
} from "@/lib/lands/land-management";

function land(overrides: Partial<Land> = {}): Land {
  return {
    id: "land-1",
    organizationId: "org-1",
    code: "LHN-01",
    name: "Lahan Timur",
    tenureType: 1,
    totalArea: 1,
    areaUnit: 2,
    totalAreaInSquareMeters: 10_000,
    allocatedPlotAreaInSquareMeters: 2_500,
    address: "Desa Makmur",
    locationDescription: null,
    latitude: null,
    longitude: null,
    notes: null,
    isActive: true,
    createdAt: "2026-08-04T00:00:00Z",
    updatedAt: null,
    plots: [],
    ...overrides,
  };
}

const validLandDraft: LandDraft = {
  code: "LHN-02",
  name: "Lahan Barat",
  tenureType: 2,
  totalArea: "1,5",
  areaUnit: 2,
  address: "",
  locationDescription: "Dekat irigasi",
  latitude: "-7.1",
  longitude: "110.2",
  notes: "",
};

const validPlotDraft: PlotDraft = {
  code: "PTK-01",
  name: "Petak Utara",
  area: "0.25",
  areaUnit: 2,
  generalCondition: "Datar",
  notes: "",
};

describe("land management helpers", () => {
  it("filters land by status and Indonesian text query", () => {
    const result = filterLands([
      land(),
      land({ id: "land-2", code: "LHN-02", name: "Kebun Barat", address: "Wonosobo", isActive: false }),
    ], "wonosobo", "inactive");

    expect(result.map((item) => item.id)).toEqual(["land-2"]);
  });

  it("calculates allocated and available areas defensively", () => {
    expect(getAllocationPercentage(land())).toBe(25);
    expect(getAvailableArea(land())).toBe(7_500);
    expect(getAllocationPercentage(land({ totalAreaInSquareMeters: 0 }))).toBe(0);
  });

  it("formats square meters using the most useful unit", () => {
    expect(formatSquareMeters(15_000)).toBe("1,5 ha");
    expect(formatSquareMeters(2_500)).toBe("2.500 m²");
  });

  it("accepts a complete valid land draft", () => {
    expect(validateLandDraft(validLandDraft, true)).toEqual([]);
  });

  it("requires paired coordinates and valid codes", () => {
    const errors = validateLandDraft({
      ...validLandDraft,
      code: "Lahan 02",
      longitude: "",
    }, true);

    expect(errors).toEqual(expect.arrayContaining([
      expect.stringContaining("Kode lahan"),
      expect.stringContaining("berpasangan"),
    ]));
  });

  it("prevents reducing land area below allocated plots", () => {
    expect(validateLandDraft({
      ...validLandDraft,
      totalArea: "0.2",
    }, false, 2_500)).toContain(
      "Luas lahan tidak boleh lebih kecil daripada total luas petak yang sudah dialokasikan.",
    );
  });

  it("accepts a plot within available capacity", () => {
    expect(validatePlotDraft(validPlotDraft, true, land())).toEqual([]);
  });

  it("allows an edited plot to reuse its current allocation but rejects overflow", () => {
    const existingPlot = {
      id: "plot-1",
      landId: "land-1",
      code: "PTK-01",
      name: "Petak Utara",
      area: 0.25,
      areaUnit: 2 as const,
      generalCondition: null,
      notes: null,
      isActive: true,
      createdAt: "2026-08-04T00:00:00Z",
      updatedAt: null,
    };
    const errors = validatePlotDraft({
      ...validPlotDraft,
      area: "1.1",
    }, false, land(), existingPlot);

    expect(errors[0]).toContain("melebihi sisa kapasitas");
  });
});
