using PropertyInventory.Api.Middleware;
using PropertyInventory.Application;
using PropertyInventory.Infrastructure;
using PropertyInventory.Infrastructure.Persistence.Seed;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularDev", policy =>
        policy.WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseCors("AngularDev");
    await DatabaseSeeder.MigrateAndSeedAsync(app.Services);
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();

/// <summary>
/// Exposed for integration-test host creation via WebApplicationFactory.
/// </summary>
public partial class Program;
