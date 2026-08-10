import type {
  AntiforgeryTokenResponse,
  AddCultivationActivityResourceRequest,
  ApiProblem,
  BootstrapOwnerRequest,
  BootstrapOwnerResponse,
  BootstrapStatus,
  AddLandPlotRequest,
  CancelCropCycleRequest,
  CancelCultivationActivityRequest,
  CancelHarvestBatchRequest,
  Commodity,
  CompleteCropCycleRequest,
  CompleteCultivationActivityRequest,
  CropCycle,
  CropCycleProfitability,
  CreateCropCycleRequest,
  CreateCultivationActivityRequest,
  CreateLandRequest,
  CreateHarvestBatchRequest,
  CultivationSop,
  CultivationActivity,
  CurrentUser,
  HarvestBatch,
  HarvestBatchFilter,
  Land,
  LoginRequest,
  Organization,
  StartCropCycleRequest,
  StartCultivationActivityRequest,
  UpdateCultivationActivityNotesRequest,
  UpdateCultivationActivityPlanRequest,
  UpdateCultivationActivityResourceRequest,
  UpdateCropCyclePlanRequest,
  UpdateCropCycleNotesRequest,
  UpdateLandPlotRequest,
  UpdateLandRequest,
  UpdateHarvestBatchRequest,
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

export function getCultivationSops(
  organizationId: string,
): Promise<CultivationSop[]> {
  return apiRequest<CultivationSop[]>(
    getOrganizationResourcePath(organizationId, "/cultivation-sops"),
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
