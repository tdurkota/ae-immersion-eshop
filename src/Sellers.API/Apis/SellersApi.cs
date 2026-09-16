namespace eShop.Sellers.API;

public static class SellersApi
{
    public static IEndpointRouteBuilder MapSellersApi(this IEndpointRouteBuilder app)
    {
        // RouteGroupBuilder for sellers endpoints
        var vApi = app.NewVersionedApi("Sellers");
        var api = vApi.MapGroup("api/sellers").HasApiVersion(1, 0);

        // Health check endpoint
        api.MapGet("", GetSellers)
            .WithName("GetSellers")
            .WithSummary("List sellers")
            .WithDescription("Get a list of all sellers")
            .WithTags("Sellers");

        return app;
    }

    private static async Task<IResult> GetSellers(SellersContext context)
    {
        var sellers = await context.Sellers.ToListAsync();
        return Results.Ok(sellers);
    }
}
