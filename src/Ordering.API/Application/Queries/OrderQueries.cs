namespace eShop.Ordering.API.Application.Queries;

using eShop.Ordering.API.Application.Services;

public class OrderQueries(OrderingContext context)
    : IOrderQueries
{
    public async Task<Order> GetOrderAsync(int id)
    {
        var order = await context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == id);
      
        if (order is null)
            throw new KeyNotFoundException();

        var commissionService = new CommissionService();

        return new Order
        {
            OrderNumber = order.Id,
            Date = order.OrderDate,
            Description = order.Description,
            City = order.Address.City,
            Country = order.Address.Country,
            State = order.Address.State,
            Street = order.Address.Street,
            Zipcode = order.Address.ZipCode,
            Status = order.OrderStatus.ToString(),
            Total = order.GetTotal(),
            OrderItems = order.OrderItems.Select(oi => new Orderitem
            {
                ProductName = oi.ProductName,
                Units = oi.Units,
                UnitPrice = (double)oi.UnitPrice,
                PictureUrl = oi.PictureUrl,
                SellerId = oi.SellerId,
                CommissionRate = oi.CommissionRate,
                CommissionAmount = commissionService.CalculateCommission(oi.UnitPrice * oi.Units - oi.Discount, oi.CommissionRate),
                SellerAmount = commissionService.CalculateSellerAmount(oi.UnitPrice * oi.Units - oi.Discount, oi.CommissionRate)
            }).ToList()
        };
    }

    public async Task<IEnumerable<OrderSummary>> GetOrdersFromUserAsync(string userId)
    {
        return await context.Orders
            .Where(o => o.Buyer.IdentityGuid == userId)  
            .Select(o => new OrderSummary
            {
                OrderNumber = o.Id,
                Date = o.OrderDate,
                Status = o.OrderStatus.ToString(),
                Total =(double) o.OrderItems.Sum(oi => oi.UnitPrice* oi.Units)
            })
            .ToListAsync();
    } 
    
    public async Task<IEnumerable<CardType>> GetCardTypesAsync() => 
        await context.CardTypes.Select(c=> new CardType { Id = c.Id, Name = c.Name }).ToListAsync();
}
