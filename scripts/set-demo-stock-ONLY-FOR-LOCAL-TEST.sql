-- Retained legacy filename; deliberately performs no inventory writes.
-- Changing only Products.StockQuantity leaves per-size inventory inconsistent.
-- Use seed-demo-size-stock-LOCAL-ONLY.sql against the separate test database.
THROW 52000, 'Total-only stock initialization is retired. Review seed-demo-size-stock-LOCAL-ONLY.sql for the separate SALLY_Test_20261030 database.', 1;
