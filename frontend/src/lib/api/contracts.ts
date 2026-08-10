export type OrganizationRole =
  | 1
  | 2
  | 3
  | 4
  | "Owner"
  | "Admin"
  | "Finance"
  | "Operator";

export type AntiforgeryTokenResponse = {
  requestToken: string;
  headerName: string;
};

export type CurrentUserMembership = {
  membershipId: string;
  organizationId: string;
  role: OrganizationRole;
  permissions: string[];
};

export type CurrentUser = {
  userId: string;
  email: string;
  emailConfirmed: boolean;
  lastLoginAt: string | null;
  memberships: CurrentUserMembership[];
};

export type LoginRequest = {
  email: string;
  password: string;
  rememberMe: boolean;
};

export type BootstrapStatus = {
  isConfigured: boolean;
  isInitialized: boolean;
  canBootstrap: boolean;
};

export type BootstrapOwnerRequest = {
  organizationCode: string;
  organizationName: string;
  organizationLegalName: string | null;
  organizationTimeZone: string;
  email: string;
  password: string;
};

export type BootstrapOwnerResponse = {
  userId: string;
  email: string;
  organizationId: string;
  organizationCode: string;
  organizationName: string;
  membershipId: string;
  role: OrganizationRole;
  createdAt: string;
};

export type Organization = {
  id: string;
  code: string;
  name: string;
  legalName: string | null;
  timeZone: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string | null;
};

export type LandPlot = {
  id: string;
  landId: string;
  code: string;
  name: string;
  area: number;
  areaUnit: 1 | 2;
  generalCondition: string | null;
  notes: string | null;
  isActive: boolean;
  createdAt: string;
  updatedAt: string | null;
};

export type AreaUnit = 1 | 2;

export type LandTenureType = 1 | 2 | 3 | 4 | 5;

export type CreateLandRequest = {
  code: string;
  name: string;
  tenureType: LandTenureType;
  totalArea: number;
  areaUnit: AreaUnit;
  address: string | null;
  locationDescription: string | null;
  latitude: number | null;
  longitude: number | null;
  notes: string | null;
};

export type UpdateLandRequest = Omit<CreateLandRequest, "code">;

export type AddLandPlotRequest = {
  code: string;
  name: string;
  area: number;
  areaUnit: AreaUnit;
  generalCondition: string | null;
  notes: string | null;
};

export type UpdateLandPlotRequest = Omit<AddLandPlotRequest, "code">;

export type Land = {
  id: string;
  organizationId: string;
  code: string;
  name: string;
  tenureType: 1 | 2 | 3 | 4 | 5;
  totalArea: number;
  areaUnit: 1 | 2;
  totalAreaInSquareMeters: number;
  allocatedPlotAreaInSquareMeters: number;
  address: string | null;
  locationDescription: string | null;
  latitude: number | null;
  longitude: number | null;
  notes: string | null;
  isActive: boolean;
  createdAt: string;
  updatedAt: string | null;
  plots: LandPlot[];
};

export type Commodity = {
  id: string;
  organizationId: string;
  code: string;
  name: string;
  commodityCategoryId: string;
  scientificName: string | null;
  description: string | null;
  isActive: boolean;
  createdAt: string;
  updatedAt: string | null;
};

export type CultivationSopStep = {
  id: string;
  organizationId: string;
  cultivationSopId: string;
  sequence: number;
  name: string;
  description: string | null;
  plannedDayOffset: number;
  estimatedDurationDays: number;
  isRequired: boolean;
  createdAt: string;
  updatedAt: string | null;
};

export type CultivationSop = {
  id: string;
  organizationId: string;
  commodityId: string;
  name: string;
  description: string | null;
  isActive: boolean;
  createdAt: string;
  updatedAt: string | null;
  steps: CultivationSopStep[];
};

export type CropCycleStatus = 1 | 2 | 3 | 4;

export type CropCycle = {
  id: string;
  organizationId: string;
  code: string;
  name: string;
  commodityId: string;
  cultivationSopId: string | null;
  landId: string;
  landPlotId: string;
  plantedArea: number;
  areaUnit: 1 | 2;
  plantedAreaInSquareMeters: number;
  plannedStartDate: string;
  expectedHarvestDate: string;
  actualStartDate: string | null;
  actualHarvestDate: string | null;
  status: CropCycleStatus;
  cancellationReason: string | null;
  notes: string | null;
  createdAt: string;
  updatedAt: string | null;
};

export type CreateCropCycleRequest = {
  code: string;
  name: string;
  commodityId: string;
  cultivationSopId: string | null;
  landId: string;
  landPlotId: string;
  plantedArea: number;
  areaUnit: AreaUnit;
  plannedStartDate: string;
  expectedHarvestDate: string;
  notes: string | null;
};

export type UpdateCropCyclePlanRequest = Omit<
  CreateCropCycleRequest,
  "code" | "commodityId" | "landId" | "landPlotId"
>;

export type StartCropCycleRequest = {
  actualStartDate: string;
};

export type CompleteCropCycleRequest = {
  actualHarvestDate: string;
};

export type CancelCropCycleRequest = {
  cancellationReason: string;
};

export type UpdateCropCycleNotesRequest = {
  notes: string | null;
};

export type CultivationActivityStatus = 1 | 2 | 3 | 4;

export type CultivationActivityType =
  | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10;

export type CultivationResourceType = 1 | 2 | 3 | 4 | 5;

export type SopComplianceStatus = 1 | 2 | 3 | 4;

export type CultivationActivityResource = {
  id: string;
  organizationId: string;
  cultivationActivityId: string;
  resourceType: CultivationResourceType;
  description: string;
  quantity: number;
  unit: string;
  unitCost: number;
  totalCost: number;
  notes: string | null;
  createdAt: string;
  updatedAt: string | null;
};

export type CultivationActivity = {
  id: string;
  organizationId: string;
  cropCycleId: string;
  code: string;
  name: string;
  activityType: CultivationActivityType;
  cultivationSopId: string | null;
  cultivationSopStepId: string | null;
  sopStepSequenceSnapshot: number | null;
  sopStepNameSnapshot: string | null;
  sopPlannedDayOffsetSnapshot: number | null;
  sopEstimatedDurationDaysSnapshot: number | null;
  sopIsRequiredSnapshot: boolean | null;
  plannedDate: string;
  actualStartDate: string | null;
  actualCompletionDate: string | null;
  status: CultivationActivityStatus;
  sopComplianceStatus: SopComplianceStatus;
  outcome: string | null;
  issueNotes: string | null;
  deviationReason: string | null;
  cancellationReason: string | null;
  notes: string | null;
  totalActualCost: number;
  resources: CultivationActivityResource[];
  createdAt: string;
  updatedAt: string | null;
};

export type CreateCultivationActivityRequest = {
  code: string;
  name: string;
  activityType: CultivationActivityType;
  plannedDate: string;
  cultivationSopId: string | null;
  cultivationSopStepId: string | null;
  notes: string | null;
};

export type UpdateCultivationActivityPlanRequest = {
  name: string;
  activityType: CultivationActivityType;
  plannedDate: string;
  notes: string | null;
};

export type StartCultivationActivityRequest = {
  actualStartDate: string;
};

export type CompleteCultivationActivityRequest = {
  actualCompletionDate: string;
  outcome: string | null;
  issueNotes: string | null;
  sopComplianceStatus: SopComplianceStatus;
  deviationReason: string | null;
};

export type CancelCultivationActivityRequest = {
  cancellationReason: string;
};

export type UpdateCultivationActivityNotesRequest = {
  notes: string | null;
  issueNotes: string | null;
};

export type AddCultivationActivityResourceRequest = {
  resourceType: CultivationResourceType;
  description: string;
  quantity: number;
  unit: string;
  unitCost: number;
  notes: string | null;
};

export type UpdateCultivationActivityResourceRequest = Omit<
  AddCultivationActivityResourceRequest,
  "resourceType"
>;

export type HarvestQuantityUnit = 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8;

export type HarvestBatchStatus = 1 | 2 | 3;

export type HarvestBatch = {
  id: string;
  organizationId: string;
  cropCycleId: string;
  code: string;
  harvestDate: string;
  grossQuantity: number;
  rejectedQuantity: number;
  netQuantity: number;
  quantityUnit: HarvestQuantityUnit;
  qualityGrade: string | null;
  storageLocation: string | null;
  notes: string | null;
  status: HarvestBatchStatus;
  confirmedAt: string | null;
  cancellationReason: string | null;
  confirmedSoldQuantity: number;
  availableQuantity: number;
  createdAt: string;
  updatedAt: string | null;
};

export type CreateHarvestBatchRequest = {
  code: string;
  harvestDate: string;
  grossQuantity: number;
  rejectedQuantity: number;
  quantityUnit: HarvestQuantityUnit;
  qualityGrade: string | null;
  storageLocation: string | null;
  notes: string | null;
};

export type UpdateHarvestBatchRequest = Omit<
  CreateHarvestBatchRequest,
  "code"
>;

export type CancelHarvestBatchRequest = {
  cancellationReason: string;
};

export type HarvestBatchFilter = {
  status?: HarvestBatchStatus;
  harvestDateFrom?: string;
  harvestDateTo?: string;
  quantityUnit?: HarvestQuantityUnit;
  qualityGrade?: string;
};

export type CropCycleProfitability = {
  organizationId: string;
  cropCycleId: string;
  cropCycleCode: string;
  cropCycleName: string;
  commodityIdSnapshot: string;
  commodityCodeSnapshot: string;
  commodityNameSnapshot: string;
  recognizedRevenue: number;
  collectedRevenue: number;
  outstandingReceivable: number;
  activityResourceCost: number;
  manualExpenseCost: number;
  totalCultivationCost: number;
  netProfit: number;
  profitMarginPercentage: number | null;
  outcome: 1 | 2 | 3;
  confirmedInvestorCapital: number;
  confirmedPartnerCapital: number;
  totalConfirmedCapital: number;
  capitalFundingGap: number;
  capitalFundingExcess: number;
  availableHarvestQuantity: number;
  harvestQuantityUnit: HarvestQuantityUnit | null;
  generatedAt: string;
};

export type ApiProblem = {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  code?: string;
  errors?: string[];
  [key: string]: unknown;
};
