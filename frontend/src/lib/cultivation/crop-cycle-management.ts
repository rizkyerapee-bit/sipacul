import type {
  AreaUnit,
  Commodity,
  CropCycle,
  CropCycleStatus,
  CultivationSop,
  Land,
  LandPlot,
} from "@/lib/api/contracts";

export type CropCycleStatusFilter = "all" | CropCycleStatus;

export type CropCycleDraft = {
  code: string;
  name: string;
  commodityId: string;
  cultivationSopId: string;
  landId: string;
  landPlotId: string;
  plantedArea: string;
  areaUnit: AreaUnit;
  plannedStartDate: string;
  expectedHarvestDate: string;
  notes: string;
};

export type CropCycleReferences = {
  commodity: Commodity | null;
  cultivationSop: CultivationSop | null;
  land: Land | null;
  plot: LandPlot | null;
};

const numberFormatter = new Intl.NumberFormat("id-ID", {
  maximumFractionDigits: 2,
});

const dateFormatter = new Intl.DateTimeFormat("id-ID", {
  day: "2-digit",
  month: "short",
  year: "numeric",
  timeZone: "UTC",
});

export const cropCycleStatusLabels: Record<CropCycleStatus, string> = {
  1: "Rencana",
  2: "Berjalan",
  3: "Selesai",
  4: "Dibatalkan",
};

export const areaUnitLabels: Record<AreaUnit, string> = {
  1: "m²",
  2: "ha",
};

export function toSquareMeters(area: number, unit: AreaUnit): number {
  return unit === 2 ? area * 10_000 : area;
}

export function parsePositiveNumber(value: string): number | null {
  const normalized = value.trim().replace(",", ".");
  if (!normalized) {
    return null;
  }

  const parsed = Number(normalized);
  return Number.isFinite(parsed) && parsed > 0 ? parsed : null;
}

export function formatArea(area: number, unit: AreaUnit): string {
  return `${numberFormatter.format(area)} ${areaUnitLabels[unit]}`;
}

export function formatDateOnly(value: string | null): string {
  if (!value) {
    return "Belum dicatat";
  }

  const parsed = new Date(`${value}T00:00:00Z`);
  return Number.isNaN(parsed.getTime()) ? value : dateFormatter.format(parsed);
}

export function getPlannedDurationDays(cycle: CropCycle): number {
  const start = Date.parse(`${cycle.plannedStartDate}T00:00:00Z`);
  const end = Date.parse(`${cycle.expectedHarvestDate}T00:00:00Z`);
  return Math.max(0, Math.round((end - start) / 86_400_000));
}

export function cropCycleDraftFrom(cycle: CropCycle | null): CropCycleDraft {
  return {
    code: cycle?.code ?? "",
    name: cycle?.name ?? "",
    commodityId: cycle?.commodityId ?? "",
    cultivationSopId: cycle?.cultivationSopId ?? "",
    landId: cycle?.landId ?? "",
    landPlotId: cycle?.landPlotId ?? "",
    plantedArea: cycle ? String(cycle.plantedArea) : "",
    areaUnit: cycle?.areaUnit ?? 1,
    plannedStartDate: cycle?.plannedStartDate ?? "",
    expectedHarvestDate: cycle?.expectedHarvestDate ?? "",
    notes: cycle?.notes ?? "",
  };
}

export function getCycleReferences(
  cycle: CropCycle,
  commodities: Commodity[],
  cultivationSops: CultivationSop[],
  lands: Land[],
): CropCycleReferences {
  const land = lands.find((item) => item.id === cycle.landId) ?? null;

  return {
    commodity: commodities.find((item) => item.id === cycle.commodityId) ?? null,
    cultivationSop: cultivationSops.find((item) => item.id === cycle.cultivationSopId) ?? null,
    land,
    plot: land?.plots.find((item) => item.id === cycle.landPlotId) ?? null,
  };
}

export function filterCropCycles(
  cycles: CropCycle[],
  commodities: Commodity[],
  lands: Land[],
  query: string,
  status: CropCycleStatusFilter,
  landId: string,
): CropCycle[] {
  const normalizedQuery = query.trim().toLocaleLowerCase("id-ID");

  return cycles
    .filter((cycle) => status === "all" || cycle.status === status)
    .filter((cycle) => !landId || cycle.landId === landId)
    .filter((cycle) => {
      if (!normalizedQuery) {
        return true;
      }

      const land = lands.find((item) => item.id === cycle.landId);
      const plot = land?.plots.find((item) => item.id === cycle.landPlotId);
      const commodity = commodities.find((item) => item.id === cycle.commodityId);
      return [
        cycle.code,
        cycle.name,
        commodity?.name ?? "",
        land?.name ?? "",
        plot?.name ?? "",
      ].some((value) => value.toLocaleLowerCase("id-ID").includes(normalizedQuery));
    })
    .sort((left, right) => {
      const statusOrder = left.status - right.status;
      return statusOrder !== 0
        ? statusOrder
        : left.plannedStartDate.localeCompare(right.plannedStartDate);
    });
}

function requiredText(
  value: string,
  label: string,
  maximum: number,
): string | null {
  const normalized = value.trim();
  if (!normalized) {
    return `${label} wajib diisi.`;
  }
  return normalized.length > maximum
    ? `${label} maksimal ${maximum} karakter.`
    : null;
}

function validDate(value: string): boolean {
  return /^\d{4}-\d{2}-\d{2}$/.test(value)
    && !Number.isNaN(Date.parse(`${value}T00:00:00Z`));
}

export function validateCropCycleDraft(
  draft: CropCycleDraft,
  isCreate: boolean,
  commodities: Commodity[],
  cultivationSops: CultivationSop[],
  lands: Land[],
): string[] {
  const errors = [
    isCreate ? requiredText(draft.code, "Kode siklus", 40) : null,
    requiredText(draft.name, "Nama siklus", 150),
    draft.notes.trim().length > 1000 ? "Catatan maksimal 1000 karakter." : null,
  ].filter((error): error is string => Boolean(error));

  if (isCreate && draft.code.trim()
    && !/^[A-Za-z0-9_-]+$/.test(draft.code.trim())) {
    errors.push("Kode siklus hanya boleh berisi huruf, angka, tanda hubung, dan garis bawah.");
  }

  const commodity = commodities.find((item) => item.id === draft.commodityId);
  if (!commodity) {
    errors.push("Komoditas wajib dipilih.");
  } else if (isCreate && !commodity.isActive) {
    errors.push("Komoditas yang dipilih sudah tidak aktif.");
  }

  const land = lands.find((item) => item.id === draft.landId);
  if (!land) {
    errors.push("Lahan wajib dipilih.");
  } else if (isCreate && !land.isActive) {
    errors.push("Lahan yang dipilih sudah tidak aktif.");
  }

  const plot = land?.plots.find((item) => item.id === draft.landPlotId);
  if (!plot) {
    errors.push("Petak wajib dipilih.");
  } else if (isCreate && !plot.isActive) {
    errors.push("Petak yang dipilih sudah tidak aktif.");
  }

  if (draft.cultivationSopId) {
    const sop = cultivationSops.find((item) => item.id === draft.cultivationSopId);
    if (!sop) {
      errors.push("SOP budidaya tidak ditemukan.");
    } else {
      if (!sop.isActive) {
        errors.push("SOP budidaya yang dipilih sudah tidak aktif.");
      }
      if (draft.commodityId && sop.commodityId !== draft.commodityId) {
        errors.push("SOP budidaya harus sesuai dengan komoditas yang dipilih.");
      }
    }
  }

  const plantedArea = parsePositiveNumber(draft.plantedArea);
  if (plantedArea === null) {
    errors.push("Luas tanam harus lebih besar dari nol.");
  } else if (plot && toSquareMeters(plantedArea, draft.areaUnit)
    > toSquareMeters(plot.area, plot.areaUnit)) {
    errors.push("Luas tanam tidak boleh melebihi luas petak.");
  }

  if (!validDate(draft.plannedStartDate)) {
    errors.push("Tanggal mulai rencana wajib diisi.");
  }
  if (!validDate(draft.expectedHarvestDate)) {
    errors.push("Perkiraan panen wajib diisi.");
  }
  if (validDate(draft.plannedStartDate)
    && validDate(draft.expectedHarvestDate)
    && draft.expectedHarvestDate <= draft.plannedStartDate) {
    errors.push("Perkiraan panen harus setelah tanggal mulai rencana.");
  }

  return errors;
}

export function optionalText(value: string): string | null {
  const normalized = value.trim();
  return normalized || null;
}
