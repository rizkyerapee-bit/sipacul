import type { AreaUnit, Land, LandPlot, LandTenureType } from "@/lib/api/contracts";

export type LandStatusFilter = "all" | "active" | "inactive";

export type LandDraft = {
  code: string;
  name: string;
  tenureType: LandTenureType;
  totalArea: string;
  areaUnit: AreaUnit;
  address: string;
  locationDescription: string;
  latitude: string;
  longitude: string;
  notes: string;
};

export type PlotDraft = {
  code: string;
  name: string;
  area: string;
  areaUnit: AreaUnit;
  generalCondition: string;
  notes: string;
};

const areaFormatter = new Intl.NumberFormat("id-ID", { maximumFractionDigits: 2 });

export const tenureLabels: Record<LandTenureType, string> = {
  1: "Milik sendiri",
  2: "Sewa",
  3: "Kelola",
  4: "Kemitraan",
  5: "Lainnya",
};

export const areaUnitLabels: Record<AreaUnit, string> = {
  1: "m²",
  2: "ha",
};

export function toSquareMeters(area: number, unit: AreaUnit): number {
  return unit === 2 ? area * 10_000 : area;
}

export function formatSquareMeters(area: number): string {
  if (Math.abs(area) >= 10_000) {
    return `${areaFormatter.format(area / 10_000)} ha`;
  }

  return `${areaFormatter.format(area)} m²`;
}

export function formatArea(area: number, unit: AreaUnit): string {
  return `${areaFormatter.format(area)} ${areaUnitLabels[unit]}`;
}

export function getAllocationPercentage(land: Land): number {
  if (land.totalAreaInSquareMeters <= 0) {
    return 0;
  }

  return Math.min(100, Math.max(0,
    (land.allocatedPlotAreaInSquareMeters / land.totalAreaInSquareMeters) * 100,
  ));
}

export function getAvailableArea(land: Land): number {
  return Math.max(0, land.totalAreaInSquareMeters - land.allocatedPlotAreaInSquareMeters);
}

export function filterLands(
  lands: Land[],
  query: string,
  status: LandStatusFilter,
): Land[] {
  const normalizedQuery = query.trim().toLocaleLowerCase("id-ID");

  return lands
    .filter((land) => status === "all" || land.isActive === (status === "active"))
    .filter((land) => {
      if (!normalizedQuery) {
        return true;
      }

      return [land.code, land.name, land.address ?? "", land.locationDescription ?? ""]
        .some((value) => value.toLocaleLowerCase("id-ID").includes(normalizedQuery));
    })
    .sort((left, right) => left.name.localeCompare(right.name, "id-ID"));
}

function requiredText(value: string, label: string, maximum: number): string | null {
  const normalized = value.trim();
  if (!normalized) {
    return `${label} wajib diisi.`;
  }
  if (normalized.length > maximum) {
    return `${label} maksimal ${maximum} karakter.`;
  }
  return null;
}

function codeError(value: string, label: string): string | null {
  const required = requiredText(value, label, 30);
  if (required) {
    return required;
  }
  return /^[A-Za-z0-9_-]+$/.test(value.trim())
    ? null
    : `${label} hanya boleh berisi huruf, angka, tanda hubung, dan garis bawah.`;
}

function optionalLengthError(value: string, label: string, maximum: number): string | null {
  return value.trim().length > maximum ? `${label} maksimal ${maximum} karakter.` : null;
}

export function parsePositiveNumber(value: string): number | null {
  const normalized = value.trim().replace(",", ".");
  if (!normalized) {
    return null;
  }
  const number = Number(normalized);
  return Number.isFinite(number) && number > 0 ? number : null;
}

function parseOptionalNumber(value: string): number | null | "invalid" {
  const normalized = value.trim().replace(",", ".");
  if (!normalized) {
    return null;
  }
  const number = Number(normalized);
  return Number.isFinite(number) ? number : "invalid";
}

export function validateLandDraft(
  draft: LandDraft,
  isCreate: boolean,
  allocatedAreaInSquareMeters = 0,
): string[] {
  const errors = [
    isCreate ? codeError(draft.code, "Kode lahan") : null,
    requiredText(draft.name, "Nama lahan", 150),
    optionalLengthError(draft.address, "Alamat", 500),
    optionalLengthError(draft.locationDescription, "Deskripsi lokasi", 500),
    optionalLengthError(draft.notes, "Catatan", 1000),
  ].filter((error): error is string => Boolean(error));

  const area = parsePositiveNumber(draft.totalArea);
  if (area === null) {
    errors.push("Luas lahan harus lebih besar dari nol.");
  } else if (toSquareMeters(area, draft.areaUnit) < allocatedAreaInSquareMeters) {
    errors.push("Luas lahan tidak boleh lebih kecil daripada total luas petak yang sudah dialokasikan.");
  }

  const latitude = parseOptionalNumber(draft.latitude);
  const longitude = parseOptionalNumber(draft.longitude);
  if ((latitude === null) !== (longitude === null)) {
    errors.push("Lintang dan bujur harus diisi berpasangan.");
  } else if (latitude === "invalid" || longitude === "invalid") {
    errors.push("Koordinat harus berupa angka yang valid.");
  } else if (latitude !== null && (latitude < -90 || latitude > 90)) {
    errors.push("Lintang harus berada di antara -90 dan 90.");
  } else if (longitude !== null && (longitude < -180 || longitude > 180)) {
    errors.push("Bujur harus berada di antara -180 dan 180.");
  }

  return errors;
}

export function validatePlotDraft(
  draft: PlotDraft,
  isCreate: boolean,
  land: Land,
  existingPlot: LandPlot | null = null,
): string[] {
  const errors = [
    isCreate ? codeError(draft.code, "Kode petak") : null,
    requiredText(draft.name, "Nama petak", 150),
    optionalLengthError(draft.generalCondition, "Kondisi umum", 500),
    optionalLengthError(draft.notes, "Catatan", 1000),
  ].filter((error): error is string => Boolean(error));
  const area = parsePositiveNumber(draft.area);

  if (area === null) {
    errors.push("Luas petak harus lebih besar dari nol.");
  } else {
    const existingArea = existingPlot
      ? toSquareMeters(existingPlot.area, existingPlot.areaUnit)
      : 0;
    const availableArea = getAvailableArea(land) + existingArea;
    if (toSquareMeters(area, draft.areaUnit) > availableArea) {
      errors.push(`Luas petak melebihi sisa kapasitas ${formatSquareMeters(availableArea)}.`);
    }
  }

  return errors;
}

export function optionalText(value: string): string | null {
  const normalized = value.trim();
  return normalized || null;
}

export function optionalNumber(value: string): number | null {
  const parsed = parseOptionalNumber(value);
  return typeof parsed === "number" ? parsed : null;
}
