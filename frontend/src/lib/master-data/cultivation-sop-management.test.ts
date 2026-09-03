import { describe, expect, it } from "vitest";
import type { CultivationSop, CultivationSopStep } from "@/lib/api/contracts";
import {
  cultivationSopDraftFrom,
  cultivationSopStepDraftFrom,
  filterCultivationSops,
  getCultivationSopStatusLabel,
  getCultivationSopStepMoveSequence,
  sortCultivationSopSteps,
  toCreateCultivationSopRequest,
  toCultivationSopStepRequest,
  toUpdateCultivationSopRequest,
  type CultivationSopDraft,
  type CultivationSopStepDraft,
  validateCultivationSopDraft,
  validateCultivationSopStepDraft,
} from "@/lib/master-data/cultivation-sop-management";

function step(overrides: Partial<CultivationSopStep> = {}): CultivationSopStep {
  return {
    id: "step-1",
    organizationId: "org-1",
    cultivationSopId: "sop-1",
    sequence: 1,
    name: "Persiapan lahan",
    description: null,
    plannedDayOffset: -7,
    estimatedDurationDays: 2,
    isRequired: true,
    createdAt: "2026-09-03T00:00:00Z",
    updatedAt: null,
    ...overrides,
  };
}

function sop(overrides: Partial<CultivationSop> = {}): CultivationSop {
  return {
    id: "sop-1",
    organizationId: "org-1",
    commodityId: "commodity-1",
    name: "SOP Cabai Musim Kemarau",
    description: "Budidaya hemat air",
    isActive: true,
    createdAt: "2026-09-03T00:00:00Z",
    updatedAt: null,
    steps: [step()],
    ...overrides,
  };
}

const validSopDraft: CultivationSopDraft = {
  commodityId: "commodity-1",
  name: " SOP Cabai ",
  description: " Panduan budidaya ",
};

const validStepDraft: CultivationSopStepDraft = {
  name: " Pemupukan dasar ",
  description: " ",
  plannedDayOffset: "-3",
  estimatedDurationDays: "2",
  isRequired: true,
};

describe("cultivation SOP management helpers", () => {
  it("creates empty drafts and restores existing SOP values", () => {
    expect(cultivationSopDraftFrom()).toEqual({
      commodityId: "",
      name: "",
      description: "",
    });
    expect(cultivationSopDraftFrom(sop())).toMatchObject({
      commodityId: "commodity-1",
      name: "SOP Cabai Musim Kemarau",
    });
    expect(cultivationSopStepDraftFrom(step())).toMatchObject({
      plannedDayOffset: "-7",
      estimatedDurationDays: "2",
      isRequired: true,
    });
  });

  it("validates SOP identity and backend length limits", () => {
    expect(validateCultivationSopDraft(validSopDraft)).toEqual([]);
    expect(validateCultivationSopDraft({
      commodityId: "",
      name: " ",
      description: "x".repeat(1001),
    })).toEqual(expect.arrayContaining([
      "Komoditas wajib dipilih.",
      "Nama SOP wajib diisi.",
      "Deskripsi SOP maksimal 1000 karakter.",
    ]));
  });

  it("validates integer and range rules for SOP steps", () => {
    expect(validateCultivationSopStepDraft(validStepDraft)).toEqual([]);
    expect(validateCultivationSopStepDraft({
      ...validStepDraft,
      plannedDayOffset: "1.5",
      estimatedDurationDays: "366",
    })).toEqual(expect.arrayContaining([
      "Offset hari rencana harus berupa bilangan bulat.",
      "Estimasi durasi harus berada di antara 1 dan 365.",
    ]));
  });

  it("normalizes API request payloads without allowing commodity updates", () => {
    expect(toCreateCultivationSopRequest(validSopDraft)).toEqual({
      commodityId: "commodity-1",
      name: "SOP Cabai",
      description: "Panduan budidaya",
    });
    expect(toUpdateCultivationSopRequest(validSopDraft)).toEqual({
      name: "SOP Cabai",
      description: "Panduan budidaya",
    });
    expect(toCultivationSopStepRequest(validStepDraft)).toEqual({
      name: "Pemupukan dasar",
      description: null,
      plannedDayOffset: -3,
      estimatedDurationDays: 2,
      isRequired: true,
    });
  });

  it("filters by commodity, lifecycle status, and searchable step content", () => {
    const result = filterCultivationSops([
      sop(),
      sop({
        id: "sop-2",
        commodityId: "commodity-2",
        name: "SOP Padi",
        isActive: false,
        steps: [step({ id: "step-2", name: "Pengairan berkala" })],
      }),
    ], "pengairan", "commodity-2", "inactive");

    expect(result.map((item) => item.id)).toEqual(["sop-2"]);
  });

  it("sorts steps without mutating backend response order", () => {
    const source = [
      step({ id: "step-2", sequence: 2 }),
      step({ id: "step-1", sequence: 1 }),
    ];

    expect(sortCultivationSopSteps(source).map((item) => item.sequence))
      .toEqual([1, 2]);
    expect(source.map((item) => item.sequence)).toEqual([2, 1]);
  });

  it("derives server move targets and protects list boundaries", () => {
    const steps = [
      step({ id: "step-1", sequence: 1 }),
      step({ id: "step-2", sequence: 2 }),
      step({ id: "step-3", sequence: 3 }),
    ];

    expect(getCultivationSopStepMoveSequence(steps, "step-2", "up")).toBe(1);
    expect(getCultivationSopStepMoveSequence(steps, "step-2", "down")).toBe(3);
    expect(getCultivationSopStepMoveSequence(steps, "step-1", "up")).toBeNull();
    expect(getCultivationSopStepMoveSequence(steps, "missing", "down")).toBeNull();
    expect(getCultivationSopStatusLabel(true)).toBe("Aktif");
    expect(getCultivationSopStatusLabel(false)).toBe("Nonaktif");
  });
});