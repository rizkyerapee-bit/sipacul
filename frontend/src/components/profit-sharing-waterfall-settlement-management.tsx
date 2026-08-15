"use client";

import { useCallback, useEffect, useMemo, useState, type FormEvent } from "react";
import {
  ApiError,
  finalizeProfitSharingWaterfallSettlement,
  getProfitSharingPreview,
  getProfitSharingWaterfallSettlement,
  getProfitSharingWaterfallSettlements,
  voidProfitSharingWaterfallSettlement,
} from "@/lib/api/client";
import type {
  CropCycle,
  ProfitSharingPreview,
  ProfitSharingWaterfallSettlement,
} from "@/lib/api/contracts";
import { cropCycleStatusLabels } from "@/lib/cultivation/crop-cycle-management";
import {
  formatSharingCurrency,
  formatSharingDate,
  profitabilityOutcomeLabels,
} from "@/lib/finance/profit-sharing-management";
import {
  createProfitSharingWaterfallSettlementDraft,
  filterProfitSharingWaterfallSettlements,
  formatProfitSharingRate,
  profitSharingParticipantRoleLabels,
  profitSharingPriorityRuleTypeLabels,
  profitSharingResidualMethodLabels,
  profitSharingWaterfallFinalizationAvailability,
  profitSharingWaterfallStatusLabels,
  summarizeProfitSharingWaterfallSettlements,
  validateProfitSharingWaterfallSettlementDraft,
  type ProfitSharingWaterfallSettlementDraft,
  type ProfitSharingWaterfallSettlementStatusFilter,
} from "@/lib/finance/profit-sharing-v2-management";
import styles from "./profit-sharing-waterfall-settlement-management.module.css";

type Props = {
  organizationId: string;
  cycle: CropCycle;
  canFinalize: boolean;
  canVoid: boolean;
};

type IconName =
  | "archive"
  | "bank"
  | "calendar"
  | "check"
  | "close"
  | "document"
  | "history"
  | "lock"
  | "money"
  | "refresh"
  | "search"
  | "shield"
  | "user"
  | "warning";

const iconPaths: Record<IconName, string> = {
  archive: "M4 7h16v13H4V7Zm-1-4h18v4H3V3Zm6 9h6",
  bank: "M3 10h18M5 10v8m4-8v8m6-8v8m4-8v8M2 21h20M12 3 2 8h20L12 3Z",
  calendar: "M6 3v3m12-3v3M4 9h16M5 5h14a1 1 0 0 1 1 1v14H4V6a1 1 0 0 1 1-1Z",
  check: "m5 12 4 4L19 6",
  close: "m6 6 12 12M18 6 6 18",
  document: "M6 3h8l4 4v14H6V3Zm8 0v5h5M9 12h6m-6 4h6",
  history: "M3 12a9 9 0 1 0 3-6.7L3 8m0-5v5h5m4-1v6l4 2",
  lock: "M6 10h12v10H6V10Zm3 0V7a3 3 0 0 1 6 0v3",
  money: "M12 3v18m4-14H9.5a3 3 0 0 0 0 6h5a3 3 0 0 1 0 6H7",
  refresh: "M20 6v5h-5M4 18v-5h5m10-2a7 7 0 0 0-12-4L4 11m16 2-3 4a7 7 0 0 1-12-4",
  search: "m21 21-4.35-4.35m2.35-5.65a8 8 0 1 1-16 0 8 8 0 0 1 16 0Z",
  shield: "M12 3 5 6v5c0 5 3 8 7 10 4-2 7-5 7-10V6l-7-3Zm-3 9 2 2 4-5",
  user: "M12 12a4 4 0 1 0 0-8 4 4 0 0 0 0 8ZM4 21c0-4 3-7 8-7s8 3 8 7",
  warning: "M12 3 2 21h20L12 3Zm0 6v5m0 3h.01",
};

function Icon({ name }: { name: IconName }) {
  return <svg viewBox="0 0 24 24" aria-hidden="true"><path d={iconPaths[name]} /></svg>;
}

function localToday(): string {
  const now = new Date();
  return new Date(now.getTime() - now.getTimezoneOffset() * 60_000)
    .toISOString().slice(0, 10);
}

function formatDateTime(value: string): string {
  return new Intl.DateTimeFormat("id-ID", {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(new Date(value));
}

function formatRatio(value: number): string {
  return new Intl.NumberFormat("id-ID", {
    style: "percent",
    maximumFractionDigits: 2,
  }).format(value);
}

function friendlyError(error: unknown): string {
  if (!(error instanceof ApiError)) {
    return error instanceof Error ? error.message : "Permintaan tidak dapat diselesaikan.";
  }

  const messages: Record<string, string> = {
    "ProfitSharingWaterfallSettlements.AssignmentNotFound": "Siklus belum memiliki snapshot skema V2.",
    "ProfitSharingWaterfallSettlements.CodeAlreadyExists": "Kode settlement sudah digunakan pada siklus ini.",
    "ProfitSharingWaterfallSettlements.ActiveSettlementExists": "Siklus sudah memiliki pembagian hasil final aktif dari V1 atau V2.",
    "ProfitSharingWaterfallSettlements.CropCycleNotTerminal": "Siklus harus selesai atau dibatalkan sebelum finalisasi.",
    "ProfitSharingWaterfallSettlements.ActiveActivityExists": "Masih ada aktivitas budidaya yang aktif.",
    "ProfitSharingWaterfallSettlements.DraftHarvestExists": "Masih ada hasil panen berstatus draf.",
    "ProfitSharingWaterfallSettlements.UnsoldHarvestExists": "Masih ada hasil panen terkonfirmasi yang belum terjual.",
    "ProfitSharingWaterfallSettlements.DraftSaleExists": "Masih ada penjualan berstatus draf.",
    "ProfitSharingWaterfallSettlements.OutstandingReceivableExists": "Masih ada piutang penjualan yang belum lunas.",
    "ProfitSharingWaterfallSettlements.DraftExpenseExists": "Masih ada biaya budidaya berstatus draf.",
    "ProfitSharingWaterfallSettlements.DraftContributionExists": "Masih ada setoran modal berstatus draf.",
    "ProfitSharingWaterfallSettlements.DraftPaymentExists": "Masih ada pembayaran penjualan berstatus draf.",
    "ProfitSharingWaterfallSettlements.CapitalDoesNotMatchCost": "Modal terkonfirmasi harus sama dengan biaya budidaya.",
    "ProfitSharingWaterfallSettlements.ZeroCostUnsupported": "Siklus tanpa biaya tidak dapat difinalkan.",
    "ProfitSharingWaterfallSettlements.CapitalIdentityConflict": "Identitas salah satu pemberi modal tidak konsisten.",
    "ProfitSharingWaterfallSettlements.CapitalNotInScheme": "Ada pemberi modal yang belum tercantum pada snapshot skema.",
    "ProfitSharingWaterfallSettlements.CapitalRoleMismatch": "Peran pemberi modal tidak cocok dengan snapshot skema.",
    "ProfitSharingWaterfallSettlements.SourceDataChanged": "Sumber transaksi berubah saat finalisasi. Muat ulang lalu periksa kembali.",
    "ProfitSharingWaterfallSettlements.CalculationUnavailable": "Perhitungan waterfall belum dapat diselesaikan dari sumber transaksi saat ini.",
    "ProfitSharingWaterfallSettlements.InvalidStatusTransition": "Status settlement tidak mengizinkan tindakan ini.",
    "ProfitSharingWaterfallSettlements.ConcurrencyConflict": "Data berubah karena proses lain. Muat ulang sebelum mencoba kembali.",
    "ProfitSharingPreview.AssignmentNotFound": "Pilih dan simpan skema pada tab Preview V2 terlebih dahulu.",
    "ProfitSharingPreview.CalculationUnavailable": "Preview belum dapat dihitung dari sumber transaksi saat ini.",
  };

  return messages[error.problem?.code ?? ""] ?? error.message;
}

function FinalizationDialog({
  cycle,
  preview,
  draft,
  isSaving,
  error,
  onChange,
  onClose,
  onSubmit,
}: {
  cycle: CropCycle;
  preview: ProfitSharingPreview;
  draft: ProfitSharingWaterfallSettlementDraft;
  isSaving: boolean;
  error: string | null;
  onChange: (next: ProfitSharingWaterfallSettlementDraft) => void;
  onClose: () => void;
  onSubmit: (event: FormEvent<HTMLFormElement>) => Promise<void>;
}) {
  return (
    <form className={styles.dialog} onSubmit={(event) => void onSubmit(event)}>
      <button className={styles.closeButton} type="button" aria-label="Tutup finalisasi" disabled={isSaving} onClick={onClose}><Icon name="close" /></button>
      <span className={styles.dialogIcon}><Icon name="lock" /></span>
      <span className={styles.eyebrow}>{cycle.code} · {cropCycleStatusLabels[cycle.status]}</span>
      <h2>Finalkan snapshot waterfall?</h2>
      <p>SiPacul akan menghitung ulang sumber transaksi lalu menyimpan skema, formula, dan seluruh alokasi sebagai snapshot immutable.</p>

      <div className={styles.dialogTotals}>
        <div><span>Laba / rugi</span><strong>{formatSharingCurrency(preview.profitability.netProfit)}</strong></div>
        <div><span>Total pembayaran</span><strong>{formatSharingCurrency(preview.totals.totalPayout)}</strong></div>
      </div>

      <div className={styles.formGrid}>
        <label><span>Kode settlement *</span><input value={draft.code} maxLength={40} disabled={isSaving} onChange={(event) => onChange({ ...draft, code: event.target.value.toUpperCase() })} /></label>
        <label><span>Tanggal settlement *</span><input type="date" value={draft.settlementDate} disabled={isSaving} onChange={(event) => onChange({ ...draft, settlementDate: event.target.value })} /></label>
        <label className={styles.fullField}><span>Catatan</span><textarea rows={3} maxLength={1000} value={draft.notes} disabled={isSaving} onChange={(event) => onChange({ ...draft, notes: event.target.value })} placeholder="Keterangan audit atau keputusan finalisasi (opsional)." /></label>
      </div>

      <div className={styles.irreversibleNotice}><Icon name="shield" /><span><strong>Nilai final tidak dapat diedit.</strong> Jika perlu koreksi, void snapshot ini dengan alasan, perbaiki sumber transaksi, lalu finalkan snapshot baru.</span></div>
      {error && <div className={styles.formAlert} role="alert">{error}</div>}
      <div className={styles.dialogActions}><button className={styles.secondaryButton} type="button" disabled={isSaving} onClick={onClose}>Kembali</button><button className={styles.primaryButton} type="submit" disabled={isSaving}>{isSaving ? "Memfinalkan..." : "Finalkan snapshot"}</button></div>
    </form>
  );
}

function VoidDialog({
  settlement,
  reason,
  isSaving,
  error,
  onReasonChange,
  onClose,
  onSubmit,
}: {
  settlement: ProfitSharingWaterfallSettlement;
  reason: string;
  isSaving: boolean;
  error: string | null;
  onReasonChange: (value: string) => void;
  onClose: () => void;
  onSubmit: (event: FormEvent<HTMLFormElement>) => Promise<void>;
}) {
  return (
    <form className={styles.dialog} onSubmit={(event) => void onSubmit(event)}>
      <button className={styles.closeButton} type="button" aria-label="Tutup pembatalan" disabled={isSaving} onClick={onClose}><Icon name="close" /></button>
      <span className={`${styles.dialogIcon} ${styles.dangerIcon}`}><Icon name="warning" /></span>
      <span className={styles.eyebrow}>{settlement.code}</span>
      <h2>Batalkan settlement final?</h2>
      <p>Snapshot tidak dihapus. Status, alasan pembatalan, formula, dan seluruh alokasi tetap disimpan untuk jejak audit.</p>
      <label className={styles.reasonField}><span>Alasan pembatalan *</span><textarea autoFocus rows={4} maxLength={1000} value={reason} disabled={isSaving} onChange={(event) => onReasonChange(event.target.value)} placeholder="Jelaskan sumber kesalahan dan tindakan koreksi yang diperlukan." /></label>
      {error && <div className={styles.formAlert} role="alert">{error}</div>}
      <div className={styles.dialogActions}><button className={styles.secondaryButton} type="button" disabled={isSaving} onClick={onClose}>Kembali</button><button className={styles.dangerButton} type="submit" disabled={isSaving}>{isSaving ? "Membatalkan..." : "Void settlement"}</button></div>
    </form>
  );
}

export function ProfitSharingWaterfallSettlementManagement({
  organizationId,
  cycle,
  canFinalize,
  canVoid,
}: Props) {
  const [settlements, setSettlements] = useState<ProfitSharingWaterfallSettlement[]>([]);
  const [preview, setPreview] = useState<ProfitSharingPreview | null>(null);
  const [selectedSettlement, setSelectedSettlement] = useState<ProfitSharingWaterfallSettlement | null>(null);
  const [query, setQuery] = useState("");
  const [status, setStatus] = useState<ProfitSharingWaterfallSettlementStatusFilter>("all");
  const [draft, setDraft] = useState<ProfitSharingWaterfallSettlementDraft>(() => createProfitSharingWaterfallSettlementDraft(cycle.code, localToday()));
  const [voidReason, setVoidReason] = useState("");
  const [isLoading, setIsLoading] = useState(true);
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [isDetailLoading, setIsDetailLoading] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [pageError, setPageError] = useState<string | null>(null);
  const [previewError, setPreviewError] = useState<string | null>(null);
  const [dialogError, setDialogError] = useState<string | null>(null);
  const [showFinalize, setShowFinalize] = useState(false);
  const [showVoid, setShowVoid] = useState(false);

  const summary = useMemo(
    () => summarizeProfitSharingWaterfallSettlements(settlements),
    [settlements],
  );
  const filteredSettlements = useMemo(
    () => filterProfitSharingWaterfallSettlements(settlements, query, status),
    [settlements, query, status],
  );
  const availability = useMemo(
    () => profitSharingWaterfallFinalizationAvailability(cycle.status, settlements, preview !== null),
    [cycle.status, settlements, preview],
  );

  const loadData = useCallback(async (background = false) => {
    if (background) setIsRefreshing(true);
    else setIsLoading(true);
    setPageError(null);
    setPreviewError(null);
    try {
      const nextSettlements = await getProfitSharingWaterfallSettlements(
        organizationId,
        cycle.id,
      );
      setSettlements(nextSettlements);

      const ordered = nextSettlements.toSorted((left, right) =>
        right.finalizedAt.localeCompare(left.finalizedAt));
      const nextSelected = ordered.find((settlement) => settlement.status === 1)
        ?? ordered[0]
        ?? null;
      if (nextSelected) {
        try {
          setSelectedSettlement(await getProfitSharingWaterfallSettlement(
            organizationId,
            cycle.id,
            nextSelected.id,
          ));
        } catch (error) {
          setSelectedSettlement(nextSelected);
          setPageError(friendlyError(error));
        }
      } else {
        setSelectedSettlement(null);
      }

      if (nextSettlements.some((settlement) => settlement.status === 1)) {
        setPreview(null);
      } else {
        try {
          setPreview(await getProfitSharingPreview(organizationId, cycle.id));
        } catch (error) {
          setPreview(null);
          setPreviewError(friendlyError(error));
        }
      }
    } catch (error) {
      setPageError(friendlyError(error));
    } finally {
      setIsLoading(false);
      setIsRefreshing(false);
    }
  }, [organizationId, cycle.id]);

  useEffect(() => {
    const animationFrame = window.requestAnimationFrame(() => {
      setDraft(createProfitSharingWaterfallSettlementDraft(cycle.code, localToday()));
      setQuery("");
      setStatus("all");
      void loadData();
    });
    return () => window.cancelAnimationFrame(animationFrame);
  }, [cycle.code, loadData]);

  async function selectSettlement(settlement: ProfitSharingWaterfallSettlement) {
    setSelectedSettlement(settlement);
    setIsDetailLoading(true);
    setPageError(null);
    try {
      setSelectedSettlement(await getProfitSharingWaterfallSettlement(
        organizationId,
        cycle.id,
        settlement.id,
      ));
    } catch (error) {
      setPageError(friendlyError(error));
    } finally {
      setIsDetailLoading(false);
    }
  }

  async function finalizeSettlement(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const validationErrors = validateProfitSharingWaterfallSettlementDraft(draft);
    if (validationErrors.length > 0) {
      setDialogError(validationErrors[0]);
      return;
    }

    setIsSaving(true);
    setDialogError(null);
    try {
      const created = await finalizeProfitSharingWaterfallSettlement(
        organizationId,
        cycle.id,
        {
          code: draft.code.trim().toUpperCase(),
          settlementDate: draft.settlementDate,
          notes: draft.notes.trim() || null,
        },
      );
      setSettlements((current) => [created, ...current]);
      setSelectedSettlement(created);
      setPreview(null);
      setShowFinalize(false);
    } catch (error) {
      setDialogError(friendlyError(error));
    } finally {
      setIsSaving(false);
    }
  }

  async function voidSettlement(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!selectedSettlement || !voidReason.trim()) {
      setDialogError("Alasan pembatalan wajib diisi.");
      return;
    }

    setIsSaving(true);
    setDialogError(null);
    try {
      const updated = await voidProfitSharingWaterfallSettlement(
        organizationId,
        cycle.id,
        selectedSettlement.id,
        { voidReason: voidReason.trim() },
      );
      setSettlements((current) => current.map((item) =>
        item.id === updated.id ? updated : item));
      setSelectedSettlement(updated);
      setShowVoid(false);
      setVoidReason("");
      try {
        setPreview(await getProfitSharingPreview(organizationId, cycle.id));
        setPreviewError(null);
      } catch (error) {
        setPreview(null);
        setPreviewError(friendlyError(error));
      }
    } catch (error) {
      setDialogError(friendlyError(error));
    } finally {
      setIsSaving(false);
    }
  }

  if (isLoading) {
    return <section className={styles.page}><div className={styles.loadingState}><span /><strong>Menyiapkan settlement waterfall...</strong><p>Membaca preview, histori finalisasi, dan snapshot alokasi.</p></div></section>;
  }

  return (
    <section className={styles.page}>
      <div className={styles.finalizationBanner}>
        <span><Icon name={summary.active ? "lock" : "shield"} /></span>
        <div><strong>{summary.active ? `Snapshot ${summary.active.code} sedang aktif` : "Finalisasi mengunci hasil perhitungan"}</strong><small>{summary.active ? "Sumber transaksi siklus terlindungi sampai snapshot di-void." : availability.reason}</small></div>
        <div className={styles.bannerActions}>
          <button className={styles.secondaryButton} type="button" disabled={isRefreshing} onClick={() => void loadData(true)}><Icon name="refresh" /> {isRefreshing ? "Memuat..." : "Muat ulang"}</button>
          {canFinalize && availability.allowed && preview && <button className={styles.primaryButton} type="button" onClick={() => { setDialogError(null); setDraft(createProfitSharingWaterfallSettlementDraft(cycle.code, localToday())); setShowFinalize(true); }}><Icon name="lock" /> Finalkan snapshot</button>}
        </div>
      </div>

      {pageError && <div className={styles.pageError} role="alert">{pageError}</div>}
      {previewError && !summary.active && <div className={styles.readinessError} role="alert"><Icon name="warning" /><div><strong>Belum siap difinalkan</strong><p>{previewError}</p><small>Periksa assignment pada Preview V2 dan selesaikan seluruh transaksi sumber terlebih dahulu.</small></div></div>}

      <div className={styles.metricGrid}>
        <article><span>Seluruh snapshot</span><strong>{summary.total}</strong><small>Riwayat tidak pernah dihapus</small></article>
        <article className={summary.active ? styles.metricPrimary : ""}><span>Settlement aktif</span><strong>{summary.finalized}</strong><small>{summary.active ? summary.active.code : "Belum ada"}</small></article>
        <article><span>Snapshot di-void</span><strong>{summary.voided}</strong><small>Jejak koreksi tersimpan</small></article>
        <article><span>Pembayaran terbaru</span><strong>{summary.latest ? formatSharingCurrency(summary.latest.totalPayout) : "—"}</strong><small>{summary.latest ? formatSharingDate(summary.latest.settlementDate) : "Belum ada histori"}</small></article>
      </div>

      {!canFinalize && !summary.active && <div className={styles.permissionNotice}><Icon name="lock" /><span>Mode baca: izin <strong>profit-sharing.finalize</strong> diperlukan untuk membuat snapshot final.</span></div>}

      <div className={styles.toolbar}>
        <label className={styles.searchField}><Icon name="search" /><input value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Cari kode settlement atau skema" /></label>
        <label className={styles.filterField}><span>Status</span><select value={status} onChange={(event) => setStatus(event.target.value === "all" ? "all" : Number(event.target.value) as 1 | 2)}><option value="all">Semua</option><option value="1">Final</option><option value="2">Dibatalkan</option></select></label>
        <span className={styles.resultCount}>{filteredSettlements.length} dari {settlements.length} snapshot</span>
      </div>

      {settlements.length === 0 ? (
        <div className={styles.emptyState}><span><Icon name="archive" /></span><h2>Belum ada snapshot final</h2><p>{availability.reason} Preview tetap dinamis sampai tombol finalisasi dijalankan.</p></div>
      ) : (
        <div className={styles.historyLayout}>
          <aside className={styles.historyList}>
            <header><span className={styles.eyebrow}>Histori settlement</span><h2>Pilih snapshot</h2></header>
            {filteredSettlements.length === 0 ? <div className={styles.noResult}>Tidak ada snapshot yang cocok dengan filter.</div> : filteredSettlements.map((settlement) => (
              <button className={`${styles.historyCard} ${selectedSettlement?.id === settlement.id ? styles.historyCardActive : ""}`} type="button" key={settlement.id} onClick={() => void selectSettlement(settlement)}>
                <span className={`${styles.statusDot} ${settlement.status === 2 ? styles.statusVoided : ""}`} />
                <span className={styles.historyIdentity}><strong>{settlement.code}</strong><small>{settlement.schemeCodeSnapshot} v{settlement.schemeVersionSnapshot} · {formatSharingDate(settlement.settlementDate)}</small></span>
                <span className={styles.historyAmount}><strong>{formatSharingCurrency(settlement.totalPayout)}</strong><small>{profitSharingWaterfallStatusLabels[settlement.status]}</small></span>
              </button>
            ))}
          </aside>

          <div className={styles.detailPanel}>
            {isDetailLoading && <div className={styles.detailLoading}>Memuat detail snapshot...</div>}
            {selectedSettlement && (
              <>
                <header className={styles.detailHeader}>
                  <div><span className={styles.eyebrow}>Snapshot immutable · {selectedSettlement.calculationVersion}</span><h2>{selectedSettlement.code}</h2><p>{selectedSettlement.schemeNameSnapshot} · {selectedSettlement.cropCycleNameSnapshot}</p></div>
                  <div className={styles.detailActions}><span className={`${styles.statusBadge} ${selectedSettlement.status === 2 ? styles.statusBadgeVoided : ""}`}>{profitSharingWaterfallStatusLabels[selectedSettlement.status]}</span>{canVoid && selectedSettlement.status === 1 && <button className={styles.dangerOutlineButton} type="button" onClick={() => { setDialogError(null); setVoidReason(""); setShowVoid(true); }}>Void snapshot</button>}</div>
                </header>

                <div className={styles.snapshotMeta}>
                  <div><span>Tanggal settlement</span><strong>{formatSharingDate(selectedSettlement.settlementDate)}</strong></div>
                  <div><span>Difinalkan</span><strong>{formatDateTime(selectedSettlement.finalizedAt)}</strong></div>
                  <div><span>Skema terkunci</span><strong>{selectedSettlement.schemeCodeSnapshot} v{selectedSettlement.schemeVersionSnapshot}</strong></div>
                  <div><span>Komoditas</span><strong>{selectedSettlement.commodityNameSnapshot}</strong></div>
                </div>

                {selectedSettlement.status === 2 && <div className={styles.voidNotice}><Icon name="history" /><div><strong>Dibatalkan {selectedSettlement.voidedAt ? formatDateTime(selectedSettlement.voidedAt) : ""}</strong><p>{selectedSettlement.voidReason}</p></div></div>}

                <div className={styles.moneyGrid}>
                  <article><span>Pendapatan</span><strong>{formatSharingCurrency(selectedSettlement.recognizedRevenue)}</strong><small>{profitabilityOutcomeLabels[selectedSettlement.outcome]}</small></article>
                  <article><span>Biaya / modal</span><strong>{formatSharingCurrency(selectedSettlement.totalCapital)}</strong><small>Biaya {formatSharingCurrency(selectedSettlement.totalCultivationCost)}</small></article>
                  <article className={selectedSettlement.netProfit >= 0 ? styles.moneyPrimary : styles.moneyDanger}><span>Laba / rugi</span><strong>{formatSharingCurrency(selectedSettlement.netProfit)}</strong><small>Snapshot sumber transaksi</small></article>
                  <article><span>Total pembayaran</span><strong>{formatSharingCurrency(selectedSettlement.totalPayout)}</strong><small>{selectedSettlement.participantAllocations.length} penerima</small></article>
                </div>

                <div className={styles.reconciliation}>
                  <div><span>Modal kembali</span><strong>{formatSharingCurrency(selectedSettlement.totalCapitalRecovery)}</strong></div><b>+</b><div><span>Bagian laba</span><strong>{formatSharingCurrency(selectedSettlement.totalProfitShare)}</strong></div><b>=</b><div><span>Total pembayaran</span><strong>{formatSharingCurrency(selectedSettlement.totalPayout)}</strong></div>{selectedSettlement.totalCapitalLoss > 0 && <em>Kerugian modal {formatSharingCurrency(selectedSettlement.totalCapitalLoss)}</em>}
                </div>

                <section className={styles.allocationSection}>
                  <header><div><span className={styles.eyebrow}>Alokasi final</span><h3>Hak setiap peserta</h3></div><small>Nilai tersimpan dan tidak mengikuti perubahan data berikutnya</small></header>
                  <div className={styles.tableShell}>
                    <table className={styles.allocationTable}>
                      <thead><tr><th>Peserta</th><th>Modal</th><th>Pemulihan / rugi</th><th>Komponen laba</th><th>Total dibayar</th></tr></thead>
                      <tbody>{selectedSettlement.participantAllocations.toSorted((left, right) => left.sequence - right.sequence).map((allocation) => <tr key={allocation.id}><td data-label="Peserta"><span className={styles.personCell}><i><Icon name={allocation.participantRole === 1 ? "bank" : "user"} /></i><span><strong>{allocation.participantNameSnapshot}</strong><small>{allocation.participantCodeSnapshot} · {profitSharingParticipantRoleLabels[allocation.participantRole]}</small></span></span></td><td data-label="Modal"><span className={styles.stackCell}><strong>{formatSharingCurrency(allocation.confirmedCapital)}</strong><small>{formatRatio(allocation.capitalRatio)}</small></span></td><td data-label="Pemulihan / rugi"><span className={styles.stackCell}><strong>{formatSharingCurrency(allocation.capitalRecovery)}</strong>{allocation.capitalLoss > 0 && <small className={styles.lossText}>Rugi {formatSharingCurrency(allocation.capitalLoss)}</small>}</span></td><td data-label="Komponen laba"><span className={styles.profitCell}><small>Kelola <b>{formatSharingCurrency(allocation.managementProfitShare)}</b></small><small>Imbal modal <b>{formatSharingCurrency(allocation.returnOnCapitalProfitShare)}</b></small><small>Residual <b>{formatSharingCurrency(allocation.residualProfitShare)}</b></small><strong>Total {formatSharingCurrency(allocation.totalProfitShare)}</strong></span></td><td data-label="Total dibayar"><strong className={styles.payout}>{formatSharingCurrency(allocation.totalPayout)}</strong></td></tr>)}</tbody>
                    </table>
                  </div>
                </section>

                <div className={styles.ruleGrid}>
                  <section><header><span className={styles.eyebrow}>Aturan prioritas</span><h3>{selectedSettlement.priorityAllocations.length} alokasi</h3></header>{selectedSettlement.priorityAllocations.length === 0 ? <p className={styles.inlineEmpty}>Tidak ada potongan prioritas.</p> : selectedSettlement.priorityAllocations.toSorted((left, right) => left.sequence - right.sequence).map((rule) => <div className={styles.ruleRow} key={rule.id}><span><strong>{profitSharingPriorityRuleTypeLabels[rule.ruleType]}</strong><small>{rule.recipientNameSnapshot} · {formatProfitSharingRate(rule.rateNumerator, rule.rateDenominator)}</small></span><span><strong>{formatSharingCurrency(rule.allocatedAmount)}</strong>{rule.unallocatedAmount > 0 && <small>Tak teralokasi {formatSharingCurrency(rule.unallocatedAmount)}</small>}</span></div>)}</section>
                  <section><header><span className={styles.eyebrow}>Kebijakan residual</span><h3>{profitSharingResidualMethodLabels[selectedSettlement.residualMethod]}</h3></header><div className={styles.auditList}><div><span>Bagian pengelolaan</span><strong>{formatSharingCurrency(selectedSettlement.totalManagementProfitShare)}</strong></div><div><span>Imbal hasil modal</span><strong>{formatSharingCurrency(selectedSettlement.totalReturnOnCapitalProfitShare)}</strong></div><div><span>Laba residual</span><strong>{formatSharingCurrency(selectedSettlement.totalResidualProfitShare)}</strong></div></div></section>
                </div>

                {(selectedSettlement.notes || selectedSettlement.schemeDescriptionSnapshot) && <div className={styles.notes}><Icon name="document" /><div>{selectedSettlement.schemeDescriptionSnapshot && <p><strong>Skema:</strong> {selectedSettlement.schemeDescriptionSnapshot}</p>}{selectedSettlement.notes && <p><strong>Catatan finalisasi:</strong> {selectedSettlement.notes}</p>}</div></div>}
              </>
            )}
          </div>
        </div>
      )}

      {showFinalize && preview && (
        <div className={styles.modalBackdrop} role="presentation" onMouseDown={(event) => { if (event.target === event.currentTarget && !isSaving) setShowFinalize(false); }}><div className={styles.modalPanel} role="dialog" aria-modal="true"><FinalizationDialog cycle={cycle} preview={preview} draft={draft} isSaving={isSaving} error={dialogError} onChange={setDraft} onClose={() => setShowFinalize(false)} onSubmit={finalizeSettlement} /></div></div>
      )}

      {showVoid && selectedSettlement && (
        <div className={styles.modalBackdrop} role="presentation" onMouseDown={(event) => { if (event.target === event.currentTarget && !isSaving) setShowVoid(false); }}><div className={styles.modalPanel} role="dialog" aria-modal="true"><VoidDialog settlement={selectedSettlement} reason={voidReason} isSaving={isSaving} error={dialogError} onReasonChange={setVoidReason} onClose={() => setShowVoid(false)} onSubmit={voidSettlement} /></div></div>
      )}
    </section>
  );
}
