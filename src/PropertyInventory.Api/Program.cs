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
    // OpenAPI document at /openapi/v1.json; Swagger UI at /swagger consumes that document.
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Property Inventory API v1");
    });

    // Restricted Angular Dev CORS before endpoints; no HTTPS redirect in Development
    // so http://localhost:4200 can call http://localhost:5248 without a 307 hop.
    app.UseCors("AngularDev");
    await DatabaseSeeder.MigrateAndSeedAsync(app.Services);
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.MapControllers();

app.Run();

/// <summary>
/// Exposed for integration-test host creation via WebApplicationFactory.
/// </summary>
public partial class Program;
