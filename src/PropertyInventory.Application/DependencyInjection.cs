using Microsoft.Extensions.DependencyInjection;
using PropertyInventory.Application.Common.Interfaces;
using PropertyInventory.Application.Contacts;
using PropertyInventory.Application.ExchangeRates;
using PropertyInventory.Application.Ownerships;
using PropertyInventory.Application.Properties;

namespace PropertyInventory.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IExchangeRateService, ConfiguredExchangeRateService>();
        services.AddScoped<PropertyService>();
        services.AddScoped<ContactService>();
        services.AddScoped<OwnershipService>();
        return services;
    }
}
