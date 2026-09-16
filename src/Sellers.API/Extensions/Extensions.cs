namespace eShop.Sellers.API;

using eShop.Sellers.API.Application.IntegrationEvents.Events;
using eShop.Sellers.API.Application.IntegrationEvents.EventHandling;
using eShop.Sellers.API.Infrastructure.Repositories;

internal static class Extensions
{
    public static void AddApplicationServices(this IHostApplicationBuilder builder)
    {
        builder.AddNpgsqlDbContext<SellersContext>("eshopdb");
        builder.Services.AddMigration<SellersContext>();

        // Register repositories
        builder.Services.AddScoped<ISellerPayoutRepository, SellerPayoutRepository>();

        // Configure RabbitMQ event bus and register event subscriptions
        builder.AddRabbitMqEventBus("eventbus")
               .AddEventBusSubscriptions();
    }

    private static void AddEventBusSubscriptions(this IEventBusBuilder eventBus)
    {
        eventBus.AddSubscription<OrderCreatedIntegrationEvent, OrderCreatedIntegrationEventHandler>();
    }
}
