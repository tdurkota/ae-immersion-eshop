namespace eShop.Sellers.API.Infrastructure;

/// <remarks>
/// Add migrations using the following command inside the 'Sellers.API' project directory:
///
/// dotnet ef migrations add --context SellersContext [migration-name]
/// </remarks>
public class SellersContext(DbContextOptions<SellersContext> options) : DbContext(options)
{
    public DbSet<Seller> Sellers => Set<Seller>();
    public DbSet<SellerPayout> SellerPayouts => Set<SellerPayout>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfiguration(new SellerEntityTypeConfiguration());
        builder.ApplyConfiguration(new SellerPayoutEntityTypeConfiguration());
    }
}
