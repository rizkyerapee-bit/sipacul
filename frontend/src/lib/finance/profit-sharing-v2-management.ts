import type {
  CreateProfitSharingSchemeRequest,
  CropCycleStatus,
  ProfitSharingParticipantRole,
  ProfitSharingPriorityRuleType,
  ProfitSharingResidualMethod,
  ProfitSharingScheme,
  ProfitSharingSchemeStatus,
  ProfitSharingPreview,
  ProfitSharingWaterfallSettlement,
  ProfitSharingWaterfallSettlementStatus,
  UpdateProfitSharingSchemeDraftRequest,
} from "@/lib/api/contracts";

export type ProfitSharingSchemePreset =
  | "internal"
  | "managed"
  | "passive-investor";

export type ProfitSharingSchemeStatusFilter = ProfitSharingSchemeStatus | "all";

export type ProfitSharingSchemeSummary = {
  total: number;
  families: number;
  draft: number;
  active: number;
  superseded: number;
};

export type ProfitSharingAssignmentAvailability = {
  allowed: boolean;
  replaceable: boolean;
  reason: string;
};

export type ProfitSharingPreviewSummary = {
  participantCount: number;
  fundedParticipantCount: number;
  unallocatedPriorityAmount: number;
  hasCapitalLoss: boolean;
  isPayoutReconciled: boolean;
};

export type ProfitSharingWaterfallSettlementStatusFilter =
  | ProfitSharingWaterfallSettlementStatus
  | "all";

export type ProfitSharingWaterfallSettlementDraft = {
  code: string;
  settlementDate: string;
  notes: string;
};

export type ProfitSharingWaterfallSettlementSummary = {
  total: number;
  finalized: number;
  voided: number;
  active: ProfitSharingWaterfallSettlement | null;
  latest: ProfitSharingWaterfallSettlement | null;
};

export type ProfitSharingWaterfallFinalizationAvailability = {
  allowed: boolean;
  reason: string;
};

export type ProfitSharingSchemeParticipantDraft = {
  participantCode: string;
  participantName: string;
  participantRole: ProfitSharingParticipantRole;
  participatesInResidualProfit: boolean;
  sequence: number;
};

export type ProfitSharingSchemePriorityRuleDraft = {
  ruleCode: string;
  ruleType: ProfitSharingPriorityRuleType;
  recipientCode: string;
  rateNumerator: string;
  rateDenominator: string;
  sequence: number;
};

export type ProfitSharingSchemeResidualShareDraft = {
  recipientCode: string;
  rateNumerator: string;
  rateDenominator: string;
  sequence: number;
};

export type ProfitSharingSchemeDraft = {
  code: string;
  name: string;
  description: string;
  participants: ProfitSharingSchemeParticipantDraft[];
  priorityRules: ProfitSharingSchemePriorityRuleDraft[];
  residualMethod: ProfitSharingResidualMethod;
  residualRecipientCode: string;
  residualShares: ProfitSharingSchemeResidualShareDraft[];
};

export const profitSharingParticipantRoleLabels: Record<
  ProfitSharingParticipantRole,
  string
> = {
  1: "Perusahaan",
  2: "Investor pasif",
  3: "Mitra pengelola",
  4: "Peran lainnya",
};

export const profitSharingPriorityRuleTypeLabels: Record<
  ProfitSharingPriorityRuleType,
  string
> = {
  1: "Biaya pengelolaan",
  2: "Imbal hasil modal",
};

export const profitSharingResidualMethodLabels: Record<
  ProfitSharingResidualMethod,
  string
> = {
  1: "Sisa ke satu peserta",
  2: "Proporsional terhadap modal",
  3: "Persentase tetap",
};

export const profitSharingSchemeStatusLabels: Record<
  ProfitSharingSchemeStatus,
  string
> = {
  1: "Draf",
  2: "Aktif",
  3: "Digantikan",
};

export const profitSharingWaterfallStatusLabels: Record<
  ProfitSharingWaterfallSettlementStatus,
  string
> = {
  1: "Final",
  2: "Dibatalkan",
};

export const profitSharingSchemePresetLabels: Record<
  ProfitSharingSchemePreset,
  string
> = {
  internal: "Internal perusahaan",
  managed: "Dikelola mitra",
  "passive-investor": "Perusahaan dan investor pasif",
};

export function summarizeProfitSharingSchemes(
  schemes: ProfitSharingScheme[],
): ProfitSharingSchemeSummary {
  return {
    total: schemes.length,
    families: new Set(schemes.map((scheme) => scheme.schemeFamilyId)).size,
    draft: schemes.filter((scheme) => scheme.status === 1).length,
    active: schemes.filter((scheme) => scheme.status === 2).length,
    superseded: schemes.filter((scheme) => scheme.status === 3).length,
  };
}

export function filterProfitSharingSchemes(
  schemes: ProfitSharingScheme[],
  query: string,
  status: ProfitSharingSchemeStatusFilter,
): ProfitSharingScheme[] {
  const normalizedQuery = query.trim().toLocaleLowerCase("id-ID");

  return schemes.filter((scheme) => {
    if (status !== "all" && scheme.status !== status) return false;
    if (!normalizedQuery) return true;

    return [scheme.code, scheme.name, scheme.description ?? ""]
      .some((value) => value.toLocaleLowerCase("id-ID").includes(normalizedQuery));
  });
}

export function profitSharingSchemeUsesPassiveInvestor(
  scheme: ProfitSharingScheme,
): boolean {
  return scheme.participants.some((participant) => participant.participantRole === 2);
}

export function profitSharingAssignmentAvailability(
  cropCycleStatus: CropCycleStatus,
  hasAssignment: boolean,
): ProfitSharingAssignmentAvailability {
  if (cropCycleStatus === 3 || cropCycleStatus === 4) {
    return {
      allowed: false,
      replaceable: false,
      reason: "Siklus yang selesai atau dibatalkan tidak dapat menerima skema.",
    };
  }

  if (hasAssignment && cropCycleStatus !== 1) {
    return {
      allowed: false,
      replaceable: false,
      reason: "Skema terkunci setelah siklus mulai berjalan.",
    };
  }

  return {
    allowed: true,
    replaceable: hasAssignment,
    reason: hasAssignment
      ? "Skema masih dapat diganti karena siklus belum dimulai."
      : "Pilih satu skema aktif untuk menyimpan snapshot aturan pada siklus.",
  };
}

export function summarizeProfitSharingPreview(
  preview: ProfitSharingPreview,
): ProfitSharingPreviewSummary {
  const expectedPayout = preview.totals.totalCapitalRecovery
    + preview.totals.totalProfitShare;

  return {
    participantCount: preview.allocations.length,
    fundedParticipantCount: preview.allocations
      .filter((allocation) => allocation.confirmedCapital > 0).length,
    unallocatedPriorityAmount: preview.priorityAllocations
      .reduce((total, allocation) => total + allocation.unallocatedAmount, 0),
    hasCapitalLoss: preview.totals.totalCapitalLoss > 0,
    isPayoutReconciled: Math.abs(preview.totals.totalPayout - expectedPayout) < 0.01,
  };
}

export function summarizeProfitSharingWaterfallSettlements(
  settlements: ProfitSharingWaterfallSettlement[],
): ProfitSharingWaterfallSettlementSummary {
  const ordered = settlements.toSorted((left, right) =>
    right.finalizedAt.localeCompare(left.finalizedAt));

  return {
    total: settlements.length,
    finalized: settlements.filter((settlement) => settlement.status === 1).length,
    voided: settlements.filter((settlement) => settlement.status === 2).length,
    active: ordered.find((settlement) => settlement.status === 1) ?? null,
    latest: ordered[0] ?? null,
  };
}

export function filterProfitSharingWaterfallSettlements(
  settlements: ProfitSharingWaterfallSettlement[],
  query: string,
  status: ProfitSharingWaterfallSettlementStatusFilter,
): ProfitSharingWaterfallSettlement[] {
  const normalizedQuery = query.trim().toLocaleLowerCase("id-ID");

  return settlements
    .filter((settlement) => status === "all" || settlement.status === status)
    .filter((settlement) => !normalizedQuery || [
      settlement.code,
      settlement.schemeCodeSnapshot,
      settlement.schemeNameSnapshot,
      settlement.cropCycleCodeSnapshot,
    ].some((value) => value.toLocaleLowerCase("id-ID").includes(normalizedQuery)))
    .toSorted((left, right) => right.finalizedAt.localeCompare(left.finalizedAt));
}

export function profitSharingWaterfallFinalizationAvailability(
  cropCycleStatus: CropCycleStatus,
  settlements: ProfitSharingWaterfallSettlement[],
  hasPreview: boolean,
): ProfitSharingWaterfallFinalizationAvailability {
  if (settlements.some((settlement) => settlement.status === 1)) {
    return {
      allowed: false,
      reason: "Siklus sudah memiliki settlement final aktif.",
    };
  }

  if (cropCycleStatus !== 3 && cropCycleStatus !== 4) {
    return {
      allowed: false,
      reason: "Selesaikan atau batalkan siklus sebelum finalisasi.",
    };
  }

  if (!hasPreview) {
    return {
      allowed: false,
      reason: "Preview valid diperlukan sebelum snapshot difinalkan.",
    };
  }

  return {
    allowed: true,
    reason: "Preview siap dikunci sebagai snapshot final immutable.",
  };
}

export function createProfitSharingWaterfallSettlementDraft(
  cropCycleCode: string,
  settlementDate: string,
): ProfitSharingWaterfallSettlementDraft {
  const compactDate = settlementDate.replaceAll("-", "");
  const normalizedCycleCode = cropCycleCode
    .trim()
    .toUpperCase()
    .replaceAll(/[^A-Z0-9._-]/g, "-");

  return {
    code: `PSV2-${normalizedCycleCode}-${compactDate}`.slice(0, 40),
    settlementDate,
    notes: "",
  };
}

export function validateProfitSharingWaterfallSettlementDraft(
  draft: ProfitSharingWaterfallSettlementDraft,
): string[] {
  const errors: string[] = [];
  const code = draft.code.trim().toUpperCase();

  if (!codePattern.test(code)) {
    errors.push("Kode settlement wajib memakai format kode maksimal 40 karakter.");
  }
  if (!/^\d{4}-\d{2}-\d{2}$/.test(draft.settlementDate)) {
    errors.push("Tanggal settlement wajib diisi.");
  }
  if (draft.notes.trim().length > 1000) {
    errors.push("Catatan settlement maksimal 1.000 karakter.");
  }

  return errors;
}

const codePattern = /^[A-Z0-9][A-Z0-9._-]{0,39}$/;
const ratePattern = /^\d+(?:[.,]\d{1,8})?$/;
const rateTolerance = 0.00000001;

function companyParticipant(): ProfitSharingSchemeParticipantDraft {
  return {
    participantCode: "PERUSAHAAN",
    participantName: "Perusahaan",
    participantRole: 1,
    participatesInResidualProfit: true,
    sequence: 1,
  };
}

export function createProfitSharingSchemeDraft(
  preset: ProfitSharingSchemePreset = "internal",
): ProfitSharingSchemeDraft {
  if (preset === "managed") {
    return {
      code: "",
      name: "Perusahaan dan mitra pengelola",
      description: "Biaya pengelolaan dipotong sebelum laba tersisa dibagi proporsional terhadap modal.",
      participants: [
        companyParticipant(),
        {
          participantCode: "MITRA",
          participantName: "Mitra pengelola",
          participantRole: 3,
          participatesInResidualProfit: true,
          sequence: 2,
        },
      ],
      priorityRules: [
        {
          ruleCode: "BIAYA-KELOLA",
          ruleType: 1,
          recipientCode: "MITRA",
          rateNumerator: "1",
          rateDenominator: "3",
          sequence: 1,
        },
      ],
      residualMethod: 2,
      residualRecipientCode: "",
      residualShares: [],
    };
  }

  if (preset === "passive-investor") {
    return {
      code: "",
      name: "Perusahaan dan investor pasif",
      description: "Laba tersisa dibagi proporsional terhadap modal perusahaan dan investor pasif.",
      participants: [
        companyParticipant(),
        {
          participantCode: "INVESTOR-PASIF",
          participantName: "Investor pasif",
          participantRole: 2,
          participatesInResidualProfit: true,
          sequence: 2,
        },
      ],
      priorityRules: [],
      residualMethod: 2,
      residualRecipientCode: "",
      residualShares: [],
    };
  }

  return {
    code: "",
    name: "Internal perusahaan",
    description: "Modal dan pengelolaan sepenuhnya berasal dari internal perusahaan.",
    participants: [companyParticipant()],
    priorityRules: [],
    residualMethod: 1,
    residualRecipientCode: "PERUSAHAAN",
    residualShares: [],
  };
}

export function profitSharingSchemeDraftFrom(
  scheme: ProfitSharingScheme,
): ProfitSharingSchemeDraft {
  return {
    code: scheme.code,
    name: scheme.name,
    description: scheme.description ?? "",
    participants: scheme.participants
      .toSorted((left, right) => left.sequence - right.sequence)
      .map(({ participantCode, participantName, participantRole, participatesInResidualProfit }, index) => ({
        participantCode,
        participantName,
        participantRole,
        participatesInResidualProfit,
        sequence: index + 1,
      })),
    priorityRules: scheme.priorityRules
      .toSorted((left, right) => left.sequence - right.sequence)
      .map(({ ruleCode, ruleType, recipientCode, rateNumerator, rateDenominator }, index) => ({
        ruleCode,
        ruleType,
        recipientCode,
        rateNumerator: String(rateNumerator),
        rateDenominator: String(rateDenominator),
        sequence: index + 1,
      })),
    residualMethod: scheme.residualMethod,
    residualRecipientCode: scheme.residualRecipientCode ?? "",
    residualShares: scheme.residualShares
      .toSorted((left, right) => left.sequence - right.sequence)
      .map(({ recipientCode, rateNumerator, rateDenominator }, index) => ({
        recipientCode,
        rateNumerator: String(rateNumerator),
        rateDenominator: String(rateDenominator),
        sequence: index + 1,
      })),
  };
}

export function moveProfitSharingSchemeItem<T extends { sequence: number }>(
  items: T[],
  sourceIndex: number,
  destinationIndex: number,
): T[] {
  if (
    sourceIndex === destinationIndex ||
    sourceIndex < 0 ||
    destinationIndex < 0 ||
    sourceIndex >= items.length ||
    destinationIndex >= items.length
  ) {
    return items;
  }

  const reordered = [...items];
  const [moved] = reordered.splice(sourceIndex, 1);
  reordered.splice(destinationIndex, 0, moved);
  return reordered.map((item, index) => ({ ...item, sequence: index + 1 }));
}

function normalizeCode(value: string): string {
  return value.trim().toUpperCase();
}

function optionalText(value: string): string | null {
  const normalized = value.trim();
  return normalized ? normalized : null;
}

function parseRate(value: string): number | null {
  const normalized = value.trim().replace(",", ".");
  if (!ratePattern.test(normalized)) return null;
  const number = Number(normalized);
  return Number.isFinite(number) ? number : null;
}

function readRate(
  numerator: string,
  denominator: string,
): { numerator: number; denominator: number } {
  const parsedNumerator = parseRate(numerator);
  const parsedDenominator = parseRate(denominator);

  if (
    parsedNumerator === null ||
    parsedDenominator === null ||
    parsedNumerator <= 0 ||
    parsedDenominator <= 0 ||
    parsedNumerator > parsedDenominator
  ) {
    throw new Error("Tarif bagi hasil tidak valid.");
  }

  return {
    numerator: parsedNumerator,
    denominator: parsedDenominator,
  };
}

function validRate(numerator: string, denominator: string): boolean {
  try {
    readRate(numerator, denominator);
    return true;
  } catch {
    return false;
  }
}

export function validateProfitSharingSchemeDraft(
  draft: ProfitSharingSchemeDraft,
): string[] {
  const errors: string[] = [];
  const code = normalizeCode(draft.code);
  const participantCodes = draft.participants.map((participant) =>
    normalizeCode(participant.participantCode));
  const participantCodeSet = new Set(participantCodes);

  if (!codePattern.test(code)) {
    errors.push("Kode skema wajib memakai format kode maksimal 40 karakter.");
  }
  if (!draft.name.trim() || draft.name.trim().length > 150) {
    errors.push("Nama skema wajib diisi dan maksimal 150 karakter.");
  }
  if (draft.description.trim().length > 1000) {
    errors.push("Deskripsi skema maksimal 1.000 karakter.");
  }
  if (draft.participants.length === 0) {
    errors.push("Skema harus memiliki minimal satu peserta.");
  }
  if (participantCodeSet.size !== participantCodes.length) {
    errors.push("Kode setiap peserta harus unik.");
  }

  draft.participants.forEach((participant, index) => {
    if (!codePattern.test(participantCodes[index])) {
      errors.push(`Kode peserta urutan ${index + 1} tidak valid.`);
    }
    if (!participant.participantName.trim() || participant.participantName.trim().length > 150) {
      errors.push(`Nama peserta urutan ${index + 1} wajib diisi dan maksimal 150 karakter.`);
    }
    if (![1, 2, 3, 4].includes(participant.participantRole)) {
      errors.push(`Peran peserta urutan ${index + 1} tidak didukung.`);
    }
  });

  const ruleCodes = draft.priorityRules.map((rule) => normalizeCode(rule.ruleCode));
  if (new Set(ruleCodes).size !== ruleCodes.length) {
    errors.push("Kode setiap aturan prioritas harus unik.");
  }
  draft.priorityRules.forEach((rule, index) => {
    if (!codePattern.test(ruleCodes[index])) {
      errors.push(`Kode aturan prioritas urutan ${index + 1} tidak valid.`);
    }
    if (![1, 2].includes(rule.ruleType)) {
      errors.push(`Jenis aturan prioritas urutan ${index + 1} tidak didukung.`);
    }
    if (!participantCodeSet.has(normalizeCode(rule.recipientCode))) {
      errors.push(`Penerima aturan prioritas urutan ${index + 1} tidak ditemukan.`);
    }
    if (!validRate(rule.rateNumerator, rule.rateDenominator)) {
      errors.push(`Tarif aturan prioritas urutan ${index + 1} tidak valid.`);
    }
  });

  const residualRecipient = normalizeCode(draft.residualRecipientCode);
  if (draft.residualMethod === 1) {
    if (!participantCodeSet.has(residualRecipient)) {
      errors.push("Penerima sisa laba harus berasal dari daftar peserta.");
    }
    if (draft.residualShares.length > 0) {
      errors.push("Metode penerima tunggal tidak boleh memiliki persentase sisa tetap.");
    }
  } else if (draft.residualMethod === 2) {
    if (residualRecipient || draft.residualShares.length > 0) {
      errors.push("Metode proporsional tidak memakai penerima tunggal atau persentase tetap.");
    }
    if (!draft.participants.some((participant) => participant.participatesInResidualProfit)) {
      errors.push("Metode proporsional membutuhkan minimal satu peserta laba tersisa.");
    }
  } else if (draft.residualMethod === 3) {
    if (residualRecipient) {
      errors.push("Metode persentase tetap tidak memakai penerima tunggal.");
    }
    if (draft.residualShares.length === 0) {
      errors.push("Metode persentase tetap membutuhkan rincian pembagian.");
    }

    const shareRecipients = draft.residualShares.map((share) =>
      normalizeCode(share.recipientCode));
    if (new Set(shareRecipients).size !== shareRecipients.length) {
      errors.push("Penerima persentase sisa harus unik.");
    }

    let totalRate = 0;
    draft.residualShares.forEach((share, index) => {
      if (!participantCodeSet.has(shareRecipients[index])) {
        errors.push(`Penerima persentase sisa urutan ${index + 1} tidak ditemukan.`);
      }
      if (!validRate(share.rateNumerator, share.rateDenominator)) {
        errors.push(`Tarif persentase sisa urutan ${index + 1} tidak valid.`);
      } else {
        const rate = readRate(share.rateNumerator, share.rateDenominator);
        totalRate += rate.numerator / rate.denominator;
      }
    });
    if (draft.residualShares.length > 0 && Math.abs(totalRate - 1) > rateTolerance) {
      errors.push("Total persentase laba tersisa harus tepat 100%.");
    }
  } else {
    errors.push("Metode pembagian laba tersisa tidak didukung.");
  }

  return errors;
}

export function buildCreateProfitSharingSchemeRequest(
  draft: ProfitSharingSchemeDraft,
): CreateProfitSharingSchemeRequest {
  const errors = validateProfitSharingSchemeDraft(draft);
  if (errors.length > 0) throw new Error(errors[0]);

  return {
    code: normalizeCode(draft.code),
    ...buildUpdateProfitSharingSchemeRequest(draft),
  };
}

export function buildUpdateProfitSharingSchemeRequest(
  draft: ProfitSharingSchemeDraft,
): UpdateProfitSharingSchemeDraftRequest {
  const errors = validateProfitSharingSchemeDraft(draft);
  if (errors.length > 0) throw new Error(errors[0]);

  return {
    name: draft.name.trim(),
    description: optionalText(draft.description),
    participants: draft.participants.map((participant, index) => ({
      participantCode: normalizeCode(participant.participantCode),
      participantName: participant.participantName.trim(),
      participantRole: participant.participantRole,
      participatesInResidualProfit: participant.participatesInResidualProfit,
      sequence: index + 1,
    })),
    priorityRules: draft.priorityRules.map((rule, index) => {
      const rate = readRate(rule.rateNumerator, rule.rateDenominator);
      return {
        ruleCode: normalizeCode(rule.ruleCode),
        ruleType: rule.ruleType,
        recipientCode: normalizeCode(rule.recipientCode),
        rateNumerator: rate.numerator,
        rateDenominator: rate.denominator,
        sequence: index + 1,
      };
    }),
    residualMethod: draft.residualMethod,
    residualRecipientCode: optionalText(normalizeCode(draft.residualRecipientCode)),
    residualShares: draft.residualShares.map((share, index) => {
      const rate = readRate(share.rateNumerator, share.rateDenominator);
      return {
        recipientCode: normalizeCode(share.recipientCode),
        rateNumerator: rate.numerator,
        rateDenominator: rate.denominator,
        sequence: index + 1,
      };
    }),
  };
}

export function formatProfitSharingRate(
  numerator: number,
  denominator: number,
): string {
  if (denominator <= 0) return "—";
  return new Intl.NumberFormat("id-ID", {
    style: "percent",
    maximumFractionDigits: 4,
  }).format(numerator / denominator);
}
