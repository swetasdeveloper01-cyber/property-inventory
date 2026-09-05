using Microsoft.EntityFrameworkCore;
using PropertyInventory.Application.Common.Interfaces;
using PropertyInventory.Domain.Entities;

namespace PropertyInventory.Infrastructure.Persistence;

public class PropertyInventoryDbContext : DbContext, IApplicationDbContext
{
    public PropertyInventoryDbContext(DbContextOptions<PropertyInventoryDbContext> options)
        : base(options)
    {
    }

    public DbSet<Property> Properties => Set<Property>();

    public DbSet<Contact> Contacts => Set<Contact>();

    public DbSet<PropertyOwnership> PropertyOwnerships => Set<PropertyOwnership>();

    public DbSet<PropertyPriceHistory> PropertyPriceHistories => Set<PropertyPriceHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PropertyInventoryDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
