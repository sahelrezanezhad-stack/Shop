-- OPTIONAL TEST DATA ONLY. Not required for the existing eight products.
-- Only the separate SALLY_Test_20261030 database is permitted.
-- This script never creates schema or executes migrations.
-- Safe to rerun: only missing names are added. Existing prices and stock are never overwritten.
-- PRICES ARE TEST VALUES IN TOMAN; STOCK=0 MEANS NOT CONFIRMED, NOT SOLD OUT.
SET NOCOUNT ON;
SET XACT_ABORT ON;
IF DB_NAME() <> N'SALLY_Test_20261030'
    THROW 52001, 'Select the separate SALLY_Test_20261030 database. The existing ShopDb is not a permitted target.', 1;
IF OBJECT_ID(N'dbo.Products', N'U') IS NULL
    THROW 52002, 'Products table is missing. Stop and review the restored test schema; do not run a migration automatically.', 1;
BEGIN TRANSACTION;
WITH Catalog(Name,Description,Price) AS (
  SELECT * FROM (VALUES
(N'LABEL PATCH WORK JACKET',N'Washed work jacket with scattered color patches and oversized pockets',CAST(289000000 AS decimal(18,2))),
(N'CAMEL SUEDE JACKET',N'Camel suede-look jacket with a classic collar',CAST(365000000 AS decimal(18,2))),
(N'TEXTURED BLACK PUFFER',N'Textured black puffer jacket with a relaxed silhouette',CAST(449000000 AS decimal(18,2))),
(N'WASHED GREY HOODIE',N'Washed grey hoodie with white graphics and a kangaroo pocket',CAST(159000000 AS decimal(18,2))),
(N'REVERSIBLE SHREDDED SEAM DROP NECK SHIRT',N'Reversible printed shirt with frayed edges',CAST(319000000 AS decimal(18,2))),
(N'COUNTRYSIDE STORY KNIT',N'Ecru illustrated knit with a countryside landscape',CAST(229000000 AS decimal(18,2))),
(N'GREY UTILITY VEST',N'Slate grey utility vest with functional side pockets',CAST(114000000 AS decimal(18,2))),
(N'GREY CREWNECK SWEATSHIRT',N'Light grey crewneck sweatshirt with a sleeve brand patch',CAST(187000000 AS decimal(18,2)))
  ) AS src(Name,Description,Price)
)
INSERT INTO dbo.Products(Name, Description, Price, StockQuantity, CreatedAt)
SELECT c.Name, c.Description, c.Price, 0, SYSUTCDATETIME()
FROM Catalog AS c
WHERE NOT EXISTS (SELECT 1 FROM dbo.Products AS p WITH (UPDLOCK, HOLDLOCK) WHERE p.Name=c.Name);
COMMIT TRANSACTION;
SELECT Id,Name,Price,StockQuantity FROM dbo.Products WHERE Name IN (
N'LABEL PATCH WORK JACKET',N'CAMEL SUEDE JACKET',N'TEXTURED BLACK PUFFER',N'WASHED GREY HOODIE',N'REVERSIBLE SHREDDED SEAM DROP NECK SHIRT',N'COUNTRYSIDE STORY KNIT',N'GREY UTILITY VEST',N'GREY CREWNECK SWEATSHIRT');
