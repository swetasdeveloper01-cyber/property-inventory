using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyInventory.Domain.Entities;

namespace PropertyInventory.Infrastructure.Persistence.Configurations;

public class PropertyPriceHistoryConfiguration : IEntityTypeConfiguration<PropertyPriceHistory>
{
    public void Configure(EntityTypeBuilder<PropertyPriceHistory> builder)
    {
        builder.ToTable("PropertyPriceHistories");

        builder.HasKey(history => history.Id);

        builder.Property(history => history.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(history => history.Currency)
            .HasMaxLength(3)
            .IsFixedLength()
            .IsRequired();

        builder.Property(history => history.EffectiveDate)
            .IsRequired();

        builder.HasIndex(history => new { history.PropertyId, history.EffectiveDate });
    }
}
