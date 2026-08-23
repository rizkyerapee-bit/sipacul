import { ApiError } from "@/lib/api/client";
import type { SeasonReviewStatus } from "@/lib/api/contracts";

export const seasonReviewStatusLabels: Record<SeasonReviewStatus, string> = {
  1: "Draf",
  2: "Final",
};

export function seasonReviewErrorMessage(error: unknown): string {
  if (!(error instanceof ApiError)) {
    return error instanceof Error ? error.message : "Evaluasi musim tidak dapat diproses.";
  }

  const messages: Record<string, string> = {
    "SeasonReviews.Validation": "Isi evaluasi belum valid. Lengkapi seluruh bagian dan periksa tanggal.",
    "SeasonReviews.CropCycleNotFound": "Siklus budidaya tidak ditemukan pada organisasi aktif.",
    "SeasonReviews.CropCycleNotTerminal": "Evaluasi hanya dapat dibuat setelah musim selesai atau dibatalkan.",
    "SeasonReviews.AlreadyExists": "Evaluasi untuk musim ini sudah tersedia.",
    "SeasonReviews.NotFound": "Evaluasi musim belum tersedia.",
    "SeasonReviews.InvalidStatusTransition": "Evaluasi final tidak dapat diubah atau difinalisasi ulang.",
  };

  return messages[error.problem?.code ?? ""] ?? error.message;
}

export type SeasonReviewViewState = "unavailable" | "loading" | "empty" | "draft" | "final";

export function getSeasonReviewViewState(
  canRead: boolean,
  isReadyForReview: boolean,
  isLoading: boolean,
  status: SeasonReviewStatus | null,
): SeasonReviewViewState {
  if (!canRead || !isReadyForReview) return "unavailable";
  if (isLoading) return "loading";
  if (status === null) return "empty";
  return status === 2 ? "final" : "draft";
}
