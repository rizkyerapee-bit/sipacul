import type {
  AntiforgeryTokenResponse,
  AddCultivationActivityResourceRequest,
  AddSaleLineRequest,
  ApiProblem,
  BootstrapOwnerRequest,
  BootstrapOwnerResponse,
  BootstrapStatus,
  AddLandPlotRequest,
  CancelCapitalContributionRequest,
  CancelCropCycleRequest,
  CancelCultivationActivityRequest,
  CancelCultivationExpenseRequest,
  CancelHarvestBatchRequest,
  CancelSalePaymentRequest,
  CancelSaleRequest,
  Commodity,
  CommodityCategory,
  CompleteCropCycleRequest,
  CompleteCultivationActivityRequest,
  CropCycle,
  CropCycleProfitability,
  CapitalContribution,
  CapitalContributionFilter,
  AssignProfitSharingSchemeRequest,
  CreateCapitalContributionRequest,
  CreateCommodityCategoryRequest,
  CreateCommodityRequest,
  CreateProfitSharingSchemeRequest,
  CreateProfitSharingSettlementRequest,
  CreateCultivationExpenseRequest,
  CreateCropCycleRequest,
  CreateCultivationActivityRequest,
  CreateLandRequest,
  CreateHarvestBatchRequest,
  CreateSalePaymentRequest,
  CreateSaleRequest,
  CultivationSop,
  AddCultivationSopStepRequest,
  CreateCultivationSopRequest,
  MoveCultivationSopStepRequest,
  UpdateCultivationSopRequest,
  UpdateCultivationSopStepRequest,
  CultivationActivity,
  CultivationExpense,
  CultivationExpenseFilter,
  CurrentUser,
  HarvestBatch,
  HarvestBatchFilter,
  Land,
  LandSeasonHistory,
  LoginRequest,
  Organization,
  ProfitSharingSettlement,
  ProfitSharingSettlementFilter,
  ProfitSharingPreview,
  ProfitSharingScheme,
  ProfitSharingSchemeAssignment,
  ProfitSharingSchemeFilter,
  ProfitSharingWaterfallSettlement,
  ProfitSharingWaterfallSettlementFilter,
  FinalizeProfitSharingWaterfallSettlementRequest,
  Sale,
  SaleFilter,
  SalePayment,
  SalePaymentFilter,
  SaleReceivable,
  SeasonHistoryFilter,
  StartCropCycleRequest,
  StartCultivationActivityRequest,
  UpdateCommodityCategoryRequest,
  UpdateCommodityRequest,
  UpdateCultivationActivityNotesRequest,
  UpdateCultivationActivityPlanRequest,
  UpdateCultivationActivityResourceRequest,
  UpdateCultivationExpenseRequest,
  UpdateCapitalContributionRequest,
  UpdateCropCyclePlanRequest,
  UpdateCropCycleNotesRequest,
  UpdateLandPlotRequest,
  UpdateLandRequest,
  UpdateHarvestBatchRequest,
  UpdateSalePaymentRequest,
  UpdateSaleLineRequest,
  UpdateSaleRequest,
  UpdateProfitSharingSettlementRequest,
  UpdateProfitSharingSchemeDraftRequest,
  VoidProfitSharingSettlementRequest,
  VoidProfitSharingWaterfallSettlementRequest,
} from "@/lib/api/contracts";

const API_PREFIX = "/api/v1";
const BOOTSTRAP_TOKEN_HEADER = "X-SiPacul-Bootstrap-Token";

export class ApiError extends Error {
  readonly status: number;
  readonly problem: ApiProblem | null;

  constructor(status: number, message: string, problem: ApiProblem | null) {
    super(message);
    this.name = "ApiError";
    this.status = status;
    this.problem = problem;
  }
}

async function readProblem(response: Response): Promise<ApiProblem | null> {
  const contentType = response.headers.get("content-type") ?? "";

  if (!contentType.includes("json")) {
    return null;
  }

  try {
    return (await response.json()) as ApiProblem;
  } catch {
    return null;
  }
}

async function apiRequest<T>(
  path: string,
  init: RequestInit = {},
): Promise<T> {
  const headers = new Headers(init.headers);
  headers.set("Accept", "application/json");

  if (init.body !== undefined && !headers.has("Content-Type")) {
    headers.set("Content-Type", "application/json");
  }

  const response = await fetch(`${API_PREFIX}${path}`, {
    ...init,
    headers,
    credentials: "include",
    cache: "no-store",
  });

  if (!response.ok) {
    const problem = await readProblem(response);
    const message =
      problem?.detail ||
      problem?.title ||
      `Permintaan gagal dengan status ${response.status}.`;

    throw new ApiError(response.status, message, problem);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

async function csrfRequest<T>(
  path: string,
  init: RequestInit,
): Promise<T> {
  const token = await apiRequest<AntiforgeryTokenResponse>("/auth/csrf");
  const headers = new Headers(init.headers);
  headers.set(token.headerName, token.requestToken);

  return apiRequest<T>(path, {
    ...init,
    headers,
  });
}

export function getBootstrapStatus(): Promise<BootstrapStatus> {
  return apiRequest<BootstrapStatus>("/bootstrap/status");
}

export function bootstrapOwner(
  bootstrapToken: string,
  request: BootstrapOwnerRequest,
): Promise<BootstrapOwnerResponse> {
  return csrfRequest<BootstrapOwnerResponse>("/bootstrap/owner", {
    method: "POST",
    headers: {
      [BOOTSTRAP_TOKEN_HEADER]: bootstrapToken,
    },
    body: JSON.stringify(request),
  });
}

export function login(request: LoginRequest): Promise<CurrentUser> {
  return csrfRequest<CurrentUser>("/auth/login", {
    method: "POST",
    body: JSON.stringify(request),
  });
}

export function logout(): Promise<void> {
  return csrfRequest<void>("/auth/logout", {
    method: "POST",
  });
}

export function getCurrentUser(): Promise<CurrentUser> {
  return apiRequest<CurrentUser>("/auth/me");
}

export function getOrganization(
  organizationId: string,
): Promise<Organization> {
  return apiRequest<Organization>(
    `/organizations/${encodeURIComponent(organizationId)}`,
  );
}

function getOrganizationResourcePath(
  organizationId: string,
  resourcePath: string,
): string {
  return `/organizations/${encodeURIComponent(organizationId)}${resourcePath}`;
}

export function getLands(
  organizationId: string,
): Promise<Land[]> {
  return apiRequest<Land[]>(
    getOrganizationResourcePath(organizationId, "/lands"),
  );
}

export function createLand(
  organizationId: string,
  request: CreateLandRequest,
): Promise<Land> {
  return csrfRequest<Land>(
    getOrganizationResourcePath(organizationId, "/lands"),
    { method: "POST", body: JSON.stringify(request) },
  );
}

export function updateLand(
  organizationId: string,
  landId: string,
  request: UpdateLandRequest,
): Promise<Land> {
  return csrfRequest<Land>(
    getOrganizationResourcePath(
      organizationId,
      `/lands/${encodeURIComponent(landId)}`,
    ),
    { method: "PUT", body: JSON.stringify(request) },
  );
}

export function deleteLand(
  organizationId: string,
  landId: string,
): Promise<void> {
  return csrfRequest<void>(
    getOrganizationResourcePath(
      organizationId,
      `/lands/${encodeURIComponent(landId)}`,
    ),
    { method: "DELETE" },
  );
}

export function setLandActive(
  organizationId: string,
  landId: string,
  isActive: boolean,
): Promise<Land> {
  return csrfRequest<Land>(
    getOrganizationResourcePath(
      organizationId,
      `/lands/${encodeURIComponent(landId)}/${isActive ? "activate" : "deactivate"}`,
    ),
    { method: "PATCH" },
  );
}

export function addLandPlot(
  organizationId: string,
  landId: string,
  request: AddLandPlotRequest,
): Promise<Land> {
  return csrfRequest<Land>(
    getOrganizationResourcePath(
      organizationId,
      `/lands/${encodeURIComponent(landId)}/plots`,
    ),
    { method: "POST", body: JSON.stringify(request) },
  );
}

export function updateLandPlot(
  organizationId: string,
  landId: string,
  plotId: string,
  request: UpdateLandPlotRequest,
): Promise<Land> {
  return csrfRequest<Land>(
    getOrganizationResourcePath(
      organizationId,
      `/lands/${encodeURIComponent(landId)}/plots/${encodeURIComponent(plotId)}`,
    ),
    { method: "PUT", body: JSON.stringify(request) },
  );
}

export function removeLandPlot(
  organizationId: string,
  landId: string,
  plotId: string,
): Promise<Land> {
  return csrfRequest<Land>(
    getOrganizationResourcePath(
      organizationId,
      `/lands/${encodeURIComponent(landId)}/plots/${encodeURIComponent(plotId)}`,
    ),
    { method: "DELETE" },
  );
}

export function setLandPlotActive(
  organizationId: string,
  landId: string,
  plotId: string,
  isActive: boolean,
): Promise<Land> {
  return csrfRequest<Land>(
    getOrganizationResourcePath(
      organizationId,
      `/lands/${encodeURIComponent(landId)}/plots/${encodeURIComponent(plotId)}/${isActive ? "activate" : "deactivate"}`,
    ),
    { method: "PATCH" },
  );
}

export function getLandSeasonHistory(
  organizationId: string,
  landId: string,
  filter: SeasonHistoryFilter = {},
): Promise<LandSeasonHistory> {
  const search = new URLSearchParams();
  if (filter.landPlotId) search.set("landPlotId", filter.landPlotId);
  if (filter.includeNonTerminal !== undefined) {
    search.set("includeNonTerminal", String(filter.includeNonTerminal));
  }
  if (filter.page !== undefined) search.set("page", String(filter.page));
  if (filter.pageSize !== undefined) search.set("pageSize", String(filter.pageSize));
  const query = search.size > 0 ? `?${search.toString()}` : "";

  return apiRequest<LandSeasonHistory>(
    getOrganizationResourcePath(
      organizationId,
      `/lands/${encodeURIComponent(landId)}/season-history${query}`,
    ),
  );
}

export function getCropCycles(
  organizationId: string,
): Promise<CropCycle[]> {
  return apiRequest<CropCycle[]>(
    getOrganizationResourcePath(organizationId, "/crop-cycles"),
  );
}

export function getCommodities(
  organizationId: string,
): Promise<Commodity[]> {
  return apiRequest<Commodity[]>(
    getOrganizationResourcePath(organizationId, "/commodities"),
  );
}

export function getCommodityCategories(
  organizationId: string,
): Promise<CommodityCategory[]> {
  return apiRequest<CommodityCategory[]>(
    getOrganizationResourcePath(organizationId, "/commodity-categories"),
  );
}

export function createCommodityCategory(
  organizationId: string,
  request: CreateCommodityCategoryRequest,
): Promise<CommodityCategory> {
  return csrfRequest<CommodityCategory>(
    getOrganizationResourcePath(organizationId, "/commodity-categories"),
    { method: "POST", body: JSON.stringify(request) },
  );
}

export function updateCommodityCategory(
  organizationId: string,
  categoryId: string,
  request: UpdateCommodityCategoryRequest,
): Promise<CommodityCategory> {
  return csrfRequest<CommodityCategory>(
    getOrganizationResourcePath(
      organizationId,
      `/commodity-categories/${encodeURIComponent(categoryId)}`,
    ),
    { method: "PUT", body: JSON.stringify(request) },
  );
}

export function setCommodityCategoryActive(
  organizationId: string,
  categoryId: string,
  isActive: boolean,
): Promise<CommodityCategory> {
  return csrfRequest<CommodityCategory>(
    getOrganizationResourcePath(
      organizationId,
      `/commodity-categories/${encodeURIComponent(categoryId)}/${isActive ? "activate" : "deactivate"}`,
    ),
    { method: "PATCH" },
  );
}

export function createCommodity(
  organizationId: string,
  request: CreateCommodityRequest,
): Promise<Commodity> {
  return csrfRequest<Commodity>(
    getOrganizationResourcePath(organizationId, "/commodities"),
    { method: "POST", body: JSON.stringify(request) },
  );
}

export function updateCommodity(
  organizationId: string,
  commodityId: string,
  request: UpdateCommodityRequest,
): Promise<Commodity> {
  return csrfRequest<Commodity>(
    getOrganizationResourcePath(
      organizationId,
      `/commodities/${encodeURIComponent(commodityId)}`,
    ),
    { method: "PUT", body: JSON.stringify(request) },
  );
}

export function setCommodityActive(
  organizationId: string,
  commodityId: string,
  isActive: boolean,
): Promise<Commodity> {
  return csrfRequest<Commodity>(
    getOrganizationResourcePath(
      organizationId,
      `/commodities/${encodeURIComponent(commodityId)}/${isActive ? "activate" : "deactivate"}`,
    ),
    { method: "PATCH" },
  );
}
export function getCultivationSops(
  organizationId: string,
  commodityId?: string,
): Promise<CultivationSop[]> {
  const query = commodityId
    ? `?commodityId=${encodeURIComponent(commodityId)}`
    : "";

  return apiRequest<CultivationSop[]>(
    getOrganizationResourcePath(
      organizationId,
      `/cultivation-sops${query}`,
    ),
  );
}

export function getCultivationSop(
  organizationId: string,
  cultivationSopId: string,
): Promise<CultivationSop> {
  return apiRequest<CultivationSop>(
    getOrganizationResourcePath(
      organizationId,
      `/cultivation-sops/${encodeURIComponent(cultivationSopId)}`,
    ),
  );
}

export function createCultivationSop(
  organizationId: string,
  request: CreateCultivationSopRequest,
): Promise<CultivationSop> {
  return csrfRequest<CultivationSop>(
    getOrganizationResourcePath(organizationId, "/cultivation-sops"),
    { method: "POST", body: JSON.stringify(request) },
  );
}

export function updateCultivationSop(
  organizationId: string,
  cultivationSopId: string,
  request: UpdateCultivationSopRequest,
): Promise<CultivationSop> {
  return csrfRequest<CultivationSop>(
    getOrganizationResourcePath(
      organizationId,
      `/cultivation-sops/${encodeURIComponent(cultivationSopId)}`,
    ),
    { method: "PUT", body: JSON.stringify(request) },
  );
}

export function setCultivationSopActive(
  organizationId: string,
  cultivationSopId: string,
  isActive: boolean,
): Promise<CultivationSop> {
  return csrfRequest<CultivationSop>(
    getOrganizationResourcePath(
      organizationId,
      `/cultivation-sops/${encodeURIComponent(cultivationSopId)}/${isActive ? "activate" : "deactivate"}`,
    ),
    { method: "PATCH" },
  );
}

export function addCultivationSopStep(
  organizationId: string,
  cultivationSopId: string,
  request: AddCultivationSopStepRequest,
): Promise<CultivationSop> {
  return csrfRequest<CultivationSop>(
    getOrganizationResourcePath(
      organizationId,
      `/cultivation-sops/${encodeURIComponent(cultivationSopId)}/steps`,
    ),
    { method: "POST", body: JSON.stringify(request) },
  );
}

export function updateCultivationSopStep(
  organizationId: string,
  cultivationSopId: string,
  stepId: string,
  request: UpdateCultivationSopStepRequest,
): Promise<CultivationSop> {
  return csrfRequest<CultivationSop>(
    getOrganizationResourcePath(
      organizationId,
      `/cultivation-sops/${encodeURIComponent(cultivationSopId)}/steps/${encodeURIComponent(stepId)}`,
    ),
    { method: "PUT", body: JSON.stringify(request) },
  );
}

export function removeCultivationSopStep(
  organizationId: string,
  cultivationSopId: string,
  stepId: string,
): Promise<CultivationSop> {
  return csrfRequest<CultivationSop>(
    getOrganizationResourcePath(
      organizationId,
      `/cultivation-sops/${encodeURIComponent(cultivationSopId)}/steps/${encodeURIComponent(stepId)}`,
    ),
    { method: "DELETE" },
  );
}

export function moveCultivationSopStep(
  organizationId: string,
  cultivationSopId: string,
  stepId: string,
  request: MoveCultivationSopStepRequest,
): Promise<CultivationSop> {
  return csrfRequest<CultivationSop>(
    getOrganizationResourcePath(
      organizationId,
      `/cultivation-sops/${encodeURIComponent(cultivationSopId)}/steps/${encodeURIComponent(stepId)}/move`,
    ),
    { method: "PATCH", body: JSON.stringify(request) },
  );
}

export function createCropCycle(
  organizationId: string,
  request: CreateCropCycleRequest,
): Promise<CropCycle> {
  return csrfRequest<CropCycle>(
    getOrganizationResourcePath(organizationId, "/crop-cycles"),
    { method: "POST", body: JSON.stringify(request) },
  );
}

export function updateCropCyclePlan(
  organizationId: string,
  cropCycleId: string,
  request: UpdateCropCyclePlanRequest,
): Promise<CropCycle> {
  return csrfRequest<CropCycle>(
    getOrganizationResourcePath(
      organizationId,
      `/crop-cycles/${encodeURIComponent(cropCycleId)}`,
    ),
    { method: "PUT", body: JSON.stringify(request) },
  );
}

export function startCropCycle(
  organizationId: string,
  cropCycleId: string,
  request: StartCropCycleRequest,
): Promise<CropCycle> {
  return csrfRequest<CropCycle>(
    getOrganizationResourcePath(
      organizationId,
      `/crop-cycles/${encodeURIComponent(cropCycleId)}/start`,
    ),
    { method: "PATCH", body: JSON.stringify(request) },
  );
}

export function completeCropCycle(
  organizationId: string,
  cropCycleId: string,
  request: CompleteCropCycleRequest,
): Promise<CropCycle> {
  return csrfRequest<CropCycle>(
    getOrganizationResourcePath(
      organizationId,
      `/crop-cycles/${encodeURIComponent(cropCycleId)}/complete`,
    ),
    { method: "PATCH", body: JSON.stringify(request) },
  );
}

export function cancelCropCycle(
  organizationId: string,
  cropCycleId: string,
  request: CancelCropCycleRequest,
): Promise<CropCycle> {
  return csrfRequest<CropCycle>(
    getOrganizationResourcePath(
      organizationId,
      `/crop-cycles/${encodeURIComponent(cropCycleId)}/cancel`,
    ),
    { method: "PATCH", body: JSON.stringify(request) },
  );
}

export function updateCropCycleNotes(
  organizationId: string,
  cropCycleId: string,
  request: UpdateCropCycleNotesRequest,
): Promise<CropCycle> {
  return csrfRequest<CropCycle>(
    getOrganizationResourcePath(
      organizationId,
      `/crop-cycles/${encodeURIComponent(cropCycleId)}/notes`,
    ),
    { method: "PATCH", body: JSON.stringify(request) },
  );
}

export function getCultivationActivities(
  organizationId: string,
  cropCycleId: string,
): Promise<CultivationActivity[]> {
  return apiRequest<CultivationActivity[]>(
    getOrganizationResourcePath(
      organizationId,
      `/crop-cycles/${encodeURIComponent(cropCycleId)}/activities`,
    ),
  );
}

function getCultivationActivityPath(
  organizationId: string,
  cropCycleId: string,
  suffix = "",
): string {
  return getOrganizationResourcePath(
    organizationId,
    `/crop-cycles/${encodeURIComponent(cropCycleId)}/activities${suffix}`,
  );
}

export function createCultivationActivity(
  organizationId: string,
  cropCycleId: string,
  request: CreateCultivationActivityRequest,
): Promise<CultivationActivity> {
  return csrfRequest<CultivationActivity>(
    getCultivationActivityPath(organizationId, cropCycleId),
    { method: "POST", body: JSON.stringify(request) },
  );
}

export function updateCultivationActivityPlan(
  organizationId: string,
  cropCycleId: string,
  activityId: string,
  request: UpdateCultivationActivityPlanRequest,
): Promise<CultivationActivity> {
  return csrfRequest<CultivationActivity>(
    getCultivationActivityPath(
      organizationId,
      cropCycleId,
      `/${encodeURIComponent(activityId)}`,
    ),
    { method: "PUT", body: JSON.stringify(request) },
  );
}

export function startCultivationActivity(
  organizationId: string,
  cropCycleId: string,
  activityId: string,
  request: StartCultivationActivityRequest,
): Promise<CultivationActivity> {
  return csrfRequest<CultivationActivity>(
    getCultivationActivityPath(
      organizationId,
      cropCycleId,
      `/${encodeURIComponent(activityId)}/start`,
    ),
    { method: "PATCH", body: JSON.stringify(request) },
  );
}

export function completeCultivationActivity(
  organizationId: string,
  cropCycleId: string,
  activityId: string,
  request: CompleteCultivationActivityRequest,
): Promise<CultivationActivity> {
  return csrfRequest<CultivationActivity>(
    getCultivationActivityPath(
      organizationId,
      cropCycleId,
      `/${encodeURIComponent(activityId)}/complete`,
    ),
    { method: "PATCH", body: JSON.stringify(request) },
  );
}

export function cancelCultivationActivity(
  organizationId: string,
  cropCycleId: string,
  activityId: string,
  request: CancelCultivationActivityRequest,
): Promise<CultivationActivity> {
  return csrfRequest<CultivationActivity>(
    getCultivationActivityPath(
      organizationId,
      cropCycleId,
      `/${encodeURIComponent(activityId)}/cancel`,
    ),
    { method: "PATCH", body: JSON.stringify(request) },
  );
}

export function updateCultivationActivityNotes(
  organizationId: string,
  cropCycleId: string,
  activityId: string,
  request: UpdateCultivationActivityNotesRequest,
): Promise<CultivationActivity> {
  return csrfRequest<CultivationActivity>(
    getCultivationActivityPath(
      organizationId,
      cropCycleId,
      `/${encodeURIComponent(activityId)}/notes`,
    ),
    { method: "PATCH", body: JSON.stringify(request) },
  );
}

export function addCultivationActivityResource(
  organizationId: string,
  cropCycleId: string,
  activityId: string,
  request: AddCultivationActivityResourceRequest,
): Promise<CultivationActivity> {
  return csrfRequest<CultivationActivity>(
    getCultivationActivityPath(
      organizationId,
      cropCycleId,
      `/${encodeURIComponent(activityId)}/resources`,
    ),
    { method: "POST", body: JSON.stringify(request) },
  );
}

export function updateCultivationActivityResource(
  organizationId: string,
  cropCycleId: string,
  activityId: string,
  resourceId: string,
  request: UpdateCultivationActivityResourceRequest,
): Promise<CultivationActivity> {
  return csrfRequest<CultivationActivity>(
    getCultivationActivityPath(
      organizationId,
      cropCycleId,
      `/${encodeURIComponent(activityId)}/resources/${encodeURIComponent(resourceId)}`,
    ),
    { method: "PUT", body: JSON.stringify(request) },
  );
}

export function removeCultivationActivityResource(
  organizationId: string,
  cropCycleId: string,
  activityId: string,
  resourceId: string,
): Promise<CultivationActivity> {
  return csrfRequest<CultivationActivity>(
    getCultivationActivityPath(
      organizationId,
      cropCycleId,
      `/${encodeURIComponent(activityId)}/resources/${encodeURIComponent(resourceId)}`,
    ),
    { method: "DELETE" },
  );
}

export function getHarvestBatches(
  organizationId: string,
  cropCycleId: string,
  filter: HarvestBatchFilter = {},
): Promise<HarvestBatch[]> {
  const search = new URLSearchParams();
  if (filter.status !== undefined) search.set("status", String(filter.status));
  if (filter.harvestDateFrom) search.set("harvestDateFrom", filter.harvestDateFrom);
  if (filter.harvestDateTo) search.set("harvestDateTo", filter.harvestDateTo);
  if (filter.quantityUnit !== undefined) search.set("quantityUnit", String(filter.quantityUnit));
  if (filter.qualityGrade?.trim()) search.set("qualityGrade", filter.qualityGrade.trim());
  const query = search.size > 0 ? `?${search.toString()}` : "";

  return apiRequest<HarvestBatch[]>(
    getOrganizationResourcePath(
      organizationId,
      `/crop-cycles/${encodeURIComponent(cropCycleId)}/harvest-batches${query}`,
    ),
  );
}

function getHarvestBatchPath(
  organizationId: string,
  cropCycleId: string,
  resourcePath = "",
): string {
  return getOrganizationResourcePath(
    organizationId,
    `/crop-cycles/${encodeURIComponent(cropCycleId)}/harvest-batches${resourcePath}`,
  );
}

export function createHarvestBatch(
  organizationId: string,
  cropCycleId: string,
  request: CreateHarvestBatchRequest,
): Promise<HarvestBatch> {
  return csrfRequest<HarvestBatch>(
    getHarvestBatchPath(organizationId, cropCycleId),
    { method: "POST", body: JSON.stringify(request) },
  );
}

export function updateHarvestBatch(
  organizationId: string,
  cropCycleId: string,
  harvestBatchId: string,
  request: UpdateHarvestBatchRequest,
): Promise<HarvestBatch> {
  return csrfRequest<HarvestBatch>(
    getHarvestBatchPath(
      organizationId,
      cropCycleId,
      `/${encodeURIComponent(harvestBatchId)}`,
    ),
    { method: "PUT", body: JSON.stringify(request) },
  );
}

export function confirmHarvestBatch(
  organizationId: string,
  cropCycleId: string,
  harvestBatchId: string,
): Promise<HarvestBatch> {
  return csrfRequest<HarvestBatch>(
    getHarvestBatchPath(
      organizationId,
      cropCycleId,
      `/${encodeURIComponent(harvestBatchId)}/confirm`,
    ),
    { method: "PATCH" },
  );
}

export function cancelHarvestBatch(
  organizationId: string,
  cropCycleId: string,
  harvestBatchId: string,
  request: CancelHarvestBatchRequest,
): Promise<HarvestBatch> {
  return csrfRequest<HarvestBatch>(
    getHarvestBatchPath(
      organizationId,
      cropCycleId,
      `/${encodeURIComponent(harvestBatchId)}/cancel`,
    ),
    { method: "PATCH", body: JSON.stringify(request) },
  );
}

function getSalesPath(
  organizationId: string,
  resourcePath = "",
): string {
  return getOrganizationResourcePath(
    organizationId,
    `/sales${resourcePath}`,
  );
}

export function getSales(
  organizationId: string,
  filter: SaleFilter = {},
): Promise<Sale[]> {
  const search = new URLSearchParams();
  if (filter.status !== undefined) search.set("status", String(filter.status));
  if (filter.saleDateFrom) search.set("saleDateFrom", filter.saleDateFrom);
  if (filter.saleDateTo) search.set("saleDateTo", filter.saleDateTo);
  if (filter.paymentTerm !== undefined) search.set("paymentTerm", String(filter.paymentTerm));
  if (filter.buyerName?.trim()) search.set("buyerName", filter.buyerName.trim());
  const query = search.size > 0 ? `?${search.toString()}` : "";

  return apiRequest<Sale[]>(getSalesPath(organizationId, query));
}

export function createSale(
  organizationId: string,
  request: CreateSaleRequest,
): Promise<Sale> {
  return csrfRequest<Sale>(getSalesPath(organizationId), {
    method: "POST",
    body: JSON.stringify(request),
  });
}

export function updateSale(
  organizationId: string,
  saleId: string,
  request: UpdateSaleRequest,
): Promise<Sale> {
  return csrfRequest<Sale>(
    getSalesPath(organizationId, `/${encodeURIComponent(saleId)}`),
    { method: "PUT", body: JSON.stringify(request) },
  );
}

export function addSaleLine(
  organizationId: string,
  saleId: string,
  request: AddSaleLineRequest,
): Promise<Sale> {
  return csrfRequest<Sale>(
    getSalesPath(organizationId, `/${encodeURIComponent(saleId)}/lines`),
    { method: "POST", body: JSON.stringify(request) },
  );
}

export function updateSaleLine(
  organizationId: string,
  saleId: string,
  saleLineId: string,
  request: UpdateSaleLineRequest,
): Promise<Sale> {
  return csrfRequest<Sale>(
    getSalesPath(
      organizationId,
      `/${encodeURIComponent(saleId)}/lines/${encodeURIComponent(saleLineId)}`,
    ),
    { method: "PUT", body: JSON.stringify(request) },
  );
}

export function removeSaleLine(
  organizationId: string,
  saleId: string,
  saleLineId: string,
): Promise<Sale> {
  return csrfRequest<Sale>(
    getSalesPath(
      organizationId,
      `/${encodeURIComponent(saleId)}/lines/${encodeURIComponent(saleLineId)}`,
    ),
    { method: "DELETE" },
  );
}

export function confirmSale(
  organizationId: string,
  saleId: string,
): Promise<Sale> {
  return csrfRequest<Sale>(
    getSalesPath(organizationId, `/${encodeURIComponent(saleId)}/confirm`),
    { method: "PATCH" },
  );
}

export function cancelSale(
  organizationId: string,
  saleId: string,
  request: CancelSaleRequest,
): Promise<Sale> {
  return csrfRequest<Sale>(
    getSalesPath(organizationId, `/${encodeURIComponent(saleId)}/cancel`),
    { method: "PATCH", body: JSON.stringify(request) },
  );
}

function getSalePaymentsPath(
  organizationId: string,
  saleId: string,
  resourcePath = "",
): string {
  return getSalesPath(
    organizationId,
    `/${encodeURIComponent(saleId)}/payments${resourcePath}`,
  );
}

export function getSalePayments(
  organizationId: string,
  saleId: string,
  filter: SalePaymentFilter = {},
): Promise<SalePayment[]> {
  const search = new URLSearchParams();
  if (filter.status !== undefined) search.set("status", String(filter.status));
  if (filter.paymentMethod !== undefined) {
    search.set("paymentMethod", String(filter.paymentMethod));
  }
  if (filter.paymentDateFrom) search.set("paymentDateFrom", filter.paymentDateFrom);
  if (filter.paymentDateTo) search.set("paymentDateTo", filter.paymentDateTo);
  if (filter.receivedFrom?.trim()) search.set("receivedFrom", filter.receivedFrom.trim());
  const query = search.size > 0 ? `?${search.toString()}` : "";

  return apiRequest<SalePayment[]>(
    getSalePaymentsPath(organizationId, saleId, query),
  );
}

export function getSaleReceivable(
  organizationId: string,
  saleId: string,
): Promise<SaleReceivable> {
  return apiRequest<SaleReceivable>(
    getSalePaymentsPath(organizationId, saleId, "/receivable"),
  );
}

export function createSalePayment(
  organizationId: string,
  saleId: string,
  request: CreateSalePaymentRequest,
): Promise<SalePayment> {
  return csrfRequest<SalePayment>(
    getSalePaymentsPath(organizationId, saleId),
    { method: "POST", body: JSON.stringify(request) },
  );
}

export function updateSalePayment(
  organizationId: string,
  saleId: string,
  paymentId: string,
  request: UpdateSalePaymentRequest,
): Promise<SalePayment> {
  return csrfRequest<SalePayment>(
    getSalePaymentsPath(
      organizationId,
      saleId,
      `/${encodeURIComponent(paymentId)}`,
    ),
    { method: "PUT", body: JSON.stringify(request) },
  );
}

export function confirmSalePayment(
  organizationId: string,
  saleId: string,
  paymentId: string,
): Promise<SalePayment> {
  return csrfRequest<SalePayment>(
    getSalePaymentsPath(
      organizationId,
      saleId,
      `/${encodeURIComponent(paymentId)}/confirm`,
    ),
    { method: "PATCH" },
  );
}

export function cancelSalePayment(
  organizationId: string,
  saleId: string,
  paymentId: string,
  request: CancelSalePaymentRequest,
): Promise<SalePayment> {
  return csrfRequest<SalePayment>(
    getSalePaymentsPath(
      organizationId,
      saleId,
      `/${encodeURIComponent(paymentId)}/cancel`,
    ),
    { method: "PATCH", body: JSON.stringify(request) },
  );
}

function getCultivationExpensesPath(
  organizationId: string,
  cropCycleId: string,
  suffix = "",
): string {
  return getOrganizationResourcePath(
    organizationId,
    `/crop-cycles/${encodeURIComponent(cropCycleId)}/expenses${suffix}`,
  );
}

export function getCultivationExpenses(
  organizationId: string,
  cropCycleId: string,
  filter: CultivationExpenseFilter = {},
): Promise<CultivationExpense[]> {
  const search = new URLSearchParams();
  if (filter.status !== undefined) search.set("status", String(filter.status));
  if (filter.category !== undefined) search.set("category", String(filter.category));
  if (filter.expenseDateFrom) search.set("expenseDateFrom", filter.expenseDateFrom);
  if (filter.expenseDateTo) search.set("expenseDateTo", filter.expenseDateTo);
  if (filter.payeeName) search.set("payeeName", filter.payeeName);
  const query = search.toString();

  return apiRequest<CultivationExpense[]>(
    `${getCultivationExpensesPath(organizationId, cropCycleId)}${query ? `?${query}` : ""}`,
  );
}

export function createCultivationExpense(
  organizationId: string,
  cropCycleId: string,
  request: CreateCultivationExpenseRequest,
): Promise<CultivationExpense> {
  return csrfRequest<CultivationExpense>(
    getCultivationExpensesPath(organizationId, cropCycleId),
    { method: "POST", body: JSON.stringify(request) },
  );
}

export function updateCultivationExpense(
  organizationId: string,
  cropCycleId: string,
  expenseId: string,
  request: UpdateCultivationExpenseRequest,
): Promise<CultivationExpense> {
  return csrfRequest<CultivationExpense>(
    getCultivationExpensesPath(
      organizationId,
      cropCycleId,
      `/${encodeURIComponent(expenseId)}`,
    ),
    { method: "PUT", body: JSON.stringify(request) },
  );
}

export function confirmCultivationExpense(
  organizationId: string,
  cropCycleId: string,
  expenseId: string,
): Promise<CultivationExpense> {
  return csrfRequest<CultivationExpense>(
    getCultivationExpensesPath(
      organizationId,
      cropCycleId,
      `/${encodeURIComponent(expenseId)}/confirm`,
    ),
    { method: "PATCH" },
  );
}

export function cancelCultivationExpense(
  organizationId: string,
  cropCycleId: string,
  expenseId: string,
  request: CancelCultivationExpenseRequest,
): Promise<CultivationExpense> {
  return csrfRequest<CultivationExpense>(
    getCultivationExpensesPath(
      organizationId,
      cropCycleId,
      `/${encodeURIComponent(expenseId)}/cancel`,
    ),
    { method: "PATCH", body: JSON.stringify(request) },
  );
}

export function getCropCycleProfitability(
  organizationId: string,
  cropCycleId: string,
): Promise<CropCycleProfitability> {
  return apiRequest<CropCycleProfitability>(
    getOrganizationResourcePath(
      organizationId,
      `/crop-cycles/${encodeURIComponent(cropCycleId)}/profitability`,
    ),
  );
}

function getCapitalContributionsPath(
  organizationId: string,
  cropCycleId: string,
  suffix = "",
): string {
  return getOrganizationResourcePath(
    organizationId,
    `/crop-cycles/${encodeURIComponent(cropCycleId)}/capital-contributions${suffix}`,
  );
}

export function getCapitalContributions(
  organizationId: string,
  cropCycleId: string,
  filter: CapitalContributionFilter = {},
): Promise<CapitalContribution[]> {
  const search = new URLSearchParams();
  if (filter.status !== undefined) search.set("status", String(filter.status));
  if (filter.contributorRole !== undefined) {
    search.set("contributorRole", String(filter.contributorRole));
  }
  if (filter.contributionDateFrom) {
    search.set("contributionDateFrom", filter.contributionDateFrom);
  }
  if (filter.contributionDateTo) search.set("contributionDateTo", filter.contributionDateTo);
  if (filter.contributorCode?.trim()) search.set("contributorCode", filter.contributorCode.trim());
  if (filter.contributorName?.trim()) search.set("contributorName", filter.contributorName.trim());
  const query = search.toString();

  return apiRequest<CapitalContribution[]>(
    `${getCapitalContributionsPath(organizationId, cropCycleId)}${query ? `?${query}` : ""}`,
  );
}

export function createCapitalContribution(
  organizationId: string,
  cropCycleId: string,
  request: CreateCapitalContributionRequest,
): Promise<CapitalContribution> {
  return csrfRequest<CapitalContribution>(
    getCapitalContributionsPath(organizationId, cropCycleId),
    { method: "POST", body: JSON.stringify(request) },
  );
}

export function updateCapitalContribution(
  organizationId: string,
  cropCycleId: string,
  contributionId: string,
  request: UpdateCapitalContributionRequest,
): Promise<CapitalContribution> {
  return csrfRequest<CapitalContribution>(
    getCapitalContributionsPath(
      organizationId,
      cropCycleId,
      `/${encodeURIComponent(contributionId)}`,
    ),
    { method: "PUT", body: JSON.stringify(request) },
  );
}

export function confirmCapitalContribution(
  organizationId: string,
  cropCycleId: string,
  contributionId: string,
): Promise<CapitalContribution> {
  return csrfRequest<CapitalContribution>(
    getCapitalContributionsPath(
      organizationId,
      cropCycleId,
      `/${encodeURIComponent(contributionId)}/confirm`,
    ),
    { method: "PATCH" },
  );
}

export function cancelCapitalContribution(
  organizationId: string,
  cropCycleId: string,
  contributionId: string,
  request: CancelCapitalContributionRequest,
): Promise<CapitalContribution> {
  return csrfRequest<CapitalContribution>(
    getCapitalContributionsPath(
      organizationId,
      cropCycleId,
      `/${encodeURIComponent(contributionId)}/cancel`,
    ),
    { method: "PATCH", body: JSON.stringify(request) },
  );
}

function getProfitSharingSettlementsPath(
  organizationId: string,
  cropCycleId: string,
  suffix = "",
): string {
  return getOrganizationResourcePath(
    organizationId,
    `/crop-cycles/${encodeURIComponent(cropCycleId)}/profit-sharing-settlements${suffix}`,
  );
}

export function getProfitSharingSettlements(
  organizationId: string,
  cropCycleId: string,
  filter: ProfitSharingSettlementFilter = {},
): Promise<ProfitSharingSettlement[]> {
  const search = new URLSearchParams();
  if (filter.status !== undefined) search.set("status", String(filter.status));
  if (filter.settlementDateFrom) search.set("settlementDateFrom", filter.settlementDateFrom);
  if (filter.settlementDateTo) search.set("settlementDateTo", filter.settlementDateTo);
  if (filter.managingPartnerCode?.trim()) {
    search.set("managingPartnerCode", filter.managingPartnerCode.trim());
  }
  const query = search.toString();

  return apiRequest<ProfitSharingSettlement[]>(
    `${getProfitSharingSettlementsPath(organizationId, cropCycleId)}${query ? `?${query}` : ""}`,
  );
}

export function createProfitSharingSettlement(
  organizationId: string,
  cropCycleId: string,
  request: CreateProfitSharingSettlementRequest,
): Promise<ProfitSharingSettlement> {
  return csrfRequest<ProfitSharingSettlement>(
    getProfitSharingSettlementsPath(organizationId, cropCycleId),
    { method: "POST", body: JSON.stringify(request) },
  );
}

export function updateProfitSharingSettlement(
  organizationId: string,
  cropCycleId: string,
  settlementId: string,
  request: UpdateProfitSharingSettlementRequest,
): Promise<ProfitSharingSettlement> {
  return csrfRequest<ProfitSharingSettlement>(
    getProfitSharingSettlementsPath(
      organizationId,
      cropCycleId,
      `/${encodeURIComponent(settlementId)}`,
    ),
    { method: "PUT", body: JSON.stringify(request) },
  );
}

export function finalizeProfitSharingSettlement(
  organizationId: string,
  cropCycleId: string,
  settlementId: string,
): Promise<ProfitSharingSettlement> {
  return csrfRequest<ProfitSharingSettlement>(
    getProfitSharingSettlementsPath(
      organizationId,
      cropCycleId,
      `/${encodeURIComponent(settlementId)}/finalize`,
    ),
    { method: "PATCH" },
  );
}

export function voidProfitSharingSettlement(
  organizationId: string,
  cropCycleId: string,
  settlementId: string,
  request: VoidProfitSharingSettlementRequest,
): Promise<ProfitSharingSettlement> {
  return csrfRequest<ProfitSharingSettlement>(
    getProfitSharingSettlementsPath(
      organizationId,
      cropCycleId,
      `/${encodeURIComponent(settlementId)}/void`,
    ),
    { method: "PATCH", body: JSON.stringify(request) },
  );
}

function getProfitSharingSchemesPath(
  organizationId: string,
  suffix = "",
): string {
  return getOrganizationResourcePath(
    organizationId,
    `/profit-sharing-schemes${suffix}`,
  );
}

export function getProfitSharingSchemes(
  organizationId: string,
  filter: ProfitSharingSchemeFilter = {},
): Promise<ProfitSharingScheme[]> {
  const search = new URLSearchParams();
  if (filter.status !== undefined) search.set("status", String(filter.status));
  if (filter.code?.trim()) search.set("code", filter.code.trim());
  const query = search.toString();

  return apiRequest<ProfitSharingScheme[]>(
    `${getProfitSharingSchemesPath(organizationId)}${query ? `?${query}` : ""}`,
  );
}

export function getProfitSharingScheme(
  organizationId: string,
  schemeId: string,
): Promise<ProfitSharingScheme> {
  return apiRequest<ProfitSharingScheme>(
    getProfitSharingSchemesPath(
      organizationId,
      `/${encodeURIComponent(schemeId)}`,
    ),
  );
}

export function createProfitSharingScheme(
  organizationId: string,
  request: CreateProfitSharingSchemeRequest,
): Promise<ProfitSharingScheme> {
  return csrfRequest<ProfitSharingScheme>(
    getProfitSharingSchemesPath(organizationId),
    { method: "POST", body: JSON.stringify(request) },
  );
}

export function updateProfitSharingScheme(
  organizationId: string,
  schemeId: string,
  request: UpdateProfitSharingSchemeDraftRequest,
): Promise<ProfitSharingScheme> {
  return csrfRequest<ProfitSharingScheme>(
    getProfitSharingSchemesPath(
      organizationId,
      `/${encodeURIComponent(schemeId)}`,
    ),
    { method: "PUT", body: JSON.stringify(request) },
  );
}

export function createNextProfitSharingSchemeVersion(
  organizationId: string,
  sourceSchemeId: string,
): Promise<ProfitSharingScheme> {
  return csrfRequest<ProfitSharingScheme>(
    getProfitSharingSchemesPath(
      organizationId,
      `/${encodeURIComponent(sourceSchemeId)}/versions`,
    ),
    { method: "POST" },
  );
}

export function activateProfitSharingScheme(
  organizationId: string,
  schemeId: string,
): Promise<ProfitSharingScheme> {
  return csrfRequest<ProfitSharingScheme>(
    getProfitSharingSchemesPath(
      organizationId,
      `/${encodeURIComponent(schemeId)}/activate`,
    ),
    { method: "PATCH" },
  );
}

function getProfitSharingSchemeAssignmentPath(
  organizationId: string,
  cropCycleId: string,
): string {
  return getOrganizationResourcePath(
    organizationId,
    `/crop-cycles/${encodeURIComponent(cropCycleId)}/profit-sharing-scheme`,
  );
}

export function getProfitSharingSchemeAssignment(
  organizationId: string,
  cropCycleId: string,
): Promise<ProfitSharingSchemeAssignment> {
  return apiRequest<ProfitSharingSchemeAssignment>(
    getProfitSharingSchemeAssignmentPath(organizationId, cropCycleId),
  );
}

export function assignProfitSharingScheme(
  organizationId: string,
  cropCycleId: string,
  request: AssignProfitSharingSchemeRequest,
): Promise<ProfitSharingSchemeAssignment> {
  return csrfRequest<ProfitSharingSchemeAssignment>(
    getProfitSharingSchemeAssignmentPath(organizationId, cropCycleId),
    { method: "PUT", body: JSON.stringify(request) },
  );
}

export function getProfitSharingPreview(
  organizationId: string,
  cropCycleId: string,
): Promise<ProfitSharingPreview> {
  return apiRequest<ProfitSharingPreview>(
    getOrganizationResourcePath(
      organizationId,
      `/crop-cycles/${encodeURIComponent(cropCycleId)}/profit-sharing-preview`,
    ),
  );
}

function getProfitSharingWaterfallSettlementsPath(
  organizationId: string,
  cropCycleId: string,
  suffix = "",
): string {
  return getOrganizationResourcePath(
    organizationId,
    `/crop-cycles/${encodeURIComponent(cropCycleId)}/profit-sharing-waterfall-settlements${suffix}`,
  );
}

export function getProfitSharingWaterfallSettlements(
  organizationId: string,
  cropCycleId: string,
  filter: ProfitSharingWaterfallSettlementFilter = {},
): Promise<ProfitSharingWaterfallSettlement[]> {
  const search = new URLSearchParams();
  if (filter.status !== undefined) search.set("status", String(filter.status));
  if (filter.settlementDateFrom) {
    search.set("settlementDateFrom", filter.settlementDateFrom);
  }
  if (filter.settlementDateTo) search.set("settlementDateTo", filter.settlementDateTo);
  const query = search.toString();

  return apiRequest<ProfitSharingWaterfallSettlement[]>(
    `${getProfitSharingWaterfallSettlementsPath(organizationId, cropCycleId)}${query ? `?${query}` : ""}`,
  );
}

export function getProfitSharingWaterfallSettlement(
  organizationId: string,
  cropCycleId: string,
  settlementId: string,
): Promise<ProfitSharingWaterfallSettlement> {
  return apiRequest<ProfitSharingWaterfallSettlement>(
    getProfitSharingWaterfallSettlementsPath(
      organizationId,
      cropCycleId,
      `/${encodeURIComponent(settlementId)}`,
    ),
  );
}

export function finalizeProfitSharingWaterfallSettlement(
  organizationId: string,
  cropCycleId: string,
  request: FinalizeProfitSharingWaterfallSettlementRequest,
): Promise<ProfitSharingWaterfallSettlement> {
  return csrfRequest<ProfitSharingWaterfallSettlement>(
    getProfitSharingWaterfallSettlementsPath(organizationId, cropCycleId),
    { method: "POST", body: JSON.stringify(request) },
  );
}

export function voidProfitSharingWaterfallSettlement(
  organizationId: string,
  cropCycleId: string,
  settlementId: string,
  request: VoidProfitSharingWaterfallSettlementRequest,
): Promise<ProfitSharingWaterfallSettlement> {
  return csrfRequest<ProfitSharingWaterfallSettlement>(
    getProfitSharingWaterfallSettlementsPath(
      organizationId,
      cropCycleId,
      `/${encodeURIComponent(settlementId)}/void`,
    ),
    { method: "PATCH", body: JSON.stringify(request) },
  );
}

export function createSeasonReview(
  organizationId: string,
  request: import("@/lib/api/contracts").CreateSeasonReviewRequest,
): Promise<import("@/lib/api/contracts").SeasonReview> {
  return csrfRequest(
    getOrganizationResourcePath(organizationId, "/season-reviews"),
    { method: "POST", body: JSON.stringify(request) },
  );
}

export function getSeasonReviewByCropCycle(
  organizationId: string,
  cropCycleId: string,
): Promise<import("@/lib/api/contracts").SeasonReview> {
  return apiRequest(
    getOrganizationResourcePath(
      organizationId,
      `/season-reviews/by-crop-cycle/${encodeURIComponent(cropCycleId)}`,
    ),
  );
}

export function updateSeasonReview(
  organizationId: string,
  reviewId: string,
  request: import("@/lib/api/contracts").UpdateSeasonReviewRequest,
): Promise<import("@/lib/api/contracts").SeasonReview> {
  return csrfRequest(
    getOrganizationResourcePath(
      organizationId,
      `/season-reviews/${encodeURIComponent(reviewId)}`,
    ),
    { method: "PUT", body: JSON.stringify(request) },
  );
}

export function finalizeSeasonReview(
  organizationId: string,
  reviewId: string,
): Promise<import("@/lib/api/contracts").SeasonReview> {
  return csrfRequest(
    getOrganizationResourcePath(
      organizationId,
      `/season-reviews/${encodeURIComponent(reviewId)}/finalize`,
    ),
    { method: "PATCH" },
  );
}
