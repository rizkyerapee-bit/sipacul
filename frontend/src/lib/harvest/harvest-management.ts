import type {
  CropCycle,
  HarvestBatch,
  HarvestBatchStatus,
  HarvestQuantityUnit,
} from "@/lib/api/contracts";

export type HarvestStatusFilter = "all" | HarvestBatchStatus;
export type HarvestUnitFilter = "all" | HarvestQuantityUnit;

export type HarvestDraft = {
  code: string;
  harvestDate: string;
  grossQuantity: string;
  rejectedQuantity: string;
  quantityUnit: HarvestQuantityUnit;
  qualityGrade: string;
  storageLocation: string;
  notes: string;
};

export const harvestStatusLabels: Record<HarvestBatchStatus, string> = {
  1: "Draf",
  2: "Dikonfirmasi",
  3: "Dibatalkan",
};

export const harvestUnitLabels: Record<HarvestQuantityUnit, string> = {
  1: "Kilogram",
  2: "Ton",
  3: "Kuintal",
  4: "Buah",
  5: "Tandan",
  6: "Karung",
  7: "Peti",
  8: "Liter",
};

export const harvestUnitSymbols: Record<HarvestQuantityUnit, string> = {
  1: "kg",
  2: "ton",
  3: "kuintal",
  4: "buah",
  5: "tandan",
  6: "karung",
  7: "peti",
  8: "L",
};

const numberFormatter = new Intl.NumberFormat("id-ID", {
  maximumFractionDigits: 4,
});

const dateFormatter = new Intl.DateTimeFormat("id-ID", {
  day: "2-digit",
  month: "short",
  year: "numeric",
  timeZone: "UTC",
});

export function formatHarvestDate(value: string | null): string {
  if (!value) return "Belum dicatat";
  const parsed = new Date(`${value}T00:00:00Z`);
  return Number.isNaN(parsed.getTime()) ? value : dateFormatter.format(parsed);
}

export function formatHarvestQuantity(
  value: number,
  unit: HarvestQuantityUnit,
): string {
  return `${numberFormatter.format(value)} ${harvestUnitSymbols[unit]}`;
}

export function formatPercentage(value: number): string {
  return `${numberFormatter.format(value)}%`;
}

export function parseHarvestNumber(
  value: string,
  allowZero = false,
): number | null {
  const normalized = value.trim().replace(/\s/g, "").replace(",", ".");
  if (!normalized) return null;
  const parsed = Number(normalized);
  if (!Number.isFinite(parsed)) return null;
  return allowZero ? (parsed >= 0 ? parsed : null) : (parsed > 0 ? parsed : null);
}

export function harvestDraftFrom(
  batch: HarvestBatch | null,
  defaultUnit: HarvestQuantityUnit = 1,
): HarvestDraft {
  return {
    code: batch?.code ?? "",
    harvestDate: batch?.harvestDate ?? "",
    grossQuantity: batch ? String(batch.grossQuantity) : "",
    rejectedQuantity: batch ? String(batch.rejectedQuantity) : "0",
    quantityUnit: batch?.quantityUnit ?? defaultUnit,
    qualityGrade: batch?.qualityGrade ?? "",
    storageLocation: batch?.storageLocation ?? "",
    notes: batch?.notes ?? "",
  };
}

export function requiredHarvestUnit(
  batches: HarvestBatch[],
  excludedHarvestBatchId: string | null = null,
): HarvestQuantityUnit | null {
  const active = batches.filter((batch) => batch.status !== 3
    && batch.id !== excludedHarvestBatchId);
  const units = new Set(active.map((batch) => batch.quantityUnit));
  return units.size === 1 ? active[0]?.quantityUnit ?? null : null;
}

function validDate(value: string): boolean {
  return /^\d{4}-\d{2}-\d{2}$/.test(value)
    && !Number.isNaN(Date.parse(`${value}T00:00:00Z`));
}

function requiredText(
  value: string,
  label: string,
  maximumLength: number,
): string | null {
  const normalized = value.trim();
  if (!normalized) return `${label} wajib diisi.`;
  return normalized.length > maximumLength
    ? `${label} maksimal ${maximumLength} karakter.`
    : null;
}

export function validateHarvestDraft(
  draft: HarvestDraft,
  isCreate: boolean,
  cycle: CropCycle,
): string[] {
  const errors = [
    isCreate ? requiredText(draft.code, "Kode batch", 40) : null,
    draft.qualityGrade.trim().length > 100
      ? "Mutu atau grade maksimal 100 karakter."
      : null,
    draft.storageLocation.trim().length > 250
      ? "Lokasi penyimpanan maksimal 250 karakter."
      : null,
    draft.notes.trim().length > 1000
      ? "Catatan maksimal 1000 karakter."
      : null,
  ].filter((error): error is string => Boolean(error));

  if (isCreate && draft.code.trim()
    && !/^[A-Za-z0-9][A-Za-z0-9._-]*$/.test(draft.code.trim())) {
    errors.push("Kode batch harus diawali huruf atau angka dan hanya boleh memakai titik, tanda hubung, atau garis bawah.");
  }

  if (!validDate(draft.harvestDate)) {
    errors.push("Tanggal panen wajib diisi.");
  } else {
    if (cycle.actualStartDate && draft.harvestDate < cycle.actualStartDate) {
      errors.push("Tanggal panen tidak boleh sebelum tanggal mulai aktual siklus.");
    }
    if (cycle.actualHarvestDate && draft.harvestDate > cycle.actualHarvestDate) {
      errors.push("Tanggal panen tidak boleh setelah tanggal panen aktual siklus.");
    }
  }

  const gross = parseHarvestNumber(draft.grossQuantity);
  const rejected = parseHarvestNumber(draft.rejectedQuantity, true);
  if (gross === null) errors.push("Hasil kotor harus lebih besar dari nol.");
  if (rejected === null) errors.push("Hasil ditolak tidak boleh negatif.");
  if (gross !== null && rejected !== null && rejected > gross) {
    errors.push("Hasil ditolak tidak boleh melebihi hasil kotor.");
  }
  if (draft.quantityUnit < 1 || draft.quantityUnit > 8) {
    errors.push("Satuan panen wajib dipilih.");
  }

  return errors;
}

export function filterHarvestBatches(
  batches: HarvestBatch[],
  query: string,
  status: HarvestStatusFilter,
  unit: HarvestUnitFilter,
): HarvestBatch[] {
  const normalizedQuery = query.trim().toLocaleLowerCase("id-ID");

  return batches
    .filter((batch) => status === "all" || batch.status === status)
    .filter((batch) => unit === "all" || batch.quantityUnit === unit)
    .filter((batch) => {
      if (!normalizedQuery) return true;
      return [
        batch.code,
        batch.qualityGrade ?? "",
        batch.storageLocation ?? "",
        batch.notes ?? "",
      ].some((value) => value.toLocaleLowerCase("id-ID").includes(normalizedQuery));
    })
    .sort((left, right) => {
      const dateOrder = right.harvestDate.localeCompare(left.harvestDate);
      return dateOrder !== 0 ? dateOrder : left.code.localeCompare(right.code);
    });
}

export function summarizeHarvest(batches: HarvestBatch[]): {
  batchCount: number;
  confirmedCount: number;
  netQuantity: number;
  availableQuantity: number;
  rejectedQuantity: number;
  unit: HarvestQuantityUnit | null;
  hasMixedUnits: boolean;
} {
  const active = batches.filter((batch) => batch.status !== 3);
  const confirmed = batches.filter((batch) => batch.status === 2);
  const units = new Set(active.map((batch) => batch.quantityUnit));
  const unit = units.size === 1 ? active[0]?.quantityUnit ?? null : null;
  const canTotal = unit !== null;

  return {
    batchCount: batches.length,
    confirmedCount: confirmed.length,
    netQuantity: canTotal
      ? confirmed.reduce((total, batch) => total + batch.netQuantity, 0)
      : 0,
    availableQuantity: canTotal
      ? confirmed.reduce((total, batch) => total + batch.availableQuantity, 0)
      : 0,
    rejectedQuantity: canTotal
      ? active.reduce((total, batch) => total + batch.rejectedQuantity, 0)
      : 0,
    unit,
    hasMixedUnits: units.size > 1,
  };
}

export function optionalHarvestText(value: string): string | null {
  const normalized = value.trim();
  return normalized || null;
}
