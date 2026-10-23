using OrderFlow.Domain;

namespace OrderFlow.DAL;

public sealed record OrderLineDraft(long ProductId, int Quantity,
    long UnitPriceCents);

public interface IShopRepository
{
    IReadOnlyList<Product> GetProducts();
    Customer? GetCustomer(long id);
    Address? GetAddress(long id);
    Product? GetProduct(long id);
    IReadOnlyList<OrderItem> GetOrderItems(long orderId);
    long SaveOrder(long customerId, long addressId,
        IReadOnlyList<OrderLineDraft> lines, DateTime createdUtc);
}
