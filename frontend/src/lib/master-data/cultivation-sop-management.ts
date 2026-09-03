import type {
  AddCultivationSopStepRequest,
  CreateCultivationSopRequest,
  CultivationSop,
  CultivationSopStep,
  UpdateCultivationSopRequest,
} from "@/lib/api/contracts";

export type CultivationSopStatusFilter = "all" | "active" | "inactive";

export type CultivationSopDraft = {
  commodityId: string;
  name: string;
  description: string;
};

export type CultivationSopStepDraft = {
  name: string;
  description: string;
  plannedDayOffset: string;
  estimatedDurationDays: string;
  isRequired: boolean;
};

function requiredText(
  value: string,
  label: string,
  maximum: number,
): string | null {
  const normalized = value.trim();
  if (!normalized) {
    return `${label} wajib diisi.`;
  }
  if (normalized.length > maximum) {
    return `${label} maksimal ${maximum} karakter.`;
  }
  return null;
}

function optionalLengthError(
  value: string,
  label: string,
  maximum: number,
): string | null {
  return value.trim().length > maximum
    ? `${label} maksimal ${maximum} karakter.`
    : null;
}

function integerError(
  value: string,
  label: string,
  minimum: number,
  maximum: number,
): string | null {
  const normalized = value.trim();
  if (!/^-?\d+$/.test(normalized)) {
    return `${label} harus berupa bilangan bulat.`;
  }

  const number = Number(normalized);
  if (!Number.isSafeInteger(number) || number < minimum || number > maximum) {
    return `${label} harus berada di antara ${minimum} dan ${maximum}.`;
  }

  return null;
}

export function cultivationSopDraftFrom(
  cultivationSop: CultivationSop | null = null,
): CultivationSopDraft {
  return {
    commodityId: cultivationSop?.commodityId ?? "",
    name: cultivationSop?.name ?? "",
    description: cultivationSop?.description ?? "",
  };
}

export function cultivationSopStepDraftFrom(
  step: CultivationSopStep | null = null,
): CultivationSopStepDraft {
  return {
    name: step?.name ?? "",
    description: step?.description ?? "",
    plannedDayOffset: String(step?.plannedDayOffset ?? 0),
    estimatedDurationDays: String(step?.estimatedDurationDays ?? 1),
    isRequired: step?.isRequired ?? true,
  };
}

export function validateCultivationSopDraft(
  draft: CultivationSopDraft,
): string[] {
  return [
    draft.commodityId ? null : "Komoditas wajib dipilih.",
    requiredText(draft.name, "Nama SOP", 150),
    optionalLengthError(draft.description, "Deskripsi SOP", 1000),
  ].filter((error): error is string => Boolean(error));
}

export function validateCultivationSopStepDraft(
  draft: CultivationSopStepDraft,
): string[] {
  return [
    requiredText(draft.name, "Nama tahapan", 150),
    optionalLengthError(draft.description, "Deskripsi tahapan", 1000),
    integerError(draft.plannedDayOffset, "Offset hari rencana", -365, 3650),
    integerError(draft.estimatedDurationDays, "Estimasi durasi", 1, 365),
  ].filter((error): error is string => Boolean(error));
}

function optionalText(value: string): string | null {
  const normalized = value.trim();
  return normalized || null;
}

export function toCreateCultivationSopRequest(
  draft: CultivationSopDraft,
): CreateCultivationSopRequest {
  return {
    commodityId: draft.commodityId,
    name: draft.name.trim(),
    description: optionalText(draft.description),
  };
}

export function toUpdateCultivationSopRequest(
  draft: CultivationSopDraft,
): UpdateCultivationSopRequest {
  return {
    name: draft.name.trim(),
    description: optionalText(draft.description),
  };
}

export function toCultivationSopStepRequest(
  draft: CultivationSopStepDraft,
): AddCultivationSopStepRequest {
  return {
    name: draft.name.trim(),
    description: optionalText(draft.description),
    plannedDayOffset: Number(draft.plannedDayOffset),
    estimatedDurationDays: Number(draft.estimatedDurationDays),
    isRequired: draft.isRequired,
  };
}

export function sortCultivationSopSteps(
  steps: CultivationSopStep[],
): CultivationSopStep[] {
  return [...steps].sort((left, right) =>
    left.sequence - right.sequence ||
    left.id.localeCompare(right.id),
  );
}

export function getCultivationSopStepMoveSequence(
  steps: CultivationSopStep[],
  stepId: string,
  direction: "up" | "down",
): number | null {
  const sorted = sortCultivationSopSteps(steps);
  const index = sorted.findIndex((step) => step.id === stepId);
  const targetIndex = direction === "up" ? index - 1 : index + 1;

  if (index < 0 || targetIndex < 0 || targetIndex >= sorted.length) {
    return null;
  }

  return sorted[targetIndex].sequence;
}

export function filterCultivationSops(
  cultivationSops: CultivationSop[],
  query: string,
  commodityId: string,
  status: CultivationSopStatusFilter,
): CultivationSop[] {
  const normalizedQuery = query.trim().toLocaleLowerCase("id-ID");

  return cultivationSops
    .filter((sop) => !commodityId || sop.commodityId === commodityId)
    .filter((sop) => status === "all" || sop.isActive === (status === "active"))
    .filter((sop) => {
      if (!normalizedQuery) {
        return true;
      }

      return [
        sop.name,
        sop.description ?? "",
        ...sop.steps.flatMap((step) => [step.name, step.description ?? ""]),
      ].some((value) =>
        value.toLocaleLowerCase("id-ID").includes(normalizedQuery),
      );
    })
    .sort((left, right) => left.name.localeCompare(right.name, "id-ID"));
}

export function getCultivationSopStatusLabel(isActive: boolean): string {
  return isActive ? "Aktif" : "Nonaktif";
}