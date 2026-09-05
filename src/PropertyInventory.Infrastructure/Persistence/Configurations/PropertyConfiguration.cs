using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyInventory.Domain.Entities;

namespace PropertyInventory.Infrastructure.Persistence.Configurations;

public class PropertyConfiguration : IEntityTypeConfiguration<Property>
{
    public void Configure(EntityTypeBuilder<Property> builder)
    {
        builder.ToTable("Properties");

        builder.HasKey(property => property.Id);

        builder.Property(property => property.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(property => property.Address)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(property => property.Price)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(property => property.Currency)
            .HasMaxLength(3)
            .IsFixedLength()
            .IsRequired();

        builder.Property(property => property.DateOfRegistration)
            .IsRequired();

        builder.HasIndex(property => property.Name);

        builder.HasMany(property => property.Ownerships)
            .WithOne(ownership => ownership.Property)
            .HasForeignKey(ownership => ownership.PropertyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(property => property.PriceHistory)
            .WithOne(history => history.Property)
            .HasForeignKey(history => history.PropertyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
