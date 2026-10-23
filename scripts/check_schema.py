"""Validate the physical model, independently of the C# data access code."""
from pathlib import Path
import sqlite3

root = Path(__file__).resolve().parents[1]
with sqlite3.connect(":memory:") as connection:
    connection.execute("PRAGMA foreign_keys=ON")
    connection.executescript((root / "schema/shop.sql").read_text(encoding="utf-8"))
    connection.executescript((root / "schema/seed.sql").read_text(encoding="utf-8"))
    tables = connection.execute(
        "SELECT name FROM sqlite_master WHERE type='table' ORDER BY name"
    ).fetchall()
    assert len(tables) == 10, tables
    assert connection.execute("PRAGMA foreign_key_check").fetchall() == []
    rejected = 0
    invalid_statements = [
        "INSERT INTO Product VALUES (99, 999, 'Unknown category', 100, 1)",
        "INSERT INTO Product VALUES (99, 1, 'Negative stock', 100, -1)",
        "INSERT INTO OrderItem VALUES (99, 1, 1, 0, 100)",
        "INSERT INTO SalesOrder VALUES (99, 1, 2, 1, '2026-09-20T10:00:00Z')",
        "INSERT INTO Payment VALUES (99, 1, 100, '2026-09-20T10:00:00Z')",
    ]
    for sql in invalid_statements:
        try:
            connection.execute(sql)
        except sqlite3.IntegrityError:
            rejected += 1
        else:
            raise AssertionError(f"Invalid record accepted: {sql}")
    print(f"SQL TABLES: {len(tables)}")
    for (name,) in tables:
        print(f"  {name}")
    print("FOREIGN KEY VIOLATIONS: 0")
    print(f"INVALID RECORDS REJECTED: {rejected}")
    print("SCHEMA CHECK PASSED")
