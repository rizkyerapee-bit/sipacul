import { describe, expect, it } from "vitest";
import type { HarvestBatch, Sale } from "@/lib/api/contracts";
import {
  calculateSaleLineTotal,
  filterSales,
  saleDraftFrom,
  selectableInventory,
  summarizeSales,
  validateSaleDraft,
  validateSaleLineDraft,
} from "@/lib/sales/sale-management";

const batch: HarvestBatch = {
  id: "harvest-1",
  organizationId: "org-1",
  cropCycleId: "cycle-1",
  code: "PNN-001",
  harvestDate: "2027-05-20",
  grossQuantity: 1250,
  rejectedQuantity: 50,
  netQuantity: 1200,
  quantityUnit: 1,
  qualityGrade: "Grade A",
  storageLocation: "Gudang Timur",
  notes: null,
  status: 2,
  confirmedAt: "2027-05-20T08:00:00Z",
  cancellationReason: null,
  confirmedSoldQuantity: 300,
  availableQuantity: 900,
  createdAt: "2027-05-20T07:00:00Z",
  updatedAt: null,
};

const sale: Sale = {
  id: "sale-1",
  organizationId: "org-1",
  code: "PJL-001",
  saleDate: "2027-05-22",
  buyerName: "Koperasi Tani",
  buyerPhone: "08123456789",
  buyerAddress: "Pasar Induk",
  paymentTerm: 2,
  dueDate: "2027-06-05",
  discountAmount: 100000,
  subtotal: 6000000,
  totalAmount: 5900000,
  status: 2,
  confirmedAt: "2027-05-22T08:00:00Z",
  cancellationReason: null,
  notes: "Pengiriman pagi",
  lines: [{
    id: "line-1",
    harvestBatchId: batch.id,
    harvestBatchCodeSnapshot: batch.code,
    cropCycleIdSnapshot: "cycle-1",
    cropCycleCodeSnapshot: "SB-01",
    commodityIdSnapshot: "commodity-1",
    commodityCodeSnapshot: "NNS",
    commodityNameSnapshot: "Nanas",
    qualityGradeSnapshot: "Grade A",
    quantity: 600,
    quantityUnit: 1,
    unitPrice: 10000,
    lineDiscount: 0,
    lineTotal: 6000000,
    notes: null,
    createdAt: "2027-05-22T07:00:00Z",
    updatedAt: null,
  }],
  createdAt: "2027-05-22T07:00:00Z",
  updatedAt: null,
};

describe("sale management helpers", () => {
  it("validates cash and credit sale headers", () => {
    expect(validateSaleDraft(saleDraftFrom(sale), false, sale.subtotal)).toEqual([]);
    const invalid = saleDraftFrom(null);
    invalid.code = "PJL 01";
    invalid.saleDate = "2027-05-22";
    invalid.buyerName = "Pembeli";
    invalid.paymentTerm = 2;
    expect(validateSaleDraft(invalid, true, 0)).toEqual(expect.arrayContaining([
      expect.stringContaining("Kode penjualan"),
      expect.stringContaining("jatuh tempo"),
    ]));
  });

  it("validates stock and calculates line totals", () => {
    const draft = {
      harvestBatchId: batch.id,
      quantity: "12,5",
      unitPrice: "10000",
      lineDiscount: "5000",
      notes: "",
    };
    expect(validateSaleLineDraft(draft, batch)).toEqual([]);
    expect(calculateSaleLineTotal(draft)).toBe(120000);
    expect(validateSaleLineDraft({ ...draft, quantity: "901" }, batch)[0])
      .toContain("stok tersedia");
  });

  it("filters sales by metadata, status, and payment term", () => {
    const draft = { ...sale, id: "sale-2", code: "PJL-002", status: 1 as const };
    expect(filterSales([sale, draft], "koperasi", 2, 2)).toEqual([sale]);
  });

  it("summarizes only confirmed revenue and separates drafts and credit", () => {
    const draft = { ...sale, id: "sale-2", status: 1 as const, totalAmount: 250000 };
    expect(summarizeSales([sale, draft])).toEqual({
      saleCount: 2,
      confirmedCount: 1,
      confirmedRevenue: 5900000,
      draftValue: 250000,
      creditRevenue: 5900000,
    });
  });

  it("excludes batches already used by another line", () => {
    const secondBatch = { ...batch, id: "harvest-2", code: "PNN-002" };
    const inventory = [batch, secondBatch].map((item) => ({
      batch: item,
      cropCycleCode: "SB-01",
      cropCycleName: "Nanas Timur",
    }));
    expect(selectableInventory(inventory, { ...sale, status: 1 }, null)
      .map((item) => item.batch.id)).toEqual(["harvest-2"]);
    expect(selectableInventory(inventory, { ...sale, status: 1 }, "line-1")
      .map((item) => item.batch.id)).toEqual(["harvest-1", "harvest-2"]);
  });
});
