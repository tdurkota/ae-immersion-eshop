namespace eShop.Catalog.API.Infrastructure.EntityConfigurations;

class SellerEntityTypeConfiguration
    : IEntityTypeConfiguration<Seller>
{
    public void Configure(EntityTypeBuilder<Seller> builder)
    {
        builder.ToTable("Sellers");

        builder.HasKey(s => s.SellerId);

        builder.Property(s => s.SellerId)
            .HasColumnName("seller_id");

        builder.Property(s => s.Name)
            .HasMaxLength(255)
            .IsRequired();

        builder.HasMany(s => s.Products)
            .WithOne(ci => ci.Seller)
            .HasForeignKey(ci => ci.SellerId);
    }
}
