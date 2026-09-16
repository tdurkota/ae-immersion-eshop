namespace eShop.Sellers.API.Infrastructure;

internal sealed class SellersContextFactory : IDesignTimeDbContextFactory<SellersContext>
{
    public SellersContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SellersContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Database=sellers;Username=postgres;Password=postgres");

        return new SellersContext(optionsBuilder.Options);
    }
}
