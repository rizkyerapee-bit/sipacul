import { describe, expect, it } from "vitest";
import type {
  CropCycle,
  CultivationActivity,
  HarvestBatch,
  Land,
} from "@/lib/api/contracts";
import {
  buildCycleStatusBreakdown,
  calculateScheduleProgress,
  selectDefaultCropCycle,
  sortActivitiesForAgenda,
  summarizeHarvests,
  summarizeOrganizationDashboard,
} from "@/lib/dashboard/dashboard-summary";

function cropCycle(
  id: string,
  status: CropCycle["status"],
  plannedStartDate: string,
): CropCycle {
  return {
    id,
    organizationId: "org-1",
    code: id.toUpperCase(),
    name: id,
    commodityId: "commodity-1",
    cultivationSopId: null,
    landId: "land-1",
    landPlotId: "plot-1",
    plantedArea: 1,
    areaUnit: 2,
    plantedAreaInSquareMeters: 10_000,
    plannedStartDate,
    expectedHarvestDate: "2026-05-01",
    actualStartDate: null,
    actualHarvestDate: null,
    status,
    cancellationReason: null,
    notes: null,
    createdAt: "2026-01-01T00:00:00Z",
    updatedAt: null,
  };
}

describe("dashboard summary", () => {
  it("counts only active lands and their active plots", () => {
    const lands = [
      {
        isActive: true,
        totalAreaInSquareMeters: 15_000,
        plots: [{ isActive: true }, { isActive: false }],
      },
      {
        isActive: false,
        totalAreaInSquareMeters: 20_000,
        plots: [{ isActive: true }],
      },
    ] as Land[];
    const cycles = [
      cropCycle("running", 2, "2026-01-01"),
      cropCycle("planned", 1, "2026-02-01"),
    ];

    expect(summarizeOrganizationDashboard(lands, cycles)).toEqual({
      activeLandCount: 1,
      activePlotCount: 1,
      activeAreaHectares: 1.5,
      inProgressCycleCount: 1,
      plannedCycleCount: 1,
    });
  });

  it("prioritizes the newest running cycle", () => {
    const selected = selectDefaultCropCycle([
      cropCycle("completed", 3, "2026-03-01"),
      cropCycle("running-old", 2, "2026-01-01"),
      cropCycle("running-new", 2, "2026-02-01"),
      cropCycle("planned", 1, "2026-04-01"),
    ]);

    expect(selected?.id).toBe("running-new");
  });

  it("returns null when no cycle exists", () => {
    expect(selectDefaultCropCycle([])).toBeNull();
  });

  it("builds all four status buckets including zero values", () => {
    const breakdown = buildCycleStatusBreakdown([
      cropCycle("running", 2, "2026-01-01"),
      cropCycle("complete", 3, "2026-01-01"),
    ]);

    expect(breakdown.map((item) => item.count)).toEqual([0, 1, 1, 0]);
  });

  it("calculates bounded schedule progress", () => {
    const cycle = cropCycle("running", 2, "2026-01-01");
    cycle.expectedHarvestDate = "2026-01-11";

    expect(calculateScheduleProgress(cycle, new Date("2026-01-06T00:00:00"))).toBe(50);
    expect(calculateScheduleProgress(cycle, new Date("2026-02-01T00:00:00"))).toBe(100);
  });

  it("puts running and planned activities before completed ones", () => {
    const activities = [
      { id: "complete", status: 3, plannedDate: "2026-01-01" },
      { id: "planned", status: 1, plannedDate: "2026-01-03" },
      { id: "running", status: 2, plannedDate: "2026-01-04" },
    ] as CultivationActivity[];

    expect(sortActivitiesForAgenda(activities).map((item) => item.id)).toEqual([
      "running",
      "planned",
      "complete",
    ]);
  });

  it("sums confirmed harvest batches without mixing quantity units", () => {
    const batches = [
      { status: 2, quantityUnit: 1, netQuantity: 120, availableQuantity: 50 },
      { status: 2, quantityUnit: 1, netQuantity: 80, availableQuantity: 20 },
      { status: 1, quantityUnit: 1, netQuantity: 500, availableQuantity: 500 },
      { status: 2, quantityUnit: 4, netQuantity: 10, availableQuantity: 10 },
    ] as HarvestBatch[];

    expect(summarizeHarvests(batches)).toEqual({
      confirmedBatchCount: 3,
      netQuantity: 200,
      availableQuantity: 70,
      quantityUnit: 1,
    });
  });
});
