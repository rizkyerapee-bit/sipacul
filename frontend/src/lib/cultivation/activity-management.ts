import type {
  CropCycle,
  CultivationActivity,
  CultivationActivityResource,
  CultivationActivityStatus,
  CultivationActivityType,
  CultivationResourceType,
  CultivationSop,
  SopComplianceStatus,
} from "@/lib/api/contracts";

export type ActivityStatusFilter = "all" | CultivationActivityStatus;
export type ActivityTypeFilter = "all" | CultivationActivityType;

export function getCultivationActivitiesPath(
  cropCycleId?: string | null,
): string {
  const normalizedCycleId = cropCycleId?.trim();
  return normalizedCycleId
    ? `/cultivation/activities?cropCycleId=${encodeURIComponent(normalizedCycleId)}`
    : "/cultivation/activities";
}

export function selectPreferredCropCycle(
  cycles: CropCycle[],
  requestedCropCycleId?: string | null,
): CropCycle | null {
  const requestedCycleId = requestedCropCycleId?.trim();
  if (requestedCycleId) {
    const requestedCycle = cycles.find((cycle) => cycle.id === requestedCycleId);
    if (requestedCycle) {
      return requestedCycle;
    }
  }

  return cycles.find((cycle) => cycle.status === 2)
    ?? cycles.find((cycle) => cycle.status === 1)
    ?? cycles[0]
    ?? null;
}

export type ActivityDraft = {
  code: string;
  name: string;
  activityType: CultivationActivityType;
  plannedDate: string;
  cultivationSopStepId: string;
  notes: string;
};

export type ResourceDraft = {
  resourceType: CultivationResourceType;
  description: string;
  quantity: string;
  unit: string;
  unitCost: string;
  notes: string;
};

export const activityStatusLabels: Record<CultivationActivityStatus, string> = {
  1: "Rencana",
  2: "Berjalan",
  3: "Selesai",
  4: "Dibatalkan",
};

export const activityTypeLabels: Record<CultivationActivityType, string> = {
  1: "Pengolahan lahan",
  2: "Persiapan benih",
  3: "Penanaman",
  4: "Penyiraman",
  5: "Pemupukan",
  6: "Penyiangan",
  7: "Pengendalian hama & penyakit",
  8: "Perawatan tanaman",
  9: "Pemantauan",
  10: "Lainnya",
};

export const resourceTypeLabels: Record<CultivationResourceType, string> = {
  1: "Bahan",
  2: "Tenaga kerja",
  3: "Alat",
  4: "Jasa",
  5: "Lainnya",
};

export const complianceLabels: Record<SopComplianceStatus, string> = {
  1: "Tidak berlaku",
  2: "Belum dinilai",
  3: "Sesuai SOP",
  4: "Menyimpang dari SOP",
};

const currencyFormatter = new Intl.NumberFormat("id-ID", {
  style: "currency",
  currency: "IDR",
  maximumFractionDigits: 0,
});

const numberFormatter = new Intl.NumberFormat("id-ID", {
  maximumFractionDigits: 4,
});

const dateFormatter = new Intl.DateTimeFormat("id-ID", {
  day: "2-digit",
  month: "short",
  year: "numeric",
  timeZone: "UTC",
});

export function formatCurrency(value: number): string {
  return currencyFormatter.format(value);
}

export function formatQuantity(value: number, unit: string): string {
  return `${numberFormatter.format(value)} ${unit}`;
}

export function formatActivityDate(value: string | null): string {
  if (!value) {
    return "Belum dicatat";
  }

  const parsed = new Date(`${value}T00:00:00Z`);
  return Number.isNaN(parsed.getTime()) ? value : dateFormatter.format(parsed);
}

export function parseDecimal(value: string, allowZero = false): number | null {
  const normalized = value.trim().replace(/\s/g, "").replace(",", ".");
  if (!normalized) {
    return null;
  }

  const parsed = Number(normalized);
  if (!Number.isFinite(parsed)) {
    return null;
  }

  return allowZero ? (parsed >= 0 ? parsed : null) : (parsed > 0 ? parsed : null);
}

export function activityDraftFrom(
  activity: CultivationActivity | null,
): ActivityDraft {
  return {
    code: activity?.code ?? "",
    name: activity?.name ?? "",
    activityType: activity?.activityType ?? 9,
    plannedDate: activity?.plannedDate ?? "",
    cultivationSopStepId: activity?.cultivationSopStepId ?? "",
    notes: activity?.notes ?? "",
  };
}

export function resourceDraftFrom(
  resource: CultivationActivityResource | null,
): ResourceDraft {
  return {
    resourceType: resource?.resourceType ?? 1,
    description: resource?.description ?? "",
    quantity: resource ? String(resource.quantity) : "",
    unit: resource?.unit ?? "",
    unitCost: resource ? String(resource.unitCost) : "",
    notes: resource?.notes ?? "",
  };
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
  if (!normalized) {
    return `${label} wajib diisi.`;
  }

  return normalized.length > maximumLength
    ? `${label} maksimal ${maximumLength} karakter.`
    : null;
}

export function validateActivityDraft(
  draft: ActivityDraft,
  isCreate: boolean,
  cycle: CropCycle,
  cultivationSops: CultivationSop[],
): string[] {
  const errors = [
    isCreate ? requiredText(draft.code, "Kode aktivitas", 40) : null,
    requiredText(draft.name, "Nama aktivitas", 150),
    draft.notes.trim().length > 1000 ? "Catatan maksimal 1000 karakter." : null,
  ].filter((error): error is string => Boolean(error));

  if (isCreate && draft.code.trim()
    && !/^[A-Za-z0-9_-]+$/.test(draft.code.trim())) {
    errors.push("Kode aktivitas hanya boleh berisi huruf, angka, tanda hubung, dan garis bawah.");
  }

  if (draft.activityType < 1 || draft.activityType > 10) {
    errors.push("Jenis aktivitas wajib dipilih.");
  }

  if (!validDate(draft.plannedDate)) {
    errors.push("Tanggal rencana wajib diisi.");
  } else if (draft.plannedDate > cycle.expectedHarvestDate) {
    errors.push("Tanggal aktivitas tidak boleh setelah perkiraan panen siklus.");
  }

  if (draft.cultivationSopStepId) {
    const sop = cultivationSops.find((item) => item.id === cycle.cultivationSopId);
    const step = sop?.steps.find((item) => item.id === draft.cultivationSopStepId);
    if (!sop || !step) {
      errors.push("Langkah SOP tidak tersedia untuk siklus ini.");
    } else if (!sop.isActive && isCreate) {
      errors.push("SOP budidaya sudah tidak aktif.");
    }
  }

  return errors;
}

export function validateResourceDraft(draft: ResourceDraft): string[] {
  const errors = [
    requiredText(draft.description, "Deskripsi sumber daya", 250),
    requiredText(draft.unit, "Satuan", 50),
    draft.notes.trim().length > 500 ? "Catatan sumber daya maksimal 500 karakter." : null,
  ].filter((error): error is string => Boolean(error));

  if (draft.resourceType < 1 || draft.resourceType > 5) {
    errors.push("Kategori sumber daya wajib dipilih.");
  }
  if (parseDecimal(draft.quantity) === null) {
    errors.push("Jumlah harus lebih besar dari nol.");
  }
  if (parseDecimal(draft.unitCost, true) === null) {
    errors.push("Biaya satuan tidak boleh negatif.");
  }

  return errors;
}

export function filterActivities(
  activities: CultivationActivity[],
  query: string,
  status: ActivityStatusFilter,
  activityType: ActivityTypeFilter,
): CultivationActivity[] {
  const normalizedQuery = query.trim().toLocaleLowerCase("id-ID");

  return activities
    .filter((activity) => status === "all" || activity.status === status)
    .filter((activity) => activityType === "all" || activity.activityType === activityType)
    .filter((activity) => {
      if (!normalizedQuery) {
        return true;
      }

      return [
        activity.code,
        activity.name,
        activityTypeLabels[activity.activityType],
        activity.sopStepNameSnapshot ?? "",
      ].some((value) => value.toLocaleLowerCase("id-ID").includes(normalizedQuery));
    })
    .sort((left, right) => {
      const statusOrder = left.status - right.status;
      return statusOrder !== 0
        ? statusOrder
        : left.plannedDate.localeCompare(right.plannedDate);
    });
}

export function optionalActivityText(value: string): string | null {
  const normalized = value.trim();
  return normalized || null;
}
