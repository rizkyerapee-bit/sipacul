"use client";

import {
  useCallback,
  useEffect,
  useMemo,
  useState,
  type DragEvent,
  type FormEvent,
} from "react";
import {
  ApiError,
  activateProfitSharingScheme,
  createNextProfitSharingSchemeVersion,
  createProfitSharingScheme,
  getProfitSharingSchemes,
  updateProfitSharingScheme,
} from "@/lib/api/client";
import type {
  ProfitSharingParticipantRole,
  ProfitSharingPriorityRuleType,
  ProfitSharingResidualMethod,
  ProfitSharingScheme,
} from "@/lib/api/contracts";
import {
  buildCreateProfitSharingSchemeRequest,
  buildUpdateProfitSharingSchemeRequest,
  createProfitSharingSchemeDraft,
  filterProfitSharingSchemes,
  formatProfitSharingRate,
  moveProfitSharingSchemeItem,
  profitSharingParticipantRoleLabels,
  profitSharingPriorityRuleTypeLabels,
  profitSharingResidualMethodLabels,
  profitSharingSchemeDraftFrom,
  profitSharingSchemePresetLabels,
  profitSharingSchemeStatusLabels,
  profitSharingSchemeUsesPassiveInvestor,
  summarizeProfitSharingSchemes,
  validateProfitSharingSchemeDraft,
  type ProfitSharingSchemeDraft,
  type ProfitSharingSchemePreset,
  type ProfitSharingSchemeStatusFilter,
} from "@/lib/finance/profit-sharing-v2-management";
import styles from "./profit-sharing-scheme-management.module.css";

type Props = {
  organizationId: string;
  canWrite: boolean;
  canActivate: boolean;
};

type EditorState = {
  schemeId: string | null;
  preset: ProfitSharingSchemePreset;
};

type ActionState = {
  kind: "activate" | "version";
  schemeId: string;
};

type DragItem = {
  section: "participants" | "rules" | "shares";
  index: number;
};

type IconName =
  | "add"
  | "arrowDown"
  | "arrowUp"
  | "check"
  | "close"
  | "copy"
  | "drag"
  | "edit"
  | "flow"
  | "refresh"
  | "search"
  | "trash"
  | "users";

const iconPaths: Record<IconName, string> = {
  add: "M12 5v14M5 12h14",
  arrowDown: "m7 10 5 5 5-5",
  arrowUp: "m7 14 5-5 5 5",
  check: "m5 12 4 4L19 6",
  close: "m6 6 12 12M18 6 6 18",
  copy: "M8 8h11v11H8V8Zm-3 8H4V4h12v1",
  drag: "M8 7h.01M8 12h.01M8 17h.01M16 7h.01M16 12h.01M16 17h.01",
  edit: "m4 20 4.5-1 10-10a2.1 2.1 0 0 0-3-3l-10 10L4 20Zm10-12 3 3",
  flow: "M5 5h5v5H5V5Zm9 9h5v5h-5v-5Zm-4-6h3a3 3 0 0 1 3 3v3M8 10v4a3 3 0 0 0 3 3h3",
  refresh: "M20 6v5h-5M4 18v-5h5m10-2a7 7 0 0 0-12-4L4 11m16 2-3 4a7 7 0 0 1-12-4",
  search: "m21 21-4.35-4.35m2.35-5.65a8 8 0 1 1-16 0 8 8 0 0 1 16 0Z",
  trash: "M4 7h16M9 7V4h6v3m-9 0 1 14h10l1-14M10 11v6m4-6v6",
  users: "M9 11a4 4 0 1 0 0-8 4 4 0 0 0 0 8Zm7-1a3 3 0 1 0 0-6M2 21c0-4 3-7 7-7s7 3 7 7m1-7c3 0 5 2 5 5",
};

function Icon({ name }: { name: IconName }) {
  return <svg viewBox="0 0 24 24" aria-hidden="true"><path d={iconPaths[name]} /></svg>;
}

function friendlyError(error: unknown): string {
  if (!(error instanceof ApiError)) {
    return error instanceof Error ? error.message : "Permintaan tidak dapat diselesaikan.";
  }

  const messages: Record<string, string> = {
    "ProfitSharingSchemes.Validation": "Isi skema belum memenuhi aturan waterfall.",
    "ProfitSharingSchemes.NotFound": "Skema tidak ditemukan atau sudah berubah.",
    "ProfitSharingSchemes.CodeAlreadyExists": "Kode skema sudah digunakan dalam organisasi ini.",
    "ProfitSharingSchemes.DraftAlreadyExists": "Keluarga skema ini sudah memiliki versi draf.",
    "ProfitSharingSchemes.InvalidStatusTransition": "Tindakan tidak sesuai dengan status skema saat ini.",
    "ProfitSharingSchemes.ConcurrencyConflict": "Skema berubah pada saat yang sama. Muat ulang lalu coba lagi.",
  };

  return messages[error.problem?.code ?? ""] ?? error.message;
}

function formatDate(value: string | null): string {
  if (!value) return "—";
  return new Intl.DateTimeFormat("id-ID", {
    day: "2-digit",
    month: "short",
    year: "numeric",
  }).format(new Date(value));
}

function replaceScheme(items: ProfitSharingScheme[], updated: ProfitSharingScheme) {
  return items.some((item) => item.id === updated.id)
    ? items.map((item) => item.id === updated.id ? updated : item)
    : [updated, ...items];
}

function draftRate(numerator: string, denominator: string): string {
  const left = Number(numerator.replace(",", "."));
  const right = Number(denominator.replace(",", "."));
  return Number.isFinite(left) && Number.isFinite(right) && left > 0 && right > 0
    ? formatProfitSharingRate(left, right)
    : "—";
}

function nextAvailableCode(prefix: string, codes: string[]): string {
  let number = 1;
  let candidate = `${prefix}-${number}`;
  const used = new Set(codes.map((code) => code.trim().toUpperCase()));
  while (used.has(candidate)) {
    number += 1;
    candidate = `${prefix}-${number}`;
  }
  return candidate;
}

function PresetPicker({
  onClose,
  onSelect,
}: {
  onClose: () => void;
  onSelect: (preset: ProfitSharingSchemePreset) => void;
}) {
  const descriptions: Record<ProfitSharingSchemePreset, string> = {
    internal: "Modal dan pengelolaan 100% perusahaan; seluruh keuntungan untuk perusahaan.",
    managed: "Biaya pengelolaan mitra dipotong lebih dulu, lalu laba tersisa dibagi menurut modal.",
    "passive-investor": "Perusahaan dan investor pasif berbagi laba tersisa secara proporsional terhadap modal.",
  };

  return (
    <section className={styles.presetDialog}>
      <header className={styles.dialogHeader}>
        <span className={styles.dialogIcon}><Icon name="flow" /></span>
        <div>
          <span className={styles.eyebrow}>Pola awal yang dapat diubah</span>
          <h2>Pilih dasar skema</h2>
          <p>Preset hanya mengisi titik awal. Peserta, urutan, tarif, dan pembagian sisa tetap dapat dikustom.</p>
        </div>
        <button className={styles.iconButton} type="button" aria-label="Tutup pilihan preset" onClick={onClose}><Icon name="close" /></button>
      </header>
      <div className={styles.presetGrid}>
        {(Object.keys(profitSharingSchemePresetLabels) as ProfitSharingSchemePreset[]).map((preset) => (
          <button className={styles.presetCard} type="button" key={preset} onClick={() => onSelect(preset)}>
            <span><Icon name={preset === "internal" ? "check" : "users"} /></span>
            <strong>{profitSharingSchemePresetLabels[preset]}</strong>
            <small>{descriptions[preset]}</small>
          </button>
        ))}
      </div>
      <footer className={styles.dialogFooter}>
        <button className={styles.secondaryButton} type="button" onClick={onClose}>Batal</button>
      </footer>
    </section>
  );
}

function SchemeEditor({
  scheme,
  preset,
  isSaving,
  apiError,
  onClose,
  onSubmit,
}: {
  scheme: ProfitSharingScheme | null;
  preset: ProfitSharingSchemePreset;
  isSaving: boolean;
  apiError: string | null;
  onClose: () => void;
  onSubmit: (draft: ProfitSharingSchemeDraft) => Promise<void>;
}) {
  const isCreate = scheme === null;
  const [draft, setDraft] = useState<ProfitSharingSchemeDraft>(() =>
    scheme ? profitSharingSchemeDraftFrom(scheme) : createProfitSharingSchemeDraft(preset));
  const [errors, setErrors] = useState<string[]>([]);
  const [dragItem, setDragItem] = useState<DragItem | null>(null);

  const participantOptions = draft.participants.map((participant) => ({
    code: participant.participantCode,
    label: participant.participantName || participant.participantCode,
  }));

  function update<Key extends keyof ProfitSharingSchemeDraft>(
    key: Key,
    value: ProfitSharingSchemeDraft[Key],
  ) {
    setDraft((current) => ({ ...current, [key]: value }));
  }

  function updateParticipant(index: number, field: string, value: string | boolean | number) {
    setDraft((current) => {
      const previousCode = current.participants[index].participantCode;
      const participants = current.participants.map((participant, participantIndex) =>
        participantIndex === index ? { ...participant, [field]: value } : participant);
      if (field !== "participantCode") return { ...current, participants };

      const nextCode = String(value);
      return {
        ...current,
        participants,
        priorityRules: current.priorityRules.map((rule) =>
          rule.recipientCode === previousCode ? { ...rule, recipientCode: nextCode } : rule),
        residualRecipientCode: current.residualRecipientCode === previousCode
          ? nextCode
          : current.residualRecipientCode,
        residualShares: current.residualShares.map((share) =>
          share.recipientCode === previousCode ? { ...share, recipientCode: nextCode } : share),
      };
    });
  }

  function addParticipant() {
    setDraft((current) => ({
      ...current,
      participants: [
        ...current.participants,
        {
          participantCode: nextAvailableCode(
            "PESERTA",
            current.participants.map((participant) => participant.participantCode),
          ),
          participantName: "Peserta baru",
          participantRole: 4,
          participatesInResidualProfit: true,
          sequence: current.participants.length + 1,
        },
      ],
    }));
  }

  function removeParticipant(index: number) {
    setDraft((current) => {
      if (current.participants.length <= 1) return current;
      const removedCode = current.participants[index].participantCode;
      const participants = current.participants
        .filter((_, participantIndex) => participantIndex !== index)
        .map((participant, participantIndex) => ({ ...participant, sequence: participantIndex + 1 }));

      return {
        ...current,
        participants,
        priorityRules: current.priorityRules
          .filter((rule) => rule.recipientCode !== removedCode)
          .map((rule, ruleIndex) => ({ ...rule, sequence: ruleIndex + 1 })),
        residualRecipientCode: current.residualRecipientCode === removedCode
          ? participants[0]?.participantCode ?? ""
          : current.residualRecipientCode,
        residualShares: current.residualShares
          .filter((share) => share.recipientCode !== removedCode)
          .map((share, shareIndex) => ({ ...share, sequence: shareIndex + 1 })),
      };
    });
  }

  function addRule() {
    setDraft((current) => ({
      ...current,
      priorityRules: [
        ...current.priorityRules,
        {
          ruleCode: nextAvailableCode(
            "ATURAN",
            current.priorityRules.map((rule) => rule.ruleCode),
          ),
          ruleType: 1,
          recipientCode: current.participants[0]?.participantCode ?? "",
          rateNumerator: "1",
          rateDenominator: "3",
          sequence: current.priorityRules.length + 1,
        },
      ],
    }));
  }

  function updateRule(index: number, field: string, value: string | number) {
    setDraft((current) => ({
      ...current,
      priorityRules: current.priorityRules.map((rule, ruleIndex) =>
        ruleIndex === index ? { ...rule, [field]: value } : rule),
    }));
  }

  function removeRule(index: number) {
    setDraft((current) => ({
      ...current,
      priorityRules: current.priorityRules
        .filter((_, ruleIndex) => ruleIndex !== index)
        .map((rule, ruleIndex) => ({ ...rule, sequence: ruleIndex + 1 })),
    }));
  }

  function changeResidualMethod(method: ProfitSharingResidualMethod) {
    setDraft((current) => {
      if (method === 1) {
        return {
          ...current,
          residualMethod: method,
          residualRecipientCode: current.participants[0]?.participantCode ?? "",
          residualShares: [],
        };
      }

      if (method === 2) {
        const hasEligible = current.participants.some((participant) =>
          participant.participatesInResidualProfit);
        return {
          ...current,
          residualMethod: method,
          residualRecipientCode: "",
          residualShares: [],
          participants: hasEligible
            ? current.participants
            : current.participants.map((participant, index) => ({
              ...participant,
              participatesInResidualProfit: index === 0,
            })),
        };
      }

      const denominator = String(Math.max(current.participants.length, 1));
      return {
        ...current,
        residualMethod: method,
        residualRecipientCode: "",
        residualShares: current.participants.map((participant, index) => ({
          recipientCode: participant.participantCode,
          rateNumerator: "1",
          rateDenominator: denominator,
          sequence: index + 1,
        })),
      };
    });
  }

  function addResidualShare() {
    setDraft((current) => {
      const used = new Set(current.residualShares.map((share) => share.recipientCode));
      const participant = current.participants.find((item) => !used.has(item.participantCode));
      if (!participant) return current;
      return {
        ...current,
        residualShares: [
          ...current.residualShares,
          {
            recipientCode: participant.participantCode,
            rateNumerator: "1",
            rateDenominator: "1",
            sequence: current.residualShares.length + 1,
          },
        ],
      };
    });
  }

  function updateResidualShare(index: number, field: string, value: string) {
    setDraft((current) => ({
      ...current,
      residualShares: current.residualShares.map((share, shareIndex) =>
        shareIndex === index ? { ...share, [field]: value } : share),
    }));
  }

  function removeResidualShare(index: number) {
    setDraft((current) => ({
      ...current,
      residualShares: current.residualShares
        .filter((_, shareIndex) => shareIndex !== index)
        .map((share, shareIndex) => ({ ...share, sequence: shareIndex + 1 })),
    }));
  }

  function move(section: DragItem["section"], sourceIndex: number, destinationIndex: number) {
    setDraft((current) => {
      if (section === "participants") {
        return {
          ...current,
          participants: moveProfitSharingSchemeItem(
            current.participants,
            sourceIndex,
            destinationIndex,
          ),
        };
      }
      if (section === "rules") {
        return {
          ...current,
          priorityRules: moveProfitSharingSchemeItem(
            current.priorityRules,
            sourceIndex,
            destinationIndex,
          ),
        };
      }
      return {
        ...current,
        residualShares: moveProfitSharingSchemeItem(
          current.residualShares,
          sourceIndex,
          destinationIndex,
        ),
      };
    });
  }

  function drop(event: DragEvent<HTMLElement>, section: DragItem["section"], index: number) {
    event.preventDefault();
    if (dragItem?.section === section) move(section, dragItem.index, index);
    setDragItem(null);
  }

  function beginDrag(event: DragEvent<HTMLElement>, item: DragItem) {
    event.dataTransfer.effectAllowed = "move";
    event.dataTransfer.setData("text/plain", `${item.section}:${item.index}`);
    setDragItem(item);
  }

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const nextErrors = validateProfitSharingSchemeDraft(draft);
    setErrors(nextErrors);
    if (nextErrors.length === 0) void onSubmit(draft);
  }

  return (
    <form className={styles.editor} onSubmit={submit} noValidate>
      <header className={styles.dialogHeader}>
        <span className={styles.dialogIcon}><Icon name="flow" /></span>
        <div>
          <span className={styles.eyebrow}>SIPACUL-PS-2 · Editor waterfall</span>
          <h2>{isCreate ? "Buat skema bagi hasil" : `Ubah ${scheme.code} v${scheme.version}`}</h2>
          <p>Aturan dijalankan dari atas ke bawah. Gunakan drag-and-drop atau tombol panah untuk mengubah urutan.</p>
        </div>
        <button className={styles.iconButton} type="button" aria-label="Tutup editor" disabled={isSaving} onClick={onClose}><Icon name="close" /></button>
      </header>

      {(errors.length > 0 || apiError) && (
        <div className={styles.formAlert} role="alert">
          <strong>Periksa kembali skema berikut:</strong>
          <ul>{errors.map((error) => <li key={error}>{error}</li>)}{apiError && <li>{apiError}</li>}</ul>
        </div>
      )}

      <fieldset disabled={isSaving}>
        <section className={styles.editorSection}>
          <header className={styles.sectionHeader}>
            <div><span className={styles.step}>1</span><div><h3>Identitas skema</h3><p>Kode tetap sama pada seluruh versi dalam satu keluarga.</p></div></div>
          </header>
          <div className={styles.formGrid}>
            <label className={styles.field}>
              <span>Kode skema <em>*</em></span>
              <input value={draft.code} maxLength={40} disabled={!isCreate} placeholder="Contoh: MITRA-UTAMA" onChange={(event) => update("code", event.target.value.toUpperCase())} />
            </label>
            <label className={styles.field}>
              <span>Nama skema <em>*</em></span>
              <input value={draft.name} maxLength={150} onChange={(event) => update("name", event.target.value)} />
            </label>
            <label className={`${styles.field} ${styles.fieldFull}`}>
              <span>Deskripsi</span>
              <textarea value={draft.description} maxLength={1000} rows={3} onChange={(event) => update("description", event.target.value)} />
            </label>
          </div>
        </section>

        <section className={styles.editorSection}>
          <header className={styles.sectionHeader}>
            <div><span className={styles.step}>2</span><div><h3>Peserta</h3><p>Perusahaan, mitra pengelola, investor pasif, atau pihak lain.</p></div></div>
            <button className={styles.smallButton} type="button" onClick={addParticipant}><Icon name="add" /> Tambah peserta</button>
          </header>
          <div className={styles.reorderList}>
            {draft.participants.map((participant, index) => (
              <article
                className={styles.reorderCard}
                draggable={!isSaving}
                key={`${participant.sequence}-${index}`}
                onDragStart={(event) => beginDrag(event, { section: "participants", index })}
                onDragOver={(event) => event.preventDefault()}
                onDrop={(event) => drop(event, "participants", index)}
                onDragEnd={() => setDragItem(null)}
              >
                <div className={styles.dragHandle} title="Seret untuk mengubah urutan"><Icon name="drag" /><span>{index + 1}</span></div>
                <div className={styles.inlineFields}>
                  <label className={styles.field}><span>Kode</span><input value={participant.participantCode} maxLength={40} onChange={(event) => updateParticipant(index, "participantCode", event.target.value.toUpperCase())} /></label>
                  <label className={styles.field}><span>Nama peserta</span><input value={participant.participantName} maxLength={150} onChange={(event) => updateParticipant(index, "participantName", event.target.value)} /></label>
                  <label className={styles.field}><span>Peran</span><select value={participant.participantRole} onChange={(event) => updateParticipant(index, "participantRole", Number(event.target.value) as ProfitSharingParticipantRole)}>{Object.entries(profitSharingParticipantRoleLabels).map(([value, label]) => <option value={value} key={value}>{label}</option>)}</select></label>
                </div>
                <label className={styles.checkField}><input type="checkbox" checked={participant.participatesInResidualProfit} onChange={(event) => updateParticipant(index, "participatesInResidualProfit", event.target.checked)} /><span>Ikut laba tersisa</span></label>
                <div className={styles.rowActions}>
                  <button type="button" aria-label="Naikkan peserta" disabled={index === 0} onClick={() => move("participants", index, index - 1)}><Icon name="arrowUp" /></button>
                  <button type="button" aria-label="Turunkan peserta" disabled={index === draft.participants.length - 1} onClick={() => move("participants", index, index + 1)}><Icon name="arrowDown" /></button>
                  <button className={styles.deleteButton} type="button" aria-label="Hapus peserta" disabled={draft.participants.length === 1} onClick={() => removeParticipant(index)}><Icon name="trash" /></button>
                </div>
              </article>
            ))}
          </div>
        </section>

        <section className={styles.editorSection}>
          <header className={styles.sectionHeader}>
            <div><span className={styles.step}>3</span><div><h3>Potongan prioritas</h3><p>Biaya pengelolaan atau imbal hasil modal diproses sebelum laba tersisa.</p></div></div>
            <button className={styles.smallButton} type="button" onClick={addRule}><Icon name="add" /> Tambah aturan</button>
          </header>
          {draft.priorityRules.length === 0 ? (
            <div className={styles.inlineEmpty}>Tidak ada potongan awal. Seluruh laba langsung masuk ke metode pembagian sisa.</div>
          ) : (
            <div className={styles.reorderList}>
              {draft.priorityRules.map((rule, index) => (
                <article
                  className={styles.reorderCard}
                  draggable={!isSaving}
                  key={`${rule.sequence}-${index}`}
                  onDragStart={(event) => beginDrag(event, { section: "rules", index })}
                  onDragOver={(event) => event.preventDefault()}
                  onDrop={(event) => drop(event, "rules", index)}
                  onDragEnd={() => setDragItem(null)}
                >
                  <div className={styles.dragHandle} title="Seret untuk mengubah urutan"><Icon name="drag" /><span>{index + 1}</span></div>
                  <div className={styles.ruleFields}>
                    <label className={styles.field}><span>Kode aturan</span><input value={rule.ruleCode} maxLength={40} onChange={(event) => updateRule(index, "ruleCode", event.target.value.toUpperCase())} /></label>
                    <label className={styles.field}><span>Jenis</span><select value={rule.ruleType} onChange={(event) => updateRule(index, "ruleType", Number(event.target.value) as ProfitSharingPriorityRuleType)}>{Object.entries(profitSharingPriorityRuleTypeLabels).map(([value, label]) => <option value={value} key={value}>{label}</option>)}</select></label>
                    <label className={styles.field}><span>Penerima</span><select value={rule.recipientCode} onChange={(event) => updateRule(index, "recipientCode", event.target.value)}>{participantOptions.map((option) => <option value={option.code} key={option.code}>{option.label} · {option.code}</option>)}</select></label>
                    <label className={styles.rateField}><span>Tarif pecahan</span><div><input aria-label="Pembilang" value={rule.rateNumerator} inputMode="decimal" onChange={(event) => updateRule(index, "rateNumerator", event.target.value)} /><b>/</b><input aria-label="Penyebut" value={rule.rateDenominator} inputMode="decimal" onChange={(event) => updateRule(index, "rateDenominator", event.target.value)} /><small>{draftRate(rule.rateNumerator, rule.rateDenominator)}</small></div></label>
                  </div>
                  <div className={styles.rowActions}>
                    <button type="button" aria-label="Naikkan aturan" disabled={index === 0} onClick={() => move("rules", index, index - 1)}><Icon name="arrowUp" /></button>
                    <button type="button" aria-label="Turunkan aturan" disabled={index === draft.priorityRules.length - 1} onClick={() => move("rules", index, index + 1)}><Icon name="arrowDown" /></button>
                    <button className={styles.deleteButton} type="button" aria-label="Hapus aturan" onClick={() => removeRule(index)}><Icon name="trash" /></button>
                  </div>
                </article>
              ))}
            </div>
          )}
        </section>

        <section className={styles.editorSection}>
          <header className={styles.sectionHeader}>
            <div><span className={styles.step}>4</span><div><h3>Pembagian laba tersisa</h3><p>Dijalankan setelah seluruh potongan prioritas selesai.</p></div></div>
          </header>
          <label className={`${styles.field} ${styles.methodField}`}><span>Metode pembagian</span><select value={draft.residualMethod} onChange={(event) => changeResidualMethod(Number(event.target.value) as ProfitSharingResidualMethod)}>{Object.entries(profitSharingResidualMethodLabels).map(([value, label]) => <option value={value} key={value}>{label}</option>)}</select></label>

          {draft.residualMethod === 1 && (
            <label className={`${styles.field} ${styles.methodField}`}><span>Penerima seluruh sisa laba</span><select value={draft.residualRecipientCode} onChange={(event) => update("residualRecipientCode", event.target.value)}>{participantOptions.map((option) => <option value={option.code} key={option.code}>{option.label} · {option.code}</option>)}</select></label>
          )}

          {draft.residualMethod === 2 && (
            <div className={styles.residualNotice}>Laba tersisa dibagi menurut porsi modal aktual hanya kepada peserta yang ditandai <strong>Ikut laba tersisa</strong>. Saat rugi, modal yang tersedia dikembalikan proporsional.</div>
          )}

          {draft.residualMethod === 3 && (
            <>
              <div className={styles.fixedHeader}><span>Rincian persentase tetap harus berjumlah tepat 100%.</span><button className={styles.smallButton} type="button" disabled={draft.residualShares.length >= draft.participants.length} onClick={addResidualShare}><Icon name="add" /> Tambah bagian</button></div>
              <div className={styles.reorderList}>
                {draft.residualShares.map((share, index) => (
                  <article
                    className={styles.reorderCard}
                    draggable={!isSaving}
                    key={`${share.sequence}-${index}`}
                    onDragStart={(event) => beginDrag(event, { section: "shares", index })}
                    onDragOver={(event) => event.preventDefault()}
                    onDrop={(event) => drop(event, "shares", index)}
                    onDragEnd={() => setDragItem(null)}
                  >
                    <div className={styles.dragHandle} title="Seret untuk mengubah urutan"><Icon name="drag" /><span>{index + 1}</span></div>
                    <div className={styles.shareFields}>
                      <label className={styles.field}><span>Penerima</span><select value={share.recipientCode} onChange={(event) => updateResidualShare(index, "recipientCode", event.target.value)}>{participantOptions.map((option) => <option value={option.code} key={option.code}>{option.label} · {option.code}</option>)}</select></label>
                      <label className={styles.rateField}><span>Tarif pecahan</span><div><input aria-label="Pembilang bagian" value={share.rateNumerator} inputMode="decimal" onChange={(event) => updateResidualShare(index, "rateNumerator", event.target.value)} /><b>/</b><input aria-label="Penyebut bagian" value={share.rateDenominator} inputMode="decimal" onChange={(event) => updateResidualShare(index, "rateDenominator", event.target.value)} /><small>{draftRate(share.rateNumerator, share.rateDenominator)}</small></div></label>
                    </div>
                    <div className={styles.rowActions}>
                      <button type="button" aria-label="Naikkan bagian" disabled={index === 0} onClick={() => move("shares", index, index - 1)}><Icon name="arrowUp" /></button>
                      <button type="button" aria-label="Turunkan bagian" disabled={index === draft.residualShares.length - 1} onClick={() => move("shares", index, index + 1)}><Icon name="arrowDown" /></button>
                      <button className={styles.deleteButton} type="button" aria-label="Hapus bagian" onClick={() => removeResidualShare(index)}><Icon name="trash" /></button>
                    </div>
                  </article>
                ))}
              </div>
            </>
          )}
        </section>
      </fieldset>

      <footer className={styles.editorFooter}>
        <span>Tarif disimpan sebagai pecahan agar nilai seperti 1/3 tidak dibulatkan lebih awal.</span>
        <div><button className={styles.secondaryButton} type="button" disabled={isSaving} onClick={onClose}>Batal</button><button className={styles.primaryButton} type="submit" disabled={isSaving}>{isSaving ? "Menyimpan..." : isCreate ? "Simpan draf" : "Simpan perubahan"}</button></div>
      </footer>
    </form>
  );
}

function ConfirmationDialog({
  action,
  scheme,
  isSaving,
  apiError,
  onClose,
  onConfirm,
}: {
  action: ActionState;
  scheme: ProfitSharingScheme;
  isSaving: boolean;
  apiError: string | null;
  onClose: () => void;
  onConfirm: () => Promise<void>;
}) {
  const isActivation = action.kind === "activate";
  return (
    <section className={styles.confirmDialog}>
      <span className={styles.confirmIcon}><Icon name={isActivation ? "check" : "copy"} /></span>
      <span className={styles.eyebrow}>{scheme.code} · Versi {scheme.version}</span>
      <h2>{isActivation ? "Aktifkan skema ini?" : "Buat versi draf berikutnya?"}</h2>
      <p>{isActivation
        ? "Setelah aktif, skema tidak dapat diedit. Versi aktif sebelumnya dalam keluarga yang sama akan ditandai sebagai digantikan."
        : "Seluruh peserta dan aturan akan disalin ke draf baru. Versi aktif tetap digunakan sampai draf baru diaktifkan."}</p>
      {apiError && <div className={styles.formAlert} role="alert">{apiError}</div>}
      <div className={styles.confirmActions}><button className={styles.secondaryButton} type="button" disabled={isSaving} onClick={onClose}>Kembali</button><button className={styles.primaryButton} type="button" disabled={isSaving} onClick={() => void onConfirm()}>{isSaving ? "Memproses..." : isActivation ? "Ya, aktifkan" : "Buat versi baru"}</button></div>
    </section>
  );
}

export function ProfitSharingSchemeManagement({
  organizationId,
  canWrite,
  canActivate,
}: Props) {
  const [schemes, setSchemes] = useState<ProfitSharingScheme[]>([]);
  const [query, setQuery] = useState("");
  const [status, setStatus] = useState<ProfitSharingSchemeStatusFilter>("all");
  const [isLoading, setIsLoading] = useState(true);
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [pageError, setPageError] = useState<string | null>(null);
  const [modalError, setModalError] = useState<string | null>(null);
  const [showPreset, setShowPreset] = useState(false);
  const [editor, setEditor] = useState<EditorState | null>(null);
  const [action, setAction] = useState<ActionState | null>(null);

  const summary = useMemo(() => summarizeProfitSharingSchemes(schemes), [schemes]);
  const filtered = useMemo(
    () => filterProfitSharingSchemes(schemes, query, status),
    [schemes, query, status],
  );

  const loadSchemes = useCallback(async (background = false) => {
    if (background) setIsRefreshing(true);
    else setIsLoading(true);
    setPageError(null);
    try {
      setSchemes(await getProfitSharingSchemes(organizationId));
    } catch (error) {
      setPageError(friendlyError(error));
    } finally {
      setIsLoading(false);
      setIsRefreshing(false);
    }
  }, [organizationId]);

  useEffect(() => {
    let cancelled = false;
    async function initialLoad() {
      setIsLoading(true);
      try {
        const result = await getProfitSharingSchemes(organizationId);
        if (!cancelled) setSchemes(result);
      } catch (error) {
        if (!cancelled) setPageError(friendlyError(error));
      } finally {
        if (!cancelled) setIsLoading(false);
      }
    }
    void initialLoad();
    return () => { cancelled = true; };
  }, [organizationId]);

  async function submitDraft(draft: ProfitSharingSchemeDraft) {
    if (!editor) return;
    setIsSaving(true);
    setModalError(null);
    try {
      const updated = editor.schemeId
        ? await updateProfitSharingScheme(
          organizationId,
          editor.schemeId,
          buildUpdateProfitSharingSchemeRequest(draft),
        )
        : await createProfitSharingScheme(
          organizationId,
          buildCreateProfitSharingSchemeRequest(draft),
        );
      setSchemes((current) => replaceScheme(current, updated));
      setEditor(null);
    } catch (error) {
      setModalError(friendlyError(error));
    } finally {
      setIsSaving(false);
    }
  }

  async function confirmAction() {
    if (!action) return;
    setIsSaving(true);
    setModalError(null);
    try {
      if (action.kind === "activate") {
        await activateProfitSharingScheme(organizationId, action.schemeId);
        setAction(null);
        await loadSchemes(true);
      } else {
        const created = await createNextProfitSharingSchemeVersion(
          organizationId,
          action.schemeId,
        );
        setSchemes((current) => replaceScheme(current, created));
        setAction(null);
        setEditor({ schemeId: created.id, preset: "internal" });
      }
    } catch (error) {
      setModalError(friendlyError(error));
    } finally {
      setIsSaving(false);
    }
  }

  const editingScheme = editor?.schemeId
    ? schemes.find((scheme) => scheme.id === editor.schemeId) ?? null
    : null;
  const actionScheme = action
    ? schemes.find((scheme) => scheme.id === action.schemeId) ?? null
    : null;

  return (
    <section className={styles.schemePage}>
      <div className={styles.introStrip}>
        <span><Icon name="flow" /></span>
        <div><strong>Skema organisasi yang berversi</strong><small>Skema aktif terkunci; perubahan selalu dibuat sebagai versi draf baru agar histori pembagian hasil tetap dapat diaudit.</small></div>
        <button type="button" disabled={isRefreshing} onClick={() => void loadSchemes(true)}><Icon name="refresh" /> {isRefreshing ? "Memuat..." : "Muat ulang"}</button>
      </div>

      {pageError && <div className={styles.pageError} role="alert">{pageError}</div>}

      <div className={styles.metricGrid}>
        <article><span>Keluarga skema</span><strong>{summary.families}</strong><small>{summary.total} versi tersimpan</small></article>
        <article className={styles.metricActive}><span>Skema aktif</span><strong>{summary.active}</strong><small>Siap dipilih untuk siklus</small></article>
        <article className={styles.metricDraft}><span>Draf</span><strong>{summary.draft}</strong><small>Dapat diedit dan diuji</small></article>
        <article><span>Versi digantikan</span><strong>{summary.superseded}</strong><small>Disimpan sebagai jejak audit</small></article>
      </div>

      <div className={styles.toolbar}>
        <label className={styles.searchField}><Icon name="search" /><input value={query} placeholder="Cari kode atau nama skema" onChange={(event) => setQuery(event.target.value)} /></label>
        <label className={styles.filterField}><span>Status</span><select value={status} onChange={(event) => setStatus(event.target.value === "all" ? "all" : Number(event.target.value) as ProfitSharingSchemeStatusFilter)}><option value="all">Semua status</option>{Object.entries(profitSharingSchemeStatusLabels).map(([value, label]) => <option value={value} key={value}>{label}</option>)}</select></label>
        {canWrite && <button className={styles.primaryButton} type="button" onClick={() => { setModalError(null); setShowPreset(true); }}><Icon name="add" /> Skema baru</button>}
      </div>

      <div className={styles.resultCount}>{filtered.length} dari {schemes.length} versi skema</div>

      {isLoading ? (
        <div className={styles.loadingState}><span /><strong>Memuat katalog skema...</strong><p>Menyiapkan peserta, aturan prioritas, dan metode pembagian laba tersisa.</p></div>
      ) : filtered.length === 0 ? (
        <div className={styles.emptyState}><span><Icon name="flow" /></span><h2>{schemes.length === 0 ? "Belum ada skema V2" : "Tidak ada skema yang cocok"}</h2><p>{schemes.length === 0 ? "Mulai dari salah satu preset, lalu sesuaikan peserta dan urutan waterfall." : "Ubah pencarian atau filter status untuk melihat versi lainnya."}</p>{canWrite && schemes.length === 0 && <button className={styles.primaryButton} type="button" onClick={() => setShowPreset(true)}><Icon name="add" /> Buat skema pertama</button>}</div>
      ) : (
        <div className={styles.schemeGrid}>
          {filtered.map((scheme) => (
            <article className={styles.schemeCard} key={scheme.id}>
              <header className={styles.cardHeader}>
                <div><span className={styles.eyebrow}>{scheme.code} · Versi {scheme.version}</span><h3>{scheme.name}</h3><p>{scheme.description || "Tanpa deskripsi."}</p></div>
                <span className={`${styles.statusBadge} ${styles[`status${scheme.status}`]}`}>{profitSharingSchemeStatusLabels[scheme.status]}</span>
              </header>
              <div className={styles.cardMeta}>
                <span><Icon name="users" /><b>{scheme.participants.length}</b> peserta</span>
                <span><Icon name="flow" /><b>{scheme.priorityRules.length}</b> potongan awal</span>
                {profitSharingSchemeUsesPassiveInvestor(scheme) && <em>Investor pasif</em>}
              </div>
              <div className={styles.participantChips}>{scheme.participants.toSorted((left, right) => left.sequence - right.sequence).map((participant) => <span key={participant.id}><b>{participant.participantName}</b><small>{profitSharingParticipantRoleLabels[participant.participantRole]}</small></span>)}</div>
              <section className={styles.waterfallPreview}>
                <span className={styles.eyebrow}>Urutan waterfall</span>
                {scheme.priorityRules.toSorted((left, right) => left.sequence - right.sequence).map((rule, index) => <div key={rule.id}><i>{index + 1}</i><span><strong>{profitSharingPriorityRuleTypeLabels[rule.ruleType]}</strong><small>{rule.recipientCode} · {formatProfitSharingRate(rule.rateNumerator, rule.rateDenominator)}</small></span></div>)}
                <div className={styles.residualStep}><i>{scheme.priorityRules.length + 1}</i><span><strong>{profitSharingResidualMethodLabels[scheme.residualMethod]}</strong><small>{scheme.residualMethod === 1 ? scheme.residualRecipientCode : scheme.residualMethod === 2 ? "Menurut modal aktual" : `${scheme.residualShares.length} bagian tetap`}</small></span></div>
              </section>
              <footer className={styles.cardFooter}>
                <small>{scheme.status === 2 ? `Aktif ${formatDate(scheme.activatedAt)}` : scheme.status === 3 ? `Digantikan ${formatDate(scheme.supersededAt)}` : `Dibuat ${formatDate(scheme.createdAt)}`}</small>
                <div>
                  {canWrite && scheme.status === 1 && <button type="button" onClick={() => { setModalError(null); setEditor({ schemeId: scheme.id, preset: "internal" }); }}><Icon name="edit" /> Ubah draf</button>}
                  {canActivate && scheme.status === 1 && <button className={styles.activateButton} type="button" onClick={() => { setModalError(null); setAction({ kind: "activate", schemeId: scheme.id }); }}><Icon name="check" /> Aktifkan</button>}
                  {canWrite && scheme.status === 2 && <button type="button" onClick={() => { setModalError(null); setAction({ kind: "version", schemeId: scheme.id }); }}><Icon name="copy" /> Versi baru</button>}
                </div>
              </footer>
            </article>
          ))}
        </div>
      )}

      {(showPreset || editor || action) && (
        <div className={styles.modalBackdrop} role="presentation" onMouseDown={(event) => { if (event.target === event.currentTarget && !isSaving) { setShowPreset(false); setEditor(null); setAction(null); } }}>
          <div className={`${styles.modalPanel} ${editor ? styles.editorPanel : ""}`} role="dialog" aria-modal="true">
            {showPreset && <PresetPicker onClose={() => setShowPreset(false)} onSelect={(preset) => { setShowPreset(false); setEditor({ schemeId: null, preset }); }} />}
            {editor && <SchemeEditor key={editor.schemeId ?? editor.preset} scheme={editingScheme} preset={editor.preset} isSaving={isSaving} apiError={modalError} onClose={() => setEditor(null)} onSubmit={submitDraft} />}
            {action && actionScheme && <ConfirmationDialog action={action} scheme={actionScheme} isSaving={isSaving} apiError={modalError} onClose={() => setAction(null)} onConfirm={confirmAction} />}
          </div>
        </div>
      )}
    </section>
  );
}
