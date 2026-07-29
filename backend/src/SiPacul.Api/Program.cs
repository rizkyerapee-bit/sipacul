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

app.Run();

public partial class Program;
