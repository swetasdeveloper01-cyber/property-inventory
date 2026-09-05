using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyInventory.Domain.Entities;

namespace PropertyInventory.Infrastructure.Persistence.Configurations;

public class PropertyOwnershipConfiguration : IEntityTypeConfiguration<PropertyOwnership>
{
    public void Configure(EntityTypeBuilder<PropertyOwnership> builder)
    {
        builder.ToTable("PropertyOwnerships", table =>
        {
            table.HasCheckConstraint(
                "CK_PropertyOwnerships_EffectiveRange",
                "[EffectiveTill] IS NULL OR [EffectiveTill] >= [EffectiveFrom]");
        });

        builder.HasKey(ownership => ownership.Id);

        builder.Property(ownership => ownership.EffectiveFrom)
            .IsRequired();

        builder.Property(ownership => ownership.AcquisitionPrice)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(ownership => ownership.AcquisitionCurrency)
            .HasMaxLength(3)
            .IsFixedLength()
            .IsRequired();

        builder.Property(ownership => ownership.AcquisitionPriceUsd)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.HasIndex(ownership => ownership.PropertyId);

        builder.HasIndex(ownership => ownership.ContactId);

        builder.HasIndex(ownership => new { ownership.PropertyId, ownership.EffectiveFrom });

        // At most one open (current) ownership period per property.
        builder.HasIndex(ownership => ownership.PropertyId)
            .IsUnique()
            .HasFilter("[EffectiveTill] IS NULL")
            .HasDatabaseName("IX_PropertyOwnerships_PropertyId_Current");

        builder.HasOne(ownership => ownership.Contact)
            .WithMany(contact => contact.Ownerships)
            .HasForeignKey(ownership => ownership.ContactId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
