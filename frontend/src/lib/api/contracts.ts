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

export type CultivationActivityStatus = 1 | 2 | 3 | 4;

export type CultivationActivityResource = {
  id: string;
  organizationId: string;
  cultivationActivityId: string;
  resourceType: 1 | 2 | 3 | 4 | 5;
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
  activityType: 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10;
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
  sopComplianceStatus: 1 | 2 | 3 | 4;
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

export type HarvestQuantityUnit = 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8;

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
  status: 1 | 2 | 3;
  confirmedAt: string | null;
  cancellationReason: string | null;
  confirmedSoldQuantity: number;
  availableQuantity: number;
  createdAt: string;
  updatedAt: string | null;
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
