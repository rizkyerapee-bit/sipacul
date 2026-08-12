import type {
  CapitalContribution,
  CapitalContributionPaymentMethod,
  CapitalContributionStatus,
  CapitalContributorRole,
  CropCycle,
  CropCycleProfitability,
  ProfitSharingSettlement,
  ProfitSharingSettlementStatus,
  ProfitabilityOutcome,
} from "@/lib/api/contracts";

export type CapitalDraft = {
  code: string;
  contributionDate: string;
  contributorCode: string;
  contributorName: string;
  contributorRole: CapitalContributorRole;
  amount: string;
  paymentMethod: CapitalContributionPaymentMethod;
  referenceNumber: string;
  notes: string;
};

export type SettlementDraft = {
  code: string;
  settlementDate: string;
  managingPartnerCode: string;
  managingPartnerName: string;
  notes: string;
};

export type CapitalStatusFilter = "all" | CapitalContributionStatus;
export type CapitalRoleFilter = "all" | CapitalContributorRole;
export type SettlementStatusFilter = "all" | ProfitSharingSettlementStatus;

export const contributorRoleLabels: Record<CapitalContributorRole, string> = {
  1: "Investor",
  2: "Mitra pengelola",
};

export const capitalPaymentMethodLabels: Record<CapitalContributionPaymentMethod, string> = {
  1: "Tunai",
  2: "Transfer bank",
  3: "Lainnya",
};

export const capitalStatusLabels: Record<CapitalContributionStatus, string> = {
  1: "Draf",
  2: "Dikonfirmasi",
  3: "Dibatalkan",
};

export const settlementStatusLabels: Record<ProfitSharingSettlementStatus, string> = {
  1: "Draf",
  2: "Final",
  3: "Dibatalkan",
};

export const profitabilityOutcomeLabels: Record<ProfitabilityOutcome, string> = {
  1: "Rugi",
  2: "Impas",
  3: "Untung",
};

export function formatSharingCurrency(value: number): string {
  return new Intl.NumberFormat("id-ID", {
    style: "currency",
    currency: "IDR",
    maximumFractionDigits: 0,
  }).format(value);
}

export function formatSharingDate(value: string | null): string {
  if (!value) return "—";
  const [year, month, day] = value.slice(0, 10).split("-").map(Number);
  if (!year || !month || !day) return value;
  return new Intl.DateTimeFormat("id-ID", {
    day: "numeric",
    month: "short",
    year: "numeric",
  }).format(new Date(year, month - 1, day));
}

export function formatRatio(value: number): string {
  return new Intl.NumberFormat("id-ID", {
    style: "percent",
    maximumFractionDigits: 2,
  }).format(value);
}

export function optionalSharingText(value: string): string | null {
  const normalized = value.trim();
  return normalized ? normalized : null;
}

export function parseCapitalAmount(value: string): number | null {
  const normalized = value.trim().replace(/\s/g, "").replace(/,/g, ".");
  if (!/^\d+(?:\.\d{1,2})?$/.test(normalized)) return null;
  const amount = Number(normalized);
  return Number.isFinite(amount) ? amount : null;
}

function shiftYear(value: string, offset: number): string {
  const [year, month, day] = value.slice(0, 10).split("-").map(Number);
  if (!year || !month || !day) return value;
  const shiftedYear = year + offset;
  const maximumDay = new Date(Date.UTC(shiftedYear, month, 0)).getUTCDate();
  return [
    shiftedYear.toString().padStart(4, "0"),
    month.toString().padStart(2, "0"),
    Math.min(day, maximumDay).toString().padStart(2, "0"),
  ].join("-");
}

export function contributionDateWindow(cycle: CropCycle): { minimum: string; maximum: string } {
  return {
    minimum: shiftYear(cycle.plannedStartDate, -1),
    maximum: shiftYear(cycle.actualHarvestDate ?? cycle.expectedHarvestDate, 1),
  };
}

export function capitalDraftFrom(
  contribution: CapitalContribution | null,
  cycle: CropCycle,
  today: string,
): CapitalDraft {
  const window = contributionDateWindow(cycle);
  const suggestedDate = today < window.minimum
    ? window.minimum
    : today > window.maximum ? window.maximum : today;

  return contribution
    ? {
      code: contribution.code,
      contributionDate: contribution.contributionDate,
      contributorCode: contribution.contributorCode,
      contributorName: contribution.contributorName,
      contributorRole: contribution.contributorRole,
      amount: String(contribution.amount),
      paymentMethod: contribution.paymentMethod,
      referenceNumber: contribution.referenceNumber ?? "",
      notes: contribution.notes ?? "",
    }
    : {
      code: "",
      contributionDate: suggestedDate,
      contributorCode: "",
      contributorName: "",
      contributorRole: 1,
      amount: "",
      paymentMethod: 2,
      referenceNumber: "",
      notes: "",
    };
}

function isValidCode(value: string): boolean {
  return /^[A-Z0-9][A-Z0-9._-]{0,39}$/.test(value.trim().toUpperCase());
}

export function validateCapitalDraft(
  draft: CapitalDraft,
  cycle: CropCycle,
  isCreate: boolean,
): string[] {
  const errors: string[] = [];
  const amount = parseCapitalAmount(draft.amount);
  const window = contributionDateWindow(cycle);
  const contributorCode = draft.contributorCode.trim().toUpperCase();
  const contributorName = draft.contributorName.trim();

  if (isCreate && !draft.code.trim()) {
    errors.push("Kode setoran modal wajib diisi.");
  } else if (isCreate && !isValidCode(draft.code)) {
    errors.push("Kode setoran maksimal 40 karakter dan hanya boleh berisi huruf, angka, titik, tanda hubung, atau garis bawah.");
  }
  if (!draft.contributionDate) {
    errors.push("Tanggal setoran wajib diisi.");
  } else if (draft.contributionDate < window.minimum || draft.contributionDate > window.maximum) {
    errors.push(`Tanggal setoran harus antara ${formatSharingDate(window.minimum)} dan ${formatSharingDate(window.maximum)}.`);
  }
  if (!contributorCode) {
    errors.push("Kode pemberi modal wajib diisi.");
  } else if (!isValidCode(contributorCode)) {
    errors.push("Kode pemberi modal maksimal 40 karakter dan memakai format kode yang valid.");
  }
  if (!contributorName) {
    errors.push("Nama pemberi modal wajib diisi.");
  } else if (contributorName.length > 150) {
    errors.push("Nama pemberi modal maksimal 150 karakter.");
  }
  if (!Object.prototype.hasOwnProperty.call(contributorRoleLabels, draft.contributorRole)) {
    errors.push("Peran pemberi modal tidak didukung.");
  }
  if (amount === null || amount <= 0) {
    errors.push("Jumlah modal harus berupa angka lebih dari nol dengan maksimal dua desimal.");
  }
  if (!Object.prototype.hasOwnProperty.call(capitalPaymentMethodLabels, draft.paymentMethod)) {
    errors.push("Metode setoran tidak didukung.");
  }
  if (draft.referenceNumber.trim().length > 100) {
    errors.push("Nomor referensi maksimal 100 karakter.");
  }
  if (draft.notes.trim().length > 1000) {
    errors.push("Catatan maksimal 1.000 karakter.");
  }

  return errors;
}

export function settlementDraftFrom(
  settlement: ProfitSharingSettlement | null,
  contributions: CapitalContribution[],
  today: string,
): SettlementDraft {
  if (settlement) {
    return {
      code: settlement.code,
      settlementDate: settlement.settlementDate,
      managingPartnerCode: settlement.managingPartnerCode,
      managingPartnerName: settlement.managingPartnerName,
      notes: settlement.notes ?? "",
    };
  }

  const partner = contributions.find(
    (contribution) => contribution.status === 2 && contribution.contributorRole === 2,
  );

  return {
    code: "",
    settlementDate: today,
    managingPartnerCode: partner?.contributorCode ?? "",
    managingPartnerName: partner?.contributorName ?? "",
    notes: "",
  };
}

export function validateSettlementDraft(
  draft: SettlementDraft,
  isCreate: boolean,
): string[] {
  const errors: string[] = [];
  if (isCreate && !draft.code.trim()) {
    errors.push("Kode pembagian hasil wajib diisi.");
  } else if (isCreate && !isValidCode(draft.code)) {
    errors.push("Kode pembagian hasil maksimal 40 karakter dan memakai format kode yang valid.");
  }
  if (!draft.settlementDate) errors.push("Tanggal pembagian hasil wajib diisi.");
  if (!draft.managingPartnerCode.trim()) {
    errors.push("Kode mitra pengelola wajib diisi.");
  } else if (!isValidCode(draft.managingPartnerCode)) {
    errors.push("Kode mitra pengelola maksimal 40 karakter dan memakai format kode yang valid.");
  }
  if (!draft.managingPartnerName.trim()) {
    errors.push("Nama mitra pengelola wajib diisi.");
  } else if (draft.managingPartnerName.trim().length > 150) {
    errors.push("Nama mitra pengelola maksimal 150 karakter.");
  }
  if (draft.notes.trim().length > 1000) errors.push("Catatan maksimal 1.000 karakter.");
  return errors;
}

export function filterCapitalContributions(
  contributions: CapitalContribution[],
  query: string,
  status: CapitalStatusFilter,
  role: CapitalRoleFilter,
): CapitalContribution[] {
  const normalized = query.trim().toLocaleLowerCase("id-ID");
  return contributions
    .filter((item) => status === "all" || item.status === status)
    .filter((item) => role === "all" || item.contributorRole === role)
    .filter((item) => !normalized || [
      item.code,
      item.contributorCode,
      item.contributorName,
      item.referenceNumber,
    ].some((value) => value?.toLocaleLowerCase("id-ID").includes(normalized)))
    .sort((left, right) => left.status - right.status || right.contributionDate.localeCompare(left.contributionDate));
}

export function filterSettlements(
  settlements: ProfitSharingSettlement[],
  status: SettlementStatusFilter,
): ProfitSharingSettlement[] {
  return settlements
    .filter((item) => status === "all" || item.status === status)
    .sort((left, right) => right.settlementDate.localeCompare(left.settlementDate));
}

export function summarizeCapital(contributions: CapitalContribution[]) {
  const confirmed = contributions.filter((item) => item.status === 2);
  return {
    investor: confirmed
      .filter((item) => item.contributorRole === 1)
      .reduce((total, item) => total + item.amount, 0),
    partner: confirmed
      .filter((item) => item.contributorRole === 2)
      .reduce((total, item) => total + item.amount, 0),
    total: confirmed.reduce((total, item) => total + item.amount, 0),
    draft: contributions
      .filter((item) => item.status === 1)
      .reduce((total, item) => total + item.amount, 0),
    draftCount: contributions.filter((item) => item.status === 1).length,
  };
}

export function profitPools(profitability: CropCycleProfitability | null) {
  const netProfit = profitability?.outcome === 3 ? profitability.netProfit : 0;
  const management = Math.round((netProfit / 3) * 100) / 100;
  return {
    management,
    capital: Math.round((netProfit - management) * 100) / 100,
  };
}

export type ReadinessItem = {
  key: string;
  label: string;
  ready: boolean;
  detail: string;
};

export function settlementReadiness(
  cycle: CropCycle,
  profitability: CropCycleProfitability | null,
  contributions: CapitalContribution[],
  settlements: ProfitSharingSettlement[],
): ReadinessItem[] {
  const capital = summarizeCapital(contributions);
  const totalCost = profitability?.totalCultivationCost ?? 0;
  return [
    {
      key: "cycle",
      label: "Siklus telah ditutup",
      ready: cycle.status === 3 || cycle.status === 4,
      detail: cycle.status === 3 || cycle.status === 4
        ? "Status siklus selesai atau dibatalkan."
        : "Selesaikan atau batalkan siklus terlebih dahulu.",
    },
    {
      key: "cost",
      label: "Biaya budidaya tersedia",
      ready: totalCost > 0,
      detail: totalCost > 0
        ? `Biaya diakui ${formatSharingCurrency(totalCost)}.`
        : "Siklus tanpa biaya tidak dapat dibagi hasil.",
    },
    {
      key: "receivable",
      label: "Piutang telah lunas",
      ready: profitability !== null && profitability.outstandingReceivable === 0,
      detail: profitability?.outstandingReceivable === 0
        ? "Seluruh penjualan terkonfirmasi telah tertagih."
        : `Masih ada piutang ${formatSharingCurrency(profitability?.outstandingReceivable ?? 0)}.`,
    },
    {
      key: "capital",
      label: "Modal sesuai biaya",
      ready: totalCost > 0 && Math.abs(capital.total - totalCost) < 0.005,
      detail: `Modal ${formatSharingCurrency(capital.total)} dari biaya ${formatSharingCurrency(totalCost)}.`,
    },
    {
      key: "draft-capital",
      label: "Tidak ada setoran draf",
      ready: capital.draftCount === 0,
      detail: capital.draftCount === 0
        ? "Semua setoran telah diselesaikan."
        : `${capital.draftCount} setoran modal masih draf.`,
    },
    {
      key: "active",
      label: "Belum ada pembagian aktif",
      ready: !settlements.some((item) => item.status === 2 && item.isActive),
      detail: settlements.some((item) => item.status === 2 && item.isActive)
        ? "Batalkan pembagian final aktif sebelum membuat pengganti."
        : "Tidak ada pembagian final aktif.",
    },
  ];
}
