"use client";

import { useCallback, useEffect, useMemo, useState, type FormEvent } from "react";
import { useRouter } from "next/navigation";
import {
  addLandPlot,
  ApiError,
  createLand,
  deleteLand,
  getLands,
  removeLandPlot,
  setLandActive,
  setLandPlotActive,
  updateLand,
  updateLandPlot,
} from "@/lib/api/client";
import type {
  AreaUnit,
  CreateLandRequest,
  Land,
  LandPlot,
  LandTenureType,
  Organization,
} from "@/lib/api/contracts";
import {
  filterLands,
  formatArea,
  formatSquareMeters,
  getAllocationPercentage,
  getAvailableArea,
  optionalNumber,
  optionalText,
  parsePositiveNumber,
  tenureLabels,
  type LandDraft,
  type LandStatusFilter,
  type PlotDraft,
  validateLandDraft,
  validatePlotDraft,
} from "@/lib/lands/land-management";
import {
  hasFormDraftChanged,
  resolveFormCloseDecision,
  type FormCloseSource,
} from "@/lib/ui/form-data-loss";
import styles from "./land-management.module.css";

type LandManagementProps = {
  organization: Organization | null;
  organizationId: string | null;
  permissions: string[];
};

type EditorState =
  | { kind: "land"; mode: "create" }
  | { kind: "land"; mode: "edit"; landId: string }
  | { kind: "plot"; mode: "create"; landId: string }
  | { kind: "plot"; mode: "edit"; landId: string; plotId: string };

type ConfirmationState = {
  title: string;
  message: string;
  confirmLabel: string;
  dangerous: boolean;
  run: () => Promise<Land | void>;
  successMessage: string;
  removedLandId?: string;
};

type IconName =
  | "add"
  | "area"
  | "chevron"
  | "close"
  | "edit"
  | "land"
  | "location"
  | "more"
  | "plot"
  | "refresh"
  | "search"
  | "status"
  | "trash";

const iconPaths: Record<IconName, string> = {
  add: "M12 5v14M5 12h14",
  area: "M4 4h6v6H4V4Zm10 0h6v6h-6V4ZM4 14h6v6H4v-6Zm10 0h6v6h-6v-6Z",
  chevron: "m9 6 6 6-6 6",
  close: "m6 6 12 12M18 6 6 18",
  edit: "m4 20 4.5-1 10-10a2.1 2.1 0 0 0-3-3l-10 10L4 20Zm10-12 3 3",
  land: "M3 6.5 9 4l6 2.5L21 4v13.5L15 20l-6-2.5L3 20V6.5Zm6-2.5v13.5M15 6.5V20",
  location: "M20 10c0 5-8 12-8 12S4 15 4 10a8 8 0 1 1 16 0Zm-5 0a3 3 0 1 1-6 0 3 3 0 0 1 6 0Z",
  more: "M6 12h.01M12 12h.01M18 12h.01",
  plot: "M4 5h16v14H4V5Zm5 0v14m6-14v14M4 11h16",
  refresh: "M20 6v5h-5M4 18v-5h5m10-2a7 7 0 0 0-12-4L4 11m16 2-3 4a7 7 0 0 1-12-4",
  search: "m21 21-4.35-4.35m2.35-5.65a8 8 0 1 1-16 0 8 8 0 0 1 16 0Z",
  status: "M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10Zm-3-10 2 2 4-4",
  trash: "M4 7h16m-10 4v6m4-6v6M6 7l1 13h10l1-13M9 7V4h6v3",
};

function Icon({ name }: { name: IconName }) {
  return (
    <svg viewBox="0 0 24 24" aria-hidden="true">
      <path d={iconPaths[name]} />
    </svg>
  );
}

function replaceLand(lands: Land[], updatedLand: Land): Land[] {
  const exists = lands.some((land) => land.id === updatedLand.id);
  const nextLands = exists
    ? lands.map((land) => land.id === updatedLand.id ? updatedLand : land)
    : [...lands, updatedLand];

  return nextLands.sort((left, right) => left.name.localeCompare(right.name, "id-ID"));
}

function friendlyError(error: unknown): string {
  if (!(error instanceof ApiError)) {
    return error instanceof Error ? error.message : "Permintaan tidak dapat diselesaikan.";
  }

  switch (error.problem?.code) {
    case "Lands.CodeAlreadyExists":
      return "Kode lahan sudah digunakan dalam organisasi ini.";
    case "Lands.PlotCodeAlreadyExists":
      return "Kode petak sudah digunakan pada lahan ini.";
    case "Lands.AreaCapacityExceeded":
      return "Total luas petak tidak boleh melebihi kapasitas lahan.";
    case "CropCycles.ActiveReferenceExists":
      return "Lahan atau petak belum dapat dinonaktifkan karena masih dipakai oleh siklus budidaya terencana atau berjalan.";
    case "CropCycles.HistoricalReferenceExists":
      return "Petak tidak dapat dihapus karena sudah menjadi bagian dari histori siklus budidaya. Nonaktifkan petak untuk mempertahankan riwayatnya.";
    case "Lands.HistoricalReferenceExists":
      return "Lahan tidak dapat dihapus karena sudah menjadi bagian dari histori siklus budidaya. Nonaktifkan lahan untuk mempertahankan riwayatnya.";
    default:
      return error.message;
  }
}

function landDraftFrom(land: Land | null): LandDraft {
  return {
    code: land?.code ?? "",
    name: land?.name ?? "",
    tenureType: land?.tenureType ?? 1,
    totalArea: land ? String(land.totalArea) : "",
    areaUnit: land?.areaUnit ?? 2,
    address: land?.address ?? "",
    locationDescription: land?.locationDescription ?? "",
    latitude: land?.latitude === null || land?.latitude === undefined ? "" : String(land.latitude),
    longitude: land?.longitude === null || land?.longitude === undefined ? "" : String(land.longitude),
    notes: land?.notes ?? "",
  };
}

function plotDraftFrom(plot: LandPlot | null): PlotDraft {
  return {
    code: plot?.code ?? "",
    name: plot?.name ?? "",
    area: plot ? String(plot.area) : "",
    areaUnit: plot?.areaUnit ?? 1,
    generalCondition: plot?.generalCondition ?? "",
    notes: plot?.notes ?? "",
  };
}

function LandEditor({
  land,
  isSaving,
  apiError,
  onDirtyChange,
  onCancel,
  onSubmit,
}: {
  land: Land | null;
  isSaving: boolean;
  apiError: string | null;
  onDirtyChange: (isDirty: boolean) => void;
  onCancel: () => void;
  onSubmit: (request: CreateLandRequest) => Promise<void>;
}) {
  const [initialDraft] = useState<LandDraft>(() => landDraftFrom(land));
  const [draft, setDraft] = useState<LandDraft>(() => initialDraft);
  const [errors, setErrors] = useState<string[]>([]);
  const isCreate = land === null;

  useEffect(() => {
    onDirtyChange(hasFormDraftChanged(initialDraft, draft));
  }, [draft, initialDraft, onDirtyChange]);

  function updateDraft<Key extends keyof LandDraft>(key: Key, value: LandDraft[Key]) {
    setDraft((current) => ({ ...current, [key]: value }));
  }

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const validationErrors = validateLandDraft(
      draft,
      isCreate,
      land?.allocatedPlotAreaInSquareMeters ?? 0,
    );
    setErrors(validationErrors);

    const totalArea = parsePositiveNumber(draft.totalArea);
    if (validationErrors.length > 0 || totalArea === null) {
      return;
    }

    void onSubmit({
      code: draft.code.trim().toUpperCase(),
      name: draft.name.trim(),
      tenureType: draft.tenureType,
      totalArea,
      areaUnit: draft.areaUnit,
      address: optionalText(draft.address),
      locationDescription: optionalText(draft.locationDescription),
      latitude: optionalNumber(draft.latitude),
      longitude: optionalNumber(draft.longitude),
      notes: optionalText(draft.notes),
    });
  }

  return (
    <form className={styles.editorForm} onSubmit={submit} noValidate>
      <div className={styles.editorHeader}>
        <span className={styles.editorIcon}><Icon name="land" /></span>
        <div>
          <span className={styles.eyebrow}>{isCreate ? "Lahan baru" : land.code}</span>
          <h2>{isCreate ? "Tambahkan lahan" : "Ubah informasi lahan"}</h2>
          <p>Data lokasi dan kapasitas menjadi fondasi seluruh pencatatan budidaya.</p>
        </div>
        <button className={styles.closeButton} type="button" aria-label="Tutup formulir" disabled={isSaving} onClick={onCancel}>
          <Icon name="close" />
        </button>
      </div>

      {(errors.length > 0 || apiError) && (
        <div className={styles.formAlert} role="alert">
          <strong>Periksa kembali data berikut:</strong>
          <ul>
            {errors.map((error) => <li key={error}>{error}</li>)}
            {apiError && <li>{apiError}</li>}
          </ul>
        </div>
      )}

      <fieldset disabled={isSaving}>
        <div className={styles.formSection}>
          <div className={styles.formSectionTitle}>
            <strong>Identitas lahan</strong>
            <span>Kode tidak dapat diubah setelah tersimpan.</span>
          </div>
          <div className={styles.formGrid}>
            <label className={styles.field}>
              <span>Kode lahan <em>*</em></span>
              <input value={draft.code} maxLength={30} placeholder="Contoh: LHN-001" disabled={!isCreate} onChange={(event) => updateDraft("code", event.target.value)} />
            </label>
            <label className={`${styles.field} ${styles.fieldWide}`}>
              <span>Nama lahan <em>*</em></span>
              <input value={draft.name} maxLength={150} placeholder="Contoh: Lahan Produksi Timur" onChange={(event) => updateDraft("name", event.target.value)} />
            </label>
            <label className={styles.field}>
              <span>Status penguasaan <em>*</em></span>
              <select value={draft.tenureType} onChange={(event) => updateDraft("tenureType", Number(event.target.value) as LandTenureType)}>
                {(Object.entries(tenureLabels) as [string, string][]).map(([value, label]) => (
                  <option value={value} key={value}>{label}</option>
                ))}
              </select>
            </label>
            <label className={styles.field}>
              <span>Total luas <em>*</em></span>
              <input value={draft.totalArea} inputMode="decimal" placeholder="Contoh: 1,5" onChange={(event) => updateDraft("totalArea", event.target.value)} />
            </label>
            <label className={styles.field}>
              <span>Satuan luas <em>*</em></span>
              <select value={draft.areaUnit} onChange={(event) => updateDraft("areaUnit", Number(event.target.value) as AreaUnit)}>
                <option value={1}>Meter persegi (m²)</option>
                <option value={2}>Hektare (ha)</option>
              </select>
            </label>
          </div>
        </div>

        <div className={styles.formSection}>
          <div className={styles.formSectionTitle}>
            <strong>Lokasi</strong>
            <span>Koordinat bersifat opsional, tetapi harus diisi berpasangan.</span>
          </div>
          <div className={styles.formGrid}>
            <label className={`${styles.field} ${styles.fieldFull}`}>
              <span>Alamat</span>
              <textarea value={draft.address} maxLength={500} rows={2} placeholder="Desa, kecamatan, kabupaten" onChange={(event) => updateDraft("address", event.target.value)} />
            </label>
            <label className={`${styles.field} ${styles.fieldFull}`}>
              <span>Deskripsi lokasi</span>
              <textarea value={draft.locationDescription} maxLength={500} rows={2} placeholder="Patokan jalan, akses masuk, atau kondisi sekitar" onChange={(event) => updateDraft("locationDescription", event.target.value)} />
            </label>
            <label className={styles.field}>
              <span>Lintang</span>
              <input value={draft.latitude} inputMode="decimal" placeholder="-7.7956" onChange={(event) => updateDraft("latitude", event.target.value)} />
            </label>
            <label className={styles.field}>
              <span>Bujur</span>
              <input value={draft.longitude} inputMode="decimal" placeholder="110.3695" onChange={(event) => updateDraft("longitude", event.target.value)} />
            </label>
          </div>
        </div>

        <div className={styles.formSection}>
          <label className={styles.field}>
            <span>Catatan internal</span>
            <textarea value={draft.notes} maxLength={1000} rows={3} placeholder="Informasi sewa, irigasi, akses alat, atau catatan lain" onChange={(event) => updateDraft("notes", event.target.value)} />
          </label>
        </div>
      </fieldset>

      <div className={styles.formActions}>
        <button className={styles.secondaryButton} type="button" disabled={isSaving} onClick={onCancel}>Batal</button>
        <button className={styles.primaryButton} type="submit" disabled={isSaving}>
          {isSaving ? "Menyimpan..." : isCreate ? "Simpan lahan" : "Simpan perubahan"}
        </button>
      </div>
    </form>
  );
}

function PlotEditor({
  land,
  plot,
  isSaving,
  apiError,
  onDirtyChange,
  onCancel,
  onSubmit,
}: {
  land: Land;
  plot: LandPlot | null;
  isSaving: boolean;
  apiError: string | null;
  onDirtyChange: (isDirty: boolean) => void;
  onCancel: () => void;
  onSubmit: (request: PlotDraft) => Promise<void>;
}) {
  const [initialDraft] = useState<PlotDraft>(() => plotDraftFrom(plot));
  const [draft, setDraft] = useState<PlotDraft>(() => initialDraft);
  const [errors, setErrors] = useState<string[]>([]);
  const isCreate = plot === null;

  useEffect(() => {
    onDirtyChange(hasFormDraftChanged(initialDraft, draft));
  }, [draft, initialDraft, onDirtyChange]);

  function updateDraft<Key extends keyof PlotDraft>(key: Key, value: PlotDraft[Key]) {
    setDraft((current) => ({ ...current, [key]: value }));
  }

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const validationErrors = validatePlotDraft(draft, isCreate, land, plot);
    setErrors(validationErrors);
    if (validationErrors.length === 0) {
      void onSubmit(draft);
    }
  }

  const reusableArea = plot ? (plot.areaUnit === 2 ? plot.area * 10_000 : plot.area) : 0;

  return (
    <form className={`${styles.editorForm} ${styles.editorFormCompact}`} onSubmit={submit} noValidate>
      <div className={styles.editorHeader}>
        <span className={styles.editorIcon}><Icon name="plot" /></span>
        <div>
          <span className={styles.eyebrow}>{land.code} · {land.name}</span>
          <h2>{isCreate ? "Tambahkan petak" : "Ubah informasi petak"}</h2>
          <p>Kapasitas tersedia: {formatSquareMeters(getAvailableArea(land) + reusableArea)}.</p>
        </div>
        <button className={styles.closeButton} type="button" aria-label="Tutup formulir" disabled={isSaving} onClick={onCancel}>
          <Icon name="close" />
        </button>
      </div>

      {(errors.length > 0 || apiError) && (
        <div className={styles.formAlert} role="alert">
          <strong>Periksa kembali data berikut:</strong>
          <ul>
            {errors.map((error) => <li key={error}>{error}</li>)}
            {apiError && <li>{apiError}</li>}
          </ul>
        </div>
      )}

      <fieldset disabled={isSaving}>
        <div className={styles.formGrid}>
          <label className={styles.field}>
            <span>Kode petak <em>*</em></span>
            <input value={draft.code} maxLength={30} placeholder="Contoh: PTK-01" disabled={!isCreate} onChange={(event) => updateDraft("code", event.target.value)} />
          </label>
          <label className={`${styles.field} ${styles.fieldWide}`}>
            <span>Nama petak <em>*</em></span>
            <input value={draft.name} maxLength={150} placeholder="Contoh: Petak Utara" onChange={(event) => updateDraft("name", event.target.value)} />
          </label>
          <label className={styles.field}>
            <span>Luas petak <em>*</em></span>
            <input value={draft.area} inputMode="decimal" placeholder="Contoh: 2500" onChange={(event) => updateDraft("area", event.target.value)} />
          </label>
          <label className={styles.field}>
            <span>Satuan luas <em>*</em></span>
            <select value={draft.areaUnit} onChange={(event) => updateDraft("areaUnit", Number(event.target.value) as AreaUnit)}>
              <option value={1}>Meter persegi (m²)</option>
              <option value={2}>Hektare (ha)</option>
            </select>
          </label>
          <label className={`${styles.field} ${styles.fieldFull}`}>
            <span>Kondisi umum</span>
            <textarea value={draft.generalCondition} maxLength={500} rows={2} placeholder="Topografi, jenis tanah, drainase, atau kondisi lain" onChange={(event) => updateDraft("generalCondition", event.target.value)} />
          </label>
          <label className={`${styles.field} ${styles.fieldFull}`}>
            <span>Catatan</span>
            <textarea value={draft.notes} maxLength={1000} rows={3} placeholder="Catatan operasional petak" onChange={(event) => updateDraft("notes", event.target.value)} />
          </label>
        </div>
      </fieldset>

      <div className={styles.formActions}>
        <button className={styles.secondaryButton} type="button" disabled={isSaving} onClick={onCancel}>Batal</button>
        <button className={styles.primaryButton} type="submit" disabled={isSaving}>
          {isSaving ? "Menyimpan..." : isCreate ? "Simpan petak" : "Simpan perubahan"}
        </button>
      </div>
    </form>
  );
}

export function LandManagement({ organization, organizationId, permissions }: LandManagementProps) {
  const router = useRouter();
  const [lands, setLands] = useState<Land[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [query, setQuery] = useState("");
  const [statusFilter, setStatusFilter] = useState<LandStatusFilter>("all");
  const [selectedLandId, setSelectedLandId] = useState<string | null>(null);
  const [editor, setEditor] = useState<EditorState | null>(null);
  const [editorDirty, setEditorDirty] = useState(false);
  const [discardEditorConfirmation, setDiscardEditorConfirmation] = useState(false);
  const [confirmation, setConfirmation] = useState<ConfirmationState | null>(null);
  const [isSaving, setIsSaving] = useState(false);
  const [pageError, setPageError] = useState<string | null>(null);
  const [editorError, setEditorError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const canRead = permissions.includes("lands.read");
  const canWrite = permissions.includes("lands.write");

  const closeEditor = useCallback(() => {
    setEditor(null);
    setEditorDirty(false);
    setDiscardEditorConfirmation(false);
    setEditorError(null);
  }, []);

  const requestEditorClose = useCallback((source: FormCloseSource) => {
    const decision = resolveFormCloseDecision({
      source,
      isDirty: editorDirty,
      isSaving,
    });

    if (decision === "ignore") {
      return;
    }

    if (decision === "confirm-discard") {
      setDiscardEditorConfirmation(true);
      return;
    }

    closeEditor();
  }, [closeEditor, editorDirty, isSaving]);

  async function refreshLands() {
    if (!organizationId || !canRead) {
      return;
    }

    setIsRefreshing(true);
    setPageError(null);

    try {
      const result = await getLands(organizationId);
      setLands(result);
      setSelectedLandId((current) => current && result.some((land) => land.id === current)
        ? current
        : result[0]?.id ?? null);
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

  useEffect(() => {
    let cancelled = false;

    async function loadInitialLands() {
      if (!organizationId || !canRead) {
        if (!cancelled) setIsLoading(false);
        return;
      }

      try {
        const result = await getLands(organizationId);
        if (!cancelled) {
          setLands(result);
          setSelectedLandId(result[0]?.id ?? null);
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

    void loadInitialLands();
    return () => {
      cancelled = true;
    };
  }, [organizationId, canRead, router]);

  useEffect(() => {
    if (!editor && !confirmation && !discardEditorConfirmation) {
      return;
    }

    const originalOverflow = document.body.style.overflow;
    document.body.style.overflow = "hidden";

    function closeWithEscape(event: KeyboardEvent) {
      if (event.key !== "Escape" || isSaving) {
        return;
      }

      if (discardEditorConfirmation) {
        setDiscardEditorConfirmation(false);
        return;
      }

      if (confirmation) {
        setConfirmation(null);
        return;
      }

      if (editor) {
        requestEditorClose("escape");
      }
    }

    window.addEventListener("keydown", closeWithEscape);

    return () => {
      document.body.style.overflow = originalOverflow;
      window.removeEventListener("keydown", closeWithEscape);
    };
  }, [confirmation, discardEditorConfirmation, editor, isSaving, requestEditorClose]);

  const filteredLands = useMemo(
    () => filterLands(lands, query, statusFilter),
    [lands, query, statusFilter],
  );
  const selectedLand = filteredLands.find((land) => land.id === selectedLandId)
    ?? filteredLands[0]
    ?? null;
  const activeLands = lands.filter((land) => land.isActive);
  const totalArea = activeLands.reduce((total, land) => total + land.totalAreaInSquareMeters, 0);
  const activePlots = activeLands.reduce(
    (total, land) => total + land.plots.filter((plot) => plot.isActive).length,
    0,
  );
  const allocatedArea = activeLands.reduce(
    (total, land) => total + land.allocatedPlotAreaInSquareMeters,
    0,
  );

  function applyUpdatedLand(updatedLand: Land, successMessage: string) {
    setLands((current) => replaceLand(current, updatedLand));
    setSelectedLandId(updatedLand.id);
    setNotice(successMessage);
    setPageError(null);
  }

  async function submitLand(request: CreateLandRequest) {
    if (!organizationId || !canWrite) {
      return;
    }
    setIsSaving(true);
    setEditorError(null);

    try {
      const updatedLand = editor?.kind === "land" && editor.mode === "edit"
        ? await updateLand(organizationId, editor.landId, {
          name: request.name,
          tenureType: request.tenureType,
          totalArea: request.totalArea,
          areaUnit: request.areaUnit,
          address: request.address,
          locationDescription: request.locationDescription,
          latitude: request.latitude,
          longitude: request.longitude,
          notes: request.notes,
        })
        : await createLand(organizationId, request);
      applyUpdatedLand(updatedLand, editor?.mode === "edit" ? "Informasi lahan diperbarui." : "Lahan baru berhasil ditambahkan.");
      closeEditor();
    } catch (error) {
      if (error instanceof ApiError && error.status === 401) {
        router.replace("/login");
        return;
      }
      setEditorError(friendlyError(error));
    } finally {
      setIsSaving(false);
    }
  }

  async function submitPlot(draft: PlotDraft) {
    if (!organizationId || !canWrite || editor?.kind !== "plot") {
      return;
    }
    const area = parsePositiveNumber(draft.area);
    if (area === null) {
      return;
    }
    setIsSaving(true);
    setEditorError(null);

    try {
      const payload = {
        name: draft.name.trim(),
        area,
        areaUnit: draft.areaUnit,
        generalCondition: optionalText(draft.generalCondition),
        notes: optionalText(draft.notes),
      };
      const updatedLand = editor.mode === "edit"
        ? await updateLandPlot(organizationId, editor.landId, editor.plotId, payload)
        : await addLandPlot(organizationId, editor.landId, {
          code: draft.code.trim().toUpperCase(),
          ...payload,
        });
      applyUpdatedLand(updatedLand, editor.mode === "edit" ? "Informasi petak diperbarui." : "Petak baru berhasil ditambahkan.");
      closeEditor();
    } catch (error) {
      if (error instanceof ApiError && error.status === 401) {
        router.replace("/login");
        return;
      }
      setEditorError(friendlyError(error));
    } finally {
      setIsSaving(false);
    }
  }

  async function runConfirmation() {
    if (!confirmation) {
      return;
    }
    setIsSaving(true);
    setPageError(null);

    try {
      const result = await confirmation.run();

      if (confirmation.removedLandId) {
        setLands((current) => current.filter(
          (land) => land.id !== confirmation.removedLandId,
        ));
        setSelectedLandId(null);
        setNotice(confirmation.successMessage);
        setPageError(null);
      } else if (result) {
        applyUpdatedLand(result, confirmation.successMessage);
      }

      setConfirmation(null);
    } catch (error) {
      if (error instanceof ApiError && error.status === 401) {
        router.replace("/login");
        return;
      }
      setPageError(friendlyError(error));
      setConfirmation(null);
    } finally {
      setIsSaving(false);
    }
  }

  function editTarget() {
    if (!editor || editor.mode !== "edit") {
      return { land: null, plot: null };
    }
    const land = lands.find((item) => item.id === editor.landId) ?? null;
    const plot = editor.kind === "plot"
      ? land?.plots.find((item) => item.id === editor.plotId) ?? null
      : null;
    return { land, plot };
  }

  const target = editTarget();

  if (!organizationId || !organization) {
    return (
      <section className={styles.accessState}>
        <span className={styles.accessIcon}><Icon name="land" /></span>
        <h1>Pilih organisasi terlebih dahulu</h1>
        <p>Hubungkan akun ke organisasi aktif untuk mengelola lahan dan petak.</p>
      </section>
    );
  }

  if (!canRead) {
    return (
      <section className={styles.accessState}>
        <span className={styles.accessIcon}><Icon name="status" /></span>
        <h1>Akses lahan dibatasi</h1>
        <p>Peran Anda tidak memiliki izin untuk melihat data lahan organisasi ini.</p>
        <button className={styles.secondaryButton} type="button" onClick={() => router.push("/dashboard")}>Kembali ke ringkasan</button>
      </section>
    );
  }

  return (
    <div className={styles.landPage}>
      <section className={styles.hero}>
        <div>
          <span className={styles.eyebrow}>Fondasi operasional</span>
          <h1>Lahan &amp; Petak</h1>
          <p>Kelola lokasi, kapasitas, dan pembagian area budidaya milik {organization.name}.</p>
        </div>
        <div className={styles.heroActions}>
          {!canWrite && <span className={styles.readOnlyBadge}>Mode baca</span>}
          <button className={styles.refreshButton} type="button" disabled={isRefreshing || isLoading} onClick={() => void refreshLands()}>
            <Icon name="refresh" /> {isRefreshing ? "Memperbarui..." : "Perbarui"}
          </button>
          {canWrite && (
            <button className={styles.primaryButton} type="button" onClick={() => {
              setEditorError(null);
              setEditor({ kind: "land", mode: "create" });
            }}>
              <Icon name="add" /> Tambah lahan
            </button>
          )}
        </div>
      </section>

      {notice && (
        <div className={styles.notice} role="status">
          <span><Icon name="status" /></span>
          <strong>{notice}</strong>
          <button type="button" aria-label="Tutup pemberitahuan" onClick={() => setNotice(null)}><Icon name="close" /></button>
        </div>
      )}
      {pageError && <div className={styles.pageError} role="alert">{pageError}</div>}

      <section className={styles.metricGrid} aria-label="Ringkasan lahan">
        <article className={`${styles.metricCard} ${styles.metricPrimary}`}>
          <span>Lahan aktif</span>
          <strong>{activeLands.length}</strong>
          <small>dari {lands.length} lahan terdaftar</small>
          <span className={styles.metricIcon}><Icon name="land" /></span>
        </article>
        <article className={styles.metricCard}>
          <span>Total luas aktif</span>
          <strong>{formatSquareMeters(totalArea)}</strong>
          <small>kapasitas seluruh lahan aktif</small>
          <span className={styles.metricIcon}><Icon name="area" /></span>
        </article>
        <article className={styles.metricCard}>
          <span>Petak aktif</span>
          <strong>{activePlots}</strong>
          <small>unit area siap dipakai budidaya</small>
          <span className={styles.metricIcon}><Icon name="plot" /></span>
        </article>
        <article className={styles.metricCard}>
          <span>Area teralokasi</span>
          <strong>{formatSquareMeters(allocatedArea)}</strong>
          <small>{totalArea > 0 ? `${Math.round((allocatedArea / totalArea) * 100)}% dari kapasitas aktif` : "belum ada kapasitas"}</small>
          <span className={styles.metricIcon}><Icon name="status" /></span>
        </article>
      </section>

      <section className={styles.toolbar} aria-label="Pencarian dan filter lahan">
        <label className={styles.searchField}>
          <Icon name="search" />
          <input value={query} placeholder="Cari kode, nama, alamat, atau lokasi..." onChange={(event) => setQuery(event.target.value)} />
        </label>
        <label className={styles.statusField}>
          <span>Status</span>
          <select value={statusFilter} onChange={(event) => setStatusFilter(event.target.value as LandStatusFilter)}>
            <option value="all">Semua lahan</option>
            <option value="active">Aktif</option>
            <option value="inactive">Nonaktif</option>
          </select>
        </label>
        <span className={styles.resultCount}>{filteredLands.length} hasil</span>
      </section>

      {isLoading ? (
        <section className={styles.loadingState}><span className="loader" /><p>Memuat data lahan...</p></section>
      ) : lands.length === 0 ? (
        <section className={styles.emptyState}>
          <span className={styles.emptyIcon}><Icon name="land" /></span>
          <span className={styles.eyebrow}>Belum ada data</span>
          <h2>Mulai dari lahan pertama</h2>
          <p>Tambahkan identitas lokasi dan luas lahan sebelum membuka siklus budidaya.</p>
          {canWrite && <button className={styles.primaryButton} type="button" onClick={() => setEditor({ kind: "land", mode: "create" })}><Icon name="add" /> Tambah lahan pertama</button>}
        </section>
      ) : filteredLands.length === 0 ? (
        <section className={styles.emptyState}>
          <span className={styles.emptyIcon}><Icon name="search" /></span>
          <h2>Lahan tidak ditemukan</h2>
          <p>Ubah kata pencarian atau status filter untuk melihat hasil lain.</p>
          <button className={styles.secondaryButton} type="button" onClick={() => { setQuery(""); setStatusFilter("all"); }}>Atur ulang filter</button>
        </section>
      ) : (
        <section className={styles.managementGrid}>
          <aside className={styles.landList} aria-label="Daftar lahan">
            <div className={styles.listHeader}>
              <div>
                <span className={styles.eyebrow}>Daftar lokasi</span>
                <h2>{filteredLands.length} lahan</h2>
              </div>
            </div>
            <div className={styles.landCards}>
              {filteredLands.map((land) => {
                const percentage = getAllocationPercentage(land);
                const activePlotsOnLand = land.plots.filter((plot) => plot.isActive).length;
                return (
                  <button
                    className={`${styles.landCard} ${selectedLand?.id === land.id ? styles.landCardActive : ""}`}
                    type="button"
                    key={land.id}
                    aria-pressed={selectedLand?.id === land.id}
                    onClick={() => setSelectedLandId(land.id)}
                  >
                    <span className={styles.landCardIcon}><Icon name="land" /></span>
                    <span className={styles.landCardMain}>
                      <span className={styles.landCardTop}>
                        <span><small>{land.code}</small><strong>{land.name}</strong></span>
                        <span className={land.isActive ? styles.activeBadge : styles.inactiveBadge}>{land.isActive ? "Aktif" : "Nonaktif"}</span>
                      </span>
                      <span className={styles.landCardMeta}>{tenureLabels[land.tenureType]} · {activePlotsOnLand} petak aktif</span>
                      <span className={styles.capacityTrack}><span style={{ width: `${percentage}%` }} /></span>
                      <span className={styles.capacityLabel}><small>{formatSquareMeters(land.allocatedPlotAreaInSquareMeters)} teralokasi</small><strong>{Math.round(percentage)}%</strong></span>
                    </span>
                    <span className={styles.cardChevron}><Icon name="chevron" /></span>
                  </button>
                );
              })}
            </div>
          </aside>

          {selectedLand && (
            <article className={styles.landDetail}>
              <header className={styles.detailHeader}>
                <div className={styles.detailIdentity}>
                  <span className={styles.detailIcon}><Icon name="land" /></span>
                  <div>
                    <span className={styles.eyebrow}>{selectedLand.code}</span>
                    <h2>{selectedLand.name}</h2>
                    <p>{tenureLabels[selectedLand.tenureType]} · dibuat {new Intl.DateTimeFormat("id-ID", { dateStyle: "medium" }).format(new Date(selectedLand.createdAt))}</p>
                  </div>
                </div>
                <div className={styles.detailActions}>
                  <span className={selectedLand.isActive ? styles.activeBadge : styles.inactiveBadge}>{selectedLand.isActive ? "Lahan aktif" : "Lahan nonaktif"}</span>
                  {canWrite && (
                    <>
                      <button className={styles.iconAction} type="button" title="Ubah lahan" aria-label="Ubah lahan" onClick={() => {
                        setEditorError(null);
                        setEditor({ kind: "land", mode: "edit", landId: selectedLand.id });
                      }}><Icon name="edit" /></button>
                      <button className={`${styles.iconAction} ${styles.dangerAction}`} type="button" title="Hapus lahan" aria-label={`Hapus ${selectedLand.name}`} onClick={() => setConfirmation({
                        title: "Hapus lahan secara permanen?",
                        message: `${selectedLand.code} · ${selectedLand.name} beserta seluruh petaknya akan dihapus. Operasi akan ditolak jika lahan pernah dipakai oleh siklus budidaya.`,
                        confirmLabel: "Hapus lahan",
                        dangerous: true,
                        run: () => deleteLand(organizationId, selectedLand.id),
                        successMessage: "Lahan berhasil dihapus.",
                        removedLandId: selectedLand.id,
                      })}><Icon name="trash" /></button>
                      <button className={styles.textAction} type="button" onClick={() => setConfirmation({
                        title: selectedLand.isActive ? "Nonaktifkan lahan?" : "Aktifkan kembali lahan?",
                        message: selectedLand.isActive
                          ? "Lahan tetap tersimpan, tetapi tidak dapat dipakai untuk siklus baru. Lahan dengan siklus aktif akan ditolak oleh sistem."
                          : "Lahan akan tersedia kembali untuk operasi budidaya.",
                        confirmLabel: selectedLand.isActive ? "Nonaktifkan" : "Aktifkan",
                        dangerous: selectedLand.isActive,
                        run: () => setLandActive(organizationId, selectedLand.id, !selectedLand.isActive),
                        successMessage: selectedLand.isActive ? "Lahan dinonaktifkan." : "Lahan diaktifkan kembali.",
                      })}>{selectedLand.isActive ? "Nonaktifkan" : "Aktifkan"}</button>
                    </>
                  )}
                </div>
              </header>

              <div className={styles.capacityPanel}>
                <div className={styles.capacitySummary}>
                  <div><small>Total luas</small><strong>{formatArea(selectedLand.totalArea, selectedLand.areaUnit)}</strong></div>
                  <div><small>Sudah dialokasikan</small><strong>{formatSquareMeters(selectedLand.allocatedPlotAreaInSquareMeters)}</strong></div>
                  <div><small>Sisa kapasitas</small><strong>{formatSquareMeters(getAvailableArea(selectedLand))}</strong></div>
                </div>
                <div className={styles.capacityLargeTrack}>
                  <span style={{ width: `${getAllocationPercentage(selectedLand)}%` }} />
                </div>
                <small>{Math.round(getAllocationPercentage(selectedLand))}% area telah dibagi menjadi petak</small>
              </div>

              <div className={styles.infoGrid}>
                <div className={styles.infoBlock}>
                  <span className={styles.infoIcon}><Icon name="location" /></span>
                  <div>
                    <small>Lokasi</small>
                    <strong>{selectedLand.address ?? "Alamat belum dicatat"}</strong>
                    <p>{selectedLand.locationDescription ?? "Deskripsi lokasi belum tersedia."}</p>
                    {selectedLand.latitude !== null && selectedLand.longitude !== null && (
                      <a href={`https://www.openstreetmap.org/?mlat=${selectedLand.latitude}&mlon=${selectedLand.longitude}#map=16/${selectedLand.latitude}/${selectedLand.longitude}`} target="_blank" rel="noreferrer">
                        {selectedLand.latitude}, {selectedLand.longitude} ↗
                      </a>
                    )}
                  </div>
                </div>
                <div className={styles.infoBlock}>
                  <span className={styles.infoIcon}><Icon name="more" /></span>
                  <div>
                    <small>Catatan lahan</small>
                    <strong>{selectedLand.notes ? "Catatan tersedia" : "Belum ada catatan"}</strong>
                    <p>{selectedLand.notes ?? "Tambahkan informasi sewa, akses irigasi, atau kondisi penting lainnya."}</p>
                  </div>
                </div>
              </div>

              <section className={styles.plotSection}>
                <div className={styles.plotHeader}>
                  <div>
                    <span className={styles.eyebrow}>Pembagian area</span>
                    <h3>Petak lahan</h3>
                    <p>{selectedLand.plots.length} petak terdaftar pada lahan ini.</p>
                  </div>
                  {canWrite && selectedLand.isActive && (
                    <button className={styles.secondaryButton} type="button" disabled={getAvailableArea(selectedLand) <= 0} onClick={() => {
                      setEditorError(null);
                      setEditor({ kind: "plot", mode: "create", landId: selectedLand.id });
                    }}><Icon name="add" /> Tambah petak</button>
                  )}
                </div>

                {selectedLand.plots.length === 0 ? (
                  <div className={styles.plotEmpty}>
                    <span><Icon name="plot" /></span>
                    <div><strong>Belum ada petak</strong><p>Bagi lahan menjadi area kerja yang lebih terukur.</p></div>
                  </div>
                ) : (
                  <div className={styles.plotTableWrap}>
                    <table className={styles.plotTable}>
                      <thead><tr><th>Petak</th><th>Luas</th><th>Kondisi</th><th>Status</th>{canWrite && <th><span className={styles.srOnly}>Aksi</span></th>}</tr></thead>
                      <tbody>
                        {selectedLand.plots.map((plot) => (
                          <tr key={plot.id}>
                            <td><span className={styles.plotIdentity}><span><Icon name="plot" /></span><span><strong>{plot.name}</strong><small>{plot.code}</small></span></span></td>
                            <td><strong>{formatArea(plot.area, plot.areaUnit)}</strong></td>
                            <td><span className={styles.conditionText}>{plot.generalCondition ?? "Belum dicatat"}</span></td>
                            <td><span className={plot.isActive ? styles.activeBadge : styles.inactiveBadge}>{plot.isActive ? "Aktif" : "Nonaktif"}</span></td>
                            {canWrite && (
                              <td>
                                <div className={styles.rowActions}>
                                  <button type="button" title="Ubah petak" aria-label={`Ubah ${plot.name}`} onClick={() => {
                                    setEditorError(null);
                                    setEditor({ kind: "plot", mode: "edit", landId: selectedLand.id, plotId: plot.id });
                                  }}><Icon name="edit" /></button>
                                  <button type="button" title={plot.isActive ? "Nonaktifkan petak" : "Aktifkan petak"} aria-label={`${plot.isActive ? "Nonaktifkan" : "Aktifkan"} ${plot.name}`} onClick={() => setConfirmation({
                                    title: plot.isActive ? "Nonaktifkan petak?" : "Aktifkan kembali petak?",
                                    message: plot.isActive ? `${plot.name} tetap tersimpan tetapi tidak tersedia untuk operasi baru.` : `${plot.name} akan tersedia kembali untuk operasi budidaya.`,
                                    confirmLabel: plot.isActive ? "Nonaktifkan" : "Aktifkan",
                                    dangerous: false,
                                    run: () => setLandPlotActive(organizationId, selectedLand.id, plot.id, !plot.isActive),
                                    successMessage: plot.isActive ? "Petak dinonaktifkan." : "Petak diaktifkan kembali.",
                                  })}><Icon name="status" /></button>
                                  <button className={styles.dangerAction} type="button" title="Hapus petak" aria-label={`Hapus ${plot.name}`} onClick={() => setConfirmation({
                                    title: "Hapus petak secara permanen?",
                                    message: `${plot.code} · ${plot.name} akan dihapus dari lahan. Operasi akan ditolak jika petak masih dirujuk oleh data budidaya.`,
                                    confirmLabel: "Hapus petak",
                                    dangerous: true,
                                    run: () => removeLandPlot(organizationId, selectedLand.id, plot.id),
                                    successMessage: "Petak berhasil dihapus.",
                                  })}><Icon name="trash" /></button>
                                </div>
                              </td>
                            )}
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                )}
              </section>
            </article>
          )}
        </section>
      )}

      {editor && (
        <div className={styles.modalBackdrop} role="presentation" onMouseDown={(event) => {
          if (event.target === event.currentTarget) {
            requestEditorClose("backdrop");
          }
        }}>
          <div className={styles.modalPanel} role="dialog" aria-modal="true" aria-label={editor.kind === "land" ? "Formulir lahan" : "Formulir petak"}>
            {editor.kind === "land" ? (
              <LandEditor land={editor.mode === "edit" ? target.land : null} isSaving={isSaving} apiError={editorError} onDirtyChange={setEditorDirty} onCancel={() => requestEditorClose("explicit")} onSubmit={submitLand} />
            ) : (
              (() => {
                const land = lands.find((item) => item.id === editor.landId);
                if (!land) return null;
                return <PlotEditor land={land} plot={editor.mode === "edit" ? target.plot : null} isSaving={isSaving} apiError={editorError} onDirtyChange={setEditorDirty} onCancel={() => requestEditorClose("explicit")} onSubmit={submitPlot} />;
              })()
            )}
          </div>
        </div>
      )}

      {discardEditorConfirmation && (
        <div className={styles.modalBackdrop} role="presentation" onMouseDown={(event) => {
          if (event.target === event.currentTarget && !isSaving) {
            setDiscardEditorConfirmation(false);
          }
        }}>
          <div className={styles.confirmDialog} role="alertdialog" aria-modal="true" aria-labelledby="discard-editor-title" aria-describedby="discard-editor-description">
            <span className={`${styles.confirmIcon} ${styles.confirmIconDanger}`}><Icon name="trash" /></span>
            <h2 id="discard-editor-title">Buang perubahan formulir?</h2>
            <p id="discard-editor-description">Perubahan yang belum disimpan akan hilang. Pilih lanjut mengedit untuk kembali ke formulir.</p>
            <div className={styles.confirmActions}>
              <button className={styles.secondaryButton} type="button" disabled={isSaving} onClick={() => setDiscardEditorConfirmation(false)}>Lanjut mengedit</button>
              <button className={styles.dangerButton} type="button" disabled={isSaving} onClick={closeEditor}>Buang perubahan</button>
            </div>
          </div>
        </div>
      )}

      {confirmation && (
        <div className={styles.modalBackdrop} role="presentation" onMouseDown={(event) => {
          if (event.target === event.currentTarget && !isSaving) setConfirmation(null);
        }}>
          <div className={styles.confirmDialog} role="alertdialog" aria-modal="true" aria-labelledby="confirmation-title" aria-describedby="confirmation-description">
            <span className={`${styles.confirmIcon} ${confirmation.dangerous ? styles.confirmIconDanger : ""}`}><Icon name={confirmation.dangerous ? "trash" : "status"} /></span>
            <h2 id="confirmation-title">{confirmation.title}</h2>
            <p id="confirmation-description">{confirmation.message}</p>
            <div className={styles.confirmActions}>
              <button className={styles.secondaryButton} type="button" disabled={isSaving} onClick={() => setConfirmation(null)}>Batal</button>
              <button className={confirmation.dangerous ? styles.dangerButton : styles.primaryButton} type="button" disabled={isSaving} onClick={() => void runConfirmation()}>
                {isSaving ? "Memproses..." : confirmation.confirmLabel}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
