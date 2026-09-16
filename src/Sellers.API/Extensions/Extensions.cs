namespace eShop.Sellers.API;

internal static class Extensions
{
    public static void AddApplicationServices(this IHostApplicationBuilder builder)
    {
        builder.AddNpgsqlDbContext<SellersContext>("eshopdb");
        builder.Services.AddMigration<SellersContext>();
    }
}
