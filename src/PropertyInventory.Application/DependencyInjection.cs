using Microsoft.Extensions.DependencyInjection;
using PropertyInventory.Application.Contacts;
using PropertyInventory.Application.Properties;

namespace PropertyInventory.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<PropertyService>();
        services.AddScoped<ContactService>();
        return services;
    }
}
