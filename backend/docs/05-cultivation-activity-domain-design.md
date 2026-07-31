# Cultivation Activity and Resource Usage Domain Design

## 1. Document Purpose

This document defines the domain baseline for Sprint 16 of SiPacul.

The Cultivation Activity module records the work performed during one
crop cycle, including:

- planned cultivation work;
- actual execution dates;
- comparison with cultivation SOP steps;
- material usage;
- labor usage;
- equipment usage;
- external services;
- actual operational cost;
- execution outcomes;
- problems and deviations encountered in the field.

The module connects Crop Cycle management from Sprint 15 with:

- harvest and sales in Sprint 17;
- financial calculation and profit sharing in Sprint 18;
- land history and evaluation in Sprint 19.

The design remains organization-scoped and suitable for a future
multi-tenant SaaS deployment.

## 2. Domain Language

The code-level aggregate name is:

```text
CultivationActivity
```

The user-facing Indonesian term is:

```text
Aktivitas Budidaya
```

A cultivation activity represents one planned or actual piece of work
performed for one crop cycle.

Examples:

- preparing a land plot before planting;
- sowing or transplanting seed;
- applying fertilizer;
- irrigating crops;
- controlling pests and diseases;
- weeding;
- pruning;
- monitoring crop conditions;
- using a tractor or water pump;
- paying workers for one activity.

## 3. Aggregate Boundary

### 3.1 Aggregate root

`CultivationActivity` is an aggregate root.

The aggregate owns:

- its activity identity;
- lifecycle status;
- planned and actual execution dates;
- optional SOP-step snapshot;
- execution result;
- SOP-compliance result;
- cost and resource lines.

### 3.2 Child entity

`CultivationActivityResource` is a child entity owned by the activity.

It records one material, labor, equipment, service, or other resource
line.

All child mutations must occur through `CultivationActivity`.

### 3.3 External references

The activity references existing organization-scoped records:

- `Organization`;
- `CropCycle`;
- optional `CultivationSop`;
- optional `CultivationSopStep`.

The referenced records remain separate aggregates.

### 3.4 Future references

The first MVP does not require master tables for:

- inventory items;
- fertilizers;
- pesticides;
- seed lots;
- workers;
- suppliers;
- equipment assets;
- service vendors.

Resource lines use descriptive snapshots first.

Optional master-data identifiers may be introduced later without
changing the historical meaning of existing lines.

## 4. Activity Type

The first version uses `CultivationActivityType`.

Recommended values:

```text
LandPreparation
SeedPreparation
Planting
Irrigation
Fertilization
Weeding
PestDiseaseControl
CropMaintenance
Monitoring
Other
```

### 4.1 Land preparation

Examples:

- clearing vegetation;
- plowing;
- harrowing;
- bed formation;
- drainage preparation.

### 4.2 Seed preparation

Examples:

- seed selection;
- seed treatment;
- nursery work;
- germination preparation.

### 4.3 Planting

Examples:

- direct seeding;
- transplanting;
- spacing and planting-line setup.

### 4.4 Irrigation

Examples:

- scheduled watering;
- irrigation-channel operation;
- pump operation.

### 4.5 Fertilization

Examples:

- base fertilizer;
- first follow-up fertilizer;
- foliar fertilizer;
- organic amendment.

### 4.6 Weeding

Examples:

- manual weeding;
- mechanical weeding;
- herbicide application.

### 4.7 Pest and disease control

Examples:

- field inspection;
- biological control;
- pesticide application;
- sanitation work.

### 4.8 Crop maintenance

Examples:

- pruning;
- staking;
- mulching;
- thinning;
- replanting.

### 4.9 Monitoring

Examples:

- growth observation;
- pest population observation;
- soil moisture inspection;
- crop-health recording.

### 4.10 Other

Used only when no existing activity type is appropriate.

A later version may add organization-configurable activity types.

## 5. Activity Status

The first version uses `CultivationActivityStatus`.

```text
Planned
InProgress
Completed
Cancelled
```

### 5.1 Planned

The activity has been registered but execution has not started.

The plan and resource estimates may still be edited.

### 5.2 InProgress

Execution has started.

Actual resource usage may still be added or corrected.

### 5.3 Completed

Execution has ended.

The record becomes historical and immutable in the MVP.

### 5.4 Cancelled

The activity will not be performed or was stopped.

A cancellation reason is mandatory.

The record remains available for audit and evaluation.

## 6. SOP Compliance Status

The first version uses `SopComplianceStatus`.

```text
NotApplicable
NotEvaluated
Compliant
Deviated
```

### 6.1 Not applicable

Used when the activity is not linked to an SOP step.

### 6.2 Not evaluated

Used when an SOP-linked activity has not yet been evaluated.

### 6.3 Compliant

The activity was completed in accordance with the linked SOP step.

### 6.4 Deviated

The activity differed from the linked SOP step.

A deviation reason is required when this value is selected.

This status is an operational observation, not the final agronomic
evaluation. Sprint 19 will combine it with costs, outcomes, harvest
results, and recurring problems.

## 7. Resource Type

The first version uses `CultivationResourceType`.

```text
Material
Labor
Equipment
Service
Other
```

### 7.1 Material

Examples:

- seed;
- fertilizer;
- pesticide;
- mulch;
- fuel;
- irrigation supplies.

### 7.2 Labor

Examples:

- land-preparation workers;
- planting workers;
- fertilizer-application workers;
- maintenance workers.

### 7.3 Equipment

Examples:

- tractor usage;
- water-pump usage;
- sprayer usage;
- cultivator usage.

### 7.4 Service

Examples:

- hired land preparation;
- external spraying service;
- laboratory service;
- transport service directly related to the activity.

### 7.5 Other

Used only when the cost or usage does not fit another category.

## 8. Cultivation Activity Properties

The aggregate should contain:

| Property | Type | Rule |
|---|---|---|
| `Id` | `Guid` | Generated by the domain |
| `OrganizationId` | `Guid` | Required and immutable |
| `CropCycleId` | `Guid` | Required and immutable |
| `Code` | `string` | Required, normalized uppercase, immutable |
| `Name` | `string` | Required |
| `ActivityType` | `CultivationActivityType` | Required |
| `CultivationSopId` | `Guid?` | Optional snapshot reference |
| `CultivationSopStepId` | `Guid?` | Optional snapshot reference |
| `SopStepSequenceSnapshot` | `int?` | Required when linked |
| `SopStepNameSnapshot` | `string?` | Required when linked |
| `SopPlannedDayOffsetSnapshot` | `int?` | Required when linked |
| `SopEstimatedDurationDaysSnapshot` | `int?` | Required when linked |
| `SopIsRequiredSnapshot` | `bool?` | Required when linked |
| `PlannedDate` | `DateOnly` | Required |
| `ActualStartDate` | `DateOnly?` | Set when execution starts |
| `ActualCompletionDate` | `DateOnly?` | Set when execution completes |
| `Status` | `CultivationActivityStatus` | Controlled by lifecycle methods |
| `SopComplianceStatus` | `SopComplianceStatus` | Controlled by lifecycle methods |
| `Outcome` | `string?` | Actual execution result |
| `IssueNotes` | `string?` | Problems encountered |
| `DeviationReason` | `string?` | Required when SOP status is Deviated |
| `CancellationReason` | `string?` | Required when cancelled |
| `Notes` | `string?` | General notes |
| `Resources` | read-only collection | Child resource lines |
| `TotalActualCost` | `decimal` | Sum of child-line totals |
| audit properties | inherited | Follow existing aggregate pattern |

## 9. Code Rules

Activity codes follow the existing organization-scoped convention:

- trim whitespace;
- convert to uppercase;
- allow letters, numbers, hyphens, and underscores;
- enforce a documented maximum length;
- immutable after creation.

The code is unique within one crop cycle, not globally within the
organization.

Example codes:

```text
ACT-001
OLAH-LAHAN-01
TANAM-01
PUPUK-01
PHT-03
```

The same code may be reused in another crop cycle.

Recommended database uniqueness:

```text
(OrganizationId, CropCycleId, Code)
```

## 10. SOP Snapshot Rules

### 10.1 Why snapshots are required

Cultivation SOPs may change over time.

Historical activity evaluation must continue to show the SOP version
that was relevant when the activity was created.

The activity therefore stores a snapshot of:

- SOP identifier;
- SOP-step identifier;
- step sequence;
- step name;
- planned day offset;
- estimated duration;
- required or optional status.

### 10.2 Linked activity

When an SOP step is supplied, the Application layer must verify that:

- the SOP belongs to the same organization;
- the SOP belongs to the Crop Cycle commodity;
- the SOP step belongs to the selected SOP;
- the SOP and step exist;
- the SOP is active for a newly created activity.

### 10.3 Unlinked activity

An activity may be created without an SOP step.

This supports:

- emergency work;
- corrective work;
- activities before a formal SOP exists;
- locally required work not yet included in the SOP.

For an unlinked activity:

```text
SopComplianceStatus = NotApplicable
```

### 10.4 Historical protection

An SOP step referenced by any cultivation activity must not be
physically removed.

The current SOP-step remove operation must gain an Application-layer
protection check before it reaches the SOP aggregate.

Existing snapshots remain readable even when the SOP is later inactive.

## 11. Activity Lifecycle

Allowed transitions:

```text
Planned -> InProgress
Planned -> Cancelled
InProgress -> Completed
InProgress -> Cancelled
```

Disallowed transitions include:

```text
Completed -> any other status
Cancelled -> any other status
InProgress -> Planned
```

No reopen operation is included in the MVP.

## 12. Domain Methods

### 12.1 `Create`

Creates an activity in `Planned` status.

Validation includes:

- organization identifier is not empty;
- Crop Cycle identifier is not empty;
- code is valid;
- name is not empty;
- activity type is supported;
- planned date is provided;
- SOP snapshot fields are either all supplied or all absent;
- optional text values are normalized.

### 12.2 `UpdatePlan`

Allowed only while `Planned`.

Editable values:

- name;
- activity type;
- planned date;
- notes.

The linked Crop Cycle and activity code remain immutable.

Changing the linked SOP step is not included in the first MVP.

A user who selected the wrong SOP step should cancel the activity and
create a corrected record, preserving audit history.

### 12.3 `Start`

Allowed only from `Planned`.

Requires:

- actual start date.

Effects:

- sets `ActualStartDate`;
- changes status to `InProgress`;
- updates the audit timestamp.

### 12.4 `Complete`

Allowed only from `InProgress`.

Requires:

- actual completion date;
- completion date must not be before actual start date;
- execution outcome may be supplied;
- issue notes may be supplied;
- SOP compliance status may be supplied;
- deviation reason is required for `Deviated`.

Effects:

- stores execution results;
- changes status to `Completed`;
- updates the audit timestamp.

### 12.5 `Cancel`

Allowed from `Planned` or `InProgress`.

Requires:

- a non-empty cancellation reason.

Effects:

- stores the reason;
- changes status to `Cancelled`;
- updates the audit timestamp.

### 12.6 `UpdateExecutionNotes`

Allowed while `Planned` or `InProgress`.

Editable values:

- notes;
- issue notes.

Terminal activities remain immutable.

### 12.7 `AddResource`

Allowed while `Planned` or `InProgress`.

Creates one resource line owned by the activity.

### 12.8 `UpdateResource`

Allowed while `Planned` or `InProgress`.

The resource category may remain immutable in the first implementation.

Editable values:

- description;
- quantity;
- unit;
- unit cost;
- notes.

### 12.9 `RemoveResource`

Allowed while `Planned` or `InProgress`.

A completed or cancelled activity cannot lose historical cost lines.

## 13. Resource-Line Properties

`CultivationActivityResource` should contain:

| Property | Type | Rule |
|---|---|---|
| `Id` | `Guid` | Generated by the domain |
| `OrganizationId` | `Guid` | Same as activity |
| `CultivationActivityId` | `Guid` | Same as parent |
| `ResourceType` | `CultivationResourceType` | Required |
| `Description` | `string` | Required |
| `Quantity` | `decimal` | Greater than zero |
| `Unit` | `string` | Required |
| `UnitCost` | `decimal` | Zero or greater |
| `TotalCost` | `decimal` | Quantity multiplied by unit cost |
| `Notes` | `string?` | Optional |
| `CreatedAt` | `DateTime` | UTC |
| `UpdatedAt` | `DateTime?` | UTC |

Examples:

| Type | Description | Quantity | Unit | Unit cost |
|---|---|---:|---|---:|
| Material | Urea fertilizer | 100 | kg | 4,500 |
| Labor | Planting worker | 12 | person-day | 120,000 |
| Equipment | Tractor | 5 | hour | 200,000 |
| Service | Soil laboratory test | 1 | service | 350,000 |

## 14. Cost Rules

### 14.1 Quantity

Quantity must be greater than zero.

### 14.2 Unit cost

Unit cost may be zero.

Zero cost supports:

- family labor;
- owned equipment with cost tracked later through depreciation;
- donated material;
- non-cash usage.

### 14.3 Total cost

The line total is:

```text
TotalCost = Quantity Ã— UnitCost
```

The aggregate total is:

```text
TotalActualCost = Sum(Resource.TotalCost)
```

Recommended monetary rounding:

- round line totals to two decimal places;
- use `MidpointRounding.AwayFromZero`;
- store monetary values with precision `18,2`.

Recommended quantity precision:

```text
18,4
```

### 14.4 Currency

The MVP assumes the organization's accounting currency.

A multi-currency model is deferred.

For the initial Indonesian deployment, the operational currency will
normally be IDR.

## 15. Crop Cycle Reference Rules

The Application layer must verify the selected Crop Cycle.

The Crop Cycle must:

- belong to the same organization;
- exist;
- not be deleted;
- have status `Planned` or `InProgress` when creating an activity.

New activities cannot be created for:

- `Completed` Crop Cycles;
- `Cancelled` Crop Cycles.

Existing activities remain historical when the Crop Cycle later becomes
terminal.

## 16. Crop Cycle and Activity Lifecycle Interaction

### 16.1 Pre-start work

An activity may be executed while the Crop Cycle is still `Planned`.

This supports work that occurs before planting, including:

- land preparation;
- nursery preparation;
- seed treatment;
- irrigation preparation.

Starting an activity does not automatically start the Crop Cycle.

### 16.2 Crop Cycle completion

A Crop Cycle must not be completed while any activity is
`InProgress`.

Planned activities may remain as historical unexecuted plans.

They become immutable when the Crop Cycle becomes terminal.

### 16.3 Crop Cycle cancellation

A Crop Cycle must not be cancelled while any activity is
`InProgress`.

The user must complete or cancel the in-progress activity first.

Completed, cancelled, and unexecuted planned activities remain attached
to the cancelled Crop Cycle for cost and evaluation history.

### 16.4 Terminal parent

When the Crop Cycle is `Completed` or `Cancelled`:

- no new activity may be created;
- no existing activity may be updated;
- no resource line may be added, updated, or removed;
- historical records remain readable.

## 17. Planned-Date Rules

The planned date is stored independently from the Crop Cycle start date.

This is required because SOP steps may use negative day offsets before
planting.

When linked to an SOP step, the Application layer can calculate a
recommended date:

```text
RecommendedDate =
    CropCycle.PlannedStartDate
    + SopPlannedDayOffsetSnapshot
```

The user may choose a different planned date.

The difference can later be evaluated as a deviation.

The first MVP should reject a planned date after the Crop Cycle expected
harvest date.

A lower-bound restriction is intentionally not introduced because
pre-plant work may occur before the Crop Cycle planned start date.

## 18. Execution-Date Rules

- actual start date is required to start;
- actual completion date is required to complete;
- actual completion date cannot be before actual start date;
- actual dates may differ from planned dates;
- late work is allowed and preserved for evaluation;
- the system must not silently rewrite actual dates.

## 19. Outcome and Problem Recording

A completed activity may store:

- `Outcome`;
- `IssueNotes`;
- `SopComplianceStatus`;
- `DeviationReason`.

Examples of outcomes:

- land beds prepared;
- 2,500 seedlings transplanted;
- fertilizer applied evenly;
- pest population decreased;
- irrigation coverage incomplete.

Examples of issue notes:

- pump failure;
- delayed fertilizer delivery;
- rain interrupted field work;
- labor shortage;
- pest pressure above threshold.

These fields become important inputs for Sprint 19 evaluation.

## 20. Evidence Attachments

File and photo attachments are deferred.

The MVP may store descriptive notes only.

A later attachment module may reference:

```text
CultivationActivityId
```

Examples:

- before-and-after photos;
- receipts;
- field worksheets;
- laboratory results;
- equipment-hour records.

## 21. Persistence Model

The planned tables are:

```text
CultivationActivities
CultivationActivityResources
```

### 21.1 Cultivation Activities

Required database rules:

- primary key on `Id`;
- alternate key on `(OrganizationId, Id)`;
- unique index on `(OrganizationId, CropCycleId, Code)`;
- index on `(OrganizationId, CropCycleId, Status)`;
- index on `(OrganizationId, PlannedDate)`;
- index on `(OrganizationId, ActivityType)`;
- optional index on `(OrganizationId, CultivationSopStepId)`;
- restrictive foreign key to `Organizations`;
- restrictive organization-scoped foreign key to `CropCycles`;
- restrictive foreign key to the optional SOP;
- restrictive foreign key to the optional SOP step when supported;
- PostgreSQL `date` columns for planned and actual dates.

### 21.2 Cultivation Activity Resources

Required database rules:

- primary key on `Id`;
- index on `(OrganizationId, CultivationActivityId)`;
- index on `(OrganizationId, ResourceType)`;
- cascade delete from an activity only at database-structure level;
- no public activity-delete operation in the MVP;
- decimal precision `18,4` for quantity;
- decimal precision `18,2` for unit cost and total cost.

The aggregate should remain the only mutation boundary.

## 22. Repository Operations

Recommended `ICultivationActivityRepository` operations:

```text
GetAllAsync
GetByIdAsync
GetByIdForUpdateAsync
CodeExistsAsync
HasInProgressActivitiesAsync
HasAnyActivityForSopStepAsync
Add
```

Recommended list filters:

```text
status
activityType
plannedFrom
plannedTo
cultivationSopStepId
```

All methods must be organization-scoped.

## 23. Application Operations

The first Application service should provide:

```text
CreateAsync
GetAllAsync
GetByIdAsync
UpdatePlanAsync
StartAsync
CompleteAsync
CancelAsync
UpdateExecutionNotesAsync
AddResourceAsync
UpdateResourceAsync
RemoveResourceAsync
```

The service must validate:

- organization;
- Crop Cycle;
- Crop Cycle lifecycle;
- optional SOP and SOP step;
- SOP commodity consistency;
- activity-code uniqueness;
- planned-date rule;
- activity lifecycle;
- resource-line values;
- Crop Cycle transition protection;
- SOP-step historical protection.

## 24. Crop Cycle Service Protection

After the Activity repository exists, `CropCycleService` must gain
protection checks.

### 24.1 Complete protection

`CompleteAsync` must return a conflict when the Crop Cycle has any
`InProgress` activity.

### 24.2 Cancel protection

`CancelAsync` must return a conflict when the Crop Cycle has any
`InProgress` activity.

### 24.3 Historical retention

Completing or cancelling the Crop Cycle must not delete its activities
or resource lines.

## 25. SOP Service Protection

After the Activity repository exists, the SOP-step remove operation must
check historical references.

An SOP step referenced by any activity must not be removed.

Recommended error:

```text
CultivationActivities.SopStepHistoricalReferenceExists
```

Updating the SOP-step text remains allowed because the activity stores a
historical snapshot.

## 26. HTTP API Baseline

Proposed routes:

```text
POST   /api/v1/organizations/{organizationId}/crop-cycles/{cropCycleId}/activities
GET    /api/v1/organizations/{organizationId}/crop-cycles/{cropCycleId}/activities
GET    /api/v1/organizations/{organizationId}/crop-cycles/{cropCycleId}/activities/{activityId}
PUT    /api/v1/organizations/{organizationId}/crop-cycles/{cropCycleId}/activities/{activityId}
PATCH  /api/v1/organizations/{organizationId}/crop-cycles/{cropCycleId}/activities/{activityId}/start
PATCH  /api/v1/organizations/{organizationId}/crop-cycles/{cropCycleId}/activities/{activityId}/complete
PATCH  /api/v1/organizations/{organizationId}/crop-cycles/{cropCycleId}/activities/{activityId}/cancel
PATCH  /api/v1/organizations/{organizationId}/crop-cycles/{cropCycleId}/activities/{activityId}/notes
POST   /api/v1/organizations/{organizationId}/crop-cycles/{cropCycleId}/activities/{activityId}/resources
PUT    /api/v1/organizations/{organizationId}/crop-cycles/{cropCycleId}/activities/{activityId}/resources/{resourceId}
DELETE /api/v1/organizations/{organizationId}/crop-cycles/{cropCycleId}/activities/{activityId}/resources/{resourceId}
```

The route design follows the existing Minimal API and `Result<T>`
patterns.

## 27. Error Baseline

Recommended Application error codes:

```text
CultivationActivities.Validation
CultivationActivities.OrganizationNotFound
CultivationActivities.CropCycleNotFound
CultivationActivities.CropCycleTerminal
CultivationActivities.NotFound
CultivationActivities.CodeAlreadyExists
CultivationActivities.SopNotFound
CultivationActivities.SopInactive
CultivationActivities.SopCommodityMismatch
CultivationActivities.SopStepNotFound
CultivationActivities.SopStepMismatch
CultivationActivities.PlannedDateOutOfRange
CultivationActivities.InvalidStatusTransition
CultivationActivities.ResourceNotFound
CultivationActivities.CropCycleHasInProgressActivities
CultivationActivities.SopStepHistoricalReferenceExists
```

Domain `ArgumentException` errors map to validation errors.

Lifecycle and historical-reference violations map to conflict errors.

Missing organization-scoped references map to not-found errors.

## 28. Explicit MVP Decisions

The following decisions are intentional:

1. One activity belongs to exactly one Crop Cycle.
2. One activity may optionally link to one SOP step.
3. SOP details are snapshotted for historical accuracy.
4. One activity owns multiple generic resource lines.
5. Generic lines cover material, labor, equipment, service, and other.
6. Master inventory and equipment references are deferred.
7. Activity codes are unique within one Crop Cycle.
8. Completed and cancelled activities are immutable.
9. Physical activity deletion is not included.
10. Resource lines cannot change after the activity is terminal.
11. Pre-start activities may occur while the Crop Cycle is Planned.
12. Crop Cycle completion or cancellation is blocked by InProgress activities.
13. Planned but unexecuted activities may remain on a terminal Crop Cycle.
14. Referenced SOP steps cannot be physically removed.
15. Attachment storage is deferred.
16. Harvest records remain in Sprint 17.
17. Profit-sharing calculations remain in Sprint 18.
18. Cross-season evaluation remains in Sprint 19.

## 29. Implementation Sequence

Sprint 16 should be delivered in the following order.

### Sprint 16A-1

- approve this domain design;
- commit the design baseline.

### Sprint 16A-2

- create activity, status, type, compliance, and resource enums;
- create the activity aggregate and resource child;
- add domain tests.

### Sprint 16A-3

- add EF Core configurations;
- add organization-scoped foreign keys;
- add migration;
- verify PostgreSQL constraints and indexes.

### Sprint 16A-4

- add repository and Application service;
- validate organization and Crop Cycle references;
- validate SOP snapshots;
- add Crop Cycle transition protection;
- add SOP-step historical protection;
- add Application tests.

### Sprint 16A-5

- add HTTP endpoints;
- add API tests;
- preserve existing `Result<T>` response conventions.

### Sprint 16A-6

- run full end-to-end testing with the real API and PostgreSQL;
- verify resource costs;
- verify lifecycle transitions;
- verify Crop Cycle protection;
- verify SOP-step protection;
- verify organization isolation;
- clean test data;
- confirm a clean Git working tree.

## 30. Acceptance Criteria

Sprint 16 is complete when:

- a valid planned activity can be created for a Crop Cycle;
- an activity may be linked to an appropriate SOP step;
- SOP details remain available through snapshots;
- invalid cross-organization references are rejected;
- activity codes are unique within one Crop Cycle;
- lifecycle transitions follow the defined state machine;
- material, labor, equipment, service, and other lines can be recorded;
- quantity and cost calculations are correct;
- total actual cost is calculated correctly;
- terminal activities cannot be edited;
- a Crop Cycle with an InProgress activity cannot complete or cancel;
- a referenced SOP step cannot be removed;
- database constraints and indexes are verified;
- automated tests pass;
- end-to-end tests pass;
- test data is cleaned up;
- repository remains clean after verification.
