import { describe, expect, it } from "vitest";
import type {
  ProfitSharingScheme,
  ProfitSharingSchemeResidualShareRequest,
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
  profitSharingSchemeStatusLabels,
  profitSharingSchemeUsesPassiveInvestor,
  profitSharingWaterfallStatusLabels,
  summarizeProfitSharingSchemes,
  validateProfitSharingSchemeDraft,
} from "@/lib/finance/profit-sharing-v2-management";

function readyManagedDraft() {
  const draft = createProfitSharingSchemeDraft("managed");
  draft.code = "MITRA-1";
  return draft;
}

function schemeFixture(): ProfitSharingScheme {
  return {
    id: "scheme-1",
    organizationId: "org-1",
    schemeFamilyId: "family-1",
    code: "MITRA-1",
    name: "Skema Mitra",
    description: null,
    version: 2,
    status: 1,
    residualMethod: 2,
    residualRecipientCode: null,
    activatedAt: null,
    supersededAt: null,
    createdAt: "2026-08-15T00:00:00Z",
    updatedAt: null,
    participants: [
      {
        id: "participant-2",
        participantCode: "MITRA",
        participantName: "Mitra",
        participantRole: 3,
        participatesInResidualProfit: true,
        sequence: 2,
      },
      {
        id: "participant-1",
        participantCode: "PERUSAHAAN",
        participantName: "Perusahaan",
        participantRole: 1,
        participatesInResidualProfit: true,
        sequence: 1,
      },
    ],
    priorityRules: [
      {
        id: "rule-1",
        ruleCode: "BIAYA-KELOLA",
        ruleType: 1,
        recipientCode: "MITRA",
        rateNumerator: 1,
        rateDenominator: 3,
        sequence: 1,
      },
    ],
    residualShares: [],
  };
}

describe("profit-sharing V2 management", () => {
  it("provides complete Indonesian labels for every V2 enum", () => {
    expect(Object.keys(profitSharingParticipantRoleLabels)).toHaveLength(4);
    expect(Object.keys(profitSharingPriorityRuleTypeLabels)).toHaveLength(2);
    expect(Object.keys(profitSharingResidualMethodLabels)).toHaveLength(3);
    expect(Object.keys(profitSharingSchemeStatusLabels)).toHaveLength(3);
    expect(Object.keys(profitSharingWaterfallStatusLabels)).toHaveLength(2);
  });

  it("creates an internal-company preset", () => {
    const draft = createProfitSharingSchemeDraft("internal");

    expect(draft.participants).toHaveLength(1);
    expect(draft.participants[0]).toMatchObject({
      participantCode: "PERUSAHAAN",
      participantRole: 1,
    });
    expect(draft.residualMethod).toBe(1);
    expect(draft.residualRecipientCode).toBe("PERUSAHAAN");
  });

  it("creates a managed-partner preset with a one-third priority rule", () => {
    const draft = createProfitSharingSchemeDraft("managed");

    expect(draft.participants.map((item) => item.participantRole)).toEqual([1, 3]);
    expect(draft.priorityRules[0]).toMatchObject({
      ruleType: 1,
      recipientCode: "MITRA",
      rateNumerator: "1",
      rateDenominator: "3",
    });
    expect(draft.residualMethod).toBe(2);
  });

  it("creates a passive-investor preset without assuming a fixed return", () => {
    const draft = createProfitSharingSchemeDraft("passive-investor");

    expect(draft.participants.map((item) => item.participantRole)).toEqual([1, 2]);
    expect(draft.priorityRules).toEqual([]);
    expect(draft.residualMethod).toBe(2);
  });

  it("builds an editor draft from a persisted scheme in sequence order", () => {
    const draft = profitSharingSchemeDraftFrom(schemeFixture());

    expect(draft.participants.map((item) => item.participantCode))
      .toEqual(["PERUSAHAAN", "MITRA"]);
    expect(draft.priorityRules[0].rateNumerator).toBe("1");
    expect(draft.description).toBe("");
  });

  it("moves an item forward and rebuilds contiguous sequences", () => {
    const participants = readyManagedDraft().participants;
    const moved = moveProfitSharingSchemeItem(participants, 0, 1);

    expect(moved.map((item) => item.participantCode)).toEqual(["MITRA", "PERUSAHAAN"]);
    expect(moved.map((item) => item.sequence)).toEqual([1, 2]);
  });

  it("moves an item backward without mutating the source array", () => {
    const participants = readyManagedDraft().participants;
    const moved = moveProfitSharingSchemeItem(participants, 1, 0);

    expect(moved[0].participantCode).toBe("MITRA");
    expect(participants[0].participantCode).toBe("PERUSAHAAN");
  });

  it("returns the source array when a move index is invalid", () => {
    const participants = readyManagedDraft().participants;

    expect(moveProfitSharingSchemeItem(participants, -1, 0)).toBe(participants);
    expect(moveProfitSharingSchemeItem(participants, 0, 9)).toBe(participants);
    expect(moveProfitSharingSchemeItem(participants, 1, 1)).toBe(participants);
  });

  it("accepts a complete managed-partner draft", () => {
    expect(validateProfitSharingSchemeDraft(readyManagedDraft())).toEqual([]);
  });

  it("requires a valid code, name, and at least one participant", () => {
    const draft = createProfitSharingSchemeDraft();
    draft.name = " ";
    draft.participants = [];

    const errors = validateProfitSharingSchemeDraft(draft);
    expect(errors).toContain("Kode skema wajib memakai format kode maksimal 40 karakter.");
    expect(errors).toContain("Nama skema wajib diisi dan maksimal 150 karakter.");
    expect(errors).toContain("Skema harus memiliki minimal satu peserta.");
  });

  it("rejects duplicate participant codes after normalization", () => {
    const draft = readyManagedDraft();
    draft.participants[1].participantCode = " perusahaan ";

    expect(validateProfitSharingSchemeDraft(draft))
      .toContain("Kode setiap peserta harus unik.");
  });

  it("rejects invalid participant identity and role", () => {
    const draft = readyManagedDraft();
    draft.participants[1].participantCode = "kode tidak valid";
    draft.participants[1].participantName = "";
    draft.participants[1].participantRole = 9 as never;

    const errors = validateProfitSharingSchemeDraft(draft);
    expect(errors.some((error) => error.startsWith("Kode peserta"))).toBe(true);
    expect(errors.some((error) => error.startsWith("Nama peserta"))).toBe(true);
    expect(errors.some((error) => error.startsWith("Peran peserta"))).toBe(true);
  });

  it("rejects a priority rule with a missing recipient or invalid rate", () => {
    const draft = readyManagedDraft();
    draft.priorityRules[0].recipientCode = "TIDAK-ADA";
    draft.priorityRules[0].rateNumerator = "4";
    draft.priorityRules[0].rateDenominator = "3";

    const errors = validateProfitSharingSchemeDraft(draft);
    expect(errors.some((error) => error.startsWith("Penerima aturan"))).toBe(true);
    expect(errors.some((error) => error.startsWith("Tarif aturan"))).toBe(true);
  });

  it("rejects duplicate priority rule codes", () => {
    const draft = readyManagedDraft();
    draft.priorityRules.push({ ...draft.priorityRules[0], sequence: 2 });

    expect(validateProfitSharingSchemeDraft(draft))
      .toContain("Kode setiap aturan prioritas harus unik.");
  });

  it("requires a known recipient for remainder-to-participant", () => {
    const draft = createProfitSharingSchemeDraft("internal");
    draft.code = "INTERNAL";
    draft.residualRecipientCode = "TIDAK-ADA";

    expect(validateProfitSharingSchemeDraft(draft))
      .toContain("Penerima sisa laba harus berasal dari daftar peserta.");
  });

  it("requires an eligible participant for pro-rata residual profit", () => {
    const draft = readyManagedDraft();
    draft.participants.forEach((participant) => {
      participant.participatesInResidualProfit = false;
    });

    expect(validateProfitSharingSchemeDraft(draft))
      .toContain("Metode proporsional membutuhkan minimal satu peserta laba tersisa.");
  });

  it("does not allow pro-rata residual fields from another method", () => {
    const draft = readyManagedDraft();
    draft.residualRecipientCode = "PERUSAHAAN";

    expect(validateProfitSharingSchemeDraft(draft))
      .toContain("Metode proporsional tidak memakai penerima tunggal atau persentase tetap.");
  });

  it("rejects duplicate or incomplete fixed residual shares", () => {
    const draft = readyManagedDraft();
    draft.residualMethod = 3;
    const share: ProfitSharingSchemeResidualShareRequest = {
      recipientCode: "PERUSAHAAN",
      rateNumerator: 1,
      rateDenominator: 4,
      sequence: 1,
    };
    draft.residualShares = [
      { ...share, rateNumerator: "1", rateDenominator: "4" },
      { ...share, rateNumerator: "1", rateDenominator: "4", sequence: 2 },
    ];

    const errors = validateProfitSharingSchemeDraft(draft);
    expect(errors).toContain("Penerima persentase sisa harus unik.");
    expect(errors).toContain("Total persentase laba tersisa harus tepat 100%.");
  });

  it("accepts fixed residual shares totaling one", () => {
    const draft = readyManagedDraft();
    draft.residualMethod = 3;
    draft.residualShares = [
      {
        recipientCode: "PERUSAHAAN",
        rateNumerator: "4",
        rateDenominator: "5",
        sequence: 1,
      },
      {
        recipientCode: "MITRA",
        rateNumerator: "1",
        rateDenominator: "5",
        sequence: 2,
      },
    ];

    expect(validateProfitSharingSchemeDraft(draft)).toEqual([]);
  });

  it("normalizes create and update requests without leaking the immutable code", () => {
    const draft = readyManagedDraft();
    draft.code = " mitra.utama ";
    draft.name = "  Skema Mitra Utama  ";
    draft.description = " ";
    draft.participants[1].participantCode = " mitra ";

    const createRequest = buildCreateProfitSharingSchemeRequest(draft);
    const updateRequest = buildUpdateProfitSharingSchemeRequest(draft);

    expect(createRequest.code).toBe("MITRA.UTAMA");
    expect(createRequest.name).toBe("Skema Mitra Utama");
    expect(createRequest.description).toBeNull();
    expect(createRequest.participants[1].participantCode).toBe("MITRA");
    expect(updateRequest).not.toHaveProperty("code");
  });

  it("formats stored fractions as localized percentages", () => {
    expect(formatProfitSharingRate(1, 3)).toBe("33,3333%");
    expect(formatProfitSharingRate(1, 0)).toBe("—");
  });

  it("summarizes versions separately from scheme families", () => {
    const active = { ...schemeFixture(), id: "active", status: 2 as const };
    const superseded = {
      ...schemeFixture(),
      id: "old",
      status: 3 as const,
      version: 1,
    };
    const anotherDraft = {
      ...schemeFixture(),
      id: "other",
      schemeFamilyId: "family-2",
      code: "INTERNAL",
    };

    expect(summarizeProfitSharingSchemes([active, superseded, anotherDraft]))
      .toEqual({ total: 3, families: 2, draft: 1, active: 1, superseded: 1 });
  });

  it("filters schemes by status and searchable identity", () => {
    const managed = schemeFixture();
    const internal = {
      ...schemeFixture(),
      id: "scheme-2",
      code: "INTERNAL",
      name: "Internal Perusahaan",
      description: "Seluruhnya dikelola perusahaan",
      status: 2 as const,
    };

    expect(filterProfitSharingSchemes([managed, internal], "mitra", "all"))
      .toEqual([managed]);
    expect(filterProfitSharingSchemes([managed, internal], "", 2))
      .toEqual([internal]);
  });

  it("detects a passive investor from participant roles", () => {
    const scheme = schemeFixture();
    expect(profitSharingSchemeUsesPassiveInvestor(scheme)).toBe(false);

    scheme.participants.push({
      id: "participant-3",
      participantCode: "INVESTOR-PASIF",
      participantName: "Investor Pasif",
      participantRole: 2,
      participatesInResidualProfit: true,
      sequence: 3,
    });
    expect(profitSharingSchemeUsesPassiveInvestor(scheme)).toBe(true);
  });
});
