using Microsoft.Data.Sqlite;
using OrderFlow.BLL;
using OrderFlow.DAL;

var directory = Path.Combine(Path.GetTempPath(), "orderflow-" + Guid.NewGuid());
Directory.CreateDirectory(directory);
var databasePath = Path.Combine(directory, "checks.db");
var repository = new SqliteShopRepository(databasePath);
repository.Initialize();
var service = new OrderService(repository);
var passed = 0;

try
{
    Check("10 SQL tables and no broken foreign keys", () =>
    {
        Equal(10L, Scalar("SELECT count(*) FROM sqlite_master WHERE type='table'"));
        Equal(0L, Scalar("SELECT count(*) FROM pragma_foreign_key_check"));
    });
    Check("Order total, persisted lines and stock", () =>
    {
        var receipt = service.PlaceOrder(1, 1,
            [new RequestedItem(1, 2), new RequestedItem(2, 1)]);
        Equal(529700L, receipt.TotalCents);
        Equal(2, receipt.LineCount);
        Equal(2, repository.GetOrderItems(receipt.OrderId).Count);
        Equal(8, repository.GetProduct(1)!.StockQuantity);
        Equal(14, repository.GetProduct(2)!.StockQuantity);
    });
    Check("Repeated product is merged into one order line", () =>
    {
        var receipt = service.PlaceOrder(1, 1,
            [new RequestedItem(3, 1), new RequestedItem(3, 1)]);
        Equal(1, receipt.LineCount);
        Equal(2, repository.GetOrderItems(receipt.OrderId).Single().Quantity);
    });
    Check("Empty order is rejected", () =>
        Throws<ArgumentException>(() => service.PlaceOrder(1, 1, [])));
    Check("Zero and negative quantities are rejected", () =>
    {
        Throws<ArgumentException>(() => service.PlaceOrder(1, 1, [new(1, 0)]));
        Throws<ArgumentException>(() => service.PlaceOrder(1, 1, [new(1, -1)]));
    });
    Check("Another customer's address is rejected", () =>
        Throws<ArgumentException>(() => service.PlaceOrder(1, 2, [new(1, 1)])));
    Check("Missing customer and product are rejected", () =>
    {
        Throws<ArgumentException>(() => service.PlaceOrder(999, 1, [new(1, 1)]));
        Throws<ArgumentException>(() => service.PlaceOrder(1, 1, [new(999, 1)]));
    });
    Check("Insufficient stock is rejected without writes", () =>
    {
        var before = Scalar("SELECT count(*) FROM SalesOrder");
        Throws<InvalidOperationException>(() => service.PlaceOrder(1, 1, [new(1, 999)]));
        Equal(before, Scalar("SELECT count(*) FROM SalesOrder"));
    });
    Check("DAL transaction rolls back partial stock changes", () =>
    {
        var orders = Scalar("SELECT count(*) FROM SalesOrder");
        var stock = repository.GetProduct(1)!.StockQuantity;
        Throws<InvalidOperationException>(() => repository.SaveOrder(1, 1,
            [new(1, 1, 199900), new(2, 999, 129900)], DateTime.UtcNow));
        Equal(orders, Scalar("SELECT count(*) FROM SalesOrder"));
        Equal(stock, repository.GetProduct(1)!.StockQuantity);
    });
    Check("Database rejects address owned by another customer", () =>
        Throws<SqliteException>(() => repository.SaveOrder(1, 2,
            [new(1, 1, 199900)], DateTime.UtcNow)));
    Check("Initialization preserves stock and orders", () =>
    {
        var orders = Scalar("SELECT count(*) FROM SalesOrder");
        var stock = repository.GetProduct(1)!.StockQuantity;
        repository.Initialize();
        Equal(orders, Scalar("SELECT count(*) FROM SalesOrder"));
        Equal(stock, repository.GetProduct(1)!.StockQuantity);
    });
    Console.WriteLine($"CHECKS PASSED: {passed}");
    return 0;
}
finally
{
    Directory.Delete(directory, recursive: true);
}

void Check(string name, Action action)
{
    action();
    passed++;
    Console.WriteLine($"PASS: {name}");
}
long Scalar(string sql)
{
    using var connection = new SqliteConnection(
        new SqliteConnectionStringBuilder { DataSource = databasePath, Pooling = false }.ToString());
    connection.Open();
    using var command = connection.CreateCommand();
    command.CommandText = sql;
    return Convert.ToInt64(command.ExecuteScalar());
}
static void Equal<T>(T expected, T actual) where T : notnull
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
        throw new InvalidOperationException($"Expected {expected}; actual {actual}");
}
static void Throws<T>(Action action) where T : Exception
{
    try { action(); }
    catch (T) { return; }
    throw new InvalidOperationException($"Expected {typeof(T).Name}");
}
