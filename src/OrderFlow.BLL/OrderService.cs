using OrderFlow.DAL;
using OrderFlow.Domain;

namespace OrderFlow.BLL;

public sealed record RequestedItem(long ProductId, int Quantity);
public sealed record OrderReceipt(long OrderId, int LineCount, long TotalCents);

public sealed class OrderService(IShopRepository repository)
{
    public IReadOnlyList<Product> GetCatalog() => repository.GetProducts();

    public IReadOnlyList<OrderItem> GetOrderItems(long orderId) =>
        repository.GetOrderItems(orderId);

    public OrderReceipt PlaceOrder(long customerId, long addressId,
        IReadOnlyList<RequestedItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        if (repository.GetCustomer(customerId) is null)
            throw new ArgumentException("Покупатель не найден.", nameof(customerId));
        var address = repository.GetAddress(addressId);
        if (address is null || address.CustomerId != customerId)
            throw new ArgumentException("Адрес не принадлежит покупателю.", nameof(addressId));
        if (items.Count == 0)
            throw new ArgumentException("В заказе должна быть хотя бы одна позиция.", nameof(items));
        if (items.Any(item => item.Quantity <= 0))
            throw new ArgumentException("Количество должно быть положительным.", nameof(items));

        var lines = new List<OrderLineDraft>();
        long total = 0;
        foreach (var group in items.GroupBy(item => item.ProductId))
        {
            var quantity = group.Sum(item => item.Quantity);
            var product = repository.GetProduct(group.Key)
                ?? throw new ArgumentException("Товар не найден.", nameof(items));
            if (quantity > product.StockQuantity)
                throw new InvalidOperationException($"Недостаточно товара: {product.Name}.");
            total = checked(total + checked(product.UnitPriceCents * quantity));
            lines.Add(new OrderLineDraft(product.Id, quantity, product.UnitPriceCents));
        }

        var orderId = repository.SaveOrder(customerId, addressId, lines, DateTime.UtcNow);
        return new OrderReceipt(orderId, lines.Count, total);
    }
}
