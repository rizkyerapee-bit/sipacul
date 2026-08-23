import type { SeasonEvaluation } from "@/lib/api/contracts";

export const minimumComparedSeasons = 2;
export const maximumComparedSeasons = 4;

export type ComparisonDirection =
  | "increase"
  | "decrease"
  | "unchanged"
  | "unavailable";

export type ComparisonValue = {
  value: number | null;
  deltaFromBaseline: number | null;
  direction: ComparisonDirection;
};

export type SeasonComparisonColumn = {
  cropCycleId: string;
  cropCycleCode: string;
  cropCycleName: string;
  commodityId: string;
  commodityName: string;
  landPlotId: string;
  landPlotName: string;
  comparisonDate: string;
};

export type SeasonComparisonMetricKey =
  | "harvestVarianceDays"
  | "issueActivityCount"
  | "activityCompletionPercentage"
  | "sopCompliancePercentage"
  | "confirmedHarvestBatchCount"
  | "recognizedRevenue"
  | "outstandingReceivable"
  | "totalCultivationCost"
  | "netProfit"
  | "profitMarginPercentage"
  | "capitalFundingGap"
  | "criticalAttentionCount"
  | "warningAttentionCount";

export type SeasonComparisonRow = {
  key: SeasonComparisonMetricKey;
  label: string;
  unit: "days" | "count" | "percentage" | "currency";
  values: ComparisonValue[];
};

export type SeasonComparison = {
  columns: SeasonComparisonColumn[];
  rows: SeasonComparisonRow[];
  sameCommodity: boolean;
  sameLandPlot: boolean;
};

export type ComparisonSelectionChange = {
  selectedIds: string[];
  limitReached: boolean;
};

const metricDefinitions: ReadonlyArray<{
  key: SeasonComparisonMetricKey;
  label: string;
  unit: SeasonComparisonRow["unit"];
}> = [
  { key: "harvestVarianceDays", label: "Varians waktu panen", unit: "days" },
  { key: "issueActivityCount", label: "Aktivitas dengan masalah", unit: "count" },
  { key: "activityCompletionPercentage", label: "Penyelesaian aktivitas", unit: "percentage" },
  { key: "sopCompliancePercentage", label: "Kepatuhan SOP", unit: "percentage" },
  { key: "confirmedHarvestBatchCount", label: "Batch panen terkonfirmasi", unit: "count" },
  { key: "recognizedRevenue", label: "Pendapatan diakui", unit: "currency" },
  { key: "outstandingReceivable", label: "Piutang tersisa", unit: "currency" },
  { key: "totalCultivationCost", label: "Biaya budidaya", unit: "currency" },
  { key: "netProfit", label: "Laba atau rugi bersih", unit: "currency" },
  { key: "profitMarginPercentage", label: "Margin laba", unit: "percentage" },
  { key: "capitalFundingGap", label: "Kesenjangan pendanaan", unit: "currency" },
  { key: "criticalAttentionCount", label: "Indikator kritis", unit: "count" },
  { key: "warningAttentionCount", label: "Indikator perhatian", unit: "count" },
];

function comparisonDate(season: SeasonEvaluation): string {
  return season.actualHarvestDate ?? season.expectedHarvestDate;
}

function compareChronologically(left: SeasonEvaluation, right: SeasonEvaluation): number {
  const dateOrder = comparisonDate(left).localeCompare(comparisonDate(right));
  return dateOrder || left.cropCycleId.localeCompare(right.cropCycleId);
}

function numericValue(
  season: SeasonEvaluation,
  key: SeasonComparisonMetricKey,
): number | null {
  return season[key];
}

function valueAgainstBaseline(value: number | null, baseline: number | null): ComparisonValue {
  if (value === null || baseline === null) {
    return { value, deltaFromBaseline: null, direction: "unavailable" };
  }
  const deltaFromBaseline = value - baseline;
  return {
    value,
    deltaFromBaseline,
    direction:
      deltaFromBaseline > 0
        ? "increase"
        : deltaFromBaseline < 0
          ? "decrease"
          : "unchanged",
  };
}

export function updateComparisonSelection(
  selectedIds: readonly string[],
  cropCycleId: string,
  visibleSeasons: readonly SeasonEvaluation[],
): ComparisonSelectionChange {
  const eligibleIds = new Set(
    visibleSeasons
      .filter((season) => season.isReadyForReview)
      .map((season) => season.cropCycleId),
  );
  const normalized = [...new Set(selectedIds)].filter((id) => eligibleIds.has(id));
  if (!eligibleIds.has(cropCycleId)) {
    return { selectedIds: normalized, limitReached: false };
  }
  if (normalized.includes(cropCycleId)) {
    return {
      selectedIds: normalized.filter((id) => id !== cropCycleId),
      limitReached: false,
    };
  }
  if (normalized.length >= maximumComparedSeasons) {
    return { selectedIds: normalized, limitReached: true };
  }
  return { selectedIds: [...normalized, cropCycleId], limitReached: false };
}

export function buildSeasonComparison(
  seasons: readonly SeasonEvaluation[],
  selectedIds: readonly string[],
): SeasonComparison | null {
  const selected = [...new Map(
    seasons
      .filter((season) => season.isReadyForReview && selectedIds.includes(season.cropCycleId))
      .map((season) => [season.cropCycleId, season]),
  ).values()].sort(compareChronologically);

  if (selected.length < minimumComparedSeasons || selected.length > maximumComparedSeasons) {
    return null;
  }

  return {
    columns: selected.map((season) => ({
      cropCycleId: season.cropCycleId,
      cropCycleCode: season.cropCycleCode,
      cropCycleName: season.cropCycleName,
      commodityId: season.commodityId,
      commodityName: season.commodityName,
      landPlotId: season.landPlotId,
      landPlotName: season.landPlotName,
      comparisonDate: comparisonDate(season),
    })),
    rows: metricDefinitions.map((definition) => {
      const baseline = numericValue(selected[0], definition.key);
      return {
        ...definition,
        values: selected.map((season) =>
          valueAgainstBaseline(numericValue(season, definition.key), baseline),
        ),
      };
    }),
    sameCommodity: new Set(selected.map((season) => season.commodityId)).size === 1,
    sameLandPlot: new Set(selected.map((season) => season.landPlotId)).size === 1,
  };
}
