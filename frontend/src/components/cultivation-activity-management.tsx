"use client";

import { useEffect, useMemo, useState, type FormEvent } from "react";
import { useRouter } from "next/navigation";
import {
  addCultivationActivityResource,
  ApiError,
  cancelCultivationActivity,
  completeCultivationActivity,
  createCultivationActivity,
  getCropCycles,
  getCultivationActivities,
  getCultivationSops,
  removeCultivationActivityResource,
  startCultivationActivity,
  updateCultivationActivityNotes,
  updateCultivationActivityPlan,
  updateCultivationActivityResource,
} from "@/lib/api/client";
import type {
  AddCultivationActivityResourceRequest,
  CompleteCultivationActivityRequest,
  CreateCultivationActivityRequest,
  CropCycle,
  CultivationActivity,
  CultivationActivityResource,
  CultivationActivityType,
  CultivationResourceType,
  CultivationSop,
  Organization,
  SopComplianceStatus,
} from "@/lib/api/contracts";
import {
  activityDraftFrom,
  activityStatusLabels,
  activityTypeLabels,
  complianceLabels,
  filterActivities,
  formatActivityDate,
  formatCurrency,
  formatQuantity,
  optionalActivityText,
  parseDecimal,
  resourceDraftFrom,
  resourceTypeLabels,
  validateActivityDraft,
  validateResourceDraft,
  type ActivityDraft,
  type ActivityStatusFilter,
  type ActivityTypeFilter,
  type ResourceDraft,
} from "@/lib/cultivation/activity-management";
import styles from "./cultivation-activity-management.module.css";

type Props = {
  organization: Organization | null;
  organizationId: string | null;
  permissions: string[];
};

type EditorState =
  | { kind: "activity"; activityId: string | null }
  | { kind: "resource"; activityId: string; resourceId: string | null };

type ActionState =
  | { kind: "start" | "complete" | "cancel" | "notes"; activityId: string }
  | { kind: "remove-resource"; activityId: string; resourceId: string };

type IconName =
  | "activity" | "add" | "arrow" | "calendar" | "check" | "close"
  | "cost" | "edit" | "issue" | "notes" | "refresh" | "resource"
  | "search" | "sop" | "start" | "stop" | "trash";

const iconPaths: Record<IconName, string> = {
  activity: "M4 19h16M6 15l3-4 3 2 5-7 2 2M7 5h3M5 7V4h3",
  add: "M12 5v14M5 12h14",
  arrow: "m15 18-6-6 6-6",
  calendar: "M6 3v3m12-3v3M4 9h16M5 5h14a1 1 0 0 1 1 1v14H4V6a1 1 0 0 1 1-1Z",
  check: "m5 12 4 4L19 6",
  close: "m6 6 12 12M18 6 6 18",
  cost: "M12 2v20m5-16H9.5a3.5 3.5 0 0 0 0 7h5a3.5 3.5 0 0 1 0 7H6",
  edit: "m4 20 4.5-1 10-10a2.1 2.1 0 0 0-3-3l-10 10L4 20Zm10-12 3 3",
  issue: "M12 9v4m0 4h.01M10.3 4.7 2.8 18a2 2 0 0 0 1.7 3h15a2 2 0 0 0 1.7-3L14.7 4.7a2.5 2.5 0 0 0-4.4 0Z",
  notes: "M5 4h14v16H5V4Zm4 4h6m-6 4h6m-6 4h4",
  refresh: "M20 6v5h-5M4 18v-5h5m10-2a7 7 0 0 0-12-4L4 11m16 2-3 4a7 7 0 0 1-12-4",
  resource: "M5 8 12 4l7 4-7 4-7-4Zm0 4 7 4 7-4m-14 4 7 4 7-4",
  search: "m21 21-4.35-4.35m2.35-5.65a8 8 0 1 1-16 0 8 8 0 0 1 16 0Z",
  sop: "M6 3h12v18H6V3Zm4 5h4m-4 4h4m-4 4h3",
  start: "M8 5v14l11-7L8 5Z",
  stop: "M6 6h12v12H6V6Z",
  trash: "M4 7h16m-10 4v6m4-6v6M9 7l1-3h4l1 3m-9 0 1 14h10l1-14",
};

function Icon({ name }: { name: IconName }) {
  return <svg viewBox="0 0 24 24" aria-hidden="true"><path d={iconPaths[name]} /></svg>;
}

function localToday(): string {
  const now = new Date();
  return new Date(now.getTime() - now.getTimezoneOffset() * 60_000)
    .toISOString().slice(0, 10);
}

function replaceActivity(
  activities: CultivationActivity[],
  updated: CultivationActivity,
): CultivationActivity[] {
  return activities.some((item) => item.id === updated.id)
    ? activities.map((item) => item.id === updated.id ? updated : item)
    : [...activities, updated];
}

function friendlyError(error: unknown): string {
  if (!(error instanceof ApiError)) {
    return error instanceof Error ? error.message : "Permintaan tidak dapat diselesaikan.";
  }

  switch (error.problem?.code) {
    case "CultivationActivities.CodeAlreadyExists":
      return "Kode aktivitas sudah digunakan dalam siklus ini.";
    case "CultivationActivities.InvalidStatusTransition":
      return "Tindakan tidak sesuai dengan status aktivitas saat ini. Muat ulang lalu periksa kembali.";
    case "CultivationActivities.PlannedDateAfterExpectedHarvest":
      return "Tanggal aktivitas tidak boleh setelah perkiraan panen siklus.";
    case "CultivationActivities.SopStepDoesNotBelongToSop":
    case "CultivationActivities.SopCommodityMismatch":
      return "Langkah SOP tidak sesuai dengan SOP atau komoditas siklus ini.";
    case "CultivationActivities.SopInactive":
      return "SOP budidaya sudah tidak aktif. Gunakan aktivitas tanpa SOP atau pilih siklus lain.";
    case "CropCycles.TerminalStatus":
      return "Aktivitas baru tidak dapat ditambahkan pada siklus yang sudah selesai atau dibatalkan.";
    default:
      return error.message;
  }
}

function ActivityEditor({
  activity,
  cycle,
  cultivationSops,
  isSaving,
  apiError,
  onCancel,
  onSubmit,
}: {
  activity: CultivationActivity | null;
  cycle: CropCycle;
  cultivationSops: CultivationSop[];
  isSaving: boolean;
  apiError: string | null;
  onCancel: () => void;
  onSubmit: (request: CreateCultivationActivityRequest) => Promise<void>;
}) {
  const [draft, setDraft] = useState<ActivityDraft>(() => activityDraftFrom(activity));
  const [errors, setErrors] = useState<string[]>([]);
  const isCreate = activity === null;
  const cycleSop = cultivationSops.find((item) => item.id === cycle.cultivationSopId) ?? null;

  function updateDraft<Key extends keyof ActivityDraft>(key: Key, value: ActivityDraft[Key]) {
    setDraft((current) => ({ ...current, [key]: value }));
  }

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const nextErrors = validateActivityDraft(draft, isCreate, cycle, cultivationSops);
    setErrors(nextErrors);
    if (nextErrors.length > 0) return;

    void onSubmit({
      code: draft.code.trim().toUpperCase(),
      name: draft.name.trim(),
      activityType: draft.activityType,
      plannedDate: draft.plannedDate,
      cultivationSopId: draft.cultivationSopStepId ? cycleSop?.id ?? null : null,
      cultivationSopStepId: draft.cultivationSopStepId || null,
      notes: optionalActivityText(draft.notes),
    });
  }

  return (
    <form className={styles.editorForm} onSubmit={submit} noValidate>
      <header className={styles.editorHeader}>
        <span className={styles.editorIcon}><Icon name="activity" /></span>
        <div><span className={styles.eyebrow}>{isCreate ? "Aktivitas baru" : activity.code}</span><h2>{isCreate ? "Rencanakan pekerjaan lapangan" : "Ubah rencana aktivitas"}</h2><p>Catat pekerjaan secara terpisah agar penggunaan sumber daya, kendala, dan hasilnya dapat dievaluasi.</p></div>
        <button className={styles.iconButton} type="button" aria-label="Tutup formulir" disabled={isSaving} onClick={onCancel}><Icon name="close" /></button>
      </header>

      {(errors.length > 0 || apiError) && <div className={styles.formAlert} role="alert"><strong>Periksa kembali data berikut:</strong><ul>{errors.map((error) => <li key={error}>{error}</li>)}{apiError && <li>{apiError}</li>}</ul></div>}

      <fieldset disabled={isSaving}>
        <div className={styles.formGrid}>
          <label className={styles.field}><span>Kode aktivitas <em>*</em></span><input value={draft.code} maxLength={40} disabled={!isCreate} placeholder="Contoh: PUPUK-01" onChange={(event) => updateDraft("code", event.target.value)} /></label>
          <label className={`${styles.field} ${styles.fieldWide}`}><span>Nama aktivitas <em>*</em></span><input value={draft.name} maxLength={150} placeholder="Contoh: Pemupukan susulan pertama" onChange={(event) => updateDraft("name", event.target.value)} /></label>
          <label className={styles.field}><span>Jenis pekerjaan <em>*</em></span><select value={draft.activityType} onChange={(event) => updateDraft("activityType", Number(event.target.value) as CultivationActivityType)}>{Object.entries(activityTypeLabels).map(([value, label]) => <option value={value} key={value}>{label}</option>)}</select></label>
          <label className={styles.field}><span>Tanggal rencana <em>*</em></span><input type="date" value={draft.plannedDate} max={cycle.expectedHarvestDate} onChange={(event) => updateDraft("plannedDate", event.target.value)} /></label>
          <label className={`${styles.field} ${styles.fieldFull}`}><span>Langkah SOP</span><select value={draft.cultivationSopStepId} disabled={!isCreate || !cycleSop} onChange={(event) => updateDraft("cultivationSopStepId", event.target.value)}><option value="">Aktivitas di luar SOP</option>{cycleSop?.steps.slice().sort((a, b) => a.sequence - b.sequence).map((step) => <option value={step.id} key={step.id}>{step.sequence}. {step.name} · H+{step.plannedDayOffset}{step.isRequired ? " · wajib" : " · opsional"}</option>)}</select><small>{cycleSop ? "Langkah SOP disimpan sebagai snapshot dan tidak dapat diganti setelah aktivitas dibuat." : "Siklus ini tidak menggunakan SOP budidaya."}</small></label>
          <label className={`${styles.field} ${styles.fieldFull}`}><span>Catatan rencana</span><textarea value={draft.notes} maxLength={1000} rows={4} placeholder="Target, dosis, metode, kondisi awal, atau instruksi lapangan" onChange={(event) => updateDraft("notes", event.target.value)} /></label>
        </div>
      </fieldset>

      <footer className={styles.formActions}><button className={styles.secondaryButton} type="button" disabled={isSaving} onClick={onCancel}>Batal</button><button className={styles.primaryButton} type="submit" disabled={isSaving}>{isSaving ? "Menyimpan..." : isCreate ? "Simpan aktivitas" : "Simpan perubahan"}</button></footer>
    </form>
  );
}

function ResourceEditor({
  resource,
  activity,
  isSaving,
  apiError,
  onCancel,
  onSubmit,
}: {
  resource: CultivationActivityResource | null;
  activity: CultivationActivity;
  isSaving: boolean;
  apiError: string | null;
  onCancel: () => void;
  onSubmit: (request: AddCultivationActivityResourceRequest) => Promise<void>;
}) {
  const [draft, setDraft] = useState<ResourceDraft>(() => resourceDraftFrom(resource));
  const [errors, setErrors] = useState<string[]>([]);
  const isCreate = resource === null;
  const quantity = parseDecimal(draft.quantity);
  const unitCost = parseDecimal(draft.unitCost, true);

  function updateDraft<Key extends keyof ResourceDraft>(key: Key, value: ResourceDraft[Key]) {
    setDraft((current) => ({ ...current, [key]: value }));
  }

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const nextErrors = validateResourceDraft(draft);
    setErrors(nextErrors);
    if (nextErrors.length > 0 || quantity === null || unitCost === null) return;

    void onSubmit({
      resourceType: draft.resourceType,
      description: draft.description.trim(),
      quantity,
      unit: draft.unit.trim(),
      unitCost,
      notes: optionalActivityText(draft.notes),
    });
  }

  return (
    <form className={styles.actionDialog} onSubmit={submit} noValidate>
      <div className={styles.actionIcon}><Icon name="resource" /></div>
      <span className={styles.eyebrow}>{isCreate ? "Sumber daya baru" : resourceTypeLabels[resource.resourceType]}</span>
      <h2>{isCreate ? "Catat pemakaian sumber daya" : "Perbarui sumber daya"}</h2>
      <p>Nilai biaya ini langsung menjadi biaya aktual aktivitas dan bahan perhitungan laba siklus.</p>
      <div className={styles.contextBox}><strong>{activity.code}</strong><span>{activity.name}</span></div>
      {(errors.length > 0 || apiError) && <div className={styles.formAlert} role="alert"><ul>{errors.map((error) => <li key={error}>{error}</li>)}{apiError && <li>{apiError}</li>}</ul></div>}
      <fieldset disabled={isSaving} className={styles.dialogFields}>
        <label className={styles.field}><span>Kategori <em>*</em></span><select value={draft.resourceType} disabled={!isCreate} onChange={(event) => updateDraft("resourceType", Number(event.target.value) as CultivationResourceType)}>{Object.entries(resourceTypeLabels).map(([value, label]) => <option value={value} key={value}>{label}</option>)}</select></label>
        <label className={styles.field}><span>Deskripsi <em>*</em></span><input value={draft.description} maxLength={250} placeholder="Contoh: Pupuk NPK 16-16-16" onChange={(event) => updateDraft("description", event.target.value)} /></label>
        <div className={styles.inlineFields}><label className={styles.field}><span>Jumlah <em>*</em></span><input value={draft.quantity} inputMode="decimal" placeholder="50" onChange={(event) => updateDraft("quantity", event.target.value)} /></label><label className={styles.field}><span>Satuan <em>*</em></span><input value={draft.unit} maxLength={50} placeholder="kg / HOK / jam" onChange={(event) => updateDraft("unit", event.target.value)} /></label></div>
        <label className={styles.field}><span>Biaya per satuan <em>*</em></span><input value={draft.unitCost} inputMode="decimal" placeholder="8000" onChange={(event) => updateDraft("unitCost", event.target.value)} /><small>Isi 0 untuk tenaga keluarga atau aset sendiri yang belum dibebankan.</small></label>
        <div className={styles.costPreview}><span>Perkiraan total</span><strong>{formatCurrency((quantity ?? 0) * (unitCost ?? 0))}</strong></div>
        <label className={styles.field}><span>Catatan</span><textarea value={draft.notes} maxLength={500} rows={3} placeholder="Merek, pemasok, kondisi alat, atau keterangan lain" onChange={(event) => updateDraft("notes", event.target.value)} /></label>
      </fieldset>
      <footer className={styles.actionButtons}><button className={styles.secondaryButton} type="button" disabled={isSaving} onClick={onCancel}>Kembali</button><button className={styles.primaryButton} type="submit" disabled={isSaving}>{isSaving ? "Menyimpan..." : isCreate ? "Tambahkan" : "Simpan perubahan"}</button></footer>
    </form>
  );
}

function ActivityActionDialog({
  action,
  activity,
  resource,
  isSaving,
  apiError,
  onCancel,
  onSubmit,
}: {
  action: ActionState;
  activity: CultivationActivity;
  resource: CultivationActivityResource | null;
  isSaving: boolean;
  apiError: string | null;
  onCancel: () => void;
  onSubmit: (payload: string | CompleteCultivationActivityRequest | { notes: string | null; issueNotes: string | null }) => Promise<void>;
}) {
  const [date, setDate] = useState(localToday());
  const [reason, setReason] = useState("");
  const [notes, setNotes] = useState(activity.notes ?? "");
  const [issueNotes, setIssueNotes] = useState(activity.issueNotes ?? "");
  const [outcome, setOutcome] = useState(activity.outcome ?? "");
  const [compliance, setCompliance] = useState<SopComplianceStatus>(activity.cultivationSopStepId ? 3 : 1);
  const [deviationReason, setDeviationReason] = useState("");
  const [error, setError] = useState<string | null>(null);
  const isRemove = action.kind === "remove-resource";

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    let nextError: string | null = null;
    if (action.kind === "start" && !date) nextError = "Tanggal mulai aktual wajib diisi.";
    if (action.kind === "complete" && (!date || Boolean(activity.actualStartDate && date < activity.actualStartDate))) nextError = "Tanggal selesai wajib diisi dan tidak boleh sebelum tanggal mulai.";
    if (action.kind === "complete" && outcome.trim().length > 1000) nextError = "Hasil pekerjaan maksimal 1000 karakter.";
    if ((action.kind === "complete" || action.kind === "notes") && issueNotes.trim().length > 1000) nextError = "Catatan kendala maksimal 1000 karakter.";
    if (action.kind === "complete" && compliance === 4 && !deviationReason.trim()) nextError = "Alasan penyimpangan SOP wajib diisi.";
    if (action.kind === "cancel" && !reason.trim()) nextError = "Alasan pembatalan wajib diisi.";
    if (action.kind === "cancel" && reason.trim().length > 500) nextError = "Alasan pembatalan maksimal 500 karakter.";
    if (action.kind === "notes" && notes.trim().length > 1000) nextError = "Catatan aktivitas maksimal 1000 karakter.";
    setError(nextError);
    if (nextError) return;

    if (action.kind === "start") void onSubmit(date);
    else if (action.kind === "cancel") void onSubmit(reason.trim());
    else if (action.kind === "notes") void onSubmit({ notes: optionalActivityText(notes), issueNotes: optionalActivityText(issueNotes) });
    else if (action.kind === "complete") void onSubmit({ actualCompletionDate: date, outcome: optionalActivityText(outcome), issueNotes: optionalActivityText(issueNotes), sopComplianceStatus: compliance, deviationReason: compliance === 4 ? optionalActivityText(deviationReason) : null });
    else void onSubmit(resource?.id ?? "");
  }

  const content = action.kind === "start"
    ? { icon: "start" as IconName, eyebrow: "Mulai pelaksanaan", title: "Mulai aktivitas", description: "Tanggal aktual membuka pencatatan pelaksanaan dan penggunaan sumber daya." }
    : action.kind === "complete"
      ? { icon: "check" as IconName, eyebrow: "Catat hasil lapangan", title: "Selesaikan aktivitas", description: "Simpan hasil, kendala, dan kesesuaian SOP. Setelah selesai, catatan menjadi histori tetap." }
      : action.kind === "cancel"
        ? { icon: "stop" as IconName, eyebrow: "Hentikan pekerjaan", title: "Batalkan aktivitas", description: "Pembatalan tetap disimpan agar alasan keputusan dapat dievaluasi." }
        : action.kind === "notes"
          ? { icon: "notes" as IconName, eyebrow: "Catatan operasional", title: "Perbarui catatan dan kendala", description: "Tambahkan kondisi lapangan selama aktivitas masih dapat berubah." }
          : { icon: "trash" as IconName, eyebrow: "Hapus sumber daya", title: "Hapus baris biaya?", description: "Baris ini akan dikeluarkan dari biaya aktual aktivitas." };

  return (
    <form className={styles.actionDialog} onSubmit={submit} noValidate>
      <div className={styles.actionIcon}><Icon name={content.icon} /></div><span className={styles.eyebrow}>{content.eyebrow}</span><h2>{content.title}</h2><p>{content.description}</p>
      <div className={styles.contextBox}><strong>{activity.code}</strong><span>{isRemove && resource ? `${resourceTypeLabels[resource.resourceType]} · ${resource.description} · ${formatCurrency(resource.totalCost)}` : activity.name}</span></div>
      {(error || apiError) && <div className={styles.formAlert} role="alert">{error ?? apiError}</div>}
      <fieldset disabled={isSaving} className={styles.dialogFields}>
        {action.kind === "start" && <label className={styles.field}><span>Tanggal mulai aktual <em>*</em></span><input type="date" value={date} max={activity.plannedDate > localToday() ? undefined : localToday()} onChange={(event) => setDate(event.target.value)} /></label>}
        {action.kind === "cancel" && <label className={styles.field}><span>Alasan pembatalan <em>*</em></span><textarea value={reason} maxLength={500} rows={5} placeholder="Jelaskan mengapa pekerjaan tidak dilanjutkan" onChange={(event) => setReason(event.target.value)} /></label>}
        {action.kind === "notes" && <><label className={styles.field}><span>Catatan aktivitas</span><textarea value={notes} maxLength={1000} rows={4} placeholder="Kondisi, instruksi, atau tindak lanjut" onChange={(event) => setNotes(event.target.value)} /></label><label className={styles.field}><span>Kendala lapangan</span><textarea value={issueNotes} maxLength={1000} rows={4} placeholder="Gejala, hambatan, atau risiko yang ditemukan" onChange={(event) => setIssueNotes(event.target.value)} /></label></>}
        {action.kind === "complete" && <><label className={styles.field}><span>Tanggal selesai aktual <em>*</em></span><input type="date" value={date} min={activity.actualStartDate ?? undefined} onChange={(event) => setDate(event.target.value)} /></label><label className={styles.field}><span>Hasil pekerjaan</span><textarea value={outcome} maxLength={1000} rows={3} placeholder="Hasil yang dicapai atau kondisi setelah pekerjaan" onChange={(event) => setOutcome(event.target.value)} /></label><label className={styles.field}><span>Kendala lapangan</span><textarea value={issueNotes} maxLength={1000} rows={3} placeholder="Masalah yang muncul selama pekerjaan" onChange={(event) => setIssueNotes(event.target.value)} /></label>{activity.cultivationSopStepId ? <label className={styles.field}><span>Kesesuaian SOP <em>*</em></span><select value={compliance} onChange={(event) => setCompliance(Number(event.target.value) as SopComplianceStatus)}><option value={3}>Sesuai SOP</option><option value={4}>Menyimpang dari SOP</option></select></label> : <div className={styles.infoNote}>Aktivitas ini tidak terhubung ke langkah SOP, sehingga statusnya otomatis “Tidak berlaku”.</div>}{compliance === 4 && <label className={styles.field}><span>Alasan penyimpangan <em>*</em></span><textarea value={deviationReason} maxLength={500} rows={3} placeholder="Jelaskan perubahan dosis, waktu, metode, atau keputusan lain" onChange={(event) => setDeviationReason(event.target.value)} /></label>}</>}
      </fieldset>
      <footer className={styles.actionButtons}><button className={styles.secondaryButton} type="button" disabled={isSaving} onClick={onCancel}>Kembali</button><button className={(action.kind === "cancel" || isRemove) ? styles.dangerButton : styles.primaryButton} type="submit" disabled={isSaving}>{isSaving ? "Memproses..." : action.kind === "start" ? "Mulai aktivitas" : action.kind === "complete" ? "Selesaikan" : action.kind === "cancel" ? "Batalkan aktivitas" : action.kind === "notes" ? "Simpan catatan" : "Hapus sumber daya"}</button></footer>
    </form>
  );
}

export function CultivationActivityManagement({ organization, organizationId, permissions }: Props) {
  const router = useRouter();
  const [cropCycles, setCropCycles] = useState<CropCycle[]>([]);
  const [cultivationSops, setCultivationSops] = useState<CultivationSop[]>([]);
  const [activities, setActivities] = useState<CultivationActivity[]>([]);
  const [selectedCycleId, setSelectedCycleId] = useState("");
  const [selectedActivityId, setSelectedActivityId] = useState<string | null>(null);
  const [query, setQuery] = useState("");
  const [statusFilter, setStatusFilter] = useState<ActivityStatusFilter>("all");
  const [typeFilter, setTypeFilter] = useState<ActivityTypeFilter>("all");
  const [editor, setEditor] = useState<EditorState | null>(null);
  const [action, setAction] = useState<ActionState | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isActivityLoading, setIsActivityLoading] = useState(false);
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [pageError, setPageError] = useState<string | null>(null);
  const [modalError, setModalError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const canRead = permissions.includes("cultivation.read");
  const canWrite = permissions.includes("cultivation.write");

  const selectedCycle = cropCycles.find((item) => item.id === selectedCycleId) ?? null;
  const filteredActivities = useMemo(() => filterActivities(activities, query, statusFilter, typeFilter), [activities, query, statusFilter, typeFilter]);
  const selectedActivity = filteredActivities.find((item) => item.id === selectedActivityId) ?? filteredActivities[0] ?? null;
  const modalActivityId = editor?.kind === "activity" ? editor.activityId : editor?.activityId ?? action?.activityId;
  const modalActivity = modalActivityId ? activities.find((item) => item.id === modalActivityId) ?? null : null;
  const modalResourceId = editor?.kind === "resource" ? editor.resourceId : action?.kind === "remove-resource" ? action.resourceId : null;
  const modalResource = modalActivity && modalResourceId ? modalActivity.resources.find((item) => item.id === modalResourceId) ?? null : null;

  useEffect(() => {
    let cancelled = false;
    async function loadReferences() {
      if (!organizationId || !canRead) { if (!cancelled) setIsLoading(false); return; }
      try {
        const [nextCycles, nextSops] = await Promise.all([getCropCycles(organizationId), getCultivationSops(organizationId)]);
        if (!cancelled) {
          setCropCycles(nextCycles);
          setCultivationSops(nextSops);
          const preferred = nextCycles.find((item) => item.status === 2) ?? nextCycles.find((item) => item.status === 1) ?? nextCycles[0];
          setSelectedCycleId(preferred?.id ?? "");
        }
      } catch (error) {
        if (error instanceof ApiError && error.status === 401) { router.replace("/login"); return; }
        if (!cancelled) setPageError(friendlyError(error));
      } finally { if (!cancelled) setIsLoading(false); }
    }
    void loadReferences();
    return () => { cancelled = true; };
  }, [organizationId, canRead, router]);

  useEffect(() => {
    let cancelled = false;
    async function loadActivities() {
      if (!organizationId || !selectedCycleId || !canRead) { if (!cancelled) setActivities([]); return; }
      setIsActivityLoading(true);
      setPageError(null);
      try {
        const next = await getCultivationActivities(organizationId, selectedCycleId);
        if (!cancelled) { setActivities(next); setSelectedActivityId(next[0]?.id ?? null); }
      } catch (error) {
        if (error instanceof ApiError && error.status === 401) { router.replace("/login"); return; }
        if (!cancelled) setPageError(friendlyError(error));
      } finally { if (!cancelled) setIsActivityLoading(false); }
    }
    void loadActivities();
    return () => { cancelled = true; };
  }, [organizationId, selectedCycleId, canRead, router]);

  useEffect(() => {
    if (!editor && !action) return;
    const originalOverflow = document.body.style.overflow;
    document.body.style.overflow = "hidden";
    function close(event: KeyboardEvent) { if (event.key === "Escape" && !isSaving) { setEditor(null); setAction(null); setModalError(null); } }
    window.addEventListener("keydown", close);
    return () => { document.body.style.overflow = originalOverflow; window.removeEventListener("keydown", close); };
  }, [editor, action, isSaving]);

  async function refreshActivities() {
    if (!organizationId || !selectedCycleId) return;
    setIsRefreshing(true); setPageError(null);
    try {
      const next = await getCultivationActivities(organizationId, selectedCycleId);
      setActivities(next);
      setSelectedActivityId((current) => current && next.some((item) => item.id === current) ? current : next[0]?.id ?? null);
    } catch (error) { setPageError(friendlyError(error)); }
    finally { setIsRefreshing(false); }
  }

  function applyUpdatedActivity(updated: CultivationActivity, message: string) {
    setActivities((current) => replaceActivity(current, updated));
    setSelectedActivityId(updated.id); setNotice(message); setPageError(null);
  }

  async function submitActivity(request: CreateCultivationActivityRequest) {
    if (!organizationId || !selectedCycle || !canWrite || editor?.kind !== "activity") return;
    setIsSaving(true); setModalError(null);
    try {
      const updated = editor.activityId
        ? await updateCultivationActivityPlan(organizationId, selectedCycle.id, editor.activityId, { name: request.name, activityType: request.activityType, plannedDate: request.plannedDate, notes: request.notes })
        : await createCultivationActivity(organizationId, selectedCycle.id, request);
      applyUpdatedActivity(updated, editor.activityId ? "Rencana aktivitas berhasil diperbarui." : "Aktivitas baru berhasil direncanakan.");
      setEditor(null);
    } catch (error) {
      if (error instanceof ApiError && error.status === 401) { router.replace("/login"); return; }
      setModalError(friendlyError(error));
    } finally { setIsSaving(false); }
  }

  async function submitResource(request: AddCultivationActivityResourceRequest) {
    if (!organizationId || !selectedCycle || !canWrite || editor?.kind !== "resource") return;
    setIsSaving(true); setModalError(null);
    try {
      const updated = editor.resourceId
        ? await updateCultivationActivityResource(organizationId, selectedCycle.id, editor.activityId, editor.resourceId, { description: request.description, quantity: request.quantity, unit: request.unit, unitCost: request.unitCost, notes: request.notes })
        : await addCultivationActivityResource(organizationId, selectedCycle.id, editor.activityId, request);
      applyUpdatedActivity(updated, editor.resourceId ? "Sumber daya berhasil diperbarui." : "Sumber daya dan biaya berhasil ditambahkan.");
      setEditor(null);
    } catch (error) { setModalError(friendlyError(error)); }
    finally { setIsSaving(false); }
  }

  async function submitAction(payload: string | CompleteCultivationActivityRequest | { notes: string | null; issueNotes: string | null }) {
    if (!organizationId || !selectedCycle || !canWrite || !action) return;
    setIsSaving(true); setModalError(null);
    try {
      let updated: CultivationActivity;
      let message: string;
      if (action.kind === "start") { updated = await startCultivationActivity(organizationId, selectedCycle.id, action.activityId, { actualStartDate: payload as string }); message = "Aktivitas mulai berjalan."; }
      else if (action.kind === "complete") { updated = await completeCultivationActivity(organizationId, selectedCycle.id, action.activityId, payload as CompleteCultivationActivityRequest); message = "Aktivitas selesai dan menjadi histori tetap."; }
      else if (action.kind === "cancel") { updated = await cancelCultivationActivity(organizationId, selectedCycle.id, action.activityId, { cancellationReason: payload as string }); message = "Aktivitas dibatalkan dan alasannya tersimpan."; }
      else if (action.kind === "notes") { updated = await updateCultivationActivityNotes(organizationId, selectedCycle.id, action.activityId, payload as { notes: string | null; issueNotes: string | null }); message = "Catatan aktivitas berhasil diperbarui."; }
      else if (action.kind === "remove-resource") { updated = await removeCultivationActivityResource(organizationId, selectedCycle.id, action.activityId, action.resourceId); message = "Sumber daya dihapus dari biaya aktivitas."; }
      else { return; }
      applyUpdatedActivity(updated, message); setAction(null);
    } catch (error) { setModalError(friendlyError(error)); }
    finally { setIsSaving(false); }
  }

  if (!organizationId) return <section className={styles.accessState}><Icon name="activity" /><h1>Pilih organisasi terlebih dahulu</h1><p>Aktivitas budidaya selalu dicatat pada satu organisasi aktif.</p></section>;
  if (!canRead) return <section className={styles.accessState}><Icon name="stop" /><h1>Akses aktivitas tidak tersedia</h1><p>Peran Anda belum memiliki izin <strong>cultivation.read</strong>.</p></section>;

  const plannedCount = activities.filter((item) => item.status === 1).length;
  const inProgressCount = activities.filter((item) => item.status === 2).length;
  const completedCount = activities.filter((item) => item.status === 3).length;
  const totalCost = activities.reduce((sum, item) => sum + item.totalActualCost, 0);
  const isCycleMutable = selectedCycle?.status === 1 || selectedCycle?.status === 2;

  return (
    <section className={styles.activityPage}>
      <div className={styles.hero}>
        <div><button className={styles.backButton} type="button" onClick={() => router.push("/cultivation")}><Icon name="arrow" /> Siklus Budidaya</button><span className={styles.eyebrow}>Pencatatan lapangan</span><h1>Aktivitas &amp; Sumber Daya</h1><p>Catat pekerjaan, bahan, tenaga kerja, alat, biaya, kendala, dan kepatuhan SOP {organization?.name ? `untuk ${organization.name}` : "organisasi aktif"}.</p></div>
        <div className={styles.heroActions}>{!canWrite && <span className={styles.readOnlyBadge}>Mode baca</span>}<button className={styles.secondaryButton} type="button" disabled={isRefreshing || isLoading || !selectedCycleId} onClick={() => void refreshActivities()}><Icon name="refresh" /> {isRefreshing ? "Memuat..." : "Muat ulang"}</button>{canWrite && isCycleMutable && <button className={styles.primaryButton} type="button" onClick={() => { setModalError(null); setEditor({ kind: "activity", activityId: null }); }}><Icon name="add" /> Rencanakan aktivitas</button>}</div>
      </div>

      {notice && <div className={styles.notice} role="status"><span><Icon name="check" /></span><strong>{notice}</strong><button type="button" aria-label="Tutup pemberitahuan" onClick={() => setNotice(null)}><Icon name="close" /></button></div>}
      {pageError && <div className={styles.pageError} role="alert">{pageError}</div>}

      <div className={styles.cycleSelector}><label><span>Siklus yang dicatat</span><select value={selectedCycleId} disabled={isLoading || cropCycles.length === 0} onChange={(event) => { setSelectedCycleId(event.target.value); setQuery(""); setStatusFilter("all"); setTypeFilter("all"); }}>{cropCycles.map((cycle) => <option value={cycle.id} key={cycle.id}>{cycle.code} · {cycle.name} · {cycle.status === 1 ? "Rencana" : cycle.status === 2 ? "Berjalan" : cycle.status === 3 ? "Selesai" : "Dibatalkan"}</option>)}</select></label>{selectedCycle && <div><small>Periode</small><strong>{formatActivityDate(selectedCycle.plannedStartDate)} – {formatActivityDate(selectedCycle.expectedHarvestDate)}</strong><span className={`${styles.cycleStatus} ${styles[`cycleStatus${selectedCycle.status}`]}`}>{selectedCycle.status === 1 ? "Rencana" : selectedCycle.status === 2 ? "Berjalan" : selectedCycle.status === 3 ? "Selesai" : "Dibatalkan"}</span></div>}</div>

      {isLoading ? <div className={styles.loadingState}><span className="loader" /><p>Memuat siklus budidaya...</p></div> : cropCycles.length === 0 ? <div className={styles.emptyState}><span><Icon name="activity" /></span><h2>Belum ada siklus budidaya</h2><p>Buat siklus terlebih dahulu sebelum mencatat pekerjaan lapangan.</p><button className={styles.primaryButton} type="button" onClick={() => router.push("/cultivation")}>Buka Siklus Budidaya</button></div> : <>
        <div className={styles.metricGrid}><article><span>Rencana</span><strong>{plannedCount}</strong><small>Menunggu dikerjakan</small><i><Icon name="calendar" /></i></article><article className={styles.metricActive}><span>Sedang berjalan</span><strong>{inProgressCount}</strong><small>Perlu diperbarui</small><i><Icon name="start" /></i></article><article><span>Selesai</span><strong>{completedCount}</strong><small>Siap dievaluasi</small><i><Icon name="check" /></i></article><article><span>Biaya aktual</span><strong>{formatCurrency(totalCost)}</strong><small>{activities.reduce((sum, item) => sum + item.resources.length, 0)} baris sumber daya</small><i><Icon name="cost" /></i></article></div>

        <div className={styles.toolbar}><label className={styles.searchField}><Icon name="search" /><input value={query} aria-label="Cari aktivitas" placeholder="Cari kode, pekerjaan, jenis, atau langkah SOP" onChange={(event) => setQuery(event.target.value)} /></label><label className={styles.filterField}><span>Status</span><select value={statusFilter} onChange={(event) => setStatusFilter(event.target.value === "all" ? "all" : Number(event.target.value) as ActivityStatusFilter)}><option value="all">Semua status</option>{Object.entries(activityStatusLabels).map(([value, label]) => <option value={value} key={value}>{label}</option>)}</select></label><label className={styles.filterField}><span>Jenis</span><select value={typeFilter} onChange={(event) => setTypeFilter(event.target.value === "all" ? "all" : Number(event.target.value) as ActivityTypeFilter)}><option value="all">Semua jenis</option>{Object.entries(activityTypeLabels).map(([value, label]) => <option value={value} key={value}>{label}</option>)}</select></label><span className={styles.resultCount}>{filteredActivities.length} hasil</span></div>

        {isActivityLoading ? <div className={styles.loadingState}><span className="loader" /><p>Memuat aktivitas budidaya...</p></div> : activities.length === 0 ? <div className={styles.emptyState}><span><Icon name="activity" /></span><h2>Belum ada aktivitas pada siklus ini</h2><p>Mulai dari pekerjaan persiapan lahan, penanaman, pemupukan, atau pemantauan.</p>{canWrite && isCycleMutable && <button className={styles.primaryButton} type="button" onClick={() => setEditor({ kind: "activity", activityId: null })}><Icon name="add" /> Rencanakan aktivitas pertama</button>}</div> : filteredActivities.length === 0 ? <div className={styles.emptyState}><span><Icon name="search" /></span><h2>Tidak ada hasil yang sesuai</h2><p>Ubah kata pencarian atau filter aktivitas.</p><button className={styles.secondaryButton} type="button" onClick={() => { setQuery(""); setStatusFilter("all"); setTypeFilter("all"); }}>Bersihkan filter</button></div> : <div className={styles.managementGrid}>
          <aside className={styles.activityList}><header><div><span className={styles.eyebrow}>Daftar pekerjaan</span><h2>{filteredActivities.length} aktivitas</h2></div></header><div className={styles.activityCards}>{filteredActivities.map((activity) => <button className={`${styles.activityCard} ${activity.id === selectedActivity?.id ? styles.activityCardSelected : ""}`} type="button" key={activity.id} aria-pressed={activity.id === selectedActivity?.id} onClick={() => setSelectedActivityId(activity.id)}><span className={styles.cardTopline}><strong>{activity.code}</strong><i className={`${styles.statusBadge} ${styles[`status${activity.status}`]}`}>{activityStatusLabels[activity.status]}</i></span><b>{activity.name}</b><span className={styles.cardType}><Icon name="activity" /> {activityTypeLabels[activity.activityType]}</span><span className={styles.cardMeta}><small>{formatActivityDate(activity.plannedDate)}</small><small>{formatCurrency(activity.totalActualCost)}</small></span></button>)}</div></aside>

          {selectedActivity && <article className={styles.activityDetail}>
            <header className={styles.detailHeader}><div className={styles.detailIdentity}><span className={styles.detailIcon}><Icon name="activity" /></span><div><span>{selectedActivity.code}</span><h2>{selectedActivity.name}</h2><p>{activityTypeLabels[selectedActivity.activityType]}</p></div></div><div className={styles.detailActions}><span className={`${styles.statusBadge} ${styles[`status${selectedActivity.status}`]}`}>{activityStatusLabels[selectedActivity.status]}</span>{canWrite && selectedActivity.status === 1 && <><button className={styles.secondaryButton} type="button" onClick={() => { setModalError(null); setEditor({ kind: "activity", activityId: selectedActivity.id }); }}><Icon name="edit" /> Ubah</button><button className={styles.primaryButton} type="button" onClick={() => setAction({ kind: "start", activityId: selectedActivity.id })}><Icon name="start" /> Mulai</button></>}{canWrite && selectedActivity.status === 2 && <button className={styles.primaryButton} type="button" onClick={() => setAction({ kind: "complete", activityId: selectedActivity.id })}><Icon name="check" /> Selesaikan</button>}</div></header>

            <div className={styles.timeline}><div className={styles.timelineActive}><span>1</span><strong>Rencana</strong><small>{formatActivityDate(selectedActivity.plannedDate)}</small></div><i /><div className={selectedActivity.status === 2 || selectedActivity.status === 3 ? styles.timelineActive : ""}><span>2</span><strong>Mulai</strong><small>{formatActivityDate(selectedActivity.actualStartDate)}</small></div><i /><div className={selectedActivity.status === 3 ? styles.timelineActive : ""}><span>3</span><strong>Selesai</strong><small>{formatActivityDate(selectedActivity.actualCompletionDate)}</small></div></div>

            <section className={styles.detailSection}><div className={styles.sectionHeader}><div><span className={styles.eyebrow}>Acuan kerja</span><h3>Rencana dan SOP</h3></div>{canWrite && (selectedActivity.status === 1 || selectedActivity.status === 2) && <button className={styles.textAction} type="button" onClick={() => setAction({ kind: "notes", activityId: selectedActivity.id })}><Icon name="notes" /> Catatan &amp; kendala</button>}</div><div className={styles.infoGrid}><div><small>Tanggal rencana</small><strong>{formatActivityDate(selectedActivity.plannedDate)}</strong><span>{selectedActivity.actualStartDate ? `Mulai ${formatActivityDate(selectedActivity.actualStartDate)}` : "Belum dimulai"}</span></div><div><small>Langkah SOP</small><strong>{selectedActivity.sopStepNameSnapshot ?? "Di luar SOP"}</strong><span>{selectedActivity.sopStepSequenceSnapshot ? `Langkah ${selectedActivity.sopStepSequenceSnapshot} · H+${selectedActivity.sopPlannedDayOffsetSnapshot}` : "Pekerjaan tambahan/korektif"}</span></div><div><small>Kepatuhan</small><strong>{complianceLabels[selectedActivity.sopComplianceStatus]}</strong><span>{selectedActivity.deviationReason ?? "Belum ada alasan deviasi"}</span></div><div><small>Biaya aktual</small><strong>{formatCurrency(selectedActivity.totalActualCost)}</strong><span>{selectedActivity.resources.length} baris sumber daya</span></div></div></section>

            <section className={styles.detailSection}><div className={styles.sectionHeader}><div><span className={styles.eyebrow}>Biaya lapangan</span><h3>Sumber daya terpakai</h3></div>{canWrite && (selectedActivity.status === 1 || selectedActivity.status === 2) && <button className={styles.primarySmallButton} type="button" onClick={() => { setModalError(null); setEditor({ kind: "resource", activityId: selectedActivity.id, resourceId: null }); }}><Icon name="add" /> Tambah sumber daya</button>}</div>{selectedActivity.resources.length === 0 ? <div className={styles.inlineEmpty}><Icon name="resource" /><span><strong>Belum ada sumber daya</strong><small>Catat bahan, tenaga kerja, alat, atau jasa yang digunakan.</small></span></div> : <div className={styles.resourceTable}><div className={styles.resourceHeader}><span>Sumber daya</span><span>Pemakaian</span><span>Biaya</span><span /></div>{selectedActivity.resources.map((resource) => <div className={styles.resourceRow} key={resource.id}><span><i>{resourceTypeLabels[resource.resourceType]}</i><strong>{resource.description}</strong><small>{resource.notes || "Tanpa catatan"}</small></span><span><strong>{formatQuantity(resource.quantity, resource.unit)}</strong><small>{formatCurrency(resource.unitCost)} / {resource.unit}</small></span><span><strong>{formatCurrency(resource.totalCost)}</strong></span><span>{canWrite && (selectedActivity.status === 1 || selectedActivity.status === 2) && <><button type="button" aria-label={`Ubah ${resource.description}`} onClick={() => setEditor({ kind: "resource", activityId: selectedActivity.id, resourceId: resource.id })}><Icon name="edit" /></button><button className={styles.deleteIconButton} type="button" aria-label={`Hapus ${resource.description}`} onClick={() => setAction({ kind: "remove-resource", activityId: selectedActivity.id, resourceId: resource.id })}><Icon name="trash" /></button></>}</span></div>)}</div>}</section>

            <section className={styles.detailSection}><div className={styles.sectionHeader}><div><span className={styles.eyebrow}>Catatan evaluasi</span><h3>Hasil dan kendala</h3></div></div><div className={styles.notesGrid}><div><span><Icon name="notes" /> Catatan</span><p>{selectedActivity.notes || "Belum ada catatan aktivitas."}</p></div><div className={selectedActivity.issueNotes ? styles.issueCard : ""}><span><Icon name="issue" /> Kendala lapangan</span><p>{selectedActivity.issueNotes || "Tidak ada kendala yang dicatat."}</p></div>{selectedActivity.status === 3 && <div className={styles.outcomeCard}><span><Icon name="check" /> Hasil pekerjaan</span><p>{selectedActivity.outcome || "Hasil belum dijelaskan."}</p></div>}{selectedActivity.status === 4 && <div className={styles.cancelCard}><span><Icon name="stop" /> Alasan pembatalan</span><p>{selectedActivity.cancellationReason}</p></div>}</div></section>

            {canWrite && (selectedActivity.status === 1 || selectedActivity.status === 2) && <footer className={styles.detailFooter}><div><strong>Aktivitas tidak dilanjutkan?</strong><span>Simpan alasan pembatalan untuk histori dan evaluasi musim.</span></div><button className={styles.dangerTextButton} type="button" onClick={() => setAction({ kind: "cancel", activityId: selectedActivity.id })}><Icon name="stop" /> Batalkan aktivitas</button></footer>}
          </article>}
        </div>}
      </>}

      {editor?.kind === "activity" && selectedCycle && (editor.activityId === null || modalActivity) && <div className={styles.modalBackdrop} role="presentation" onMouseDown={(event) => { if (event.target === event.currentTarget && !isSaving) setEditor(null); }}><div className={styles.modalPanel} role="dialog" aria-modal="true" aria-label={editor.activityId ? "Ubah aktivitas" : "Buat aktivitas"}><ActivityEditor key={editor.activityId ?? "new-activity"} activity={modalActivity} cycle={selectedCycle} cultivationSops={cultivationSops} isSaving={isSaving} apiError={modalError} onCancel={() => { setEditor(null); setModalError(null); }} onSubmit={submitActivity} /></div></div>}
      {editor?.kind === "resource" && modalActivity && (editor.resourceId === null || modalResource) && <div className={styles.modalBackdrop} role="presentation" onMouseDown={(event) => { if (event.target === event.currentTarget && !isSaving) setEditor(null); }}><ResourceEditor key={editor.resourceId ?? "new-resource"} resource={modalResource} activity={modalActivity} isSaving={isSaving} apiError={modalError} onCancel={() => { setEditor(null); setModalError(null); }} onSubmit={submitResource} /></div>}
      {action && modalActivity && <div className={styles.modalBackdrop} role="presentation" onMouseDown={(event) => { if (event.target === event.currentTarget && !isSaving) setAction(null); }}><ActivityActionDialog key={`${action.kind}-${modalActivity.id}`} action={action} activity={modalActivity} resource={modalResource} isSaving={isSaving} apiError={modalError} onCancel={() => { setAction(null); setModalError(null); }} onSubmit={submitAction} /></div>}
    </section>
  );
}
