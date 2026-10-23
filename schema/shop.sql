-- Physical ER model: ten entities. Money is stored in kopecks, dates in UTC.
CREATE TABLE IF NOT EXISTS Customer (
    Id INTEGER PRIMARY KEY,
    FullName TEXT NOT NULL CHECK (length(trim(FullName)) > 0),
    Email TEXT NOT NULL COLLATE NOCASE UNIQUE
) STRICT;

CREATE TABLE IF NOT EXISTS Address (
    Id INTEGER PRIMARY KEY,
    CustomerId INTEGER NOT NULL REFERENCES Customer(Id),
    City TEXT NOT NULL,
    Street TEXT NOT NULL,
    UNIQUE (CustomerId, Id)
) STRICT;

CREATE TABLE IF NOT EXISTS Category (
    Id INTEGER PRIMARY KEY,
    Name TEXT NOT NULL UNIQUE
) STRICT;

CREATE TABLE IF NOT EXISTS Product (
    Id INTEGER PRIMARY KEY,
    CategoryId INTEGER NOT NULL REFERENCES Category(Id),
    Name TEXT NOT NULL,
    UnitPriceCents INTEGER NOT NULL CHECK (UnitPriceCents >= 0),
    StockQuantity INTEGER NOT NULL CHECK (StockQuantity >= 0)
) STRICT;

CREATE TABLE IF NOT EXISTS OrderStatus (
    Id INTEGER PRIMARY KEY,
    Name TEXT NOT NULL UNIQUE
) STRICT;

CREATE TABLE IF NOT EXISTS SalesOrder (
    Id INTEGER PRIMARY KEY,
    CustomerId INTEGER NOT NULL REFERENCES Customer(Id),
    AddressId INTEGER NOT NULL,
    StatusId INTEGER NOT NULL REFERENCES OrderStatus(Id),
    CreatedUtc TEXT NOT NULL,
    FOREIGN KEY (CustomerId, AddressId) REFERENCES Address(CustomerId, Id)
) STRICT;

CREATE TABLE IF NOT EXISTS OrderItem (
    Id INTEGER PRIMARY KEY,
    OrderId INTEGER NOT NULL REFERENCES SalesOrder(Id),
    ProductId INTEGER NOT NULL REFERENCES Product(Id),
    Quantity INTEGER NOT NULL CHECK (Quantity > 0),
    UnitPriceCents INTEGER NOT NULL CHECK (UnitPriceCents >= 0),
    UNIQUE (OrderId, ProductId)
) STRICT;

CREATE TABLE IF NOT EXISTS Payment (
    Id INTEGER PRIMARY KEY,
    OrderId INTEGER NOT NULL UNIQUE REFERENCES SalesOrder(Id),
    AmountCents INTEGER NOT NULL CHECK (AmountCents > 0),
    PaidUtc TEXT NOT NULL
) STRICT;

CREATE TABLE IF NOT EXISTS DeliveryMethod (
    Id INTEGER PRIMARY KEY,
    Name TEXT NOT NULL UNIQUE,
    PriceCents INTEGER NOT NULL CHECK (PriceCents >= 0)
) STRICT;

CREATE TABLE IF NOT EXISTS Shipment (
    Id INTEGER PRIMARY KEY,
    OrderId INTEGER NOT NULL UNIQUE REFERENCES SalesOrder(Id),
    DeliveryMethodId INTEGER NOT NULL REFERENCES DeliveryMethod(Id),
    TrackingNumber TEXT NOT NULL UNIQUE,
    ShippedUtc TEXT
) STRICT;

CREATE INDEX IF NOT EXISTS IX_Address_Customer ON Address(CustomerId);
CREATE INDEX IF NOT EXISTS IX_Product_Category ON Product(CategoryId);
CREATE INDEX IF NOT EXISTS IX_Order_Customer ON SalesOrder(CustomerId);
CREATE INDEX IF NOT EXISTS IX_Order_Status ON SalesOrder(StatusId);
CREATE INDEX IF NOT EXISTS IX_Order_Address ON SalesOrder(CustomerId, AddressId);
CREATE INDEX IF NOT EXISTS IX_Item_Product ON OrderItem(ProductId);
CREATE INDEX IF NOT EXISTS IX_Shipment_Method ON Shipment(DeliveryMethodId);
