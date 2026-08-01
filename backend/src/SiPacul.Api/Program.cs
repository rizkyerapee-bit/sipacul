using SiPacul.Api.Endpoints.Sales;
using SiPacul.Api.Endpoints.Harvests;
using SiPacul.Api.Endpoints.Cultivation.Activities;
using SiPacul.Api.Endpoints.Cultivation.CropCycles;
using SiPacul.Api.Endpoints.Cultivation.Sops;
using SiPacul.Api.Endpoints.Lands;
using SiPacul.Api.Endpoints.MasterData.Commodities;
using SiPacul.Api.Endpoints.MasterData.CommodityCategories;
using SiPacul.Api.Endpoints.Organizations;
using SiPacul.Application;
using SiPacul.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddApplication();

builder.Services.AddInfrastructure(
    builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapOrganizationEndpoints();
app.MapCommodityCategoryEndpoints();
app.MapCommodityEndpoints();
app.MapCultivationSopEndpoints();
app.MapLandEndpoints();
app.MapCropCycleEndpoints();
app.MapCultivationActivityEndpoints();
app.MapHarvestBatchEndpoints();
app.MapSaleEndpoints();

app.Run();

public partial class Program;
