using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eShop.Catalog.API.Infrastructure.EntityConfigurations;

public class SellerEntityTypeConfiguration : IEntityTypeConfiguration<eShop.Catalog.API.Model.Seller.Seller>
{
    public void Configure(EntityTypeBuilder<eShop.Catalog.API.Model.Seller.Seller> builder)
    {
        builder.ToTable("Sellers");

        builder.HasKey(s => s.SellerId);

        builder.Property(s => s.Name)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(s => s.Email)
            .HasMaxLength(255)
            .IsRequired();

        builder.HasIndex(s => s.Email)
            .IsUnique();

        builder.Property(s => s.Description)
            .HasMaxLength(1000);

        builder.Property(s => s.PhoneNumber)
            .HasMaxLength(20);

        builder.Property(s => s.BankAccountInfo)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(s => s.CommissionRate)
            .HasColumnType("numeric(5,4)")
            .IsRequired();

        builder.Property(s => s.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(s => s.CreatedAt)
            .IsRequired();

        builder.Property(s => s.UpdatedAt)
            .IsRequired();

        builder.HasIndex(s => s.Status);
        builder.HasIndex(s => new { s.Status, s.CreatedAt });
    }
}
