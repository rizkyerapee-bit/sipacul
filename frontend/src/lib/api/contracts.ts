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

export type CommodityCategory = {
  id: string;
  organizationId: string;
  name: string;
  description: string | null;
  isActive: boolean;
  createdAt: string;
  updatedAt: string | null;
};

export type CreateCommodityCategoryRequest = {
  name: string;
  description: string | null;
};

export type UpdateCommodityCategoryRequest = CreateCommodityCategoryRequest;
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

export type CreateCommodityRequest = {
  code: string;
  name: string;
  commodityCategoryId: string;
  scientificName: string | null;
  description: string | null;
};

export type UpdateCommodityRequest = Omit<CreateCommodityRequest, "code">;
export type CreateCultivationSopRequest = {
  commodityId: string;
  name: string;
  description: string | null;
};

export type UpdateCultivationSopRequest = Omit<
  CreateCultivationSopRequest,
  "commodityId"
>;

export type AddCultivationSopStepRequest = {
  name: string;
  description: string | null;
  plannedDayOffset: number;
  estimatedDurationDays: number;
  isRequired: boolean;
};

export type UpdateCultivationSopStepRequest = AddCultivationSopStepRequest;

export type MoveCultivationSopStepRequest = {
  newSequence: number;
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

export type SaleStatus = 1 | 2 | 3;

export type SalePaymentTerm = 1 | 2;

export type SaleLine = {
  id: string;
  harvestBatchId: string;
  harvestBatchCodeSnapshot: string;
  cropCycleIdSnapshot: string;
  cropCycleCodeSnapshot: string;
  commodityIdSnapshot: string;
  commodityCodeSnapshot: string;
  commodityNameSnapshot: string;
  qualityGradeSnapshot: string | null;
  quantity: number;
  quantityUnit: HarvestQuantityUnit;
  unitPrice: number;
  lineDiscount: number;
  lineTotal: number;
  notes: string | null;
  createdAt: string;
  updatedAt: string | null;
};

export type Sale = {
  id: string;
  organizationId: string;
  code: string;
  saleDate: string;
  buyerName: string;
  buyerPhone: string | null;
  buyerAddress: string | null;
  paymentTerm: SalePaymentTerm;
  dueDate: string | null;
  discountAmount: number;
  subtotal: number;
  totalAmount: number;
  status: SaleStatus;
  confirmedAt: string | null;
  cancellationReason: string | null;
  notes: string | null;
  lines: SaleLine[];
  createdAt: string;
  updatedAt: string | null;
};

export type CreateSaleRequest = {
  code: string;
  saleDate: string;
  buyerName: string;
  buyerPhone: string | null;
  buyerAddress: string | null;
  paymentTerm: SalePaymentTerm;
  dueDate: string | null;
  notes: string | null;
};

export type UpdateSaleRequest = Omit<CreateSaleRequest, "code"> & {
  discountAmount: number;
};

export type AddSaleLineRequest = {
  harvestBatchId: string;
  quantity: number;
  quantityUnit: HarvestQuantityUnit;
  unitPrice: number;
  lineDiscount: number;
  notes: string | null;
};

export type UpdateSaleLineRequest = Omit<
  AddSaleLineRequest,
  "harvestBatchId" | "quantityUnit"
>;

export type CancelSaleRequest = {
  cancellationReason: string;
};

export type SaleFilter = {
  status?: SaleStatus;
  saleDateFrom?: string;
  saleDateTo?: string;
  paymentTerm?: SalePaymentTerm;
  buyerName?: string;
};

export type SalePaymentStatus = 1 | 2 | 3;

export type SalePaymentMethod = 1 | 2 | 3;

export type SalePaymentState = 1 | 2 | 3;

export type SalePayment = {
  id: string;
  organizationId: string;
  saleId: string;
  code: string;
  paymentDate: string;
  amount: number;
  paymentMethod: SalePaymentMethod;
  referenceNumber: string | null;
  receivedFrom: string | null;
  notes: string | null;
  status: SalePaymentStatus;
  isCollectedRevenue: boolean;
  confirmedAt: string | null;
  cancellationReason: string | null;
  createdAt: string;
  updatedAt: string | null;
};

export type SaleReceivable = {
  saleId: string;
  saleCode: string;
  saleDate: string;
  buyerName: string;
  paymentTerm: SalePaymentTerm;
  dueDate: string | null;
  saleTotalAmount: number;
  confirmedPaidAmount: number;
  outstandingReceivable: number;
  paymentState: SalePaymentState;
  isFullyPaid: boolean;
  hasCollectedRevenue: boolean;
};

export type CreateSalePaymentRequest = {
  code: string;
  paymentDate: string;
  amount: number;
  paymentMethod: SalePaymentMethod;
  referenceNumber: string | null;
  receivedFrom: string | null;
  notes: string | null;
};

export type UpdateSalePaymentRequest = Omit<
  CreateSalePaymentRequest,
  "code"
>;

export type CancelSalePaymentRequest = {
  cancellationReason: string;
};

export type SalePaymentFilter = {
  status?: SalePaymentStatus;
  paymentMethod?: SalePaymentMethod;
  paymentDateFrom?: string;
  paymentDateTo?: string;
  receivedFrom?: string;
};

export type CultivationExpenseStatus = 1 | 2 | 3;

export type CultivationExpenseCategory =
  | 1 | 2 | 3 | 4 | 5
  | 6 | 7 | 8 | 9 | 10
  | 11 | 12 | 13 | 14 | 15;

export type CultivationExpense = {
  id: string;
  organizationId: string;
  cropCycleId: string;
  code: string;
  expenseDate: string;
  category: CultivationExpenseCategory;
  description: string;
  amount: number;
  payeeName: string | null;
  referenceNumber: string | null;
  evidenceUrl: string | null;
  notes: string | null;
  status: CultivationExpenseStatus;
  isRecognizedCost: boolean;
  confirmedAt: string | null;
  cancellationReason: string | null;
  createdAt: string;
  updatedAt: string | null;
};

export type CreateCultivationExpenseRequest = {
  code: string;
  expenseDate: string;
  category: CultivationExpenseCategory;
  description: string;
  amount: number;
  payeeName: string | null;
  referenceNumber: string | null;
  evidenceUrl: string | null;
  notes: string | null;
};

export type UpdateCultivationExpenseRequest = Omit<
  CreateCultivationExpenseRequest,
  "code"
>;

export type CancelCultivationExpenseRequest = {
  cancellationReason: string;
};

export type CultivationExpenseFilter = {
  status?: CultivationExpenseStatus;
  category?: CultivationExpenseCategory;
  expenseDateFrom?: string;
  expenseDateTo?: string;
  payeeName?: string;
};

export type CapitalContributorRole = 1 | 2;
export type CapitalContributionPaymentMethod = 1 | 2 | 3;
export type CapitalContributionStatus = 1 | 2 | 3;

export type CapitalContribution = {
  id: string;
  organizationId: string;
  cropCycleId: string;
  code: string;
  contributionDate: string;
  contributorCode: string;
  contributorName: string;
  contributorRole: CapitalContributorRole;
  amount: number;
  paymentMethod: CapitalContributionPaymentMethod;
  referenceNumber: string | null;
  notes: string | null;
  status: CapitalContributionStatus;
  isConfirmedCapital: boolean;
  isInvestorCapital: boolean;
  isPartnerCapital: boolean;
  confirmedAt: string | null;
  cancellationReason: string | null;
  createdAt: string;
  updatedAt: string | null;
};

export type CreateCapitalContributionRequest = {
  code: string;
  contributionDate: string;
  contributorCode: string;
  contributorName: string;
  contributorRole: CapitalContributorRole;
  amount: number;
  paymentMethod: CapitalContributionPaymentMethod;
  referenceNumber: string | null;
  notes: string | null;
};

export type UpdateCapitalContributionRequest = Omit<
  CreateCapitalContributionRequest,
  "code"
>;

export type CancelCapitalContributionRequest = {
  cancellationReason: string;
};

export type CapitalContributionFilter = {
  status?: CapitalContributionStatus;
  contributorRole?: CapitalContributorRole;
  contributionDateFrom?: string;
  contributionDateTo?: string;
  contributorCode?: string;
  contributorName?: string;
};

export type ProfitabilityOutcome = 1 | 2 | 3;

export type SeasonEvaluationAttentionCode =
  | 1 | 2 | 3 | 4 | 5 | 6 | 7
  | 8 | 9 | 10 | 11 | 12 | 13 | 14;

export type SeasonEvaluationAttentionSeverity = 1 | 2 | 3;

export type SeasonEvaluationAttention = {
  code: SeasonEvaluationAttentionCode;
  severity: SeasonEvaluationAttentionSeverity;
  value: number | null;
};

export type SeasonEvaluation = {
  organizationId: string;
  cropCycleId: string;
  cropCycleCode: string;
  cropCycleName: string;
  landId: string;
  landCode: string;
  landName: string;
  landPlotId: string;
  landPlotCode: string;
  landPlotName: string;
  commodityId: string;
  commodityCode: string;
  commodityName: string;
  cropCycleStatus: CropCycleStatus;
  plannedStartDate: string;
  expectedHarvestDate: string;
  actualStartDate: string | null;
  actualHarvestDate: string | null;
  startVarianceDays: number | null;
  harvestVarianceDays: number | null;
  totalActivityCount: number;
  completedActivityCount: number;
  cancelledActivityCount: number;
  pendingActivityCount: number;
  issueActivityCount: number;
  activityCompletionPercentage: number | null;
  sopLinkedActivityCount: number;
  sopCompliantActivityCount: number;
  sopDeviatedActivityCount: number;
  sopNotEvaluatedActivityCount: number;
  sopCompliancePercentage: number | null;
  confirmedHarvestBatchCount: number;
  recognizedRevenue: number;
  collectedRevenue: number;
  outstandingReceivable: number;
  totalCultivationCost: number;
  netProfit: number;
  profitMarginPercentage: number | null;
  profitabilityOutcome: ProfitabilityOutcome;
  capitalFundingGap: number;
  isReadyForReview: boolean;
  requiresAttention: boolean;
  criticalAttentionCount: number;
  warningAttentionCount: number;
  informationAttentionCount: number;
  attentions: SeasonEvaluationAttention[];
  generatedAt: string;
};

export type LandSeasonHistory = {
  organizationId: string;
  landId: string;
  landCode: string;
  landName: string;
  landPlotId: string | null;
  landPlotCode: string | null;
  landPlotName: string | null;
  includeNonTerminal: boolean;
  page: number;
  pageSize: number;
  totalSeasonCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
  seasons: SeasonEvaluation[];
  generatedAt: string;
};

export type SeasonHistoryFilter = {
  landPlotId?: string;
  includeNonTerminal?: boolean;
  page?: number;
  pageSize?: number;
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
  outcome: ProfitabilityOutcome;
  confirmedInvestorCapital: number;
  confirmedPartnerCapital: number;
  totalConfirmedCapital: number;
  capitalFundingGap: number;
  capitalFundingExcess: number;
  availableHarvestQuantity: number;
  harvestQuantityUnit: HarvestQuantityUnit | null;
  generatedAt: string;
};

export type ProfitSharingSettlementStatus = 1 | 2 | 3;

export type ProfitSharingAllocation = {
  id: string;
  organizationId: string;
  profitSharingSettlementId: string;
  contributorCodeSnapshot: string;
  contributorNameSnapshot: string;
  contributorRole: CapitalContributorRole;
  confirmedCapital: number;
  capitalRatio: number;
  capitalRecovery: number;
  capitalLoss: number;
  managementProfitShare: number;
  capitalProfitShare: number;
  totalProfitShare: number;
  totalPayout: number;
  sequence: number;
  createdAt: string;
};

export type ProfitSharingSettlement = {
  id: string;
  organizationId: string;
  cropCycleId: string;
  code: string;
  settlementDate: string;
  managingPartnerCode: string;
  managingPartnerName: string;
  recognizedRevenue: number;
  collectedRevenue: number;
  outstandingReceivable: number;
  activityResourceCost: number;
  manualExpenseCost: number;
  totalCultivationCost: number;
  netProfit: number;
  outcome: ProfitabilityOutcome;
  managementProfitPool: number;
  capitalProfitPool: number;
  totalInvestorCapital: number;
  totalPartnerCapital: number;
  totalCapital: number;
  totalCapitalRecovery: number;
  totalCapitalLoss: number;
  totalInvestorProfitShare: number;
  totalPartnerProfitShare: number;
  totalPayout: number;
  calculationVersion: string;
  notes: string | null;
  status: ProfitSharingSettlementStatus;
  isActive: boolean;
  finalizedAt: string | null;
  voidedAt: string | null;
  voidReason: string | null;
  createdAt: string;
  updatedAt: string | null;
  allocations: ProfitSharingAllocation[];
};

export type CreateProfitSharingSettlementRequest = {
  code: string;
  settlementDate: string;
  managingPartnerCode: string;
  managingPartnerName: string;
  notes: string | null;
};

export type UpdateProfitSharingSettlementRequest = {
  settlementDate: string;
  notes: string | null;
};

export type VoidProfitSharingSettlementRequest = {
  voidReason: string;
};

export type ProfitSharingSettlementFilter = {
  status?: ProfitSharingSettlementStatus;
  settlementDateFrom?: string;
  settlementDateTo?: string;
  managingPartnerCode?: string;
};

export type ProfitSharingParticipantRole = 1 | 2 | 3 | 4;

export type ProfitSharingPriorityRuleType = 1 | 2;

export type ProfitSharingResidualMethod = 1 | 2 | 3;

export type ProfitSharingSchemeStatus = 1 | 2 | 3;

export type ProfitSharingWaterfallSettlementStatus = 1 | 2;

export type ProfitSharingSchemeParticipantRequest = {
  participantCode: string;
  participantName: string;
  participantRole: ProfitSharingParticipantRole;
  participatesInResidualProfit: boolean;
  sequence: number;
};

export type ProfitSharingSchemePriorityRuleRequest = {
  ruleCode: string;
  ruleType: ProfitSharingPriorityRuleType;
  recipientCode: string;
  rateNumerator: number;
  rateDenominator: number;
  sequence: number;
};

export type ProfitSharingSchemeResidualShareRequest = {
  recipientCode: string;
  rateNumerator: number;
  rateDenominator: number;
  sequence: number;
};

export type CreateProfitSharingSchemeRequest = {
  code: string;
  name: string;
  description: string | null;
  participants: ProfitSharingSchemeParticipantRequest[];
  priorityRules: ProfitSharingSchemePriorityRuleRequest[];
  residualMethod: ProfitSharingResidualMethod;
  residualRecipientCode: string | null;
  residualShares: ProfitSharingSchemeResidualShareRequest[];
};

export type UpdateProfitSharingSchemeDraftRequest = Omit<
  CreateProfitSharingSchemeRequest,
  "code"
>;

export type ProfitSharingSchemeFilter = {
  status?: ProfitSharingSchemeStatus;
  code?: string;
};

export type ProfitSharingSchemeParticipant =
  ProfitSharingSchemeParticipantRequest & {
    id: string;
  };

export type ProfitSharingSchemePriorityRule =
  ProfitSharingSchemePriorityRuleRequest & {
    id: string;
  };

export type ProfitSharingSchemeResidualShare =
  ProfitSharingSchemeResidualShareRequest & {
    id: string;
  };

export type ProfitSharingScheme = {
  id: string;
  organizationId: string;
  schemeFamilyId: string;
  code: string;
  name: string;
  description: string | null;
  version: number;
  status: ProfitSharingSchemeStatus;
  residualMethod: ProfitSharingResidualMethod;
  residualRecipientCode: string | null;
  activatedAt: string | null;
  supersededAt: string | null;
  createdAt: string;
  updatedAt: string | null;
  participants: ProfitSharingSchemeParticipant[];
  priorityRules: ProfitSharingSchemePriorityRule[];
  residualShares: ProfitSharingSchemeResidualShare[];
};

export type AssignProfitSharingSchemeRequest = {
  schemeId: string;
};

export type ProfitSharingSchemeAssignmentParticipant =
  ProfitSharingSchemeParticipant;

export type ProfitSharingSchemeAssignmentPriorityRule =
  ProfitSharingSchemePriorityRule;

export type ProfitSharingSchemeAssignmentResidualShare =
  ProfitSharingSchemeResidualShare;

export type ProfitSharingSchemeAssignment = {
  id: string;
  organizationId: string;
  cropCycleId: string;
  sourceSchemeId: string;
  schemeFamilyId: string;
  schemeCode: string;
  schemeName: string;
  schemeDescription: string | null;
  schemeVersion: number;
  residualMethod: ProfitSharingResidualMethod;
  residualRecipientCode: string | null;
  assignedAt: string;
  createdAt: string;
  updatedAt: string | null;
  participants: ProfitSharingSchemeAssignmentParticipant[];
  priorityRules: ProfitSharingSchemeAssignmentPriorityRule[];
  residualShares: ProfitSharingSchemeAssignmentResidualShare[];
};

export type ProfitSharingPriorityAllocationPreview = {
  ruleCode: string;
  ruleType: ProfitSharingPriorityRuleType;
  recipientCodeSnapshot: string;
  recipientNameSnapshot: string;
  rateNumerator: number;
  rateDenominator: number;
  baseAmount: number;
  requestedAmount: number;
  allocatedAmount: number;
  unallocatedAmount: number;
  sequence: number;
};

export type ProfitSharingParticipantAllocationPreview = {
  participantCodeSnapshot: string;
  participantNameSnapshot: string;
  participantRole: ProfitSharingParticipantRole;
  confirmedCapital: number;
  capitalRatio: number;
  participatesInResidualProfit: boolean;
  capitalRecovery: number;
  capitalLoss: number;
  managementProfitShare: number;
  returnOnCapitalProfitShare: number;
  residualProfitShare: number;
  totalProfitShare: number;
  totalPayout: number;
  sequence: number;
};

export type ProfitSharingPreviewTotals = {
  totalCapital: number;
  totalCapitalRecovery: number;
  totalCapitalLoss: number;
  totalManagementProfitShare: number;
  totalReturnOnCapitalProfitShare: number;
  totalPriorityProfitShare: number;
  totalResidualProfitShare: number;
  totalProfitShare: number;
  totalPayout: number;
  residualMethod: ProfitSharingResidualMethod;
};

export type ProfitSharingPreview = {
  organizationId: string;
  cropCycleId: string;
  isPersisted: boolean;
  calculationVersion: string;
  generatedAt: string;
  schemeSnapshot: ProfitSharingSchemeAssignment;
  profitability: CropCycleProfitability;
  totals: ProfitSharingPreviewTotals;
  priorityAllocations: ProfitSharingPriorityAllocationPreview[];
  allocations: ProfitSharingParticipantAllocationPreview[];
};

export type FinalizeProfitSharingWaterfallSettlementRequest = {
  code: string;
  settlementDate: string;
  notes: string | null;
};

export type VoidProfitSharingWaterfallSettlementRequest = {
  voidReason: string;
};

export type ProfitSharingWaterfallSettlementFilter = {
  status?: ProfitSharingWaterfallSettlementStatus;
  settlementDateFrom?: string;
  settlementDateTo?: string;
};

export type ProfitSharingWaterfallPriorityAllocation =
  ProfitSharingPriorityAllocationPreview & {
    id: string;
  };

export type ProfitSharingWaterfallParticipantAllocation =
  ProfitSharingParticipantAllocationPreview & {
    id: string;
  };

export type ProfitSharingWaterfallResidualShare = {
  id: string;
  recipientCodeSnapshot: string;
  rateNumerator: number;
  rateDenominator: number;
  sequence: number;
};

export type ProfitSharingWaterfallSettlement = {
  id: string;
  organizationId: string;
  cropCycleId: string;
  assignmentId: string;
  sourceSchemeId: string;
  schemeFamilyId: string;
  code: string;
  settlementDate: string;
  schemeCodeSnapshot: string;
  schemeNameSnapshot: string;
  schemeDescriptionSnapshot: string | null;
  schemeVersionSnapshot: number;
  schemeAssignedAtSnapshot: string;
  residualMethod: ProfitSharingResidualMethod;
  residualRecipientCodeSnapshot: string | null;
  cropCycleCodeSnapshot: string;
  cropCycleNameSnapshot: string;
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
  outcome: ProfitabilityOutcome;
  confirmedInvestorCapital: number;
  confirmedPartnerCapital: number;
  totalConfirmedCapital: number;
  availableHarvestQuantity: number;
  totalCapital: number;
  totalCapitalRecovery: number;
  totalCapitalLoss: number;
  totalManagementProfitShare: number;
  totalReturnOnCapitalProfitShare: number;
  totalPriorityProfitShare: number;
  totalResidualProfitShare: number;
  totalProfitShare: number;
  totalPayout: number;
  calculationVersion: string;
  calculatedAt: string;
  notes: string | null;
  status: ProfitSharingWaterfallSettlementStatus;
  finalizedAt: string;
  voidedAt: string | null;
  voidReason: string | null;
  createdAt: string;
  updatedAt: string | null;
  priorityAllocations: ProfitSharingWaterfallPriorityAllocation[];
  participantAllocations: ProfitSharingWaterfallParticipantAllocation[];
  residualShares: ProfitSharingWaterfallResidualShare[];
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

export type SeasonReviewStatus = 1 | 2;

export type SeasonReview = {
  id: string;
  organizationId: string;
  cropCycleId: string;
  reviewDate: string;
  findings: string;
  lessonsLearned: string;
  nextSeasonRecommendations: string;
  status: SeasonReviewStatus;
  finalizedAt: string | null;
  createdAt: string;
  updatedAt: string | null;
};

export type CreateSeasonReviewRequest = {
  cropCycleId: string;
  reviewDate: string;
  findings: string;
  lessonsLearned: string;
  nextSeasonRecommendations: string;
};

export type UpdateSeasonReviewRequest = Omit<CreateSeasonReviewRequest, "cropCycleId">;
