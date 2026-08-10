import type {
  HarvestBatch,
  HarvestQuantityUnit,
  Sale,
  SaleLine,
  SalePaymentTerm,
  SaleStatus,
} from "@/lib/api/contracts";

export type SaleStatusFilter = "all" | SaleStatus;
export type SalePaymentTermFilter = "all" | SalePaymentTerm;

export type SaleDraft = {
  code: string;
  saleDate: string;
  buyerName: string;
  buyerPhone: string;
  buyerAddress: string;
  paymentTerm: SalePaymentTerm;
  dueDate: string;
  discountAmount: string;
  notes: string;
};

export type SaleLineDraft = {
  harvestBatchId: string;
  quantity: string;
  unitPrice: string;
  lineDiscount: string;
  notes: string;
};

export type HarvestInventoryItem = {
  batch: HarvestBatch;
  cropCycleCode: string;
  cropCycleName: string;
};

export const saleStatusLabels: Record<SaleStatus, string> = {
  1: "Draf",
  2: "Dikonfirmasi",
  3: "Dibatalkan",
};

export const salePaymentTermLabels: Record<SalePaymentTerm, string> = {
  1: "Tunai",
  2: "Kredit",
};

const currencyFormatter = new Intl.NumberFormat("id-ID", {
  style: "currency",
  currency: "IDR",
  maximumFractionDigits: 0,
});

const numberFormatter = new Intl.NumberFormat("id-ID", {
  maximumFractionDigits: 4,
});

const dateFormatter = new Intl.DateTimeFormat("id-ID", {
  day: "2-digit",
  month: "short",
  year: "numeric",
  timeZone: "UTC",
});

export function formatSaleCurrency(value: number): string {
  return currencyFormatter.format(value);
}

export function formatSaleDate(value: string | null): string {
  if (!value) return "Belum ditentukan";
  const parsed = new Date(`${value}T00:00:00Z`);
  return Number.isNaN(parsed.getTime()) ? value : dateFormatter.format(parsed);
}

export function formatSaleQuantity(
  value: number,
  unit: HarvestQuantityUnit,
): string {
  const symbols: Record<HarvestQuantityUnit, string> = {
    1: "kg",
    2: "ton",
    3: "kuintal",
    4: "buah",
    5: "tandan",
    6: "karung",
    7: "peti",
    8: "L",
  };
  return `${numberFormatter.format(value)} ${symbols[unit]}`;
}

export function parseSaleNumber(
  value: string,
  allowZero = false,
): number | null {
  const normalized = value.trim().replace(/\s/g, "").replace(",", ".");
  if (!normalized) return null;
  const parsed = Number(normalized);
  if (!Number.isFinite(parsed)) return null;
  return allowZero ? (parsed >= 0 ? parsed : null) : (parsed > 0 ? parsed : null);
}

export function optionalSaleText(value: string): string | null {
  return value.trim() || null;
}

export function saleDraftFrom(sale: Sale | null): SaleDraft {
  return {
    code: sale?.code ?? "",
    saleDate: sale?.saleDate ?? "",
    buyerName: sale?.buyerName ?? "",
    buyerPhone: sale?.buyerPhone ?? "",
    buyerAddress: sale?.buyerAddress ?? "",
    paymentTerm: sale?.paymentTerm ?? 1,
    dueDate: sale?.dueDate ?? "",
    discountAmount: sale ? String(sale.discountAmount) : "0",
    notes: sale?.notes ?? "",
  };
}

export function saleLineDraftFrom(
  line: SaleLine | null,
  inventory: HarvestInventoryItem[],
): SaleLineDraft {
  const firstBatchId = inventory.find((item) => item.batch.availableQuantity > 0)
    ?.batch.id ?? "";
  return {
    harvestBatchId: line?.harvestBatchId ?? firstBatchId,
    quantity: line ? String(line.quantity) : "",
    unitPrice: line ? String(line.unitPrice) : "",
    lineDiscount: line ? String(line.lineDiscount) : "0",
    notes: line?.notes ?? "",
  };
}

function validDate(value: string): boolean {
  return /^\d{4}-\d{2}-\d{2}$/.test(value)
    && !Number.isNaN(Date.parse(`${value}T00:00:00Z`));
}

function validateOptionalLength(
  value: string,
  label: string,
  maximumLength: number,
): string | null {
  return value.trim().length > maximumLength
    ? `${label} maksimal ${maximumLength} karakter.`
    : null;
}

export function validateSaleDraft(
  draft: SaleDraft,
  isCreate: boolean,
  subtotal: number,
): string[] {
  const errors: string[] = [];
  const code = draft.code.trim();
  const buyer = draft.buyerName.trim();

  if (isCreate && !code) errors.push("Kode penjualan wajib diisi.");
  if (isCreate && code && !/^[A-Za-z0-9][A-Za-z0-9._-]*$/.test(code)) {
    errors.push("Kode penjualan hanya boleh memakai huruf, angka, titik, tanda hubung, atau garis bawah.");
  }
  if (code.length > 40) errors.push("Kode penjualan maksimal 40 karakter.");
  if (!validDate(draft.saleDate)) errors.push("Tanggal penjualan wajib diisi.");
  if (!buyer) errors.push("Nama pembeli wajib diisi.");
  if (buyer.length > 150) errors.push("Nama pembeli maksimal 150 karakter.");

  [
    validateOptionalLength(draft.buyerPhone, "Nomor telepon", 50),
    validateOptionalLength(draft.buyerAddress, "Alamat pembeli", 500),
    validateOptionalLength(draft.notes, "Catatan", 1000),
  ].forEach((error) => { if (error) errors.push(error); });

  if (draft.paymentTerm !== 1 && draft.paymentTerm !== 2) {
    errors.push("Termin pembayaran wajib dipilih.");
  }
  if (draft.paymentTerm === 2 && !validDate(draft.dueDate)) {
    errors.push("Tanggal jatuh tempo wajib diisi untuk penjualan kredit.");
  }
  if (draft.paymentTerm === 2 && validDate(draft.dueDate)
    && validDate(draft.saleDate) && draft.dueDate < draft.saleDate) {
    errors.push("Tanggal jatuh tempo tidak boleh sebelum tanggal penjualan.");
  }

  const discount = parseSaleNumber(draft.discountAmount, true);
  if (discount === null) errors.push("Diskon transaksi tidak boleh negatif.");
  if (discount !== null && discount > subtotal) {
    errors.push("Diskon transaksi tidak boleh melebihi subtotal.");
  }
  if (isCreate && discount !== null && discount !== 0) {
    errors.push("Tambahkan item terlebih dahulu sebelum mengatur diskon transaksi.");
  }

  return errors;
}

export function validateSaleLineDraft(
  draft: SaleLineDraft,
  batch: HarvestBatch | null,
): string[] {
  const errors: string[] = [];
  if (!draft.harvestBatchId || !batch) {
    errors.push("Batch panen wajib dipilih.");
    return errors;
  }

  const quantity = parseSaleNumber(draft.quantity);
  const unitPrice = parseSaleNumber(draft.unitPrice, true);
  const lineDiscount = parseSaleNumber(draft.lineDiscount, true);
  if (quantity === null) errors.push("Kuantitas harus lebih besar dari nol.");
  if (unitPrice === null) errors.push("Harga satuan tidak boleh negatif.");
  if (lineDiscount === null) errors.push("Diskon item tidak boleh negatif.");

  if (quantity !== null && quantity > batch.availableQuantity) {
    errors.push(`Kuantitas melebihi stok tersedia ${formatSaleQuantity(batch.availableQuantity, batch.quantityUnit)}.`);
  }
  if (quantity !== null && unitPrice !== null && lineDiscount !== null
    && lineDiscount > quantity * unitPrice) {
    errors.push("Diskon item tidak boleh melebihi nilai kotor item.");
  }
  if (draft.notes.trim().length > 500) {
    errors.push("Catatan item maksimal 500 karakter.");
  }
  return errors;
}

export function calculateSaleLineTotal(draft: SaleLineDraft): number {
  const quantity = parseSaleNumber(draft.quantity) ?? 0;
  const unitPrice = parseSaleNumber(draft.unitPrice, true) ?? 0;
  const discount = parseSaleNumber(draft.lineDiscount, true) ?? 0;
  return Math.max(0, quantity * unitPrice - discount);
}

export function filterSales(
  sales: Sale[],
  query: string,
  status: SaleStatusFilter,
  paymentTerm: SalePaymentTermFilter,
): Sale[] {
  const normalizedQuery = query.trim().toLocaleLowerCase("id-ID");
  return sales
    .filter((sale) => status === "all" || sale.status === status)
    .filter((sale) => paymentTerm === "all" || sale.paymentTerm === paymentTerm)
    .filter((sale) => {
      if (!normalizedQuery) return true;
      return [sale.code, sale.buyerName, sale.buyerPhone ?? "", sale.notes ?? ""]
        .some((value) => value.toLocaleLowerCase("id-ID").includes(normalizedQuery));
    })
    .sort((left, right) => {
      const dateOrder = right.saleDate.localeCompare(left.saleDate);
      return dateOrder !== 0 ? dateOrder : left.code.localeCompare(right.code);
    });
}

export function summarizeSales(sales: Sale[]): {
  saleCount: number;
  confirmedCount: number;
  confirmedRevenue: number;
  draftValue: number;
  creditRevenue: number;
} {
  const confirmed = sales.filter((sale) => sale.status === 2);
  return {
    saleCount: sales.length,
    confirmedCount: confirmed.length,
    confirmedRevenue: confirmed.reduce((total, sale) => total + sale.totalAmount, 0),
    draftValue: sales.filter((sale) => sale.status === 1)
      .reduce((total, sale) => total + sale.totalAmount, 0),
    creditRevenue: confirmed.filter((sale) => sale.paymentTerm === 2)
      .reduce((total, sale) => total + sale.totalAmount, 0),
  };
}

export function selectableInventory(
  inventory: HarvestInventoryItem[],
  sale: Sale,
  editingLineId: string | null,
): HarvestInventoryItem[] {
  const editingLine = sale.lines.find((line) => line.id === editingLineId) ?? null;
  const usedBatchIds = new Set(
    sale.lines
      .filter((line) => line.id !== editingLineId)
      .map((line) => line.harvestBatchId),
  );

  return inventory.filter((item) =>
    item.batch.status === 2
    && !usedBatchIds.has(item.batch.id)
    && (item.batch.availableQuantity > 0
      || item.batch.id === editingLine?.harvestBatchId));
}
