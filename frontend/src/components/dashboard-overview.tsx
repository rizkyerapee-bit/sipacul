"use client";

import { useEffect, useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import {
  ApiError,
  getCropCycleProfitability,
  getCropCycles,
  getCultivationActivities,
  getHarvestBatches,
  getLands,
} from "@/lib/api/client";
import type {
  CropCycle,
  CropCycleProfitability,
  CultivationActivity,
  HarvestBatch,
  Land,
  Organization,
} from "@/lib/api/contracts";
import {
  buildCycleStatusBreakdown,
  calculateScheduleProgress,
  formatCurrency,
  formatDate,
  formatDecimal,
  getActivityStatusLabel,
  getCropCycleStatusLabel,
  getHarvestQuantityUnitLabel,
  selectDefaultCropCycle,
  sortActivitiesForAgenda,
  summarizeHarvests,
  summarizeOrganizationDashboard,
} from "@/lib/dashboard/dashboard-summary";
import styles from "./dashboard-overview.module.css";

type DashboardOverviewProps = {
  firstName: string;
  organization: Organization | null;
  organizationId: string | null;
  permissions: string[];
};

type DashboardIconName =
  | "land"
  | "sprout"
  | "wallet"
  | "trend"
  | "refresh"
  | "copy"
  | "calendar"
  | "harvest"
  | "chart"
  | "check";

const iconPaths: Record<DashboardIconName, string> = {
  land: "M3 6.5 9 4l6 2.5L21 4v13.5L15 20l-6-2.5L3 20V6.5Zm6-2.5v13.5M15 6.5V20",
  sprout: "M12 21v-9m0 2c-4 0-7-2-7-6 4 0 7 2 7 6Zm0-3c4 0 7-2 7-6-4 0-7 2-7 6Z",
  wallet: "M4 6h14a2 2 0 0 1 2 2v11H4a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2h12m4 7h-5a2 2 0 0 0 0 4h5",
  trend: "m4 17 5-5 4 4 7-8m-5 0h5v5",
  refresh: "M20 7v5h-5M4 17v-5h5m10.5 0A8 8 0 0 0 6.2 6.2L4 8m.5 4A8 8 0 0 0 17.8 17.8L20 16",
  copy: "M8 8h11v12H8V8Zm-3 8H4V4h11v1",
  calendar: "M5 4v3m14-3v3M4 9h16M5 6h14a1 1 0 0 1 1 1v13H4V7a1 1 0 0 1 1-1Z",
  harvest: "M5 20h14M7 20V9m4 11V5m4 15V8m4 12V4M5 9c2 0 4 1 6 3m0-7c2 0 3 1 4 3m0 0c2-1 3-2 4-4",
  chart: "M4 19V10m6 9V5m6 14v-6m4 6H2",
  check: "m5 12 4 4L19 6",
};

const sourceLabels: Record<string, string> = {
  lands: "lahan",
  cropCycles: "siklus tanam",
  activities: "aktivitas",
  harvests: "panen",
  profitability: "profitabilitas",
};

function DashboardIcon({ name }: { name: DashboardIconName }) {
  return (
    <svg viewBox="0 0 24 24" aria-hidden="true">
      <path d={iconPaths[name]} />
    </svg>
  );
}

function describeSourceError(source: string, error: unknown): string {
  if (error instanceof ApiError && error.status === 403) {
    return `Izin untuk data ${sourceLabels[source]} berubah. Silakan masuk ulang.`;
  }

  const message = error instanceof Error ? error.message : "Permintaan gagal.";
  return `Data ${sourceLabels[source]} gagal dimuat: ${message}`;
}

function getStatusClassName(status: number): string {
  if (status === 2) {
    return styles.statusInProgress;
  }

  if (status === 3) {
    return styles.statusCompleted;
  }

  if (status === 4) {
    return styles.statusCancelled;
  }

  return styles.statusPlanned;
}

export function DashboardOverview({
  firstName,
  organization,
  organizationId,
  permissions,
}: DashboardOverviewProps) {
  const router = useRouter();
  const [lands, setLands] = useState<Land[] | null>(null);
  const [cropCycles, setCropCycles] = useState<CropCycle[] | null>(null);
  const [selectedCropCycleId, setSelectedCropCycleId] = useState("");
  const [activities, setActivities] = useState<CultivationActivity[] | null>(null);
  const [harvestBatches, setHarvestBatches] = useState<HarvestBatch[] | null>(null);
  const [profitability, setProfitability] = useState<CropCycleProfitability | null>(null);
  const [baseErrors, setBaseErrors] = useState<string[]>([]);
  const [cycleErrors, setCycleErrors] = useState<string[]>([]);
  const [isBaseLoading, setIsBaseLoading] = useState(true);
  const [isCycleLoading, setIsCycleLoading] = useState(false);
  const [refreshVersion, setRefreshVersion] = useState(0);
  const [lastUpdatedAt, setLastUpdatedAt] = useState<Date | null>(null);
  const [copyStatus, setCopyStatus] = useState<string | null>(null);

  const canReadLands = permissions.includes("lands.read");
  const canReadCultivation = permissions.includes("cultivation.read");
  const canReadHarvest = permissions.includes("harvest.read");
  const canReadFinance = permissions.includes("finance.read");

  useEffect(() => {
    let cancelled = false;

    async function loadBaseData() {
      if (!organizationId) {
        setLands(null);
        setCropCycles(null);
        setSelectedCropCycleId("");
        setIsBaseLoading(false);
        return;
      }

      setIsBaseLoading(true);
      setBaseErrors([]);
      const errors: string[] = [];
      const requests: Promise<void>[] = [];

      if (canReadLands) {
        requests.push(
          getLands(organizationId)
            .then((response) => {
              if (!cancelled) {
                setLands(response);
              }
            })
            .catch((error: unknown) => {
              if (error instanceof ApiError && error.status === 401) {
                router.replace("/login");
                return;
              }

              errors.push(describeSourceError("lands", error));
              if (!cancelled) {
                setLands([]);
              }
            }),
        );
      } else {
        setLands(null);
      }

      if (canReadCultivation) {
        requests.push(
          getCropCycles(organizationId)
            .then((response) => {
              if (cancelled) {
                return;
              }

              setCropCycles(response);
              setSelectedCropCycleId((current) => {
                if (response.some((cycle) => cycle.id === current)) {
                  return current;
                }

                return selectDefaultCropCycle(response)?.id ?? "";
              });
            })
            .catch((error: unknown) => {
              if (error instanceof ApiError && error.status === 401) {
                router.replace("/login");
                return;
              }

              errors.push(describeSourceError("cropCycles", error));
              if (!cancelled) {
                setCropCycles([]);
                setSelectedCropCycleId("");
              }
            }),
        );
      } else {
        setCropCycles(null);
        setSelectedCropCycleId("");
      }

      await Promise.all(requests);

      if (!cancelled) {
        setBaseErrors(errors);
        setLastUpdatedAt(new Date());
        setIsBaseLoading(false);
      }
    }

    void loadBaseData();

    return () => {
      cancelled = true;
    };
  }, [
    canReadCultivation,
    canReadLands,
    organizationId,
    refreshVersion,
    router,
  ]);

  useEffect(() => {
    let cancelled = false;

    async function loadCycleData() {
      if (!organizationId || !selectedCropCycleId) {
        setActivities(null);
        setHarvestBatches(null);
        setProfitability(null);
        setCycleErrors([]);
        setIsCycleLoading(false);
        return;
      }

      setIsCycleLoading(true);
      setCycleErrors([]);
      const errors: string[] = [];
      const requests: Promise<void>[] = [];

      if (canReadCultivation) {
        requests.push(
          getCultivationActivities(organizationId, selectedCropCycleId)
            .then((response) => {
              if (!cancelled) {
                setActivities(response);
              }
            })
            .catch((error: unknown) => {
              if (error instanceof ApiError && error.status === 401) {
                router.replace("/login");
                return;
              }

              errors.push(describeSourceError("activities", error));
              if (!cancelled) {
                setActivities([]);
              }
            }),
        );
      } else {
        setActivities(null);
      }

      if (canReadHarvest) {
        requests.push(
          getHarvestBatches(organizationId, selectedCropCycleId)
            .then((response) => {
              if (!cancelled) {
                setHarvestBatches(response);
              }
            })
            .catch((error: unknown) => {
              if (error instanceof ApiError && error.status === 401) {
                router.replace("/login");
                return;
              }

              errors.push(describeSourceError("harvests", error));
              if (!cancelled) {
                setHarvestBatches([]);
              }
            }),
        );
      } else {
        setHarvestBatches(null);
      }

      if (canReadFinance) {
        requests.push(
          getCropCycleProfitability(organizationId, selectedCropCycleId)
            .then((response) => {
              if (!cancelled) {
                setProfitability(response);
              }
            })
            .catch((error: unknown) => {
              if (error instanceof ApiError && error.status === 401) {
                router.replace("/login");
                return;
              }

              errors.push(describeSourceError("profitability", error));
              if (!cancelled) {
                setProfitability(null);
              }
            }),
        );
      } else {
        setProfitability(null);
      }

      await Promise.all(requests);

      if (!cancelled) {
        setCycleErrors(errors);
        setLastUpdatedAt(new Date());
        setIsCycleLoading(false);
      }
    }

    void loadCycleData();

    return () => {
      cancelled = true;
    };
  }, [
    canReadCultivation,
    canReadFinance,
    canReadHarvest,
    organizationId,
    refreshVersion,
    router,
    selectedCropCycleId,
  ]);

  const selectedCropCycle = useMemo(
    () => cropCycles?.find((cycle) => cycle.id === selectedCropCycleId) ?? null,
    [cropCycles, selectedCropCycleId],
  );
  const organizationSummary = useMemo(
    () => summarizeOrganizationDashboard(lands ?? [], cropCycles ?? []),
    [cropCycles, lands],
  );
  const statusBreakdown = useMemo(
    () => buildCycleStatusBreakdown(cropCycles ?? []),
    [cropCycles],
  );
  const agenda = useMemo(
    () => sortActivitiesForAgenda(activities ?? []).slice(0, 5),
    [activities],
  );
  const harvestSummary = useMemo(
    () => summarizeHarvests(harvestBatches ?? []),
    [harvestBatches],
  );
  const scheduleProgress = selectedCropCycle
    ? calculateScheduleProgress(selectedCropCycle)
    : 0;
  const totalCycles = cropCycles?.length ?? 0;
  const financialBars = profitability
    ? [
        { label: "Pendapatan diakui", value: profitability.recognizedRevenue, tone: "positive" },
        { label: "Biaya budidaya", value: profitability.totalCultivationCost, tone: "cost" },
        { label: "Laba bersih", value: profitability.netProfit, tone: profitability.netProfit < 0 ? "negative" : "profit" },
        { label: "Piutang", value: profitability.outstandingReceivable, tone: "receivable" },
      ]
    : [];
  const financialMaximum = Math.max(
    1,
    ...financialBars.map((item) => Math.abs(item.value)),
  );
  const warnings = [...baseErrors, ...cycleErrors];

  function refreshDashboard() {
    setCopyStatus(null);
    setRefreshVersion((current) => current + 1);
  }

  async function copyDashboardSummary() {
    if (!navigator.clipboard) {
      setCopyStatus("Clipboard tidak tersedia di browser ini.");
      return;
    }

    const lines = [
      `Ringkasan ${organization?.name ?? "SiPacul"}`,
      `Lahan aktif: ${organizationSummary.activeLandCount}`,
      `Siklus berjalan: ${organizationSummary.inProgressCycleCount}`,
    ];

    if (selectedCropCycle) {
      lines.push(`Siklus dipilih: ${selectedCropCycle.name}`);
    }

    if (profitability) {
      lines.push(`Biaya budidaya: ${formatCurrency(profitability.totalCultivationCost)}`);
      lines.push(`Laba bersih: ${formatCurrency(profitability.netProfit)}`);
    }

    try {
      await navigator.clipboard.writeText(lines.join("\n"));
      setCopyStatus("Ringkasan berhasil disalin.");
    } catch {
      setCopyStatus("Ringkasan tidak dapat disalin.");
    }
  }

  if (!organizationId) {
    return (
      <section className={styles.emptyWorkspace}>
        <span className={styles.emptyIcon}><DashboardIcon name="land" /></span>
        <h1>Belum ada organisasi aktif</h1>
        <p>Hubungkan akun ke organisasi untuk mulai melihat dashboard operasional.</p>
      </section>
    );
  }

  return (
    <div className={styles.overview}>
      <section className={styles.hero}>
        <div>
          <span className={styles.eyebrow}>Dashboard produksi</span>
          <h1>Selamat datang, {firstName}.</h1>
          <p>
            Pantau kondisi {organization?.name ?? "organisasi"} berdasarkan data API SiPacul terbaru.
          </p>
        </div>
        <div className={styles.heroStatus}>
          <span className={styles.heroStatusIcon}><DashboardIcon name="check" /></span>
          <span>
            <small>Terakhir diperbarui</small>
            <strong>
              {lastUpdatedAt
                ? lastUpdatedAt.toLocaleTimeString("id-ID", { hour: "2-digit", minute: "2-digit" })
                : "Memuat..."}
            </strong>
          </span>
        </div>
      </section>

      <section className={styles.controlBar} aria-label="Filter dan aksi dashboard">
        <label className={styles.cycleFilter}>
          <span>Siklus yang dianalisis</span>
          <select
            value={selectedCropCycleId}
            onChange={(event) => setSelectedCropCycleId(event.target.value)}
            disabled={!cropCycles || cropCycles.length === 0 || isBaseLoading}
          >
            {cropCycles?.length
              ? cropCycles.map((cycle) => (
                  <option value={cycle.id} key={cycle.id}>
                    {cycle.code} — {cycle.name}
                  </option>
                ))
              : <option value="">Belum ada siklus tanam</option>}
          </select>
        </label>
        <div className={styles.quickActions}>
          <button
            type="button"
            onClick={refreshDashboard}
            disabled={isBaseLoading || isCycleLoading}
          >
            <DashboardIcon name="refresh" />
            {isBaseLoading || isCycleLoading ? "Memuat..." : "Perbarui"}
          </button>
          <button
            type="button"
            onClick={() => void copyDashboardSummary()}
            disabled={isBaseLoading}
          >
            <DashboardIcon name="copy" />
            Salin ringkasan
          </button>
        </div>
      </section>

      {copyStatus && <div className={styles.copyNotice} role="status">{copyStatus}</div>}
      {warnings.length > 0 && (
        <div className={styles.warningAlert} role="alert">
          <strong>Sebagian data belum tersedia.</strong>
          <ul>{warnings.map((warning) => <li key={warning}>{warning}</li>)}</ul>
        </div>
      )}

      <section className={styles.metricGrid} aria-label="Metrik utama">
        <article className={`${styles.metricCard} ${styles.metricPrimary}`}>
          <span className={styles.metricIcon}><DashboardIcon name="land" /></span>
          <span className={styles.metricLabel}>Lahan aktif</span>
          <strong>{isBaseLoading ? "…" : lands ? organizationSummary.activeLandCount : "—"}</strong>
          <small>
            {lands
              ? `${organizationSummary.activePlotCount} petak · ${formatDecimal(organizationSummary.activeAreaHectares)} ha`
              : "Tidak memiliki izin data lahan"}
          </small>
        </article>
        <article className={styles.metricCard}>
          <span className={styles.metricIcon}><DashboardIcon name="sprout" /></span>
          <span className={styles.metricLabel}>Siklus berjalan</span>
          <strong>{isBaseLoading ? "…" : cropCycles ? organizationSummary.inProgressCycleCount : "—"}</strong>
          <small>
            {cropCycles
              ? `${organizationSummary.plannedCycleCount} siklus masih direncanakan`
              : "Tidak memiliki izin data budidaya"}
          </small>
        </article>
        <article className={styles.metricCard}>
          <span className={styles.metricIcon}><DashboardIcon name="wallet" /></span>
          <span className={styles.metricLabel}>Biaya budidaya</span>
          <strong className={styles.metricCurrency}>
            {isCycleLoading
              ? "…"
              : profitability
                ? formatCurrency(profitability.totalCultivationCost)
                : "—"}
          </strong>
          <small>
            {canReadFinance
              ? selectedCropCycle?.name ?? "Pilih siklus tanam"
              : "Tidak memiliki izin data keuangan"}
          </small>
        </article>
        <article className={styles.metricCard}>
          <span className={styles.metricIcon}><DashboardIcon name="trend" /></span>
          <span className={styles.metricLabel}>Laba bersih</span>
          <strong className={`${styles.metricCurrency} ${profitability && profitability.netProfit < 0 ? styles.negativeValue : ""}`}>
            {isCycleLoading
              ? "…"
              : profitability
                ? formatCurrency(profitability.netProfit)
                : "—"}
          </strong>
          <small>
            {profitability?.profitMarginPercentage === null || !profitability
              ? "Margin belum dapat dihitung"
              : `Margin ${formatDecimal(profitability.profitMarginPercentage)}%`}
          </small>
        </article>
      </section>

      <section className={styles.dashboardGrid}>
        <article className={`${styles.panel} ${styles.financialPanel}`}>
          <div className={styles.panelHeader}>
            <div>
              <span className={styles.eyebrow}>Kinerja keuangan</span>
              <h2>{selectedCropCycle?.name ?? "Belum ada siklus dipilih"}</h2>
            </div>
            <span className={styles.panelIcon}><DashboardIcon name="chart" /></span>
          </div>

          {isCycleLoading ? (
            <div className={styles.loadingPanel}><span />Memuat perhitungan resmi...</div>
          ) : profitability ? (
            <div className={styles.financialBars}>
              {financialBars.map((item) => (
                <div className={styles.financialRow} key={item.label}>
                  <div><span>{item.label}</span><strong>{formatCurrency(item.value)}</strong></div>
                  <div className={styles.barTrack}>
                    <span
                      className={styles[`bar_${item.tone}`]}
                      style={{ width: `${Math.max(0, Math.abs(item.value) / financialMaximum * 100)}%` }}
                    />
                  </div>
                </div>
              ))}
              <div className={styles.financialFootnote}>
                <span>Kas terkumpul <strong>{formatCurrency(profitability.collectedRevenue)}</strong></span>
                <span>Modal terkonfirmasi <strong>{formatCurrency(profitability.totalConfirmedCapital)}</strong></span>
              </div>
            </div>
          ) : (
            <div className={styles.panelEmpty}>
              <DashboardIcon name="wallet" />
              <strong>{canReadFinance ? "Belum ada laporan profitabilitas" : "Data keuangan dibatasi"}</strong>
              <span>{canReadFinance ? "Pilih atau buat siklus tanam untuk melihat perhitungan." : "Widget ini mengikuti permission organisasi Anda."}</span>
            </div>
          )}
        </article>

        <article className={`${styles.panel} ${styles.statusPanel}`}>
          <div className={styles.panelHeader}>
            <div>
              <span className={styles.eyebrow}>Portofolio budidaya</span>
              <h2>Status siklus tanam</h2>
            </div>
            <span className={styles.totalBadge}>{totalCycles} siklus</span>
          </div>
          {cropCycles ? (
            <div className={styles.statusBars}>
              {statusBreakdown.map((item) => (
                <div key={item.status}>
                  <span><i className={getStatusClassName(item.status)} />{item.label}</span>
                  <div className={styles.statusTrack}>
                    <span
                      className={getStatusClassName(item.status)}
                      style={{ width: `${totalCycles ? item.count / totalCycles * 100 : 0}%` }}
                    />
                  </div>
                  <strong>{item.count}</strong>
                </div>
              ))}
            </div>
          ) : (
            <div className={styles.panelEmpty}><strong>Data budidaya dibatasi</strong></div>
          )}
        </article>

        <article className={`${styles.panel} ${styles.agendaPanel}`}>
          <div className={styles.panelHeader}>
            <div>
              <span className={styles.eyebrow}>Agenda lapangan</span>
              <h2>Aktivitas siklus terpilih</h2>
            </div>
            <span className={styles.panelIcon}><DashboardIcon name="calendar" /></span>
          </div>
          {isCycleLoading ? (
            <div className={styles.loadingPanel}><span />Memuat agenda...</div>
          ) : agenda.length > 0 ? (
            <div className={styles.agendaList}>
              {agenda.map((activity) => (
                <div key={activity.id}>
                  <span className={`${styles.agendaMarker} ${getStatusClassName(activity.status)}`} />
                  <div>
                    <strong>{activity.name}</strong>
                    <small>{activity.code} · {formatDate(activity.plannedDate)}</small>
                  </div>
                  <span className={`${styles.statusBadge} ${getStatusClassName(activity.status)}`}>
                    {getActivityStatusLabel(activity.status)}
                  </span>
                </div>
              ))}
            </div>
          ) : (
            <div className={styles.panelEmpty}>
              <DashboardIcon name="calendar" />
              <strong>Belum ada aktivitas</strong>
              <span>Agenda akan tampil setelah aktivitas budidaya dicatat.</span>
            </div>
          )}
        </article>

        <article className={`${styles.panel} ${styles.cyclePanel}`}>
          <div className={styles.panelHeader}>
            <div>
              <span className={styles.eyebrow}>Siklus terpilih</span>
              <h2>Jadwal dan hasil panen</h2>
            </div>
            <span className={styles.panelIcon}><DashboardIcon name="harvest" /></span>
          </div>
          {selectedCropCycle ? (
            <div className={styles.cycleDetails}>
              <div className={styles.cycleIdentity}>
                <span className={`${styles.statusBadge} ${getStatusClassName(selectedCropCycle.status)}`}>
                  {getCropCycleStatusLabel(selectedCropCycle.status)}
                </span>
                <strong>{selectedCropCycle.code}</strong>
              </div>
              <div className={styles.scheduleTrack}>
                <span style={{ width: `${scheduleProgress}%` }} />
              </div>
              <div className={styles.scheduleMeta}>
                <span><small>Mulai rencana</small><strong>{formatDate(selectedCropCycle.plannedStartDate)}</strong></span>
                <span><small>Estimasi panen</small><strong>{formatDate(selectedCropCycle.expectedHarvestDate)}</strong></span>
                <span><small>Kemajuan waktu</small><strong>{scheduleProgress}%</strong></span>
              </div>
              <div className={styles.harvestSummary}>
                <div><small>Batch terkonfirmasi</small><strong>{harvestBatches ? harvestSummary.confirmedBatchCount : "—"}</strong></div>
                <div><small>Hasil bersih</small><strong>{harvestBatches ? `${formatDecimal(harvestSummary.netQuantity)} ${getHarvestQuantityUnitLabel(harvestSummary.quantityUnit)}` : "—"}</strong></div>
                <div><small>Stok tersedia</small><strong>{profitability ? `${formatDecimal(profitability.availableHarvestQuantity)} ${getHarvestQuantityUnitLabel(profitability.harvestQuantityUnit)}` : harvestBatches ? `${formatDecimal(harvestSummary.availableQuantity)} ${getHarvestQuantityUnitLabel(harvestSummary.quantityUnit)}` : "—"}</strong></div>
              </div>
            </div>
          ) : (
            <div className={styles.panelEmpty}>
              <DashboardIcon name="sprout" />
              <strong>Belum ada siklus tanam</strong>
              <span>Data jadwal dan panen akan tampil setelah siklus dibuat.</span>
            </div>
          )}
        </article>

        <article className={`${styles.panel} ${styles.landPanel}`}>
          <div className={styles.panelHeader}>
            <div>
              <span className={styles.eyebrow}>Aset budidaya</span>
              <h2>Ringkasan lahan aktif</h2>
            </div>
            <span className={styles.totalBadge}>{organizationSummary.activeLandCount} lahan</span>
          </div>
          {lands && lands.filter((land) => land.isActive).length > 0 ? (
            <div className={styles.landTable}>
              <div className={styles.landTableHeader}>
                <span>Lahan</span><span>Petak aktif</span><span>Luas</span><span>Alokasi</span>
              </div>
              {lands
                .filter((land) => land.isActive)
                .sort((left, right) => left.name.localeCompare(right.name, "id"))
                .slice(0, 5)
                .map((land) => (
                  <div className={styles.landTableRow} key={land.id}>
                    <span><strong>{land.name}</strong><small>{land.code}</small></span>
                    <span data-label="Petak aktif">{land.plots.filter((plot) => plot.isActive).length}</span>
                    <span data-label="Luas">{formatDecimal(land.totalAreaInSquareMeters / 10_000)} ha</span>
                    <span data-label="Alokasi">{formatDecimal(land.allocatedPlotAreaInSquareMeters / 10_000)} ha</span>
                  </div>
                ))}
            </div>
          ) : (
            <div className={styles.panelEmpty}>
              <DashboardIcon name="land" />
              <strong>{lands ? "Belum ada lahan aktif" : "Data lahan dibatasi"}</strong>
              <span>{lands ? "Lahan aktif akan muncul pada ringkasan ini." : "Widget ini mengikuti permission organisasi Anda."}</span>
            </div>
          )}
        </article>
      </section>
    </div>
  );
}
