import { describe, expect, it } from "vitest";
import type {
  CropCycle,
  CultivationActivity,
  CultivationSop,
} from "@/lib/api/contracts";
import {
  activityDraftFrom,
  filterActivities,
  formatCurrency,
  parseDecimal,
  resourceDraftFrom,
  validateActivityDraft,
  validateResourceDraft,
} from "@/lib/cultivation/activity-management";

const cycle: CropCycle = {
  id: "cycle-1",
  organizationId: "org-1",
  code: "SB-01",
  name: "Cabai Musim Kemarau",
  commodityId: "commodity-1",
  cultivationSopId: "sop-1",
  landId: "land-1",
  landPlotId: "plot-1",
  plantedArea: 0.25,
  areaUnit: 2,
  plantedAreaInSquareMeters: 2500,
  plannedStartDate: "2026-08-10",
  expectedHarvestDate: "2026-11-10",
  actualStartDate: "2026-08-11",
  actualHarvestDate: null,
  status: 2,
  cancellationReason: null,
  notes: null,
  createdAt: "2026-08-04T00:00:00Z",
  updatedAt: null,
};

const sop: CultivationSop = {
  id: "sop-1",
  organizationId: "org-1",
  commodityId: "commodity-1",
  name: "SOP Cabai",
  description: null,
  isActive: true,
  createdAt: "2026-08-04T00:00:00Z",
  updatedAt: null,
  steps: [{
    id: "step-1",
    organizationId: "org-1",
    cultivationSopId: "sop-1",
    sequence: 1,
    name: "Pemupukan dasar",
    description: null,
    plannedDayOffset: 7,
    estimatedDurationDays: 1,
    isRequired: true,
    createdAt: "2026-08-04T00:00:00Z",
    updatedAt: null,
  }],
};

const activity: CultivationActivity = {
  id: "activity-1",
  organizationId: "org-1",
  cropCycleId: cycle.id,
  code: "ACT-001",
  name: "Pemupukan dasar",
  activityType: 5,
  cultivationSopId: sop.id,
  cultivationSopStepId: sop.steps[0].id,
  sopStepSequenceSnapshot: 1,
  sopStepNameSnapshot: "Pemupukan dasar",
  sopPlannedDayOffsetSnapshot: 7,
  sopEstimatedDurationDaysSnapshot: 1,
  sopIsRequiredSnapshot: true,
  plannedDate: "2026-08-17",
  actualStartDate: null,
  actualCompletionDate: null,
  status: 1,
  sopComplianceStatus: 2,
  outcome: null,
  issueNotes: null,
  deviationReason: null,
  cancellationReason: null,
  notes: "Gunakan dosis sesuai SOP.",
  totalActualCost: 450000,
  resources: [],
  createdAt: "2026-08-04T00:00:00Z",
  updatedAt: null,
};

describe("cultivation activity helpers", () => {
  it("filters activities by text, status, and type", () => {
    const result = filterActivities(
      [activity, { ...activity, id: "activity-2", status: 3, activityType: 9 }],
      "pemupukan",
      1,
      5,
    );

    expect(result.map((item) => item.id)).toEqual(["activity-1"]);
  });

  it("accepts a complete SOP-linked activity plan", () => {
    expect(validateActivityDraft(
      activityDraftFrom(activity),
      true,
      cycle,
      [sop],
    )).toEqual([]);
  });

  it("rejects an invalid activity code, date, and SOP step", () => {
    const errors = validateActivityDraft({
      ...activityDraftFrom(activity),
      code: "ACT 01",
      plannedDate: "2026-12-01",
      cultivationSopStepId: "missing-step",
    }, true, cycle, [sop]);

    expect(errors).toEqual(expect.arrayContaining([
      expect.stringContaining("Kode aktivitas"),
      expect.stringContaining("perkiraan panen"),
      expect.stringContaining("Langkah SOP"),
    ]));
  });

  it("validates resource quantity and permits a zero unit cost", () => {
    const freeLabor = {
      ...resourceDraftFrom(null),
      resourceType: 2 as const,
      description: "Tenaga keluarga",
      quantity: "2,5",
      unit: "HOK",
      unitCost: "0",
    };

    expect(validateResourceDraft(freeLabor)).toEqual([]);
    expect(parseDecimal(freeLabor.quantity)).toBe(2.5);
    expect(formatCurrency(450000)).toContain("450.000");
  });

  it("rejects incomplete or negative resource data", () => {
    expect(validateResourceDraft({
      ...resourceDraftFrom(null),
      description: "",
      quantity: "0",
      unit: "",
      unitCost: "-1",
    })).toHaveLength(4);
  });
});
