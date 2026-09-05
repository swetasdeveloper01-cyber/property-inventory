using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PropertyInventory.Infrastructure.Persistence;

namespace PropertyInventory.IntegrationTests;

public class PropertyInventoryApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"PropertyInventoryApiTests-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IDbContextOptionsConfiguration<PropertyInventoryDbContext>>();
            services.RemoveAll<DbContextOptions<PropertyInventoryDbContext>>();
            services.RemoveAll<PropertyInventoryDbContext>();

            services.AddDbContext<PropertyInventoryDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));
        });
    }
}
