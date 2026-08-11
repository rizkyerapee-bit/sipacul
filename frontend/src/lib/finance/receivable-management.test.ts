import { describe, expect, it } from "vitest";
import type { Sale, SalePayment, SaleReceivable } from "@/lib/api/contracts";
import {
  filterReceivables,
  formatFinanceCurrency,
  isDueSoon,
  isOverdue,
  parsePaymentAmount,
  paymentDraftFrom,
  summarizeReceivables,
  validatePaymentDraft,
  type ReceivableEntry,
} from "@/lib/finance/receivable-management";

const sale: Sale = {
  id: "sale-1",
  organizationId: "org-1",
  code: "PJL-001",
  saleDate: "2027-05-20",
  buyerName: "Koperasi Tani",
  buyerPhone: "0812",
  buyerAddress: null,
  paymentTerm: 2,
  dueDate: "2027-06-05",
  discountAmount: 0,
  subtotal: 1_000_000,
  totalAmount: 1_000_000,
  status: 2,
  confirmedAt: "2027-05-20T12:00:00Z",
  cancellationReason: null,
  notes: null,
  lines: [],
  createdAt: "2027-05-20T10:00:00Z",
  updatedAt: null,
};

function receivable(overrides: Partial<SaleReceivable> = {}): SaleReceivable {
  return {
    saleId: sale.id,
    saleCode: sale.code,
    saleDate: sale.saleDate,
    buyerName: sale.buyerName,
    paymentTerm: sale.paymentTerm,
    dueDate: sale.dueDate,
    saleTotalAmount: 1_000_000,
    confirmedPaidAmount: 250_000,
    outstandingReceivable: 750_000,
    paymentState: 2,
    isFullyPaid: false,
    hasCollectedRevenue: true,
    ...overrides,
  };
}

function entry(overrides: Partial<SaleReceivable> = {}): ReceivableEntry {
  return { sale, receivable: receivable(overrides), payments: [] };
}

describe("receivable management", () => {
  it("parses decimal input without accepting ambiguous values", () => {
    expect(parsePaymentAmount("125000,50")).toBe(125000.5);
    expect(parsePaymentAmount("0")).toBe(0);
    expect(parsePaymentAmount("12.345.000")).toBeNull();
    expect(parsePaymentAmount("abc")).toBeNull();
  });

  it("prepares a new payment from the buyer and outstanding balance", () => {
    expect(paymentDraftFrom(null, sale, 750_000, "2027-05-24")).toMatchObject({
      paymentDate: "2027-05-24",
      amount: "750000",
      paymentMethod: 2,
      receivedFrom: "Koperasi Tani",
    });
  });

  it("keeps an existing draft intact for editing", () => {
    const payment = {
      id: "payment-1",
      organizationId: "org-1",
      saleId: sale.id,
      code: "BYR-001",
      paymentDate: "2027-05-25",
      amount: 300_000,
      paymentMethod: 1,
      referenceNumber: null,
      receivedFrom: "Kasir",
      notes: null,
      status: 1,
      isCollectedRevenue: false,
      confirmedAt: null,
      cancellationReason: null,
      createdAt: "2027-05-25T08:00:00Z",
      updatedAt: null,
    } satisfies SalePayment;
    expect(paymentDraftFrom(payment, sale, 750_000, "2027-05-26")).toMatchObject({
      code: "BYR-001",
      paymentDate: "2027-05-25",
      amount: "300000",
      paymentMethod: 1,
    });
  });

  it("validates code, date, amount, balance, and text limits", () => {
    const errors = validatePaymentDraft({
      code: " kode tidak valid ",
      paymentDate: "2027-05-19",
      amount: "800000",
      paymentMethod: 2,
      referenceNumber: "R".repeat(101),
      receivedFrom: "P".repeat(151),
      notes: "N".repeat(1001),
    }, sale, 750_000, true);
    expect(errors).toHaveLength(6);
    expect(errors.join(" ")).toContain(formatFinanceCurrency(750_000));
  });

  it("identifies overdue and due-soon balances", () => {
    expect(isOverdue(entry({ dueDate: "2027-06-01" }), "2027-06-10")).toBe(true);
    expect(isDueSoon(entry({ dueDate: "2027-06-15" }), "2027-06-10")).toBe(true);
    expect(isOverdue(entry({ outstandingReceivable: 0 }), "2027-06-10")).toBe(false);
  });

  it("filters by buyer, payment state, and due condition", () => {
    const overdue = entry({ dueDate: "2027-06-01" });
    const paid = entry({
      dueDate: "2027-06-15",
      confirmedPaidAmount: 1_000_000,
      outstandingReceivable: 0,
      paymentState: 3,
      isFullyPaid: true,
    });
    expect(filterReceivables([paid, overdue], "koperasi", 2, "overdue", "2027-06-10"))
      .toEqual([overdue]);
  });

  it("summarizes billed, collected, outstanding, overdue, and collection rate", () => {
    const summary = summarizeReceivables([
      entry({ dueDate: "2027-06-01" }),
      entry({
        saleTotalAmount: 500_000,
        confirmedPaidAmount: 500_000,
        outstandingReceivable: 0,
        paymentState: 3,
        isFullyPaid: true,
      }),
    ], "2027-06-10");
    expect(summary).toEqual({
      billed: 1_500_000,
      collected: 750_000,
      outstanding: 750_000,
      overdueCount: 1,
      paidCount: 1,
      collectionRate: 50,
    });
  });
});
