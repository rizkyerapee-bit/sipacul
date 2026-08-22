"use client";

import { useEffect, useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { ApiError, getLands, getLandSeasonHistory } from "@/lib/api/client";
import type {
  Land,
  LandSeasonHistory,
  Organization,
  SeasonEvaluation,
  SeasonEvaluationAttentionSeverity,
} from "@/lib/api/contracts";
import { cropCycleStatusLabels } from "@/lib/cultivation/crop-cycle-management";
import {
  attentionLabels,
  attentionSeverityLabels,
  attentionValueLabel,
  formatSeasonCurrency,
  formatSeasonDate,
  formatSeasonPercentage,
  formatVarianceDays,
  profitabilityOutcomeLabels,
  summarizeSeasonPage,
} from "@/lib/evaluations/season-history";
import styles from "./season-history-management.module.css";

type Props = {
  organization: Organization | null;
  organizationId: string | null;
  permissions: string[];
};

type IconName =
  | "calendar"
  | "check"
  | "clock"
  | "field"
  | "finance"
  | "history"
  | "leaf"
  | "next"
  | "previous"
  | "refresh"
  | "stop"
  | "trend"
  | "warning";

const iconPaths: Record<IconName, string> = {
  calendar: "M6 3v3m12-3v3M4 9h16M5 5h14a1 1 0 0 1 1 1v14H4V6a1 1 0 0 1 1-1Z",
  check: "m5 12 4 4L19 6",
  clock: "M12 21a9 9 0 1 0 0-18 9 9 0 0 0 0 18Zm0-13v5l3 2",
  field: "M3 6.5 9 4l6 2.5L21 4v13.5L15 20l-6-2.5L3 20V6.5Zm6-2.5v13.5M15 6.5V20",
  finance: "M4 19V10m6 9V5m6 14v-6m4 6H2",
  history: "M3 12a9 9 0 1 0 3-6.7L3 8m0-5v5h5m4-1v6l4 2",
  leaf: "M12 21v-9m0 2c-4 0-7-2-7-6 4 0 7 2 7 6Zm0-3c4 0 7-2 7-6-4 0-7 2-7 6Z",
  next: "m9 6 6 6-6 6",
  previous: "m15 6-6 6 6 6",
  refresh: "M20 6v5h-5M4 18v-5h5m10-2a7 7 0 0 0-12-4L4 11m16 2-3 4a7 7 0 0 1-12-4",
  stop: "M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10Zm-3.5-6.5 7-7m-7 0 7 7",
  trend: "m4 17 5-5 4 4 7-8m-5 0h5v5",
  warning: "M12 3 2 21h20L12 3Zm0 6v5m0 3h.01",
};

function Icon({ name }: { name: IconName }) {
  return <svg viewBox="0 0 24 24" aria-hidden="true"><path d={iconPaths[name]} /></svg>;
}

function friendlyError(error: unknown): string {
  if (!(error instanceof ApiError)) {
    return error instanceof Error ? error.message : "Histori musim tidak dapat dimuat.";
  }

  const messages: Record<string, string> = {
    "SeasonHistory.Validation": "Filter histori tidak valid. Periksa halaman dan jumlah data per halaman.",
    "SeasonHistory.LandNotFound": "Lahan tidak ditemukan pada organisasi aktif.",
    "SeasonHistory.LandPlotNotFound": "Petak tidak ditemukan pada lahan yang dipilih.",
    "SeasonHistory.SourceDataInvalid": "Sumber data musim belum konsisten. Periksa catatan budidaya, panen, penjualan, biaya, dan modal.",
  };

  return messages[error.problem?.code ?? ""] ?? error.message;
}

function formatGeneratedAt(value: string): string {
  return new Intl.DateTimeFormat("id-ID", {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(new Date(value));
}

function severityClass(severity: SeasonEvaluationAttentionSeverity): string {
  if (severity === 3) return styles.attentionCritical;
  if (severity === 2) return styles.attentionWarning;
  return styles.attentionInformation;
}

function PercentageBar({ value }: { value: number | null }) {
  const width = value === null ? 0 : Math.min(100, Math.max(0, value));
  return (
    <span className={styles.progressTrack} aria-hidden="true">
      <i style={{ width: `${width}%` }} />
    </span>
  );
}

function SeasonDetail({ season }: { season: SeasonEvaluation }) {
  return (
    <article className={styles.detailPanel}>
      <header className={styles.detailHeader}>
        <span className={styles.detailIcon}><Icon name="leaf" /></span>
        <div>
          <span className={styles.eyebrow}>{season.cropCycleCode} · {season.commodityName}</span>
          <h2>{season.cropCycleName}</h2>
          <p>{season.landPlotCode} · {season.landPlotName}</p>
        </div>
        <span className={`${styles.reviewBadge} ${season.isReadyForReview ? styles.reviewReady : styles.reviewPending}`}>
          {season.isReadyForReview ? "Siap dievaluasi" : "Musim berjalan"}
        </span>
      </header>

      <section className={styles.timelineSection} aria-label="Timeline musim">
        <div><span>Rencana mulai</span><strong>{formatSeasonDate(season.plannedStartDate)}</strong><small>{formatVarianceDays(season.startVarianceDays)}</small></div>
        <div><span>Mulai aktual</span><strong>{formatSeasonDate(season.actualStartDate)}</strong><small>{cropCycleStatusLabels[season.cropCycleStatus]}</small></div>
        <div><span>Target panen</span><strong>{formatSeasonDate(season.expectedHarvestDate)}</strong><small>{formatVarianceDays(season.harvestVarianceDays)}</small></div>
        <div><span>Panen aktual</span><strong>{formatSeasonDate(season.actualHarvestDate)}</strong><small>{season.confirmedHarvestBatchCount} batch terkonfirmasi</small></div>
      </section>

      <section className={styles.performanceGrid}>
        <div>
          <span>Aktivitas selesai</span>
          <strong>{formatSeasonPercentage(season.activityCompletionPercentage)}</strong>
          <PercentageBar value={season.activityCompletionPercentage} />
          <small>{season.completedActivityCount} selesai · {season.pendingActivityCount} tertunda · {season.cancelledActivityCount} dibatalkan</small>
        </div>
        <div>
          <span>Kepatuhan SOP</span>
          <strong>{formatSeasonPercentage(season.sopCompliancePercentage)}</strong>
          <PercentageBar value={season.sopCompliancePercentage} />
          <small>{season.sopCompliantActivityCount} sesuai · {season.sopDeviatedActivityCount} deviasi · {season.sopNotEvaluatedActivityCount} belum dinilai</small>
        </div>
        <div>
          <span>Masalah lapangan</span>
          <strong>{season.issueActivityCount}</strong>
          <span className={styles.issueScale}><i className={season.issueActivityCount > 0 ? styles.issueActive : ""} /></span>
          <small>Dihitung dari aktivitas yang memiliki catatan masalah.</small>
        </div>
      </section>

      <section className={styles.financeSection}>
        <header><span><Icon name="finance" /></span><div><small>Rekonsiliasi finansial</small><h3>{profitabilityOutcomeLabels[season.profitabilityOutcome]}</h3></div></header>
        <div className={styles.financeGrid}>
          <div><span>Pendapatan diakui</span><strong>{formatSeasonCurrency(season.recognizedRevenue)}</strong></div>
          <div><span>Sudah tertagih</span><strong>{formatSeasonCurrency(season.collectedRevenue)}</strong></div>
          <div className={season.outstandingReceivable > 0 ? styles.financeWarning : ""}><span>Piutang</span><strong>{formatSeasonCurrency(season.outstandingReceivable)}</strong></div>
          <div><span>Biaya budidaya</span><strong>{formatSeasonCurrency(season.totalCultivationCost)}</strong></div>
          <div className={season.netProfit < 0 ? styles.financeDanger : styles.financeProfit}><span>Laba / rugi</span><strong>{formatSeasonCurrency(season.netProfit)}</strong><small>Margin {formatSeasonPercentage(season.profitMarginPercentage)}</small></div>
          <div className={season.capitalFundingGap > 0 ? styles.financeWarning : ""}><span>Kekurangan modal</span><strong>{formatSeasonCurrency(season.capitalFundingGap)}</strong></div>
        </div>
      </section>

      <section className={styles.attentionSection}>
        <header>
          <div><span className={styles.eyebrow}>Indikator perhatian</span><h3>{season.attentions.length > 0 ? `${season.attentions.length} catatan objektif` : "Tidak ada perhatian"}</h3></div>
          <span className={styles.attentionTotals}>{season.criticalAttentionCount} kritis · {season.warningAttentionCount} perhatian · {season.informationAttentionCount} informasi</span>
        </header>
        {season.attentions.length === 0 ? (
          <div className={styles.healthyState}><Icon name="check" /><span><strong>Musim sehat berdasarkan data tercatat.</strong><small>Tidak ada indikator kritis atau peringatan yang terdeteksi.</small></span></div>
        ) : (
          <div className={styles.attentionList}>
            {season.attentions.map((attention) => (
              <div className={`${styles.attentionItem} ${severityClass(attention.severity)}`} key={attention.code}>
                <span><Icon name={attention.severity === 1 ? "history" : "warning"} /></span>
                <div><strong>{attentionLabels[attention.code]}</strong><small>{attentionSeverityLabels[attention.severity]}</small></div>
                {attentionValueLabel(attention) && <b>{attentionValueLabel(attention)}</b>}
              </div>
            ))}
          </div>
        )}
      </section>
    </article>
  );
}

export function SeasonHistoryManagement({ organization, organizationId, permissions }: Props) {
  const router = useRouter();
  const [lands, setLands] = useState<Land[]>([]);
  const [selectedLandId, setSelectedLandId] = useState<string>("");
  const [selectedPlotId, setSelectedPlotId] = useState<string>("");
  const [includeNonTerminal, setIncludeNonTerminal] = useState(false);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [history, setHistory] = useState<LandSeasonHistory | null>(null);
  const [selectedSeasonId, setSelectedSeasonId] = useState<string | null>(null);
  const [isLoadingLands, setIsLoadingLands] = useState(true);
  const [isLoadingHistory, setIsLoadingHistory] = useState(false);
  const [pageError, setPageError] = useState<string | null>(null);
  const [refreshKey, setRefreshKey] = useState(0);
  const canReadFinance = permissions.includes("finance.read");
  const canReadLands = permissions.includes("lands.read");
  const canRead = canReadFinance && canReadLands;

  const selectedLand = useMemo(
    () => lands.find((land) => land.id === selectedLandId) ?? null,
    [lands, selectedLandId],
  );
  const selectedSeason = useMemo(
    () => history?.seasons.find((season) => season.cropCycleId === selectedSeasonId)
      ?? history?.seasons[0]
      ?? null,
    [history, selectedSeasonId],
  );
  const summary = useMemo(
    () => summarizeSeasonPage(history?.seasons ?? []),
    [history],
  );

  useEffect(() => {
    let cancelled = false;

    async function loadLands() {
      if (!organizationId || !canRead) {
        if (!cancelled) setIsLoadingLands(false);
        return;
      }
      setIsLoadingLands(true);
      setPageError(null);
      try {
        const result = await getLands(organizationId);
        if (!cancelled) {
          const ordered = result.toSorted((left, right) => left.name.localeCompare(right.name, "id-ID"));
          setLands(ordered);
          setSelectedLandId((current) => ordered.some((land) => land.id === current)
            ? current
            : ordered[0]?.id ?? "");
        }
      } catch (error) {
        if (error instanceof ApiError && error.status === 401) {
          router.replace("/login");
          return;
        }
        if (!cancelled) setPageError(friendlyError(error));
      } finally {
        if (!cancelled) setIsLoadingLands(false);
      }
    }

    void loadLands();
    return () => { cancelled = true; };
  }, [canRead, organizationId, router]);

  useEffect(() => {
    let cancelled = false;

    async function loadHistory() {
      if (!organizationId || !selectedLandId || !canRead) {
        if (!cancelled) {
          setHistory(null);
          setIsLoadingHistory(false);
        }
        return;
      }

      setIsLoadingHistory(true);
      setPageError(null);
      try {
        const result = await getLandSeasonHistory(organizationId, selectedLandId, {
          landPlotId: selectedPlotId || undefined,
          includeNonTerminal,
          page,
          pageSize,
        });
        if (!cancelled) {
          setHistory(result);
          setSelectedSeasonId((current) => result.seasons.some((season) => season.cropCycleId === current)
            ? current
            : result.seasons[0]?.cropCycleId ?? null);
        }
      } catch (error) {
        if (error instanceof ApiError && error.status === 401) {
          router.replace("/login");
          return;
        }
        if (!cancelled) {
          setHistory(null);
          setPageError(friendlyError(error));
        }
      } finally {
        if (!cancelled) setIsLoadingHistory(false);
      }
    }

    void loadHistory();
    return () => { cancelled = true; };
  }, [canRead, includeNonTerminal, organizationId, page, pageSize, refreshKey, router, selectedLandId, selectedPlotId]);

  if (!organizationId) {
    return <section className={styles.accessState}><Icon name="field" /><h1>Pilih organisasi terlebih dahulu</h1><p>Histori musim selalu dibaca dalam satu organisasi aktif.</p></section>;
  }

  if (!canRead) {
    return <section className={styles.accessState}><Icon name="stop" /><h1>Akses evaluasi tidak tersedia</h1><p>Halaman ini memerlukan izin <strong>finance.read</strong> dan <strong>lands.read</strong> karena menampilkan data finansial per musim.</p></section>;
  }

  return (
    <section className={styles.page}>
      <header className={styles.hero}>
        <div><span className={styles.eyebrow}>Histori lahan &amp; evaluasi musim</span><h1>Belajar dari setiap musim.</h1><p>Bandingkan fakta budidaya dan finansial tanpa mengubah catatan sumber {organization ? `di ${organization.name}` : "organisasi"}.</p></div>
        <button className={styles.refreshButton} type="button" disabled={isLoadingLands || isLoadingHistory || !selectedLandId} onClick={() => setRefreshKey((current) => current + 1)}><Icon name="refresh" /> {isLoadingHistory ? "Memuat..." : "Muat ulang"}</button>
      </header>

      <div className={styles.filterBar}>
        <label><span>Lahan</span><select value={selectedLandId} disabled={isLoadingLands || lands.length === 0} onChange={(event) => { setSelectedLandId(event.target.value); setSelectedPlotId(""); setPage(1); setHistory(null); }}><option value="">Pilih lahan</option>{lands.map((land) => <option value={land.id} key={land.id}>{land.code} · {land.name}</option>)}</select></label>
        <label><span>Petak</span><select value={selectedPlotId} disabled={!selectedLand || selectedLand.plots.length === 0} onChange={(event) => { setSelectedPlotId(event.target.value); setPage(1); }}><option value="">Semua petak</option>{selectedLand?.plots.toSorted((left, right) => left.name.localeCompare(right.name, "id-ID")).map((plot) => <option value={plot.id} key={plot.id}>{plot.code} · {plot.name}</option>)}</select></label>
        <label><span>Data per halaman</span><select value={pageSize} onChange={(event) => { setPageSize(Number(event.target.value)); setPage(1); }}><option value={10}>10 musim</option><option value={20}>20 musim</option><option value={50}>50 musim</option></select></label>
        <label className={styles.toggleField}><input type="checkbox" checked={includeNonTerminal} onChange={(event) => { setIncludeNonTerminal(event.target.checked); setPage(1); }} /><span><i /><b>Sertakan musim aktif</b><small>Terencana dan berjalan</small></span></label>
      </div>

      {pageError && <div className={styles.pageError} role="alert"><Icon name="warning" /><span>{pageError}</span></div>}

      {isLoadingLands || (isLoadingHistory && !history) ? (
        <div className={styles.loadingState}><span className="loader" /><p>Menyusun histori musim...</p></div>
      ) : lands.length === 0 ? (
        <div className={styles.emptyState}><Icon name="field" /><h2>Belum ada lahan</h2><p>Tambahkan lahan dan petak terlebih dahulu sebelum membuka histori musim.</p></div>
      ) : history ? (
        <>
          <div className={styles.metricGrid}>
            <article className={styles.metricPrimary}><span>Total histori</span><strong>{history.totalSeasonCount}</strong><small>{history.landPlotName ? `${history.landPlotCode} · ${history.landPlotName}` : "Seluruh petak pada lahan"}</small><i><Icon name="history" /></i></article>
            <article><span>Tampil di halaman ini</span><strong>{summary.visibleSeasonCount}</strong><small>{summary.reviewReadyCount} siap untuk evaluasi</small><i><Icon name="calendar" /></i></article>
            <article className={summary.requiresAttentionCount > 0 ? styles.metricWarning : ""}><span>Perlu perhatian</span><strong>{summary.requiresAttentionCount}</strong><small>{summary.criticalAttentionCount} indikator kritis</small><i><Icon name="warning" /></i></article>
            <article className={summary.outstandingReceivable > 0 ? styles.metricWarning : ""}><span>Piutang pada halaman</span><strong>{formatSeasonCurrency(summary.outstandingReceivable)}</strong><small>Hanya musim yang sedang ditampilkan</small><i><Icon name="trend" /></i></article>
          </div>

          {history.seasons.length === 0 ? (
            <div className={styles.emptyState}><Icon name="history" /><h2>Belum ada musim yang cocok</h2><p>Coba pilih petak lain atau sertakan musim aktif. Histori formal hanya memuat siklus selesai dan dibatalkan.</p></div>
          ) : (
            <div className={styles.historyGrid}>
              <aside className={styles.seasonList}>
                <header><div><span className={styles.eyebrow}>Musim terbaru</span><h2>{history.landCode} · {history.landName}</h2></div><small>Dibuat {formatGeneratedAt(history.generatedAt)}</small></header>
                <div className={styles.seasonButtons}>
                  {history.seasons.map((season) => (
                    <button className={`${styles.seasonButton} ${selectedSeason?.cropCycleId === season.cropCycleId ? styles.seasonButtonActive : ""}`} type="button" key={season.cropCycleId} onClick={() => setSelectedSeasonId(season.cropCycleId)}>
                      <span className={`${styles.outcomeDot} ${styles[`outcome${season.profitabilityOutcome}`]}`} />
                      <span><strong>{season.cropCycleName}</strong><small>{season.cropCycleCode} · {season.commodityName}</small><small>{formatSeasonDate(season.plannedStartDate)} — {formatSeasonDate(season.actualHarvestDate ?? season.expectedHarvestDate)}</small></span>
                      <span className={styles.seasonResult}><b>{formatSeasonCurrency(season.netProfit)}</b><small>{formatSeasonPercentage(season.profitMarginPercentage)}</small>{season.requiresAttention && <em>{season.criticalAttentionCount > 0 ? `${season.criticalAttentionCount} kritis` : `${season.warningAttentionCount} perhatian`}</em>}</span>
                    </button>
                  ))}
                </div>
                <footer className={styles.pagination}><button type="button" disabled={!history.hasPreviousPage || isLoadingHistory} onClick={() => setPage((current) => Math.max(1, current - 1))}><Icon name="previous" /> Sebelumnya</button><span>Halaman <strong>{history.page}</strong> dari <strong>{Math.max(1, history.totalPages)}</strong></span><button type="button" disabled={!history.hasNextPage || isLoadingHistory} onClick={() => setPage((current) => current + 1)}>Berikutnya <Icon name="next" /></button></footer>
              </aside>
              {selectedSeason && <SeasonDetail season={selectedSeason} />}
            </div>
          )}
        </>
      ) : null}
    </section>
  );
}
