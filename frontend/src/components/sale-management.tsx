"use client";

import { useCallback, useEffect, useMemo, useState, type FormEvent } from "react";
import { useRouter } from "next/navigation";
import {
  ApiError,
  addSaleLine,
  cancelSale,
  confirmSale,
  createSale,
  getCropCycles,
  getHarvestBatches,
  getSales,
  removeSaleLine,
  updateSale,
  updateSaleLine,
} from "@/lib/api/client";
import type {
  AddSaleLineRequest,
  CreateSaleRequest,
  Organization,
  Sale,
  SaleLine,
  SalePaymentTerm,
  SaleStatus,
} from "@/lib/api/contracts";
import {
  calculateSaleLineTotal,
  filterSales,
  formatSaleCurrency,
  formatSaleDate,
  formatSaleQuantity,
  optionalSaleText,
  parseSaleNumber,
  saleDraftFrom,
  saleLineDraftFrom,
  salePaymentTermLabels,
  saleStatusLabels,
  selectableInventory,
  summarizeSales,
  validateSaleDraft,
  validateSaleLineDraft,
  type HarvestInventoryItem,
  type SaleDraft,
  type SaleLineDraft,
  type SalePaymentTermFilter,
  type SaleStatusFilter,
} from "@/lib/sales/sale-management";
import styles from "./sale-management.module.css";

type Props = {
  organization: Organization | null;
  organizationId: string | null;
  permissions: string[];
};

type EditorState = { saleId: string | null };
type LineEditorState = { saleId: string; saleLineId: string | null };
type ActionState = {
  kind: "confirm" | "cancel" | "remove-line";
  saleId: string;
  saleLineId?: string;
};

type IconName =
  | "add" | "arrow" | "buyer" | "calendar" | "check" | "close"
  | "credit" | "edit" | "harvest" | "invoice" | "money" | "phone"
  | "refresh" | "remove" | "search" | "stop" | "trend";

const iconPaths: Record<IconName, string> = {
  add: "M12 5v14M5 12h14",
  arrow: "m15 18-6-6 6-6",
  buyer: "M12 12a4 4 0 1 0 0-8 4 4 0 0 0 0 8ZM4 21c0-4 3-7 8-7s8 3 8 7",
  calendar: "M6 3v3m12-3v3M4 9h16M5 5h14a1 1 0 0 1 1 1v14H4V6a1 1 0 0 1 1-1Z",
  check: "m5 12 4 4L19 6",
  close: "m6 6 12 12M18 6 6 18",
  credit: "M3 6h18v12H3V6Zm0 4h18M7 15h4",
  edit: "m4 20 4.5-1 10-10a2.1 2.1 0 0 0-3-3l-10 10L4 20Zm10-12 3 3",
  harvest: "M5 20h14M7 20V9m4 11V5m4 15V8m4 12V4",
  invoice: "M6 3h12v18l-3-2-3 2-3-2-3 2V3Zm3 5h6m-6 4h6m-6 4h4",
  money: "M12 3v18m4-14H9.5a3 3 0 0 0 0 6h5a3 3 0 0 1 0 6H7",
  phone: "M6.5 3h3l1.5 5-2 1.5a15 15 0 0 0 5.5 5.5L16 13l5 1.5v3c0 2-1.5 3.5-3.5 3.5C9.5 21 3 14.5 3 6.5 3 4.5 4.5 3 6.5 3Z",
  refresh: "M20 6v5h-5M4 18v-5h5m10-2a7 7 0 0 0-12-4L4 11m16 2-3 4a7 7 0 0 1-12-4",
  remove: "M5 7h14M9 7V4h6v3m-8 0 1 14h8l1-14M10 11v6m4-6v6",
  search: "m21 21-4.35-4.35m2.35-5.65a8 8 0 1 1-16 0 8 8 0 0 1 16 0Z",
  stop: "M6 6h12v12H6V6Z",
  trend: "m4 17 5-5 4 4 7-8m-5 0h5v5",
};

function Icon({ name }: { name: IconName }) {
  return <svg viewBox="0 0 24 24" aria-hidden="true"><path d={iconPaths[name]} /></svg>;
}

function localToday(): string {
  const now = new Date();
  return new Date(now.getTime() - now.getTimezoneOffset() * 60_000)
    .toISOString().slice(0, 10);
}

function replaceSale(sales: Sale[], updated: Sale): Sale[] {
  return sales.some((sale) => sale.id === updated.id)
    ? sales.map((sale) => sale.id === updated.id ? updated : sale)
    : [...sales, updated];
}

function friendlyError(error: unknown): string {
  if (!(error instanceof ApiError)) {
    return error instanceof Error ? error.message : "Permintaan tidak dapat diselesaikan.";
  }
  switch (error.problem?.code) {
    case "Sales.CodeAlreadyExists":
      return "Kode penjualan sudah digunakan dalam organisasi ini.";
    case "Sales.HarvestBatchNotConfirmed":
      return "Batch panen belum dikonfirmasi dan belum dapat dijual.";
    case "Sales.QuantityUnitMismatch":
      return "Satuan item tidak sama dengan satuan batch panen.";
    case "Sales.InsufficientQuantity":
      return "Stok panen berubah atau tidak lagi mencukupi. Muat ulang lalu periksa kuantitas.";
    case "Sales.ConfirmationConcurrency":
      return "Stok berubah bersamaan dengan konfirmasi. Muat ulang transaksi lalu coba lagi.";
    case "Sales.ConfirmedPaymentsExist":
      return "Penjualan memiliki pembayaran terkonfirmasi. Batalkan pembayaran sebelum membatalkan penjualan.";
    case "Sales.InvalidStatusTransition":
      return "Tindakan tidak sesuai dengan status penjualan saat ini. Muat ulang lalu periksa kembali.";
    default:
      return error.message;
  }
}

function SaleEditor({
  sale,
  isSaving,
  apiError,
  onCancel,
  onSubmit,
}: {
  sale: Sale | null;
  isSaving: boolean;
  apiError: string | null;
  onCancel: () => void;
  onSubmit: (draft: SaleDraft) => Promise<void>;
}) {
  const [draft, setDraft] = useState<SaleDraft>(() => {
    const value = saleDraftFrom(sale);
    return sale ? value : { ...value, saleDate: localToday() };
  });
  const [errors, setErrors] = useState<string[]>([]);
  const isCreate = sale === null;

  function update<Key extends keyof SaleDraft>(key: Key, value: SaleDraft[Key]) {
    setDraft((current) => ({
      ...current,
      [key]: value,
      ...(key === "paymentTerm" && value === 1 ? { dueDate: "" } : {}),
    }));
  }

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const nextErrors = validateSaleDraft(draft, isCreate, sale?.subtotal ?? 0);
    setErrors(nextErrors);
    if (nextErrors.length === 0) void onSubmit(draft);
  }

  return (
    <form className={styles.editorForm} onSubmit={submit} noValidate>
      <header className={styles.editorHeader}>
        <span className={styles.editorIcon}><Icon name="invoice" /></span>
        <div>
          <span className={styles.eyebrow}>{isCreate ? "Transaksi baru" : sale.code}</span>
          <h2>{isCreate ? "Buat draf penjualan" : "Perbarui informasi penjualan"}</h2>
          <p>Informasi pembeli dan termin dapat diubah selama transaksi masih berstatus draf.</p>
        </div>
        <button className={styles.iconButton} type="button" aria-label="Tutup formulir" disabled={isSaving} onClick={onCancel}><Icon name="close" /></button>
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
            <span>Kode penjualan <em>*</em></span>
            <input value={draft.code} maxLength={40} disabled={!isCreate} placeholder="Contoh: PJL-001" onChange={(event) => update("code", event.target.value)} />
          </label>
          <label className={styles.field}>
            <span>Tanggal penjualan <em>*</em></span>
            <input type="date" value={draft.saleDate} onChange={(event) => update("saleDate", event.target.value)} />
          </label>
          <label className={`${styles.field} ${styles.fieldFull}`}>
            <span>Nama pembeli <em>*</em></span>
            <input value={draft.buyerName} maxLength={150} placeholder="Nama pelanggan, toko, koperasi, atau perusahaan" onChange={(event) => update("buyerName", event.target.value)} />
          </label>
          <label className={styles.field}>
            <span>Nomor telepon</span>
            <input value={draft.buyerPhone} maxLength={50} inputMode="tel" placeholder="08xxxxxxxxxx" onChange={(event) => update("buyerPhone", event.target.value)} />
          </label>
          <label className={styles.field}>
            <span>Termin pembayaran <em>*</em></span>
            <select value={draft.paymentTerm} onChange={(event) => update("paymentTerm", Number(event.target.value) as SalePaymentTerm)}>
              {Object.entries(salePaymentTermLabels).map(([value, label]) => <option value={value} key={value}>{label}</option>)}
            </select>
          </label>
          <label className={`${styles.field} ${styles.fieldFull}`}>
            <span>Alamat pembeli</span>
            <textarea value={draft.buyerAddress} maxLength={500} rows={3} placeholder="Alamat pengiriman atau penagihan" onChange={(event) => update("buyerAddress", event.target.value)} />
          </label>
          {draft.paymentTerm === 2 && (
            <label className={styles.field}>
              <span>Jatuh tempo <em>*</em></span>
              <input type="date" value={draft.dueDate} min={draft.saleDate || undefined} onChange={(event) => update("dueDate", event.target.value)} />
            </label>
          )}
          {!isCreate && (
            <label className={styles.field}>
              <span>Diskon transaksi</span>
              <input value={draft.discountAmount} inputMode="decimal" placeholder="0" onChange={(event) => update("discountAmount", event.target.value)} />
              <small>Maksimal {formatSaleCurrency(sale.subtotal)}</small>
            </label>
          )}
          <label className={`${styles.field} ${styles.fieldFull}`}>
            <span>Catatan</span>
            <textarea value={draft.notes} maxLength={1000} rows={4} placeholder="Jadwal kirim, syarat transaksi, atau keterangan lain" onChange={(event) => update("notes", event.target.value)} />
          </label>
        </div>
      </fieldset>

      {!isCreate && (
        <div className={styles.totalPreview}>
          <span><small>Subtotal</small><strong>{formatSaleCurrency(sale.subtotal)}</strong></span>
          <span><small>Total setelah diskon</small><strong>{formatSaleCurrency(Math.max(0, sale.subtotal - (parseSaleNumber(draft.discountAmount, true) ?? 0)))}</strong></span>
          <i><Icon name="money" /></i>
        </div>
      )}

      <footer className={styles.formActions}>
        <button className={styles.secondaryButton} type="button" disabled={isSaving} onClick={onCancel}>Batal</button>
        <button className={styles.primaryButton} type="submit" disabled={isSaving}>{isSaving ? "Menyimpan..." : isCreate ? "Simpan draf" : "Simpan perubahan"}</button>
      </footer>
    </form>
  );
}

function SaleLineEditor({
  sale,
  line,
  inventory,
  isSaving,
  apiError,
  onCancel,
  onSubmit,
}: {
  sale: Sale;
  line: SaleLine | null;
  inventory: HarvestInventoryItem[];
  isSaving: boolean;
  apiError: string | null;
  onCancel: () => void;
  onSubmit: (request: AddSaleLineRequest) => Promise<void>;
}) {
  const availableInventory = useMemo(
    () => selectableInventory(inventory, sale, line?.id ?? null),
    [inventory, line?.id, sale],
  );
  const [draft, setDraft] = useState<SaleLineDraft>(() =>
    saleLineDraftFrom(line, availableInventory));
  const [errors, setErrors] = useState<string[]>([]);
  const selectedItem = availableInventory.find((item) =>
    item.batch.id === draft.harvestBatchId) ?? null;
  const isCreate = line === null;

  function update<Key extends keyof SaleLineDraft>(key: Key, value: SaleLineDraft[Key]) {
    setDraft((current) => ({ ...current, [key]: value }));
  }

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const nextErrors = validateSaleLineDraft(draft, selectedItem?.batch ?? null);
    setErrors(nextErrors);
    const quantity = parseSaleNumber(draft.quantity);
    const unitPrice = parseSaleNumber(draft.unitPrice, true);
    const lineDiscount = parseSaleNumber(draft.lineDiscount, true);
    if (nextErrors.length > 0 || !selectedItem || quantity === null
      || unitPrice === null || lineDiscount === null) return;
    void onSubmit({
      harvestBatchId: selectedItem.batch.id,
      quantity,
      quantityUnit: selectedItem.batch.quantityUnit,
      unitPrice,
      lineDiscount,
      notes: optionalSaleText(draft.notes),
    });
  }

  return (
    <form className={styles.editorForm} onSubmit={submit} noValidate>
      <header className={styles.editorHeader}>
        <span className={styles.editorIcon}><Icon name="harvest" /></span>
        <div>
          <span className={styles.eyebrow}>{sale.code}</span>
          <h2>{isCreate ? "Tambahkan hasil panen" : `Ubah ${line.harvestBatchCodeSnapshot}`}</h2>
          <p>Satu batch panen hanya dapat digunakan sekali dalam transaksi yang sama.</p>
        </div>
        <button className={styles.iconButton} type="button" aria-label="Tutup formulir" disabled={isSaving} onClick={onCancel}><Icon name="close" /></button>
      </header>

      {(errors.length > 0 || apiError) && <div className={styles.formAlert} role="alert"><strong>Periksa item penjualan:</strong><ul>{errors.map((error) => <li key={error}>{error}</li>)}{apiError && <li>{apiError}</li>}</ul></div>}

      <fieldset disabled={isSaving}>
        <div className={styles.formGrid}>
          <label className={`${styles.field} ${styles.fieldFull}`}>
            <span>Batch panen <em>*</em></span>
            <select value={draft.harvestBatchId} disabled={!isCreate || availableInventory.length === 0} onChange={(event) => update("harvestBatchId", event.target.value)}>
              {availableInventory.length === 0 && <option value="">Tidak ada stok tersedia</option>}
              {availableInventory.map((item) => <option value={item.batch.id} key={item.batch.id}>{item.cropCycleCode} · {item.batch.code} · {formatSaleQuantity(item.batch.availableQuantity, item.batch.quantityUnit)} tersedia</option>)}
            </select>
          </label>
          {selectedItem && (
            <div className={`${styles.inventoryContext} ${styles.fieldFull}`}>
              <span><small>Siklus</small><strong>{selectedItem.cropCycleCode} · {selectedItem.cropCycleName}</strong></span>
              <span><small>Mutu</small><strong>{selectedItem.batch.qualityGrade ?? "Belum dinilai"}</strong></span>
              <span><small>Stok tersedia</small><strong>{formatSaleQuantity(selectedItem.batch.availableQuantity, selectedItem.batch.quantityUnit)}</strong></span>
            </div>
          )}
          <label className={styles.field}>
            <span>Kuantitas <em>*</em></span>
            <input value={draft.quantity} inputMode="decimal" placeholder="0" onChange={(event) => update("quantity", event.target.value)} />
          </label>
          <label className={styles.field}>
            <span>Harga per satuan <em>*</em></span>
            <input value={draft.unitPrice} inputMode="decimal" placeholder="0" onChange={(event) => update("unitPrice", event.target.value)} />
          </label>
          <label className={styles.field}>
            <span>Diskon item</span>
            <input value={draft.lineDiscount} inputMode="decimal" placeholder="0" onChange={(event) => update("lineDiscount", event.target.value)} />
          </label>
          <label className={styles.field}>
            <span>Satuan</span>
            <input value={selectedItem ? formatSaleQuantity(1, selectedItem.batch.quantityUnit).replace(/^1\s/, "") : "—"} disabled />
          </label>
          <label className={`${styles.field} ${styles.fieldFull}`}>
            <span>Catatan item</span>
            <textarea value={draft.notes} maxLength={500} rows={3} placeholder="Mutu khusus, kemasan, atau catatan pengiriman" onChange={(event) => update("notes", event.target.value)} />
          </label>
        </div>
      </fieldset>

      <div className={styles.totalPreview}>
        <span><small>Nilai item</small><strong>{formatSaleCurrency(calculateSaleLineTotal(draft))}</strong></span>
        <span><small>Stok sesudah dialokasikan</small><strong>{selectedItem ? formatSaleQuantity(Math.max(0, selectedItem.batch.availableQuantity - (parseSaleNumber(draft.quantity) ?? 0)), selectedItem.batch.quantityUnit) : "—"}</strong></span>
        <i><Icon name="trend" /></i>
      </div>

      <footer className={styles.formActions}>
        <button className={styles.secondaryButton} type="button" disabled={isSaving} onClick={onCancel}>Batal</button>
        <button className={styles.primaryButton} type="submit" disabled={isSaving || availableInventory.length === 0}>{isSaving ? "Menyimpan..." : isCreate ? "Tambahkan item" : "Simpan perubahan"}</button>
      </footer>
    </form>
  );
}

function SaleAction({
  action,
  sale,
  line,
  isSaving,
  apiError,
  onCancel,
  onSubmit,
}: {
  action: ActionState;
  sale: Sale;
  line: SaleLine | null;
  isSaving: boolean;
  apiError: string | null;
  onCancel: () => void;
  onSubmit: (reason: string) => Promise<void>;
}) {
  const [reason, setReason] = useState("");
  const [validationError, setValidationError] = useState<string | null>(null);
  const isConfirm = action.kind === "confirm";
  const isRemove = action.kind === "remove-line";

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const normalized = reason.trim();
    if (action.kind === "cancel" && !normalized) {
      setValidationError("Alasan pembatalan wajib diisi.");
      return;
    }
    if (normalized.length > 500) {
      setValidationError("Alasan pembatalan maksimal 500 karakter.");
      return;
    }
    void onSubmit(normalized);
  }

  const title = isConfirm ? "Konfirmasi penjualan?"
    : isRemove ? "Hapus item dari draf?" : "Batalkan penjualan?";
  const copy = isConfirm
    ? "Stok setiap batch akan dialokasikan secara permanen dan nilai transaksi masuk ke pendapatan. Data tidak dapat diubah setelah dikonfirmasi."
    : isRemove
      ? "Item akan dikeluarkan dari draf dan total transaksi dihitung ulang."
      : "Transaksi tetap tersimpan sebagai jejak. Jika sudah dikonfirmasi, stok akan dilepaskan kembali selama belum ada pembayaran terkonfirmasi.";

  return (
    <form className={styles.actionDialog} onSubmit={submit} noValidate>
      <div className={`${styles.actionIcon} ${!isConfirm ? styles.actionIconDanger : ""}`}><Icon name={isConfirm ? "check" : isRemove ? "remove" : "stop"} /></div>
      <span className={styles.eyebrow}>{sale.code}</span>
      <h2>{title}</h2>
      <p>{copy}</p>
      <div className={styles.actionSummary}>
        <span><small>{isRemove ? "Item" : "Total transaksi"}</small><strong>{isRemove && line ? line.harvestBatchCodeSnapshot : formatSaleCurrency(sale.totalAmount)}</strong></span>
        <span><small>{isRemove ? "Nilai item" : "Pembeli"}</small><strong>{isRemove && line ? formatSaleCurrency(line.lineTotal) : sale.buyerName}</strong></span>
      </div>
      {action.kind === "cancel" && <label className={styles.field}><span>Alasan pembatalan <em>*</em></span><textarea value={reason} maxLength={500} rows={4} autoFocus placeholder="Jelaskan alasan pembatalan transaksi" disabled={isSaving} onChange={(event) => setReason(event.target.value)} /></label>}
      {(validationError || apiError) && <div className={styles.formAlert} role="alert"><ul>{validationError && <li>{validationError}</li>}{apiError && <li>{apiError}</li>}</ul></div>}
      <div className={styles.actionButtons}>
        <button className={styles.secondaryButton} type="button" disabled={isSaving} onClick={onCancel}>Kembali</button>
        <button className={isConfirm ? styles.primaryButton : styles.dangerButton} type="submit" disabled={isSaving}>{isSaving ? "Memproses..." : isConfirm ? "Konfirmasi penjualan" : isRemove ? "Hapus item" : "Batalkan penjualan"}</button>
      </div>
    </form>
  );
}

export function SaleManagement({ organization, organizationId, permissions }: Props) {
  const router = useRouter();
  const [sales, setSales] = useState<Sale[]>([]);
  const [inventory, setInventory] = useState<HarvestInventoryItem[]>([]);
  const [selectedSaleId, setSelectedSaleId] = useState("");
  const [query, setQuery] = useState("");
  const [statusFilter, setStatusFilter] = useState<SaleStatusFilter>("all");
  const [paymentFilter, setPaymentFilter] = useState<SalePaymentTermFilter>("all");
  const [editor, setEditor] = useState<EditorState | null>(null);
  const [lineEditor, setLineEditor] = useState<LineEditorState | null>(null);
  const [action, setAction] = useState<ActionState | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [pageError, setPageError] = useState<string | null>(null);
  const [modalError, setModalError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);

  const canRead = permissions.includes("sales.read");
  const canWrite = permissions.includes("sales.write");
  const canReadHarvest = permissions.includes("harvest.read");
  const canReadFinance = permissions.includes("finance.read");
  const filteredSales = useMemo(
    () => filterSales(sales, query, statusFilter, paymentFilter),
    [paymentFilter, query, sales, statusFilter],
  );
  const selectedSale = filteredSales.find((sale) => sale.id === selectedSaleId)
    ?? filteredSales[0] ?? null;
  const summary = useMemo(() => summarizeSales(sales), [sales]);
  const editorSale = editor?.saleId
    ? sales.find((sale) => sale.id === editor.saleId) ?? null : null;
  const lineEditorSale = lineEditor
    ? sales.find((sale) => sale.id === lineEditor.saleId) ?? null : null;
  const editedLine = lineEditor?.saleLineId && lineEditorSale
    ? lineEditorSale.lines.find((line) => line.id === lineEditor.saleLineId) ?? null
    : null;
  const actionSale = action
    ? sales.find((sale) => sale.id === action.saleId) ?? null : null;
  const actionLine = action?.saleLineId && actionSale
    ? actionSale.lines.find((line) => line.id === action.saleLineId) ?? null : null;
  const availableBatchCount = inventory.filter((item) => item.batch.availableQuantity > 0).length;

  const loadData = useCallback(async (): Promise<{
    sales: Sale[];
    inventory: HarvestInventoryItem[];
  }> => {
    if (!organizationId) return { sales: [], inventory: [] };
    const [nextSales, cycles] = await Promise.all([
      getSales(organizationId),
      canReadHarvest ? getCropCycles(organizationId) : Promise.resolve([]),
    ]);
    const batchGroups = await Promise.all(cycles.map(async (cycle) => ({
      cycle,
      batches: await getHarvestBatches(organizationId, cycle.id, { status: 2 }),
    })));
    return {
      sales: nextSales,
      inventory: batchGroups.flatMap(({ cycle, batches }) => batches.map((batch) => ({
        batch,
        cropCycleCode: cycle.code,
        cropCycleName: cycle.name,
      }))),
    };
  }, [canReadHarvest, organizationId]);

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
        setSales(result.sales);
        setInventory(result.inventory);
        setSelectedSaleId((current) => result.sales.some((sale) => sale.id === current)
          ? current : result.sales[0]?.id ?? "");
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
    if (!editor && !lineEditor && !action) return;
    const overflow = document.body.style.overflow;
    document.body.style.overflow = "hidden";
    function close(event: KeyboardEvent) {
      if (event.key === "Escape" && !isSaving) {
        setEditor(null);
        setLineEditor(null);
        setAction(null);
        setModalError(null);
      }
    }
    window.addEventListener("keydown", close);
    return () => {
      document.body.style.overflow = overflow;
      window.removeEventListener("keydown", close);
    };
  }, [action, editor, isSaving, lineEditor]);

  function applyUpdatedSale(updated: Sale, message: string) {
    setSales((current) => replaceSale(current, updated));
    setSelectedSaleId(updated.id);
    setNotice(message);
    setPageError(null);
  }

  async function refresh() {
    setIsRefreshing(true);
    setPageError(null);
    try {
      const result = await loadData();
      setSales(result.sales);
      setInventory(result.inventory);
      setSelectedSaleId((current) => result.sales.some((sale) => sale.id === current)
        ? current : result.sales[0]?.id ?? "");
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

  async function submitSale(draft: SaleDraft) {
    if (!organizationId || !canWrite) return;
    setIsSaving(true);
    setModalError(null);
    try {
      const common: CreateSaleRequest = {
        code: draft.code.trim().toUpperCase(),
        saleDate: draft.saleDate,
        buyerName: draft.buyerName.trim(),
        buyerPhone: optionalSaleText(draft.buyerPhone),
        buyerAddress: optionalSaleText(draft.buyerAddress),
        paymentTerm: draft.paymentTerm,
        dueDate: draft.paymentTerm === 2 ? draft.dueDate : null,
        notes: optionalSaleText(draft.notes),
      };
      const updated = editor?.saleId
        ? await updateSale(organizationId, editor.saleId, {
          saleDate: common.saleDate,
          buyerName: common.buyerName,
          buyerPhone: common.buyerPhone,
          buyerAddress: common.buyerAddress,
          paymentTerm: common.paymentTerm,
          dueDate: common.dueDate,
          discountAmount: parseSaleNumber(draft.discountAmount, true) ?? 0,
          notes: common.notes,
        })
        : await createSale(organizationId, common);
      applyUpdatedSale(updated, editor?.saleId
        ? "Informasi penjualan berhasil diperbarui."
        : "Draf penjualan dibuat. Tambahkan hasil panen yang dijual.");
      setEditor(null);
      if (!editor?.saleId) setLineEditor({ saleId: updated.id, saleLineId: null });
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

  async function submitLine(request: AddSaleLineRequest) {
    if (!organizationId || !lineEditor || !canWrite) return;
    setIsSaving(true);
    setModalError(null);
    try {
      const updated = lineEditor.saleLineId
        ? await updateSaleLine(organizationId, lineEditor.saleId, lineEditor.saleLineId, {
          quantity: request.quantity,
          unitPrice: request.unitPrice,
          lineDiscount: request.lineDiscount,
          notes: request.notes,
        })
        : await addSaleLine(organizationId, lineEditor.saleId, request);
      applyUpdatedSale(updated, lineEditor.saleLineId
        ? "Item penjualan berhasil diperbarui."
        : "Hasil panen berhasil ditambahkan ke draf penjualan.");
      setLineEditor(null);
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
      let updated: Sale;
      if (action.kind === "confirm") {
        updated = await confirmSale(organizationId, action.saleId);
      } else if (action.kind === "cancel") {
        updated = await cancelSale(organizationId, action.saleId, { cancellationReason: reason });
      } else {
        updated = await removeSaleLine(
          organizationId,
          action.saleId,
          action.saleLineId ?? "",
        );
      }
      applyUpdatedSale(updated, action.kind === "confirm"
        ? "Penjualan dikonfirmasi dan stok panen telah dialokasikan."
        : action.kind === "cancel"
          ? "Penjualan dibatalkan dan stok yang terikat telah dilepaskan."
          : "Item dihapus dan total draf dihitung ulang.");
      setAction(null);
      if (action.kind !== "remove-line") await refresh();
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
    return <section className={styles.accessState}><Icon name="invoice" /><h1>Pilih organisasi terlebih dahulu</h1><p>Setiap transaksi penjualan terikat pada satu organisasi aktif.</p></section>;
  }
  if (!canRead) {
    return <section className={styles.accessState}><Icon name="stop" /><h1>Akses penjualan tidak tersedia</h1><p>Peran Anda belum memiliki izin <strong>sales.read</strong>.</p></section>;
  }

  return (
    <section className={styles.salePage}>
      <div className={styles.hero}>
        <div>
          <button className={styles.backButton} type="button" onClick={() => router.push("/harvest")}><Icon name="arrow" /> Manajemen panen</button>
          <span className={styles.eyebrow}>Pendapatan usaha</span>
          <h1>Penjualan</h1>
          <p>Kelola transaksi hasil panen {organization?.name ? `untuk ${organization.name}` : "organisasi aktif"}, dari draf hingga stok teralokasi dan pendapatan diakui.</p>
        </div>
        <div className={styles.heroActions}>
          {!canWrite && <span className={styles.readOnlyBadge}>Mode baca</span>}
          <button className={styles.secondaryButton} type="button" disabled={isRefreshing || isLoading} onClick={() => void refresh()}><Icon name="refresh" /> {isRefreshing ? "Memuat..." : "Muat ulang"}</button>
          {canWrite && <button className={styles.primaryButton} type="button" onClick={() => { setModalError(null); setEditor({ saleId: null }); }}><Icon name="add" /> Buat penjualan</button>}
        </div>
      </div>

      {notice && <div className={styles.notice} role="status"><span><Icon name="check" /></span><strong>{notice}</strong><button type="button" aria-label="Tutup pemberitahuan" onClick={() => setNotice(null)}><Icon name="close" /></button></div>}
      {pageError && <div className={styles.pageError} role="alert">{pageError}</div>}

      <div className={styles.metricGrid}>
        <article className={styles.metricCard}><span>Total transaksi</span><strong>{summary.saleCount}</strong><small>{summary.confirmedCount} dikonfirmasi</small><i><Icon name="invoice" /></i></article>
        <article className={`${styles.metricCard} ${styles.metricPrimary}`}><span>Pendapatan diakui</span><strong>{formatSaleCurrency(summary.confirmedRevenue)}</strong><small>Penjualan terkonfirmasi</small><i><Icon name="trend" /></i></article>
        <article className={styles.metricCard}><span>Nilai draf</span><strong>{formatSaleCurrency(summary.draftValue)}</strong><small>Belum mengurangi stok</small><i><Icon name="money" /></i></article>
        <article className={styles.metricCard}><span>Penjualan kredit</span><strong>{formatSaleCurrency(summary.creditRevenue)}</strong><small>{canReadFinance ? "Kelola pada menu Keuangan" : "Memerlukan akses keuangan"}</small><i><Icon name="credit" /></i></article>
      </div>

      <div className={styles.inventoryStrip}>
        <span><Icon name="harvest" /></span>
        <div><strong>{availableBatchCount} batch panen memiliki stok tersedia</strong><small>Hanya batch terkonfirmasi yang dapat ditambahkan ke draf penjualan.</small></div>
        <button type="button" onClick={() => router.push("/harvest")}>Lihat stok panen</button>
      </div>

      <div className={styles.toolbar}>
        <label className={styles.searchField}><Icon name="search" /><input value={query} placeholder="Cari kode, pembeli, telepon, atau catatan" aria-label="Cari penjualan" onChange={(event) => setQuery(event.target.value)} /></label>
        <label className={styles.filterField}><span>Status</span><select value={statusFilter} onChange={(event) => setStatusFilter(event.target.value === "all" ? "all" : Number(event.target.value) as SaleStatus)}><option value="all">Semua status</option>{Object.entries(saleStatusLabels).map(([value, label]) => <option value={value} key={value}>{label}</option>)}</select></label>
        <label className={styles.filterField}><span>Termin</span><select value={paymentFilter} onChange={(event) => setPaymentFilter(event.target.value === "all" ? "all" : Number(event.target.value) as SalePaymentTerm)}><option value="all">Semua termin</option>{Object.entries(salePaymentTermLabels).map(([value, label]) => <option value={value} key={value}>{label}</option>)}</select></label>
        <span className={styles.resultCount}>{filteredSales.length} hasil</span>
      </div>

      {isLoading ? (
        <div className={styles.loadingState}><span className="loader" /><p>Memuat transaksi penjualan...</p></div>
      ) : sales.length === 0 ? (
        <div className={styles.emptyState}><span><Icon name="invoice" /></span><h2>Belum ada penjualan</h2><p>Buat draf transaksi pertama, lalu tambahkan hasil panen yang dijual.</p>{canWrite && <button className={styles.primaryButton} type="button" onClick={() => setEditor({ saleId: null })}><Icon name="add" /> Buat penjualan pertama</button>}</div>
      ) : filteredSales.length === 0 ? (
        <div className={styles.emptyState}><span><Icon name="search" /></span><h2>Tidak ada hasil yang sesuai</h2><p>Ubah kata pencarian atau filter untuk melihat transaksi lain.</p><button className={styles.secondaryButton} type="button" onClick={() => { setQuery(""); setStatusFilter("all"); setPaymentFilter("all"); }}>Bersihkan filter</button></div>
      ) : (
        <div className={styles.managementGrid}>
          <aside className={styles.saleList}>
            <header><div><span className={styles.eyebrow}>Daftar transaksi</span><h2>{filteredSales.length} penjualan</h2></div></header>
            <div className={styles.saleCards}>
              {filteredSales.map((sale) => <button className={`${styles.saleCard} ${sale.id === selectedSale?.id ? styles.saleCardSelected : ""}`} type="button" aria-pressed={sale.id === selectedSale?.id} key={sale.id} onClick={() => setSelectedSaleId(sale.id)}><span className={styles.cardTopline}><strong>{sale.code}</strong><i className={`${styles.statusBadge} ${styles[`status${sale.status}`]}`}>{saleStatusLabels[sale.status]}</i></span><b>{formatSaleCurrency(sale.totalAmount)}</b><span className={styles.cardMeta}><small>{sale.buyerName}</small><small>{formatSaleDate(sale.saleDate)}</small></span><span className={styles.termLine}><Icon name={sale.paymentTerm === 1 ? "money" : "credit"} /> {salePaymentTermLabels[sale.paymentTerm]}{sale.paymentTerm === 2 && sale.dueDate ? ` · jatuh tempo ${formatSaleDate(sale.dueDate)}` : ""}</span></button>)}
            </div>
          </aside>

          {selectedSale && (
            <article className={styles.saleDetail}>
              <header className={styles.detailHeader}>
                <div className={styles.detailIdentity}><span className={styles.detailIcon}><Icon name="invoice" /></span><div><span>{selectedSale.code}</span><h2>{formatSaleCurrency(selectedSale.totalAmount)}</h2><p>{selectedSale.buyerName} · {formatSaleDate(selectedSale.saleDate)}</p></div></div>
                <div className={styles.detailActions}>
                  <span className={`${styles.statusBadge} ${styles[`status${selectedSale.status}`]}`}>{saleStatusLabels[selectedSale.status]}</span>
                  {canWrite && selectedSale.status === 1 && <><button className={styles.secondaryButton} type="button" onClick={() => { setModalError(null); setEditor({ saleId: selectedSale.id }); }}><Icon name="edit" /> Ubah</button><button className={styles.primaryButton} type="button" disabled={selectedSale.lines.length === 0} title={selectedSale.lines.length === 0 ? "Tambahkan minimal satu item" : undefined} onClick={() => { setModalError(null); setAction({ kind: "confirm", saleId: selectedSale.id }); }}><Icon name="check" /> Konfirmasi</button></>}
                  {canReadFinance && selectedSale.status === 2 && <button className={styles.primaryButton} type="button" onClick={() => router.push(`/finance?saleId=${encodeURIComponent(selectedSale.id)}`)}><Icon name="money" /> Kelola pembayaran</button>}
                </div>
              </header>

              <div className={styles.amountGrid}>
                <div><span>Subtotal</span><strong>{formatSaleCurrency(selectedSale.subtotal)}</strong></div>
                <div><span>Diskon transaksi</span><strong>{formatSaleCurrency(selectedSale.discountAmount)}</strong></div>
                <div className={styles.amountTotal}><span>Total penjualan</span><strong>{formatSaleCurrency(selectedSale.totalAmount)}</strong></div>
              </div>

              <div className={styles.infoGrid}>
                <section><i><Icon name="buyer" /></i><span><small>Pembeli</small><strong>{selectedSale.buyerName}</strong></span></section>
                <section><i><Icon name="phone" /></i><span><small>Kontak</small><strong>{selectedSale.buyerPhone ?? "Belum dicatat"}</strong></span></section>
                <section><i><Icon name={selectedSale.paymentTerm === 1 ? "money" : "credit"} /></i><span><small>Termin</small><strong>{salePaymentTermLabels[selectedSale.paymentTerm]}</strong></span></section>
                <section><i><Icon name="calendar" /></i><span><small>Jatuh tempo</small><strong>{selectedSale.paymentTerm === 2 ? formatSaleDate(selectedSale.dueDate) : "Dibayar langsung"}</strong></span></section>
              </div>

              <section className={styles.lineSection}>
                <header><div><span className={styles.eyebrow}>Rincian hasil panen</span><h3>{selectedSale.lines.length} item penjualan</h3></div>{canWrite && selectedSale.status === 1 && <button className={styles.secondaryButton} type="button" onClick={() => { setModalError(null); setLineEditor({ saleId: selectedSale.id, saleLineId: null }); }}><Icon name="add" /> Tambah item</button>}</header>
                {selectedSale.lines.length === 0 ? (
                  <div className={styles.lineEmpty}><Icon name="harvest" /><strong>Belum ada hasil panen</strong><p>Tambahkan minimal satu batch sebelum mengonfirmasi penjualan.</p></div>
                ) : (
                  <div className={styles.lineList}>{selectedSale.lines.map((line) => <article className={styles.lineCard} key={line.id}><div className={styles.lineMain}><span className={styles.lineIcon}><Icon name="harvest" /></span><div><strong>{line.commodityNameSnapshot}</strong><span>{line.cropCycleCodeSnapshot} · {line.harvestBatchCodeSnapshot} · {line.qualityGradeSnapshot ?? "Mutu belum dicatat"}</span></div></div><div className={styles.lineNumbers}><span><small>Kuantitas</small><strong>{formatSaleQuantity(line.quantity, line.quantityUnit)}</strong></span><span><small>Harga satuan</small><strong>{formatSaleCurrency(line.unitPrice)}</strong></span><span><small>Diskon</small><strong>{formatSaleCurrency(line.lineDiscount)}</strong></span><span><small>Total</small><strong>{formatSaleCurrency(line.lineTotal)}</strong></span></div>{line.notes && <p className={styles.lineNotes}>{line.notes}</p>}{canWrite && selectedSale.status === 1 && <div className={styles.lineActions}><button type="button" onClick={() => { setModalError(null); setLineEditor({ saleId: selectedSale.id, saleLineId: line.id }); }}><Icon name="edit" /> Ubah</button><button className={styles.dangerTextButton} type="button" onClick={() => { setModalError(null); setAction({ kind: "remove-line", saleId: selectedSale.id, saleLineId: line.id }); }}><Icon name="remove" /> Hapus</button></div>}</article>)}</div>
                )}
              </section>

              <section className={styles.notesPanel}><span className={styles.eyebrow}>Informasi tambahan</span><div><strong>Alamat pembeli</strong><p>{selectedSale.buyerAddress ?? "Belum ada alamat pengiriman atau penagihan."}</p></div><div><strong>Catatan transaksi</strong><p>{selectedSale.notes ?? "Belum ada catatan tambahan."}</p></div></section>
              {selectedSale.cancellationReason && <section className={styles.cancellationPanel}><strong>Alasan pembatalan</strong><p>{selectedSale.cancellationReason}</p></section>}

              {canWrite && selectedSale.status !== 3 && <footer className={styles.detailFooter}><span>{selectedSale.status === 2 ? "Pembatalan melepaskan stok selama belum ada pembayaran terkonfirmasi." : "Draf dapat dibatalkan tanpa memengaruhi stok panen."}</span><button className={styles.dangerTextButton} type="button" onClick={() => { setModalError(null); setAction({ kind: "cancel", saleId: selectedSale.id }); }}><Icon name="stop" /> Batalkan penjualan</button></footer>}
            </article>
          )}
        </div>
      )}

      {editor && (
        <div className={styles.modalBackdrop} role="presentation" onMouseDown={(event) => { if (event.target === event.currentTarget && !isSaving) setEditor(null); }}>
          <div className={styles.modalPanel} role="dialog" aria-modal="true" aria-label={editorSale ? "Ubah penjualan" : "Buat penjualan"}><SaleEditor key={editor.saleId ?? "create"} sale={editorSale} isSaving={isSaving} apiError={modalError} onCancel={() => { setEditor(null); setModalError(null); }} onSubmit={submitSale} /></div>
        </div>
      )}
      {lineEditor && lineEditorSale && (
        <div className={styles.modalBackdrop} role="presentation" onMouseDown={(event) => { if (event.target === event.currentTarget && !isSaving) setLineEditor(null); }}>
          <div className={styles.modalPanel} role="dialog" aria-modal="true" aria-label={editedLine ? "Ubah item penjualan" : "Tambah item penjualan"}><SaleLineEditor key={lineEditor.saleLineId ?? `create-${lineEditor.saleId}`} sale={lineEditorSale} line={editedLine} inventory={inventory} isSaving={isSaving} apiError={modalError} onCancel={() => { setLineEditor(null); setModalError(null); }} onSubmit={submitLine} /></div>
        </div>
      )}
      {action && actionSale && (
        <div className={styles.modalBackdrop} role="presentation" onMouseDown={(event) => { if (event.target === event.currentTarget && !isSaving) setAction(null); }}>
          <div className={styles.actionPanel} role="dialog" aria-modal="true" aria-label="Tindakan penjualan"><SaleAction key={`${action.kind}-${action.saleId}-${action.saleLineId ?? ""}`} action={action} sale={actionSale} line={actionLine} isSaving={isSaving} apiError={modalError} onCancel={() => { setAction(null); setModalError(null); }} onSubmit={submitAction} /></div>
        </div>
      )}
    </section>
  );
}
