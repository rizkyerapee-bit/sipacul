"use client";

import { useCallback, useEffect, useMemo, useState, type FormEvent } from "react";
import { useRouter } from "next/navigation";
import {
  ApiError,
  cancelSalePayment,
  confirmSalePayment,
  createSalePayment,
  getSalePayments,
  getSaleReceivable,
  getSales,
  updateSalePayment,
} from "@/lib/api/client";
import type {
  CreateSalePaymentRequest,
  Organization,
  Sale,
  SalePayment,
  SalePaymentMethod,
} from "@/lib/api/contracts";
import {
  daysUntil,
  filterReceivables,
  formatFinanceCurrency,
  formatFinanceDate,
  isDueSoon,
  isOverdue,
  optionalPaymentText,
  parsePaymentAmount,
  paymentDraftFrom,
  paymentMethodLabels,
  paymentStateLabels,
  paymentStatusLabels,
  summarizeReceivables,
  validatePaymentDraft,
  type DueStateFilter,
  type PaymentDraft,
  type ReceivableEntry,
  type ReceivableStateFilter,
} from "@/lib/finance/receivable-management";
import styles from "./receivable-management.module.css";

type Props = {
  organization: Organization | null;
  organizationId: string | null;
  permissions: string[];
};

type EditorState = { saleId: string; paymentId: string | null };
type ActionState = { kind: "confirm" | "cancel"; saleId: string; paymentId: string };

type IconName =
  | "add" | "arrow" | "bank" | "calendar" | "check" | "clock"
  | "close" | "credit" | "edit" | "invoice" | "money" | "refresh"
  | "search" | "stop" | "trend" | "user" | "wallet";

const iconPaths: Record<IconName, string> = {
  add: "M12 5v14M5 12h14",
  arrow: "m15 18-6-6 6-6",
  bank: "M3 10h18M5 10v8m4-8v8m6-8v8m4-8v8M2 21h20M12 3 2 8h20L12 3Z",
  calendar: "M6 3v3m12-3v3M4 9h16M5 5h14a1 1 0 0 1 1 1v14H4V6a1 1 0 0 1 1-1Z",
  check: "m5 12 4 4L19 6",
  clock: "M12 7v5l3 2m6-2a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z",
  close: "m6 6 12 12M18 6 6 18",
  credit: "M3 6h18v12H3V6Zm0 4h18M7 15h4",
  edit: "m4 20 4.5-1 10-10a2.1 2.1 0 0 0-3-3l-10 10L4 20Zm10-12 3 3",
  invoice: "M6 3h12v18l-3-2-3 2-3-2-3 2V3Zm3 5h6m-6 4h6m-6 4h4",
  money: "M12 3v18m4-14H9.5a3 3 0 0 0 0 6h5a3 3 0 0 1 0 6H7",
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
    case "SalePayments.CodeAlreadyExists":
      return "Kode pembayaran sudah digunakan dalam organisasi ini.";
    case "SalePayments.SaleNotConfirmed":
      return "Pembayaran hanya dapat dicatat untuk penjualan yang sudah dikonfirmasi.";
    case "SalePayments.PaymentDateBeforeSaleDate":
      return "Tanggal pembayaran tidak boleh sebelum tanggal penjualan.";
    case "SalePayments.Overpayment":
      return "Jumlah pembayaran terkonfirmasi melebihi sisa tagihan. Muat ulang lalu periksa saldo.";
    case "SalePayments.ConfirmationConcurrency":
      return "Saldo berubah bersamaan dengan konfirmasi. Muat ulang lalu coba lagi.";
    case "SalePayments.InvalidStatusTransition":
      return "Tindakan tidak sesuai dengan status pembayaran saat ini.";
    default:
      return error.message;
  }
}

function dueCopy(entry: ReceivableEntry, today: string): string {
  if (entry.receivable.isFullyPaid) return "Tagihan lunas";
  if (!entry.receivable.dueDate) return "Tanpa jatuh tempo";
  const remaining = daysUntil(entry.receivable.dueDate, today);
  if (remaining === null) return formatFinanceDate(entry.receivable.dueDate);
  if (remaining < 0) return `Terlambat ${Math.abs(remaining)} hari`;
  if (remaining === 0) return "Jatuh tempo hari ini";
  return `${remaining} hari lagi`;
}

function PaymentEditor({
  sale,
  payment,
  outstanding,
  isSaving,
  apiError,
  onCancel,
  onSubmit,
}: {
  sale: Sale;
  payment: SalePayment | null;
  outstanding: number;
  isSaving: boolean;
  apiError: string | null;
  onCancel: () => void;
  onSubmit: (draft: PaymentDraft) => Promise<void>;
}) {
  const isCreate = payment === null;
  const [draft, setDraft] = useState<PaymentDraft>(() =>
    paymentDraftFrom(payment, sale, outstanding, localToday()));
  const [errors, setErrors] = useState<string[]>([]);

  function update<Key extends keyof PaymentDraft>(key: Key, value: PaymentDraft[Key]) {
    setDraft((current) => ({ ...current, [key]: value }));
  }

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const nextErrors = validatePaymentDraft(draft, sale, outstanding, isCreate);
    setErrors(nextErrors);
    if (nextErrors.length === 0) void onSubmit(draft);
  }

  return (
    <form className={styles.editorForm} onSubmit={submit} noValidate>
      <header className={styles.editorHeader}>
        <span className={styles.editorIcon}><Icon name="wallet" /></span>
        <div>
          <span className={styles.eyebrow}>{sale.code} · {sale.buyerName}</span>
          <h2>{isCreate ? "Catat penerimaan pembayaran" : `Ubah ${payment.code}`}</h2>
          <p>Draf belum menambah kas terkumpul sampai pembayaran dikonfirmasi.</p>
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
        <span><small>Total tagihan</small><strong>{formatFinanceCurrency(sale.totalAmount)}</strong></span>
        <span><small>Sisa yang dapat dibayar</small><strong>{formatFinanceCurrency(outstanding)}</strong></span>
        <i><Icon name="credit" /></i>
      </div>

      <fieldset disabled={isSaving}>
        <div className={styles.formGrid}>
          <label className={styles.field}>
            <span>Kode pembayaran <em>*</em></span>
            <input value={draft.code} maxLength={40} disabled={!isCreate} placeholder="Contoh: BYR-001" onChange={(event) => update("code", event.target.value)} />
          </label>
          <label className={styles.field}>
            <span>Tanggal pembayaran <em>*</em></span>
            <input type="date" value={draft.paymentDate} min={sale.saleDate} onChange={(event) => update("paymentDate", event.target.value)} />
          </label>
          <label className={styles.field}>
            <span>Jumlah diterima <em>*</em></span>
            <input value={draft.amount} inputMode="decimal" placeholder="0" onChange={(event) => update("amount", event.target.value)} />
          </label>
          <label className={styles.field}>
            <span>Metode pembayaran <em>*</em></span>
            <select value={draft.paymentMethod} onChange={(event) => update("paymentMethod", Number(event.target.value) as SalePaymentMethod)}>
              {Object.entries(paymentMethodLabels).map(([value, label]) => <option value={value} key={value}>{label}</option>)}
            </select>
          </label>
          <label className={styles.field}>
            <span>Nomor referensi</span>
            <input value={draft.referenceNumber} maxLength={100} placeholder="Nomor transfer, kuitansi, atau bukti" onChange={(event) => update("referenceNumber", event.target.value)} />
          </label>
          <label className={styles.field}>
            <span>Diterima dari</span>
            <input value={draft.receivedFrom} maxLength={150} placeholder="Nama penyetor atau pembeli" onChange={(event) => update("receivedFrom", event.target.value)} />
          </label>
          <label className={`${styles.field} ${styles.fieldFull}`}>
            <span>Catatan</span>
            <textarea value={draft.notes} maxLength={1000} rows={4} placeholder="Keterangan penerimaan atau rekonsiliasi" onChange={(event) => update("notes", event.target.value)} />
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

function PaymentAction({
  action,
  entry,
  payment,
  isSaving,
  apiError,
  onCancel,
  onSubmit,
}: {
  action: ActionState;
  entry: ReceivableEntry;
  payment: SalePayment;
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
      <span className={styles.eyebrow}>{payment.code} · {entry.sale.code}</span>
      <h2>{isConfirm ? "Konfirmasi pembayaran?" : "Batalkan pembayaran?"}</h2>
      <p>{isConfirm
        ? "Nilai pembayaran akan masuk ke kas terkumpul dan mengurangi sisa piutang. Data tidak dapat diubah setelah dikonfirmasi."
        : payment.status === 2
          ? "Kas terkumpul akan berkurang dan sisa piutang bertambah kembali. Jejak pembayaran tetap tersimpan."
          : "Draf dibatalkan tanpa memengaruhi kas terkumpul atau sisa piutang."}</p>
      <div className={styles.actionSummary}>
        <span><small>Jumlah pembayaran</small><strong>{formatFinanceCurrency(payment.amount)}</strong></span>
        <span><small>Sisa piutang saat ini</small><strong>{formatFinanceCurrency(entry.receivable.outstandingReceivable)}</strong></span>
      </div>
      {!isConfirm && <label className={styles.field}><span>Alasan pembatalan <em>*</em></span><textarea value={reason} maxLength={500} rows={4} autoFocus placeholder="Jelaskan alasan pembatalan pembayaran" disabled={isSaving} onChange={(event) => setReason(event.target.value)} /></label>}
      {(validationError || apiError) && <div className={styles.formAlert} role="alert"><ul>{validationError && <li>{validationError}</li>}{apiError && <li>{apiError}</li>}</ul></div>}
      <div className={styles.actionButtons}>
        <button className={styles.secondaryButton} type="button" disabled={isSaving} onClick={onCancel}>Kembali</button>
        <button className={isConfirm ? styles.primaryButton : styles.dangerButton} type="submit" disabled={isSaving}>{isSaving ? "Memproses..." : isConfirm ? "Konfirmasi pembayaran" : "Batalkan pembayaran"}</button>
      </div>
    </form>
  );
}

export function ReceivableManagement({ organization, organizationId, permissions }: Props) {
  const router = useRouter();
  const [today] = useState(() => localToday());
  const [entries, setEntries] = useState<ReceivableEntry[]>([]);
  const [selectedSaleId, setSelectedSaleId] = useState("");
  const [query, setQuery] = useState("");
  const [stateFilter, setStateFilter] = useState<ReceivableStateFilter>("all");
  const [dueFilter, setDueFilter] = useState<DueStateFilter>("all");
  const [editor, setEditor] = useState<EditorState | null>(null);
  const [action, setAction] = useState<ActionState | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [pageError, setPageError] = useState<string | null>(null);
  const [modalError, setModalError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);

  const canRead = permissions.includes("finance.read") && permissions.includes("sales.read");
  const canWrite = permissions.includes("finance.write");

  const loadData = useCallback(async (): Promise<ReceivableEntry[]> => {
    if (!organizationId || !canRead) return [];
    const sales = await getSales(organizationId, { status: 2 });
    return Promise.all(sales.map(async (sale) => {
      const [receivable, payments] = await Promise.all([
        getSaleReceivable(organizationId, sale.id),
        getSalePayments(organizationId, sale.id),
      ]);
      return { sale, receivable, payments };
    }));
  }, [canRead, organizationId]);

  const filteredEntries = useMemo(
    () => filterReceivables(entries, query, stateFilter, dueFilter, today),
    [dueFilter, entries, query, stateFilter, today],
  );
  const summary = useMemo(() => summarizeReceivables(entries, today), [entries, today]);
  const selectedEntry = filteredEntries.find((entry) => entry.sale.id === selectedSaleId)
    ?? filteredEntries[0] ?? null;
  const editorEntry = editor ? entries.find((entry) => entry.sale.id === editor.saleId) ?? null : null;
  const editedPayment = editorEntry && editor?.paymentId
    ? editorEntry.payments.find((payment) => payment.id === editor.paymentId) ?? null
    : null;
  const actionEntry = action ? entries.find((entry) => entry.sale.id === action.saleId) ?? null : null;
  const actionPayment = actionEntry && action
    ? actionEntry.payments.find((payment) => payment.id === action.paymentId) ?? null
    : null;

  useEffect(() => {
    let cancelled = false;
    async function load() {
      if (!organizationId || !canRead) {
        setIsLoading(false);
        return;
      }
      setIsLoading(true);
      setPageError(null);
      try {
        const result = await loadData();
        if (cancelled) return;
        setEntries(result);
        const requestedSaleId = new URLSearchParams(window.location.search).get("saleId");
        setSelectedSaleId((current) => {
          if (requestedSaleId && result.some((entry) => entry.sale.id === requestedSaleId)) {
            return requestedSaleId;
          }
          return result.some((entry) => entry.sale.id === current)
            ? current
            : result[0]?.sale.id ?? "";
        });
      } catch (error) {
        if (error instanceof ApiError && error.status === 401) {
          router.replace("/login");
          return;
        }
        if (!cancelled) setPageError(friendlyError(error));
      } finally {
        if (!cancelled) setIsLoading(false);
      }
    }
    void load();
    return () => { cancelled = true; };
  }, [canRead, loadData, organizationId, router]);

  useEffect(() => {
    if (!editor && !action) return;
    const overflow = document.body.style.overflow;
    document.body.style.overflow = "hidden";
    function close(event: KeyboardEvent) {
      if (event.key === "Escape" && !isSaving) {
        setEditor(null);
        setAction(null);
        setModalError(null);
      }
    }
    window.addEventListener("keydown", close);
    return () => {
      document.body.style.overflow = overflow;
      window.removeEventListener("keydown", close);
    };
  }, [action, editor, isSaving]);

  async function refresh(message?: string) {
    setIsRefreshing(true);
    setPageError(null);
    try {
      const result = await loadData();
      setEntries(result);
      setSelectedSaleId((current) => result.some((entry) => entry.sale.id === current)
        ? current : result[0]?.sale.id ?? "");
      if (message) setNotice(message);
    } catch (error) {
      if (error instanceof ApiError && error.status === 401) {
        router.replace("/login");
        return;
      }
      setPageError(friendlyError(error));
    } finally {
      setIsRefreshing(false);
    }
  }

  async function submitPayment(draft: PaymentDraft) {
    if (!organizationId || !editor || !editorEntry || !canWrite) return;
    const amount = parsePaymentAmount(draft.amount);
    if (amount === null) return;
    setIsSaving(true);
    setModalError(null);
    try {
      const common = {
        paymentDate: draft.paymentDate,
        amount,
        paymentMethod: draft.paymentMethod,
        referenceNumber: optionalPaymentText(draft.referenceNumber),
        receivedFrom: optionalPaymentText(draft.receivedFrom),
        notes: optionalPaymentText(draft.notes),
      };
      if (editor.paymentId) {
        await updateSalePayment(organizationId, editor.saleId, editor.paymentId, common);
      } else {
        const request: CreateSalePaymentRequest = {
          code: draft.code.trim().toUpperCase(),
          ...common,
        };
        await createSalePayment(organizationId, editor.saleId, request);
      }
      setEditor(null);
      await refresh(editor.paymentId
        ? "Draf pembayaran berhasil diperbarui."
        : "Draf pembayaran dibuat. Konfirmasikan setelah dana benar-benar diterima.");
    } catch (error) {
      if (error instanceof ApiError && error.status === 401) {
        router.replace("/login");
        return;
      }
      setModalError(friendlyError(error));
    } finally {
      setIsSaving(false);
    }
  }

  async function submitAction(reason: string) {
    if (!organizationId || !action || !canWrite) return;
    setIsSaving(true);
    setModalError(null);
    try {
      if (action.kind === "confirm") {
        await confirmSalePayment(organizationId, action.saleId, action.paymentId);
      } else {
        await cancelSalePayment(organizationId, action.saleId, action.paymentId, {
          cancellationReason: reason,
        });
      }
      setAction(null);
      await refresh(action.kind === "confirm"
        ? "Pembayaran dikonfirmasi, kas bertambah, dan sisa piutang diperbarui."
        : "Pembayaran dibatalkan dan saldo piutang telah dihitung ulang.");
    } catch (error) {
      if (error instanceof ApiError && error.status === 401) {
        router.replace("/login");
        return;
      }
      setModalError(friendlyError(error));
    } finally {
      setIsSaving(false);
    }
  }

  if (!organizationId) {
    return <section className={styles.accessState}><Icon name="wallet" /><h1>Pilih organisasi terlebih dahulu</h1><p>Setiap piutang dan pembayaran terikat pada satu organisasi aktif.</p></section>;
  }
  if (!canRead) {
    return <section className={styles.accessState}><Icon name="stop" /><h1>Akses keuangan tidak tersedia</h1><p>Peran Anda memerlukan izin <strong>finance.read</strong> dan <strong>sales.read</strong>.</p></section>;
  }

  return (
    <section className={styles.financePage}>
      <div className={styles.hero}>
        <div>
          <button className={styles.backButton} type="button" onClick={() => router.push("/sales")}><Icon name="arrow" /> Manajemen penjualan</button>
          <span className={styles.eyebrow}>Arus kas penjualan</span>
          <h1>Pembayaran &amp; piutang</h1>
          <p>Pantau tagihan {organization?.name ? `untuk ${organization.name}` : "organisasi aktif"}, catat penerimaan, dan prioritaskan piutang yang mendekati atau melewati jatuh tempo.</p>
        </div>
        <div className={styles.heroActions}>
          {!canWrite && <span className={styles.readOnlyBadge}>Mode baca</span>}
          <button className={styles.secondaryButton} type="button" disabled={isRefreshing || isLoading} onClick={() => void refresh()}><Icon name="refresh" /> {isRefreshing ? "Memuat..." : "Muat ulang"}</button>
          {canWrite && selectedEntry && !selectedEntry.receivable.isFullyPaid && <button className={styles.primaryButton} type="button" onClick={() => { setModalError(null); setEditor({ saleId: selectedEntry.sale.id, paymentId: null }); }}><Icon name="add" /> Terima pembayaran</button>}
        </div>
      </div>

      {notice && <div className={styles.notice} role="status"><span><Icon name="check" /></span><strong>{notice}</strong><button type="button" aria-label="Tutup pemberitahuan" onClick={() => setNotice(null)}><Icon name="close" /></button></div>}
      {pageError && <div className={styles.pageError} role="alert">{pageError}</div>}

      <div className={styles.metricGrid}>
        <article className={styles.metricCard}><span>Total tagihan</span><strong>{formatFinanceCurrency(summary.billed)}</strong><small>{entries.length} penjualan terkonfirmasi</small><i><Icon name="invoice" /></i></article>
        <article className={`${styles.metricCard} ${styles.metricPrimary}`}><span>Kas terkumpul</span><strong>{formatFinanceCurrency(summary.collected)}</strong><small>{summary.collectionRate.toLocaleString("id-ID", { maximumFractionDigits: 1 })}% dari tagihan</small><i><Icon name="wallet" /></i></article>
        <article className={styles.metricCard}><span>Sisa piutang</span><strong>{formatFinanceCurrency(summary.outstanding)}</strong><small>{summary.paidCount} tagihan lunas</small><i><Icon name="credit" /></i></article>
        <article className={`${styles.metricCard} ${summary.overdueCount > 0 ? styles.metricWarning : ""}`}><span>Melewati jatuh tempo</span><strong>{summary.overdueCount}</strong><small>{summary.overdueCount > 0 ? "Perlu ditindaklanjuti" : "Tidak ada tunggakan"}</small><i><Icon name="clock" /></i></article>
      </div>

      <div className={styles.collectionStrip}>
        <span><Icon name="trend" /></span>
        <div><strong>Rasio penagihan {summary.collectionRate.toLocaleString("id-ID", { maximumFractionDigits: 1 })}%</strong><small>Pembayaran hanya masuk kas setelah dikonfirmasi. Draf tidak mengubah saldo.</small></div>
        <div className={styles.progressTrack}><i style={{ width: `${Math.min(100, summary.collectionRate)}%` }} /></div>
      </div>

      <div className={styles.toolbar}>
        <label className={styles.searchField}><Icon name="search" /><input value={query} placeholder="Cari kode, pembeli, telepon, atau jatuh tempo" aria-label="Cari piutang" onChange={(event) => setQuery(event.target.value)} /></label>
        <label className={styles.filterField}><span>Status pembayaran</span><select value={stateFilter} onChange={(event) => setStateFilter(event.target.value === "all" ? "all" : Number(event.target.value) as ReceivableStateFilter)}><option value="all">Semua status</option>{Object.entries(paymentStateLabels).map(([value, label]) => <option value={value} key={value}>{label}</option>)}</select></label>
        <label className={styles.filterField}><span>Jatuh tempo</span><select value={dueFilter} onChange={(event) => setDueFilter(event.target.value as DueStateFilter)}><option value="all">Semua tagihan</option><option value="overdue">Terlambat</option><option value="due-soon">Jatuh tempo ≤ 7 hari</option></select></label>
        <span className={styles.resultCount}>{filteredEntries.length} hasil</span>
      </div>

      {isLoading ? (
        <div className={styles.loadingState}><span className="loader" /><p>Memuat pembayaran dan piutang...</p></div>
      ) : entries.length === 0 ? (
        <div className={styles.emptyState}><span><Icon name="credit" /></span><h2>Belum ada tagihan penjualan</h2><p>Konfirmasikan penjualan terlebih dahulu agar tagihan dan pembayaran dapat dikelola.</p><button className={styles.secondaryButton} type="button" onClick={() => router.push("/sales")}>Buka penjualan</button></div>
      ) : filteredEntries.length === 0 ? (
        <div className={styles.emptyState}><span><Icon name="search" /></span><h2>Tidak ada hasil yang sesuai</h2><p>Ubah kata pencarian atau filter untuk melihat tagihan lain.</p><button className={styles.secondaryButton} type="button" onClick={() => { setQuery(""); setStateFilter("all"); setDueFilter("all"); }}>Bersihkan filter</button></div>
      ) : (
        <div className={styles.managementGrid}>
          <aside className={styles.receivableList}>
            <header><div><span className={styles.eyebrow}>Daftar tagihan</span><h2>{filteredEntries.length} penjualan</h2></div></header>
            <div className={styles.receivableCards}>
              {filteredEntries.map((entry) => {
                const overdue = isOverdue(entry, today);
                const dueSoon = isDueSoon(entry, today);
                return <button className={`${styles.receivableCard} ${entry.sale.id === selectedEntry?.sale.id ? styles.receivableCardSelected : ""}`} type="button" aria-pressed={entry.sale.id === selectedEntry?.sale.id} key={entry.sale.id} onClick={() => setSelectedSaleId(entry.sale.id)}><span className={styles.cardTopline}><strong>{entry.sale.code}</strong><i className={`${styles.stateBadge} ${styles[`state${entry.receivable.paymentState}`]}`}>{paymentStateLabels[entry.receivable.paymentState]}</i></span><b>{formatFinanceCurrency(entry.receivable.outstandingReceivable)}</b><span className={styles.cardMeta}><small>{entry.sale.buyerName}</small><small>{formatFinanceCurrency(entry.receivable.saleTotalAmount)}</small></span><span className={`${styles.dueLine} ${overdue ? styles.dueOverdue : dueSoon ? styles.dueSoon : ""}`}><Icon name="calendar" /> {dueCopy(entry, today)}</span></button>;
              })}
            </div>
          </aside>

          {selectedEntry && (
            <article className={styles.receivableDetail}>
              <header className={styles.detailHeader}>
                <div className={styles.detailIdentity}><span className={styles.detailIcon}><Icon name="invoice" /></span><div><span>{selectedEntry.sale.code}</span><h2>{selectedEntry.sale.buyerName}</h2><p>{formatFinanceDate(selectedEntry.sale.saleDate)} · {selectedEntry.sale.paymentTerm === 1 ? "Tunai" : "Kredit"}</p></div></div>
                <div className={styles.detailActions}>
                  <span className={`${styles.stateBadge} ${styles[`state${selectedEntry.receivable.paymentState}`]}`}>{paymentStateLabels[selectedEntry.receivable.paymentState]}</span>
                  {canWrite && !selectedEntry.receivable.isFullyPaid && <button className={styles.primaryButton} type="button" onClick={() => { setModalError(null); setEditor({ saleId: selectedEntry.sale.id, paymentId: null }); }}><Icon name="add" /> Terima pembayaran</button>}
                </div>
              </header>

              <div className={styles.amountGrid}>
                <div><span>Total tagihan</span><strong>{formatFinanceCurrency(selectedEntry.receivable.saleTotalAmount)}</strong></div>
                <div><span>Sudah diterima</span><strong>{formatFinanceCurrency(selectedEntry.receivable.confirmedPaidAmount)}</strong></div>
                <div className={styles.amountTotal}><span>Sisa piutang</span><strong>{formatFinanceCurrency(selectedEntry.receivable.outstandingReceivable)}</strong></div>
              </div>

              <div className={styles.infoGrid}>
                <section><i><Icon name="user" /></i><span><small>Pembeli</small><strong>{selectedEntry.sale.buyerName}</strong></span></section>
                <section><i><Icon name="calendar" /></i><span><small>Jatuh tempo</small><strong>{selectedEntry.receivable.dueDate ? formatFinanceDate(selectedEntry.receivable.dueDate) : "Tidak ditentukan"}</strong></span></section>
                <section><i><Icon name="clock" /></i><span><small>Kondisi tagihan</small><strong className={isOverdue(selectedEntry, today) ? styles.dangerText : ""}>{dueCopy(selectedEntry, today)}</strong></span></section>
                <section><i><Icon name="wallet" /></i><span><small>Kas terkumpul</small><strong>{selectedEntry.receivable.hasCollectedRevenue ? "Sudah ada penerimaan" : "Belum ada penerimaan"}</strong></span></section>
              </div>

              <section className={styles.paymentSection}>
                <header><div><span className={styles.eyebrow}>Riwayat penerimaan</span><h3>{selectedEntry.payments.length} pembayaran</h3></div></header>
                {selectedEntry.payments.length === 0 ? (
                  <div className={styles.paymentEmpty}><Icon name="wallet" /><strong>Belum ada pembayaran</strong><p>Catat pembayaran ketika bukti penerimaan dana sudah tersedia.</p></div>
                ) : (
                  <div className={styles.paymentList}>{selectedEntry.payments.map((payment) => <article className={styles.paymentCard} key={payment.id}><div className={styles.paymentMain}><span className={styles.paymentIcon}><Icon name={payment.paymentMethod === 1 ? "money" : payment.paymentMethod === 2 ? "bank" : "wallet"} /></span><div><strong>{payment.code}</strong><span>{formatFinanceDate(payment.paymentDate)} · {paymentMethodLabels[payment.paymentMethod]}</span></div></div><div className={styles.paymentAmount}><strong>{formatFinanceCurrency(payment.amount)}</strong><span className={`${styles.paymentStatus} ${styles[`paymentStatus${payment.status}`]}`}>{paymentStatusLabels[payment.status]}</span></div><div className={styles.paymentMeta}><span><small>Referensi</small><strong>{payment.referenceNumber ?? "—"}</strong></span><span><small>Diterima dari</small><strong>{payment.receivedFrom ?? selectedEntry.sale.buyerName}</strong></span></div>{payment.notes && <p className={styles.paymentNotes}>{payment.notes}</p>}{payment.cancellationReason && <p className={styles.cancellationNote}><strong>Dibatalkan:</strong> {payment.cancellationReason}</p>}{canWrite && payment.status !== 3 && <div className={styles.paymentActions}>{payment.status === 1 && <><button type="button" onClick={() => { setModalError(null); setEditor({ saleId: selectedEntry.sale.id, paymentId: payment.id }); }}><Icon name="edit" /> Ubah</button><button className={styles.confirmTextButton} type="button" onClick={() => { setModalError(null); setAction({ kind: "confirm", saleId: selectedEntry.sale.id, paymentId: payment.id }); }}><Icon name="check" /> Konfirmasi</button></>}<button className={styles.dangerTextButton} type="button" onClick={() => { setModalError(null); setAction({ kind: "cancel", saleId: selectedEntry.sale.id, paymentId: payment.id }); }}><Icon name="stop" /> Batalkan</button></div>}</article>)}</div>
                )}
              </section>

              <footer className={styles.detailFooter}><span>Pembayaran terkonfirmasi menentukan kas terkumpul dan saldo piutang.</span><button type="button" onClick={() => router.push(`/sales`)}>Lihat transaksi penjualan</button></footer>
            </article>
          )}
        </div>
      )}

      {editor && editorEntry && (
        <div className={styles.modalBackdrop} role="presentation" onMouseDown={(event) => { if (event.target === event.currentTarget && !isSaving) setEditor(null); }}>
          <div className={styles.modalPanel} role="dialog" aria-modal="true" aria-label={editedPayment ? "Ubah pembayaran" : "Catat pembayaran"}><PaymentEditor key={editor.paymentId ?? `create-${editor.saleId}`} sale={editorEntry.sale} payment={editedPayment} outstanding={editorEntry.receivable.outstandingReceivable} isSaving={isSaving} apiError={modalError} onCancel={() => { setEditor(null); setModalError(null); }} onSubmit={submitPayment} /></div>
        </div>
      )}
      {action && actionEntry && actionPayment && (
        <div className={styles.modalBackdrop} role="presentation" onMouseDown={(event) => { if (event.target === event.currentTarget && !isSaving) setAction(null); }}>
          <div className={styles.actionPanel} role="dialog" aria-modal="true" aria-label="Tindakan pembayaran"><PaymentAction key={`${action.kind}-${action.paymentId}`} action={action} entry={actionEntry} payment={actionPayment} isSaving={isSaving} apiError={modalError} onCancel={() => { setAction(null); setModalError(null); }} onSubmit={submitAction} /></div>
        </div>
      )}
    </section>
  );
}
