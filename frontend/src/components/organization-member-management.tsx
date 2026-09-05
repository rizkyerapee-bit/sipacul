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
  changeOrganizationMemberRole,
  createOrganizationMember,
  getOrganizationMembers,
  setOrganizationMembershipActive,
} from "@/lib/api/client";
import type {
  AssignableOrganizationRole,
  Organization,
  OrganizationMember,
  OrganizationRole,
} from "@/lib/api/contracts";
import {
  assignableOrganizationMemberRoles,
  filterOrganizationMembers,
  getOrganizationMemberAccountLabel,
  getOrganizationMemberRoleLabel,
  getOrganizationMemberStatusLabel,
  isOrganizationMembershipActive,
  isOrganizationOwner,
  organizationMemberDraft,
  toCreateOrganizationMemberRequest,
  toUpdateOrganizationMemberRoleRequest,
  validateOrganizationMemberDraft,
  type OrganizationMemberDraft,
  type OrganizationMemberRoleFilter,
  type OrganizationMemberStatusFilter,
} from "@/lib/organizations/organization-member-management";
import {
  hasFormDraftChanged,
  resolveFormCloseDecision,
  type FormCloseSource,
} from "@/lib/ui/form-data-loss";
import styles from "./organization-member-management.module.css";

type OrganizationMemberManagementProps = {
  organization: Organization | null;
  organizationId: string | null;
  currentMembershipId: string | null;
  permissions: string[];
};

type EditorState =
  | { kind: "create" }
  | {
      kind: "role";
      membershipId: string;
      baselineRole: AssignableOrganizationRole;
    };

type ConfirmationState = {
  membershipId: string;
  nextActive: boolean;
};

type IconName =
  | "add"
  | "check"
  | "close"
  | "edit"
  | "key"
  | "mail"
  | "pause"
  | "play"
  | "refresh"
  | "search"
  | "shield"
  | "team"
  | "user"
  | "warning";

const iconPaths: Record<IconName, string> = {
  add: "M12 5v14M5 12h14",
  check: "m5 12 4 4L19 6",
  close: "m6 6 12 12M18 6 6 18",
  edit: "m4 20 4.5-1 10-10a2.1 2.1 0 0 0-3-3l-10 10L4 20Zm10-12 3 3",
  key: "M15 7a5 5 0 1 1-9.7 1.7A5 5 0 0 1 15 7Zm0 0h7m-3 0v3",
  mail: "M3 6h18v12H3V6Zm0 1 9 7 9-7",
  pause: "M8 5v14m8-14v14",
  play: "m8 5 11 7-11 7V5Z",
  refresh: "M20 6v5h-5M4 18v-5h5m10-2a7 7 0 0 0-12-4L4 11m16 2-3 4a7 7 0 0 1-12-4",
  search: "m21 21-4.35-4.35m2.35-5.65a8 8 0 1 1-16 0 8 8 0 0 1 16 0Z",
  shield: "M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10Zm-3-10 2 2 4-4",
  team: "M9 11a4 4 0 1 0 0-8 4 4 0 0 0 0 8ZM2 21c0-4 3-7 7-7s7 3 7 7m8-10a3 3 0 1 0 0-6m0 9c3 0 5 2 5 5",
  user: "M12 12a4 4 0 1 0 0-8 4 4 0 0 0 0 8Zm-7 9c0-4 3-7 7-7s7 3 7 7",
  warning: "M12 9v4m0 4h.01M10.3 4.5 2.6 18a2 2 0 0 0 1.7 3h15.4a2 2 0 0 0 1.7-3L13.7 4.5a2 2 0 0 0-3.4 0Z",
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
    case "OrganizationMembers.AlreadyExists":
      return "Pengguna dengan email tersebut sudah menjadi anggota organisasi ini.";
    case "OrganizationMembers.UserInactive":
      return "Akun pengguna tersebut sedang nonaktif dan tidak dapat ditambahkan.";
    case "OrganizationMembers.OwnerProtected":
      return "Keanggotaan Owner hanya dapat diubah melalui alur transfer kepemilikan khusus.";
    case "OrganizationMembers.NotFound":
      return "Data anggota sudah tidak tersedia. Muat ulang daftar anggota.";
    case "OrganizationMembers.OrganizationNotFound":
      return "Organisasi aktif sudah tidak tersedia.";
    case "OrganizationMembers.IdentityValidation":
      return error.message || "Email atau password awal tidak memenuhi kebijakan akun.";
    case "OrganizationMembers.DataConflict":
      return "Data anggota berbenturan dengan data yang sudah ada. Muat ulang lalu coba kembali.";
    case "OrganizationMembers.Validation":
      return error.message || "Data anggota belum valid.";
    default:
      return error.message;
  }
}

function toAssignableRole(
  role: OrganizationRole,
): AssignableOrganizationRole | null {
  const normalized = String(role).toLocaleLowerCase("en-US");
  if (normalized === "2" || normalized === "admin") return 2;
  if (normalized === "3" || normalized === "finance") return 3;
  if (normalized === "4" || normalized === "operator") return 4;
  return null;
}

function orderMembers(members: OrganizationMember[]): OrganizationMember[] {
  return filterOrganizationMembers(members, "", "all", "all");
}

function replaceMember(
  members: OrganizationMember[],
  updated: OrganizationMember,
): OrganizationMember[] {
  const next = members.some((member) => member.membershipId === updated.membershipId)
    ? members.map((member) => member.membershipId === updated.membershipId ? updated : member)
    : [...members, updated];
  return orderMembers(next);
}

function formatDate(value: string | null): string {
  if (!value) return "-";
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return "-";
  return new Intl.DateTimeFormat("id-ID", {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(date);
}

function memberInitial(email: string): string {
  return email.trim().slice(0, 1).toUpperCase() || "A";
}

export function OrganizationMemberManagement({
  organization,
  organizationId,
  currentMembershipId,
  permissions,
}: OrganizationMemberManagementProps) {
  const router = useRouter();
  const [members, setMembers] = useState<OrganizationMember[]>([]);
  const [query, setQuery] = useState("");
  const [roleFilter, setRoleFilter] = useState<OrganizationMemberRoleFilter>("all");
  const [statusFilter, setStatusFilter] = useState<OrganizationMemberStatusFilter>("all");
  const [selectedMemberId, setSelectedMemberId] = useState<string | null>(null);
  const [editor, setEditor] = useState<EditorState | null>(null);
  const [confirmation, setConfirmation] = useState<ConfirmationState | null>(null);
  const [createDraft, setCreateDraft] = useState<OrganizationMemberDraft>(() => organizationMemberDraft());
  const [roleDraft, setRoleDraft] = useState<AssignableOrganizationRole>(4);
  const [formErrors, setFormErrors] = useState<string[]>([]);
  const [discardOpen, setDiscardOpen] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [pageError, setPageError] = useState<string | null>(null);
  const [modalError, setModalError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);

  const canRead = permissions.includes("members.read");
  const canManage = permissions.includes("members.manage");

  const filteredMembers = useMemo(
    () => filterOrganizationMembers(members, query, roleFilter, statusFilter),
    [members, query, roleFilter, statusFilter],
  );
  const selectedMember = useMemo(
    () => filteredMembers.find((member) => member.membershipId === selectedMemberId)
      ?? filteredMembers[0]
      ?? null,
    [filteredMembers, selectedMemberId],
  );
  const editorMember = useMemo(
    () => editor?.kind === "role"
      ? members.find((member) => member.membershipId === editor.membershipId) ?? null
      : null,
    [editor, members],
  );
  const confirmationMember = useMemo(
    () => confirmation
      ? members.find((member) => member.membershipId === confirmation.membershipId) ?? null
      : null,
    [confirmation, members],
  );
  const isEditorDirty = editor?.kind === "create"
    ? hasFormDraftChanged(organizationMemberDraft(), createDraft)
    : editor?.kind === "role"
      ? hasFormDraftChanged({ role: editor.baselineRole }, { role: roleDraft })
      : false;

  const activeCount = members.filter((member) =>
    isOrganizationMembershipActive(member.status)).length;
  const suspendedCount = members.length - activeCount;
  const managerCount = members.filter((member) => {
    const label = getOrganizationMemberRoleLabel(member.role);
    return label === "Owner" || label === "Admin";
  }).length;

  async function refreshMembers() {
    if (!organizationId || !canRead) return;
    setIsRefreshing(true);
    setPageError(null);
    try {
      const nextMembers = orderMembers(await getOrganizationMembers(organizationId));
      setMembers(nextMembers);
      setSelectedMemberId((current) =>
        current && nextMembers.some((member) => member.membershipId === current)
          ? current
          : nextMembers[0]?.membershipId ?? null);
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

    async function loadInitialMembers() {
      if (!organizationId || !canRead) return;

      try {
        const nextMembers = orderMembers(await getOrganizationMembers(organizationId));
        if (!cancelled) {
          setMembers(nextMembers);
          setSelectedMemberId(nextMembers[0]?.membershipId ?? null);
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

    void loadInitialMembers();
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
    setDiscardOpen(false);
    setCreateDraft(organizationMemberDraft());
    setFormErrors([]);
    setModalError(null);
  }, []);

  const requestEditorClose = useCallback((source: FormCloseSource) => {
    if (!editor) return;
    const decision = resolveFormCloseDecision({
      source,
      isDirty: isEditorDirty,
      isSaving,
    });
    if (decision === "close") closeEditor();
    else if (decision === "confirm-discard") setDiscardOpen(true);
  }, [editor, isEditorDirty, isSaving, closeEditor]);

  useEffect(() => {
    if (!editor && !confirmation && !discardOpen) return;

    function handleKeyDown(event: KeyboardEvent) {
      if (event.key !== "Escape") return;
      if (discardOpen) {
        setDiscardOpen(false);
        return;
      }
      if (confirmation) {
        if (!isSaving) {
          setConfirmation(null);
          setModalError(null);
        }
        return;
      }
      requestEditorClose("escape");
    }

    document.addEventListener("keydown", handleKeyDown);
    return () => document.removeEventListener("keydown", handleKeyDown);
  }, [editor, confirmation, discardOpen, isSaving, requestEditorClose]);

  function openCreateEditor() {
    setCreateDraft(organizationMemberDraft());
    setFormErrors([]);
    setModalError(null);
    setNotice(null);
    setEditor({ kind: "create" });
  }

  function openRoleEditor(member: OrganizationMember) {
    const role = toAssignableRole(member.role);
    if (role === null) return;
    setRoleDraft(role);
    setFormErrors([]);
    setModalError(null);
    setNotice(null);
    setEditor({
      kind: "role",
      membershipId: member.membershipId,
      baselineRole: role,
    });
  }

  async function submitCreate(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!organizationId || editor?.kind !== "create") return;

    const validationErrors = validateOrganizationMemberDraft(createDraft);
    setFormErrors(validationErrors);
    if (validationErrors.length > 0) return;

    setIsSaving(true);
    setModalError(null);
    try {
      const created = await createOrganizationMember(
        organizationId,
        toCreateOrganizationMemberRequest(createDraft),
      );
      setMembers((current) => replaceMember(current, created));
      setSelectedMemberId(created.membershipId);
      closeEditor();
      setNotice(`${created.email} berhasil ditambahkan sebagai ${getOrganizationMemberRoleLabel(created.role)}.`);
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

  async function submitRole(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!organizationId || editor?.kind !== "role" || !editorMember) return;

    if (roleDraft === editor.baselineRole) {
      closeEditor();
      return;
    }

    setIsSaving(true);
    setModalError(null);
    try {
      const updated = await changeOrganizationMemberRole(
        organizationId,
        editorMember.membershipId,
        toUpdateOrganizationMemberRoleRequest(roleDraft),
      );
      setMembers((current) => replaceMember(current, updated));
      setSelectedMemberId(updated.membershipId);
      closeEditor();
      setNotice(`Peran ${updated.email} diubah menjadi ${getOrganizationMemberRoleLabel(updated.role)}.`);
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

  async function confirmStatusChange() {
    if (!organizationId || !confirmation || !confirmationMember) return;

    setIsSaving(true);
    setModalError(null);
    try {
      const updated = await setOrganizationMembershipActive(
        organizationId,
        confirmationMember.membershipId,
        confirmation.nextActive,
      );
      setMembers((current) => replaceMember(current, updated));
      setSelectedMemberId(updated.membershipId);
      setConfirmation(null);
      setNotice(`${updated.email} berhasil ${confirmation.nextActive ? "diaktifkan" : "ditangguhkan"}.`);
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

  if (!organization || !organizationId) {
    return (
      <section className={styles.stateCard}>
        <Icon name="team" />
        <h1>Pilih organisasi terlebih dahulu</h1>
        <p>Daftar anggota tersedia setelah organisasi aktif dipilih.</p>
      </section>
    );
  }

  if (!canRead) {
    return (
      <section className={styles.stateCard}>
        <Icon name="shield" />
        <h1>Akses anggota dibatasi</h1>
        <p>Peran Anda tidak memiliki permission <code>members.read</code>.</p>
      </section>
    );
  }

  if (isLoading) {
    return (
      <section className={styles.stateCard} aria-live="polite">
        <span className={styles.loader} />
        <h1>Memuat anggota organisasi</h1>
        <p>SiPacul sedang menyiapkan status akun dan akses tim.</p>
      </section>
    );
  }

  return (
    <section className={styles.page}>
      <header className={styles.hero}>
        <div>
          <span className={styles.eyebrow}>Organisasi</span>
          <h1>Anggota &amp; Akses</h1>
          <p>Kelola tim <strong>{organization.name}</strong>, peran kerja, dan status keanggotaannya.</p>
        </div>
        <div className={styles.heroActions}>
          <button
            className={styles.secondaryButton}
            type="button"
            disabled={isRefreshing}
            onClick={() => void refreshMembers()}
          >
            <Icon name="refresh" />
            {isRefreshing ? "Memuat..." : "Muat ulang"}
          </button>
          {canManage && (
            <button className={styles.primaryButton} type="button" onClick={openCreateEditor}>
              <Icon name="add" />
              Tambah anggota
            </button>
          )}
        </div>
      </header>

      {notice && <div className={styles.notice} role="status"><strong>Berhasil.</strong> {notice}</div>}
      {pageError && <div className={styles.errorAlert} role="alert"><strong>Data belum diperbarui.</strong> {pageError}</div>}
      {!canManage && (
        <div className={styles.infoAlert}>
          <strong>Akses baca saja.</strong> Peran Anda dapat melihat anggota, tetapi tidak dapat mengubah peran atau status.
        </div>
      )}

      <div className={styles.metrics} aria-label="Ringkasan anggota">
        <article><span>Total anggota</span><strong>{members.length}</strong></article>
        <article><span>Keanggotaan aktif</span><strong>{activeCount}</strong></article>
        <article><span>Ditangguhkan</span><strong>{suspendedCount}</strong></article>
        <article><span>Pengelola akses</span><strong>{managerCount}</strong></article>
      </div>

      <div className={styles.filters}>
        <label className={styles.searchField}>
          <span className={styles.srOnly}>Cari anggota</span>
          <Icon name="search" />
          <input
            type="search"
            value={query}
            placeholder="Cari email, peran, atau status..."
            onChange={(event) => setQuery(event.target.value)}
          />
        </label>
        <label>
          <span className={styles.srOnly}>Filter peran</span>
          <select
            aria-label="Filter peran anggota"
            value={roleFilter}
            onChange={(event) => setRoleFilter(event.target.value as OrganizationMemberRoleFilter)}
          >
            <option value="all">Semua peran</option>
            <option value="owner">Owner</option>
            <option value="admin">Admin</option>
            <option value="finance">Finance</option>
            <option value="operator">Operator</option>
          </select>
        </label>
        <label>
          <span className={styles.srOnly}>Filter status</span>
          <select
            aria-label="Filter status anggota"
            value={statusFilter}
            onChange={(event) => setStatusFilter(event.target.value as OrganizationMemberStatusFilter)}
          >
            <option value="all">Semua status</option>
            <option value="active">Aktif</option>
            <option value="suspended">Ditangguhkan</option>
          </select>
        </label>
      </div>

      <div className={styles.workspace}>
        <aside className={styles.catalog}>
          <div className={styles.panelTitle}>
            <div><span>Daftar anggota</span><small>{filteredMembers.length} dari {members.length} anggota</small></div>
          </div>
          <div className={styles.memberList}>
            {filteredMembers.map((member) => {
              const isActive = isOrganizationMembershipActive(member.status);
              const isCurrent = member.membershipId === currentMembershipId;
              return (
                <button
                  className={`${styles.memberCard} ${selectedMember?.membershipId === member.membershipId ? styles.memberCardActive : ""}`}
                  type="button"
                  aria-pressed={selectedMember?.membershipId === member.membershipId}
                  key={member.membershipId}
                  onClick={() => setSelectedMemberId(member.membershipId)}
                >
                  <span className={styles.avatar}>{memberInitial(member.email)}</span>
                  <span className={styles.memberCardCopy}>
                    <strong>{member.email}</strong>
                    <small>{getOrganizationMemberRoleLabel(member.role)} · {getOrganizationMemberAccountLabel(member)}</small>
                  </span>
                  <span className={styles.memberCardBadges}>
                    {isCurrent && <span className={styles.selfBadge}>Anda</span>}
                    <span className={isActive ? styles.statusActive : styles.statusSuspended}>
                      {getOrganizationMemberStatusLabel(member.status)}
                    </span>
                  </span>
                </button>
              );
            })}
            {filteredMembers.length === 0 && (
              <div className={styles.emptySmall}>
                <strong>Tidak ada anggota yang cocok</strong>
                <span>Ubah kata kunci atau filter untuk melihat hasil lain.</span>
              </div>
            )}
          </div>
        </aside>

        <article className={styles.detail}>
          {selectedMember ? (() => {
            const membershipActive = isOrganizationMembershipActive(selectedMember.status);
            const ownerProtected = isOrganizationOwner(selectedMember);
            const selfProtected = selectedMember.membershipId === currentMembershipId;
            const mutationProtected = ownerProtected || selfProtected;
            return (
              <>
                <div className={styles.detailHeader}>
                  <div className={styles.identity}>
                    <span className={styles.detailAvatar}>{memberInitial(selectedMember.email)}</span>
                    <div>
                      <span className={styles.eyebrow}>Profil anggota</span>
                      <h2>{selectedMember.email}</h2>
                      <div className={styles.detailBadges}>
                        <span className={styles.roleBadge}><Icon name="shield" />{getOrganizationMemberRoleLabel(selectedMember.role)}</span>
                        <span className={membershipActive ? styles.statusActive : styles.statusSuspended}>
                          {getOrganizationMemberStatusLabel(selectedMember.status)}
                        </span>
                        {selfProtected && <span className={styles.selfBadge}>Akun Anda</span>}
                      </div>
                    </div>
                  </div>
                  {canManage && !mutationProtected && (
                    <div className={styles.detailActions}>
                      <button className={styles.secondaryButton} type="button" onClick={() => openRoleEditor(selectedMember)}>
                        <Icon name="edit" /> Ubah peran
                      </button>
                      <button
                        className={membershipActive ? styles.dangerButton : styles.successButton}
                        type="button"
                        onClick={() => {
                          setModalError(null);
                          setNotice(null);
                          setConfirmation({
                            membershipId: selectedMember.membershipId,
                            nextActive: !membershipActive,
                          });
                        }}
                      >
                        <Icon name={membershipActive ? "pause" : "play"} />
                        {membershipActive ? "Tangguhkan" : "Aktifkan"}
                      </button>
                    </div>
                  )}
                </div>

                {canManage && mutationProtected && (
                  <div className={styles.protectionPanel}>
                    <Icon name="shield" />
                    <div>
                      <strong>{ownerProtected ? "Keanggotaan Owner dilindungi" : "Keanggotaan Anda dilindungi di halaman ini"}</strong>
                      <p>{ownerProtected
                        ? "Peran dan status Owner hanya dapat diubah melalui alur transfer kepemilikan khusus."
                        : "Untuk mencegah kehilangan akses, minta Owner atau Admin lain mengubah keanggotaan Anda."}</p>
                    </div>
                  </div>
                )}

                <div className={styles.factGrid}>
                  <div><span><Icon name="mail" /> Status email</span><strong>{selectedMember.emailConfirmed ? "Terkonfirmasi" : "Belum dikonfirmasi"}</strong></div>
                  <div><span><Icon name="user" /> Status akun</span><strong>{selectedMember.userIsActive ? "Aktif" : "Nonaktif"}</strong></div>
                  <div><span><Icon name="team" /> Bergabung</span><strong>{formatDate(selectedMember.joinedAt)}</strong></div>
                  <div><span><Icon name="pause" /> Ditangguhkan</span><strong>{formatDate(selectedMember.suspendedAt)}</strong></div>
                </div>

                <section className={styles.accessSummary}>
                  <div className={styles.sectionHeading}>
                    <span className={styles.sectionIcon}><Icon name="key" /></span>
                    <div><h3>Ringkasan akses</h3><p>Akses efektif ditentukan oleh peran pada organisasi aktif.</p></div>
                  </div>
                  <div className={styles.accessCard}>
                    <div><span>Peran organisasi</span><strong>{getOrganizationMemberRoleLabel(selectedMember.role)}</strong></div>
                    <div><span>Status keanggotaan</span><strong>{getOrganizationMemberStatusLabel(selectedMember.status)}</strong></div>
                    <div><span>Kondisi akun</span><strong>{getOrganizationMemberAccountLabel(selectedMember)}</strong></div>
                  </div>
                </section>
              </>
            );
          })() : (
            <div className={styles.emptyDetail}>
              <Icon name="team" />
              <h2>Pilih anggota</h2>
              <p>Pilih satu anggota dari daftar untuk melihat peran dan status aksesnya.</p>
            </div>
          )}
        </article>
      </div>

      {editor && (
        <div
          className={styles.modalBackdrop}
          role="presentation"
          onMouseDown={(event) => {
            if (event.currentTarget === event.target) requestEditorClose("backdrop");
          }}
        >
          <div className={styles.modalPanel} role="dialog" aria-modal="true" aria-labelledby="member-editor-title">
            {editor.kind === "create" ? (
              <form className={styles.editorForm} onSubmit={(event) => void submitCreate(event)} noValidate>
                <div className={styles.modalHeader}>
                  <div className={styles.modalHeading}>
                    <span className={styles.modalIcon}><Icon name="add" /></span>
                    <div><span className={styles.eyebrow}>Anggota baru</span><h2 id="member-editor-title">Tambah anggota organisasi</h2><p>Hubungkan akun yang sudah ada atau buat akun baru dengan password awal.</p></div>
                  </div>
                  <button className={styles.iconButton} type="button" aria-label="Tutup formulir anggota" disabled={isSaving} onClick={() => requestEditorClose("explicit")}><Icon name="close" /></button>
                </div>

                {(formErrors.length > 0 || modalError) && (
                  <div className={styles.errorPanel} role="alert">
                    <strong>Periksa data anggota.</strong>
                    {modalError && <span>{modalError}</span>}
                    {formErrors.length > 0 && <ul>{formErrors.map((error) => <li key={error}>{error}</li>)}</ul>}
                  </div>
                )}

                <div className={styles.formGrid}>
                  <label className={styles.fieldFull}>
                    <span>Email anggota <b>*</b></span>
                    <input
                      type="email"
                      autoComplete="email"
                      maxLength={256}
                      value={createDraft.email}
                      disabled={isSaving}
                      placeholder="anggota@perusahaan.id"
                      onChange={(event) => setCreateDraft((current) => ({ ...current, email: event.target.value }))}
                    />
                    <small>Email menjadi identitas akun dan selalu dinormalisasi ke huruf kecil.</small>
                  </label>
                  <label className={styles.fieldFull}>
                    <span>Peran <b>*</b></span>
                    <select
                      value={createDraft.role}
                      disabled={isSaving}
                      onChange={(event) => setCreateDraft((current) => ({
                        ...current,
                        role: Number(event.target.value) as AssignableOrganizationRole,
                      }))}
                    >
                      {assignableOrganizationMemberRoles.map((role) => <option value={role.value} key={role.value}>{role.label}</option>)}
                    </select>
                    <small>{assignableOrganizationMemberRoles.find((role) => role.value === createDraft.role)?.description}</small>
                  </label>
                  <label>
                    <span>Password awal <em>opsional</em></span>
                    <input
                      type="password"
                      autoComplete="new-password"
                      maxLength={1024}
                      value={createDraft.initialPassword}
                      disabled={isSaving}
                      onChange={(event) => setCreateDraft((current) => ({ ...current, initialPassword: event.target.value }))}
                    />
                    <small>Wajib hanya jika email belum memiliki akun SiPacul.</small>
                  </label>
                  <label>
                    <span>Konfirmasi password</span>
                    <input
                      type="password"
                      autoComplete="new-password"
                      maxLength={1024}
                      value={createDraft.confirmInitialPassword}
                      disabled={isSaving}
                      onChange={(event) => setCreateDraft((current) => ({ ...current, confirmInitialPassword: event.target.value }))}
                    />
                    <small>Minimal 12 karakter dengan huruf besar, kecil, angka, dan simbol.</small>
                  </label>
                </div>

                <div className={styles.passwordNote}>
                  <Icon name="key" />
                  <p><strong>Akun yang sudah ada:</strong> kosongkan password awal. <strong>Akun baru:</strong> isi password awal yang memenuhi kebijakan keamanan.</p>
                </div>

                <div className={styles.modalFooter}>
                  <button className={styles.secondaryButton} type="button" disabled={isSaving} onClick={() => requestEditorClose("explicit")}>Batal</button>
                  <button className={styles.primaryButton} type="submit" disabled={isSaving}>{isSaving ? "Menambahkan..." : "Tambah anggota"}</button>
                </div>
              </form>
            ) : editorMember ? (
              <form className={styles.editorForm} onSubmit={(event) => void submitRole(event)}>
                <div className={styles.modalHeader}>
                  <div className={styles.modalHeading}>
                    <span className={styles.modalIcon}><Icon name="edit" /></span>
                    <div><span className={styles.eyebrow}>Peran anggota</span><h2 id="member-editor-title">Ubah peran</h2><p>{editorMember.email}</p></div>
                  </div>
                  <button className={styles.iconButton} type="button" aria-label="Tutup formulir peran" disabled={isSaving} onClick={() => requestEditorClose("explicit")}><Icon name="close" /></button>
                </div>
                {modalError && <div className={styles.errorPanel} role="alert"><strong>Peran belum diubah.</strong><span>{modalError}</span></div>}
                <div className={styles.roleOptions}>
                  {assignableOrganizationMemberRoles.map((role) => (
                    <label className={roleDraft === role.value ? styles.roleOptionActive : styles.roleOption} key={role.value}>
                      <input type="radio" name="member-role" value={role.value} checked={roleDraft === role.value} disabled={isSaving} onChange={() => setRoleDraft(role.value)} />
                      <span><strong>{role.label}</strong><small>{role.description}</small></span>
                    </label>
                  ))}
                </div>
                <div className={styles.infoAlert}><strong>Owner tidak tersedia.</strong> Transfer kepemilikan membutuhkan alur khusus dan tidak dilakukan dari halaman ini.</div>
                <div className={styles.modalFooter}>
                  <button className={styles.secondaryButton} type="button" disabled={isSaving} onClick={() => requestEditorClose("explicit")}>Batal</button>
                  <button className={styles.primaryButton} type="submit" disabled={isSaving || !isEditorDirty}>{isSaving ? "Menyimpan..." : "Simpan peran"}</button>
                </div>
              </form>
            ) : (
              <div className={styles.editorForm}>
                <div className={styles.errorPanel} role="alert"><strong>Anggota tidak tersedia.</strong><span>Tutup formulir dan muat ulang daftar anggota.</span></div>
                <button className={styles.secondaryButton} type="button" onClick={closeEditor}>Tutup</button>
              </div>
            )}
          </div>
        </div>
      )}

      {confirmation && confirmationMember && (
        <div className={styles.modalBackdrop} role="presentation">
          <div className={styles.confirmDialog} role="dialog" aria-modal="true" aria-labelledby="status-confirm-title">
            <span className={confirmation.nextActive ? styles.confirmIconSuccess : styles.confirmIconDanger}>
              <Icon name={confirmation.nextActive ? "play" : "warning"} />
            </span>
            <span className={styles.eyebrow}>Konfirmasi status</span>
            <h2 id="status-confirm-title">{confirmation.nextActive ? "Aktifkan keanggotaan?" : "Tangguhkan keanggotaan?"}</h2>
            <p>{confirmation.nextActive
              ? `${confirmationMember.email} akan kembali memperoleh akses sesuai perannya.`
              : `${confirmationMember.email} tidak dapat memakai akses organisasi sampai diaktifkan kembali.`}</p>
            {modalError && <div className={styles.errorPanel} role="alert"><span>{modalError}</span></div>}
            <div className={styles.modalFooter}>
              <button className={styles.secondaryButton} type="button" disabled={isSaving} onClick={() => { setConfirmation(null); setModalError(null); }}>Batal</button>
              <button className={confirmation.nextActive ? styles.successButton : styles.dangerButton} type="button" disabled={isSaving} onClick={() => void confirmStatusChange()}>
                {isSaving ? "Memproses..." : confirmation.nextActive ? "Ya, aktifkan" : "Ya, tangguhkan"}
              </button>
            </div>
          </div>
        </div>
      )}

      {discardOpen && (
        <div className={styles.modalBackdrop} role="presentation">
          <div className={styles.confirmDialog} role="dialog" aria-modal="true" aria-labelledby="discard-title">
            <span className={styles.confirmIconDanger}><Icon name="warning" /></span>
            <span className={styles.eyebrow}>Perubahan belum disimpan</span>
            <h2 id="discard-title">Buang perubahan formulir?</h2>
            <p>Data yang baru Anda isi akan hilang dan tidak dapat dipulihkan.</p>
            <div className={styles.modalFooter}>
              <button className={styles.secondaryButton} type="button" onClick={() => setDiscardOpen(false)}>Kembali mengedit</button>
              <button className={styles.dangerButton} type="button" onClick={closeEditor}>Buang perubahan</button>
            </div>
          </div>
        </div>
      )}
    </section>
  );
}
