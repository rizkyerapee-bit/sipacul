import { describe, expect, it } from "vitest";
import type { CropCycle, CultivationExpense } from "@/lib/api/contracts";
import {
  expenseDateWindow,
  expenseDraftFrom,
  filterExpenses,
  optionalExpenseText,
  parseExpenseAmount,
  summarizeExpenses,
  validateExpenseDraft,
} from "@/lib/finance/expense-management";

const cycle: CropCycle = {
  id: "cycle-1",
  organizationId: "org-1",
  code: "NNS-2027-01",
  name: "Nanas Musim 2027",
  commodityId: "commodity-1",
  cultivationSopId: null,
  landId: "land-1",
  landPlotId: "plot-1",
  plantedArea: 1,
  areaUnit: 2,
  plantedAreaInSquareMeters: 10_000,
  plannedStartDate: "2027-01-10",
  expectedHarvestDate: "2027-12-20",
  actualStartDate: "2027-01-12",
  actualHarvestDate: null,
  status: 2,
  cancellationReason: null,
  notes: null,
  createdAt: "2026-12-01T00:00:00Z",
  updatedAt: null,
};

function expense(
  id: string,
  status: 1 | 2 | 3,
  amount: number,
  category: 1 | 2 | 3 | 4 | 5 = 5,
): CultivationExpense {
  return {
    id,
    organizationId: "org-1",
    cropCycleId: cycle.id,
    code: `EXP-${id}`,
    expenseDate: `2027-02-0${id}`,
    category,
    description: id === "1" ? "Upah pengolahan lahan" : "Pembelian pupuk",
    amount,
    payeeName: id === "1" ? "Kelompok Tani Maju" : null,
    referenceNumber: id === "2" ? "INV-PUPUK" : null,
    evidenceUrl: null,
    notes: null,
    status,
    isRecognizedCost: status === 2,
    confirmedAt: status === 2 ? "2027-02-10T00:00:00Z" : null,
    cancellationReason: status === 3 ? "Duplikat" : null,
    createdAt: "2027-02-01T00:00:00Z",
    updatedAt: null,
  };
}

describe("expense management", () => {
  it("normalizes optional text and parses positive decimal values", () => {
    expect(optionalExpenseText("  INV-01  ")).toBe("INV-01");
    expect(optionalExpenseText("  ")).toBeNull();
    expect(parseExpenseAmount("1 250 000,50")).toBe(1_250_000.5);
    expect(parseExpenseAmount("10.999")).toBeNull();
    expect(parseExpenseAmount("-1")).toBeNull();
  });

  it("uses the backend-compatible one-year expense window", () => {
    expect(expenseDateWindow(cycle)).toEqual({
      minimum: "2026-01-10",
      maximum: "2028-12-20",
    });
  });

  it("creates a practical default draft inside the cycle window", () => {
    expect(expenseDraftFrom(null, cycle, "2030-01-01")).toMatchObject({
      expenseDate: "2028-12-20",
      category: 5,
      amount: "",
    });
  });

  it("rejects invalid codes, dates, values, and required descriptions", () => {
    expect(validateExpenseDraft({
      code: "EXP / 01",
      expenseDate: "2025-12-31",
      category: 5,
      description: "",
      amount: "0",
      payeeName: "",
      referenceNumber: "",
      evidenceUrl: "",
      notes: "",
    }, cycle, true)).toEqual(expect.arrayContaining([
      expect.stringContaining("Kode maksimal"),
      expect.stringContaining("Tanggal biaya harus"),
      "Deskripsi biaya wajib diisi.",
      expect.stringContaining("Jumlah biaya"),
    ]));
  });

  it("accepts a valid draft and does not revalidate immutable code on edit", () => {
    const valid = {
      code: "EXP-001",
      expenseDate: "2027-02-15",
      category: 3 as const,
      description: "Pupuk dasar",
      amount: "750000",
      payeeName: "Kios Tani",
      referenceNumber: "INV-01",
      evidenceUrl: "https://example.test/inv-01",
      notes: "Transfer",
    };
    expect(validateExpenseDraft(valid, cycle, true)).toEqual([]);
    expect(validateExpenseDraft({ ...valid, code: "kode lama / server" }, cycle, false)).toEqual([]);
  });

  it("filters by query, status, and category then prioritizes draft records", () => {
    const records = [
      expense("1", 2, 1_000_000, 5),
      expense("2", 1, 500_000, 3),
      expense("3", 3, 250_000, 3),
    ];
    expect(filterExpenses(records, "pupuk", "all", "all").map((item) => item.id))
      .toEqual(["2", "3"]);
    expect(filterExpenses(records, "", 2, 5).map((item) => item.id))
      .toEqual(["1"]);
  });

  it("counts only confirmed expenses as recognized cultivation cost", () => {
    const summary = summarizeExpenses([
      expense("1", 2, 1_000_000, 5),
      expense("2", 2, 600_000, 3),
      expense("3", 1, 500_000, 3),
      expense("4", 3, 250_000, 5),
    ]);
    expect(summary).toMatchObject({
      recognized: 1_600_000,
      draft: 500_000,
      cancelled: 250_000,
      confirmedCount: 2,
      draftCount: 1,
      cancelledCount: 1,
      topCategory: { category: 5, amount: 1_000_000 },
    });
  });
});
