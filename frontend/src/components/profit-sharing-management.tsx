"use client";

import { useCallback, useEffect, useMemo, useState, type FormEvent } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import {
  ApiError,
  cancelCapitalContribution,
  confirmCapitalContribution,
  createCapitalContribution,
  createProfitSharingSettlement,
  finalizeProfitSharingSettlement,
  getCapitalContributions,
  getCropCycleProfitability,
  getCropCycles,
  getProfitSharingSettlements,
  updateCapitalContribution,
  updateProfitSharingSettlement,
  voidProfitSharingSettlement,
} from "@/lib/api/client";
import type {
  CapitalContribution,
  CapitalContributionPaymentMethod,
  CapitalContributorRole,
  CropCycle,
  CropCycleProfitability,
  Organization,
  ProfitSharingSettlement,
} from "@/lib/api/contracts";
import { cropCycleStatusLabels } from "@/lib/cultivation/crop-cycle-management";
import {
  capitalDraftFrom,
  capitalPaymentMethodLabels,
  capitalStatusLabels,
  contributionDateWindow,
  contributorRoleLabels,
  filterCapitalContributions,
  filterSettlements,
  formatRatio,
  formatSharingCurrency,
  formatSharingDate,
  optionalSharingText,
  parseCapitalAmount,
  profitabilityOutcomeLabels,
  profitPools,
  settlementDraftFrom,
  settlementReadiness,
  settlementStatusLabels,
  summarizeCapital,
  validateCapitalDraft,
  validateSettlementDraft,
  type CapitalDraft,
  type CapitalRoleFilter,
  type CapitalStatusFilter,
  type SettlementDraft,
  type SettlementStatusFilter,
} from "@/lib/finance/profit-sharing-management";
import styles from "./receivable-management.module.css";

type Props = {
  organization: Organization | null;
  organizationId: string | null;
  permissions: string[];
};

type View = "overview" | "capital" | "settlements";
type CapitalEditorState = { contributionId: string | null };
type SettlementEditorState = { settlementId: string | null };
type ActionState =
  | { kind: "confirm-capital" | "cancel-capital"; id: string }
  | { kind: "finalize-settlement" | "void-settlement"; id: string };

type IconName =
  | "add" | "arrow" | "bank" | "calendar" | "check" | "close"
  | "edit" | "invoice" | "money" | "refresh" | "search" | "share"
  | "stop" | "trend" | "user" | "wallet";

const iconPaths: Record<IconName, string> = {
  add: "M12 5v14M5 12h14",
  arrow: "m15 18-6-6 6-6",
  bank: "M3 10h18M5 10v8m4-8v8m6-8v8m4-8v8M2 21h20M12 3 2 8h20L12 3Z",
  calendar: "M6 3v3m12-3v3M4 9h16M5 5h14a1 1 0 0 1 1 1v14H4V6a1 1 0 0 1 1-1Z",
  check: "m5 12 4 4L19 6",
  close: "m6 6 12 12M18 6 6 18",
  edit: "m4 20 4.5-1 10-10a2.1 2.1 0 0 0-3-3l-10 10L4 20Zm10-12 3 3",
  invoice: "M6 3h12v18l-3-2-3 2-3-2-3 2V3Zm3 5h6m-6 4h6m-6 4h4",
  money: "M12 3v18m4-14H9.5a3 3 0 0 0 0 6h5a3 3 0 0 1 0 6H7",
  refresh: "M20 6v5h-5M4 18v-5h5m10-2a7 7 0 0 0-12-4L4 11m16 2-3 4a7 7 0 0 1-12-4",
  search: "m21 21-4.35-4.35m2.35-5.65a8 8 0 1 1-16 0 8 8 0 0 1 16 0Z",
  share: "M8 12a3 3 0 1 0 0-6 3 3 0 0 0 0 6Zm8 6a3 3 0 1 0 0-6 3 3 0 0 0 0 6Zm-5.5-7.5 3 3",
  stop: "M6 6h12v12H6V6Z",
  trend: "m4 17 5-5 4 4 7-8m-5 0h5v5",
  user: "M12 12a4 4 0 1 0 0-8 4 4 0 0 0 0 8ZM4 21c0-4 3-7 8-7s8 3 8 7",
  wallet: "M4 6h14a2 2 0 0 1 2 2v11H4a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2h12m4 7h-5a2 2 0 0 0 0 4h5",
};

function Icon({ name }: { name: IconName }) {
  return <svg viewBox="0 0 24 24" aria-hidden="true"><path d={iconPaths[name]} /></svg>;
}

function localToday(): string {
  const now = new Date();
  return new Date(now.getTime() - now.getTimezoneOffset() * 60_000)
    .toISOString().slice(0, 10);
}

function friendlyError(error: unknown): string {
  if (!(error instanceof ApiError)) {
    return error instanceof Error ? error.message : "Permintaan tidak dapat diselesaikan.";
  }

  const messages: Record<string, string> = {
    "CapitalContributions.CodeAlreadyExists": "Kode setoran modal sudah digunakan pada siklus ini.",
    "CapitalContributions.DateOutOfRange": "Tanggal setoran berada di luar rentang siklus yang diizinkan.",
    "CapitalContributions.ContributorIdentityConflict": "Kode dan nama pemberi modal bertentangan dengan identitas yang sudah tercatat.",
    "CapitalContributions.InvalidStatusTransition": "Tindakan tidak sesuai dengan status setoran modal.",
    "CapitalContributions.FinalizedSettlementExists": "Modal terkunci karena pembagian hasil sudah difinalkan.",
    "ProfitSharingSettlements.CodeAlreadyExists": "Kode pembagian hasil sudah digunakan pada siklus ini.",
    "ProfitSharingSettlements.ActiveSettlementExists": "Siklus ini sudah memiliki pembagian hasil final yang aktif.",
    "ProfitSharingSettlements.CropCycleNotTerminal": "Siklus harus diselesaikan atau dibatalkan sebelum finalisasi.",
    "ProfitSharingSettlements.ActiveActivityExists": "Masih ada aktivitas budidaya yang aktif.",
    "ProfitSharingSettlements.DraftHarvestExists": "Masih ada hasil panen berstatus draf.",
    "ProfitSharingSettlements.UnsoldHarvestExists": "Masih ada hasil panen terkonfirmasi yang belum terjual.",
    "ProfitSharingSettlements.DraftSaleExists": "Masih ada penjualan berstatus draf.",
    "ProfitSharingSettlements.OutstandingReceivableExists": "Masih ada piutang penjualan yang belum lunas.",
    "ProfitSharingSettlements.DraftExpenseExists": "Masih ada biaya budidaya berstatus draf.",
    "ProfitSharingSettlements.DraftContributionExists": "Masih ada setoran modal berstatus draf.",
    "ProfitSharingSettlements.DraftPaymentExists": "Masih ada pembayaran penjualan berstatus draf.",
    "ProfitSharingSettlements.CapitalDoesNotMatchCost": "Total modal terkonfirmasi harus sama dengan total biaya budidaya.",
    "ProfitSharingSettlements.ZeroCostUnsupported": "Siklus tanpa biaya budidaya tidak dapat dibagi hasil.",
    "ProfitSharingSettlements.SourceDataChanged": "Data sumber berubah setelah draf dibuat. Buat draf pembagian hasil baru.",
    "ProfitSharingSettlements.InvalidStatusTransition": "Tindakan tidak sesuai dengan status pembagian hasil.",
    "ProfitSharingSettlements.ConcurrencyConflict": "Data berubah bersamaan dengan finalisasi. Muat ulang lalu coba lagi.",
  };

  return messages[error.problem?.code ?? ""] ?? error.message;
}

function replaceById<T extends { id: string }>(items: T[], updated: T): T[] {
  return items.some((item) => item.id === updated.id)
    ? items.map((item) => item.id === updated.id ? updated : item)
    : [...items, updated];
}

function CapitalEditor({
  cycle,
  contribution,
  isSaving,
  apiError,
  onClose,
  onSubmit,
}: {
  cycle: CropCycle;
  contribution: CapitalContribution | null;
  isSaving: boolean;
  apiError: string | null;
  onClose: () => void;
  onSubmit: (draft: CapitalDraft) => Promise<void>;
}) {
  const isCreate = contribution === null;
  const [draft, setDraft] = useState(() => capitalDraftFrom(contribution, cycle, localToday()));
  const [errors, setErrors] = useState<string[]>([]);
  const window = contributionDateWindow(cycle);

  function update<Key extends keyof CapitalDraft>(key: Key, value: CapitalDraft[Key]) {
    setDraft((current) => ({ ...current, [key]: value }));
  }

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const nextErrors = validateCapitalDraft(draft, cycle, isCreate);
    setErrors(nextErrors);
    if (nextErrors.length === 0) void onSubmit(draft);
  }

  return (
    <form className={styles.editorForm} onSubmit={submit} noValidate>
      <header className={styles.editorHeader}>
        <span className={styles.editorIcon}><Icon name="bank" /></span>
        <div>
          <span className={styles.eyebrow}>{cycle.code} · Modal budidaya</span>
          <h2>{isCreate ? "Catat setoran modal" : `Ubah ${contribution.code}`}</h2>
          <p>Hanya setoran terkonfirmasi yang menjadi dasar pemulihan modal dan bagi hasil.</p>
        </div>
        <button className={styles.iconButton} type="button" aria-label="Tutup formulir" disabled={isSaving} onClick={onClose}><Icon name="close" /></button>
      </header>

      {(errors.length > 0 || apiError) && (
        <div className={styles.formAlert} role="alert">
          <strong>Periksa kembali data berikut:</strong>
          <ul>{errors.map((error) => <li key={error}>{error}</li>)}{apiError && <li>{apiError}</li>}</ul>
        </div>
      )}

      <div className={styles.balancePreview}>
        <span><small>Siklus budidaya</small><strong>{cycle.code}</strong></span>
        <span><small>Rentang setoran</small><strong>{formatSharingDate(window.minimum)} – {formatSharingDate(window.maximum)}</strong></span>
        <i><Icon name="calendar" /></i>
      </div>

      <fieldset disabled={isSaving}>
        <div className={styles.formGrid}>
          <label className={styles.field}>
            <span>Kode setoran <em>*</em></span>
            <input value={draft.code} maxLength={40} disabled={!isCreate} placeholder="Contoh: MOD-001" onChange={(event) => update("code", event.target.value)} />
          </label>
          <label className={styles.field}>
            <span>Tanggal setoran <em>*</em></span>
            <input type="date" value={draft.contributionDate} min={window.minimum} max={window.maximum} onChange={(event) => update("contributionDate", event.target.value)} />
          </label>
          <label className={styles.field}>
            <span>Peran <em>*</em></span>
            <select value={draft.contributorRole} onChange={(event) => update("contributorRole", Number(event.target.value) as CapitalContributorRole)}>
              {Object.entries(contributorRoleLabels).map(([value, label]) => <option value={value} key={value}>{label}</option>)}
            </select>
          </label>
          <label className={styles.field}>
            <span>Jumlah modal <em>*</em></span>
            <input value={draft.amount} inputMode="decimal" placeholder="0" onChange={(event) => update("amount", event.target.value)} />
          </label>
          <label className={styles.field}>
            <span>Kode pemberi modal <em>*</em></span>
            <input value={draft.contributorCode} maxLength={40} placeholder="Contoh: INV-001" onChange={(event) => update("contributorCode", event.target.value)} />
          </label>
          <label className={styles.field}>
            <span>Nama pemberi modal <em>*</em></span>
            <input value={draft.contributorName} maxLength={150} placeholder="Nama investor atau mitra" onChange={(event) => update("contributorName", event.target.value)} />
          </label>
          <label className={styles.field}>
            <span>Metode setoran <em>*</em></span>
            <select value={draft.paymentMethod} onChange={(event) => update("paymentMethod", Number(event.target.value) as CapitalContributionPaymentMethod)}>
              {Object.entries(capitalPaymentMethodLabels).map(([value, label]) => <option value={value} key={value}>{label}</option>)}
            </select>
          </label>
          <label className={styles.field}>
            <span>Nomor referensi</span>
            <input value={draft.referenceNumber} maxLength={100} placeholder="Nomor transfer atau kuitansi" onChange={(event) => update("referenceNumber", event.target.value)} />
          </label>
          <label className={`${styles.field} ${styles.fieldFull}`}>
            <span>Catatan</span>
            <textarea value={draft.notes} maxLength={1000} rows={4} placeholder="Keterangan tambahan" onChange={(event) => update("notes", event.target.value)} />
          </label>
        </div>
      </fieldset>

      <footer className={styles.formActions}>
        <button className={styles.secondaryButton} type="button" disabled={isSaving} onClick={onClose}>Batal</button>
        <button className={styles.primaryButton} type="submit" disabled={isSaving}>{isSaving ? "Menyimpan..." : isCreate ? "Simpan draf" : "Simpan perubahan"}</button>
      </footer>
    </form>
  );
}

function SettlementEditor({
  settlement,
  contributions,
  isSaving,
  apiError,
  onClose,
  onSubmit,
}: {
  settlement: ProfitSharingSettlement | null;
  contributions: CapitalContribution[];
  isSaving: boolean;
  apiError: string | null;
  onClose: () => void;
  onSubmit: (draft: SettlementDraft) => Promise<void>;
}) {
  const isCreate = settlement === null;
  const [draft, setDraft] = useState(() => settlementDraftFrom(settlement, contributions, localToday()));
  const [errors, setErrors] = useState<string[]>([]);

  function update<Key extends keyof SettlementDraft>(key: Key, value: SettlementDraft[Key]) {
    setDraft((current) => ({ ...current, [key]: value }));
  }

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const nextErrors = validateSettlementDraft(draft, isCreate);
    setErrors(nextErrors);
    if (nextErrors.length === 0) void onSubmit(draft);
  }

  return (
    <form className={styles.editorForm} onSubmit={submit} noValidate>
      <header className={styles.editorHeader}>
        <span className={styles.editorIcon}><Icon name="share" /></span>
        <div>
          <span className={styles.eyebrow}>Snapshot pembagian hasil</span>
          <h2>{isCreate ? "Buat draf pembagian hasil" : `Ubah ${settlement.code}`}</h2>
          <p>Draf menyimpan snapshot pendapatan, biaya, modal, dan alokasi untuk diperiksa sebelum final.</p>
        </div>
        <button className={styles.iconButton} type="button" aria-label="Tutup formulir" disabled={isSaving} onClick={onClose}><Icon name="close" /></button>
      </header>

      {(errors.length > 0 || apiError) && (
        <div className={styles.formAlert} role="alert">
          <strong>Periksa kembali data berikut:</strong>
          <ul>{errors.map((error) => <li key={error}>{error}</li>)}{apiError && <li>{apiError}</li>}</ul>
        </div>
      )}

      <fieldset disabled={isSaving}>
        <div className={styles.formGrid}>
          <label className={styles.field}>
            <span>Kode pembagian <em>*</em></span>
            <input value={draft.code} maxLength={40} disabled={!isCreate} placeholder="Contoh: BH-001" onChange={(event) => update("code", event.target.value)} />
          </label>
          <label className={styles.field}>
            <span>Tanggal pembagian <em>*</em></span>
            <input type="date" value={draft.settlementDate} onChange={(event) => update("settlementDate", event.target.value)} />
          </label>
          <label className={styles.field}>
            <span>Kode mitra pengelola <em>*</em></span>
            <input value={draft.managingPartnerCode} maxLength={40} disabled={!isCreate} placeholder="Contoh: MIT-001" onChange={(event) => update("managingPartnerCode", event.target.value)} />
          </label>
          <label className={styles.field}>
            <span>Nama mitra pengelola <em>*</em></span>
            <input value={draft.managingPartnerName} maxLength={150} disabled={!isCreate} placeholder="Nama mitra pengelola" onChange={(event) => update("managingPartnerName", event.target.value)} />
          </label>
          <label className={`${styles.field} ${styles.fieldFull}`}>
            <span>Catatan</span>
            <textarea value={draft.notes} maxLength={1000} rows={4} placeholder="Catatan pemeriksaan dan kesepakatan" onChange={(event) => update("notes", event.target.value)} />
          </label>
        </div>
      </fieldset>

      <footer className={styles.formActions}>
        <button className={styles.secondaryButton} type="button" disabled={isSaving} onClick={onClose}>Batal</button>
        <button className={styles.primaryButton} type="submit" disabled={isSaving}>{isSaving ? "Menyimpan..." : isCreate ? "Buat snapshot draf" : "Simpan perubahan"}</button>
      </footer>
    </form>
  );
}

function ConfirmationDialog({
  action,
  isSaving,
  apiError,
  onClose,
  onSubmit,
}: {
  action: ActionState;
  isSaving: boolean;
  apiError: string | null;
  onClose: () => void;
  onSubmit: (reason: string) => Promise<void>;
}) {
  const needsReason = action.kind === "cancel-capital" || action.kind === "void-settlement";
  const isFinal = action.kind === "finalize-settlement";
  const [reason, setReason] = useState("");
  const [validationError, setValidationError] = useState<string | null>(null);

  const title = action.kind === "confirm-capital"
    ? "Konfirmasi setoran modal?"
    : action.kind === "cancel-capital"
      ? "Batalkan setoran modal?"
      : isFinal ? "Finalisasi pembagian hasil?" : "Batalkan pembagian final?";

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const normalized = reason.trim();
    if (needsReason && !normalized) {
      setValidationError("Alasan wajib diisi.");
      return;
    }
    if (normalized.length > 500) {
      setValidationError("Alasan maksimal 500 karakter.");
      return;
    }
    void onSubmit(normalized);
  }

  return (
    <form className={styles.actionDialog} onSubmit={submit} noValidate>
      <div className={`${styles.actionIcon} ${needsReason ? styles.actionIconDanger : ""}`}><Icon name={needsReason ? "stop" : "check"} /></div>
      <span className={styles.eyebrow}>{isFinal ? "Keputusan tidak dapat diedit" : "Konfirmasi tindakan"}</span>
      <h2>{title}</h2>
      <p>{isFinal
        ? "Server akan memeriksa siklus, aktivitas, panen, penjualan, piutang, biaya, pembayaran, dan modal sekali lagi sebelum mengunci snapshot."
        : needsReason
          ? "Tindakan ini disimpan sebagai jejak audit dan mengubah status transaksi."
          : "Setoran terkonfirmasi akan masuk ke total modal dan dasar pembagian hasil."}</p>
      {needsReason && (
        <label className={styles.field}>
          <span>Alasan <em>*</em></span>
          <textarea value={reason} maxLength={500} rows={4} disabled={isSaving} onChange={(event) => setReason(event.target.value)} />
        </label>
      )}
      {(validationError || apiError) && <div className={styles.formAlert} role="alert">{validationError ?? apiError}</div>}
      <div className={styles.actionButtons}>
        <button className={styles.secondaryButton} type="button" disabled={isSaving} onClick={onClose}>Kembali</button>
        <button className={needsReason ? styles.dangerButton : styles.primaryButton} type="submit" disabled={isSaving}>{isSaving ? "Memproses..." : "Ya, lanjutkan"}</button>
      </div>
    </form>
  );
}

export function ProfitSharingManagement({ organization, organizationId, permissions }: Props) {
  const router = useRouter();
  const searchParams = useSearchParams();
  const canReadFinance = permissions.includes("finance.read");
  const canWriteFinance = permissions.includes("finance.write");
  const canReadSharing = permissions.includes("profit-sharing.read");
  const canWriteSharing = permissions.includes("profit-sharing.write");
  const canFinalize = permissions.includes("profit-sharing.finalize");
  const canVoid = permissions.includes("profit-sharing.void");

  const [cycles, setCycles] = useState<CropCycle[]>([]);
  const [selectedCycleId, setSelectedCycleId] = useState("");
  const [profitability, setProfitability] = useState<CropCycleProfitability | null>(null);
  const [contributions, setContributions] = useState<CapitalContribution[]>([]);
  const [settlements, setSettlements] = useState<ProfitSharingSettlement[]>([]);
  const [view, setView] = useState<View>("overview");
  const [query, setQuery] = useState("");
  const [capitalStatus, setCapitalStatus] = useState<CapitalStatusFilter>("all");
  const [capitalRole, setCapitalRole] = useState<CapitalRoleFilter>("all");
  const [settlementStatus, setSettlementStatus] = useState<SettlementStatusFilter>("all");
  const [capitalEditor, setCapitalEditor] = useState<CapitalEditorState | null>(null);
  const [settlementEditor, setSettlementEditor] = useState<SettlementEditorState | null>(null);
  const [action, setAction] = useState<ActionState | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [pageError, setPageError] = useState<string | null>(null);
  const [modalError, setModalError] = useState<string | null>(null);

  const selectedCycle = useMemo(
    () => cycles.find((cycle) => cycle.id === selectedCycleId) ?? null,
    [cycles, selectedCycleId],
  );

  const capitalSummary = useMemo(() => summarizeCapital(contributions), [contributions]);
  const pools = useMemo(() => profitPools(profitability), [profitability]);
  const readiness = useMemo(
    () => selectedCycle
      ? settlementReadiness(selectedCycle, profitability, contributions, settlements)
      : [],
    [selectedCycle, profitability, contributions, settlements],
  );
  const isReady = readiness.length > 0 && readiness.every((item) => item.ready);
  const canCreateSnapshot = readiness
    .filter((item) => item.key === "cost" || item.key === "capital")
    .every((item) => item.ready) && profitability !== null;
  const filteredCapital = useMemo(
    () => filterCapitalContributions(contributions, query, capitalStatus, capitalRole),
    [contributions, query, capitalStatus, capitalRole],
  );
  const filteredSettlements = useMemo(
    () => filterSettlements(settlements, settlementStatus),
    [settlements, settlementStatus],
  );

  const loadCycleData = useCallback(async (
    nextOrganizationId: string,
    cycleId: string,
    background = false,
  ) => {
    if (background) setIsRefreshing(true);
    else setIsLoading(true);
    setPageError(null);
    try {
      const [nextProfitability, nextContributions, nextSettlements] = await Promise.all([
        canReadFinance ? getCropCycleProfitability(nextOrganizationId, cycleId) : Promise.resolve(null),
        canReadFinance ? getCapitalContributions(nextOrganizationId, cycleId) : Promise.resolve([]),
        canReadSharing ? getProfitSharingSettlements(nextOrganizationId, cycleId) : Promise.resolve([]),
      ]);
      setProfitability(nextProfitability);
      setContributions(nextContributions);
      setSettlements(nextSettlements);
    } catch (error) {
      setPageError(friendlyError(error));
    } finally {
      setIsLoading(false);
      setIsRefreshing(false);
    }
  }, [canReadFinance, canReadSharing]);

  useEffect(() => {
    if (!organizationId) {
      return;
    }
    let cancelled = false;
    async function loadCycles() {
      setIsLoading(true);
      setPageError(null);
      try {
        const nextCycles = await getCropCycles(organizationId!);
        if (cancelled) return;
        setCycles(nextCycles);
        const requested = searchParams.get("cycle");
        const selected = nextCycles.find((cycle) => cycle.id === requested)?.id
          ?? nextCycles[0]?.id
          ?? "";
        setSelectedCycleId(selected);
        if (!selected) setIsLoading(false);
      } catch (error) {
        if (!cancelled) {
          setPageError(friendlyError(error));
          setIsLoading(false);
        }
      }
    }
    void loadCycles();
    return () => { cancelled = true; };
  }, [organizationId, searchParams]);

  useEffect(() => {
    if (organizationId && selectedCycleId) {
      const animationFrame = window.requestAnimationFrame(() => {
        void loadCycleData(organizationId, selectedCycleId);
      });
      return () => window.cancelAnimationFrame(animationFrame);
    }
  }, [organizationId, selectedCycleId, loadCycleData]);

  function selectCycle(cycleId: string) {
    setSelectedCycleId(cycleId);
    setCapitalEditor(null);
    setSettlementEditor(null);
    setAction(null);
    const next = new URLSearchParams(searchParams.toString());
    if (cycleId) next.set("cycle", cycleId);
    else next.delete("cycle");
    router.replace(`/profit-sharing${next.size > 0 ? `?${next.toString()}` : ""}`);
  }

  async function submitCapital(draft: CapitalDraft) {
    if (!organizationId || !selectedCycle || !capitalEditor) return;
    setIsSaving(true);
    setModalError(null);
    const amount = parseCapitalAmount(draft.amount);
    if (amount === null) {
      setIsSaving(false);
      return;
    }
    const request = {
      contributionDate: draft.contributionDate,
      contributorCode: draft.contributorCode.trim().toUpperCase(),
      contributorName: draft.contributorName.trim(),
      contributorRole: draft.contributorRole,
      amount,
      paymentMethod: draft.paymentMethod,
      referenceNumber: optionalSharingText(draft.referenceNumber),
      notes: optionalSharingText(draft.notes),
    };
    try {
      const updated = capitalEditor.contributionId
        ? await updateCapitalContribution(organizationId, selectedCycle.id, capitalEditor.contributionId, request)
        : await createCapitalContribution(organizationId, selectedCycle.id, {
          code: draft.code.trim().toUpperCase(),
          ...request,
        });
      setContributions((current) => replaceById(current, updated));
      setCapitalEditor(null);
      await loadCycleData(organizationId, selectedCycle.id, true);
    } catch (error) {
      setModalError(friendlyError(error));
    } finally {
      setIsSaving(false);
    }
  }

  async function submitSettlement(draft: SettlementDraft) {
    if (!organizationId || !selectedCycle || !settlementEditor) return;
    setIsSaving(true);
    setModalError(null);
    try {
      const updated = settlementEditor.settlementId
        ? await updateProfitSharingSettlement(
          organizationId,
          selectedCycle.id,
          settlementEditor.settlementId,
          { settlementDate: draft.settlementDate, notes: optionalSharingText(draft.notes) },
        )
        : await createProfitSharingSettlement(organizationId, selectedCycle.id, {
          code: draft.code.trim().toUpperCase(),
          settlementDate: draft.settlementDate,
          managingPartnerCode: draft.managingPartnerCode.trim().toUpperCase(),
          managingPartnerName: draft.managingPartnerName.trim(),
          notes: optionalSharingText(draft.notes),
        });
      setSettlements((current) => replaceById(current, updated));
      setSettlementEditor(null);
      setView("settlements");
    } catch (error) {
      setModalError(friendlyError(error));
    } finally {
      setIsSaving(false);
    }
  }

  async function submitAction(reason: string) {
    if (!organizationId || !selectedCycle || !action) return;
    setIsSaving(true);
    setModalError(null);
    try {
      if (action.kind === "confirm-capital") {
        const updated = await confirmCapitalContribution(organizationId, selectedCycle.id, action.id);
        setContributions((current) => replaceById(current, updated));
      } else if (action.kind === "cancel-capital") {
        const updated = await cancelCapitalContribution(
          organizationId,
          selectedCycle.id,
          action.id,
          { cancellationReason: reason },
        );
        setContributions((current) => replaceById(current, updated));
      } else if (action.kind === "finalize-settlement") {
        const updated = await finalizeProfitSharingSettlement(organizationId, selectedCycle.id, action.id);
        setSettlements((current) => replaceById(current, updated));
      } else {
        const updated = await voidProfitSharingSettlement(
          organizationId,
          selectedCycle.id,
          action.id,
          { voidReason: reason },
        );
        setSettlements((current) => replaceById(current, updated));
      }
      setAction(null);
      await loadCycleData(organizationId, selectedCycle.id, true);
    } catch (error) {
      setModalError(friendlyError(error));
    } finally {
      setIsSaving(false);
    }
  }

  const editingCapital = capitalEditor?.contributionId
    ? contributions.find((item) => item.id === capitalEditor.contributionId) ?? null
    : null;
  const editingSettlement = settlementEditor?.settlementId
    ? settlements.find((item) => item.id === settlementEditor.settlementId) ?? null
    : null;

  if (!organizationId || !organization) {
    return <section className={styles.accessState}><span className={styles.editorIcon}><Icon name="share" /></span><h1>Pembagian hasil belum tersedia</h1><p>Pilih organisasi aktif untuk melihat profitabilitas dan hak investor–mitra.</p></section>;
  }

  if (!canReadFinance && !canReadSharing) {
    return <section className={styles.accessState}><span className={styles.editorIcon}><Icon name="share" /></span><h1>Akses dibatasi</h1><p>Anda memerlukan izin keuangan atau pembagian hasil untuk membuka halaman ini.</p></section>;
  }

  return (
    <section className={styles.financePage}>
      <header className={styles.hero}>
        <div>
          <button className={styles.backButton} type="button" onClick={() => router.push("/finance")}><Icon name="arrow" /> Kembali ke keuangan</button>
          <span className={styles.eyebrow}>Profitabilitas · Modal · Hak usaha</span>
          <h1>Profitabilitas &amp; pembagian hasil</h1>
          <p>Audit pendapatan dan biaya per siklus, cocokkan sumber modal, lalu finalkan hak investor dan mitra pengelola dari satu snapshot yang terlindungi.</p>
        </div>
        <div className={styles.heroActions}>
          {(!canWriteFinance || !canWriteSharing) && <span className={styles.readOnlyBadge}>Sebagian mode baca</span>}
          <button className={styles.secondaryButton} type="button" disabled={!selectedCycleId || isRefreshing} onClick={() => selectedCycleId && void loadCycleData(organizationId, selectedCycleId, true)}><Icon name="refresh" /> {isRefreshing ? "Memuat..." : "Muat ulang"}</button>
        </div>
      </header>

      <div className={`${styles.toolbar} ${styles.profitToolbar}`}>
        <label className={styles.filterField}>
          <span>Siklus budidaya</span>
          <select value={selectedCycleId} disabled={cycles.length === 0} onChange={(event) => selectCycle(event.target.value)}>
            {cycles.length === 0 && <option value="">Belum ada siklus</option>}
            {cycles.map((cycle) => <option value={cycle.id} key={cycle.id}>{cycle.code} · {cycle.name} · {cropCycleStatusLabels[cycle.status]}</option>)}
          </select>
        </label>
        <div className={`${styles.heroActions} ${styles.profitTabs}`} role="tablist" aria-label="Bagian pembagian hasil">
          <button aria-selected={view === "overview"} className={view === "overview" ? styles.primaryButton : styles.secondaryButton} role="tab" type="button" onClick={() => setView("overview")}>Profitabilitas</button>
          {canReadFinance && <button aria-selected={view === "capital"} className={view === "capital" ? styles.primaryButton : styles.secondaryButton} role="tab" type="button" onClick={() => setView("capital")}>Modal</button>}
          {canReadSharing && <button aria-selected={view === "settlements"} className={view === "settlements" ? styles.primaryButton : styles.secondaryButton} role="tab" type="button" onClick={() => setView("settlements")}>Pembagian hasil</button>}
        </div>
      </div>

      {pageError && <div className={styles.pageError} role="alert">{pageError}</div>}
      {isLoading ? (
        <div className={styles.loadingState}><span /><strong>Memuat data profitabilitas...</strong><p>SiPacul sedang menyatukan pendapatan, biaya, modal, dan settlement.</p></div>
      ) : !selectedCycle ? (
        <div className={styles.emptyState}><span className={styles.editorIcon}><Icon name="trend" /></span><h2>Belum ada siklus budidaya</h2><p>Buat siklus budidaya sebelum mencatat modal dan pembagian hasil.</p></div>
      ) : view === "overview" ? (
        <>
          {profitability ? (
            <>
              <div className={styles.metricGrid}>
                <article className={styles.metricCard}><span>Pendapatan diakui</span><strong>{formatSharingCurrency(profitability.recognizedRevenue)}</strong><small>Kas {formatSharingCurrency(profitability.collectedRevenue)}</small></article>
                <article className={styles.metricCard}><span>Biaya budidaya</span><strong>{formatSharingCurrency(profitability.totalCultivationCost)}</strong><small>Aktivitas + pengeluaran manual</small></article>
                <article className={`${styles.metricCard} ${styles.metricPrimary}`}><span>Laba / rugi bersih</span><strong>{formatSharingCurrency(profitability.netProfit)}</strong><small>{profitabilityOutcomeLabels[profitability.outcome]} · margin {profitability.profitMarginPercentage === null ? "—" : `${profitability.profitMarginPercentage.toLocaleString("id-ID", { maximumFractionDigits: 2 })}%`}</small></article>
                <article className={`${styles.metricCard} ${profitability.outstandingReceivable > 0 ? styles.metricWarning : ""}`}><span>Piutang tersisa</span><strong>{formatSharingCurrency(profitability.outstandingReceivable)}</strong><small>{profitability.outstandingReceivable === 0 ? "Siap diperiksa untuk finalisasi" : "Harus lunas sebelum finalisasi"}</small></article>
              </div>

              <div className={`${styles.managementGrid} ${styles.profitOverviewGrid}`}>
                <article className={styles.receivableDetail}>
                  <header className={styles.detailHeader}>
                    <div className={styles.detailIdentity}>
                      <span className={styles.detailIcon}><Icon name="share" /></span>
                      <div><span className={styles.eyebrow}>Formula SiPacul · SIPACUL-PS-1</span><h2>Pratinjau pool keuntungan</h2><p>Dihitung dari laba bersih; nilai resmi tersimpan saat draf dibuat.</p></div>
                    </div>
                  </header>
                  <div className={`${styles.amountGrid} ${styles.profitPoolGrid}`}>
                    <div><span>Hak pengelolaan mitra (1/3)</span><strong>{formatSharingCurrency(pools.management)}</strong></div>
                    <div><span>Pool pemilik modal (2/3)</span><strong>{formatSharingCurrency(pools.capital)}</strong></div>
                    <div><span>Modal investor</span><strong>{formatSharingCurrency(capitalSummary.investor)}</strong></div>
                    <div><span>Modal mitra</span><strong>{formatSharingCurrency(capitalSummary.partner)}</strong></div>
                  </div>
                  <p className={styles.notice}>Jika mitra tidak menyetor modal, mitra hanya menerima 1/3 laba sebagai hak pengelolaan. Jika mitra ikut menyetor modal, mitra juga memperoleh bagian proporsional dari pool 2/3 berdasarkan modal terkonfirmasi.</p>
                </article>

                <article className={styles.receivableDetail}>
                  <header className={styles.detailHeader}>
                    <div className={styles.detailIdentity}>
                      <span className={styles.detailIcon}><Icon name="check" /></span>
                      <div><span className={styles.eyebrow}>Kesiapan finalisasi</span><h2>{readiness.filter((item) => item.ready).length}/{readiness.length} pemeriksaan siap</h2><p>Server tetap memeriksa semua sumber transaksi saat finalisasi.</p></div>
                    </div>
                  </header>
                  <div className={`${styles.paymentList} ${styles.profitReadinessList}`}>
                    {readiness.map((item) => (
                      <div className={styles.paymentCard} key={item.key}>
                        <div className={styles.paymentMain}>
                          <span className={styles.paymentIcon}><Icon name={item.ready ? "check" : "stop"} /></span>
                          <div><strong>{item.label}</strong><span>{item.detail}</span></div>
                        </div>
                        <span className={`${styles.stateBadge} ${styles[`state${item.ready ? 3 : 2}`]}`}>{item.ready ? "Siap" : "Belum"}</span>
                      </div>
                    ))}
                  </div>
                  {canWriteSharing && canReadSharing && (
                    <footer className={styles.detailFooter}>
                      <button type="button" disabled={!canCreateSnapshot} onClick={() => { setModalError(null); setSettlementEditor({ settlementId: null }); }}>Buat snapshot draf <Icon name="add" /></button>
                      {!canCreateSnapshot
                        ? <small>Samakan modal terkonfirmasi dengan biaya budidaya terlebih dahulu.</small>
                        : !isReady && <small>Snapshot dapat diaudit; seluruh syarat lain wajib terpenuhi saat finalisasi.</small>}
                    </footer>
                  )}
                </article>
              </div>
            </>
          ) : (
            <div className={styles.notice}>Laporan profitabilitas memerlukan izin <strong>finance.read</strong>. Riwayat pembagian hasil tetap dapat dibaca sesuai izin Anda.</div>
          )}
        </>
      ) : view === "capital" ? (
        <>
          <div className={styles.metricGrid}>
            <article className={`${styles.metricCard} ${styles.metricPrimary}`}><span>Modal terkonfirmasi</span><strong>{formatSharingCurrency(capitalSummary.total)}</strong><small>{capitalSummary.total === (profitability?.totalCultivationCost ?? -1) ? "Sesuai biaya budidaya" : "Belum sama dengan biaya"}</small></article>
            <article className={styles.metricCard}><span>Modal investor</span><strong>{formatSharingCurrency(capitalSummary.investor)}</strong><small>Dasar bagian modal investor</small></article>
            <article className={styles.metricCard}><span>Modal mitra</span><strong>{formatSharingCurrency(capitalSummary.partner)}</strong><small>Tambahan atas hak pengelolaan</small></article>
            <article className={`${styles.metricCard} ${capitalSummary.draftCount > 0 ? styles.metricWarning : ""}`}><span>Modal draf</span><strong>{formatSharingCurrency(capitalSummary.draft)}</strong><small>{capitalSummary.draftCount} transaksi belum dikonfirmasi</small></article>
          </div>

          <div className={styles.toolbar}>
            <label className={styles.searchField}><Icon name="search" /><input value={query} placeholder="Cari kode atau pemberi modal" onChange={(event) => setQuery(event.target.value)} /></label>
            <label className={styles.filterField}><span>Status</span><select value={capitalStatus} onChange={(event) => setCapitalStatus(event.target.value === "all" ? "all" : Number(event.target.value) as CapitalStatusFilter)}><option value="all">Semua</option>{Object.entries(capitalStatusLabels).map(([value, label]) => <option value={value} key={value}>{label}</option>)}</select></label>
            <label className={styles.filterField}><span>Peran</span><select value={capitalRole} onChange={(event) => setCapitalRole(event.target.value === "all" ? "all" : Number(event.target.value) as CapitalRoleFilter)}><option value="all">Semua</option>{Object.entries(contributorRoleLabels).map(([value, label]) => <option value={value} key={value}>{label}</option>)}</select></label>
            {canWriteFinance && <button className={styles.primaryButton} type="button" onClick={() => { setModalError(null); setCapitalEditor({ contributionId: null }); }}><Icon name="add" /> Catat modal</button>}
          </div>

          <div className={styles.resultCount}>{filteredCapital.length} dari {contributions.length} setoran</div>
          {filteredCapital.length > 0 ? (
            <div className={styles.capitalTableShell}>
              <table className={styles.capitalTable}>
                <thead>
                  <tr>
                    <th scope="col">Kode &amp; tanggal</th>
                    <th scope="col">Pemberi modal</th>
                    <th scope="col">Peran</th>
                    <th scope="col">Jumlah &amp; metode</th>
                    <th scope="col">Status</th>
                    <th scope="col">Tindakan</th>
                  </tr>
                </thead>
                <tbody>
                  {filteredCapital.map((item) => (
                    <tr key={item.id}>
                      <td data-label="Kode & tanggal">
                        <span className={styles.capitalCell}>
                          <strong className={styles.eyebrow}>{item.code}</strong>
                          <small>{formatSharingDate(item.contributionDate)}</small>
                        </span>
                      </td>
                      <td data-label="Pemberi modal">
                        <span className={styles.capitalCell}>
                          <strong>{item.contributorName}</strong>
                          <small>{item.contributorCode}</small>
                        </span>
                      </td>
                      <td data-label="Peran">
                        <span className={styles.capitalCell}><strong>{contributorRoleLabels[item.contributorRole]}</strong></span>
                      </td>
                      <td data-label="Jumlah & metode">
                        <span className={styles.capitalCell}>
                          <strong className={styles.capitalAmount}>{formatSharingCurrency(item.amount)}</strong>
                          <small>{capitalPaymentMethodLabels[item.paymentMethod]}</small>
                        </span>
                      </td>
                      <td data-label="Status">
                        <span className={styles.capitalCell}>
                          <span className={`${styles.stateBadge} ${styles[`capitalStatus${item.status}`]}`}>{capitalStatusLabels[item.status]}</span>
                          {item.cancellationReason && <small className={styles.capitalCancellation}>Dibatalkan: {item.cancellationReason}</small>}
                        </span>
                      </td>
                      <td data-label="Tindakan">
                        {canWriteFinance && item.status === 1 && (
                          <div className={styles.capitalActions}>
                            <button type="button" onClick={() => { setModalError(null); setCapitalEditor({ contributionId: item.id }); }}><Icon name="edit" /> Ubah</button>
                            <button className={styles.confirmTextButton} type="button" onClick={() => { setModalError(null); setAction({ kind: "confirm-capital", id: item.id }); }}><Icon name="check" /> Konfirmasi</button>
                            <button className={styles.dangerTextButton} type="button" onClick={() => { setModalError(null); setAction({ kind: "cancel-capital", id: item.id }); }}><Icon name="stop" /> Batalkan</button>
                          </div>
                        )}
                        {canWriteFinance && item.status === 2 && (
                          <div className={styles.capitalActions}><button className={styles.dangerTextButton} type="button" onClick={() => { setModalError(null); setAction({ kind: "cancel-capital", id: item.id }); }}><Icon name="stop" /> Batalkan setoran</button></div>
                        )}
                        {(!canWriteFinance || item.status === 3) && <span className={styles.capitalNoAction}>—</span>}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          ) : (
            <div className={`${styles.emptyState} ${styles.capitalEmptyState}`}><span className={styles.editorIcon}><Icon name="bank" /></span><h2>Belum ada setoran yang cocok</h2><p>Ubah filter atau catat modal investor dan mitra untuk siklus ini.</p></div>
          )}
        </>
      ) : (
        <>
          <div className={styles.toolbar}>
            <label className={styles.filterField}><span>Status</span><select value={settlementStatus} onChange={(event) => setSettlementStatus(event.target.value === "all" ? "all" : Number(event.target.value) as SettlementStatusFilter)}><option value="all">Semua</option>{Object.entries(settlementStatusLabels).map(([value, label]) => <option value={value} key={value}>{label}</option>)}</select></label>
            {canWriteSharing && <button className={styles.primaryButton} type="button" disabled={!canCreateSnapshot} onClick={() => { setModalError(null); setSettlementEditor({ settlementId: null }); }}><Icon name="add" /> Buat draf</button>}
          </div>

          <div className={`${styles.managementGrid} ${styles.profitSettlementGrid}`}>
            {filteredSettlements.map((settlement) => (
              <article className={styles.receivableDetail} key={settlement.id}>
                <header className={styles.detailHeader}>
                  <div className={styles.detailIdentity}>
                    <span className={styles.detailIcon}><Icon name="invoice" /></span>
                    <div><span className={styles.eyebrow}>{settlement.code} · {formatSharingDate(settlement.settlementDate)}</span><h2>{settlement.managingPartnerName}</h2><p>Mitra pengelola {settlement.managingPartnerCode} · {settlement.calculationVersion}</p></div>
                  </div>
                  <span className={`${styles.stateBadge} ${styles[`state${settlement.status}`]}`}>{settlementStatusLabels[settlement.status]}</span>
                </header>
                <div className={styles.amountGrid}>
                  <div><span>Laba / rugi bersih</span><strong>{formatSharingCurrency(settlement.netProfit)}</strong></div>
                  <div><span>Total modal kembali</span><strong>{formatSharingCurrency(settlement.totalCapitalRecovery)}</strong></div>
                  <div><span>Bagi hasil investor</span><strong>{formatSharingCurrency(settlement.totalInvestorProfitShare)}</strong></div>
                  <div><span>Bagi hasil mitra</span><strong>{formatSharingCurrency(settlement.totalPartnerProfitShare)}</strong></div>
                  <div className={styles.amountTotal}><span>Total pembayaran</span><strong>{formatSharingCurrency(settlement.totalPayout)}</strong></div>
                </div>
                <section className={styles.paymentSection}>
                  <header><div><span className={styles.eyebrow}>Rincian penerima</span><h3>{settlement.allocations.length} alokasi</h3></div></header>
                  <div className={styles.paymentList}>
                    {settlement.allocations.map((allocation) => (
                      <div className={styles.paymentCard} key={allocation.id}>
                        <div className={styles.paymentMain}>
                          <span className={styles.paymentIcon}><Icon name={allocation.contributorRole === 1 ? "bank" : "user"} /></span>
                          <div>
                            <strong>{allocation.contributorNameSnapshot}</strong>
                            <span>{contributorRoleLabels[allocation.contributorRole]} · modal {formatSharingCurrency(allocation.confirmedCapital)} · rasio {formatRatio(allocation.capitalRatio)}</span>
                            <span>Pemulihan {formatSharingCurrency(allocation.capitalRecovery)} · pengelolaan {formatSharingCurrency(allocation.managementProfitShare)} · bagian modal {formatSharingCurrency(allocation.capitalProfitShare)}</span>
                          </div>
                        </div>
                        <strong className={styles.paymentAmount}>{formatSharingCurrency(allocation.totalPayout)}</strong>
                      </div>
                    ))}
                  </div>
                </section>
                {settlement.voidReason && <p className={styles.cancellationNote}>Dibatalkan: {settlement.voidReason}</p>}
                <footer className={styles.detailFooter}>
                  <div className={styles.detailActions}>
                    {canWriteSharing && settlement.status === 1 && <button type="button" onClick={() => { setModalError(null); setSettlementEditor({ settlementId: settlement.id }); }}><Icon name="edit" /> Ubah draf</button>}
                    {canFinalize && settlement.status === 1 && <button className={styles.confirmTextButton} type="button" onClick={() => { setModalError(null); setAction({ kind: "finalize-settlement", id: settlement.id }); }}><Icon name="check" /> Finalisasi</button>}
                    {canVoid && settlement.status === 2 && settlement.isActive && <button className={styles.dangerTextButton} type="button" onClick={() => { setModalError(null); setAction({ kind: "void-settlement", id: settlement.id }); }}><Icon name="stop" /> Batalkan final</button>}
                  </div>
                  <small>{settlement.status === 2 ? `Final ${formatSharingDate(settlement.finalizedAt)}` : profitabilityOutcomeLabels[settlement.outcome]}</small>
                </footer>
              </article>
            ))}
            {filteredSettlements.length === 0 && <div className={styles.emptyState}><span className={styles.editorIcon}><Icon name="share" /></span><h2>Belum ada pembagian hasil</h2><p>Buat snapshot draf setelah pendapatan, biaya, dan modal siklus telah diperiksa.</p></div>}
          </div>
        </>
      )}

      {(capitalEditor || settlementEditor || action) && (
        <div className={styles.modalBackdrop} role="presentation" onMouseDown={(event) => { if (event.target === event.currentTarget && !isSaving) { setCapitalEditor(null); setSettlementEditor(null); setAction(null); } }}>
          <div className={styles.modalPanel} role="dialog" aria-modal="true">
            {capitalEditor && selectedCycle && <CapitalEditor key={capitalEditor.contributionId ?? "new-capital"} cycle={selectedCycle} contribution={editingCapital} isSaving={isSaving} apiError={modalError} onClose={() => setCapitalEditor(null)} onSubmit={submitCapital} />}
            {settlementEditor && <SettlementEditor key={settlementEditor.settlementId ?? "new-settlement"} settlement={editingSettlement} contributions={contributions} isSaving={isSaving} apiError={modalError} onClose={() => setSettlementEditor(null)} onSubmit={submitSettlement} />}
            {action && <ConfirmationDialog key={`${action.kind}-${action.id}`} action={action} isSaving={isSaving} apiError={modalError} onClose={() => setAction(null)} onSubmit={submitAction} />}
          </div>
        </div>
      )}
    </section>
  );
}
