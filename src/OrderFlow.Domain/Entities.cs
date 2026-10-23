namespace OrderFlow.Domain;

// Every record corresponds to one table in schema/shop.sql.
// Monetary values are integer kopecks to avoid rounding in SQLite.
public sealed record Customer(long Id, string FullName, string Email);
public sealed record Address(long Id, long CustomerId, string City, string Street);
public sealed record Category(long Id, string Name);
public sealed record Product(long Id, long CategoryId, string Name,
    long UnitPriceCents, int StockQuantity);
public sealed record OrderStatus(long Id, string Name);
public sealed record Order(long Id, long CustomerId, long AddressId,
    long StatusId, DateTime CreatedUtc);
public sealed record OrderItem(long Id, long OrderId, long ProductId,
    int Quantity, long UnitPriceCents);
public sealed record Payment(long Id, long OrderId, long AmountCents,
    DateTime PaidUtc);
public sealed record DeliveryMethod(long Id, string Name, long PriceCents);
public sealed record Shipment(long Id, long OrderId, long DeliveryMethodId,
    string TrackingNumber, DateTime? ShippedUtc);
