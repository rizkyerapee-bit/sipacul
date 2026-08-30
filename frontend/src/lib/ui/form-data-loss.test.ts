import { describe, expect, it } from "vitest";
import {
  hasFormDraftChanged,
  resolveFormCloseDecision,
} from "@/lib/ui/form-data-loss";

describe("form data-loss protection", () => {
  it("never closes an editor from a backdrop click", () => {
    expect(resolveFormCloseDecision({
      source: "backdrop",
      isDirty: false,
      isSaving: false,
    })).toBe("ignore");

    expect(resolveFormCloseDecision({
      source: "backdrop",
      isDirty: true,
      isSaving: false,
    })).toBe("ignore");
  });

  it("blocks every close attempt while a save is in progress", () => {
    expect(resolveFormCloseDecision({
      source: "explicit",
      isDirty: false,
      isSaving: true,
    })).toBe("ignore");

    expect(resolveFormCloseDecision({
      source: "escape",
      isDirty: true,
      isSaving: true,
    })).toBe("ignore");
  });

  it("closes a clean editor from an explicit action or Escape", () => {
    expect(resolveFormCloseDecision({
      source: "explicit",
      isDirty: false,
      isSaving: false,
    })).toBe("close");

    expect(resolveFormCloseDecision({
      source: "escape",
      isDirty: false,
      isSaving: false,
    })).toBe("close");
  });

  it("requires confirmation before discarding dirty form data", () => {
    expect(resolveFormCloseDecision({
      source: "explicit",
      isDirty: true,
      isSaving: false,
    })).toBe("confirm-discard");

    expect(resolveFormCloseDecision({
      source: "escape",
      isDirty: true,
      isSaving: false,
    })).toBe("confirm-discard");
  });

  it("detects changed draft values and recognizes a fully reverted draft", () => {
    const baseline = {
      code: "",
      name: "",
      area: "",
      notes: "",
    };

    expect(hasFormDraftChanged(baseline, { ...baseline })).toBe(false);
    expect(hasFormDraftChanged(baseline, { ...baseline, name: "Petak Utara" })).toBe(true);
    expect(hasFormDraftChanged(baseline, { ...baseline })).toBe(false);
  });
});
