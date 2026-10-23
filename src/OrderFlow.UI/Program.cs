using System.Globalization;
using System.Text;
using OrderFlow.BLL;
using OrderFlow.DAL;

Console.OutputEncoding = Encoding.UTF8;
CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ru-RU");

var databasePath = args.Length > 0 ? args[0] : "artifacts/orders.db";
var repository = new SqliteShopRepository(databasePath);
repository.Initialize();
var service = new OrderService(repository);

Console.WriteLine("OrderFlow — система управления заказами");
Console.WriteLine("Слои: UI → BLL → DAL → SQLite; общие модели: Domain");
Console.WriteLine("Каталог товаров:");
foreach (var product in service.GetCatalog())
    Console.WriteLine($"{product.Id}. {product.Name}: " +
        $"{product.UnitPriceCents / 100m:F2} руб., остаток {product.StockQuantity}");

try
{
    var receipt = service.PlaceOrder(1, 1,
        [new RequestedItem(1, 2), new RequestedItem(2, 1)]);
    Console.WriteLine($"Создан заказ №{receipt.OrderId}; позиций: {receipt.LineCount}");
    Console.WriteLine($"Сумма товаров: {receipt.TotalCents / 100m:F2} руб.");
    Console.WriteLine($"Сохранено позиций в SQLite: " +
        service.GetOrderItems(receipt.OrderId).Count);
    Console.WriteLine("Остатки после заказа:");
    foreach (var product in service.GetCatalog())
        Console.WriteLine($"{product.Name}: {product.StockQuantity}");
}
catch (ArgumentException error)
{
    Console.Error.WriteLine(error.Message);
    return 1;
}
catch (InvalidOperationException error)
{
    Console.Error.WriteLine(error.Message);
    return 1;
}

Console.WriteLine($"Файл базы данных: {databasePath}");
return 0;
