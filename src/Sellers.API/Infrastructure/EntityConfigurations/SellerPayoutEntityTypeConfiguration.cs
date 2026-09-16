namespace eShop.Sellers.API.Infrastructure.EntityConfigurations;

internal sealed class SellerPayoutEntityTypeConfiguration : IEntityTypeConfiguration<SellerPayout>
{
    public void Configure(EntityTypeBuilder<SellerPayout> builder)
    {
        builder.ToTable("seller_payouts", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint("CK_seller_payouts_amounts_non_negative", "gross_amount >= 0 AND commission_amount >= 0 AND seller_amount >= 0");
            tableBuilder.HasCheckConstraint("CK_seller_payouts_amounts_reconcile", "gross_amount = commission_amount + seller_amount");
            tableBuilder.HasCheckConstraint("CK_seller_payouts_status_valid", "status IN ('Pending', 'Processed', 'Paid')");
        });

        builder.HasKey(payout => payout.PayoutId);

        builder.Property(payout => payout.PayoutId)
            .HasColumnName("payout_id")
            .ValueGeneratedNever();

        builder.Property(payout => payout.SellerId)
            .HasColumnName("seller_id")
            .IsRequired();

        builder.Property(payout => payout.OrderId)
            .HasColumnName("order_id")
            .IsRequired();

        builder.Property(payout => payout.OrderLineItemId)
            .HasColumnName("order_line_item_id")
            .IsRequired();

        builder.Property(payout => payout.GrossAmount)
            .HasColumnName("gross_amount")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(payout => payout.CommissionAmount)
            .HasColumnName("commission_amount")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(payout => payout.SellerAmount)
            .HasColumnName("seller_amount")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(payout => payout.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(payout => payout.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(payout => payout.PaidAt)
            .HasColumnName("paid_at");

        builder.HasOne(payout => payout.Seller)
            .WithMany(seller => seller.Payouts)
            .HasForeignKey(payout => payout.SellerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(payout => new { payout.SellerId, payout.CreatedAt });
        builder.HasIndex(payout => new { payout.OrderId, payout.OrderLineItemId })
            .IsUnique();
    }
}
