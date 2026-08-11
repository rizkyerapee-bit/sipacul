"use client";

import { useCallback, useEffect, useMemo, useState, type FormEvent } from "react";
import { useRouter } from "next/navigation";
import {
  ApiError,
  cancelCultivationExpense,
  confirmCultivationExpense,
  createCultivationExpense,
  getCropCycles,
  getCultivationExpenses,
  updateCultivationExpense,
} from "@/lib/api/client";
import type {
  CreateCultivationExpenseRequest,
  CropCycle,
  CultivationExpense,
  CultivationExpenseCategory,
  Organization,
} from "@/lib/api/contracts";
import { cropCycleStatusLabels } from "@/lib/cultivation/crop-cycle-management";
import {
  expenseCategoryLabels,
  expenseDateWindow,
  expenseDraftFrom,
  expenseStatusLabels,
  filterExpenses,
  formatExpenseCurrency,
  formatExpenseDate,
  optionalExpenseText,
  parseExpenseAmount,
  summarizeExpenses,
  validateExpenseDraft,
  type ExpenseCategoryFilter,
  type ExpenseDraft,
  type ExpenseStatusFilter,
} from "@/lib/finance/expense-management";
import styles from "./receivable-management.module.css";

type Props = {
  organization: Organization | null;
  organizationId: string | null;
  permissions: string[];
};

type EditorState = { expenseId: string | null };
type ActionState = { kind: "confirm" | "cancel"; expenseId: string };

type IconName =
  | "add" | "arrow" | "calendar" | "category" | "check" | "close"
  | "edit" | "invoice" | "money" | "receipt" | "refresh" | "search"
  | "stop" | "trend" | "user" | "wallet";

const iconPaths: Record<IconName, string> = {
  add: "M12 5v14M5 12h14",
  arrow: "m15 18-6-6 6-6",
  calendar: "M6 3v3m12-3v3M4 9h16M5 5h14a1 1 0 0 1 1 1v14H4V6a1 1 0 0 1 1-1Z",
  category: "M4 4h6v6H4V4Zm10 0h6v6h-6V4ZM4 14h6v6H4v-6Zm10 0h6v6h-6v-6Z",
  check: "m5 12 4 4L19 6",
  close: "m6 6 12 12M18 6 6 18",
  edit: "m4 20 4.5-1 10-10a2.1 2.1 0 0 0-3-3l-10 10L4 20Zm10-12 3 3",
  invoice: "M6 3h12v18l-3-2-3 2-3-2-3 2V3Zm3 5h6m-6 4h6m-6 4h4",
  money: "M12 3v18m4-14H9.5a3 3 0 0 0 0 6h5a3 3 0 0 1 0 6H7",
  receipt: "M5 3h14v18l-3-2-4 2-4-2-3 2V3Zm4 5h6m-6 4h6m-6 4h4",
  refresh: "M20 6v5h-5M4 18v-5h5m10-2a7 7 0 0 0-12-4L4 11m16 2-3 4a7 7 0 0 1-12-4",
  search: "m21 21-4.35-4.35m2.35-5.65a8 8 0 1 1-16 0 8 8 0 0 1 16 0Z",
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
  switch (error.problem?.code) {
    case "CultivationExpenses.CodeAlreadyExists":
      return "Kode biaya sudah digunakan pada siklus ini.";
    case "CultivationExpenses.DateOutOfRange":
      return "Tanggal biaya berada di luar rentang yang diizinkan untuk siklus ini.";
    case "CultivationExpenses.InvalidStatusTransition":
      return "Tindakan tidak sesuai dengan status biaya saat ini.";
    case "CultivationExpenses.FinalizedSettlementExists":
      return "Biaya terkunci karena pembagian hasil siklus ini sudah difinalkan.";
    case "CultivationExpenses.CropCycleNotFound":
      return "Siklus budidaya tidak ditemukan dalam organisasi aktif.";
    default:
      return error.message;
  }
}

function replaceExpense(
  expenses: CultivationExpense[],
  updated: CultivationExpense,
): CultivationExpense[] {
  return expenses.some((expense) => expense.id === updated.id)
    ? expenses.map((expense) => expense.id === updated.id ? updated : expense)
    : [...expenses, updated];
}

function ExpenseEditor({
  cycle,
  expense,
  today,
  isSaving,
  apiError,
  onCancel,
  onSubmit,
}: {
  cycle: CropCycle;
  expense: CultivationExpense | null;
  today: string;
  isSaving: boolean;
  apiError: string | null;
  onCancel: () => void;
  onSubmit: (draft: ExpenseDraft) => Promise<void>;
}) {
  const isCreate = expense === null;
  const [draft, setDraft] = useState<ExpenseDraft>(() =>
    expenseDraftFrom(expense, cycle, today));
  const [errors, setErrors] = useState<string[]>([]);
  const window = expenseDateWindow(cycle);

  function update<Key extends keyof ExpenseDraft>(key: Key, value: ExpenseDraft[Key]) {
    setDraft((current) => ({ ...current, [key]: value }));
  }

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const nextErrors = validateExpenseDraft(draft, cycle, isCreate);
    setErrors(nextErrors);
    if (nextErrors.length === 0) void onSubmit(draft);
  }

  return (
    <form className={styles.editorForm} onSubmit={submit} noValidate>
      <header className={styles.editorHeader}>
        <span className={styles.editorIcon}><Icon name="receipt" /></span>
        <div>
          <span className={styles.eyebrow}>{cycle.code} · {cycle.name}</span>
          <h2>{isCreate ? "Catat biaya budidaya" : `Ubah ${expense.code}`}</h2>
          <p>Draf belum masuk ke biaya budidaya sampai transaksi dikonfirmasi.</p>
        </div>
        <button className={styles.iconButton} type="button" aria-label="Tutup formulir" disabled={isSaving} onClick={onCancel}><Icon name="close" /></button>
      </header>

      {(errors.length > 0 || apiError) && (
        <div className={styles.formAlert} role="alert">
          <strong>Periksa kembali data berikut:</strong>
          <ul>{errors.map((error) => <li key={error}>{error}</li>)}{apiError && <li>{apiError}</li>}</ul>
        </div>
      )}

      <div className={styles.balancePreview}>
        <span><small>Siklus budidaya</small><strong>{cycle.code}</strong></span>
        <span><small>Rentang biaya</small><strong>{formatExpenseDate(window.minimum)} – {formatExpenseDate(window.maximum)}</strong></span>
        <i><Icon name="calendar" /></i>
      </div>

      <fieldset disabled={isSaving}>
        <div className={styles.formGrid}>
          <label className={styles.field}>
            <span>Kode biaya <em>*</em></span>
            <input value={draft.code} maxLength={40} disabled={!isCreate} placeholder="Contoh: BIA-001" onChange={(event) => update("code", event.target.value)} />
          </label>
          <label className={styles.field}>
            <span>Tanggal biaya <em>*</em></span>
            <input type="date" value={draft.expenseDate} min={window.minimum} max={window.maximum} onChange={(event) => update("expenseDate", event.target.value)} />
          </label>
          <label className={styles.field}>
            <span>Kategori <em>*</em></span>
            <select value={draft.category} onChange={(event) => update("category", Number(event.target.value) as CultivationExpenseCategory)}>
              {Object.entries(expenseCategoryLabels).map(([value, label]) => <option value={value} key={value}>{label}</option>)}
            </select>
          </label>
          <label className={styles.field}>
            <span>Jumlah biaya <em>*</em></span>
            <input value={draft.amount} inputMode="decimal" placeholder="0" onChange={(event) => update("amount", event.target.value)} />
          </label>
          <label className={`${styles.field} ${styles.fieldFull}`}>
            <span>Deskripsi biaya <em>*</em></span>
            <input value={draft.description} maxLength={250} placeholder="Contoh: Upah tenaga pengolahan lahan" onChange={(event) => update("description", event.target.value)} />
          </label>
          <label className={styles.field}>
            <span>Dibayarkan kepada</span>
            <input value={draft.payeeName} maxLength={150} placeholder="Nama penerima atau pemasok" onChange={(event) => update("payeeName", event.target.value)} />
          </label>
          <label className={styles.field}>
            <span>Nomor referensi</span>
            <input value={draft.referenceNumber} maxLength={100} placeholder="Nomor kuitansi atau invoice" onChange={(event) => update("referenceNumber", event.target.value)} />
          </label>
          <label className={`${styles.field} ${styles.fieldFull}`}>
            <span>Tautan bukti</span>
            <input value={draft.evidenceUrl} maxLength={1000} placeholder="https://..." onChange={(event) => update("evidenceUrl", event.target.value)} />
          </label>
          <label className={`${styles.field} ${styles.fieldFull}`}>
            <span>Catatan</span>
            <textarea value={draft.notes} maxLength={1000} rows={4} placeholder="Keterangan pembayaran, rincian, atau informasi tambahan" onChange={(event) => update("notes", event.target.value)} />
          </label>
        </div>
      </fieldset>

      <footer className={styles.formActions}>
        <button className={styles.secondaryButton} type="button" disabled={isSaving} onClick={onCancel}>Batal</button>
        <button className={styles.primaryButton} type="submit" disabled={isSaving}>{isSaving ? "Menyimpan..." : isCreate ? "Simpan draf" : "Simpan perubahan"}</button>
      </footer>
    </form>
  );
}

function ExpenseAction({
  action,
  expense,
  isSaving,
  apiError,
  onCancel,
  onSubmit,
}: {
  action: ActionState;
  expense: CultivationExpense;
  isSaving: boolean;
  apiError: string | null;
  onCancel: () => void;
  onSubmit: (reason: string) => Promise<void>;
}) {
  const [reason, setReason] = useState("");
  const [validationError, setValidationError] = useState<string | null>(null);
  const isConfirm = action.kind === "confirm";

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const normalized = reason.trim();
    if (!isConfirm && !normalized) {
      setValidationError("Alasan pembatalan wajib diisi.");
      return;
    }
    if (normalized.length > 500) {
      setValidationError("Alasan pembatalan maksimal 500 karakter.");
      return;
    }
    void onSubmit(normalized);
  }

  return (
    <form className={styles.actionDialog} onSubmit={submit} noValidate>
      <div className={`${styles.actionIcon} ${!isConfirm ? styles.actionIconDanger : ""}`}><Icon name={isConfirm ? "check" : "stop"} /></div>
      <span className={styles.eyebrow}>{expense.code} · {expenseCategoryLabels[expense.category]}</span>
      <h2>{isConfirm ? "Konfirmasi biaya?" : "Batalkan biaya?"}</h2>
      <p>{isConfirm
        ? "Nilai transaksi akan diakui sebagai biaya budidaya dan memengaruhi profitabilitas siklus. Data tidak dapat diubah setelah dikonfirmasi."
        : expense.status === 2
          ? "Nilai transaksi akan dikeluarkan dari biaya budidaya. Jejak pembatalan tetap tersimpan."
          : "Draf dibatalkan tanpa memengaruhi biaya budidaya."}</p>
      <div className={styles.actionSummary}>
        <span><small>Jumlah biaya</small><strong>{formatExpenseCurrency(expense.amount)}</strong></span>
        <span><small>Status saat ini</small><strong>{expenseStatusLabels[expense.status]}</strong></span>
      </div>
      {!isConfirm && <label className={styles.field}><span>Alasan pembatalan <em>*</em></span><textarea value={reason} maxLength={500} rows={4} autoFocus placeholder="Jelaskan alasan pembatalan biaya" disabled={isSaving} onChange={(event) => setReason(event.target.value)} /></label>}
      {(validationError || apiError) && <div className={styles.formAlert} role="alert"><ul>{validationError && <li>{validationError}</li>}{apiError && <li>{apiError}</li>}</ul></div>}
      <div className={styles.actionButtons}>
        <button className={styles.secondaryButton} type="button" disabled={isSaving} onClick={onCancel}>Kembali</button>
        <button className={isConfirm ? styles.primaryButton : styles.dangerButton} type="submit" disabled={isSaving}>{isSaving ? "Memproses..." : isConfirm ? "Konfirmasi biaya" : "Batalkan biaya"}</button>
      </div>
    </form>
  );
}

export function ExpenseManagement({ organization, organizationId, permissions }: Props) {
  const router = useRouter();
  const [today] = useState(() => localToday());
  const [cycles, setCycles] = useState<CropCycle[]>([]);
  const [selectedCycleId, setSelectedCycleId] = useState("");
  const [expenses, setExpenses] = useState<CultivationExpense[]>([]);
  const [selectedExpenseId, setSelectedExpenseId] = useState("");
  const [query, setQuery] = useState("");
  const [statusFilter, setStatusFilter] = useState<ExpenseStatusFilter>("all");
  const [categoryFilter, setCategoryFilter] = useState<ExpenseCategoryFilter>("all");
  const [editor, setEditor] = useState<EditorState | null>(null);
  const [action, setAction] = useState<ActionState | null>(null);
  const [isLoadingCycles, setIsLoadingCycles] = useState(true);
  const [isLoadingExpenses, setIsLoadingExpenses] = useState(false);
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [pageError, setPageError] = useState<string | null>(null);
  const [modalError, setModalError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);

  const canRead = permissions.includes("finance.read")
    && permissions.includes("cultivation.read");
  const canWrite = permissions.includes("finance.write");
  const selectedCycle = cycles.find((cycle) => cycle.id === selectedCycleId) ?? null;
  const filteredExpenses = useMemo(
    () => filterExpenses(expenses, query, statusFilter, categoryFilter),
    [categoryFilter, expenses, query, statusFilter],
  );
  const summary = useMemo(() => summarizeExpenses(expenses), [expenses]);
  const selectedExpense = filteredExpenses.find((expense) => expense.id === selectedExpenseId)
    ?? filteredExpenses[0] ?? null;
  const editedExpense = editor?.expenseId
    ? expenses.find((expense) => expense.id === editor.expenseId) ?? null
    : null;
  const actionExpense = action
    ? expenses.find((expense) => expense.id === action.expenseId) ?? null
    : null;

  useEffect(() => {
    let cancelled = false;
    async function load() {
      if (!organizationId || !canRead) {
        setIsLoadingCycles(false);
        return;
      }
      setIsLoadingCycles(true);
      setPageError(null);
      try {
        const result = await getCropCycles(organizationId);
        if (cancelled) return;
        setCycles(result);
        const requestedCycleId = new URLSearchParams(window.location.search).get("cycleId");
        setSelectedCycleId((current) => {
          if (requestedCycleId && result.some((cycle) => cycle.id === requestedCycleId)) {
            return requestedCycleId;
          }
          if (result.some((cycle) => cycle.id === current)) return current;
          return result.find((cycle) => cycle.status !== 4)?.id ?? result[0]?.id ?? "";
        });
      } catch (error) {
        if (error instanceof ApiError && error.status === 401) {
          router.replace("/login");
          return;
        }
        if (!cancelled) setPageError(friendlyError(error));
      } finally {
        if (!cancelled) setIsLoadingCycles(false);
      }
    }
    void load();
    return () => { cancelled = true; };
  }, [canRead, organizationId, router]);

  useEffect(() => {
    let cancelled = false;
    async function load() {
      if (!organizationId || !selectedCycleId || !canRead) {
        setExpenses([]);
        setIsLoadingExpenses(false);
        return;
      }
      setIsLoadingExpenses(true);
      setPageError(null);
      try {
        const result = await getCultivationExpenses(organizationId, selectedCycleId);
        if (cancelled) return;
        setExpenses(result);
        setSelectedExpenseId((current) => result.some((expense) => expense.id === current)
          ? current : result[0]?.id ?? "");
      } catch (error) {
        if (error instanceof ApiError && error.status === 401) {
          router.replace("/login");
          return;
        }
        if (!cancelled) setPageError(friendlyError(error));
      } finally {
        if (!cancelled) setIsLoadingExpenses(false);
      }
    }
    void load();
    return () => { cancelled = true; };
  }, [canRead, organizationId, router, selectedCycleId]);

  const refresh = useCallback(async () => {
    if (!organizationId || !selectedCycleId) return;
    setIsRefreshing(true);
    setPageError(null);
    try {
      const result = await getCultivationExpenses(organizationId, selectedCycleId);
      setExpenses(result);
      setSelectedExpenseId((current) => result.some((expense) => expense.id === current)
        ? current : result[0]?.id ?? "");
      setNotice("Data biaya telah dimuat ulang.");
    } catch (error) {
      if (error instanceof ApiError && error.status === 401) {
        router.replace("/login");
        return;
      }
      setPageError(friendlyError(error));
    } finally {
      setIsRefreshing(false);
    }
  }, [organizationId, router, selectedCycleId]);

  async function submitExpense(draft: ExpenseDraft) {
    if (!organizationId || !selectedCycle || !editor) return;
    const amount = parseExpenseAmount(draft.amount);
    if (amount === null) return;
    const request: CreateCultivationExpenseRequest = {
      code: draft.code.trim().toUpperCase(),
      expenseDate: draft.expenseDate,
      category: draft.category,
      description: draft.description.trim(),
      amount,
      payeeName: optionalExpenseText(draft.payeeName),
      referenceNumber: optionalExpenseText(draft.referenceNumber),
      evidenceUrl: optionalExpenseText(draft.evidenceUrl),
      notes: optionalExpenseText(draft.notes),
    };

    setIsSaving(true);
    setModalError(null);
    try {
      const updated = editor.expenseId
        ? await updateCultivationExpense(
          organizationId,
          selectedCycle.id,
          editor.expenseId,
          {
            expenseDate: request.expenseDate,
            category: request.category,
            description: request.description,
            amount: request.amount,
            payeeName: request.payeeName,
            referenceNumber: request.referenceNumber,
            evidenceUrl: request.evidenceUrl,
            notes: request.notes,
          },
        )
        : await createCultivationExpense(organizationId, selectedCycle.id, request);
      setExpenses((current) => replaceExpense(current, updated));
      setSelectedExpenseId(updated.id);
      setEditor(null);
      setNotice(editor.expenseId ? `Biaya ${updated.code} diperbarui.` : `Draf biaya ${updated.code} dibuat.`);
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
      const updated = action.kind === "confirm"
        ? await confirmCultivationExpense(organizationId, selectedCycle.id, action.expenseId)
        : await cancelCultivationExpense(
          organizationId,
          selectedCycle.id,
          action.expenseId,
          { cancellationReason: reason.trim() },
        );
      setExpenses((current) => replaceExpense(current, updated));
      setSelectedExpenseId(updated.id);
      setAction(null);
      setNotice(action.kind === "confirm"
        ? `Biaya ${updated.code} dikonfirmasi dan masuk biaya budidaya.`
        : `Biaya ${updated.code} dibatalkan.`);
    } catch (error) {
      setModalError(friendlyError(error));
    } finally {
      setIsSaving(false);
    }
  }

  if (!organizationId) {
    return <section className={styles.accessState}><Icon name="wallet" /><h1>Pilih organisasi terlebih dahulu</h1><p>Setiap biaya budidaya terikat pada satu organisasi aktif.</p></section>;
  }
  if (!canRead) {
    return <section className={styles.accessState}><Icon name="stop" /><h1>Akses biaya tidak tersedia</h1><p>Peran Anda memerlukan izin <strong>finance.read</strong> dan <strong>cultivation.read</strong>.</p></section>;
  }

  return (
    <section className={styles.financePage}>
      <div className={styles.hero}>
        <div>
          <button className={styles.backButton} type="button" onClick={() => router.push("/finance")}><Icon name="arrow" /> Pembayaran &amp; piutang</button>
          <span className={styles.eyebrow}>Arus kas keluar</span>
          <h1>Pengeluaran &amp; biaya budidaya</h1>
          <p>Kelola bukti pengeluaran {organization?.name ? `untuk ${organization.name}` : "organisasi aktif"} per siklus dan pastikan hanya biaya terkonfirmasi yang masuk perhitungan laba.</p>
        </div>
        <div className={styles.heroActions}>
          {!canWrite && <span className={styles.readOnlyBadge}>Mode baca</span>}
          <button className={styles.secondaryButton} type="button" disabled={isRefreshing || isLoadingCycles || isLoadingExpenses || !selectedCycle} onClick={() => void refresh()}><Icon name="refresh" /> {isRefreshing ? "Memuat..." : "Muat ulang"}</button>
          {canWrite && selectedCycle && <button className={styles.primaryButton} type="button" onClick={() => { setModalError(null); setEditor({ expenseId: null }); }}><Icon name="add" /> Catat biaya</button>}
        </div>
      </div>

      {notice && <div className={styles.notice} role="status"><span><Icon name="check" /></span><strong>{notice}</strong><button type="button" aria-label="Tutup pemberitahuan" onClick={() => setNotice(null)}><Icon name="close" /></button></div>}
      {pageError && <div className={styles.pageError} role="alert">{pageError}</div>}

      <div className={styles.toolbar}>
        <label className={styles.filterField}><span>Siklus budidaya</span><select value={selectedCycleId} disabled={isLoadingCycles || cycles.length === 0} onChange={(event) => { setSelectedCycleId(event.target.value); setSelectedExpenseId(""); setNotice(null); }}><option value="">Pilih siklus</option>{cycles.map((cycle) => <option value={cycle.id} key={cycle.id}>{cycle.code} · {cycle.name} · {cropCycleStatusLabels[cycle.status]}</option>)}</select></label>
        {selectedCycle && <span className={styles.resultCount}>{formatExpenseDate(selectedCycle.plannedStartDate)} – {formatExpenseDate(selectedCycle.actualHarvestDate ?? selectedCycle.expectedHarvestDate)}</span>}
      </div>

      <div className={styles.metricGrid}>
        <article className={`${styles.metricCard} ${styles.metricPrimary}`}><span>Biaya diakui</span><strong>{formatExpenseCurrency(summary.recognized)}</strong><small>{summary.confirmedCount} transaksi terkonfirmasi</small><i><Icon name="wallet" /></i></article>
        <article className={styles.metricCard}><span>Menunggu konfirmasi</span><strong>{formatExpenseCurrency(summary.draft)}</strong><small>{summary.draftCount} transaksi draf</small><i><Icon name="invoice" /></i></article>
        <article className={styles.metricCard}><span>Biaya dibatalkan</span><strong>{formatExpenseCurrency(summary.cancelled)}</strong><small>{summary.cancelledCount} transaksi tersimpan sebagai jejak</small><i><Icon name="stop" /></i></article>
        <article className={styles.metricCard}><span>Kategori terbesar</span><strong>{summary.topCategory ? expenseCategoryLabels[summary.topCategory.category] : "—"}</strong><small>{summary.topCategory ? formatExpenseCurrency(summary.topCategory.amount) : "Belum ada biaya dikonfirmasi"}</small><i><Icon name="category" /></i></article>
      </div>

      <div className={styles.collectionStrip}>
        <span><Icon name="trend" /></span>
        <div><strong>Biaya terkonfirmasi membentuk biaya budidaya</strong><small>Draf belum memengaruhi laba. Pembatalan mengeluarkan kembali nilai transaksi dari biaya yang diakui.</small></div>
      </div>

      <div className={styles.toolbar}>
        <label className={styles.searchField}><Icon name="search" /><input value={query} placeholder="Cari kode, deskripsi, penerima, atau referensi" aria-label="Cari biaya" onChange={(event) => setQuery(event.target.value)} /></label>
        <label className={styles.filterField}><span>Status</span><select value={statusFilter} onChange={(event) => setStatusFilter(event.target.value === "all" ? "all" : Number(event.target.value) as ExpenseStatusFilter)}><option value="all">Semua status</option>{Object.entries(expenseStatusLabels).map(([value, label]) => <option value={value} key={value}>{label}</option>)}</select></label>
        <label className={styles.filterField}><span>Kategori</span><select value={categoryFilter} onChange={(event) => setCategoryFilter(event.target.value === "all" ? "all" : Number(event.target.value) as ExpenseCategoryFilter)}><option value="all">Semua kategori</option>{Object.entries(expenseCategoryLabels).map(([value, label]) => <option value={value} key={value}>{label}</option>)}</select></label>
        <span className={styles.resultCount}>{filteredExpenses.length} hasil</span>
      </div>

      {isLoadingCycles || isLoadingExpenses ? (
        <div className={styles.loadingState}><span className="loader" /><p>Memuat pengeluaran budidaya...</p></div>
      ) : cycles.length === 0 ? (
        <div className={styles.emptyState}><span><Icon name="calendar" /></span><h2>Belum ada siklus budidaya</h2><p>Buat siklus terlebih dahulu sebelum mencatat pengeluaran.</p><button className={styles.secondaryButton} type="button" onClick={() => router.push("/cultivation")}>Buka siklus budidaya</button></div>
      ) : expenses.length === 0 ? (
        <div className={styles.emptyState}><span><Icon name="receipt" /></span><h2>Belum ada biaya pada siklus ini</h2><p>Catat pengeluaran berdasarkan kuitansi, invoice, atau bukti transaksi yang tersedia.</p>{canWrite && <button className={styles.primaryButton} type="button" onClick={() => setEditor({ expenseId: null })}><Icon name="add" /> Catat biaya pertama</button>}</div>
      ) : filteredExpenses.length === 0 ? (
        <div className={styles.emptyState}><span><Icon name="search" /></span><h2>Tidak ada hasil yang sesuai</h2><p>Ubah kata pencarian atau filter untuk melihat biaya lain.</p><button className={styles.secondaryButton} type="button" onClick={() => { setQuery(""); setStatusFilter("all"); setCategoryFilter("all"); }}>Bersihkan filter</button></div>
      ) : (
        <div className={styles.managementGrid}>
          <aside className={styles.receivableList}>
            <header><div><span className={styles.eyebrow}>Daftar pengeluaran</span><h2>{filteredExpenses.length} transaksi</h2></div></header>
            <div className={styles.receivableCards}>
              {filteredExpenses.map((expense) => <button className={`${styles.receivableCard} ${expense.id === selectedExpense?.id ? styles.receivableCardSelected : ""}`} type="button" aria-pressed={expense.id === selectedExpense?.id} key={expense.id} onClick={() => setSelectedExpenseId(expense.id)}><span className={styles.cardTopline}><strong>{expense.code}</strong><i className={`${styles.stateBadge} ${styles[`state${expense.status}`]}`}>{expenseStatusLabels[expense.status]}</i></span><b>{formatExpenseCurrency(expense.amount)}</b><span className={styles.cardMeta}><small>{expenseCategoryLabels[expense.category]}</small><small>{formatExpenseDate(expense.expenseDate)}</small></span><span className={styles.dueLine}><Icon name="user" /> {expense.payeeName ?? "Penerima tidak dicatat"}</span></button>)}
            </div>
          </aside>

          {selectedExpense && (
            <article className={styles.receivableDetail}>
              <header className={styles.detailHeader}>
                <div className={styles.detailIdentity}><span className={styles.detailIcon}><Icon name="receipt" /></span><div><span>{selectedExpense.code}</span><h2>{selectedExpense.description}</h2><p>{formatExpenseDate(selectedExpense.expenseDate)} · {expenseCategoryLabels[selectedExpense.category]}</p></div></div>
                <div className={styles.detailActions}><span className={`${styles.stateBadge} ${styles[`state${selectedExpense.status}`]}`}>{expenseStatusLabels[selectedExpense.status]}</span>{canWrite && selectedExpense.status === 1 && <button className={styles.primaryButton} type="button" onClick={() => { setModalError(null); setEditor({ expenseId: selectedExpense.id }); }}><Icon name="edit" /> Ubah draf</button>}</div>
              </header>

              <div className={styles.amountGrid}>
                <div><span>Jumlah transaksi</span><strong>{formatExpenseCurrency(selectedExpense.amount)}</strong></div>
                <div><span>Kategori</span><strong>{expenseCategoryLabels[selectedExpense.category]}</strong></div>
                <div className={styles.amountTotal}><span>Biaya diakui</span><strong>{selectedExpense.isRecognizedCost ? formatExpenseCurrency(selectedExpense.amount) : formatExpenseCurrency(0)}</strong></div>
              </div>

              <div className={styles.infoGrid}>
                <section><i><Icon name="user" /></i><span><small>Dibayarkan kepada</small><strong>{selectedExpense.payeeName ?? "Tidak dicatat"}</strong></span></section>
                <section><i><Icon name="invoice" /></i><span><small>Nomor referensi</small><strong>{selectedExpense.referenceNumber ?? "Tidak dicatat"}</strong></span></section>
                <section><i><Icon name="calendar" /></i><span><small>Dikonfirmasi</small><strong>{selectedExpense.confirmedAt ? new Date(selectedExpense.confirmedAt).toLocaleString("id-ID", { dateStyle: "medium", timeStyle: "short" }) : "Belum dikonfirmasi"}</strong></span></section>
                <section><i><Icon name="money" /></i><span><small>Dampak profitabilitas</small><strong>{selectedExpense.isRecognizedCost ? "Mengurangi laba" : "Belum memengaruhi laba"}</strong></span></section>
              </div>

              <section className={styles.paymentSection}>
                <header><div><span className={styles.eyebrow}>Dokumentasi transaksi</span><h3>Rincian &amp; bukti</h3></div></header>
                <div className={styles.paymentList}>
                  <article className={styles.paymentCard}>
                    <div className={styles.paymentMain}><span className={styles.paymentIcon}><Icon name="invoice" /></span><div><strong>{selectedExpense.referenceNumber ?? "Tanpa nomor referensi"}</strong><span>{selectedExpense.evidenceUrl ?? "Tautan bukti belum dicatat"}</span></div></div>
                    {selectedExpense.notes && <p className={styles.paymentNotes}>{selectedExpense.notes}</p>}
                    {selectedExpense.cancellationReason && <p className={styles.cancellationNote}><strong>Dibatalkan:</strong> {selectedExpense.cancellationReason}</p>}
                    {canWrite && selectedExpense.status !== 3 && <div className={styles.paymentActions}>{selectedExpense.status === 1 && <><button type="button" onClick={() => { setModalError(null); setEditor({ expenseId: selectedExpense.id }); }}><Icon name="edit" /> Ubah</button><button className={styles.confirmTextButton} type="button" onClick={() => { setModalError(null); setAction({ kind: "confirm", expenseId: selectedExpense.id }); }}><Icon name="check" /> Konfirmasi</button></>}<button className={styles.dangerTextButton} type="button" onClick={() => { setModalError(null); setAction({ kind: "cancel", expenseId: selectedExpense.id }); }}><Icon name="stop" /> Batalkan</button></div>}
                  </article>
                </div>
              </section>

              <footer className={styles.detailFooter}><span>Biaya terkunci ketika pembagian hasil siklus sudah difinalkan.</span><button type="button" onClick={() => router.push(`/cultivation?cycleId=${encodeURIComponent(selectedCycleId)}`)}>Lihat siklus budidaya</button></footer>
            </article>
          )}
        </div>
      )}

      {editor && selectedCycle && (
        <div className={styles.modalBackdrop} role="presentation" onMouseDown={(event) => { if (event.target === event.currentTarget && !isSaving) setEditor(null); }}>
          <div className={styles.modalPanel} role="dialog" aria-modal="true" aria-label={editedExpense ? "Ubah biaya" : "Catat biaya"}><ExpenseEditor key={editor.expenseId ?? `create-${selectedCycle.id}`} cycle={selectedCycle} expense={editedExpense} today={today} isSaving={isSaving} apiError={modalError} onCancel={() => { setEditor(null); setModalError(null); }} onSubmit={submitExpense} /></div>
        </div>
      )}
      {action && actionExpense && (
        <div className={styles.modalBackdrop} role="presentation" onMouseDown={(event) => { if (event.target === event.currentTarget && !isSaving) setAction(null); }}>
          <div className={styles.actionPanel} role="dialog" aria-modal="true" aria-label="Tindakan biaya"><ExpenseAction key={`${action.kind}-${action.expenseId}`} action={action} expense={actionExpense} isSaving={isSaving} apiError={modalError} onCancel={() => { setAction(null); setModalError(null); }} onSubmit={submitAction} /></div>
        </div>
      )}
    </section>
  );
}
