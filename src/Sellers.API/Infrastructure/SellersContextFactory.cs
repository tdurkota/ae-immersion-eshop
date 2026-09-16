namespace eShop.Sellers.API.Infrastructure;

internal sealed class SellersContextFactory : IDesignTimeDbContextFactory<SellersContext>
{
    public SellersContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SellersContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Database=eshop;Username=postgres;Password=postgres");

        return new SellersContext(optionsBuilder.Options);
    }
}
