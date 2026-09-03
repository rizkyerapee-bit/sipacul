"use client";

import {
  useCallback,
  useEffect,
  useMemo,
  useState,
  type FormEvent,
} from "react";
import { useRouter } from "next/navigation";
import {
  ApiError,
  addCultivationSopStep,
  createCultivationSop,
  getCommodities,
  getCultivationSops,
  moveCultivationSopStep,
  removeCultivationSopStep,
  setCultivationSopActive,
  updateCultivationSop,
  updateCultivationSopStep,
} from "@/lib/api/client";
import type {
  Commodity,
  CultivationSop,
  CultivationSopStep,
  Organization,
} from "@/lib/api/contracts";
import {
  cultivationSopDraftFrom,
  cultivationSopStepDraftFrom,
  filterCultivationSops,
  getCultivationSopStatusLabel,
  getCultivationSopStepMoveSequence,
  sortCultivationSopSteps,
  toCreateCultivationSopRequest,
  toCultivationSopStepRequest,
  toUpdateCultivationSopRequest,
  type CultivationSopDraft,
  type CultivationSopStatusFilter,
  type CultivationSopStepDraft,
  validateCultivationSopDraft,
  validateCultivationSopStepDraft,
} from "@/lib/master-data/cultivation-sop-management";
import {
  hasFormDraftChanged,
  resolveFormCloseDecision,
  type FormCloseSource,
} from "@/lib/ui/form-data-loss";
import styles from "./cultivation-sop-management.module.css";

type CultivationSopManagementProps = {
  organization: Organization | null;
  organizationId: string | null;
  permissions: string[];
};

type EditorState =
  | { kind: "sop"; sopId: string | null }
  | { kind: "step"; sopId: string; stepId: string | null };

type ConfirmationState =
  | { kind: "toggle"; sopId: string; nextActive: boolean }
  | { kind: "remove-step"; sopId: string; stepId: string };

type IconName =
  | "add"
  | "check"
  | "clock"
  | "close"
  | "down"
  | "edit"
  | "leaf"
  | "refresh"
  | "required"
  | "search"
  | "steps"
  | "stop"
  | "trash"
  | "up";

const iconPaths: Record<IconName, string> = {
  add: "M12 5v14M5 12h14",
  check: "m5 12 4 4L19 6",
  clock: "M12 22a10 10 0 1 0 0-20 10 10 0 0 0 0 20Zm0-15v5l3 2",
  close: "m6 6 12 12M18 6 6 18",
  down: "m7 10 5 5 5-5",
  edit: "m4 20 4.5-1 10-10a2.1 2.1 0 0 0-3-3l-10 10L4 20Zm10-12 3 3",
  leaf: "M12 21v-9m0 2c-4 0-7-2-7-6 4 0 7 2 7 6Zm0-3c4 0 7-2 7-6-4 0-7 2-7 6Z",
  refresh: "M20 6v5h-5M4 18v-5h5m10-2a7 7 0 0 0-12-4L4 11m16 2-3 4a7 7 0 0 1-12-4",
  required: "m5 12 4 4L19 6",
  search: "m21 21-4.35-4.35m2.35-5.65a8 8 0 1 1-16 0 8 8 0 0 1 16 0Z",
  steps: "M6 5h14M6 12h14M6 19h14M3 5h.01M3 12h.01M3 19h.01",
  stop: "M6 6h12v12H6V6Z",
  trash: "M4 7h16m-10 4v6m4-6v6M9 7V4h6v3m-9 0 1 14h10l1-14",
  up: "m7 14 5-5 5 5",
};

function Icon({ name }: { name: IconName }) {
  return (
    <svg viewBox="0 0 24 24" aria-hidden="true">
      <path d={iconPaths[name]} />
    </svg>
  );
}

function friendlyError(error: unknown): string {
  if (!(error instanceof ApiError)) {
    return error instanceof Error
      ? error.message
      : "Permintaan tidak dapat diselesaikan.";
  }

  switch (error.problem?.code) {
    case "CultivationSops.NameAlreadyExists":
      return "Nama SOP sudah digunakan untuk komoditas ini.";
    case "CultivationSops.CommodityNotFound":
      return "Komoditas tidak tersedia. Muat ulang data lalu pilih komoditas lain.";
    case "CultivationSops.NotFound":
    case "CultivationSops.StepNotFound":
      return "SOP atau tahapannya sudah tidak tersedia. Muat ulang halaman.";
    default:
      return error.message;
  }
}

function orderSops(items: CultivationSop[]): CultivationSop[] {
  return [...items].sort((left, right) =>
    left.name.localeCompare(right.name, "id-ID"),
  );
}

function replaceSop(items: CultivationSop[], updated: CultivationSop): CultivationSop[] {
  const next = items.some((item) => item.id === updated.id)
    ? items.map((item) => item.id === updated.id ? updated : item)
    : [...items, updated];
  return orderSops(next);
}

function SopEditor({
  sop,
  commodities,
  isSaving,
  apiError,
  onDirtyChange,
  onCancel,
  onSubmit,
}: {
  sop: CultivationSop | null;
  commodities: Commodity[];
  isSaving: boolean;
  apiError: string | null;
  onDirtyChange: (isDirty: boolean) => void;
  onCancel: () => void;
  onSubmit: (draft: CultivationSopDraft) => Promise<void>;
}) {
  const baselineDraft = useMemo(() => cultivationSopDraftFrom(sop), [sop]);
  const [draft, setDraft] = useState(() => baselineDraft);
  const [errors, setErrors] = useState<string[]>([]);
  const isCreate = sop === null;
  const commodityOptions = commodities.filter(
    (commodity) => commodity.isActive || commodity.id === sop?.commodityId,
  );

  useEffect(() => {
    onDirtyChange(hasFormDraftChanged(baselineDraft, draft));
  }, [baselineDraft, draft, onDirtyChange]);

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const validationErrors = validateCultivationSopDraft(draft);
    setErrors(validationErrors);
    if (validationErrors.length === 0) {
      await onSubmit(draft);
    }
  }

  return (
    <form className={styles.editorForm} onSubmit={(event) => void submit(event)} noValidate>
      <div className={styles.modalHeader}>
        <div className={styles.modalHeading}>
          <span className={styles.modalIcon}><Icon name="leaf" /></span>
          <div>
            <span className={styles.eyebrow}>{isCreate ? "SOP baru" : "Ubah SOP"}</span>
            <h2>{isCreate ? "Tambah SOP budidaya" : sop.name}</h2>
            <p>Atur identitas SOP. Tahapan kerja dikelola dari detail SOP setelah tersimpan.</p>
          </div>
        </div>
        <button className={styles.iconButton} type="button" aria-label="Tutup formulir SOP" disabled={isSaving} onClick={onCancel}>
          <Icon name="close" />
        </button>
      </div>

      {(errors.length > 0 || apiError) && (
        <div className={styles.errorPanel} role="alert">
          <strong>Periksa kembali data berikut:</strong>
          {errors.map((error) => <span key={error}>{error}</span>)}
          {apiError && <span>{apiError}</span>}
        </div>
      )}

      <div className={styles.formGrid}>
        <label className={styles.field}>
          <span>Komoditas <b>*</b></span>
          <select
            value={draft.commodityId}
            disabled={isSaving || !isCreate}
            onChange={(event) => setDraft((current) => ({ ...current, commodityId: event.target.value }))}
          >
            <option value="">Pilih komoditas</option>
            {commodityOptions.map((commodity) => (
              <option value={commodity.id} key={commodity.id}>
                {commodity.name}{commodity.isActive ? "" : " - nonaktif"}
              </option>
            ))}
          </select>
          {!isCreate && <small>Komoditas dikunci setelah SOP dibuat.</small>}
        </label>

        <label className={styles.field}>
          <span>Nama SOP <b>*</b></span>
          <input
            autoFocus
            value={draft.name}
            maxLength={150}
            placeholder="Contoh: SOP Budidaya Cabai Musim Hujan"
            disabled={isSaving}
            onChange={(event) => setDraft((current) => ({ ...current, name: event.target.value }))}
          />
        </label>

        <label className={`${styles.field} ${styles.fieldFull}`}>
          <span>Deskripsi</span>
          <textarea
            value={draft.description}
            maxLength={1000}
            rows={5}
            placeholder="Jelaskan sasaran, kondisi penggunaan, atau catatan penting SOP."
            disabled={isSaving}
            onChange={(event) => setDraft((current) => ({ ...current, description: event.target.value }))}
          />
          <small>{draft.description.trim().length}/1000 karakter</small>
        </label>
      </div>

      <div className={styles.modalFooter}>
        <button className={styles.secondaryButton} type="button" disabled={isSaving} onClick={onCancel}>Batal</button>
        <button className={styles.primaryButton} type="submit" disabled={isSaving}>
          {isSaving ? "Menyimpan..." : isCreate ? "Tambah SOP" : "Simpan perubahan"}
        </button>
      </div>
    </form>
  );
}

function StepEditor({
  step,
  isSaving,
  apiError,
  onDirtyChange,
  onCancel,
  onSubmit,
}: {
  step: CultivationSopStep | null;
  isSaving: boolean;
  apiError: string | null;
  onDirtyChange: (isDirty: boolean) => void;
  onCancel: () => void;
  onSubmit: (draft: CultivationSopStepDraft) => Promise<void>;
}) {
  const baselineDraft = useMemo(() => cultivationSopStepDraftFrom(step), [step]);
  const [draft, setDraft] = useState(() => baselineDraft);
  const [errors, setErrors] = useState<string[]>([]);

  useEffect(() => {
    onDirtyChange(hasFormDraftChanged(baselineDraft, draft));
  }, [baselineDraft, draft, onDirtyChange]);

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const validationErrors = validateCultivationSopStepDraft(draft);
    setErrors(validationErrors);
    if (validationErrors.length === 0) {
      await onSubmit(draft);
    }
  }

  return (
    <form className={styles.editorForm} onSubmit={(event) => void submit(event)} noValidate>
      <div className={styles.modalHeader}>
        <div className={styles.modalHeading}>
          <span className={styles.modalIcon}><Icon name="steps" /></span>
          <div>
            <span className={styles.eyebrow}>{step ? "Ubah tahapan" : "Tahapan baru"}</span>
            <h2>{step ? step.name : "Tambah tahapan SOP"}</h2>
            <p>Waktu dihitung relatif terhadap hari mulai siklus budidaya.</p>
          </div>
        </div>
        <button className={styles.iconButton} type="button" aria-label="Tutup formulir tahapan" disabled={isSaving} onClick={onCancel}>
          <Icon name="close" />
        </button>
      </div>

      {(errors.length > 0 || apiError) && (
        <div className={styles.errorPanel} role="alert">
          <strong>Periksa kembali data berikut:</strong>
          {errors.map((error) => <span key={error}>{error}</span>)}
          {apiError && <span>{apiError}</span>}
        </div>
      )}

      <div className={styles.formGrid}>
        <label className={`${styles.field} ${styles.fieldFull}`}>
          <span>Nama tahapan <b>*</b></span>
          <input
            autoFocus
            value={draft.name}
            maxLength={150}
            placeholder="Contoh: Pemupukan dasar"
            disabled={isSaving}
            onChange={(event) => setDraft((current) => ({ ...current, name: event.target.value }))}
          />
        </label>

        <label className={styles.field}>
          <span>Offset hari rencana <b>*</b></span>
          <input
            type="number"
            min={-365}
            max={3650}
            step={1}
            value={draft.plannedDayOffset}
            disabled={isSaving}
            onChange={(event) => setDraft((current) => ({ ...current, plannedDayOffset: event.target.value }))}
          />
          <small>0 = hari mulai, nilai negatif = sebelum mulai.</small>
        </label>

        <label className={styles.field}>
          <span>Estimasi durasi (hari) <b>*</b></span>
          <input
            type="number"
            min={1}
            max={365}
            step={1}
            value={draft.estimatedDurationDays}
            disabled={isSaving}
            onChange={(event) => setDraft((current) => ({ ...current, estimatedDurationDays: event.target.value }))}
          />
        </label>

        <label className={`${styles.field} ${styles.fieldFull}`}>
          <span>Deskripsi</span>
          <textarea
            value={draft.description}
            maxLength={1000}
            rows={4}
            placeholder="Instruksi, standar hasil, atau catatan pelaksanaan tahapan."
            disabled={isSaving}
            onChange={(event) => setDraft((current) => ({ ...current, description: event.target.value }))}
          />
          <small>{draft.description.trim().length}/1000 karakter</small>
        </label>

        <label className={`${styles.checkboxField} ${styles.fieldFull}`}>
          <input
            type="checkbox"
            checked={draft.isRequired}
            disabled={isSaving}
            onChange={(event) => setDraft((current) => ({ ...current, isRequired: event.target.checked }))}
          />
          <span><strong>Tahapan wajib</strong><small>Aktivitas wajib harus menjadi bagian pelaksanaan SOP.</small></span>
        </label>
      </div>

      <div className={styles.modalFooter}>
        <button className={styles.secondaryButton} type="button" disabled={isSaving} onClick={onCancel}>Batal</button>
        <button className={styles.primaryButton} type="submit" disabled={isSaving}>
          {isSaving ? "Menyimpan..." : step ? "Simpan perubahan" : "Tambah tahapan"}
        </button>
      </div>
    </form>
  );
}

export function CultivationSopManagement({
  organization,
  organizationId,
  permissions,
}: CultivationSopManagementProps) {
  const router = useRouter();
  const [commodities, setCommodities] = useState<Commodity[]>([]);
  const [sops, setSops] = useState<CultivationSop[]>([]);
  const [query, setQuery] = useState("");
  const [commodityFilter, setCommodityFilter] = useState("");
  const [statusFilter, setStatusFilter] = useState<CultivationSopStatusFilter>("all");
  const [selectedSopId, setSelectedSopId] = useState<string | null>(null);
  const [editor, setEditor] = useState<EditorState | null>(null);
  const [confirmation, setConfirmation] = useState<ConfirmationState | null>(null);
  const [editorDirty, setEditorDirty] = useState(false);
  const [discardOpen, setDiscardOpen] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [movingStepId, setMovingStepId] = useState<string | null>(null);
  const [pageError, setPageError] = useState<string | null>(null);
  const [modalError, setModalError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const canRead = permissions.includes("master-data.read");
  const canWrite = permissions.includes("master-data.write");

  const commodityNames = useMemo(
    () => new Map(commodities.map((commodity) => [commodity.id, commodity.name])),
    [commodities],
  );
  const filteredSops = useMemo(
    () => filterCultivationSops(sops, query, commodityFilter, statusFilter),
    [sops, query, commodityFilter, statusFilter],
  );
  const selectedSop = useMemo(
    () => filteredSops.find((sop) => sop.id === selectedSopId) ?? filteredSops[0] ?? null,
    [filteredSops, selectedSopId],
  );
  const orderedSteps = useMemo(
    () => sortCultivationSopSteps(selectedSop?.steps ?? []),
    [selectedSop],
  );

  async function refreshData() {
    if (!organizationId || !canRead) return;
    setIsRefreshing(true);
    setPageError(null);
    try {
      const [nextCommodities, nextSops] = await Promise.all([
        getCommodities(organizationId),
        getCultivationSops(organizationId),
      ]);
      setCommodities(nextCommodities.sort((left, right) => left.name.localeCompare(right.name, "id-ID")));
      setSops(orderSops(nextSops));
      setSelectedSopId((current) => current && nextSops.some((sop) => sop.id === current)
        ? current
        : nextSops[0]?.id ?? null);
    } catch (error) {
      if (error instanceof ApiError && error.status === 401) {
        router.replace("/login");
        return;
      }
      setPageError(friendlyError(error));
    } finally {
      setIsLoading(false);
      setIsRefreshing(false);
    }
  }

  useEffect(() => {
    let cancelled = false;

    async function loadInitialData() {
      if (!organizationId || !canRead) return;

      try {
        const [nextCommodities, nextSops] = await Promise.all([
          getCommodities(organizationId),
          getCultivationSops(organizationId),
        ]);
        if (!cancelled) {
          setCommodities(nextCommodities.sort((left, right) => left.name.localeCompare(right.name, "id-ID")));
          setSops(orderSops(nextSops));
          setSelectedSopId(nextSops[0]?.id ?? null);
        }
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

    void loadInitialData();
    return () => {
      cancelled = true;
    };
  }, [organizationId, canRead, router]);

  useEffect(() => {
    if (!editor && !confirmation && !discardOpen) return;
    const originalOverflow = document.body.style.overflow;
    document.body.style.overflow = "hidden";
    return () => {
      document.body.style.overflow = originalOverflow;
    };
  }, [editor, confirmation, discardOpen]);

  const closeEditor = useCallback(() => {
    setEditor(null);
    setEditorDirty(false);
    setDiscardOpen(false);
    setModalError(null);
  }, []);

  const requestEditorClose = useCallback((source: FormCloseSource) => {
    if (!editor) return;
    const decision = resolveFormCloseDecision({ source, isDirty: editorDirty, isSaving });
    if (decision === "close") closeEditor();
    else if (decision === "confirm-discard") setDiscardOpen(true);
  }, [editor, editorDirty, isSaving, closeEditor]);

  useEffect(() => {
    if (!editor) return;
    function handleEscape(event: KeyboardEvent) {
      if (event.key !== "Escape") return;
      event.preventDefault();
      if (discardOpen) setDiscardOpen(false);
      else requestEditorClose("escape");
    }
    window.addEventListener("keydown", handleEscape);
    return () => window.removeEventListener("keydown", handleEscape);
  }, [editor, discardOpen, requestEditorClose]);

  function openEditor(next: EditorState) {
    setNotice(null);
    setModalError(null);
    setEditorDirty(false);
    setDiscardOpen(false);
    setEditor(next);
  }

  async function submitSop(draft: CultivationSopDraft) {
    if (!organizationId || editor?.kind !== "sop") return;
    setIsSaving(true);
    setModalError(null);
    try {
      const updated = editor.sopId
        ? await updateCultivationSop(organizationId, editor.sopId, toUpdateCultivationSopRequest(draft))
        : await createCultivationSop(organizationId, toCreateCultivationSopRequest(draft));
      setSops((current) => replaceSop(current, updated));
      setSelectedSopId(updated.id);
      setNotice(editor.sopId ? "Perubahan SOP berhasil disimpan." : "SOP baru berhasil ditambahkan.");
      closeEditor();
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

  async function submitStep(draft: CultivationSopStepDraft) {
    if (!organizationId || editor?.kind !== "step") return;
    setIsSaving(true);
    setModalError(null);
    try {
      const request = toCultivationSopStepRequest(draft);
      const updated = editor.stepId
        ? await updateCultivationSopStep(organizationId, editor.sopId, editor.stepId, request)
        : await addCultivationSopStep(organizationId, editor.sopId, request);
      setSops((current) => replaceSop(current, updated));
      setSelectedSopId(updated.id);
      setNotice(editor.stepId ? "Tahapan berhasil diperbarui." : "Tahapan berhasil ditambahkan.");
      closeEditor();
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

  async function runConfirmation() {
    if (!organizationId || !confirmation) return;
    setIsSaving(true);
    setModalError(null);
    try {
      const updated = confirmation.kind === "toggle"
        ? await setCultivationSopActive(organizationId, confirmation.sopId, confirmation.nextActive)
        : await removeCultivationSopStep(organizationId, confirmation.sopId, confirmation.stepId);
      setSops((current) => replaceSop(current, updated));
      setSelectedSopId(updated.id);
      setNotice(confirmation.kind === "toggle"
        ? `SOP berhasil ${confirmation.nextActive ? "diaktifkan" : "dinonaktifkan"}.`
        : "Tahapan berhasil dihapus.");
      setConfirmation(null);
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

  async function moveStep(stepId: string, direction: "up" | "down") {
    if (!organizationId || !selectedSop) return;
    const newSequence = getCultivationSopStepMoveSequence(selectedSop.steps, stepId, direction);
    if (newSequence === null) return;
    setMovingStepId(stepId);
    setPageError(null);
    try {
      const updated = await moveCultivationSopStep(
        organizationId,
        selectedSop.id,
        stepId,
        { newSequence },
      );
      setSops((current) => replaceSop(current, updated));
      setNotice("Urutan tahapan berhasil diperbarui.");
    } catch (error) {
      if (error instanceof ApiError && error.status === 401) {
        router.replace("/login");
        return;
      }
      setPageError(friendlyError(error));
    } finally {
      setMovingStepId(null);
    }
  }

  if (!organizationId || !organization) {
    return (
      <section className={styles.stateCard}>
        <Icon name="leaf" />
        <h1>Pilih organisasi terlebih dahulu</h1>
        <p>SOP budidaya dikelola terpisah untuk setiap organisasi.</p>
      </section>
    );
  }

  if (!canRead) {
    return (
      <section className={styles.stateCard}>
        <Icon name="stop" />
        <h1>Akses SOP budidaya dibatasi</h1>
        <p>Akun ini tidak mempunyai izin <code>master-data.read</code>.</p>
      </section>
    );
  }

  const activeCount = sops.filter((sop) => sop.isActive).length;
  const totalSteps = sops.reduce((total, sop) => total + sop.steps.length, 0);
  const selectedCommodity = selectedSop ? commodityNames.get(selectedSop.commodityId) ?? "Komoditas tidak ditemukan" : "";
  const modalSop = editor?.kind === "sop" && editor.sopId
    ? sops.find((sop) => sop.id === editor.sopId) ?? null
    : null;
  const modalStep = editor?.kind === "step" && editor.stepId
    ? sops.find((sop) => sop.id === editor.sopId)?.steps.find((step) => step.id === editor.stepId) ?? null
    : null;

  return (
    <section className={styles.page}>
      <header className={styles.hero}>
        <div>
          <span className={styles.eyebrow}>Master Data</span>
          <h1>SOP Budidaya</h1>
          <p>Susun standar kerja dan urutan tahapan budidaya untuk {organization.name}.</p>
        </div>
        <div className={styles.heroActions}>
          <button className={styles.secondaryButton} type="button" disabled={isRefreshing} onClick={() => void refreshData()}>
            <Icon name="refresh" />{isRefreshing ? "Memuat..." : "Muat ulang"}
          </button>
          {canWrite && (
            <button className={styles.primaryButton} type="button" disabled={!commodities.some((item) => item.isActive)} onClick={() => openEditor({ kind: "sop", sopId: null })}>
              <Icon name="add" />Tambah SOP
            </button>
          )}
        </div>
      </header>

      {!commodities.some((item) => item.isActive) && canWrite && !isLoading && (
        <div className={styles.infoAlert}>Aktifkan minimal satu komoditas sebelum membuat SOP baru.</div>
      )}
      {notice && <div className={styles.notice} role="status">{notice}</div>}
      {pageError && <div className={styles.errorAlert} role="alert">{pageError}</div>}

      <div className={styles.metrics}>
        <article><span>Total SOP</span><strong>{sops.length}</strong></article>
        <article><span>SOP aktif</span><strong>{activeCount}</strong></article>
        <article><span>Total tahapan</span><strong>{totalSteps}</strong></article>
      </div>

      <div className={styles.filters}>
        <label className={styles.searchField}>
          <Icon name="search" />
          <input value={query} placeholder="Cari nama, deskripsi, atau tahapan..." aria-label="Cari SOP budidaya" onChange={(event) => setQuery(event.target.value)} />
        </label>
        <select value={commodityFilter} aria-label="Filter komoditas" onChange={(event) => setCommodityFilter(event.target.value)}>
          <option value="">Semua komoditas</option>
          {commodities.map((commodity) => <option value={commodity.id} key={commodity.id}>{commodity.name}</option>)}
        </select>
        <select value={statusFilter} aria-label="Filter status SOP" onChange={(event) => setStatusFilter(event.target.value as CultivationSopStatusFilter)}>
          <option value="all">Semua status</option>
          <option value="active">Aktif</option>
          <option value="inactive">Nonaktif</option>
        </select>
      </div>

      {isLoading ? (
        <div className={styles.loading}><span className="loader" /><p>Memuat SOP budidaya...</p></div>
      ) : (
        <div className={styles.workspace}>
          <aside className={styles.catalog} aria-label="Daftar SOP budidaya">
            <div className={styles.panelTitle}>
              <div><span>Daftar SOP</span><small>{filteredSops.length} dari {sops.length} SOP</small></div>
            </div>
            <div className={styles.sopList}>
              {filteredSops.map((sop) => (
                <button
                  className={`${styles.sopCard} ${sop.id === selectedSop?.id ? styles.sopCardActive : ""}`}
                  type="button"
                  key={sop.id}
                  onClick={() => setSelectedSopId(sop.id)}
                >
                  <span className={styles.sopCardTop}>
                    <strong>{sop.name}</strong>
                    <span className={sop.isActive ? styles.statusActive : styles.statusInactive}>{getCultivationSopStatusLabel(sop.isActive)}</span>
                  </span>
                  <span>{commodityNames.get(sop.commodityId) ?? "Komoditas tidak ditemukan"}</span>
                  <small>{sop.steps.length} tahapan</small>
                </button>
              ))}
              {filteredSops.length === 0 && (
                <div className={styles.emptySmall}><Icon name="search" /><strong>Tidak ada SOP ditemukan</strong><span>Ubah kata kunci atau filter yang digunakan.</span></div>
              )}
            </div>
          </aside>

          <main className={styles.detail}>
            {selectedSop ? (
              <>
                <div className={styles.detailHeader}>
                  <div>
                    <div className={styles.detailMeta}>
                      <span className={selectedSop.isActive ? styles.statusActive : styles.statusInactive}>{getCultivationSopStatusLabel(selectedSop.isActive)}</span>
                      <span>{selectedCommodity}</span>
                    </div>
                    <h2>{selectedSop.name}</h2>
                    <p>{selectedSop.description || "Belum ada deskripsi SOP."}</p>
                  </div>
                  {canWrite && (
                    <div className={styles.detailActions}>
                      <button className={styles.secondaryButton} type="button" onClick={() => openEditor({ kind: "sop", sopId: selectedSop.id })}><Icon name="edit" />Ubah SOP</button>
                      <button className={selectedSop.isActive ? styles.dangerButton : styles.successButton} type="button" onClick={() => { setModalError(null); setConfirmation({ kind: "toggle", sopId: selectedSop.id, nextActive: !selectedSop.isActive }); }}>
                        <Icon name={selectedSop.isActive ? "stop" : "check"} />{selectedSop.isActive ? "Nonaktifkan" : "Aktifkan"}
                      </button>
                    </div>
                  )}
                </div>

                <div className={styles.stepHeading}>
                  <div><span className={styles.eyebrow}>Alur kerja</span><h3>Tahapan SOP</h3><p>Urutan dari backend menjadi sumber kebenaran setelah setiap perubahan.</p></div>
                  {canWrite && <button className={styles.primaryButton} type="button" onClick={() => openEditor({ kind: "step", sopId: selectedSop.id, stepId: null })}><Icon name="add" />Tambah tahapan</button>}
                </div>

                <ol className={styles.stepList}>
                  {orderedSteps.map((step, index) => (
                    <li className={styles.stepCard} key={step.id}>
                      <span className={styles.sequence}>{step.sequence}</span>
                      <div className={styles.stepContent}>
                        <div className={styles.stepTitle}>
                          <strong>{step.name}</strong>
                          <span className={step.isRequired ? styles.required : styles.optional}>{step.isRequired ? "Wajib" : "Opsional"}</span>
                        </div>
                        <p>{step.description || "Belum ada deskripsi tahapan."}</p>
                        <div className={styles.stepFacts}>
                          <span><Icon name="clock" />Hari {step.plannedDayOffset >= 0 ? `+${step.plannedDayOffset}` : step.plannedDayOffset}</span>
                          <span><Icon name="required" />{step.estimatedDurationDays} hari</span>
                        </div>
                      </div>
                      {canWrite && (
                        <div className={styles.stepActions}>
                          <button type="button" aria-label={`Naikkan ${step.name}`} disabled={index === 0 || movingStepId !== null} onClick={() => void moveStep(step.id, "up")}><Icon name="up" /></button>
                          <button type="button" aria-label={`Turunkan ${step.name}`} disabled={index === orderedSteps.length - 1 || movingStepId !== null} onClick={() => void moveStep(step.id, "down")}><Icon name="down" /></button>
                          <button type="button" aria-label={`Ubah ${step.name}`} disabled={movingStepId !== null} onClick={() => openEditor({ kind: "step", sopId: selectedSop.id, stepId: step.id })}><Icon name="edit" /></button>
                          <button className={styles.trashButton} type="button" aria-label={`Hapus ${step.name}`} disabled={movingStepId !== null} onClick={() => { setModalError(null); setConfirmation({ kind: "remove-step", sopId: selectedSop.id, stepId: step.id }); }}><Icon name="trash" /></button>
                        </div>
                      )}
                    </li>
                  ))}
                </ol>
                {orderedSteps.length === 0 && (
                  <div className={styles.emptyDetail}><Icon name="steps" /><h3>Belum ada tahapan</h3><p>Tambahkan tahapan agar SOP dapat menjadi panduan operasional yang utuh.</p></div>
                )}
              </>
            ) : (
              <div className={styles.emptyDetail}><Icon name="leaf" /><h2>Pilih SOP untuk melihat detail</h2><p>Detail dan tahapan SOP akan tampil di area ini.</p></div>
            )}
          </main>
        </div>
      )}

      {editor?.kind === "sop" && (editor.sopId === null || modalSop) && (
        <div className={styles.modalBackdrop} role="presentation" onMouseDown={(event) => { if (event.target === event.currentTarget) requestEditorClose("backdrop"); }}>
          <div className={styles.modalPanel} role="dialog" aria-modal="true" aria-label={editor.sopId ? "Ubah SOP budidaya" : "Tambah SOP budidaya"}>
            <SopEditor
              key={editor.sopId ?? "new-sop"}
              sop={modalSop}
              commodities={commodities}
              isSaving={isSaving}
              apiError={modalError}
              onDirtyChange={setEditorDirty}
              onCancel={() => requestEditorClose("explicit")}
              onSubmit={submitSop}
            />
          </div>
        </div>
      )}

      {editor?.kind === "step" && (editor.stepId === null || modalStep) && (
        <div className={styles.modalBackdrop} role="presentation" onMouseDown={(event) => { if (event.target === event.currentTarget) requestEditorClose("backdrop"); }}>
          <div className={styles.modalPanel} role="dialog" aria-modal="true" aria-label={editor.stepId ? "Ubah tahapan SOP" : "Tambah tahapan SOP"}>
            <StepEditor
              key={editor.stepId ?? `new-step-${editor.sopId}`}
              step={modalStep}
              isSaving={isSaving}
              apiError={modalError}
              onDirtyChange={setEditorDirty}
              onCancel={() => requestEditorClose("explicit")}
              onSubmit={submitStep}
            />
          </div>
        </div>
      )}

      {discardOpen && (
        <div className={styles.modalBackdrop} role="presentation" onMouseDown={(event) => { if (event.target === event.currentTarget && !isSaving) setDiscardOpen(false); }}>
          <div className={styles.confirmDialog} role="alertdialog" aria-modal="true" aria-labelledby="sop-discard-title">
            <span className={styles.confirmIcon}><Icon name="stop" /></span>
            <span className={styles.eyebrow}>Perubahan belum disimpan</span>
            <h2 id="sop-discard-title">Buang perubahan formulir?</h2>
            <p>Data yang sudah diubah di formulir ini tidak akan tersimpan.</p>
            <div className={styles.modalFooter}>
              <button className={styles.secondaryButton} type="button" onClick={() => setDiscardOpen(false)}>Lanjut mengisi</button>
              <button className={styles.dangerButton} type="button" onClick={closeEditor}>Buang perubahan</button>
            </div>
          </div>
        </div>
      )}

      {confirmation && (
        <div className={styles.modalBackdrop} role="presentation" onMouseDown={(event) => { if (event.target === event.currentTarget && !isSaving) { setConfirmation(null); setModalError(null); } }}>
          <div className={styles.confirmDialog} role="alertdialog" aria-modal="true">
            <span className={styles.confirmIcon}><Icon name={confirmation.kind === "toggle" && confirmation.nextActive ? "check" : "stop"} /></span>
            <span className={styles.eyebrow}>{confirmation.kind === "remove-step" ? "Hapus tahapan" : "Ubah status SOP"}</span>
            <h2>{confirmation.kind === "remove-step" ? "Hapus tahapan ini?" : `${confirmation.nextActive ? "Aktifkan" : "Nonaktifkan"} SOP ini?`}</h2>
            <p>{confirmation.kind === "remove-step" ? "Tahapan akan dihapus dari SOP. Urutan tahapan lain akan mengikuti respons backend." : confirmation.nextActive ? "SOP akan kembali tersedia untuk penggunaan operasional." : "SOP tetap terlihat untuk histori, tetapi tidak aktif untuk penggunaan baru."}</p>
            {modalError && <div className={styles.errorPanel} role="alert"><span>{modalError}</span></div>}
            <div className={styles.modalFooter}>
              <button className={styles.secondaryButton} type="button" disabled={isSaving} onClick={() => { setConfirmation(null); setModalError(null); }}>Batal</button>
              <button className={confirmation.kind === "toggle" && confirmation.nextActive ? styles.successButton : styles.dangerButton} type="button" disabled={isSaving} onClick={() => void runConfirmation()}>
                {isSaving ? "Memproses..." : confirmation.kind === "remove-step" ? "Hapus tahapan" : confirmation.nextActive ? "Aktifkan SOP" : "Nonaktifkan SOP"}
              </button>
            </div>
          </div>
        </div>
      )}
    </section>
  );
}
