"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import {
  ApiError,
  assignProfitSharingScheme,
  getProfitSharingPreview,
  getProfitSharingSchemeAssignment,
  getProfitSharingSchemes,
} from "@/lib/api/client";
import type {
  CropCycle,
  ProfitSharingPreview,
  ProfitSharingScheme,
  ProfitSharingSchemeAssignment,
} from "@/lib/api/contracts";
import { cropCycleStatusLabels } from "@/lib/cultivation/crop-cycle-management";
import {
  formatSharingCurrency,
  profitabilityOutcomeLabels,
} from "@/lib/finance/profit-sharing-management";
import {
  formatProfitSharingRate,
  profitSharingAssignmentAvailability,
  profitSharingParticipantRoleLabels,
  profitSharingPriorityRuleTypeLabels,
  profitSharingResidualMethodLabels,
  summarizeProfitSharingPreview,
} from "@/lib/finance/profit-sharing-v2-management";
import styles from "./profit-sharing-waterfall-preview.module.css";

type Props = {
  organizationId: string;
  cycle: CropCycle;
  canWrite: boolean;
};

type IconName =
  | "arrow"
  | "bank"
  | "check"
  | "close"
  | "flow"
  | "lock"
  | "money"
  | "refresh"
  | "shield"
  | "user"
  | "warning";

const iconPaths: Record<IconName, string> = {
  arrow: "m9 18 6-6-6-6",
  bank: "M3 10h18M5 10v8m4-8v8m6-8v8m4-8v8M2 21h20M12 3 2 8h20L12 3Z",
  check: "m5 12 4 4L19 6",
  close: "m6 6 12 12M18 6 6 18",
  flow: "M5 5h5v5H5V5Zm9 9h5v5h-5v-5Zm-4-6h3a3 3 0 0 1 3 3v3M8 10v4a3 3 0 0 0 3 3h3",
  lock: "M6 10h12v10H6V10Zm3 0V7a3 3 0 0 1 6 0v3",
  money: "M12 3v18m4-14H9.5a3 3 0 0 0 0 6h5a3 3 0 0 1 0 6H7",
  refresh: "M20 6v5h-5M4 18v-5h5m10-2a7 7 0 0 0-12-4L4 11m16 2-3 4a7 7 0 0 1-12-4",
  shield: "M12 3 5 6v5c0 5 3 8 7 10 4-2 7-5 7-10V6l-7-3Zm-3 9 2 2 4-5",
  user: "M12 12a4 4 0 1 0 0-8 4 4 0 0 0 0 8ZM4 21c0-4 3-7 8-7s8 3 8 7",
  warning: "M12 3 2 21h20L12 3Zm0 6v5m0 3h.01",
};

function Icon({ name }: { name: IconName }) {
  return <svg viewBox="0 0 24 24" aria-hidden="true"><path d={iconPaths[name]} /></svg>;
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
    "ProfitSharingSchemeAssignments.SchemeNotActive": "Hanya skema berstatus aktif yang dapat dipilih.",
    "ProfitSharingSchemeAssignments.CropCycleClosed": "Siklus yang selesai atau dibatalkan tidak dapat menerima skema.",
    "ProfitSharingSchemeAssignments.AssignmentLocked": "Skema tidak dapat diganti setelah siklus mulai berjalan.",
    "ProfitSharingPreview.AssignmentNotFound": "Pilih skema aktif sebelum meminta preview waterfall.",
    "ProfitSharingPreview.CapitalIdentityConflict": "Satu kode modal memiliki identitas pemberi modal yang tidak konsisten.",
    "ProfitSharingPreview.CapitalNotInScheme": "Ada kode pemberi modal terkonfirmasi yang belum tercantum sebagai peserta skema.",
    "ProfitSharingPreview.CapitalRoleMismatch": "Peran pemberi modal tidak sama dengan peran peserta pada snapshot skema.",
    "ProfitSharingPreview.SourceDataChanged": "Data modal berubah saat dihitung. Muat ulang preview.",
    "ProfitSharingPreview.CalculationUnavailable": "Preview belum dapat dihitung. Pastikan total modal sama dengan biaya dan seluruh aturan skema valid.",
  };

  return messages[error.problem?.code ?? ""] ?? error.message;
}

async function optionalAssignment(
  organizationId: string,
  cropCycleId: string,
): Promise<ProfitSharingSchemeAssignment | null> {
  try {
    return await getProfitSharingSchemeAssignment(organizationId, cropCycleId);
  } catch (error) {
    if (
      error instanceof ApiError
      && error.problem?.code === "ProfitSharingSchemeAssignments.AssignmentNotFound"
    ) {
      return null;
    }
    throw error;
  }
}

function AssignmentDialog({
  scheme,
  cycle,
  isReplacement,
  isSaving,
  error,
  onClose,
  onConfirm,
}: {
  scheme: ProfitSharingScheme;
  cycle: CropCycle;
  isReplacement: boolean;
  isSaving: boolean;
  error: string | null;
  onClose: () => void;
  onConfirm: () => Promise<void>;
}) {
  return (
    <section className={styles.dialog}>
      <button className={styles.closeButton} type="button" aria-label="Tutup konfirmasi" disabled={isSaving} onClick={onClose}><Icon name="close" /></button>
      <span className={styles.dialogIcon}><Icon name="shield" /></span>
      <span className={styles.eyebrow}>{cycle.code} · {cropCycleStatusLabels[cycle.status]}</span>
      <h2>{isReplacement ? "Ganti snapshot skema siklus?" : "Gunakan skema untuk siklus ini?"}</h2>
      <p>SiPacul akan menyalin <strong>{scheme.code} v{scheme.version}</strong> beserta peserta dan seluruh aturan waterfall sebagai snapshot siklus.</p>
      <div className={styles.dialogScheme}><strong>{scheme.name}</strong><span>{scheme.participants.length} peserta · {scheme.priorityRules.length} aturan prioritas · {profitSharingResidualMethodLabels[scheme.residualMethod]}</span></div>
      <small>{isReplacement
        ? "Snapshot lama akan diganti. Tindakan ini hanya diizinkan selama siklus masih direncanakan."
        : "Assignment pertama tetap diizinkan pada siklus berjalan untuk mengakomodasi data lama, lalu langsung terkunci."}</small>
      {error && <div className={styles.formAlert} role="alert">{error}</div>}
      <div className={styles.dialogActions}><button className={styles.secondaryButton} type="button" disabled={isSaving} onClick={onClose}>Kembali</button><button className={styles.primaryButton} type="button" disabled={isSaving} onClick={() => void onConfirm()}>{isSaving ? "Menyimpan..." : isReplacement ? "Ya, ganti skema" : "Ya, gunakan skema"}</button></div>
    </section>
  );
}

export function ProfitSharingWaterfallPreview({
  organizationId,
  cycle,
  canWrite,
}: Props) {
  const [activeSchemes, setActiveSchemes] = useState<ProfitSharingScheme[]>([]);
  const [assignment, setAssignment] = useState<ProfitSharingSchemeAssignment | null>(null);
  const [preview, setPreview] = useState<ProfitSharingPreview | null>(null);
  const [selectedSchemeId, setSelectedSchemeId] = useState("");
  const [isLoading, setIsLoading] = useState(true);
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [pageError, setPageError] = useState<string | null>(null);
  const [previewError, setPreviewError] = useState<string | null>(null);
  const [dialogError, setDialogError] = useState<string | null>(null);
  const [showConfirm, setShowConfirm] = useState(false);

  const availability = useMemo(
    () => profitSharingAssignmentAvailability(cycle.status, assignment !== null),
    [cycle.status, assignment],
  );
  const previewSummary = useMemo(
    () => preview ? summarizeProfitSharingPreview(preview) : null,
    [preview],
  );
  const selectedScheme = activeSchemes.find((scheme) => scheme.id === selectedSchemeId) ?? null;
  const assignedSourceIsActive = assignment
    ? activeSchemes.some((scheme) => scheme.id === assignment.sourceSchemeId)
    : false;

  const loadData = useCallback(async (background = false) => {
    if (background) setIsRefreshing(true);
    else setIsLoading(true);
    setPageError(null);
    setPreviewError(null);
    try {
      const [schemes, nextAssignment] = await Promise.all([
        getProfitSharingSchemes(organizationId, { status: 2 }),
        optionalAssignment(organizationId, cycle.id),
      ]);
      setActiveSchemes(schemes);
      setAssignment(nextAssignment);
      setSelectedSchemeId(nextAssignment?.sourceSchemeId ?? schemes[0]?.id ?? "");
      if (nextAssignment) {
        try {
          setPreview(await getProfitSharingPreview(organizationId, cycle.id));
        } catch (error) {
          setPreview(null);
          setPreviewError(friendlyError(error));
        }
      } else {
        setPreview(null);
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
      void loadData();
    });
    return () => window.cancelAnimationFrame(animationFrame);
  }, [loadData]);

  async function confirmAssignment() {
    if (!selectedScheme) return;
    setIsSaving(true);
    setDialogError(null);
    try {
      const nextAssignment = await assignProfitSharingScheme(
        organizationId,
        cycle.id,
        { schemeId: selectedScheme.id },
      );
      setAssignment(nextAssignment);
      setShowConfirm(false);
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
    return <section className={styles.previewPage}><div className={styles.loadingState}><span /><strong>Menyiapkan waterfall siklus...</strong><p>Membaca skema aktif, snapshot assignment, modal terkonfirmasi, dan profitabilitas.</p></div></section>;
  }

  return (
    <section className={styles.previewPage}>
      <div className={styles.previewNotice}>
        <span><Icon name="flow" /></span>
        <div><strong>Preview dinamis · belum menjadi transaksi final</strong><small>Nilai akan berubah mengikuti pendapatan, biaya, dan modal sampai difinalkan pada Stage 3C2.</small></div>
        <button type="button" disabled={isRefreshing} onClick={() => void loadData(true)}><Icon name="refresh" /> {isRefreshing ? "Menghitung..." : "Hitung ulang"}</button>
      </div>

      {pageError && <div className={styles.pageError} role="alert">{pageError}</div>}

      <div className={styles.assignmentGrid}>
        <article className={styles.assignmentCard}>
          <header className={styles.cardHeader}><div><span className={styles.eyebrow}>Assignment siklus</span><h2>{assignment ? `${assignment.schemeName} v${assignment.schemeVersion}` : "Pilih skema aktif"}</h2><p>{assignment ? `Snapshot ${assignment.schemeCode} disimpan ${formatDateTime(assignment.assignedAt)}.` : "Siklus belum memiliki aturan pembagian hasil V2."}</p></div><span className={`${styles.lockBadge} ${availability.allowed ? styles.lockOpen : ""}`}><Icon name={availability.allowed ? "flow" : "lock"} /> {availability.allowed ? assignment ? "Dapat diganti" : "Dapat dipilih" : "Terkunci"}</span></header>

          <label className={styles.schemeSelect}><span>Skema aktif</span><select value={selectedSchemeId} disabled={!canWrite || !availability.allowed || activeSchemes.length === 0} onChange={(event) => setSelectedSchemeId(event.target.value)}>{assignment && !assignedSourceIsActive && <option value={assignment.sourceSchemeId}>{assignment.schemeCode} v{assignment.schemeVersion} · snapshot saat ini</option>}{activeSchemes.length === 0 && <option value="">Belum ada skema aktif</option>}{activeSchemes.map((scheme) => <option value={scheme.id} key={scheme.id}>{scheme.code} v{scheme.version} · {scheme.name}</option>)}</select></label>
          <p className={styles.availabilityNote}>{availability.reason}</p>

          {assignment && (
            <div className={styles.snapshotParticipants}>{assignment.participants.toSorted((left, right) => left.sequence - right.sequence).map((participant) => <span key={participant.id}><b>{participant.participantName}</b><small>{participant.participantCode} · {profitSharingParticipantRoleLabels[participant.participantRole]}</small></span>)}</div>
          )}

          <footer className={styles.assignmentFooter}>
            <small>{activeSchemes.length === 0 ? "Aktifkan minimal satu skema melalui tab Skema V2." : !canWrite ? "Mode baca: izin profit-sharing.write diperlukan." : "Snapshot menjaga aturan musim ini dari perubahan versi berikutnya."}</small>
            {canWrite && availability.allowed && selectedScheme && selectedScheme.id !== assignment?.sourceSchemeId && <button className={styles.primaryButton} type="button" onClick={() => { setDialogError(null); setShowConfirm(true); }}>{assignment ? "Ganti skema" : "Gunakan skema"}<Icon name="arrow" /></button>}
          </footer>
        </article>

        <article className={styles.snapshotCard}>
          <span className={styles.eyebrow}>Alur yang dikunci</span>
          <h2>{assignment ? `${assignment.priorityRules.length + 1} tahap waterfall` : "Menunggu assignment"}</h2>
          {assignment ? <div className={styles.flowList}>{assignment.priorityRules.toSorted((left, right) => left.sequence - right.sequence).map((rule, index) => <div key={rule.id}><i>{index + 1}</i><span><strong>{profitSharingPriorityRuleTypeLabels[rule.ruleType]}</strong><small>{rule.recipientCode} · {formatProfitSharingRate(rule.rateNumerator, rule.rateDenominator)}</small></span></div>)}<div className={styles.residualStep}><i>{assignment.priorityRules.length + 1}</i><span><strong>{profitSharingResidualMethodLabels[assignment.residualMethod]}</strong><small>{assignment.residualMethod === 1 ? assignment.residualRecipientCode : assignment.residualMethod === 2 ? "Menurut modal terkonfirmasi" : `${assignment.residualShares.length} bagian tetap`}</small></span></div></div> : <div className={styles.snapshotEmpty}><Icon name="shield" /><p>Peserta dan aturan akan muncul di sini setelah skema dipilih.</p></div>}
        </article>
      </div>

      {previewError && <div className={styles.previewError} role="alert"><span><Icon name="warning" /></span><div><strong>Preview belum dapat dihitung</strong><p>{previewError}</p><small>Cocokkan kode dan peran pemberi modal dengan peserta skema, lalu pastikan modal terkonfirmasi sama dengan biaya budidaya.</small></div></div>}

      {preview && previewSummary && (
        <>
          <div className={styles.metricGrid}>
            <article><span>Pendapatan diakui</span><strong>{formatSharingCurrency(preview.profitability.recognizedRevenue)}</strong><small>{profitabilityOutcomeLabels[preview.profitability.outcome]}</small></article>
            <article><span>Modal / biaya</span><strong>{formatSharingCurrency(preview.totals.totalCapital)}</strong><small>Biaya {formatSharingCurrency(preview.profitability.totalCultivationCost)}</small></article>
            <article className={preview.profitability.netProfit >= 0 ? styles.metricPrimary : styles.metricDanger}><span>Laba / rugi bersih</span><strong>{formatSharingCurrency(preview.profitability.netProfit)}</strong><small>{preview.calculationVersion}</small></article>
            <article><span>Total uang dialokasikan</span><strong>{formatSharingCurrency(preview.totals.totalPayout)}</strong><small>{previewSummary.isPayoutReconciled ? "Rekonsiliasi seimbang" : "Perlu pemeriksaan"}</small></article>
          </div>

          <div className={styles.reconciliationStrip}>
            <div><span>Modal kembali</span><strong>{formatSharingCurrency(preview.totals.totalCapitalRecovery)}</strong></div>
            <b>+</b>
            <div><span>Total bagian laba</span><strong>{formatSharingCurrency(preview.totals.totalProfitShare)}</strong></div>
            <b>=</b>
            <div><span>Total pembayaran</span><strong>{formatSharingCurrency(preview.totals.totalPayout)}</strong></div>
            {previewSummary.hasCapitalLoss && <em>Kerugian modal {formatSharingCurrency(preview.totals.totalCapitalLoss)}</em>}
          </div>

          <section className={styles.prioritySection}>
            <header><div><span className={styles.eyebrow}>Tahap prioritas</span><h2>Potongan sebelum laba tersisa</h2></div><small>{preview.priorityAllocations.length} aturan · sisa tak teralokasi {formatSharingCurrency(previewSummary.unallocatedPriorityAmount)}</small></header>
            {preview.priorityAllocations.length > 0 ? <div className={styles.priorityGrid}>{preview.priorityAllocations.toSorted((left, right) => left.sequence - right.sequence).map((rule, index) => <article key={`${rule.ruleCode}-${index}`}><i>{index + 1}</i><div><strong>{profitSharingPriorityRuleTypeLabels[rule.ruleType]}</strong><span>{rule.recipientNameSnapshot} · {formatProfitSharingRate(rule.rateNumerator, rule.rateDenominator)}</span></div><dl><div><dt>Dasar</dt><dd>{formatSharingCurrency(rule.baseAmount)}</dd></div><div><dt>Diminta</dt><dd>{formatSharingCurrency(rule.requestedAmount)}</dd></div><div><dt>Dialokasikan</dt><dd>{formatSharingCurrency(rule.allocatedAmount)}</dd></div>{rule.unallocatedAmount > 0 && <div className={styles.unallocated}><dt>Tidak teralokasi</dt><dd>{formatSharingCurrency(rule.unallocatedAmount)}</dd></div>}</dl></article>)}</div> : <div className={styles.inlineEmpty}>Skema ini tidak memiliki potongan prioritas; laba langsung masuk ke pembagian tersisa.</div>}
          </section>

          <section className={styles.allocationSection}>
            <header><div><span className={styles.eyebrow}>Rincian penerima</span><h2>{previewSummary.participantCount} peserta · {previewSummary.fundedParticipantCount} menyetor modal</h2></div><small>Dihitung {formatDateTime(preview.generatedAt)}</small></header>
            <div className={styles.allocationTableShell}>
              <table className={styles.allocationTable}>
                <thead><tr><th>Peserta</th><th>Modal &amp; rasio</th><th>Pemulihan / rugi modal</th><th>Komponen laba</th><th>Total pembayaran</th></tr></thead>
                <tbody>{preview.allocations.toSorted((left, right) => left.sequence - right.sequence).map((allocation) => <tr key={allocation.participantCodeSnapshot}><td data-label="Peserta"><span className={styles.personCell}><i><Icon name={allocation.participantRole === 1 ? "bank" : "user"} /></i><span><strong>{allocation.participantNameSnapshot}</strong><small>{allocation.participantCodeSnapshot} · {profitSharingParticipantRoleLabels[allocation.participantRole]}</small></span></span></td><td data-label="Modal & rasio"><span className={styles.stackCell}><strong>{formatSharingCurrency(allocation.confirmedCapital)}</strong><small>{formatRatio(allocation.capitalRatio)}</small></span></td><td data-label="Pemulihan / rugi modal"><span className={styles.stackCell}><strong>{formatSharingCurrency(allocation.capitalRecovery)}</strong>{allocation.capitalLoss > 0 && <small className={styles.lossText}>Rugi {formatSharingCurrency(allocation.capitalLoss)}</small>}</span></td><td data-label="Komponen laba"><span className={styles.profitCell}><small>Kelola <b>{formatSharingCurrency(allocation.managementProfitShare)}</b></small><small>Imbal modal <b>{formatSharingCurrency(allocation.returnOnCapitalProfitShare)}</b></small><small>Residual <b>{formatSharingCurrency(allocation.residualProfitShare)}</b></small><strong>Total laba {formatSharingCurrency(allocation.totalProfitShare)}</strong></span></td><td data-label="Total pembayaran"><strong className={styles.payout}>{formatSharingCurrency(allocation.totalPayout)}</strong></td></tr>)}</tbody>
              </table>
            </div>
          </section>

          <div className={styles.dynamicWarning}><Icon name="warning" /><span><strong>Preview belum dikunci.</strong> Perubahan pendapatan, biaya, atau modal akan mengubah hasil. Stage 3C2 akan menambahkan finalisasi immutable dan histori.</span></div>
        </>
      )}

      {showConfirm && selectedScheme && (
        <div className={styles.modalBackdrop} role="presentation" onMouseDown={(event) => { if (event.target === event.currentTarget && !isSaving) setShowConfirm(false); }}>
          <div className={styles.modalPanel} role="dialog" aria-modal="true"><AssignmentDialog scheme={selectedScheme} cycle={cycle} isReplacement={assignment !== null} isSaving={isSaving} error={dialogError} onClose={() => setShowConfirm(false)} onConfirm={confirmAssignment} /></div>
        </div>
      )}
    </section>
  );
}
