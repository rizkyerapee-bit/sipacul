"use client";

import { useEffect, useState } from "react";
import type { FormEvent } from "react";
import {
  ApiError,
  createSeasonReview,
  finalizeSeasonReview,
  getSeasonReviewByCropCycle,
  updateSeasonReview,
} from "@/lib/api/client";
import type { SeasonReview, UpdateSeasonReviewRequest } from "@/lib/api/contracts";
import { formatSeasonDate } from "@/lib/evaluations/season-history";
import { seasonReviewErrorMessage, seasonReviewStatusLabels } from "@/lib/evaluations/season-review";
import styles from "./season-review-panel.module.css";

type Props = {
  organizationId: string;
  cropCycleId: string;
  isReadyForReview: boolean;
  canRead: boolean;
  canWrite: boolean;
};

type FormState = UpdateSeasonReviewRequest;

const today = () => new Date().toISOString().slice(0, 10);
const emptyForm = (): FormState => ({
  reviewDate: today(),
  findings: "",
  lessonsLearned: "",
  nextSeasonRecommendations: "",
});

function toForm(review: SeasonReview): FormState {
  return {
    reviewDate: review.reviewDate,
    findings: review.findings,
    lessonsLearned: review.lessonsLearned,
    nextSeasonRecommendations: review.nextSeasonRecommendations,
  };
}

export function SeasonReviewPanel({ organizationId, cropCycleId, isReadyForReview, canRead, canWrite }: Props) {
  const [review, setReview] = useState<SeasonReview | null>(null);
  const [form, setForm] = useState<FormState>(emptyForm);
  const [isLoading, setIsLoading] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const isFinal = review?.status === 2;

  useEffect(() => {
    let cancelled = false;
    async function load() {
      setReview(null);
      setForm(emptyForm());
      setError(null);
      if (!canRead) return;
      setIsLoading(true);
      try {
        const result = await getSeasonReviewByCropCycle(organizationId, cropCycleId);
        if (!cancelled) { setReview(result); setForm(toForm(result)); }
      } catch (caught) {
        if (!cancelled && caught instanceof ApiError && caught.status === 404) return;
        if (!cancelled) setError(seasonReviewErrorMessage(caught));
      } finally {
        if (!cancelled) setIsLoading(false);
      }
    }
    void load();
    return () => { cancelled = true; };
  }, [canRead, cropCycleId, organizationId]);

  function setField<K extends keyof FormState>(field: K, value: FormState[K]) {
    setForm((current) => ({ ...current, [field]: value }));
  }

  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!canWrite || isFinal) return;
    setIsSaving(true); setError(null);
    try {
      const result = review
        ? await updateSeasonReview(organizationId, review.id, form)
        : await createSeasonReview(organizationId, { cropCycleId, ...form });
      setReview(result); setForm(toForm(result));
    } catch (caught) { setError(seasonReviewErrorMessage(caught)); }
    finally { setIsSaving(false); }
  }

  async function finalize() {
    if (!review || !canWrite || isFinal) return;
    if (!window.confirm("Finalisasi evaluasi? Isi tidak dapat diubah setelah menjadi final.")) return;
    setIsSaving(true); setError(null);
    try {
      const result = await finalizeSeasonReview(organizationId, review.id);
      setReview(result); setForm(toForm(result));
    } catch (caught) { setError(seasonReviewErrorMessage(caught)); }
    finally { setIsSaving(false); }
  }

  if (!canRead) {
    return <section className={styles.panel}><h3>Catatan evaluasi musim</h3><p className={styles.notice}>Memerlukan izin <strong>cultivation.read</strong>.</p></section>;
  }
  if (!isReadyForReview) {
    return <section className={styles.panel}><h3>Catatan evaluasi musim</h3><p className={styles.notice}>Catatan dapat dibuat setelah musim selesai atau dibatalkan.</p></section>;
  }
  if (isLoading) {
    return <section className={styles.panel}><h3>Catatan evaluasi musim</h3><p className={styles.notice}>Memuat evaluasi...</p></section>;
  }

  return (
    <section className={styles.panel} aria-label="Catatan evaluasi musim">
      <header className={styles.header}>
        <div><span>Evaluasi manusia</span><h3>Temuan, pelajaran, dan rekomendasi</h3></div>
        <b className={isFinal ? styles.finalBadge : styles.draftBadge}>{seasonReviewStatusLabels[review?.status ?? 1]}</b>
      </header>
      {error && <p className={styles.error} role="alert">{error}</p>}
      {isFinal && review ? (
        <div className={styles.readOnly}>
          <small>Dievaluasi {formatSeasonDate(review.reviewDate)} · Final {review.finalizedAt ? new Intl.DateTimeFormat("id-ID", { dateStyle: "medium", timeStyle: "short" }).format(new Date(review.finalizedAt)) : ""}</small>
          <article><h4>Temuan musim</h4><p>{review.findings}</p></article>
          <article><h4>Pelajaran yang diperoleh</h4><p>{review.lessonsLearned}</p></article>
          <article><h4>Rekomendasi musim berikutnya</h4><p>{review.nextSeasonRecommendations}</p></article>
        </div>
      ) : (
        <form className={styles.form} onSubmit={save}>
          {!review && <p className={styles.notice}>Belum ada catatan. Isi ketiga bagian untuk membuat draf evaluasi.</p>}
          <label><span>Tanggal evaluasi</span><input type="date" required value={form.reviewDate} disabled={!canWrite || isSaving} onChange={(e) => setField("reviewDate", e.target.value)} /></label>
          <label><span>Temuan musim</span><textarea required maxLength={4000} rows={4} value={form.findings} disabled={!canWrite || isSaving} onChange={(e) => setField("findings", e.target.value)} /><small>{form.findings.length}/4000</small></label>
          <label><span>Pelajaran yang diperoleh</span><textarea required maxLength={4000} rows={4} value={form.lessonsLearned} disabled={!canWrite || isSaving} onChange={(e) => setField("lessonsLearned", e.target.value)} /><small>{form.lessonsLearned.length}/4000</small></label>
          <label><span>Rekomendasi musim berikutnya</span><textarea required maxLength={4000} rows={4} value={form.nextSeasonRecommendations} disabled={!canWrite || isSaving} onChange={(e) => setField("nextSeasonRecommendations", e.target.value)} /><small>{form.nextSeasonRecommendations.length}/4000</small></label>
          <footer>
            {!canWrite && <span>Memerlukan izin cultivation.write untuk mengubah evaluasi.</span>}
            <button type="submit" disabled={!canWrite || isSaving}>{isSaving ? "Menyimpan..." : review ? "Simpan perubahan" : "Simpan draf"}</button>
            {review && <button className={styles.finalizeButton} type="button" disabled={!canWrite || isSaving} onClick={() => void finalize()}>Finalisasi</button>}
          </footer>
        </form>
      )}
    </section>
  );
}
