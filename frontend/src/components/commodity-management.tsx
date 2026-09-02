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
  createCommodity,
  createCommodityCategory,
  getCommodities,
  getCommodityCategories,
  setCommodityActive,
  setCommodityCategoryActive,
  updateCommodity,
  updateCommodityCategory,
} from "@/lib/api/client";
import type {
  Commodity,
  CommodityCategory,
  CreateCommodityCategoryRequest,
  CreateCommodityRequest,
  Organization,
  UpdateCommodityCategoryRequest,
} from "@/lib/api/contracts";
import {
  commodityCategoryDraftFrom,
  commodityDraftFrom,
  filterCommodities,
  normalizeCommodityCode,
  optionalMasterDataText,
  type MasterDataStatusFilter,
  validateCommodityCategoryDraft,
  validateCommodityDraft,
} from "@/lib/master-data/commodity-management";
import {
  hasFormDraftChanged,
  resolveFormCloseDecision,
  type FormCloseSource,
} from "@/lib/ui/form-data-loss";
import styles from "./commodity-management.module.css";

type CommodityManagementProps = {
  organization: Organization | null;
  organizationId: string | null;
  permissions: string[];
};

type EditorState =
  | { kind: "category"; id: string | null }
  | { kind: "commodity"; id: string | null };

type ToggleState = {
  kind: "category" | "commodity";
  id: string;
  nextActive: boolean;
};

type IconName =
  | "add"
  | "back"
  | "category"
  | "check"
  | "close"
  | "edit"
  | "leaf"
  | "refresh"
  | "search"
  | "stop";

const iconPaths: Record<IconName, string> = {
  add: "M12 5v14M5 12h14",
  back: "m15 18-6-6 6-6",
  category: "M4 4h6v6H4V4Zm10 0h6v6h-6V4ZM4 14h6v6H4v-6Zm10 0h6v6h-6v-6Z",
  check: "m5 12 4 4L19 6",
  close: "m6 6 12 12M18 6 6 18",
  edit: "m4 20 4.5-1 10-10a2.1 2.1 0 0 0-3-3l-10 10L4 20Zm10-12 3 3",
  leaf: "M12 21v-9m0 2c-4 0-7-2-7-6 4 0 7 2 7 6Zm0-3c4 0 7-2 7-6-4 0-7 2-7 6Z",
  refresh: "M20 6v5h-5M4 18v-5h5m10-2a7 7 0 0 0-12-4L4 11m16 2-3 4a7 7 0 0 1-12-4",
  search: "m21 21-4.35-4.35m2.35-5.65a8 8 0 1 1-16 0 8 8 0 0 1 16 0Z",
  stop: "M6 6h12v12H6V6Z",
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
    case "CommodityCategories.NameAlreadyExists":
      return "Nama kategori sudah digunakan dalam organisasi ini.";
    case "Commodities.CodeAlreadyExists":
      return "Kode komoditas sudah digunakan dalam organisasi ini.";
    case "Commodities.CategoryNotFound":
      return "Kategori komoditas tidak ditemukan. Muat ulang data lalu pilih kategori lain.";
    case "CommodityCategories.NotFound":
    case "Commodities.NotFound":
      return "Data sudah tidak tersedia. Muat ulang halaman.";
    default:
      return error.message;
  }
}

function replaceCategory(
  categories: CommodityCategory[],
  updated: CommodityCategory,
): CommodityCategory[] {
  const next = categories.some((item) => item.id === updated.id)
    ? categories.map((item) => item.id === updated.id ? updated : item)
    : [...categories, updated];

  return next.sort((left, right) => left.name.localeCompare(right.name, "id-ID"));
}

function replaceCommodity(
  commodities: Commodity[],
  updated: Commodity,
): Commodity[] {
  const next = commodities.some((item) => item.id === updated.id)
    ? commodities.map((item) => item.id === updated.id ? updated : item)
    : [...commodities, updated];

  return next.sort((left, right) => left.name.localeCompare(right.name, "id-ID"));
}

function CategoryEditor({
  category,
  isSaving,
  apiError,
  onDirtyChange,
  onCancel,
  onSubmit,
}: {
  category: CommodityCategory | null;
  isSaving: boolean;
  apiError: string | null;
  onDirtyChange: (isDirty: boolean) => void;
  onCancel: () => void;
  onSubmit: (
    request: CreateCommodityCategoryRequest | UpdateCommodityCategoryRequest,
  ) => Promise<void>;
}) {
  const baselineDraft = useMemo(
    () => commodityCategoryDraftFrom(category),
    [category],
  );
  const [draft, setDraft] = useState(() => baselineDraft);
  const [errors, setErrors] = useState<string[]>([]);

  useEffect(() => {
    onDirtyChange(hasFormDraftChanged(baselineDraft, draft));
  }, [baselineDraft, draft, onDirtyChange]);

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const validationErrors = validateCommodityCategoryDraft(draft);
    setErrors(validationErrors);

    if (validationErrors.length > 0) {
      return;
    }

    await onSubmit({
      name: draft.name.trim(),
      description: optionalMasterDataText(draft.description),
    });
  }

  return (
    <form className={styles.editorForm} onSubmit={(event) => void submit(event)}>
      <div className={styles.modalHeader}>
        <div className={styles.modalHeading}>
          <span className={styles.modalIcon}><Icon name="category" /></span>
          <div>
            <span className={styles.eyebrow}>
              {category ? "Ubah kategori" : "Kategori baru"}
            </span>
            <h2>{category ? category.name : "Tambah kategori komoditas"}</h2>
            <p>Kategori membantu mengelompokkan komoditas sebelum musim budidaya dibuat.</p>
          </div>
        </div>
        <button
          className={styles.iconButton}
          type="button"
          aria-label="Tutup formulir kategori"
          disabled={isSaving}
          onClick={onCancel}
        >
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
          <span>Nama kategori <b>*</b></span>
          <input
            autoFocus
            value={draft.name}
            maxLength={150}
            placeholder="Contoh: Tanaman Hortikultura"
            disabled={isSaving}
            onChange={(event) => setDraft((current) => ({
              ...current,
              name: event.target.value,
            }))}
          />
        </label>

        <label className={`${styles.field} ${styles.fieldFull}`}>
          <span>Deskripsi</span>
          <textarea
            value={draft.description}
            maxLength={500}
            rows={4}
            placeholder="Jelaskan cakupan kategori agar konsisten saat dipakai tim."
            disabled={isSaving}
            onChange={(event) => setDraft((current) => ({
              ...current,
              description: event.target.value,
            }))}
          />
          <small>{draft.description.trim().length}/500 karakter</small>
        </label>
      </div>

      <div className={styles.modalFooter}>
        <button
          className={styles.secondaryButton}
          type="button"
          disabled={isSaving}
          onClick={onCancel}
        >
          Batal
        </button>
        <button className={styles.primaryButton} type="submit" disabled={isSaving}>
          {isSaving ? "Menyimpan..." : category ? "Simpan perubahan" : "Tambah kategori"}
        </button>
      </div>
    </form>
  );
}

function CommodityEditor({
  commodity,
  categories,
  isSaving,
  apiError,
  onDirtyChange,
  onCancel,
  onSubmit,
}: {
  commodity: Commodity | null;
  categories: CommodityCategory[];
  isSaving: boolean;
  apiError: string | null;
  onDirtyChange: (isDirty: boolean) => void;
  onCancel: () => void;
  onSubmit: (request: CreateCommodityRequest) => Promise<void>;
}) {
  const baselineDraft = useMemo(
    () => commodityDraftFrom(commodity),
    [commodity],
  );
  const [draft, setDraft] = useState(() => baselineDraft);
  const [errors, setErrors] = useState<string[]>([]);
  const isCreate = commodity === null;
  const categoryOptions = categories.filter(
    (category) => category.isActive || category.id === commodity?.commodityCategoryId,
  );

  useEffect(() => {
    onDirtyChange(hasFormDraftChanged(baselineDraft, draft));
  }, [baselineDraft, draft, onDirtyChange]);

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const validationErrors = validateCommodityDraft(draft, isCreate);
    setErrors(validationErrors);

    if (validationErrors.length > 0) {
      return;
    }

    await onSubmit({
      code: normalizeCommodityCode(draft.code),
      name: draft.name.trim(),
      commodityCategoryId: draft.commodityCategoryId,
      scientificName: optionalMasterDataText(draft.scientificName),
      description: optionalMasterDataText(draft.description),
    });
  }

  return (
    <form className={styles.editorForm} onSubmit={(event) => void submit(event)}>
      <div className={styles.modalHeader}>
        <div className={styles.modalHeading}>
          <span className={styles.modalIcon}><Icon name="leaf" /></span>
          <div>
            <span className={styles.eyebrow}>
              {commodity ? "Ubah komoditas" : "Komoditas baru"}
            </span>
            <h2>{commodity ? commodity.name : "Tambah master komoditas"}</h2>
            <p>Komoditas aktif akan tersedia saat tim membuka rencana budidaya.</p>
          </div>
        </div>
        <button
          className={styles.iconButton}
          type="button"
          aria-label="Tutup formulir komoditas"
          disabled={isSaving}
          onClick={onCancel}
        >
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
          <span>Kode komoditas <b>*</b></span>
          <input
            autoFocus={isCreate}
            value={draft.code}
            placeholder="Contoh: CABAI"
            disabled={isSaving || !isCreate}
            onChange={(event) => setDraft((current) => ({
              ...current,
              code: event.target.value.toUpperCase(),
            }))}
          />
          <small>{isCreate ? "Huruf, angka, tanda hubung, atau garis bawah." : "Kode dikunci setelah komoditas dibuat."}</small>
        </label>

        <label className={styles.field}>
          <span>Nama komoditas <b>*</b></span>
          <input
            autoFocus={!isCreate}
            value={draft.name}
            placeholder="Contoh: Cabai Merah Keriting"
            disabled={isSaving}
            onChange={(event) => setDraft((current) => ({
              ...current,
              name: event.target.value,
            }))}
          />
        </label>

        <label className={styles.field}>
          <span>Kategori <b>*</b></span>
          <select
            value={draft.commodityCategoryId}
            disabled={isSaving}
            onChange={(event) => setDraft((current) => ({
              ...current,
              commodityCategoryId: event.target.value,
            }))}
          >
            <option value="">Pilih kategori</option>
            {categoryOptions.map((category) => (
              <option value={category.id} key={category.id}>
                {category.name}{category.isActive ? "" : " - nonaktif"}
              </option>
            ))}
          </select>
        </label>

        <label className={styles.field}>
          <span>Nama ilmiah</span>
          <input
            value={draft.scientificName}
            placeholder="Contoh: Capsicum annuum"
            disabled={isSaving}
            onChange={(event) => setDraft((current) => ({
              ...current,
              scientificName: event.target.value,
            }))}
          />
        </label>

        <label className={`${styles.field} ${styles.fieldFull}`}>
          <span>Deskripsi</span>
          <textarea
            value={draft.description}
            rows={4}
            placeholder="Catatan varietas umum, karakteristik, atau konteks penggunaan."
            disabled={isSaving}
            onChange={(event) => setDraft((current) => ({
              ...current,
              description: event.target.value,
            }))}
          />
        </label>
      </div>

      <div className={styles.modalFooter}>
        <button
          className={styles.secondaryButton}
          type="button"
          disabled={isSaving}
          onClick={onCancel}
        >
          Batal
        </button>
        <button className={styles.primaryButton} type="submit" disabled={isSaving}>
          {isSaving ? "Menyimpan..." : commodity ? "Simpan perubahan" : "Tambah komoditas"}
        </button>
      </div>
    </form>
  );
}

export function CommodityManagement({
  organization,
  organizationId,
  permissions,
}: CommodityManagementProps) {
  const router = useRouter();
  const [categories, setCategories] = useState<CommodityCategory[]>([]);
  const [commodities, setCommodities] = useState<Commodity[]>([]);
  const [editor, setEditor] = useState<EditorState | null>(null);
  const [toggleTarget, setToggleTarget] = useState<ToggleState | null>(null);
  const [editorDirty, setEditorDirty] = useState(false);
  const [discardOpen, setDiscardOpen] = useState(false);
  const [query, setQuery] = useState("");
  const [statusFilter, setStatusFilter] = useState<MasterDataStatusFilter>("all");
  const [isLoading, setIsLoading] = useState(true);
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [pageError, setPageError] = useState<string | null>(null);
  const [modalError, setModalError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);

  const canRead = permissions.includes("master-data.read");
  const canWrite = permissions.includes("master-data.write");
  const activeCategories = categories.filter((category) => category.isActive);
  const activeCommodities = commodities.filter((commodity) => commodity.isActive);
  const filteredCommodities = useMemo(
    () => filterCommodities(commodities, categories, query, statusFilter),
    [commodities, categories, query, statusFilter],
  );
  const categoryById = useMemo(
    () => new Map(categories.map((category) => [category.id, category])),
    [categories],
  );
  const modalCategory = editor?.kind === "category" && editor.id
    ? categories.find((category) => category.id === editor.id) ?? null
    : null;
  const modalCommodity = editor?.kind === "commodity" && editor.id
    ? commodities.find((commodity) => commodity.id === editor.id) ?? null
    : null;

  const fetchMasterData = useCallback(async () => {
    if (!organizationId || !canRead) {
      return null;
    }

    const [loadedCategories, loadedCommodities] = await Promise.all([
      getCommodityCategories(organizationId),
      getCommodities(organizationId),
    ]);

    return {
      categories: [...loadedCategories].sort(
        (left, right) => left.name.localeCompare(right.name, "id-ID"),
      ),
      commodities: [...loadedCommodities].sort(
        (left, right) => left.name.localeCompare(right.name, "id-ID"),
      ),
    };
  }, [organizationId, canRead]);

  const loadData = useCallback(async (refresh = false) => {
    if (!organizationId || !canRead) {
      return;
    }

    if (refresh) {
      setIsRefreshing(true);
    } else {
      setIsLoading(true);
    }

    setPageError(null);

    try {
      const data = await fetchMasterData();
      if (!data) {
        return;
      }

      setCategories(data.categories);
      setCommodities(data.commodities);
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
  }, [organizationId, canRead, router, fetchMasterData]);

  useEffect(() => {
    let cancelled = false;

    async function loadInitialData() {
      if (!organizationId || !canRead) {
        return;
      }

      try {
        const data = await fetchMasterData();
        if (cancelled || !data) {
          return;
        }

        setCategories(data.categories);
        setCommodities(data.commodities);
      } catch (error) {
        if (cancelled) {
          return;
        }

        if (error instanceof ApiError && error.status === 401) {
          router.replace("/login");
          return;
        }

        setPageError(friendlyError(error));
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
  }, [organizationId, canRead, router, fetchMasterData]);

  const closeEditor = useCallback(() => {
    setEditor(null);
    setEditorDirty(false);
    setDiscardOpen(false);
    setModalError(null);
  }, []);

  const requestEditorClose = useCallback((source: FormCloseSource) => {
    if (!editor) {
      return;
    }

    const decision = resolveFormCloseDecision({
      source,
      isDirty: editorDirty,
      isSaving,
    });

    if (decision === "close") {
      closeEditor();
    } else if (decision === "confirm-discard") {
      setDiscardOpen(true);
    }
  }, [editor, editorDirty, isSaving, closeEditor]);

  useEffect(() => {
    if (!editor) {
      return;
    }

    function handleEscape(event: KeyboardEvent) {
      if (event.key !== "Escape") {
        return;
      }

      event.preventDefault();

      if (discardOpen) {
        setDiscardOpen(false);
        return;
      }

      requestEditorClose("escape");
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

  async function submitCategory(
    request: CreateCommodityCategoryRequest | UpdateCommodityCategoryRequest,
  ) {
    if (!organizationId || !canWrite || editor?.kind !== "category") {
      return;
    }

    setIsSaving(true);
    setModalError(null);

    try {
      const updated = editor.id
        ? await updateCommodityCategory(
          organizationId,
          editor.id,
          request as UpdateCommodityCategoryRequest,
        )
        : await createCommodityCategory(
          organizationId,
          request as CreateCommodityCategoryRequest,
        );

      setCategories((current) => replaceCategory(current, updated));
      setNotice(editor.id
        ? "Kategori komoditas berhasil diperbarui."
        : "Kategori komoditas berhasil ditambahkan.");
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

  async function submitCommodity(request: CreateCommodityRequest) {
    if (!organizationId || !canWrite || editor?.kind !== "commodity") {
      return;
    }

    setIsSaving(true);
    setModalError(null);

    try {
      const updated = editor.id
        ? await updateCommodity(
          organizationId,
          editor.id,
          {
            name: request.name,
            commodityCategoryId: request.commodityCategoryId,
            scientificName: request.scientificName,
            description: request.description,
          },
        )
        : await createCommodity(organizationId, request);

      setCommodities((current) => replaceCommodity(current, updated));
      setNotice(editor.id
        ? "Master komoditas berhasil diperbarui."
        : "Komoditas baru berhasil ditambahkan dan siap dipakai pada rencana budidaya.");
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

  async function submitToggle() {
    if (!organizationId || !canWrite || !toggleTarget) {
      return;
    }

    setIsSaving(true);
    setModalError(null);

    try {
      if (toggleTarget.kind === "category") {
        const updated = await setCommodityCategoryActive(
          organizationId,
          toggleTarget.id,
          toggleTarget.nextActive,
        );
        setCategories((current) => replaceCategory(current, updated));
        setNotice(updated.isActive
          ? "Kategori kembali diaktifkan."
          : "Kategori dinonaktifkan.");
      } else {
        const updated = await setCommodityActive(
          organizationId,
          toggleTarget.id,
          toggleTarget.nextActive,
        );
        setCommodities((current) => replaceCommodity(current, updated));
        setNotice(updated.isActive
          ? "Komoditas kembali aktif dan dapat dipilih pada rencana budidaya."
          : "Komoditas dinonaktifkan dan tidak ditawarkan untuk siklus baru.");
      }

      setToggleTarget(null);
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
    return (
      <section className={styles.accessState}>
        <Icon name="category" />
        <h1>Pilih organisasi terlebih dahulu</h1>
        <p>Master komoditas selalu dikelola dalam organisasi aktif.</p>
      </section>
    );
  }

  if (!canRead) {
    return (
      <section className={styles.accessState}>
        <Icon name="stop" />
        <h1>Akses master data tidak tersedia</h1>
        <p>Peran Anda belum memiliki izin untuk membaca master data.</p>
      </section>
    );
  }

  return (
    <section className={styles.page}>
      <div className={styles.hero}>
        <div>
          <span className={styles.eyebrow}>Fondasi budidaya</span>
          <h1>Master Komoditas</h1>
          <p>
            Kelola kategori dan daftar komoditas untuk {organization?.name ?? "organisasi aktif"}.
            Komoditas aktif menjadi referensi resmi saat membuka siklus budidaya.
          </p>
        </div>
        <div className={styles.heroActions}>
          <button
            className={styles.secondaryButton}
            type="button"
            onClick={() => router.push("/cultivation")}
          >
            <Icon name="back" /> Budidaya
          </button>
          <button
            className={styles.secondaryButton}
            type="button"
            disabled={isRefreshing || isLoading}
            onClick={() => void loadData(true)}
          >
            <Icon name="refresh" /> {isRefreshing ? "Memuat..." : "Muat ulang"}
          </button>
          {canWrite && (
            <button
              className={styles.primaryButton}
              type="button"
              disabled={activeCategories.length === 0}
              title={activeCategories.length === 0 ? "Buat kategori aktif terlebih dahulu." : undefined}
              onClick={() => openEditor({ kind: "commodity", id: null })}
            >
              <Icon name="add" /> Tambah komoditas
            </button>
          )}
        </div>
      </div>

      {pageError && <div className={styles.pageError} role="alert">{pageError}</div>}
      {notice && <div className={styles.notice} role="status"><Icon name="check" /> {notice}</div>}

      <div className={styles.metricGrid}>
        <article>
          <span>Total komoditas</span>
          <strong>{commodities.length}</strong>
          <small>Seluruh master organisasi</small>
          <i><Icon name="leaf" /></i>
        </article>
        <article className={styles.metricActive}>
          <span>Komoditas aktif</span>
          <strong>{activeCommodities.length}</strong>
          <small>Tersedia untuk siklus baru</small>
          <i><Icon name="check" /></i>
        </article>
        <article>
          <span>Kategori aktif</span>
          <strong>{activeCategories.length}</strong>
          <small>Dari {categories.length} kategori</small>
          <i><Icon name="category" /></i>
        </article>
      </div>

      {isLoading ? (
        <div className={styles.loadingState}>
          <span className="loader" />
          <p>Memuat master komoditas...</p>
        </div>
      ) : (
        <div className={styles.contentGrid}>
          <section className={styles.panel}>
            <div className={styles.panelHeader}>
              <div>
                <span className={styles.eyebrow}>Pengelompokan</span>
                <h2>Kategori</h2>
                <p>Komoditas wajib berada dalam satu kategori.</p>
              </div>
              {canWrite && (
                <button
                  className={styles.compactButton}
                  type="button"
                  onClick={() => openEditor({ kind: "category", id: null })}
                >
                  <Icon name="add" /> Tambah
                </button>
              )}
            </div>

            {categories.length === 0 ? (
              <div className={styles.emptyState}>
                <span><Icon name="category" /></span>
                <h3>Belum ada kategori</h3>
                <p>Buat kategori pertama sebelum menambahkan komoditas.</p>
                {canWrite && (
                  <button
                    className={styles.primaryButton}
                    type="button"
                    onClick={() => openEditor({ kind: "category", id: null })}
                  >
                    <Icon name="add" /> Buat kategori
                  </button>
                )}
              </div>
            ) : (
              <div className={styles.categoryList}>
                {categories.map((category) => {
                  const commodityCount = commodities.filter(
                    (commodity) => commodity.commodityCategoryId === category.id,
                  ).length;

                  return (
                    <article className={styles.categoryCard} key={category.id}>
                      <div className={styles.categoryCopy}>
                        <div className={styles.titleRow}>
                          <strong>{category.name}</strong>
                          <span className={category.isActive ? styles.statusActive : styles.statusInactive}>
                            {category.isActive ? "Aktif" : "Nonaktif"}
                          </span>
                        </div>
                        <p>{category.description || "Tanpa deskripsi kategori."}</p>
                        <small>{commodityCount} komoditas</small>
                      </div>
                      {canWrite && (
                        <div className={styles.rowActions}>
                          <button
                            type="button"
                            aria-label={`Ubah kategori ${category.name}`}
                            onClick={() => openEditor({ kind: "category", id: category.id })}
                          >
                            <Icon name="edit" />
                          </button>
                          <button
                            className={category.isActive ? styles.dangerTextButton : styles.successTextButton}
                            type="button"
                            onClick={() => {
                              setModalError(null);
                              setToggleTarget({
                                kind: "category",
                                id: category.id,
                                nextActive: !category.isActive,
                              });
                            }}
                          >
                            {category.isActive ? "Nonaktifkan" : "Aktifkan"}
                          </button>
                        </div>
                      )}
                    </article>
                  );
                })}
              </div>
            )}
          </section>

          <section className={`${styles.panel} ${styles.commodityPanel}`}>
            <div className={styles.panelHeader}>
              <div>
                <span className={styles.eyebrow}>Referensi operasional</span>
                <h2>Komoditas</h2>
                <p>Daftar ini dipakai langsung oleh form rencana budidaya.</p>
              </div>
              {canWrite && activeCategories.length > 0 && (
                <button
                  className={styles.compactButton}
                  type="button"
                  onClick={() => openEditor({ kind: "commodity", id: null })}
                >
                  <Icon name="add" /> Tambah
                </button>
              )}
            </div>

            <div className={styles.toolbar}>
              <label className={styles.searchField}>
                <Icon name="search" />
                <input
                  value={query}
                  aria-label="Cari komoditas"
                  placeholder="Cari kode, nama, kategori, atau nama ilmiah"
                  onChange={(event) => setQuery(event.target.value)}
                />
              </label>
              <label className={styles.filterField}>
                <span>Status</span>
                <select
                  value={statusFilter}
                  onChange={(event) => setStatusFilter(event.target.value as MasterDataStatusFilter)}
                >
                  <option value="all">Semua status</option>
                  <option value="active">Aktif</option>
                  <option value="inactive">Nonaktif</option>
                </select>
              </label>
              <span className={styles.resultCount}>{filteredCommodities.length} hasil</span>
            </div>

            {commodities.length === 0 ? (
              <div className={styles.emptyState}>
                <span><Icon name="leaf" /></span>
                <h3>Belum ada komoditas</h3>
                <p>
                  {activeCategories.length === 0
                    ? "Buat kategori aktif terlebih dahulu."
                    : "Tambahkan komoditas pertama agar siklus budidaya dapat dibuat."}
                </p>
                {canWrite && activeCategories.length > 0 && (
                  <button
                    className={styles.primaryButton}
                    type="button"
                    onClick={() => openEditor({ kind: "commodity", id: null })}
                  >
                    <Icon name="add" /> Tambah komoditas pertama
                  </button>
                )}
              </div>
            ) : filteredCommodities.length === 0 ? (
              <div className={styles.emptyState}>
                <span><Icon name="search" /></span>
                <h3>Tidak ada hasil</h3>
                <p>Ubah kata kunci atau filter status.</p>
              </div>
            ) : (
              <div className={styles.commodityList}>
                {filteredCommodities.map((commodity) => {
                  const category = categoryById.get(commodity.commodityCategoryId);

                  return (
                    <article className={styles.commodityCard} key={commodity.id}>
                      <div className={styles.commodityIdentity}>
                        <span className={styles.commodityIcon}><Icon name="leaf" /></span>
                        <div>
                          <div className={styles.titleRow}>
                            <strong>{commodity.name}</strong>
                            <span className={commodity.isActive ? styles.statusActive : styles.statusInactive}>
                              {commodity.isActive ? "Aktif" : "Nonaktif"}
                            </span>
                          </div>
                          <code>{commodity.code}</code>
                          <p>
                            {category?.name ?? "Kategori tidak ditemukan"}
                            {commodity.scientificName ? ` · ${commodity.scientificName}` : ""}
                          </p>
                          {commodity.description && <small>{commodity.description}</small>}
                        </div>
                      </div>
                      {canWrite && (
                        <div className={styles.rowActions}>
                          <button
                            type="button"
                            aria-label={`Ubah komoditas ${commodity.name}`}
                            onClick={() => openEditor({ kind: "commodity", id: commodity.id })}
                          >
                            <Icon name="edit" />
                          </button>
                          <button
                            className={commodity.isActive ? styles.dangerTextButton : styles.successTextButton}
                            type="button"
                            onClick={() => {
                              setModalError(null);
                              setToggleTarget({
                                kind: "commodity",
                                id: commodity.id,
                                nextActive: !commodity.isActive,
                              });
                            }}
                          >
                            {commodity.isActive ? "Nonaktifkan" : "Aktifkan"}
                          </button>
                        </div>
                      )}
                    </article>
                  );
                })}
              </div>
            )}
          </section>
        </div>
      )}

      {editor?.kind === "category" && (editor.id === null || modalCategory) && (
        <div
          className={styles.modalBackdrop}
          role="presentation"
          onMouseDown={(event) => {
            if (event.target === event.currentTarget) {
              requestEditorClose("backdrop");
            }
          }}
        >
          <div className={styles.modalPanel} role="dialog" aria-modal="true" aria-label={editor.id ? "Ubah kategori komoditas" : "Tambah kategori komoditas"}>
            <CategoryEditor
              key={editor.id ?? "new-category"}
              category={modalCategory}
              isSaving={isSaving}
              apiError={modalError}
              onDirtyChange={setEditorDirty}
              onCancel={() => requestEditorClose("explicit")}
              onSubmit={submitCategory}
            />
          </div>
        </div>
      )}

      {editor?.kind === "commodity" && (editor.id === null || modalCommodity) && (
        <div
          className={styles.modalBackdrop}
          role="presentation"
          onMouseDown={(event) => {
            if (event.target === event.currentTarget) {
              requestEditorClose("backdrop");
            }
          }}
        >
          <div className={styles.modalPanel} role="dialog" aria-modal="true" aria-label={editor.id ? "Ubah komoditas" : "Tambah komoditas"}>
            <CommodityEditor
              key={editor.id ?? "new-commodity"}
              commodity={modalCommodity}
              categories={categories}
              isSaving={isSaving}
              apiError={modalError}
              onDirtyChange={setEditorDirty}
              onCancel={() => requestEditorClose("explicit")}
              onSubmit={submitCommodity}
            />
          </div>
        </div>
      )}

      {discardOpen && (
        <div
          className={styles.modalBackdrop}
          role="presentation"
          onMouseDown={(event) => {
            if (event.target === event.currentTarget && !isSaving) {
              setDiscardOpen(false);
            }
          }}
        >
          <div
            className={styles.confirmDialog}
            role="alertdialog"
            aria-modal="true"
            aria-labelledby="master-data-discard-title"
            aria-describedby="master-data-discard-description"
          >
            <span className={styles.confirmIcon}><Icon name="stop" /></span>
            <span className={styles.eyebrow}>Perubahan belum disimpan</span>
            <h2 id="master-data-discard-title">Buang perubahan formulir?</h2>
            <p id="master-data-discard-description">
              Perubahan yang belum disimpan akan hilang. Pilih lanjut mengedit untuk kembali ke formulir.
            </p>
            <div className={styles.modalFooter}>
              <button
                className={styles.secondaryButton}
                type="button"
                disabled={isSaving}
                onClick={() => setDiscardOpen(false)}
              >
                Lanjut mengedit
              </button>
              <button
                className={styles.dangerButton}
                type="button"
                disabled={isSaving}
                onClick={closeEditor}
              >
                Buang perubahan
              </button>
            </div>
          </div>
        </div>
      )}

      {toggleTarget && (
        <div
          className={styles.modalBackdrop}
          role="presentation"
          onMouseDown={(event) => {
            if (event.target === event.currentTarget && !isSaving) {
              setToggleTarget(null);
              setModalError(null);
            }
          }}
        >
          <div className={styles.confirmDialog} role="alertdialog" aria-modal="true">
            <span className={styles.confirmIcon}>
              <Icon name={toggleTarget.nextActive ? "check" : "stop"} />
            </span>
            <span className={styles.eyebrow}>
              {toggleTarget.nextActive ? "Aktifkan kembali" : "Nonaktifkan"}
            </span>
            <h2>
              {toggleTarget.kind === "commodity" ? "Ubah status komoditas?" : "Ubah status kategori?"}
            </h2>
            <p>
              {toggleTarget.kind === "commodity"
                ? toggleTarget.nextActive
                  ? "Komoditas akan kembali tersedia untuk siklus budidaya baru."
                  : "Komoditas tidak akan ditawarkan untuk siklus budidaya baru. Histori yang sudah ada tetap dipertahankan."
                : toggleTarget.nextActive
                  ? "Kategori akan kembali tersedia saat membuat atau mengubah master komoditas."
                  : "Kategori tidak akan ditawarkan untuk master komoditas baru. Komoditas yang sudah terhubung tidak dihapus."}
            </p>
            {modalError && <div className={styles.errorPanel} role="alert"><span>{modalError}</span></div>}
            <div className={styles.modalFooter}>
              <button
                className={styles.secondaryButton}
                type="button"
                disabled={isSaving}
                onClick={() => {
                  setToggleTarget(null);
                  setModalError(null);
                }}
              >
                Batal
              </button>
              <button
                className={toggleTarget.nextActive ? styles.primaryButton : styles.dangerButton}
                type="button"
                disabled={isSaving}
                onClick={() => void submitToggle()}
              >
                {isSaving
                  ? "Memproses..."
                  : toggleTarget.nextActive
                    ? "Aktifkan"
                    : "Nonaktifkan"}
              </button>
            </div>
          </div>
        </div>
      )}
    </section>
  );
}
