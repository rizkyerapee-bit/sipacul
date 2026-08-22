import type {
  ProfitabilityOutcome,
  SeasonEvaluation,
  SeasonEvaluationAttention,
  SeasonEvaluationAttentionCode,
  SeasonEvaluationAttentionSeverity,
} from "@/lib/api/contracts";

export const attentionLabels: Record<SeasonEvaluationAttentionCode, string> = {
  1: "Siklus belum berakhir",
  2: "Siklus dibatalkan",
  3: "Mulai terlambat",
  4: "Panen terlambat",
  5: "Aktivitas belum selesai",
  6: "Aktivitas dibatalkan",
  7: "Masalah lapangan tercatat",
  8: "Deviasi SOP tercatat",
  9: "Kepatuhan SOP belum dinilai",
  10: "Belum ada panen terkonfirmasi",
  11: "Piutang belum tertagih",
  12: "Hasil impas",
  13: "Musim merugi",
  14: "Modal belum menutup biaya",
};

export const attentionSeverityLabels: Record<SeasonEvaluationAttentionSeverity, string> = {
  1: "Informasi",
  2: "Perhatian",
  3: "Kritis",
};

export const profitabilityOutcomeLabels: Record<ProfitabilityOutcome, string> = {
  1: "Rugi",
  2: "Impas",
  3: "Untung",
};

const numberFormatter = new Intl.NumberFormat("id-ID", {
  maximumFractionDigits: 2,
});

const currencyFormatter = new Intl.NumberFormat("id-ID", {
  style: "currency",
  currency: "IDR",
  maximumFractionDigits: 0,
});

export function formatSeasonCurrency(value: number): string {
  return currencyFormatter.format(value);
}

export function formatSeasonPercentage(value: number | null): string {
  return value === null ? "Belum tersedia" : `${numberFormatter.format(value)}%`;
}

export function formatSeasonDate(value: string | null): string {
  if (!value) return "Belum tercatat";
  const [year, month, day] = value.slice(0, 10).split("-").map(Number);
  if (!year || !month || !day) return value;
  return new Intl.DateTimeFormat("id-ID", {
    day: "numeric",
    month: "short",
    year: "numeric",
  }).format(new Date(year, month - 1, day));
}

export function formatVarianceDays(value: number | null): string {
  if (value === null) return "Belum tersedia";
  if (value === 0) return "Sesuai rencana";
  return value > 0
    ? `${numberFormatter.format(value)} hari terlambat`
    : `${numberFormatter.format(Math.abs(value))} hari lebih awal`;
}

export function attentionValueLabel(attention: SeasonEvaluationAttention): string | null {
  if (attention.value === null) return null;
  if (attention.code === 3 || attention.code === 4) {
    return `${numberFormatter.format(attention.value)} hari`;
  }
  if (attention.code >= 5 && attention.code <= 9) {
    return `${numberFormatter.format(attention.value)} aktivitas`;
  }
  if (attention.code === 11 || attention.code === 13 || attention.code === 14) {
    return formatSeasonCurrency(attention.value);
  }
  return numberFormatter.format(attention.value);
}

export type SeasonPageSummary = {
  visibleSeasonCount: number;
  reviewReadyCount: number;
  requiresAttentionCount: number;
  criticalAttentionCount: number;
  outstandingReceivable: number;
};

export function summarizeSeasonPage(seasons: SeasonEvaluation[]): SeasonPageSummary {
  return seasons.reduce<SeasonPageSummary>((summary, season) => ({
    visibleSeasonCount: summary.visibleSeasonCount + 1,
    reviewReadyCount: summary.reviewReadyCount + (season.isReadyForReview ? 1 : 0),
    requiresAttentionCount: summary.requiresAttentionCount + (season.requiresAttention ? 1 : 0),
    criticalAttentionCount: summary.criticalAttentionCount + season.criticalAttentionCount,
    outstandingReceivable: summary.outstandingReceivable + season.outstandingReceivable,
  }), {
    visibleSeasonCount: 0,
    reviewReadyCount: 0,
    requiresAttentionCount: 0,
    criticalAttentionCount: 0,
    outstandingReceivable: 0,
  });
}
