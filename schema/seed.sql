-- Idempotent demonstration data. Existing orders and stock are not reset.
INSERT INTO Customer VALUES (1, 'Иван Петров', 'ivan@example.test')
    ON CONFLICT(Id) DO NOTHING;
INSERT INTO Customer VALUES (2, 'Анна Соколова', 'anna@example.test')
    ON CONFLICT(Id) DO NOTHING;
INSERT INTO Address VALUES (1, 1, 'Москва', 'Учебная улица, дом 1')
    ON CONFLICT(Id) DO NOTHING;
INSERT INTO Address VALUES (2, 2, 'Казань', 'Учебная улица, дом 2')
    ON CONFLICT(Id) DO NOTHING;
INSERT INTO Category VALUES (1, 'Периферия'), (2, 'Мониторы')
    ON CONFLICT(Id) DO NOTHING;
INSERT INTO Product VALUES
    (1, 1, 'Клавиатура', 199900, 10),
    (2, 1, 'Мышь', 129900, 15),
    (3, 2, 'Монитор', 1499900, 4)
    ON CONFLICT(Id) DO NOTHING;
INSERT INTO OrderStatus VALUES (1, 'Новый'), (2, 'Оплачен'), (3, 'Отправлен')
    ON CONFLICT(Id) DO NOTHING;
INSERT INTO DeliveryMethod VALUES (1, 'Самовывоз', 0), (2, 'Курьер', 35000)
    ON CONFLICT(Id) DO NOTHING;
-- One historical completed order; initial stock above is the current stock.
INSERT INTO SalesOrder VALUES (1, 1, 1, 3, '2026-09-20T10:00:00.0000000Z')
    ON CONFLICT(Id) DO NOTHING;
INSERT INTO OrderItem VALUES (1, 1, 2, 1, 129900)
    ON CONFLICT(Id) DO NOTHING;
INSERT INTO Payment VALUES (1, 1, 129900, '2026-09-20T10:05:00.0000000Z')
    ON CONFLICT(Id) DO NOTHING;
INSERT INTO Shipment VALUES (1, 1, 1, 'DEMO-0001', '2026-09-20T12:00:00.0000000Z')
    ON CONFLICT(Id) DO NOTHING;
