using System.Globalization;
using Microsoft.Data.Sqlite;
using OrderFlow.Domain;

namespace OrderFlow.DAL;

public sealed class SqliteShopRepository : IShopRepository
{
    private readonly string _connectionString;

    public SqliteShopRepository(string databasePath)
    {
        var fullPath = Path.GetFullPath(databasePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = fullPath,
            ForeignKeys = true,
            Pooling = false
        }.ToString();
    }

    public void Initialize()
    {
        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        ExecuteResource(connection, transaction, "OrderFlow.DAL.Schema.sql");
        ExecuteResource(connection, transaction, "OrderFlow.DAL.Seed.sql");
        transaction.Commit();
    }

    public IReadOnlyList<Product> GetProducts()
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, CategoryId, Name, UnitPriceCents, " +
            "StockQuantity FROM Product ORDER BY Id";
        using var reader = command.ExecuteReader();
        var products = new List<Product>();
        while (reader.Read()) products.Add(ReadProduct(reader));
        return products;
    }

    public Product? GetProduct(long id) =>
        GetProducts().SingleOrDefault(product => product.Id == id);

    public Customer? GetCustomer(long id)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, FullName, Email FROM Customer WHERE Id=$id";
        command.Parameters.AddWithValue("$id", id);
        using var reader = command.ExecuteReader();
        return reader.Read()
            ? new Customer(reader.GetInt64(0), reader.GetString(1), reader.GetString(2))
            : null;
    }

    public Address? GetAddress(long id)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, CustomerId, City, Street FROM Address WHERE Id=$id";
        command.Parameters.AddWithValue("$id", id);
        using var reader = command.ExecuteReader();
        return reader.Read()
            ? new Address(reader.GetInt64(0), reader.GetInt64(1),
                reader.GetString(2), reader.GetString(3))
            : null;
    }

    public IReadOnlyList<OrderItem> GetOrderItems(long orderId)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, OrderId, ProductId, Quantity, UnitPriceCents " +
            "FROM OrderItem WHERE OrderId=$id ORDER BY Id";
        command.Parameters.AddWithValue("$id", orderId);
        using var reader = command.ExecuteReader();
        var items = new List<OrderItem>();
        while (reader.Read())
            items.Add(new OrderItem(reader.GetInt64(0), reader.GetInt64(1),
                reader.GetInt64(2), reader.GetInt32(3), reader.GetInt64(4)));
        return items;
    }

    public long SaveOrder(long customerId, long addressId,
        IReadOnlyList<OrderLineDraft> lines, DateTime createdUtc)
    {
        if (lines.Count == 0)
            throw new ArgumentException("Заказ не содержит товаров.", nameof(lines));

        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        using var order = connection.CreateCommand();
        order.Transaction = transaction;
        order.CommandText = "INSERT INTO SalesOrder(CustomerId, AddressId, StatusId, CreatedUtc) " +
            "VALUES ($customer, $address, 1, $created); SELECT last_insert_rowid();";
        order.Parameters.AddWithValue("$customer", customerId);
        order.Parameters.AddWithValue("$address", addressId);
        order.Parameters.AddWithValue("$created", createdUtc.ToUniversalTime()
            .ToString("O", CultureInfo.InvariantCulture));
        var orderId = (long)order.ExecuteScalar()!;

        foreach (var line in lines)
        {
            if (line.Quantity <= 0 || line.UnitPriceCents < 0)
                throw new ArgumentException("Неверная позиция заказа.", nameof(lines));

            using var stock = connection.CreateCommand();
            stock.Transaction = transaction;
            stock.CommandText = "UPDATE Product SET StockQuantity=StockQuantity-$quantity " +
                "WHERE Id=$product AND StockQuantity >= $quantity AND UnitPriceCents=$price";
            stock.Parameters.AddWithValue("$quantity", line.Quantity);
            stock.Parameters.AddWithValue("$product", line.ProductId);
            stock.Parameters.AddWithValue("$price", line.UnitPriceCents);
            if (stock.ExecuteNonQuery() != 1)
                throw new InvalidOperationException("Остаток или цена товара изменились.");

            using var item = connection.CreateCommand();
            item.Transaction = transaction;
            item.CommandText = "INSERT INTO OrderItem(OrderId, ProductId, Quantity, UnitPriceCents) " +
                "VALUES ($order, $product, $quantity, $price)";
            item.Parameters.AddWithValue("$order", orderId);
            item.Parameters.AddWithValue("$product", line.ProductId);
            item.Parameters.AddWithValue("$quantity", line.Quantity);
            item.Parameters.AddWithValue("$price", line.UnitPriceCents);
            item.ExecuteNonQuery();
        }

        transaction.Commit();
        return orderId;
    }

    private SqliteConnection Open()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }

    private static Product ReadProduct(SqliteDataReader reader) =>
        new(reader.GetInt64(0), reader.GetInt64(1), reader.GetString(2),
            reader.GetInt64(3), reader.GetInt32(4));

    private static void ExecuteResource(SqliteConnection connection,
        SqliteTransaction transaction, string name)
    {
        using var stream = typeof(SqliteShopRepository).Assembly
            .GetManifestResourceStream(name)
            ?? throw new InvalidOperationException($"Ресурс {name} отсутствует.");
        using var reader = new StreamReader(stream);
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = reader.ReadToEnd();
        command.ExecuteNonQuery();
    }
}
