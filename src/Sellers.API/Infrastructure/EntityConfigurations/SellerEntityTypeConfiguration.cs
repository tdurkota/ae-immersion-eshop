namespace eShop.Sellers.API.Infrastructure.EntityConfigurations;

internal sealed class SellerEntityTypeConfiguration : IEntityTypeConfiguration<Seller>
{
    public void Configure(EntityTypeBuilder<Seller> builder)
    {
        builder.ToTable("sellers", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint("CK_sellers_commission_rate_range", "commission_rate >= 0 AND commission_rate <= 1");
            tableBuilder.HasCheckConstraint("CK_sellers_status_valid", "status IN ('Active', 'Inactive', 'Suspended')");
        });

        builder.HasKey(seller => seller.SellerId);

        builder.Property(seller => seller.SellerId)
            .HasColumnName("seller_id")
            .ValueGeneratedNever();

        builder.Property(seller => seller.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(seller => seller.Email)
            .HasColumnName("email")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(seller => seller.Description)
            .HasColumnName("description")
            .HasMaxLength(2000);

        builder.Property(seller => seller.PhoneNumber)
            .HasColumnName("phone_number")
            .HasMaxLength(32);

        builder.Property(seller => seller.BankAccountInfo)
            .HasColumnName("bank_account_info")
            .HasMaxLength(512);

        builder.Property(seller => seller.CommissionRate)
            .HasColumnName("commission_rate")
            .HasPrecision(5, 4)
            .IsRequired();

        builder.Property(seller => seller.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(seller => seller.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(seller => seller.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.HasIndex(seller => seller.Email)
            .IsUnique();

        builder.HasIndex(seller => seller.Status);
    }
}
