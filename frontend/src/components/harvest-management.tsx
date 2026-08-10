"use client";

import { useEffect, useMemo, useState, type FormEvent } from "react";
import { useRouter } from "next/navigation";
import {
  ApiError,
  cancelHarvestBatch,
  confirmHarvestBatch,
  createHarvestBatch,
  getCropCycles,
  getHarvestBatches,
  updateHarvestBatch,
} from "@/lib/api/client";
import type {
  CreateHarvestBatchRequest,
  CropCycle,
  HarvestBatch,
  HarvestBatchStatus,
  HarvestQuantityUnit,
  Organization,
} from "@/lib/api/contracts";
import {
  filterHarvestBatches,
  formatHarvestDate,
  formatHarvestQuantity,
  formatPercentage,
  harvestDraftFrom,
  harvestStatusLabels,
  harvestUnitLabels,
  harvestUnitSymbols,
  optionalHarvestText,
  parseHarvestNumber,
  requiredHarvestUnit,
  summarizeHarvest,
  validateHarvestDraft,
  type HarvestDraft,
  type HarvestStatusFilter,
  type HarvestUnitFilter,
} from "@/lib/harvest/harvest-management";
import styles from "./harvest-management.module.css";

type Props = {
  organization: Organization | null;
  organizationId: string | null;
  permissions: string[];
};

type EditorState = { harvestBatchId: string | null };
type ActionState = {
  kind: "confirm" | "cancel";
  harvestBatchId: string;
};

type IconName =
  | "add" | "arrow" | "calendar" | "check" | "close" | "edit"
  | "harvest" | "location" | "quality" | "refresh" | "scale"
  | "search" | "stock" | "stop" | "trend";

const iconPaths: Record<IconName, string> = {
  add: "M12 5v14M5 12h14",
  arrow: "m15 18-6-6 6-6",
  calendar: "M6 3v3m12-3v3M4 9h16M5 5h14a1 1 0 0 1 1 1v14H4V6a1 1 0 0 1 1-1Z",
  check: "m5 12 4 4L19 6",
  close: "m6 6 12 12M18 6 6 18",
  edit: "m4 20 4.5-1 10-10a2.1 2.1 0 0 0-3-3l-10 10L4 20Zm10-12 3 3",
  harvest: "M5 20h14M7 20V9m4 11V5m4 15V8m4 12V4M5 9c2 0 4 1 6 3m0-7c2 0 3 1 4 3m0 0c2-1 3-2 4-4",
  location: "M12 21s7-6 7-12a7 7 0 1 0-14 0c0 6 7 12 7 12Zm0-9a3 3 0 1 0 0-6 3 3 0 0 0 0 6Z",
  quality: "m12 3 2.4 4.9 5.4.8-3.9 3.8.9 5.4-4.8-2.6-4.8 2.6.9-5.4-3.9-3.8 5.4-.8L12 3Z",
  refresh: "M20 6v5h-5M4 18v-5h5m10-2a7 7 0 0 0-12-4L4 11m16 2-3 4a7 7 0 0 1-12-4",
  scale: "M12 3v18M5 7h14M5 7l-3 6h6L5 7Zm14 0-3 6h6l-3-6ZM8 21h8",
  search: "m21 21-4.35-4.35m2.35-5.65a8 8 0 1 1-16 0 8 8 0 0 1 16 0Z",
  stock: "M4 7 12 3l8 4-8 4-8-4Zm0 5 8 4 8-4m-16 5 8 4 8-4",
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

function replaceBatch(batches: HarvestBatch[], updated: HarvestBatch): HarvestBatch[] {
  return batches.some((batch) => batch.id === updated.id)
    ? batches.map((batch) => batch.id === updated.id ? updated : batch)
    : [...batches, updated];
}

function cycleStatusLabel(status: CropCycle["status"]): string {
  return ({ 1: "Rencana", 2: "Berjalan", 3: "Selesai", 4: "Dibatalkan" })[status];
}

function friendlyError(error: unknown): string {
  if (!(error instanceof ApiError)) {
    return error instanceof Error ? error.message : "Permintaan tidak dapat diselesaikan.";
  }

  switch (error.problem?.code) {
    case "HarvestBatches.CodeAlreadyExists":
      return "Kode batch panen sudah digunakan pada siklus ini.";
    case "HarvestBatches.CropCycleNotInProgress":
      return "Batch panen hanya dapat dibuat, diubah, atau dikonfirmasi saat siklus masih berjalan.";
    case "HarvestBatches.InvalidHarvestDate":
      return "Tanggal panen berada di luar periode aktual siklus.";
    case "HarvestBatches.QuantityUnitConflict":
      return "Gunakan satuan yang sama untuk seluruh batch aktif dalam siklus ini.";
    case "HarvestBatches.InvalidStatusTransition":
      return "Tindakan tidak sesuai dengan status batch saat ini. Muat ulang lalu periksa kembali.";
    case "HarvestBatches.ActiveConfirmedSaleExists":
    case "HarvestBatches.ConfirmedSaleReferenceExists":
      return "Batch tidak dapat dibatalkan karena sudah digunakan pada penjualan terkonfirmasi.";
    default:
      return error.message;
  }
}

function HarvestEditor({
  batch,
  cycle,
  requiredUnit,
  isSaving,
  apiError,
  onCancel,
  onSubmit,
}: {
  batch: HarvestBatch | null;
  cycle: CropCycle;
  requiredUnit: HarvestQuantityUnit | null;
  isSaving: boolean;
  apiError: string | null;
  onCancel: () => void;
  onSubmit: (request: CreateHarvestBatchRequest) => Promise<void>;
}) {
  const [draft, setDraft] = useState<HarvestDraft>(() => {
    const value = harvestDraftFrom(batch, requiredUnit ?? 1);
    return batch ? value : { ...value, harvestDate: localToday() };
  });
  const [errors, setErrors] = useState<string[]>([]);
  const isCreate = batch === null;
  const gross = parseHarvestNumber(draft.grossQuantity) ?? 0;
  const rejected = parseHarvestNumber(draft.rejectedQuantity, true) ?? 0;
  const net = Math.max(0, gross - rejected);
  const rejectionRate = gross > 0 ? (rejected / gross) * 100 : 0;

  function updateDraft<Key extends keyof HarvestDraft>(
    key: Key,
    value: HarvestDraft[Key],
  ) {
    setDraft((current) => ({ ...current, [key]: value }));
  }

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const nextErrors = validateHarvestDraft(draft, isCreate, cycle);
    setErrors(nextErrors);
    const grossQuantity = parseHarvestNumber(draft.grossQuantity);
    const rejectedQuantity = parseHarvestNumber(draft.rejectedQuantity, true);
    if (nextErrors.length > 0 || grossQuantity === null || rejectedQuantity === null) return;

    void onSubmit({
      code: draft.code.trim().toUpperCase(),
      harvestDate: draft.harvestDate,
      grossQuantity,
      rejectedQuantity,
      quantityUnit: draft.quantityUnit,
      qualityGrade: optionalHarvestText(draft.qualityGrade),
      storageLocation: optionalHarvestText(draft.storageLocation),
      notes: optionalHarvestText(draft.notes),
    });
  }

  return (
    <form className={styles.editorForm} onSubmit={submit} noValidate>
      <header className={styles.editorHeader}>
        <span className={styles.editorIcon}><Icon name="harvest" /></span>
        <div>
          <span className={styles.eyebrow}>{isCreate ? "Batch baru" : batch.code}</span>
          <h2>{isCreate ? "Catat hasil panen" : "Perbarui draf panen"}</h2>
          <p>Hasil bersih dihitung otomatis dari hasil kotor dikurangi hasil yang ditolak.</p>
        </div>
        <button className={styles.iconButton} type="button" aria-label="Tutup formulir" disabled={isSaving} onClick={onCancel}><Icon name="close" /></button>
      </header>

      <div className={styles.contextBox}>
        <span>Siklus budidaya</span>
        <strong>{cycle.code} · {cycle.name}</strong>
        <small>{cycleStatusLabel(cycle.status)}</small>
      </div>

      {(errors.length > 0 || apiError) && (
        <div className={styles.formAlert} role="alert">
          <strong>Periksa kembali data berikut:</strong>
          <ul>{errors.map((error) => <li key={error}>{error}</li>)}{apiError && <li>{apiError}</li>}</ul>
        </div>
      )}

      <fieldset disabled={isSaving}>
        <div className={styles.formGrid}>
          <label className={styles.field}>
            <span>Kode batch <em>*</em></span>
            <input value={draft.code} maxLength={40} disabled={!isCreate} placeholder="Contoh: PNN-001" onChange={(event) => updateDraft("code", event.target.value)} />
          </label>
          <label className={styles.field}>
            <span>Tanggal panen <em>*</em></span>
            <input type="date" value={draft.harvestDate} min={cycle.actualStartDate ?? cycle.plannedStartDate} max={cycle.actualHarvestDate ?? undefined} onChange={(event) => updateDraft("harvestDate", event.target.value)} />
          </label>
          <label className={styles.field}>
            <span>Hasil kotor <em>*</em></span>
            <input value={draft.grossQuantity} inputMode="decimal" placeholder="1250" onChange={(event) => updateDraft("grossQuantity", event.target.value)} />
          </label>
          <label className={styles.field}>
            <span>Hasil ditolak <em>*</em></span>
            <input value={draft.rejectedQuantity} inputMode="decimal" placeholder="0" onChange={(event) => updateDraft("rejectedQuantity", event.target.value)} />
          </label>
          <label className={styles.field}>
            <span>Satuan <em>*</em></span>
            <select value={draft.quantityUnit} disabled={requiredUnit !== null} onChange={(event) => updateDraft("quantityUnit", Number(event.target.value) as HarvestQuantityUnit)}>
              {Object.entries(harvestUnitLabels).map(([value, label]) => <option value={value} key={value}>{label}</option>)}
            </select>
            {requiredUnit !== null && <small className={styles.unitHint}>Mengikuti satuan batch aktif lain pada siklus ini.</small>}
          </label>
          <label className={styles.field}>
            <span>Mutu / grade</span>
            <input value={draft.qualityGrade} maxLength={100} placeholder="Contoh: Grade A" onChange={(event) => updateDraft("qualityGrade", event.target.value)} />
          </label>
          <label className={`${styles.field} ${styles.fieldFull}`}>
            <span>Lokasi penyimpanan</span>
            <input value={draft.storageLocation} maxLength={250} placeholder="Gudang, rumah sortasi, atau lokasi pengiriman" onChange={(event) => updateDraft("storageLocation", event.target.value)} />
          </label>
          <label className={`${styles.field} ${styles.fieldFull}`}>
            <span>Catatan panen</span>
            <textarea value={draft.notes} maxLength={1000} rows={4} placeholder="Kondisi hasil, metode sortasi, cuaca, atau temuan lapangan" onChange={(event) => updateDraft("notes", event.target.value)} />
          </label>
        </div>
      </fieldset>

      <div className={styles.yieldPreview}>
        <span><small>Hasil bersih</small><strong>{formatHarvestQuantity(net, draft.quantityUnit)}</strong></span>
        <span><small>Persentase ditolak</small><strong>{formatPercentage(rejectionRate)}</strong></span>
        <i><Icon name="scale" /></i>
      </div>

      <footer className={styles.formActions}>
        <button className={styles.secondaryButton} type="button" disabled={isSaving} onClick={onCancel}>Batal</button>
        <button className={styles.primaryButton} type="submit" disabled={isSaving}>{isSaving ? "Menyimpan..." : isCreate ? "Simpan draf" : "Simpan perubahan"}</button>
      </footer>
    </form>
  );
}

function HarvestAction({
  kind,
  batch,
  isSaving,
  apiError,
  onCancel,
  onSubmit,
}: {
  kind: ActionState["kind"];
  batch: HarvestBatch;
  isSaving: boolean;
  apiError: string | null;
  onCancel: () => void;
  onSubmit: (value: string) => Promise<void>;
}) {
  const [reason, setReason] = useState("");
  const [validationError, setValidationError] = useState<string | null>(null);
  const isConfirm = kind === "confirm";

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const normalized = reason.trim();
    if (!isConfirm && !normalized) {
      setValidationError("Alasan pembatalan wajib diisi.");
      return;
    }
    if (!isConfirm && normalized.length > 500) {
      setValidationError("Alasan pembatalan maksimal 500 karakter.");
      return;
    }
    void onSubmit(normalized);
  }

  return (
    <form className={styles.actionDialog} onSubmit={submit} noValidate>
      <div className={`${styles.actionIcon} ${isConfirm ? "" : styles.actionIconDanger}`}><Icon name={isConfirm ? "check" : "stop"} /></div>
      <span className={styles.eyebrow}>{batch.code}</span>
      <h2>{isConfirm ? "Konfirmasi hasil panen?" : "Batalkan batch panen?"}</h2>
      <p>{isConfirm
        ? "Setelah dikonfirmasi, jumlah ini menjadi stok yang dapat digunakan pada transaksi penjualan dan draf tidak dapat diubah lagi."
        : "Batch yang dibatalkan tetap tersimpan sebagai jejak evaluasi dan tidak dapat dijual."}</p>
      <div className={styles.actionSummary}>
        <span><small>Hasil bersih</small><strong>{formatHarvestQuantity(batch.netQuantity, batch.quantityUnit)}</strong></span>
        <span><small>Mutu</small><strong>{batch.qualityGrade ?? "Belum dinilai"}</strong></span>
      </div>
      {!isConfirm && (
        <label className={styles.field}>
          <span>Alasan pembatalan <em>*</em></span>
          <textarea value={reason} maxLength={500} rows={4} autoFocus placeholder="Jelaskan mengapa catatan panen dibatalkan" disabled={isSaving} onChange={(event) => setReason(event.target.value)} />
        </label>
      )}
      {(validationError || apiError) && <div className={styles.formAlert} role="alert"><ul>{validationError && <li>{validationError}</li>}{apiError && <li>{apiError}</li>}</ul></div>}
      <div className={styles.actionButtons}>
        <button className={styles.secondaryButton} type="button" disabled={isSaving} onClick={onCancel}>Kembali</button>
        <button className={isConfirm ? styles.primaryButton : styles.dangerButton} type="submit" disabled={isSaving}>{isSaving ? "Memproses..." : isConfirm ? "Konfirmasi panen" : "Batalkan batch"}</button>
      </div>
    </form>
  );
}

export function HarvestManagement({ organization, organizationId, permissions }: Props) {
  const router = useRouter();
  const [cycles, setCycles] = useState<CropCycle[]>([]);
  const [batches, setBatches] = useState<HarvestBatch[]>([]);
  const [selectedCycleId, setSelectedCycleId] = useState("");
  const [selectedBatchId, setSelectedBatchId] = useState("");
  const [query, setQuery] = useState("");
  const [statusFilter, setStatusFilter] = useState<HarvestStatusFilter>("all");
  const [unitFilter, setUnitFilter] = useState<HarvestUnitFilter>("all");
  const [editor, setEditor] = useState<EditorState | null>(null);
  const [action, setAction] = useState<ActionState | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isLoadingBatches, setIsLoadingBatches] = useState(false);
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [pageError, setPageError] = useState<string | null>(null);
  const [modalError, setModalError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);

  const canRead = permissions.includes("harvest.read");
  const canWrite = permissions.includes("harvest.write");
  const selectedCycle = cycles.find((cycle) => cycle.id === selectedCycleId) ?? null;
  const filteredBatches = useMemo(
    () => filterHarvestBatches(batches, query, statusFilter, unitFilter),
    [batches, query, statusFilter, unitFilter],
  );
  const selectedBatch = filteredBatches.find((batch) => batch.id === selectedBatchId)
    ?? filteredBatches[0]
    ?? null;
  const summary = useMemo(
    () => summarizeHarvest(unitFilter === "all"
      ? batches
      : batches.filter((batch) => batch.quantityUnit === unitFilter)),
    [batches, unitFilter],
  );
  const editorBatch = editor?.harvestBatchId
    ? batches.find((batch) => batch.id === editor.harvestBatchId) ?? null
    : null;
  const cycleUnit = requiredHarvestUnit(batches);
  const editorRequiredUnit = requiredHarvestUnit(
    batches,
    editorBatch?.id ?? null,
  );
  const actionBatch = action
    ? batches.find((batch) => batch.id === action.harvestBatchId) ?? null
    : null;

  useEffect(() => {
    let cancelled = false;
    async function loadCycles() {
      if (!organizationId || !canRead) {
        setIsLoading(false);
        return;
      }
      setIsLoading(true);
      setPageError(null);
      try {
        const result = await getCropCycles(organizationId);
        if (cancelled) return;
        setCycles(result);
        setSelectedCycleId((current) => {
          if (result.some((cycle) => cycle.id === current)) return current;
          return result.find((cycle) => cycle.status === 2)?.id
            ?? result.find((cycle) => cycle.status === 3)?.id
            ?? result[0]?.id
            ?? "";
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
    void loadCycles();
    return () => { cancelled = true; };
  }, [canRead, organizationId, router]);

  useEffect(() => {
    let cancelled = false;
    async function loadBatches() {
      if (!organizationId || !selectedCycleId || !canRead) {
        setBatches([]);
        return;
      }
      setIsLoadingBatches(true);
      setPageError(null);
      try {
        const result = await getHarvestBatches(organizationId, selectedCycleId);
        if (cancelled) return;
        setBatches(result);
        setSelectedBatchId((current) => result.some((batch) => batch.id === current)
          ? current
          : result[0]?.id ?? "");
      } catch (error) {
        if (error instanceof ApiError && error.status === 401) {
          router.replace("/login");
          return;
        }
        if (!cancelled) setPageError(friendlyError(error));
      } finally {
        if (!cancelled) setIsLoadingBatches(false);
      }
    }
    void loadBatches();
    return () => { cancelled = true; };
  }, [canRead, organizationId, router, selectedCycleId]);

  useEffect(() => {
    if (!editor && !action) return;
    const originalOverflow = document.body.style.overflow;
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
      document.body.style.overflow = originalOverflow;
      window.removeEventListener("keydown", close);
    };
  }, [action, editor, isSaving]);

  function applyUpdatedBatch(updated: HarvestBatch, message: string) {
    setBatches((current) => replaceBatch(current, updated));
    setSelectedBatchId(updated.id);
    setNotice(message);
    setPageError(null);
  }

  async function refresh() {
    if (!organizationId || !selectedCycleId) return;
    setIsRefreshing(true);
    setPageError(null);
    try {
      const [nextCycles, nextBatches] = await Promise.all([
        getCropCycles(organizationId),
        getHarvestBatches(organizationId, selectedCycleId),
      ]);
      setCycles(nextCycles);
      setBatches(nextBatches);
      setSelectedBatchId((current) => nextBatches.some((batch) => batch.id === current)
        ? current
        : nextBatches[0]?.id ?? "");
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

  async function submitBatch(request: CreateHarvestBatchRequest) {
    if (!organizationId || !selectedCycle || !canWrite) return;
    setIsSaving(true);
    setModalError(null);
    try {
      const updated = editor?.harvestBatchId
        ? await updateHarvestBatch(
          organizationId,
          selectedCycle.id,
          editor.harvestBatchId,
          {
            harvestDate: request.harvestDate,
            grossQuantity: request.grossQuantity,
            rejectedQuantity: request.rejectedQuantity,
            quantityUnit: request.quantityUnit,
            qualityGrade: request.qualityGrade,
            storageLocation: request.storageLocation,
            notes: request.notes,
          },
        )
        : await createHarvestBatch(organizationId, selectedCycle.id, request);
      applyUpdatedBatch(updated, editor?.harvestBatchId
        ? "Draf panen berhasil diperbarui."
        : "Batch panen baru berhasil disimpan sebagai draf.");
      setEditor(null);
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

  async function submitAction(value: string) {
    if (!organizationId || !selectedCycle || !action || !canWrite) return;
    setIsSaving(true);
    setModalError(null);
    try {
      const updated = action.kind === "confirm"
        ? await confirmHarvestBatch(organizationId, selectedCycle.id, action.harvestBatchId)
        : await cancelHarvestBatch(
          organizationId,
          selectedCycle.id,
          action.harvestBatchId,
          { cancellationReason: value },
        );
      applyUpdatedBatch(updated, action.kind === "confirm"
        ? "Hasil panen dikonfirmasi dan siap digunakan pada penjualan."
        : "Batch panen dibatalkan dan tetap tersimpan untuk evaluasi.");
      setAction(null);
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

  function changeCycle(cycleId: string) {
    setSelectedCycleId(cycleId);
    setSelectedBatchId("");
    setQuery("");
    setStatusFilter("all");
    setUnitFilter("all");
    setNotice(null);
  }

  if (!organizationId) {
    return <section className={styles.accessState}><Icon name="harvest" /><h1>Pilih organisasi terlebih dahulu</h1><p>Catatan panen selalu terikat pada satu organisasi aktif.</p></section>;
  }

  if (!canRead) {
    return <section className={styles.accessState}><Icon name="stop" /><h1>Akses panen tidak tersedia</h1><p>Peran Anda belum memiliki izin <strong>harvest.read</strong>.</p></section>;
  }

  const showQuantity = (value: number) => summary.unit
    ? formatHarvestQuantity(value, summary.unit)
    : summary.hasMixedUnits ? "Beragam" : "0";

  return (
    <section className={styles.harvestPage}>
      <div className={styles.hero}>
        <div>
          <button className={styles.backButton} type="button" onClick={() => router.push("/cultivation")}><Icon name="arrow" /> Siklus budidaya</button>
          <span className={styles.eyebrow}>Hasil produksi</span>
          <h1>Panen</h1>
          <p>Catat hasil, mutu, dan stok tersedia {organization?.name ? `untuk ${organization.name}` : "organisasi aktif"} sebagai penghubung antara pekerjaan lapangan dan penjualan.</p>
        </div>
        <div className={styles.heroActions}>
          {!canWrite && <span className={styles.readOnlyBadge}>Mode baca</span>}
          <button className={styles.secondaryButton} type="button" disabled={isRefreshing || isLoading || !selectedCycle} onClick={() => void refresh()}><Icon name="refresh" /> {isRefreshing ? "Memuat..." : "Muat ulang"}</button>
          {canWrite && selectedCycle?.status === 2 && <button className={styles.primaryButton} type="button" onClick={() => { setModalError(null); setEditor({ harvestBatchId: null }); }}><Icon name="add" /> Catat panen</button>}
        </div>
      </div>

      {notice && <div className={styles.notice} role="status"><span><Icon name="check" /></span><strong>{notice}</strong><button type="button" aria-label="Tutup pemberitahuan" onClick={() => setNotice(null)}><Icon name="close" /></button></div>}
      {pageError && <div className={styles.pageError} role="alert">{pageError}</div>}

      <div className={styles.cycleBar}>
        <label>
          <span>Siklus budidaya</span>
          <select value={selectedCycleId} disabled={isLoading || cycles.length === 0} onChange={(event) => changeCycle(event.target.value)}>
            {cycles.length === 0 && <option value="">Belum ada siklus</option>}
            {cycles.map((cycle) => <option value={cycle.id} key={cycle.id}>{cycle.code} · {cycle.name} · {cycleStatusLabel(cycle.status)}</option>)}
          </select>
        </label>
        {selectedCycle && <div className={styles.cycleSummary}><span className={`${styles.cycleBadge} ${styles[`cycle${selectedCycle.status}`]}`}>{cycleStatusLabel(selectedCycle.status)}</span>{cycleUnit !== null && <small>Satuan panen: {harvestUnitLabels[cycleUnit]}</small>}<small>{selectedCycle.actualStartDate ? `Mulai ${formatHarvestDate(selectedCycle.actualStartDate)}` : `Rencana mulai ${formatHarvestDate(selectedCycle.plannedStartDate)}`}</small></div>}
      </div>

      <div className={styles.metricGrid}>
        <article className={styles.metricCard}><span>Total batch</span><strong>{summary.batchCount}</strong><small>{summary.confirmedCount} sudah dikonfirmasi</small><i><Icon name="harvest" /></i></article>
        <article className={`${styles.metricCard} ${styles.metricPrimary}`}><span>Hasil bersih</span><strong>{showQuantity(summary.netQuantity)}</strong><small>{summary.hasMixedUnits ? "Gunakan filter satuan untuk melihat total" : "Panen terkonfirmasi"}</small><i><Icon name="trend" /></i></article>
        <article className={styles.metricCard}><span>Stok tersedia</span><strong>{showQuantity(summary.availableQuantity)}</strong><small>Siap dialokasikan ke penjualan</small><i><Icon name="stock" /></i></article>
        <article className={styles.metricCard}><span>Hasil ditolak</span><strong>{showQuantity(summary.rejectedQuantity)}</strong><small>Akumulasi batch aktif</small><i><Icon name="scale" /></i></article>
      </div>

      <div className={styles.toolbar}>
        <label className={styles.searchField}><Icon name="search" /><input value={query} placeholder="Cari kode, mutu, lokasi, atau catatan" aria-label="Cari batch panen" onChange={(event) => setQuery(event.target.value)} /></label>
        <label className={styles.filterField}><span>Status</span><select value={statusFilter} onChange={(event) => setStatusFilter(event.target.value === "all" ? "all" : Number(event.target.value) as HarvestBatchStatus)}><option value="all">Semua status</option>{Object.entries(harvestStatusLabels).map(([value, label]) => <option value={value} key={value}>{label}</option>)}</select></label>
        <label className={styles.filterField}><span>Satuan</span><select value={unitFilter} onChange={(event) => setUnitFilter(event.target.value === "all" ? "all" : Number(event.target.value) as HarvestQuantityUnit)}><option value="all">Semua satuan</option>{Object.entries(harvestUnitLabels).map(([value, label]) => <option value={value} key={value}>{label}</option>)}</select></label>
        <span className={styles.resultCount}>{filteredBatches.length} hasil</span>
      </div>

      {isLoading || isLoadingBatches ? (
        <div className={styles.loadingState}><span className="loader" /><p>Memuat catatan panen...</p></div>
      ) : cycles.length === 0 ? (
        <div className={styles.emptyState}><span><Icon name="calendar" /></span><h2>Belum ada siklus budidaya</h2><p>Buka dan mulai siklus budidaya sebelum mencatat hasil panen.</p><button className={styles.secondaryButton} type="button" onClick={() => router.push("/cultivation")}>Buka Siklus Budidaya</button></div>
      ) : batches.length === 0 ? (
        <div className={styles.emptyState}><span><Icon name="harvest" /></span><h2>Belum ada batch panen</h2><p>{selectedCycle?.status === 2 ? "Catat hasil panen pertama untuk siklus yang sedang berjalan." : "Siklus ini tidak memiliki catatan panen dan tidak dapat menerima batch baru pada status saat ini."}</p>{canWrite && selectedCycle?.status === 2 && <button className={styles.primaryButton} type="button" onClick={() => setEditor({ harvestBatchId: null })}><Icon name="add" /> Catat panen pertama</button>}</div>
      ) : filteredBatches.length === 0 ? (
        <div className={styles.emptyState}><span><Icon name="search" /></span><h2>Tidak ada hasil yang sesuai</h2><p>Ubah kata pencarian atau filter untuk melihat batch lainnya.</p><button className={styles.secondaryButton} type="button" onClick={() => { setQuery(""); setStatusFilter("all"); setUnitFilter("all"); }}>Bersihkan filter</button></div>
      ) : (
        <div className={styles.managementGrid}>
          <aside className={styles.batchList}>
            <header><div><span className={styles.eyebrow}>Daftar hasil</span><h2>{filteredBatches.length} batch</h2></div></header>
            <div className={styles.batchCards}>
              {filteredBatches.map((batch) => <button className={`${styles.batchCard} ${batch.id === selectedBatch?.id ? styles.batchCardSelected : ""}`} type="button" aria-pressed={batch.id === selectedBatch?.id} key={batch.id} onClick={() => setSelectedBatchId(batch.id)}><span className={styles.cardTopline}><strong>{batch.code}</strong><i className={`${styles.statusBadge} ${styles[`status${batch.status}`]}`}>{harvestStatusLabels[batch.status]}</i></span><b>{formatHarvestQuantity(batch.netQuantity, batch.quantityUnit)}</b><span className={styles.cardMeta}><small>{formatHarvestDate(batch.harvestDate)}</small><small>{batch.qualityGrade ?? "Mutu belum dicatat"}</small></span><span className={styles.stockLine}><Icon name="stock" /> {batch.status === 2 ? `${formatHarvestQuantity(batch.availableQuantity, batch.quantityUnit)} tersedia` : batch.status === 1 ? "Menunggu konfirmasi" : "Tidak masuk stok"}</span></button>)}
            </div>
          </aside>

          {selectedBatch && (
            <article className={styles.batchDetail}>
              <header className={styles.detailHeader}>
                <div className={styles.detailIdentity}><span className={styles.detailIcon}><Icon name="harvest" /></span><div><span>{selectedBatch.code}</span><h2>{formatHarvestQuantity(selectedBatch.netQuantity, selectedBatch.quantityUnit)}</h2><p>Dipanen {formatHarvestDate(selectedBatch.harvestDate)}</p></div></div>
                <div className={styles.detailActions}>
                  <span className={`${styles.statusBadge} ${styles[`status${selectedBatch.status}`]}`}>{harvestStatusLabels[selectedBatch.status]}</span>
                  {canWrite && selectedBatch.status === 1 && selectedCycle?.status === 2 && <><button className={styles.secondaryButton} type="button" onClick={() => { setModalError(null); setEditor({ harvestBatchId: selectedBatch.id }); }}><Icon name="edit" /> Ubah</button><button className={styles.primaryButton} type="button" onClick={() => { setModalError(null); setAction({ kind: "confirm", harvestBatchId: selectedBatch.id }); }}><Icon name="check" /> Konfirmasi</button></>}
                </div>
              </header>

              <div className={styles.quantityGrid}>
                <div><span>Hasil kotor</span><strong>{formatHarvestQuantity(selectedBatch.grossQuantity, selectedBatch.quantityUnit)}</strong></div>
                <div><span>Hasil ditolak</span><strong>{formatHarvestQuantity(selectedBatch.rejectedQuantity, selectedBatch.quantityUnit)}</strong><small>{formatPercentage(selectedBatch.grossQuantity > 0 ? selectedBatch.rejectedQuantity / selectedBatch.grossQuantity * 100 : 0)}</small></div>
                <div className={styles.quantityNet}><span>Hasil bersih</span><strong>{formatHarvestQuantity(selectedBatch.netQuantity, selectedBatch.quantityUnit)}</strong></div>
                <div className={styles.quantityAvailable}><span>Stok tersedia</span><strong>{formatHarvestQuantity(selectedBatch.availableQuantity, selectedBatch.quantityUnit)}</strong><small>{formatHarvestQuantity(selectedBatch.confirmedSoldQuantity, selectedBatch.quantityUnit)} sudah terjual</small></div>
              </div>

              <div className={styles.infoGrid}>
                <section><i><Icon name="quality" /></i><span><small>Mutu / grade</small><strong>{selectedBatch.qualityGrade ?? "Belum dicatat"}</strong></span></section>
                <section><i><Icon name="location" /></i><span><small>Lokasi penyimpanan</small><strong>{selectedBatch.storageLocation ?? "Belum dicatat"}</strong></span></section>
                <section><i><Icon name="calendar" /></i><span><small>Waktu konfirmasi</small><strong>{selectedBatch.confirmedAt ? new Date(selectedBatch.confirmedAt).toLocaleString("id-ID", { dateStyle: "medium", timeStyle: "short" }) : "Belum dikonfirmasi"}</strong></span></section>
                <section><i><Icon name="scale" /></i><span><small>Satuan pencatatan</small><strong>{harvestUnitLabels[selectedBatch.quantityUnit]} ({harvestUnitSymbols[selectedBatch.quantityUnit]})</strong></span></section>
              </div>

              <section className={styles.notesPanel}><span className={styles.eyebrow}>Catatan panen</span><p>{selectedBatch.notes ?? "Belum ada catatan tambahan untuk batch ini."}</p></section>
              {selectedBatch.cancellationReason && <section className={styles.cancellationPanel}><strong>Alasan pembatalan</strong><p>{selectedBatch.cancellationReason}</p></section>}

              {canWrite && selectedBatch.status !== 3 && (
                <footer className={styles.detailFooter}>
                  <span>{selectedBatch.confirmedSoldQuantity > 0 ? "Batch sudah terikat pada penjualan terkonfirmasi dan tidak dapat dibatalkan." : "Pembatalan disimpan sebagai jejak evaluasi."}</span>
                  {selectedBatch.confirmedSoldQuantity === 0 && <button className={styles.dangerTextButton} type="button" onClick={() => { setModalError(null); setAction({ kind: "cancel", harvestBatchId: selectedBatch.id }); }}><Icon name="stop" /> Batalkan batch</button>}
                </footer>
              )}
            </article>
          )}
        </div>
      )}

      {editor && selectedCycle && (
        <div className={styles.modalBackdrop} role="presentation" onMouseDown={(event) => { if (event.target === event.currentTarget && !isSaving) setEditor(null); }}>
          <div className={styles.modalPanel} role="dialog" aria-modal="true" aria-label={editorBatch ? "Ubah draf panen" : "Catat panen"}>
            <HarvestEditor key={editor.harvestBatchId ?? "create"} batch={editorBatch} cycle={selectedCycle} requiredUnit={editorRequiredUnit} isSaving={isSaving} apiError={modalError} onCancel={() => { setEditor(null); setModalError(null); }} onSubmit={submitBatch} />
          </div>
        </div>
      )}

      {action && actionBatch && (
        <div className={styles.modalBackdrop} role="presentation" onMouseDown={(event) => { if (event.target === event.currentTarget && !isSaving) setAction(null); }}>
          <div className={styles.actionPanel} role="dialog" aria-modal="true" aria-label={action.kind === "confirm" ? "Konfirmasi panen" : "Batalkan panen"}>
            <HarvestAction key={`${action.kind}-${action.harvestBatchId}`} kind={action.kind} batch={actionBatch} isSaving={isSaving} apiError={modalError} onCancel={() => { setAction(null); setModalError(null); }} onSubmit={submitAction} />
          </div>
        </div>
      )}
    </section>
  );
}
