import type {
  CropCycle,
  CultivationExpense,
  CultivationExpenseCategory,
  CultivationExpenseStatus,
} from "@/lib/api/contracts";

export type ExpenseDraft = {
  code: string;
  expenseDate: string;
  category: CultivationExpenseCategory;
  description: string;
  amount: string;
  payeeName: string;
  referenceNumber: string;
  evidenceUrl: string;
  notes: string;
};

export type ExpenseStatusFilter = "all" | CultivationExpenseStatus;
export type ExpenseCategoryFilter = "all" | CultivationExpenseCategory;

export const expenseStatusLabels: Record<CultivationExpenseStatus, string> = {
  1: "Draf",
  2: "Dikonfirmasi",
  3: "Dibatalkan",
};

export const expenseCategoryLabels: Record<CultivationExpenseCategory, string> = {
  1: "Sewa lahan",
  2: "Benih",
  3: "Pupuk",
  4: "Pestisida",
  5: "Tenaga kerja",
  6: "Peralatan",
  7: "Irigasi",
  8: "Bahan bakar",
  9: "Transportasi",
  10: "Penyimpanan",
  11: "Panen",
  12: "Pascapanen",
  13: "Pemasaran",
  14: "Administrasi",
  15: "Lainnya",
};

export function optionalExpenseText(value: string): string | null {
  const normalized = value.trim();
  return normalized.length > 0 ? normalized : null;
}

export function parseExpenseAmount(value: string): number | null {
  const normalized = value.trim().replace(/\s/g, "").replace(/,/g, ".");
  if (!/^\d+(?:\.\d{1,2})?$/.test(normalized)) return null;
  const amount = Number(normalized);
  return Number.isFinite(amount) ? amount : null;
}

export function formatExpenseCurrency(value: number): string {
  return new Intl.NumberFormat("id-ID", {
    style: "currency",
    currency: "IDR",
    maximumFractionDigits: 0,
  }).format(value);
}

export function formatExpenseDate(value: string | null): string {
  if (!value) return "—";
  const [year, month, day] = value.slice(0, 10).split("-").map(Number);
  if (!year || !month || !day) return value;
  return new Intl.DateTimeFormat("id-ID", {
    day: "numeric",
    month: "short",
    year: "numeric",
  }).format(new Date(year, month - 1, day));
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

export function expenseDateWindow(cycle: CropCycle): { minimum: string; maximum: string } {
  return {
    minimum: shiftYear(cycle.plannedStartDate, -1),
    maximum: shiftYear(cycle.actualHarvestDate ?? cycle.expectedHarvestDate, 1),
  };
}

export function expenseDraftFrom(
  expense: CultivationExpense | null,
  cycle: CropCycle,
  today: string,
): ExpenseDraft {
  const window = expenseDateWindow(cycle);
  const suggestedDate = today < window.minimum
    ? window.minimum
    : today > window.maximum ? window.maximum : today;

  return expense
    ? {
      code: expense.code,
      expenseDate: expense.expenseDate,
      category: expense.category,
      description: expense.description,
      amount: String(expense.amount),
      payeeName: expense.payeeName ?? "",
      referenceNumber: expense.referenceNumber ?? "",
      evidenceUrl: expense.evidenceUrl ?? "",
      notes: expense.notes ?? "",
    }
    : {
      code: "",
      expenseDate: suggestedDate,
      category: 5,
      description: "",
      amount: "",
      payeeName: "",
      referenceNumber: "",
      evidenceUrl: "",
      notes: "",
    };
}

export function validateExpenseDraft(
  draft: ExpenseDraft,
  cycle: CropCycle,
  isCreate: boolean,
): string[] {
  const errors: string[] = [];
  const code = draft.code.trim().toUpperCase();
  const description = draft.description.trim();
  const amount = parseExpenseAmount(draft.amount);
  const window = expenseDateWindow(cycle);

  if (isCreate && !code) {
    errors.push("Kode biaya wajib diisi.");
  } else if (isCreate && !/^[A-Z0-9][A-Z0-9._-]{0,39}$/.test(code)) {
    errors.push("Kode maksimal 40 karakter dan hanya boleh berisi huruf, angka, titik, tanda hubung, atau garis bawah.");
  }
  if (!draft.expenseDate) {
    errors.push("Tanggal biaya wajib diisi.");
  } else if (draft.expenseDate < window.minimum || draft.expenseDate > window.maximum) {
    errors.push(`Tanggal biaya harus antara ${formatExpenseDate(window.minimum)} dan ${formatExpenseDate(window.maximum)}.`);
  }
  if (!Object.prototype.hasOwnProperty.call(expenseCategoryLabels, draft.category)) {
    errors.push("Kategori biaya tidak didukung.");
  }
  if (!description) {
    errors.push("Deskripsi biaya wajib diisi.");
  } else if (description.length > 250) {
    errors.push("Deskripsi biaya maksimal 250 karakter.");
  }
  if (amount === null || amount <= 0) {
    errors.push("Jumlah biaya harus berupa angka lebih dari nol dengan maksimal dua desimal.");
  }
  if (draft.payeeName.trim().length > 150) {
    errors.push("Nama penerima maksimal 150 karakter.");
  }
  if (draft.referenceNumber.trim().length > 100) {
    errors.push("Nomor referensi maksimal 100 karakter.");
  }
  if (draft.evidenceUrl.trim().length > 1000) {
    errors.push("Tautan bukti maksimal 1.000 karakter.");
  }
  if (draft.notes.trim().length > 1000) {
    errors.push("Catatan maksimal 1.000 karakter.");
  }

  return errors;
}

export function filterExpenses(
  expenses: CultivationExpense[],
  query: string,
  status: ExpenseStatusFilter,
  category: ExpenseCategoryFilter,
): CultivationExpense[] {
  const normalizedQuery = query.trim().toLocaleLowerCase("id-ID");

  return expenses
    .filter((expense) => status === "all" || expense.status === status)
    .filter((expense) => category === "all" || expense.category === category)
    .filter((expense) => !normalizedQuery || [
      expense.code,
      expense.description,
      expense.payeeName,
      expense.referenceNumber,
    ].some((value) => value?.toLocaleLowerCase("id-ID").includes(normalizedQuery)))
    .sort((left, right) => {
      const statusOrder = left.status - right.status;
      return statusOrder !== 0
        ? statusOrder
        : right.expenseDate.localeCompare(left.expenseDate);
    });
}

export function summarizeExpenses(expenses: CultivationExpense[]) {
  const recognized = expenses
    .filter((expense) => expense.status === 2)
    .reduce((total, expense) => total + expense.amount, 0);
  const draft = expenses
    .filter((expense) => expense.status === 1)
    .reduce((total, expense) => total + expense.amount, 0);
  const cancelled = expenses
    .filter((expense) => expense.status === 3)
    .reduce((total, expense) => total + expense.amount, 0);
  const categoryTotals = new Map<CultivationExpenseCategory, number>();
  for (const expense of expenses.filter((item) => item.status === 2)) {
    categoryTotals.set(
      expense.category,
      (categoryTotals.get(expense.category) ?? 0) + expense.amount,
    );
  }
  const topCategory = [...categoryTotals.entries()]
    .sort((left, right) => right[1] - left[1])[0] ?? null;

  return {
    recognized,
    draft,
    cancelled,
    confirmedCount: expenses.filter((expense) => expense.status === 2).length,
    draftCount: expenses.filter((expense) => expense.status === 1).length,
    cancelledCount: expenses.filter((expense) => expense.status === 3).length,
    topCategory: topCategory
      ? { category: topCategory[0], amount: topCategory[1] }
      : null,
  };
}
