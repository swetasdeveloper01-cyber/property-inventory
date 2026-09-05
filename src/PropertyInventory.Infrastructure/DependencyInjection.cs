using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PropertyInventory.Application.Common.Interfaces;
using PropertyInventory.Infrastructure.Persistence;

namespace PropertyInventory.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers EF Core SQL Server persistence for the Property Inventory system.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<PropertyInventoryDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<PropertyInventoryDbContext>());

        return services;
    }
}
