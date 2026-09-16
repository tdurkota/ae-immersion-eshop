namespace eShop.Sellers.API;

using eShop.Sellers.API.Infrastructure;

internal static class Extensions
{
    public static void AddApplicationServices(this IHostApplicationBuilder builder)
    {
        builder.AddNpgsqlDbContext<SellersContext>("eshopdb");
        builder.Services.AddMigration<SellersContext>();
        builder.Services.AddScoped<ISellerPayoutRepository, SellerPayoutRepository>();
    }
}
