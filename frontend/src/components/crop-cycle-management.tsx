"use client";

import { useEffect, useMemo, useState, type FormEvent } from "react";
import { useRouter } from "next/navigation";
import {
  ApiError,
  cancelCropCycle,
  completeCropCycle,
  createCropCycle,
  getCommodities,
  getCropCycles,
  getCultivationSops,
  getLands,
  startCropCycle,
  updateCropCycleNotes,
  updateCropCyclePlan,
} from "@/lib/api/client";
import type {
  AreaUnit,
  Commodity,
  CreateCropCycleRequest,
  CropCycle,
  CultivationSop,
  Land,
  Organization,
} from "@/lib/api/contracts";
import {
  cropCycleDraftFrom,
  cropCycleStatusLabels,
  filterCropCycles,
  formatArea,
  formatDateOnly,
  getCycleReferences,
  getPlannedDurationDays,
  optionalText,
  parsePositiveNumber,
  type CropCycleDraft,
  type CropCycleStatusFilter,
  validateCropCycleDraft,
} from "@/lib/cultivation/crop-cycle-management";
import styles from "./crop-cycle-management.module.css";

type CropCycleManagementProps = {
  organization: Organization | null;
  organizationId: string | null;
  permissions: string[];
};

type EditorState =
  | { mode: "create" }
  | { mode: "edit"; cropCycleId: string };

type ActionKind = "start" | "complete" | "cancel" | "notes";

type ActionState = {
  kind: ActionKind;
  cropCycleId: string;
};

type IconName =
  | "add"
  | "calendar"
  | "check"
  | "close"
  | "edit"
  | "field"
  | "flag"
  | "notes"
  | "refresh"
  | "search"
  | "sprout"
  | "stop";

const iconPaths: Record<IconName, string> = {
  add: "M12 5v14M5 12h14",
  calendar: "M6 3v3m12-3v3M4 9h16M5 5h14a1 1 0 0 1 1 1v14H4V6a1 1 0 0 1 1-1Z",
  check: "m5 12 4 4L19 6",
  close: "m6 6 12 12M18 6 6 18",
  edit: "m4 20 4.5-1 10-10a2.1 2.1 0 0 0-3-3l-10 10L4 20Zm10-12 3 3",
  field: "M3 6.5 9 4l6 2.5L21 4v13.5L15 20l-6-2.5L3 20V6.5Zm6-2.5v13.5M15 6.5V20",
  flag: "M5 21V4m0 1h11l-2 4 2 4H5",
  notes: "M5 4h14v16H5V4Zm4 4h6m-6 4h6m-6 4h4",
  refresh: "M20 6v5h-5M4 18v-5h5m10-2a7 7 0 0 0-12-4L4 11m16 2-3 4a7 7 0 0 1-12-4",
  search: "m21 21-4.35-4.35m2.35-5.65a8 8 0 1 1-16 0 8 8 0 0 1 16 0Z",
  sprout: "M12 21v-9m0 2c-4 0-7-2-7-6 4 0 7 2 7 6Zm0-3c4 0 7-2 7-6-4 0-7 2-7 6Z",
  stop: "M6 6h12v12H6V6Z",
};

function Icon({ name }: { name: IconName }) {
  return (
    <svg viewBox="0 0 24 24" aria-hidden="true">
      <path d={iconPaths[name]} />
    </svg>
  );
}

function replaceCropCycle(cycles: CropCycle[], updatedCycle: CropCycle): CropCycle[] {
  return cycles.some((cycle) => cycle.id === updatedCycle.id)
    ? cycles.map((cycle) => cycle.id === updatedCycle.id ? updatedCycle : cycle)
    : [...cycles, updatedCycle];
}

function friendlyError(error: unknown): string {
  if (!(error instanceof ApiError)) {
    return error instanceof Error ? error.message : "Permintaan tidak dapat diselesaikan.";
  }

  switch (error.problem?.code) {
    case "CropCycles.CodeAlreadyExists":
      return "Kode siklus sudah digunakan dalam organisasi ini.";
    case "CropCycles.ScheduleConflict":
      return "Periode budidaya bertumpang tindih dengan siklus lain pada petak yang sama.";
    case "CropCycles.ActiveCycleAlreadyExists":
      return "Petak ini sudah memiliki siklus budidaya yang sedang berjalan.";
    case "CropCycles.AreaCapacityExceeded":
      return "Luas tanam melebihi kapasitas petak yang dipilih.";
    case "CropCycles.SopCommodityMismatch":
      return "SOP budidaya tidak sesuai dengan komoditas yang dipilih.";
    case "CropCycles.CommodityInactive":
    case "CropCycles.LandInactive":
    case "CropCycles.PlotInactive":
    case "CropCycles.SopInactive":
      return "Salah satu referensi sudah tidak aktif. Pilih data aktif sebelum melanjutkan.";
    case "CropCycles.InvalidStatusTransition":
      return "Tindakan ini tidak sesuai dengan status siklus saat ini. Muat ulang data lalu periksa kembali.";
    case "CultivationActivities.CropCycleHasInProgressActivities":
      return "Siklus belum dapat ditutup atau dibatalkan karena masih memiliki aktivitas yang sedang berjalan.";
    case "HarvestBatches.CropCycleHasDraftHarvests":
      return "Siklus belum dapat diselesaikan karena masih memiliki catatan panen berstatus draf.";
    case "HarvestBatches.CropCycleHasNonCancelledHarvests":
      return "Siklus tidak dapat dibatalkan karena sudah memiliki catatan panen yang tidak dibatalkan.";
    default:
      return error.message;
  }
}

function localToday(): string {
  const now = new Date();
  const localTime = new Date(now.getTime() - now.getTimezoneOffset() * 60_000);
  return localTime.toISOString().slice(0, 10);
}

function CycleEditor({
  cycle,
  commodities,
  cultivationSops,
  lands,
  isSaving,
  apiError,
  onCancel,
  onSubmit,
}: {
  cycle: CropCycle | null;
  commodities: Commodity[];
  cultivationSops: CultivationSop[];
  lands: Land[];
  isSaving: boolean;
  apiError: string | null;
  onCancel: () => void;
  onSubmit: (request: CreateCropCycleRequest) => Promise<void>;
}) {
  const [draft, setDraft] = useState<CropCycleDraft>(() => cropCycleDraftFrom(cycle));
  const [errors, setErrors] = useState<string[]>([]);
  const isCreate = cycle === null;
  const selectedLand = lands.find((land) => land.id === draft.landId) ?? null;
  const selectedPlot = selectedLand?.plots.find((plot) => plot.id === draft.landPlotId) ?? null;
  const commodityOptions = commodities.filter((item) => item.isActive || item.id === cycle?.commodityId);
  const landOptions = lands.filter((item) => item.isActive || item.id === cycle?.landId);
  const plotOptions = selectedLand?.plots.filter(
    (item) => item.isActive || item.id === cycle?.landPlotId,
  ) ?? [];
  const sopOptions = cultivationSops.filter(
    (item) => item.commodityId === draft.commodityId
      && (item.isActive || item.id === cycle?.cultivationSopId),
  );

  function updateDraft<Key extends keyof CropCycleDraft>(
    key: Key,
    value: CropCycleDraft[Key],
  ) {
    setDraft((current) => ({ ...current, [key]: value }));
  }

  function changeCommodity(commodityId: string) {
    setDraft((current) => {
      const currentSop = cultivationSops.find(
        (item) => item.id === current.cultivationSopId,
      );
      return {
        ...current,
        commodityId,
        cultivationSopId: currentSop?.commodityId === commodityId
          ? current.cultivationSopId
          : "",
      };
    });
  }

  function changeLand(landId: string) {
    setDraft((current) => ({ ...current, landId, landPlotId: "" }));
  }

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const validationErrors = validateCropCycleDraft(
      draft,
      isCreate,
      commodities,
      cultivationSops,
      lands,
    );
    setErrors(validationErrors);
    const plantedArea = parsePositiveNumber(draft.plantedArea);

    if (validationErrors.length > 0 || plantedArea === null) {
      return;
    }

    void onSubmit({
      code: draft.code.trim().toUpperCase(),
      name: draft.name.trim(),
      commodityId: draft.commodityId,
      cultivationSopId: draft.cultivationSopId || null,
      landId: draft.landId,
      landPlotId: draft.landPlotId,
      plantedArea,
      areaUnit: draft.areaUnit,
      plannedStartDate: draft.plannedStartDate,
      expectedHarvestDate: draft.expectedHarvestDate,
      notes: optionalText(draft.notes),
    });
  }

  return (
    <form className={styles.editorForm} onSubmit={submit} noValidate>
      <div className={styles.editorHeader}>
        <span className={styles.editorIcon}><Icon name="sprout" /></span>
        <div>
          <span className={styles.eyebrow}>{isCreate ? "Siklus baru" : cycle.code}</span>
          <h2>{isCreate ? "Buka rencana budidaya" : "Ubah rencana budidaya"}</h2>
          <p>Hubungkan lokasi, komoditas, SOP, luas tanam, dan jadwal dalam satu periode.</p>
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
        <section className={styles.formSection}>
          <div className={styles.formSectionTitle}>
            <strong>Identitas siklus</strong>
            <span>Kode, komoditas, lahan, dan petak terkunci setelah disimpan.</span>
          </div>
          <div className={styles.formGrid}>
            <label className={styles.field}>
              <span>Kode siklus <em>*</em></span>
              <input value={draft.code} maxLength={40} placeholder="Contoh: SB-CABAI-2608" disabled={!isCreate} onChange={(event) => updateDraft("code", event.target.value)} />
            </label>
            <label className={`${styles.field} ${styles.fieldWide}`}>
              <span>Nama siklus <em>*</em></span>
              <input value={draft.name} maxLength={150} placeholder="Contoh: Cabai Petak Utara Musim Kemarau" onChange={(event) => updateDraft("name", event.target.value)} />
            </label>
            <label className={styles.field}>
              <span>Komoditas <em>*</em></span>
              <select value={draft.commodityId} disabled={!isCreate} onChange={(event) => changeCommodity(event.target.value)}>
                <option value="">Pilih komoditas</option>
                {commodityOptions.map((item) => (
                  <option value={item.id} key={item.id}>{item.code} · {item.name}{item.isActive ? "" : " (nonaktif)"}</option>
                ))}
              </select>
            </label>
            <label className={`${styles.field} ${styles.fieldWide}`}>
              <span>SOP budidaya</span>
              <select value={draft.cultivationSopId} onChange={(event) => updateDraft("cultivationSopId", event.target.value)}>
                <option value="">Tanpa SOP</option>
                {sopOptions.map((item) => (
                  <option value={item.id} key={item.id}>{item.name} · {item.steps.length} langkah{item.isActive ? "" : " (nonaktif)"}</option>
                ))}
              </select>
              {draft.commodityId && sopOptions.length === 0 && <small>Belum ada SOP aktif untuk komoditas ini.</small>}
            </label>
          </div>
        </section>

        <section className={styles.formSection}>
          <div className={styles.formSectionTitle}>
            <strong>Lokasi dan kapasitas</strong>
            <span>Satu siklus hanya berlaku untuk satu petak.</span>
          </div>
          <div className={styles.formGrid}>
            <label className={styles.field}>
              <span>Lahan <em>*</em></span>
              <select value={draft.landId} disabled={!isCreate} onChange={(event) => changeLand(event.target.value)}>
                <option value="">Pilih lahan</option>
                {landOptions.map((item) => (
                  <option value={item.id} key={item.id}>{item.code} · {item.name}{item.isActive ? "" : " (nonaktif)"}</option>
                ))}
              </select>
            </label>
            <label className={`${styles.field} ${styles.fieldWide}`}>
              <span>Petak <em>*</em></span>
              <select value={draft.landPlotId} disabled={!isCreate || !draft.landId} onChange={(event) => updateDraft("landPlotId", event.target.value)}>
                <option value="">Pilih petak</option>
                {plotOptions.map((item) => (
                  <option value={item.id} key={item.id}>{item.code} · {item.name} · {formatArea(item.area, item.areaUnit)}{item.isActive ? "" : " (nonaktif)"}</option>
                ))}
              </select>
            </label>
            <label className={styles.field}>
              <span>Luas tanam <em>*</em></span>
              <input value={draft.plantedArea} inputMode="decimal" placeholder="Contoh: 2500" onChange={(event) => updateDraft("plantedArea", event.target.value)} />
              {selectedPlot && <small>Kapasitas petak {formatArea(selectedPlot.area, selectedPlot.areaUnit)}.</small>}
            </label>
            <label className={styles.field}>
              <span>Satuan luas <em>*</em></span>
              <select value={draft.areaUnit} onChange={(event) => updateDraft("areaUnit", Number(event.target.value) as AreaUnit)}>
                <option value={1}>Meter persegi (m²)</option>
                <option value={2}>Hektare (ha)</option>
              </select>
            </label>
          </div>
        </section>

        <section className={styles.formSection}>
          <div className={styles.formSectionTitle}>
            <strong>Jadwal budidaya</strong>
            <span>Periode pada petak yang sama tidak boleh bertumpang tindih.</span>
          </div>
          <div className={styles.formGrid}>
            <label className={styles.field}>
              <span>Mulai rencana <em>*</em></span>
              <input type="date" value={draft.plannedStartDate} onChange={(event) => updateDraft("plannedStartDate", event.target.value)} />
            </label>
            <label className={styles.field}>
              <span>Perkiraan panen <em>*</em></span>
              <input type="date" value={draft.expectedHarvestDate} min={draft.plannedStartDate || undefined} onChange={(event) => updateDraft("expectedHarvestDate", event.target.value)} />
            </label>
            <label className={`${styles.field} ${styles.fieldFull}`}>
              <span>Catatan rencana</span>
              <textarea value={draft.notes} maxLength={1000} rows={4} placeholder="Target, asumsi musim, penanggung jawab, atau catatan persiapan" onChange={(event) => updateDraft("notes", event.target.value)} />
            </label>
          </div>
        </section>
      </fieldset>

      <div className={styles.formActions}>
        <button className={styles.secondaryButton} type="button" disabled={isSaving} onClick={onCancel}>Batal</button>
        <button className={styles.primaryButton} type="submit" disabled={isSaving}>
          {isSaving ? "Menyimpan..." : isCreate ? "Simpan rencana" : "Simpan perubahan"}
        </button>
      </div>
    </form>
  );
}

function CycleActionDialog({
  cycle,
  kind,
  isSaving,
  apiError,
  onCancel,
  onSubmit,
}: {
  cycle: CropCycle;
  kind: ActionKind;
  isSaving: boolean;
  apiError: string | null;
  onCancel: () => void;
  onSubmit: (value: string) => Promise<void>;
}) {
  const initialValue = kind === "notes" ? cycle.notes ?? "" : localToday();
  const [value, setValue] = useState(initialValue);
  const [error, setError] = useState<string | null>(null);
  const content = {
    start: {
      eyebrow: "Mulai pelaksanaan",
      title: "Mulai siklus budidaya",
      description: "Catat tanggal mulai nyata. Identitas dan rencana utama akan terkunci setelah siklus berjalan.",
      label: "Tanggal mulai aktual",
      button: "Mulai siklus",
      icon: "flag" as IconName,
      input: "date" as const,
    },
    complete: {
      eyebrow: "Tutup musim",
      title: "Selesaikan siklus budidaya",
      description: "Catat tanggal panen akhir. Siklus selesai dipertahankan sebagai histori dan tidak dapat dibuka kembali.",
      label: "Tanggal panen aktual",
      button: "Selesaikan siklus",
      icon: "check" as IconName,
      input: "date" as const,
    },
    cancel: {
      eyebrow: "Hentikan siklus",
      title: "Batalkan siklus budidaya",
      description: "Pembatalan tetap disimpan untuk audit dan evaluasi musim berikutnya.",
      label: "Alasan pembatalan",
      button: "Batalkan siklus",
      icon: "stop" as IconName,
      input: "textarea" as const,
    },
    notes: {
      eyebrow: "Catatan operasional",
      title: "Perbarui catatan siklus",
      description: "Tambahkan konteks penting tanpa mengubah identitas atau jadwal utama.",
      label: "Catatan",
      button: "Simpan catatan",
      icon: "notes" as IconName,
      input: "textarea" as const,
    },
  }[kind];

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const normalized = value.trim();
    let validationError: string | null = null;

    if (kind === "start" && (!normalized || normalized > cycle.expectedHarvestDate)) {
      validationError = "Tanggal mulai aktual wajib diisi dan tidak boleh setelah perkiraan panen.";
    } else if (kind === "complete"
      && (!normalized || Boolean(cycle.actualStartDate && normalized < cycle.actualStartDate))) {
      validationError = "Tanggal panen aktual wajib diisi dan tidak boleh sebelum tanggal mulai aktual.";
    } else if (kind === "cancel" && !normalized) {
      validationError = "Alasan pembatalan wajib diisi.";
    } else if (kind === "cancel" && normalized.length > 500) {
      validationError = "Alasan pembatalan maksimal 500 karakter.";
    } else if (kind === "notes" && normalized.length > 1000) {
      validationError = "Catatan maksimal 1000 karakter.";
    }

    setError(validationError);
    if (!validationError) {
      void onSubmit(kind === "notes" ? value : normalized);
    }
  }

  return (
    <form className={styles.actionDialog} onSubmit={submit} noValidate>
      <div className={styles.actionIcon}><Icon name={content.icon} /></div>
      <span className={styles.eyebrow}>{content.eyebrow}</span>
      <h2>{content.title}</h2>
      <p>{content.description}</p>
      <div className={styles.cycleContext}>
        <strong>{cycle.code}</strong>
        <span>{cycle.name}</span>
      </div>
      {(error || apiError) && <div className={styles.formAlert} role="alert">{error ?? apiError}</div>}
      <label className={styles.field}>
        <span>{content.label} <em>{kind === "notes" ? "" : "*"}</em></span>
        {content.input === "date" ? (
          <input type="date" value={value} min={kind === "complete" ? cycle.actualStartDate ?? undefined : undefined} max={kind === "start" ? cycle.expectedHarvestDate : undefined} onChange={(event) => setValue(event.target.value)} />
        ) : (
          <textarea value={value} maxLength={kind === "cancel" ? 500 : 1000} rows={5} placeholder={kind === "cancel" ? "Jelaskan alasan siklus dihentikan" : "Catatan kondisi, keputusan, atau tindak lanjut"} onChange={(event) => setValue(event.target.value)} />
        )}
      </label>
      <div className={styles.actionButtons}>
        <button className={styles.secondaryButton} type="button" disabled={isSaving} onClick={onCancel}>Kembali</button>
        <button className={kind === "cancel" ? styles.dangerButton : styles.primaryButton} type="submit" disabled={isSaving}>
          {isSaving ? "Memproses..." : content.button}
        </button>
      </div>
    </form>
  );
}

export function CropCycleManagement({
  organization,
  organizationId,
  permissions,
}: CropCycleManagementProps) {
  const router = useRouter();
  const [cropCycles, setCropCycles] = useState<CropCycle[]>([]);
  const [commodities, setCommodities] = useState<Commodity[]>([]);
  const [cultivationSops, setCultivationSops] = useState<CultivationSop[]>([]);
  const [lands, setLands] = useState<Land[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [query, setQuery] = useState("");
  const [statusFilter, setStatusFilter] = useState<CropCycleStatusFilter>("all");
  const [landFilter, setLandFilter] = useState("");
  const [selectedCycleId, setSelectedCycleId] = useState<string | null>(null);
  const [editor, setEditor] = useState<EditorState | null>(null);
  const [action, setAction] = useState<ActionState | null>(null);
  const [pageError, setPageError] = useState<string | null>(null);
  const [modalError, setModalError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const canRead = permissions.includes("cultivation.read");
  const canWrite = permissions.includes("cultivation.write");

  async function loadData(refresh = false) {
    if (!organizationId || !canRead) {
      return;
    }

    if (refresh) {
      setIsRefreshing(true);
    }
    setPageError(null);

    try {
      const [nextCycles, nextCommodities, nextSops, nextLands] = await Promise.all([
        getCropCycles(organizationId),
        getCommodities(organizationId),
        getCultivationSops(organizationId),
        getLands(organizationId),
      ]);
      setCropCycles(nextCycles);
      setCommodities(nextCommodities);
      setCultivationSops(nextSops);
      setLands(nextLands);
      setSelectedCycleId((current) => current && nextCycles.some((cycle) => cycle.id === current)
        ? current
        : nextCycles[0]?.id ?? null);
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
      if (!organizationId || !canRead) {
        if (!cancelled) {
          setIsLoading(false);
        }
        return;
      }

      try {
        const [nextCycles, nextCommodities, nextSops, nextLands] = await Promise.all([
          getCropCycles(organizationId),
          getCommodities(organizationId),
          getCultivationSops(organizationId),
          getLands(organizationId),
        ]);

        if (!cancelled) {
          setCropCycles(nextCycles);
          setCommodities(nextCommodities);
          setCultivationSops(nextSops);
          setLands(nextLands);
          setSelectedCycleId(nextCycles[0]?.id ?? null);
        }
      } catch (error) {
        if (error instanceof ApiError && error.status === 401) {
          router.replace("/login");
          return;
        }
        if (!cancelled) {
          setPageError(friendlyError(error));
        }
      } finally {
        if (!cancelled) {
          setIsLoading(false);
        }
      }
    }

    void loadInitialData();
    return () => {
      cancelled = true;
    };
  }, [organizationId, canRead, router]);

  useEffect(() => {
    if (!editor && !action) {
      return;
    }

    const originalOverflow = document.body.style.overflow;
    document.body.style.overflow = "hidden";
    function closeWithEscape(event: KeyboardEvent) {
      if (event.key === "Escape" && !isSaving) {
        setEditor(null);
        setAction(null);
        setModalError(null);
      }
    }
    window.addEventListener("keydown", closeWithEscape);
    return () => {
      document.body.style.overflow = originalOverflow;
      window.removeEventListener("keydown", closeWithEscape);
    };
  }, [editor, action, isSaving]);

  const filteredCycles = useMemo(
    () => filterCropCycles(
      cropCycles,
      commodities,
      lands,
      query,
      statusFilter,
      landFilter,
    ),
    [cropCycles, commodities, lands, query, statusFilter, landFilter],
  );
  const selectedCycle = filteredCycles.find((cycle) => cycle.id === selectedCycleId)
    ?? filteredCycles[0]
    ?? null;
  const selectedReferences = selectedCycle
    ? getCycleReferences(selectedCycle, commodities, cultivationSops, lands)
    : null;
  const modalCycleId = editor?.mode === "edit" ? editor.cropCycleId : action?.cropCycleId;
  const modalCycle = modalCycleId
    ? cropCycles.find((cycle) => cycle.id === modalCycleId) ?? null
    : null;
  const plannedCount = cropCycles.filter((cycle) => cycle.status === 1).length;
  const activeCount = cropCycles.filter((cycle) => cycle.status === 2).length;
  const completedCount = cropCycles.filter((cycle) => cycle.status === 3).length;
  const totalActiveArea = cropCycles
    .filter((cycle) => cycle.status === 2)
    .reduce((total, cycle) => total + cycle.plantedAreaInSquareMeters, 0);

  function applyUpdatedCycle(updatedCycle: CropCycle, successMessage: string) {
    setCropCycles((current) => replaceCropCycle(current, updatedCycle));
    setSelectedCycleId(updatedCycle.id);
    setNotice(successMessage);
    setPageError(null);
  }

  async function submitCycle(request: CreateCropCycleRequest) {
    if (!organizationId || !canWrite) {
      return;
    }

    setIsSaving(true);
    setModalError(null);
    try {
      const updatedCycle = editor?.mode === "edit"
        ? await updateCropCyclePlan(organizationId, editor.cropCycleId, {
          name: request.name,
          cultivationSopId: request.cultivationSopId,
          plantedArea: request.plantedArea,
          areaUnit: request.areaUnit,
          plannedStartDate: request.plannedStartDate,
          expectedHarvestDate: request.expectedHarvestDate,
          notes: request.notes,
        })
        : await createCropCycle(organizationId, request);
      applyUpdatedCycle(
        updatedCycle,
        editor?.mode === "edit"
          ? "Rencana budidaya berhasil diperbarui."
          : "Siklus budidaya baru berhasil direncanakan.",
      );
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
    if (!organizationId || !canWrite || !action) {
      return;
    }

    setIsSaving(true);
    setModalError(null);
    try {
      let updatedCycle: CropCycle;
      let successMessage: string;

      if (action.kind === "start") {
        updatedCycle = await startCropCycle(
          organizationId,
          action.cropCycleId,
          { actualStartDate: value },
        );
        successMessage = "Siklus budidaya mulai berjalan.";
      } else if (action.kind === "complete") {
        updatedCycle = await completeCropCycle(
          organizationId,
          action.cropCycleId,
          { actualHarvestDate: value },
        );
        successMessage = "Siklus budidaya selesai dan masuk histori.";
      } else if (action.kind === "cancel") {
        updatedCycle = await cancelCropCycle(
          organizationId,
          action.cropCycleId,
          { cancellationReason: value },
        );
        successMessage = "Siklus budidaya dibatalkan dan tetap disimpan untuk evaluasi.";
      } else {
        updatedCycle = await updateCropCycleNotes(
          organizationId,
          action.cropCycleId,
          { notes: optionalText(value) },
        );
        successMessage = "Catatan siklus berhasil diperbarui.";
      }

      applyUpdatedCycle(updatedCycle, successMessage);
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

  function openEditor(state: EditorState) {
    setModalError(null);
    setEditor(state);
  }

  function openAction(kind: ActionKind, cropCycleId: string) {
    setModalError(null);
    setAction({ kind, cropCycleId });
  }

  if (!organizationId) {
    return (
      <section className={styles.accessState}>
        <Icon name="sprout" />
        <h1>Pilih organisasi terlebih dahulu</h1>
        <p>Siklus budidaya selalu dicatat pada satu organisasi aktif.</p>
      </section>
    );
  }

  if (!canRead) {
    return (
      <section className={styles.accessState}>
        <Icon name="stop" />
        <h1>Akses budidaya tidak tersedia</h1>
        <p>Peran Anda belum memiliki izin <strong>cultivation.read</strong>.</p>
      </section>
    );
  }

  return (
    <section className={styles.cultivationPage}>
      <div className={styles.hero}>
        <div>
          <span className={styles.eyebrow}>Operasional budidaya</span>
          <h1>Siklus Budidaya</h1>
          <p>Rencanakan, jalankan, dan tutup musim tanam {organization?.name ? `untuk ${organization.name}` : "organisasi aktif"} tanpa kehilangan jejak keputusan lapangan.</p>
        </div>
        <div className={styles.heroActions}>
          {!canWrite && <span className={styles.readOnlyBadge}>Mode baca</span>}
          <button className={styles.refreshButton} type="button" disabled={isRefreshing || isLoading} onClick={() => void loadData(true)}>
            <Icon name="refresh" /> {isRefreshing ? "Memuat..." : "Muat ulang"}
          </button>
          {canWrite && (
            <button className={styles.primaryButton} type="button" onClick={() => openEditor({ mode: "create" })}>
              <Icon name="add" /> Buka siklus
            </button>
          )}
        </div>
      </div>

      {notice && (
        <div className={styles.notice} role="status">
          <span><Icon name="check" /></span>
          <strong>{notice}</strong>
          <button type="button" aria-label="Tutup pemberitahuan" onClick={() => setNotice(null)}><Icon name="close" /></button>
        </div>
      )}
      {pageError && <div className={styles.pageError} role="alert">{pageError}</div>}

      <div className={styles.metricGrid}>
        <article className={styles.metricCard}>
          <span>Total siklus</span><strong>{cropCycles.length}</strong><small>{plannedCount} menunggu dimulai</small><i><Icon name="calendar" /></i>
        </article>
        <article className={`${styles.metricCard} ${styles.metricActive}`}>
          <span>Sedang berjalan</span><strong>{activeCount}</strong><small>Memerlukan pencatatan aktivitas</small><i><Icon name="sprout" /></i>
        </article>
        <article className={styles.metricCard}>
          <span>Musim selesai</span><strong>{completedCount}</strong><small>Tersimpan untuk histori lahan</small><i><Icon name="check" /></i>
        </article>
        <article className={styles.metricCard}>
          <span>Luas tanam aktif</span><strong>{totalActiveArea >= 10_000 ? `${(totalActiveArea / 10_000).toLocaleString("id-ID", { maximumFractionDigits: 2 })} ha` : `${totalActiveArea.toLocaleString("id-ID")} m²`}</strong><small>Akumulasi siklus berjalan</small><i><Icon name="field" /></i>
        </article>
      </div>

      <div className={styles.toolbar}>
        <label className={styles.searchField}>
          <Icon name="search" />
          <input value={query} placeholder="Cari kode, siklus, komoditas, lahan, atau petak" aria-label="Cari siklus budidaya" onChange={(event) => setQuery(event.target.value)} />
        </label>
        <label className={styles.filterField}>
          <span>Status</span>
          <select value={statusFilter} onChange={(event) => setStatusFilter(event.target.value === "all" ? "all" : Number(event.target.value) as CropCycleStatusFilter)}>
            <option value="all">Semua status</option>
            <option value={1}>Rencana</option>
            <option value={2}>Berjalan</option>
            <option value={3}>Selesai</option>
            <option value={4}>Dibatalkan</option>
          </select>
        </label>
        <label className={styles.filterField}>
          <span>Lahan</span>
          <select value={landFilter} onChange={(event) => setLandFilter(event.target.value)}>
            <option value="">Semua lahan</option>
            {lands.map((land) => <option value={land.id} key={land.id}>{land.code} · {land.name}</option>)}
          </select>
        </label>
        <span className={styles.resultCount}>{filteredCycles.length} hasil</span>
      </div>

      {isLoading ? (
        <div className={styles.loadingState}><span className="loader" /><p>Memuat siklus budidaya...</p></div>
      ) : cropCycles.length === 0 ? (
        <div className={styles.emptyState}>
          <span><Icon name="sprout" /></span>
          <h2>Belum ada siklus budidaya</h2>
          <p>Buka rencana pertama setelah komoditas, lahan, dan petak siap digunakan.</p>
          {canWrite && <button className={styles.primaryButton} type="button" onClick={() => openEditor({ mode: "create" })}><Icon name="add" /> Buka siklus pertama</button>}
        </div>
      ) : filteredCycles.length === 0 ? (
        <div className={styles.emptyState}>
          <span><Icon name="search" /></span>
          <h2>Tidak ada hasil yang sesuai</h2>
          <p>Ubah kata pencarian atau filter untuk melihat siklus lainnya.</p>
          <button className={styles.secondaryButton} type="button" onClick={() => { setQuery(""); setStatusFilter("all"); setLandFilter(""); }}>Bersihkan filter</button>
        </div>
      ) : (
        <div className={styles.managementGrid}>
          <aside className={styles.cycleList}>
            <div className={styles.listHeader}>
              <div><span className={styles.eyebrow}>Daftar periode</span><h2>{filteredCycles.length} siklus</h2></div>
            </div>
            <div className={styles.cycleCards}>
              {filteredCycles.map((cycle) => {
                const references = getCycleReferences(cycle, commodities, cultivationSops, lands);
                const isSelected = cycle.id === selectedCycle?.id;
                return (
                  <button className={`${styles.cycleCard} ${isSelected ? styles.cycleCardSelected : ""}`} type="button" key={cycle.id} aria-pressed={isSelected} onClick={() => setSelectedCycleId(cycle.id)}>
                    <span className={styles.cardTopline}>
                      <strong>{cycle.code}</strong>
                      <i className={`${styles.statusBadge} ${styles[`status${cycle.status}`]}`}>{cropCycleStatusLabels[cycle.status]}</i>
                    </span>
                    <b>{cycle.name}</b>
                    <span className={styles.cardLocation}><Icon name="field" /> {references.land?.name ?? "Lahan tidak ditemukan"} · {references.plot?.name ?? "Petak tidak ditemukan"}</span>
                    <span className={styles.cardMeta}>
                      <small>{references.commodity?.name ?? "Komoditas tidak ditemukan"}</small>
                      <small>{formatDateOnly(cycle.plannedStartDate)}</small>
                    </span>
                  </button>
                );
              })}
            </div>
          </aside>

          {selectedCycle && selectedReferences && (
            <article className={styles.cycleDetail}>
              <header className={styles.detailHeader}>
                <div className={styles.detailIdentity}>
                  <span className={styles.detailIcon}><Icon name="sprout" /></span>
                  <div>
                    <span className={styles.detailCode}>{selectedCycle.code}</span>
                    <h2>{selectedCycle.name}</h2>
                    <p>{selectedReferences.commodity?.name ?? "Komoditas tidak ditemukan"}</p>
                  </div>
                </div>
                <div className={styles.detailActions}>
                  <span className={`${styles.statusBadge} ${styles[`status${selectedCycle.status}`]}`}>{cropCycleStatusLabels[selectedCycle.status]}</span>
                  {canWrite && selectedCycle.status === 1 && (
                    <>
                      <button className={styles.secondaryButton} type="button" onClick={() => openEditor({ mode: "edit", cropCycleId: selectedCycle.id })}><Icon name="edit" /> Ubah</button>
                      <button className={styles.primaryButton} type="button" onClick={() => openAction("start", selectedCycle.id)}><Icon name="flag" /> Mulai</button>
                    </>
                  )}
                  {canWrite && selectedCycle.status === 2 && (
                    <button className={styles.primaryButton} type="button" onClick={() => openAction("complete", selectedCycle.id)}><Icon name="check" /> Selesaikan</button>
                  )}
                </div>
              </header>

              <div className={styles.timeline}>
                <div className={selectedCycle.status >= 1 ? styles.timelineActive : ""}><span>1</span><strong>Rencana</strong><small>{formatDateOnly(selectedCycle.plannedStartDate)}</small></div>
                <i />
                <div className={selectedCycle.status === 2 || selectedCycle.status === 3 ? styles.timelineActive : ""}><span>2</span><strong>Mulai</strong><small>{formatDateOnly(selectedCycle.actualStartDate)}</small></div>
                <i />
                <div className={selectedCycle.status === 3 ? styles.timelineActive : ""}><span>3</span><strong>Panen akhir</strong><small>{formatDateOnly(selectedCycle.actualHarvestDate ?? selectedCycle.expectedHarvestDate)}</small></div>
              </div>

              <section className={styles.detailSection}>
                <div className={styles.sectionHeader}><div><span className={styles.eyebrow}>Fondasi musim</span><h3>Lokasi dan rencana</h3></div><span>{getPlannedDurationDays(selectedCycle)} hari rencana</span></div>
                <div className={styles.infoGrid}>
                  <div><small>Lahan</small><strong>{selectedReferences.land?.name ?? "—"}</strong><span>{selectedReferences.land?.code ?? "Referensi tidak ditemukan"}</span></div>
                  <div><small>Petak</small><strong>{selectedReferences.plot?.name ?? "—"}</strong><span>{selectedReferences.plot?.code ?? "Referensi tidak ditemukan"}</span></div>
                  <div><small>Luas tanam</small><strong>{formatArea(selectedCycle.plantedArea, selectedCycle.areaUnit)}</strong><span>{selectedCycle.plantedAreaInSquareMeters.toLocaleString("id-ID")} m²</span></div>
                  <div><small>SOP budidaya</small><strong>{selectedReferences.cultivationSop?.name ?? "Tanpa SOP"}</strong><span>{selectedReferences.cultivationSop ? `${selectedReferences.cultivationSop.steps.length} langkah` : "Pelaksanaan manual"}</span></div>
                </div>
              </section>

              <section className={styles.detailSection}>
                <div className={styles.sectionHeader}><div><span className={styles.eyebrow}>Jadwal</span><h3>Tanggal penting</h3></div></div>
                <div className={styles.dateGrid}>
                  <div><Icon name="calendar" /><span><small>Mulai rencana</small><strong>{formatDateOnly(selectedCycle.plannedStartDate)}</strong></span></div>
                  <div><Icon name="flag" /><span><small>Mulai aktual</small><strong>{formatDateOnly(selectedCycle.actualStartDate)}</strong></span></div>
                  <div><Icon name="calendar" /><span><small>Perkiraan panen</small><strong>{formatDateOnly(selectedCycle.expectedHarvestDate)}</strong></span></div>
                  <div><Icon name="check" /><span><small>Panen aktual</small><strong>{formatDateOnly(selectedCycle.actualHarvestDate)}</strong></span></div>
                </div>
              </section>

              <section className={styles.detailSection}>
                <div className={styles.sectionHeader}>
                  <div><span className={styles.eyebrow}>Konteks lapangan</span><h3>Catatan dan keputusan</h3></div>
                  {canWrite && (selectedCycle.status === 1 || selectedCycle.status === 2) && <button className={styles.textAction} type="button" onClick={() => openAction("notes", selectedCycle.id)}><Icon name="notes" /> Ubah catatan</button>}
                </div>
                <div className={styles.notesBox}>{selectedCycle.notes || "Belum ada catatan untuk siklus ini."}</div>
                {selectedCycle.status === 4 && <div className={styles.cancellationBox}><strong>Alasan pembatalan</strong><p>{selectedCycle.cancellationReason}</p></div>}
              </section>

              {canWrite && (selectedCycle.status === 1 || selectedCycle.status === 2) && (
                <footer className={styles.detailFooter}>
                  <div><strong>Perlu menghentikan rencana?</strong><span>Pembatalan disimpan permanen sebagai bahan evaluasi.</span></div>
                  <button className={styles.dangerTextButton} type="button" onClick={() => openAction("cancel", selectedCycle.id)}><Icon name="stop" /> Batalkan siklus</button>
                </footer>
              )}
            </article>
          )}
        </div>
      )}

      {editor && (editor.mode === "create" || modalCycle) && (
        <div className={styles.modalBackdrop} role="presentation" onMouseDown={(event) => { if (event.target === event.currentTarget && !isSaving) setEditor(null); }}>
          <div className={styles.modalPanel} role="dialog" aria-modal="true" aria-label={editor.mode === "create" ? "Buka siklus budidaya" : "Ubah rencana budidaya"}>
            <CycleEditor
              key={editor.mode === "create" ? "new-cycle" : modalCycle?.id}
              cycle={editor.mode === "edit" ? modalCycle : null}
              commodities={commodities}
              cultivationSops={cultivationSops}
              lands={lands}
              isSaving={isSaving}
              apiError={modalError}
              onCancel={() => { setEditor(null); setModalError(null); }}
              onSubmit={submitCycle}
            />
          </div>
        </div>
      )}

      {action && modalCycle && (
        <div className={styles.modalBackdrop} role="presentation" onMouseDown={(event) => { if (event.target === event.currentTarget && !isSaving) setAction(null); }}>
          <CycleActionDialog
            key={`${action.kind}-${modalCycle.id}`}
            cycle={modalCycle}
            kind={action.kind}
            isSaving={isSaving}
            apiError={modalError}
            onCancel={() => { setAction(null); setModalError(null); }}
            onSubmit={submitAction}
          />
        </div>
      )}
    </section>
  );
}
