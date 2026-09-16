using System.Text.Json.Serialization;

namespace eShop.Catalog.API.Model.Seller;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SellerStatus
{
    Active = 1,
    Suspended = 2,
    Inactive = 3
}
