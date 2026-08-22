import { beforeEach, describe, expect, it, vi } from "vitest";
import {
  addCultivationActivityResource,
  addLandPlot,
  addSaleLine,
  activateProfitSharingScheme,
  ApiError,
  assignProfitSharingScheme,
  bootstrapOwner,
  cancelCapitalContribution,
  cancelCultivationExpense,
  cancelHarvestBatch,
  cancelCropCycle,
  cancelCultivationActivity,
  cancelSale,
  cancelSalePayment,
  completeCropCycle,
  completeCultivationActivity,
  confirmHarvestBatch,
  confirmCapitalContribution,
  confirmCultivationExpense,
  confirmSale,
  confirmSalePayment,
  createCropCycle,
  createCapitalContribution,
  createNextProfitSharingSchemeVersion,
  createProfitSharingScheme,
  createProfitSharingSettlement,
  createCultivationActivity,
  createCultivationExpense,
  createLand,
  createHarvestBatch,
  createSale,
  createSalePayment,
  deleteLand,
  getBootstrapStatus,
  getCapitalContributions,
  getCommodities,
  getCropCycleProfitability,
  getCropCycles,
  getCultivationSops,
  getCultivationActivities,
  getCultivationExpenses,
  getCurrentUser,
  getHarvestBatches,
  getLands,
  getLandSeasonHistory,
  getOrganization,
  getProfitSharingPreview,
  getProfitSharingScheme,
  getProfitSharingSchemeAssignment,
  getProfitSharingSchemes,
  getProfitSharingSettlements,
  getProfitSharingWaterfallSettlement,
  getProfitSharingWaterfallSettlements,
  getSales,
  getSalePayments,
  getSaleReceivable,
  login,
  logout,
  removeLandPlot,
  removeCultivationActivityResource,
  removeSaleLine,
  setLandActive,
  setLandPlotActive,
  startCropCycle,
  startCultivationActivity,
  updateCultivationActivityNotes,
  updateCapitalContribution,
  updateCultivationActivityPlan,
  updateCultivationActivityResource,
  updateCultivationExpense,
  updateCropCycleNotes,
  updateCropCyclePlan,
  updateLand,
  updateLandPlot,
  updateHarvestBatch,
  updateSale,
  updateSalePayment,
  updateSaleLine,
  updateProfitSharingSettlement,
  updateProfitSharingScheme,
  finalizeProfitSharingSettlement,
  finalizeProfitSharingWaterfallSettlement,
  voidProfitSharingSettlement,
  voidProfitSharingWaterfallSettlement,
} from "@/lib/api/client";

const fetchMock = vi.fn<typeof fetch>();

function jsonResponse(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { "content-type": "application/json" },
  });
}

describe("SiPacul API client", () => {
  beforeEach(() => {
    fetchMock.mockReset();
    vi.stubGlobal("fetch", fetchMock);
  });

  it("reads bootstrap status without credentials in source code", async () => {
    fetchMock.mockResolvedValueOnce(jsonResponse({ isConfigured: true, isInitialized: false, canBootstrap: true }));

    await expect(getBootstrapStatus()).resolves.toMatchObject({ canBootstrap: true });
    expect(fetchMock).toHaveBeenCalledWith("/api/v1/bootstrap/status", expect.objectContaining({ credentials: "include", cache: "no-store" }));
  });

  it("gets the current authenticated user", async () => {
    fetchMock.mockResolvedValueOnce(jsonResponse({ userId: "user-1", email: "owner@example.com", memberships: [] }));

    await expect(getCurrentUser()).resolves.toMatchObject({ email: "owner@example.com" });
  });

  it("gets an organization by the membership organization identifier", async () => {
    fetchMock.mockResolvedValueOnce(jsonResponse({ id: "org 1", code: "ORG", name: "Farm" }));

    await getOrganization("org 1");
    expect(fetchMock.mock.calls[0][0]).toBe("/api/v1/organizations/org%201");
  });

  it("reads organization dashboard collections with encoded identifiers", async () => {
    fetchMock
      .mockResolvedValueOnce(jsonResponse([]))
      .mockResolvedValueOnce(jsonResponse([]))
      .mockResolvedValueOnce(jsonResponse([]))
      .mockResolvedValueOnce(jsonResponse([]));

    await getLands("org 1");
    await getCropCycles("org 1");
    await getCommodities("org 1");
    await getCultivationSops("org 1");

    expect(fetchMock.mock.calls[0][0]).toBe("/api/v1/organizations/org%201/lands");
    expect(fetchMock.mock.calls[1][0]).toBe("/api/v1/organizations/org%201/crop-cycles");
    expect(fetchMock.mock.calls[2][0]).toBe("/api/v1/organizations/org%201/commodities");
    expect(fetchMock.mock.calls[3][0]).toBe("/api/v1/organizations/org%201/cultivation-sops");
  });

  it("reads paged land season history with encoded filters", async () => {
    fetchMock.mockResolvedValueOnce(jsonResponse({
      landId: "land 1",
      page: 2,
      pageSize: 10,
      seasons: [],
    }));

    await getLandSeasonHistory("org 1", "land 1", {
      landPlotId: "plot 1",
      includeNonTerminal: true,
      page: 2,
      pageSize: 10,
    });

    expect(fetchMock.mock.calls[0][0]).toBe(
      "/api/v1/organizations/org%201/lands/land%201/season-history" +
      "?landPlotId=plot+1&includeNonTerminal=true&page=2&pageSize=10",
    );
    expect((fetchMock.mock.calls[0][1] as RequestInit).method).toBeUndefined();
  });

  it("writes every crop-cycle transition through CSRF-protected encoded routes", async () => {
    const createRequest = {
      code: "SB-01",
      name: "Cabai Musim Kemarau",
      commodityId: "commodity 1",
      cultivationSopId: "sop 1",
      landId: "land 1",
      landPlotId: "plot 1",
      plantedArea: 0.25,
      areaUnit: 2 as const,
      plannedStartDate: "2026-08-10",
      expectedHarvestDate: "2026-11-10",
      notes: null,
    };
    for (let index = 0; index < 6; index += 1) {
      fetchMock
        .mockResolvedValueOnce(jsonResponse({ requestToken: `csrf-${index}`, headerName: "X-CSRF" }))
        .mockResolvedValueOnce(jsonResponse({ id: "cycle-1", ...createRequest }, index === 0 ? 201 : 200));
    }

    await createCropCycle("org 1", createRequest);
    await updateCropCyclePlan("org 1", "cycle 1", {
      name: createRequest.name,
      cultivationSopId: createRequest.cultivationSopId,
      plantedArea: createRequest.plantedArea,
      areaUnit: createRequest.areaUnit,
      plannedStartDate: createRequest.plannedStartDate,
      expectedHarvestDate: createRequest.expectedHarvestDate,
      notes: createRequest.notes,
    });
    await startCropCycle("org 1", "cycle 1", { actualStartDate: "2026-08-11" });
    await completeCropCycle("org 1", "cycle 1", { actualHarvestDate: "2026-11-08" });
    await cancelCropCycle("org 1", "cycle 1", { cancellationReason: "Cuaca ekstrem" });
    await updateCropCycleNotes("org 1", "cycle 1", { notes: "Pengamatan awal" });

    const cyclePath = "/api/v1/organizations/org%201/crop-cycles/cycle%201";
    expect([1, 3, 5, 7, 9, 11].map((index) => fetchMock.mock.calls[index][0])).toEqual([
      "/api/v1/organizations/org%201/crop-cycles",
      cyclePath,
      `${cyclePath}/start`,
      `${cyclePath}/complete`,
      `${cyclePath}/cancel`,
      `${cyclePath}/notes`,
    ]);
    expect([1, 3, 5, 7, 9, 11].map((index) => (fetchMock.mock.calls[index][1] as RequestInit).method))
      .toEqual(["POST", "PUT", "PATCH", "PATCH", "PATCH", "PATCH"]);
    expect(new Headers((fetchMock.mock.calls[11][1] as RequestInit).headers).get("X-CSRF"))
      .toBe("csrf-5");
  });

  it("reads selected-cycle dashboard sources with encoded identifiers", async () => {
    fetchMock
      .mockResolvedValueOnce(jsonResponse([]))
      .mockResolvedValueOnce(jsonResponse([]))
      .mockResolvedValueOnce(jsonResponse({ cropCycleId: "cycle 1" }));

    await getCultivationActivities("org 1", "cycle 1");
    await getHarvestBatches("org 1", "cycle 1");
    await getCropCycleProfitability("org 1", "cycle 1");

    const basePath = "/api/v1/organizations/org%201/crop-cycles/cycle%201";
    expect(fetchMock.mock.calls[0][0]).toBe(`${basePath}/activities`);
    expect(fetchMock.mock.calls[1][0]).toBe(`${basePath}/harvest-batches`);
    expect(fetchMock.mock.calls[2][0]).toBe(`${basePath}/profitability`);
  });

  it("reads filters and writes every harvest mutation through CSRF-protected routes", async () => {
    const request = {
      code: "PNN-001",
      harvestDate: "2027-05-20",
      grossQuantity: 1250,
      rejectedQuantity: 50,
      quantityUnit: 1 as const,
      qualityGrade: "Grade A",
      storageLocation: "Gudang Timur",
      notes: null,
    };

    fetchMock.mockResolvedValueOnce(jsonResponse([]));
    await getHarvestBatches("org 1", "cycle 1", {
      status: 2,
      harvestDateFrom: "2027-05-01",
      harvestDateTo: "2027-05-31",
      quantityUnit: 1,
      qualityGrade: "Grade A",
    });

    for (let index = 0; index < 4; index += 1) {
      fetchMock
        .mockResolvedValueOnce(jsonResponse({ requestToken: `harvest-csrf-${index}`, headerName: "X-CSRF" }))
        .mockResolvedValueOnce(jsonResponse({ id: "harvest-1", ...request }, index === 0 ? 201 : 200));
    }

    await createHarvestBatch("org 1", "cycle 1", request);
    await updateHarvestBatch("org 1", "cycle 1", "harvest 1", {
      harvestDate: request.harvestDate,
      grossQuantity: request.grossQuantity,
      rejectedQuantity: request.rejectedQuantity,
      quantityUnit: request.quantityUnit,
      qualityGrade: request.qualityGrade,
      storageLocation: request.storageLocation,
      notes: request.notes,
    });
    await confirmHarvestBatch("org 1", "cycle 1", "harvest 1");
    await cancelHarvestBatch("org 1", "cycle 1", "harvest 1", {
      cancellationReason: "Data timbangan tidak valid",
    });

    const collectionPath = "/api/v1/organizations/org%201/crop-cycles/cycle%201/harvest-batches";
    expect(fetchMock.mock.calls[0][0]).toBe(
      `${collectionPath}?status=2&harvestDateFrom=2027-05-01&harvestDateTo=2027-05-31&quantityUnit=1&qualityGrade=Grade+A`,
    );
    expect([2, 4, 6, 8].map((index) => fetchMock.mock.calls[index][0])).toEqual([
      collectionPath,
      `${collectionPath}/harvest%201`,
      `${collectionPath}/harvest%201/confirm`,
      `${collectionPath}/harvest%201/cancel`,
    ]);
    expect([2, 4, 6, 8].map((index) => (fetchMock.mock.calls[index][1] as RequestInit).method))
      .toEqual(["POST", "PUT", "PATCH", "PATCH"]);
    expect(new Headers((fetchMock.mock.calls[8][1] as RequestInit).headers).get("X-CSRF"))
      .toBe("harvest-csrf-3");
  });

  it("reads filters and writes the complete sale lifecycle through CSRF-protected routes", async () => {
    const createRequest = {
      code: "PJL-001",
      saleDate: "2027-05-22",
      buyerName: "Koperasi Tani",
      buyerPhone: "08123456789",
      buyerAddress: "Pasar Induk",
      paymentTerm: 2 as const,
      dueDate: "2027-06-05",
      notes: null,
    };
    const lineRequest = {
      harvestBatchId: "harvest 1",
      quantity: 600,
      quantityUnit: 1 as const,
      unitPrice: 10000,
      lineDiscount: 0,
      notes: null,
    };

    fetchMock.mockResolvedValueOnce(jsonResponse([]));
    await getSales("org 1", {
      status: 2,
      saleDateFrom: "2027-05-01",
      saleDateTo: "2027-05-31",
      paymentTerm: 2,
      buyerName: "Koperasi Tani",
    });

    for (let index = 0; index < 7; index += 1) {
      fetchMock
        .mockResolvedValueOnce(jsonResponse({ requestToken: `sale-csrf-${index}`, headerName: "X-CSRF" }))
        .mockResolvedValueOnce(jsonResponse({ id: "sale-1", ...createRequest }, index === 0 ? 201 : 200));
    }

    await createSale("org 1", createRequest);
    await updateSale("org 1", "sale 1", {
      saleDate: createRequest.saleDate,
      buyerName: createRequest.buyerName,
      buyerPhone: createRequest.buyerPhone,
      buyerAddress: createRequest.buyerAddress,
      paymentTerm: createRequest.paymentTerm,
      dueDate: createRequest.dueDate,
      discountAmount: 100000,
      notes: createRequest.notes,
    });
    await addSaleLine("org 1", "sale 1", lineRequest);
    await updateSaleLine("org 1", "sale 1", "line 1", {
      quantity: 500,
      unitPrice: lineRequest.unitPrice,
      lineDiscount: lineRequest.lineDiscount,
      notes: null,
    });
    await removeSaleLine("org 1", "sale 1", "line 1");
    await confirmSale("org 1", "sale 1");
    await cancelSale("org 1", "sale 1", { cancellationReason: "Pesanan dibatalkan" });

    const salePath = "/api/v1/organizations/org%201/sales/sale%201";
    expect(fetchMock.mock.calls[0][0]).toBe(
      "/api/v1/organizations/org%201/sales?status=2&saleDateFrom=2027-05-01&saleDateTo=2027-05-31&paymentTerm=2&buyerName=Koperasi+Tani",
    );
    expect([2, 4, 6, 8, 10, 12, 14].map((index) => fetchMock.mock.calls[index][0]))
      .toEqual([
        "/api/v1/organizations/org%201/sales",
        salePath,
        `${salePath}/lines`,
        `${salePath}/lines/line%201`,
        `${salePath}/lines/line%201`,
        `${salePath}/confirm`,
        `${salePath}/cancel`,
      ]);
    expect([2, 4, 6, 8, 10, 12, 14]
      .map((index) => (fetchMock.mock.calls[index][1] as RequestInit).method))
      .toEqual(["POST", "PUT", "POST", "PUT", "DELETE", "PATCH", "PATCH"]);
    expect(new Headers((fetchMock.mock.calls[14][1] as RequestInit).headers).get("X-CSRF"))
      .toBe("sale-csrf-6");
  });

  it("reads receivables and writes the complete payment lifecycle through CSRF-protected routes", async () => {
    const request = {
      code: "BYR-001",
      paymentDate: "2027-05-24",
      amount: 2_500_000,
      paymentMethod: 2 as const,
      referenceNumber: "TRX-20270524",
      receivedFrom: "Koperasi Tani",
      notes: null,
    };

    fetchMock
      .mockResolvedValueOnce(jsonResponse([]))
      .mockResolvedValueOnce(jsonResponse({
        saleId: "sale 1",
        saleTotalAmount: 6_000_000,
        confirmedPaidAmount: 2_500_000,
        outstandingReceivable: 3_500_000,
      }));

    await getSalePayments("org 1", "sale 1", {
      status: 2,
      paymentMethod: 2,
      paymentDateFrom: "2027-05-01",
      paymentDateTo: "2027-05-31",
      receivedFrom: "Koperasi Tani",
    });
    await getSaleReceivable("org 1", "sale 1");

    for (let index = 0; index < 4; index += 1) {
      fetchMock
        .mockResolvedValueOnce(jsonResponse({
          requestToken: `payment-csrf-${index}`,
          headerName: "X-CSRF",
        }))
        .mockResolvedValueOnce(jsonResponse({
          id: "payment-1",
          saleId: "sale 1",
          ...request,
        }, index === 0 ? 201 : 200));
    }

    await createSalePayment("org 1", "sale 1", request);
    await updateSalePayment("org 1", "sale 1", "payment 1", {
      paymentDate: request.paymentDate,
      amount: request.amount,
      paymentMethod: 1,
      referenceNumber: null,
      receivedFrom: request.receivedFrom,
      notes: "Diterima tunai",
    });
    await confirmSalePayment("org 1", "sale 1", "payment 1");
    await cancelSalePayment("org 1", "sale 1", "payment 1", {
      cancellationReason: "Transfer dikembalikan",
    });

    const paymentPath = "/api/v1/organizations/org%201/sales/sale%201/payments";
    expect(fetchMock.mock.calls[0][0]).toBe(
      `${paymentPath}?status=2&paymentMethod=2&paymentDateFrom=2027-05-01&paymentDateTo=2027-05-31&receivedFrom=Koperasi+Tani`,
    );
    expect(fetchMock.mock.calls[1][0]).toBe(`${paymentPath}/receivable`);
    expect([3, 5, 7, 9].map((index) => fetchMock.mock.calls[index][0])).toEqual([
      paymentPath,
      `${paymentPath}/payment%201`,
      `${paymentPath}/payment%201/confirm`,
      `${paymentPath}/payment%201/cancel`,
    ]);
    expect([3, 5, 7, 9]
      .map((index) => (fetchMock.mock.calls[index][1] as RequestInit).method))
      .toEqual(["POST", "PUT", "PATCH", "PATCH"]);
    expect(new Headers((fetchMock.mock.calls[9][1] as RequestInit).headers).get("X-CSRF"))
      .toBe("payment-csrf-3");
  });

  it("reads filters and writes the complete cultivation expense lifecycle through CSRF-protected routes", async () => {
    const request = {
      code: "BIA-001",
      expenseDate: "2027-02-15",
      category: 5 as const,
      description: "Upah pengolahan lahan",
      amount: 1_250_000,
      payeeName: "Kelompok Tani Maju",
      referenceNumber: "KWT-2027-01",
      evidenceUrl: "https://example.test/kwt-2027-01",
      notes: null,
    };

    fetchMock.mockResolvedValueOnce(jsonResponse([]));
    await getCultivationExpenses("org 1", "cycle 1", {
      status: 2,
      category: 5,
      expenseDateFrom: "2027-02-01",
      expenseDateTo: "2027-02-28",
      payeeName: "Kelompok Tani Maju",
    });

    for (let index = 0; index < 4; index += 1) {
      fetchMock
        .mockResolvedValueOnce(jsonResponse({
          requestToken: `expense-csrf-${index}`,
          headerName: "X-CSRF",
        }))
        .mockResolvedValueOnce(jsonResponse({
          id: "expense-1",
          cropCycleId: "cycle 1",
          ...request,
        }, index === 0 ? 201 : 200));
    }

    await createCultivationExpense("org 1", "cycle 1", request);
    await updateCultivationExpense("org 1", "cycle 1", "expense 1", {
      expenseDate: request.expenseDate,
      category: 3,
      description: "Pupuk dasar",
      amount: 850_000,
      payeeName: "Kios Tani",
      referenceNumber: null,
      evidenceUrl: null,
      notes: "Transfer",
    });
    await confirmCultivationExpense("org 1", "cycle 1", "expense 1");
    await cancelCultivationExpense("org 1", "cycle 1", "expense 1", {
      cancellationReason: "Kuitansi duplikat",
    });

    const expensePath = "/api/v1/organizations/org%201/crop-cycles/cycle%201/expenses";
    expect(fetchMock.mock.calls[0][0]).toBe(
      `${expensePath}?status=2&category=5&expenseDateFrom=2027-02-01&expenseDateTo=2027-02-28&payeeName=Kelompok+Tani+Maju`,
    );
    expect([2, 4, 6, 8].map((index) => fetchMock.mock.calls[index][0])).toEqual([
      expensePath,
      `${expensePath}/expense%201`,
      `${expensePath}/expense%201/confirm`,
      `${expensePath}/expense%201/cancel`,
    ]);
    expect([2, 4, 6, 8]
      .map((index) => (fetchMock.mock.calls[index][1] as RequestInit).method))
      .toEqual(["POST", "PUT", "PATCH", "PATCH"]);
    expect(new Headers((fetchMock.mock.calls[8][1] as RequestInit).headers).get("X-CSRF"))
      .toBe("expense-csrf-3");
  });

  it("writes every activity and resource mutation through CSRF-protected nested routes", async () => {
    const createRequest = {
      code: "ACT-01",
      name: "Pemupukan dasar",
      activityType: 5 as const,
      plannedDate: "2026-08-17",
      cultivationSopId: "sop 1",
      cultivationSopStepId: "step 1",
      notes: null,
    };
    const resourceRequest = {
      resourceType: 1 as const,
      description: "Pupuk NPK",
      quantity: 50,
      unit: "kg",
      unitCost: 8000,
      notes: null,
    };
    for (let index = 0; index < 9; index += 1) {
      fetchMock
        .mockResolvedValueOnce(jsonResponse({ requestToken: `activity-csrf-${index}`, headerName: "X-CSRF" }))
        .mockResolvedValueOnce(jsonResponse({ id: "activity-1", ...createRequest }, index === 0 ? 201 : 200));
    }

    await createCultivationActivity("org 1", "cycle 1", createRequest);
    await updateCultivationActivityPlan("org 1", "cycle 1", "activity 1", {
      name: createRequest.name,
      activityType: createRequest.activityType,
      plannedDate: createRequest.plannedDate,
      notes: null,
    });
    await startCultivationActivity("org 1", "cycle 1", "activity 1", {
      actualStartDate: "2026-08-17",
    });
    await completeCultivationActivity("org 1", "cycle 1", "activity 1", {
      actualCompletionDate: "2026-08-18",
      outcome: "Selesai",
      issueNotes: null,
      sopComplianceStatus: 3,
      deviationReason: null,
    });
    await cancelCultivationActivity("org 1", "cycle 1", "activity 1", {
      cancellationReason: "Tidak lagi diperlukan",
    });
    await updateCultivationActivityNotes("org 1", "cycle 1", "activity 1", {
      notes: "Catatan",
      issueNotes: "Kendala",
    });
    await addCultivationActivityResource("org 1", "cycle 1", "activity 1", resourceRequest);
    await updateCultivationActivityResource(
      "org 1",
      "cycle 1",
      "activity 1",
      "resource 1",
      {
        description: resourceRequest.description,
        quantity: resourceRequest.quantity,
        unit: resourceRequest.unit,
        unitCost: resourceRequest.unitCost,
        notes: "Aktual",
      },
    );
    await removeCultivationActivityResource(
      "org 1",
      "cycle 1",
      "activity 1",
      "resource 1",
    );

    const basePath = "/api/v1/organizations/org%201/crop-cycles/cycle%201/activities";
    const activityPath = `${basePath}/activity%201`;
    expect([1, 3, 5, 7, 9, 11, 13, 15, 17].map((index) => fetchMock.mock.calls[index][0]))
      .toEqual([
        basePath,
        activityPath,
        `${activityPath}/start`,
        `${activityPath}/complete`,
        `${activityPath}/cancel`,
        `${activityPath}/notes`,
        `${activityPath}/resources`,
        `${activityPath}/resources/resource%201`,
        `${activityPath}/resources/resource%201`,
      ]);
    expect([1, 3, 5, 7, 9, 11, 13, 15, 17]
      .map((index) => (fetchMock.mock.calls[index][1] as RequestInit).method))
      .toEqual(["POST", "PUT", "PATCH", "PATCH", "PATCH", "PATCH", "POST", "PUT", "DELETE"]);
    expect(new Headers((fetchMock.mock.calls[17][1] as RequestInit).headers).get("X-CSRF"))
      .toBe("activity-csrf-8");
  });

  it("keeps dashboard reads cookie-based and uncached", async () => {
    fetchMock.mockResolvedValueOnce(jsonResponse([]));

    await getLands("org-1");

    expect(fetchMock.mock.calls[0][1]).toEqual(expect.objectContaining({
      credentials: "include",
      cache: "no-store",
    }));
  });

  it("writes land records through CSRF-protected encoded routes", async () => {
    const landRequest = {
      code: "LHN-01",
      name: "Lahan Timur",
      tenureType: 1 as const,
      totalArea: 1,
      areaUnit: 2 as const,
      address: null,
      locationDescription: null,
      latitude: null,
      longitude: null,
      notes: null,
    };
    fetchMock
      .mockResolvedValueOnce(jsonResponse({ requestToken: "csrf-1", headerName: "X-CSRF" }))
      .mockResolvedValueOnce(jsonResponse({ id: "land-1" }, 201))
      .mockResolvedValueOnce(jsonResponse({ requestToken: "csrf-2", headerName: "X-CSRF" }))
      .mockResolvedValueOnce(jsonResponse({ id: "land-1" }))
      .mockResolvedValueOnce(jsonResponse({ requestToken: "csrf-3", headerName: "X-CSRF" }))
      .mockResolvedValueOnce(jsonResponse({ id: "land-1", isActive: false }));

    await createLand("org 1", landRequest);
    await updateLand("org 1", "land 1", {
      name: landRequest.name,
      tenureType: landRequest.tenureType,
      totalArea: landRequest.totalArea,
      areaUnit: landRequest.areaUnit,
      address: landRequest.address,
      locationDescription: landRequest.locationDescription,
      latitude: landRequest.latitude,
      longitude: landRequest.longitude,
      notes: landRequest.notes,
    });
    await setLandActive("org 1", "land 1", false);

    expect(fetchMock.mock.calls[1][0]).toBe("/api/v1/organizations/org%201/lands");
    expect((fetchMock.mock.calls[1][1] as RequestInit).method).toBe("POST");
    expect(fetchMock.mock.calls[3][0]).toBe("/api/v1/organizations/org%201/lands/land%201");
    expect((fetchMock.mock.calls[3][1] as RequestInit).method).toBe("PUT");
    expect(fetchMock.mock.calls[5][0]).toBe("/api/v1/organizations/org%201/lands/land%201/deactivate");
    expect((fetchMock.mock.calls[5][1] as RequestInit).method).toBe("PATCH");
    expect(new Headers((fetchMock.mock.calls[1][1] as RequestInit).headers).get("X-CSRF")).toBe("csrf-1");
  });

  it("writes plot records through CSRF-protected nested routes", async () => {
    const plotRequest = {
      code: "PTK-01",
      name: "Petak Utara",
      area: 2500,
      areaUnit: 1 as const,
      generalCondition: null,
      notes: null,
    };
    for (let index = 0; index < 4; index += 1) {
      fetchMock
        .mockResolvedValueOnce(jsonResponse({ requestToken: `csrf-${index}`, headerName: "X-CSRF" }))
        .mockResolvedValueOnce(jsonResponse({ id: "land-1", plots: [] }));
    }

    await addLandPlot("org 1", "land 1", plotRequest);
    await updateLandPlot("org 1", "land 1", "plot 1", {
      name: plotRequest.name,
      area: plotRequest.area,
      areaUnit: plotRequest.areaUnit,
      generalCondition: plotRequest.generalCondition,
      notes: plotRequest.notes,
    });
    await setLandPlotActive("org 1", "land 1", "plot 1", false);
    await removeLandPlot("org 1", "land 1", "plot 1");

    const resource = "/api/v1/organizations/org%201/lands/land%201/plots/plot%201";
    expect(fetchMock.mock.calls[1][0]).toBe("/api/v1/organizations/org%201/lands/land%201/plots");
    expect(fetchMock.mock.calls[3][0]).toBe(resource);
    expect(fetchMock.mock.calls[5][0]).toBe(`${resource}/deactivate`);
    expect(fetchMock.mock.calls[7][0]).toBe(resource);
    expect([1, 3, 5, 7].map((index) => (fetchMock.mock.calls[index][1] as RequestInit).method))
      .toEqual(["POST", "PUT", "PATCH", "DELETE"]);
  });

  it("deletes an unused land through a CSRF-protected encoded route", async () => {
    fetchMock
      .mockResolvedValueOnce(jsonResponse({ requestToken: "csrf-delete", headerName: "X-CSRF" }))
      .mockResolvedValueOnce(new Response(null, { status: 204 }));

    await expect(deleteLand("org 1", "land 1")).resolves.toBeUndefined();

    expect(fetchMock.mock.calls[1][0]).toBe("/api/v1/organizations/org%201/lands/land%201");
    expect((fetchMock.mock.calls[1][1] as RequestInit).method).toBe("DELETE");
    expect(new Headers((fetchMock.mock.calls[1][1] as RequestInit).headers).get("X-CSRF"))
      .toBe("csrf-delete");
  });

  it("obtains a CSRF token before login", async () => {
    fetchMock
      .mockResolvedValueOnce(jsonResponse({ requestToken: "csrf-value", headerName: "X-CSRF-TOKEN" }))
      .mockResolvedValueOnce(jsonResponse({ userId: "user-1", email: "owner@example.com", memberships: [] }));

    await login({ email: "owner@example.com", password: "secret", rememberMe: true });

    expect(fetchMock).toHaveBeenCalledTimes(2);
    const loginInit = fetchMock.mock.calls[1][1] as RequestInit;
    const headers = new Headers(loginInit.headers);
    expect(headers.get("X-CSRF-TOKEN")).toBe("csrf-value");
    expect(loginInit.body).toBe(JSON.stringify({ email: "owner@example.com", password: "secret", rememberMe: true }));
  });

  it("sends bootstrap authorization and CSRF headers together", async () => {
    fetchMock
      .mockResolvedValueOnce(jsonResponse({ requestToken: "csrf-value", headerName: "X-CSRF-TOKEN" }))
      .mockResolvedValueOnce(jsonResponse({ userId: "user-1", organizationId: "org-1" }, 201));

    await bootstrapOwner("bootstrap-secret", {
      organizationCode: "FARM",
      organizationName: "Farm",
      organizationLegalName: null,
      organizationTimeZone: "Asia/Jakarta",
      email: "owner@example.com",
      password: "secret",
    });

    const requestInit = fetchMock.mock.calls[1][1] as RequestInit;
    const headers = new Headers(requestInit.headers);
    expect(headers.get("X-CSRF-TOKEN")).toBe("csrf-value");
    expect(Array.from(headers.values())).toContain("bootstrap-secret");
  });

  it("supports a no-content logout response", async () => {
    fetchMock
      .mockResolvedValueOnce(jsonResponse({ requestToken: "csrf-value", headerName: "X-CSRF-TOKEN" }))
      .mockResolvedValueOnce(new Response(null, { status: 204 }));

    await expect(logout()).resolves.toBeUndefined();
  });

  it("exposes RFC problem details through ApiError", async () => {
    fetchMock.mockResolvedValueOnce(jsonResponse({ title: "Authentication failed", detail: "Email or password is invalid.", code: "Authentication.InvalidCredentials" }, 401));

    const error = await getCurrentUser().catch((caught) => caught);
    expect(error).toBeInstanceOf(ApiError);
    expect(error).toMatchObject({ status: 401, message: "Email or password is invalid." });
    expect((error as ApiError).problem?.code).toBe("Authentication.InvalidCredentials");
  });

  it("creates a safe fallback for an empty authorization response", async () => {
    fetchMock.mockResolvedValueOnce(new Response(null, { status: 403 }));

    await expect(getCurrentUser()).rejects.toMatchObject({ status: 403, message: "Permintaan gagal dengan status 403." });
  });

  it("reads and mutates capital contributions through encoded CSRF routes", async () => {
    fetchMock.mockResolvedValueOnce(jsonResponse([]));
    await getCapitalContributions("org 1", "cycle 1", {
      status: 2,
      contributorRole: 1,
      contributorCode: " INV-01 ",
    });

    const request = {
      code: "MOD-001",
      contributionDate: "2026-08-11",
      contributorCode: "INV-01",
      contributorName: "Investor Utama",
      contributorRole: 1 as const,
      amount: 9_000_000,
      paymentMethod: 2 as const,
      referenceNumber: null,
      notes: null,
    };

    for (let index = 0; index < 4; index += 1) {
      fetchMock
        .mockResolvedValueOnce(jsonResponse({ requestToken: `capital-${index}`, headerName: "X-CSRF" }))
        .mockResolvedValueOnce(jsonResponse({ id: "capital 1" }, index === 0 ? 201 : 200));
    }

    await createCapitalContribution("org 1", "cycle 1", request);
    await updateCapitalContribution("org 1", "cycle 1", "capital 1", {
      contributionDate: request.contributionDate,
      contributorCode: request.contributorCode,
      contributorName: request.contributorName,
      contributorRole: request.contributorRole,
      amount: request.amount,
      paymentMethod: request.paymentMethod,
      referenceNumber: request.referenceNumber,
      notes: request.notes,
    });
    await confirmCapitalContribution("org 1", "cycle 1", "capital 1");
    await cancelCapitalContribution("org 1", "cycle 1", "capital 1", {
      cancellationReason: "Setoran dikoreksi",
    });

    const base = "/api/v1/organizations/org%201/crop-cycles/cycle%201/capital-contributions";
    expect(fetchMock.mock.calls[0][0]).toBe(`${base}?status=2&contributorRole=1&contributorCode=INV-01`);
    expect([2, 4, 6, 8].map((index) => fetchMock.mock.calls[index][0])).toEqual([
      base,
      `${base}/capital%201`,
      `${base}/capital%201/confirm`,
      `${base}/capital%201/cancel`,
    ]);
    expect([2, 4, 6, 8].map((index) => (fetchMock.mock.calls[index][1] as RequestInit).method))
      .toEqual(["POST", "PUT", "PATCH", "PATCH"]);
  });

  it("reads and mutates profit-sharing settlements through encoded CSRF routes", async () => {
    fetchMock.mockResolvedValueOnce(jsonResponse([]));
    await getProfitSharingSettlements("org 1", "cycle 1", {
      status: 1,
      managingPartnerCode: " MIT-01 ",
    });

    const request = {
      code: "BH-001",
      settlementDate: "2026-12-20",
      managingPartnerCode: "MIT-01",
      managingPartnerName: "Mitra Tani",
      notes: null,
    };
    for (let index = 0; index < 4; index += 1) {
      fetchMock
        .mockResolvedValueOnce(jsonResponse({ requestToken: `sharing-${index}`, headerName: "X-CSRF" }))
        .mockResolvedValueOnce(jsonResponse({ id: "sharing 1" }, index === 0 ? 201 : 200));
    }

    await createProfitSharingSettlement("org 1", "cycle 1", request);
    await updateProfitSharingSettlement("org 1", "cycle 1", "sharing 1", {
      settlementDate: request.settlementDate,
      notes: "Diperiksa",
    });
    await finalizeProfitSharingSettlement("org 1", "cycle 1", "sharing 1");
    await voidProfitSharingSettlement("org 1", "cycle 1", "sharing 1", {
      voidReason: "Perlu koreksi sumber",
    });

    const base = "/api/v1/organizations/org%201/crop-cycles/cycle%201/profit-sharing-settlements";
    expect(fetchMock.mock.calls[0][0]).toBe(`${base}?status=1&managingPartnerCode=MIT-01`);
    expect([2, 4, 6, 8].map((index) => fetchMock.mock.calls[index][0])).toEqual([
      base,
      `${base}/sharing%201`,
      `${base}/sharing%201/finalize`,
      `${base}/sharing%201/void`,
    ]);
    expect([2, 4, 6, 8].map((index) => (fetchMock.mock.calls[index][1] as RequestInit).method))
      .toEqual(["POST", "PUT", "PATCH", "PATCH"]);
  });

  it("reads the versioned profit-sharing scheme catalog with encoded filters", async () => {
    fetchMock
      .mockResolvedValueOnce(jsonResponse([]))
      .mockResolvedValueOnce(jsonResponse({ id: "scheme 1" }));

    await getProfitSharingSchemes("org 1", {
      status: 2,
      code: " MITRA UTAMA ",
    });
    await getProfitSharingScheme("org 1", "scheme 1");

    expect(fetchMock.mock.calls[0][0]).toBe(
      "/api/v1/organizations/org%201/profit-sharing-schemes?status=2&code=MITRA+UTAMA",
    );
    expect(fetchMock.mock.calls[1][0]).toBe(
      "/api/v1/organizations/org%201/profit-sharing-schemes/scheme%201",
    );
  });

  it("writes the complete profit-sharing scheme version lifecycle through CSRF", async () => {
    const createRequest = {
      code: "MITRA-1",
      name: "Skema Mitra",
      description: null,
      participants: [
        {
          participantCode: "PERUSAHAAN",
          participantName: "Perusahaan",
          participantRole: 1 as const,
          participatesInResidualProfit: true,
          sequence: 1,
        },
      ],
      priorityRules: [],
      residualMethod: 1 as const,
      residualRecipientCode: "PERUSAHAAN",
      residualShares: [],
    };

    for (let index = 0; index < 4; index += 1) {
      fetchMock
        .mockResolvedValueOnce(jsonResponse({
          requestToken: `scheme-${index}`,
          headerName: "X-CSRF",
        }))
        .mockResolvedValueOnce(jsonResponse({ id: "scheme 1" }, index < 2 ? 201 : 200));
    }

    await createProfitSharingScheme("org 1", createRequest);
    await updateProfitSharingScheme("org 1", "scheme 1", {
      name: createRequest.name,
      description: createRequest.description,
      participants: createRequest.participants,
      priorityRules: createRequest.priorityRules,
      residualMethod: createRequest.residualMethod,
      residualRecipientCode: createRequest.residualRecipientCode,
      residualShares: createRequest.residualShares,
    });
    await createNextProfitSharingSchemeVersion("org 1", "scheme 1");
    await activateProfitSharingScheme("org 1", "scheme 1");

    const base = "/api/v1/organizations/org%201/profit-sharing-schemes";
    expect([1, 3, 5, 7].map((index) => fetchMock.mock.calls[index][0])).toEqual([
      base,
      `${base}/scheme%201`,
      `${base}/scheme%201/versions`,
      `${base}/scheme%201/activate`,
    ]);
    expect([1, 3, 5, 7].map((index) =>
      (fetchMock.mock.calls[index][1] as RequestInit).method))
      .toEqual(["POST", "PUT", "POST", "PATCH"]);
  });

  it("reads and assigns a scheme snapshot before loading the waterfall preview", async () => {
    fetchMock
      .mockResolvedValueOnce(jsonResponse({ id: "assignment-1" }))
      .mockResolvedValueOnce(jsonResponse({ requestToken: "assignment-csrf", headerName: "X-CSRF" }))
      .mockResolvedValueOnce(jsonResponse({ id: "assignment-1" }))
      .mockResolvedValueOnce(jsonResponse({ calculationVersion: "SIPACUL-PS-2" }));

    await getProfitSharingSchemeAssignment("org 1", "cycle 1");
    await assignProfitSharingScheme("org 1", "cycle 1", { schemeId: "scheme 1" });
    await getProfitSharingPreview("org 1", "cycle 1");

    const base = "/api/v1/organizations/org%201/crop-cycles/cycle%201";
    expect(fetchMock.mock.calls[0][0]).toBe(`${base}/profit-sharing-scheme`);
    expect(fetchMock.mock.calls[2][0]).toBe(`${base}/profit-sharing-scheme`);
    expect((fetchMock.mock.calls[2][1] as RequestInit).method).toBe("PUT");
    expect(fetchMock.mock.calls[3][0]).toBe(`${base}/profit-sharing-preview`);
  });

  it("reads, finalizes, and voids waterfall settlements through V2 routes", async () => {
    fetchMock
      .mockResolvedValueOnce(jsonResponse([]))
      .mockResolvedValueOnce(jsonResponse({ id: "settlement 1" }))
      .mockResolvedValueOnce(jsonResponse({ requestToken: "finalize-csrf", headerName: "X-CSRF" }))
      .mockResolvedValueOnce(jsonResponse({ id: "settlement 1" }, 201))
      .mockResolvedValueOnce(jsonResponse({ requestToken: "void-csrf", headerName: "X-CSRF" }))
      .mockResolvedValueOnce(jsonResponse({ id: "settlement 1", status: 2 }));

    await getProfitSharingWaterfallSettlements("org 1", "cycle 1", {
      status: 1,
      settlementDateFrom: "2027-07-01",
      settlementDateTo: "2027-07-31",
    });
    await getProfitSharingWaterfallSettlement("org 1", "cycle 1", "settlement 1");
    await finalizeProfitSharingWaterfallSettlement("org 1", "cycle 1", {
      code: "WF-001",
      settlementDate: "2027-07-24",
      notes: null,
    });
    await voidProfitSharingWaterfallSettlement(
      "org 1",
      "cycle 1",
      "settlement 1",
      { voidReason: "Koreksi sumber" },
    );

    const base = "/api/v1/organizations/org%201/crop-cycles/cycle%201/profit-sharing-waterfall-settlements";
    expect(fetchMock.mock.calls[0][0]).toBe(
      `${base}?status=1&settlementDateFrom=2027-07-01&settlementDateTo=2027-07-31`,
    );
    expect(fetchMock.mock.calls[1][0]).toBe(`${base}/settlement%201`);
    expect(fetchMock.mock.calls[3][0]).toBe(base);
    expect((fetchMock.mock.calls[3][1] as RequestInit).method).toBe("POST");
    expect(fetchMock.mock.calls[5][0]).toBe(`${base}/settlement%201/void`);
    expect((fetchMock.mock.calls[5][1] as RequestInit).method).toBe("PATCH");
  });
});
