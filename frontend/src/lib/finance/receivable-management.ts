import type {
  Sale,
  SalePayment,
  SalePaymentMethod,
  SalePaymentState,
  SalePaymentStatus,
  SaleReceivable,
} from "@/lib/api/contracts";

export type ReceivableEntry = {
  sale: Sale;
  receivable: SaleReceivable;
  payments: SalePayment[];
};

export type PaymentDraft = {
  code: string;
  paymentDate: string;
  amount: string;
  paymentMethod: SalePaymentMethod;
  referenceNumber: string;
  receivedFrom: string;
  notes: string;
};

export type ReceivableStateFilter = "all" | SalePaymentState;

export type DueStateFilter = "all" | "overdue" | "due-soon";

export const paymentStatusLabels: Record<SalePaymentStatus, string> = {
  1: "Draf",
  2: "Dikonfirmasi",
  3: "Dibatalkan",
};

export const paymentMethodLabels: Record<SalePaymentMethod, string> = {
  1: "Tunai",
  2: "Transfer bank",
  3: "Lainnya",
};

export const paymentStateLabels: Record<SalePaymentState, string> = {
  1: "Belum dibayar",
  2: "Dibayar sebagian",
  3: "Lunas",
};

export function optionalPaymentText(value: string): string | null {
  const normalized = value.trim();
  return normalized.length > 0 ? normalized : null;
}

export function parsePaymentAmount(value: string): number | null {
  const normalized = value.trim().replace(/\s/g, "").replace(/,/g, ".");
  if (!/^\d+(?:\.\d{1,2})?$/.test(normalized)) return null;
  const amount = Number(normalized);
  return Number.isFinite(amount) ? amount : null;
}

export function formatFinanceCurrency(value: number): string {
  return new Intl.NumberFormat("id-ID", {
    style: "currency",
    currency: "IDR",
    maximumFractionDigits: 0,
  }).format(value);
}

export function formatFinanceDate(value: string | null): string {
  if (!value) return "—";
  const [year, month, day] = value.slice(0, 10).split("-").map(Number);
  if (!year || !month || !day) return value;
  return new Intl.DateTimeFormat("id-ID", {
    day: "numeric",
    month: "short",
    year: "numeric",
  }).format(new Date(year, month - 1, day));
}

export function paymentDraftFrom(
  payment: SalePayment | null,
  sale: Sale,
  outstanding: number,
  today: string,
): PaymentDraft {
  return payment
    ? {
      code: payment.code,
      paymentDate: payment.paymentDate,
      amount: String(payment.amount),
      paymentMethod: payment.paymentMethod,
      referenceNumber: payment.referenceNumber ?? "",
      receivedFrom: payment.receivedFrom ?? "",
      notes: payment.notes ?? "",
    }
    : {
      code: "",
      paymentDate: today < sale.saleDate ? sale.saleDate : today,
      amount: outstanding > 0 ? String(outstanding) : "",
      paymentMethod: 2,
      referenceNumber: "",
      receivedFrom: sale.buyerName,
      notes: "",
    };
}

export function validatePaymentDraft(
  draft: PaymentDraft,
  sale: Sale,
  availableBalance: number,
  isCreate: boolean,
): string[] {
  const errors: string[] = [];
  const code = draft.code.trim().toUpperCase();
  const amount = parsePaymentAmount(draft.amount);

  if (isCreate && !code) {
    errors.push("Kode pembayaran wajib diisi.");
  } else if (isCreate && !/^[A-Z0-9][A-Z0-9._-]{0,39}$/.test(code)) {
    errors.push("Kode maksimal 40 karakter dan hanya boleh berisi huruf, angka, titik, tanda hubung, atau garis bawah.");
  }
  if (!draft.paymentDate) {
    errors.push("Tanggal pembayaran wajib diisi.");
  } else if (draft.paymentDate < sale.saleDate) {
    errors.push("Tanggal pembayaran tidak boleh sebelum tanggal penjualan.");
  }
  if (amount === null || amount <= 0) {
    errors.push("Jumlah pembayaran harus berupa angka lebih dari nol dengan maksimal dua desimal.");
  } else if (amount > availableBalance) {
    errors.push(`Jumlah pembayaran tidak boleh melebihi sisa tagihan ${formatFinanceCurrency(availableBalance)}.`);
  }
  if (![1, 2, 3].includes(draft.paymentMethod)) {
    errors.push("Metode pembayaran tidak didukung.");
  }
  if (draft.referenceNumber.trim().length > 100) {
    errors.push("Nomor referensi maksimal 100 karakter.");
  }
  if (draft.receivedFrom.trim().length > 150) {
    errors.push("Nama penyetor maksimal 150 karakter.");
  }
  if (draft.notes.trim().length > 1000) {
    errors.push("Catatan maksimal 1.000 karakter.");
  }

  return errors;
}

export function daysUntil(date: string | null, today: string): number | null {
  if (!date) return null;
  const end = Date.parse(`${date.slice(0, 10)}T00:00:00Z`);
  const start = Date.parse(`${today.slice(0, 10)}T00:00:00Z`);
  if (!Number.isFinite(end) || !Number.isFinite(start)) return null;
  return Math.round((end - start) / 86_400_000);
}

export function isOverdue(entry: ReceivableEntry, today: string): boolean {
  const remaining = daysUntil(entry.receivable.dueDate, today);
  return entry.receivable.outstandingReceivable > 0
    && remaining !== null
    && remaining < 0;
}

export function isDueSoon(entry: ReceivableEntry, today: string): boolean {
  const remaining = daysUntil(entry.receivable.dueDate, today);
  return entry.receivable.outstandingReceivable > 0
    && remaining !== null
    && remaining >= 0
    && remaining <= 7;
}

export function filterReceivables(
  entries: ReceivableEntry[],
  query: string,
  state: ReceivableStateFilter,
  due: DueStateFilter,
  today: string,
): ReceivableEntry[] {
  const normalizedQuery = query.trim().toLocaleLowerCase("id-ID");

  return entries
    .filter((entry) => {
      const matchesQuery = !normalizedQuery || [
        entry.sale.code,
        entry.sale.buyerName,
        entry.sale.buyerPhone,
        entry.receivable.dueDate,
      ].some((value) => value?.toLocaleLowerCase("id-ID").includes(normalizedQuery));
      const matchesState = state === "all" || entry.receivable.paymentState === state;
      const matchesDue = due === "all"
        || (due === "overdue" && isOverdue(entry, today))
        || (due === "due-soon" && isDueSoon(entry, today));
      return matchesQuery && matchesState && matchesDue;
    })
    .sort((left, right) => {
      const leftOverdue = isOverdue(left, today);
      const rightOverdue = isOverdue(right, today);
      if (leftOverdue !== rightOverdue) return leftOverdue ? -1 : 1;
      if (left.receivable.isFullyPaid !== right.receivable.isFullyPaid) {
        return left.receivable.isFullyPaid ? 1 : -1;
      }
      return (left.receivable.dueDate ?? "9999-12-31")
        .localeCompare(right.receivable.dueDate ?? "9999-12-31");
    });
}

export function summarizeReceivables(entries: ReceivableEntry[], today: string) {
  const billed = entries.reduce(
    (total, entry) => total + entry.receivable.saleTotalAmount,
    0,
  );
  const collected = entries.reduce(
    (total, entry) => total + entry.receivable.confirmedPaidAmount,
    0,
  );
  const outstanding = entries.reduce(
    (total, entry) => total + entry.receivable.outstandingReceivable,
    0,
  );

  return {
    billed,
    collected,
    outstanding,
    overdueCount: entries.filter((entry) => isOverdue(entry, today)).length,
    paidCount: entries.filter((entry) => entry.receivable.isFullyPaid).length,
    collectionRate: billed > 0 ? (collected / billed) * 100 : 0,
  };
}
