namespace eShop.Catalog.API.Infrastructure.EntityConfigurations;

class CatalogItemEntityTypeConfiguration
    : IEntityTypeConfiguration<CatalogItem>
{
    public void Configure(EntityTypeBuilder<CatalogItem> builder)
    {
        builder.ToTable("Catalog");

        builder.Property(ci => ci.Name)
            .HasMaxLength(50);

        builder.Property(ci => ci.Embedding)
            .HasColumnType("vector(384)");

        builder.Property(ci => ci.SellerId)
            .HasColumnName("seller_id");

        builder.HasOne(ci => ci.CatalogBrand)
            .WithMany();

        builder.HasOne(ci => ci.CatalogType)
            .WithMany();

        builder.HasOne(ci => ci.Seller)
            .WithMany(s => s.Products)
            .HasForeignKey(ci => ci.SellerId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(ci => ci.Name);
        builder.HasIndex(ci => ci.SellerId);
    }
}
