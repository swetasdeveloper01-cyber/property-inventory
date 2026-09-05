using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyInventory.Domain.Entities;

namespace PropertyInventory.Infrastructure.Persistence.Configurations;

public class ContactConfiguration : IEntityTypeConfiguration<Contact>
{
    public void Configure(EntityTypeBuilder<Contact> builder)
    {
        builder.ToTable("Contacts");

        builder.HasKey(contact => contact.Id);

        builder.Property(contact => contact.FirstName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(contact => contact.LastName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(contact => contact.PhoneNumber)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(contact => contact.Email)
            .HasMaxLength(256)
            .IsRequired();

        builder.HasIndex(contact => contact.Email)
            .IsUnique();

        builder.HasIndex(contact => new { contact.LastName, contact.FirstName });
    }
}
