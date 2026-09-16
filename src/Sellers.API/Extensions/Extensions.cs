namespace eShop.Sellers.API;

internal static class Extensions
{
    public static void AddApplicationServices(this IHostApplicationBuilder builder)
    {
        // Avoid loading full database config and migrations if startup
        // is being invoked from build-time OpenAPI generation
        if (builder.Environment.IsBuild())
        {
            builder.Services.AddDbContext<SellersContext>();
            return;
        }

        builder.AddNpgsqlDbContext<SellersContext>("sellersdb");
        builder.Services.AddMigration<SellersContext>();
    }
}
