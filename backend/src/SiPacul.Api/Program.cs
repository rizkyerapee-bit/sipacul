using SiPacul.Api.Endpoints.Bootstrap;
using SiPacul.Api.Endpoints.Authentication;
using SiPacul.Api.Security;
using SiPacul.Api.Common.Http;
using SiPacul.Api.Endpoints.Finance.ProfitSharing;
using SiPacul.Api.Endpoints.Finance.ProfitSharing.Assignments;
using SiPacul.Api.Endpoints.Finance.ProfitSharing.Previews;
using SiPacul.Api.Endpoints.Finance.ProfitSharing.Schemes;
using SiPacul.Api.Endpoints.Finance.Profitability;
using SiPacul.Api.Endpoints.Finance.SalePayments;
using SiPacul.Api.Endpoints.Finance.CapitalContributions;
using SiPacul.Api.Endpoints.Finance.Expenses;
using SiPacul.Api.Endpoints.Sales;
using SiPacul.Api.Endpoints.Harvests;
using SiPacul.Api.Endpoints.Cultivation.Activities;
using SiPacul.Api.Endpoints.Cultivation.CropCycles;
using SiPacul.Api.Endpoints.Cultivation.Sops;
using SiPacul.Api.Endpoints.Lands;
using SiPacul.Api.Endpoints.MasterData.Commodities;
using SiPacul.Api.Endpoints.MasterData.CommodityCategories;
using SiPacul.Api.Endpoints.Organizations;
using SiPacul.Api.Endpoints.Organizations.Members;
using SiPacul.Application;
using SiPacul.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddApplication();

builder.Services.AddInfrastructure(
    builder.Configuration);

builder.Services.AddSiPaculAuthentication();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapBootstrapEndpoints();

app.MapAuthenticationEndpoints();
app.MapOrganizationEndpoints();
app.MapOrganizationMemberEndpoints();
app.MapCommodityCategoryEndpoints();
app.MapCommodityEndpoints();
app.MapCultivationSopEndpoints();
app.MapLandEndpoints();
app.MapCropCycleEndpoints();
app.MapCultivationActivityEndpoints();
app.MapHarvestBatchEndpoints();
app.MapSaleEndpoints();
app.MapCultivationExpenseEndpoints();
app.MapCapitalContributionEndpoints();
app.MapSalePaymentEndpoints();

app.MapProfitabilityEndpoints();

app.MapProfitSharingSettlementEndpoints();
app.MapProfitSharingSchemeEndpoints();
app.MapProfitSharingSchemeAssignmentEndpoints();
app.MapProfitSharingPreviewEndpoints();
app.UseMiddleware<ProfitSharingSourceLockMiddleware>();

app.Run();

public partial class Program;
