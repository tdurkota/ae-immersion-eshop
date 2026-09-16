using System.Text.Json.Serialization;

namespace eShop.Catalog.API.Model.Seller;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SellerPayoutStatus
{
    Pending = 1,
    Processed = 2,
    Paid = 3,
    Failed = 4
}
