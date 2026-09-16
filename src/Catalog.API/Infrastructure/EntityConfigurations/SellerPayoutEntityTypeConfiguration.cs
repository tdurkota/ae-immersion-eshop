using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eShop.Catalog.API.Infrastructure.EntityConfigurations;

public class SellerPayoutEntityTypeConfiguration : IEntityTypeConfiguration<eShop.Catalog.API.Model.Seller.SellerPayout>
{
    public void Configure(EntityTypeBuilder<eShop.Catalog.API.Model.Seller.SellerPayout> builder)
    {
        builder.ToTable("SellerPayouts");

        builder.HasKey(sp => sp.PayoutId);

        builder.Property(sp => sp.SellerId)
            .IsRequired();

        builder.Property(sp => sp.OrderId)
            .IsRequired();

        builder.Property(sp => sp.OrderLineItemId)
            .IsRequired();

        builder.Property(sp => sp.GrossAmount)
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        builder.Property(sp => sp.CommissionAmount)
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        builder.Property(sp => sp.SellerAmount)
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        builder.Property(sp => sp.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(sp => sp.CreatedAt)
            .IsRequired();

        builder.Property(sp => sp.PaidAt);

        builder.HasIndex(sp => new { sp.SellerId, sp.CreatedAt });
        builder.HasIndex(sp => sp.Status);
        builder.HasIndex(sp => sp.OrderId);
    }
}
