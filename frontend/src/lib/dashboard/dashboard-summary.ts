import type {
  CropCycle,
  CropCycleStatus,
  CultivationActivity,
  CultivationActivityStatus,
  HarvestBatch,
  HarvestQuantityUnit,
  Land,
} from "@/lib/api/contracts";

export const CROP_CYCLE_STATUS = {
  planned: 1,
  inProgress: 2,
  completed: 3,
  cancelled: 4,
} as const satisfies Record<string, CropCycleStatus>;

export const ACTIVITY_STATUS = {
  planned: 1,
  inProgress: 2,
  completed: 3,
  cancelled: 4,
} as const satisfies Record<string, CultivationActivityStatus>;

export type OrganizationDashboardSummary = {
  activeLandCount: number;
  activePlotCount: number;
  activeAreaHectares: number;
  inProgressCycleCount: number;
  plannedCycleCount: number;
};

export type CycleStatusBreakdown = {
  status: CropCycleStatus;
  label: string;
  count: number;
};

export type HarvestSummary = {
  confirmedBatchCount: number;
  netQuantity: number;
  availableQuantity: number;
  quantityUnit: HarvestQuantityUnit | null;
};

const cropCycleLabels: Record<CropCycleStatus, string> = {
  1: "Direncanakan",
  2: "Berjalan",
  3: "Selesai",
  4: "Dibatalkan",
};

const activityLabels: Record<CultivationActivityStatus, string> = {
  1: "Direncanakan",
  2: "Berjalan",
  3: "Selesai",
  4: "Dibatalkan",
};

const quantityUnitLabels: Record<HarvestQuantityUnit, string> = {
  1: "kg",
  2: "ton",
  3: "kuintal",
  4: "buah",
  5: "tandan",
  6: "karung",
  7: "peti",
  8: "liter",
};

export function summarizeOrganizationDashboard(
  lands: Land[],
  cropCycles: CropCycle[],
): OrganizationDashboardSummary {
  const activeLands = lands.filter((land) => land.isActive);

  return {
    activeLandCount: activeLands.length,
    activePlotCount: activeLands.reduce(
      (total, land) => total + land.plots.filter((plot) => plot.isActive).length,
      0,
    ),
    activeAreaHectares: activeLands.reduce(
      (total, land) => total + land.totalAreaInSquareMeters,
      0,
    ) / 10_000,
    inProgressCycleCount: cropCycles.filter(
      (cycle) => cycle.status === CROP_CYCLE_STATUS.inProgress,
    ).length,
    plannedCycleCount: cropCycles.filter(
      (cycle) => cycle.status === CROP_CYCLE_STATUS.planned,
    ).length,
  };
}

export function selectDefaultCropCycle(
  cropCycles: CropCycle[],
): CropCycle | null {
  const priority: Record<CropCycleStatus, number> = {
    2: 0,
    1: 1,
    3: 2,
    4: 3,
  };

  return [...cropCycles].sort((left, right) => {
    const priorityDifference = priority[left.status] - priority[right.status];

    if (priorityDifference !== 0) {
      return priorityDifference;
    }

    return right.plannedStartDate.localeCompare(left.plannedStartDate);
  })[0] ?? null;
}

export function buildCycleStatusBreakdown(
  cropCycles: CropCycle[],
): CycleStatusBreakdown[] {
  return ([1, 2, 3, 4] as CropCycleStatus[]).map((status) => ({
    status,
    label: cropCycleLabels[status],
    count: cropCycles.filter((cycle) => cycle.status === status).length,
  }));
}

export function calculateScheduleProgress(
  cropCycle: CropCycle,
  today: Date = new Date(),
): number {
  if (cropCycle.status === CROP_CYCLE_STATUS.completed) {
    return 100;
  }

  if (cropCycle.status === CROP_CYCLE_STATUS.planned) {
    return 0;
  }

  const startDate = new Date(`${cropCycle.actualStartDate ?? cropCycle.plannedStartDate}T00:00:00`);
  const endDate = new Date(`${cropCycle.actualHarvestDate ?? cropCycle.expectedHarvestDate}T00:00:00`);
  const elapsed = today.getTime() - startDate.getTime();
  const duration = endDate.getTime() - startDate.getTime();

  if (!Number.isFinite(duration) || duration <= 0) {
    return 0;
  }

  return Math.min(100, Math.max(0, Math.round((elapsed / duration) * 100)));
}

export function sortActivitiesForAgenda(
  activities: CultivationActivity[],
): CultivationActivity[] {
  const statusPriority: Record<CultivationActivityStatus, number> = {
    2: 0,
    1: 1,
    3: 2,
    4: 3,
  };

  return [...activities].sort((left, right) => {
    const priorityDifference = statusPriority[left.status] - statusPriority[right.status];

    if (priorityDifference !== 0) {
      return priorityDifference;
    }

    return left.plannedDate.localeCompare(right.plannedDate);
  });
}

export function summarizeHarvests(
  harvestBatches: HarvestBatch[],
): HarvestSummary {
  const confirmedBatches = harvestBatches.filter((batch) => batch.status === 2);
  const quantityUnit = confirmedBatches[0]?.quantityUnit ?? null;
  const compatibleBatches = confirmedBatches.filter(
    (batch) => batch.quantityUnit === quantityUnit,
  );

  return {
    confirmedBatchCount: confirmedBatches.length,
    netQuantity: compatibleBatches.reduce(
      (total, batch) => total + batch.netQuantity,
      0,
    ),
    availableQuantity: compatibleBatches.reduce(
      (total, batch) => total + batch.availableQuantity,
      0,
    ),
    quantityUnit,
  };
}

export function getCropCycleStatusLabel(status: CropCycleStatus): string {
  return cropCycleLabels[status];
}

export function getActivityStatusLabel(
  status: CultivationActivityStatus,
): string {
  return activityLabels[status];
}

export function getHarvestQuantityUnitLabel(
  unit: HarvestQuantityUnit | null,
): string {
  return unit ? quantityUnitLabels[unit] : "unit";
}

export function formatCurrency(value: number): string {
  return new Intl.NumberFormat("id-ID", {
    style: "currency",
    currency: "IDR",
    maximumFractionDigits: 0,
  }).format(value);
}

export function formatDecimal(value: number, maximumFractionDigits = 2): string {
  return new Intl.NumberFormat("id-ID", {
    maximumFractionDigits,
  }).format(value);
}

export function formatDate(value: string | null): string {
  if (!value) {
    return "—";
  }

  return new Intl.DateTimeFormat("id-ID", {
    day: "2-digit",
    month: "short",
    year: "numeric",
  }).format(new Date(`${value}T00:00:00`));
}
