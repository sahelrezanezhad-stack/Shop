-- TEST FIXTURE ONLY. User confirmed all displayed stock values are test data.
-- Restore a separate copy of the current database named SALLY_Test_20261030 first.
-- Never run against ShopDb. No schema/migrations, deletion, or stock reset occurs.
-- Default @Apply = 0 only previews changes. Set it to 1 only to apply this fixture
-- to the separate test database, with the test website stopped.
-- Missing sizes: S=2, M=3, L=3, XL=2; the reversible shirt uses XS=2, S=3, M=3, L=2.
-- Existing size quantities are preserved; totals are synchronized with their sum.
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Apply bit = 0;

IF DB_NAME() <> N'SALLY_Test_20261030'
    THROW 52001, 'Select the separate SALLY_Test_20261030 database. ShopDb is not a permitted target.', 1;
IF @@TRANCOUNT <> 0
    THROW 52002, 'Use a fresh query window without an open transaction.', 1;
IF OBJECT_ID(N'dbo.Products', N'U') IS NULL OR OBJECT_ID(N'dbo.ProductSizeStocks', N'U') IS NULL
    THROW 52003, 'Required tables are missing. Review the restored test schema; do not execute a migration automatically.', 1;

DECLARE @Catalog table ([Name] nvarchar(200) NOT NULL PRIMARY KEY, IsShirt bit NOT NULL);
INSERT @Catalog([Name],IsShirt) VALUES
 (N'LABEL PATCH WORK JACKET',0),
 (N'CAMEL SUEDE JACKET',0),
 (N'TEXTURED BLACK PUFFER',0),
 (N'WASHED GREY HOODIE',0),
 (N'REVERSIBLE SHREDDED SEAM DROP NECK SHIRT',1),
 (N'COUNTRYSIDE STORY KNIT',0),
 (N'GREY UTILITY VEST',0),
 (N'GREY CREWNECK SWEATSHIRT',0);

IF EXISTS (
    SELECT c.[Name]
    FROM @Catalog c LEFT JOIN dbo.Products p ON p.[Name]=c.[Name]
    GROUP BY c.[Name] HAVING COUNT(p.Id)<>1
)
    THROW 52004, 'Expected exactly one existing product for each of the eight approved names. No stock was changed.', 1;

DECLARE @Wanted table (ProductId int NOT NULL, Size nvarchar(4) NOT NULL,
                      DemoQuantity int NOT NULL, PRIMARY KEY(ProductId,Size));
INSERT @Wanted(ProductId,Size,DemoQuantity)
SELECT p.Id,s.Size,s.Qty
FROM @Catalog c JOIN dbo.Products p ON p.[Name]=c.[Name]
CROSS APPLY (VALUES
    (CASE WHEN c.IsShirt=1 THEN N'XS' ELSE N'S' END,2),
    (CASE WHEN c.IsShirt=1 THEN N'S' ELSE N'M' END,3),
    (CASE WHEN c.IsShirt=1 THEN N'M' ELSE N'L' END,3),
    (CASE WHEN c.IsShirt=1 THEN N'L' ELSE N'XL' END,2)
) s(Size,Qty);

IF EXISTS (
    SELECT 1 FROM dbo.ProductSizeStocks s
    WHERE s.ProductId IN (SELECT ProductId FROM @Wanted)
      AND (s.Quantity<0 OR NOT EXISTS (
          SELECT 1 FROM @Wanted w WHERE w.ProductId=s.ProductId AND w.Size=s.Size))
)
    THROW 52005, 'Unexpected or negative existing size inventory requires review. No stock was changed.', 1;
IF EXISTS (
    SELECT s.ProductId,s.Size FROM dbo.ProductSizeStocks s
    WHERE s.ProductId IN (SELECT ProductId FROM @Wanted)
    GROUP BY s.ProductId,s.Size HAVING COUNT(*)>1
)
    THROW 52006, 'Duplicate size rows require schema/data review. No stock was changed.', 1;

SELECT p.Id,p.[Name],w.Size,s.Quantity AS CurrentSizeQuantity,
       COALESCE(s.Quantity,w.DemoQuantity) AS ProposedSizeQuantity,
       CASE WHEN s.Id IS NULL THEN N'INSERT TEST SIZE' ELSE N'PRESERVE EXISTING SIZE' END AS PlannedAction,
       p.StockQuantity AS CurrentProductTotal,
       SUM(COALESCE(s.Quantity,w.DemoQuantity)) OVER(PARTITION BY p.Id) AS ProposedProductTotal
FROM @Wanted w JOIN dbo.Products p ON p.Id=w.ProductId
LEFT JOIN dbo.ProductSizeStocks s ON s.ProductId=w.ProductId AND s.Size=w.Size
ORDER BY p.Id,CASE w.Size WHEN N'XS' THEN 0 WHEN N'S' THEN 1 WHEN N'M' THEN 2 WHEN N'L' THEN 3 ELSE 4 END;

IF @Apply=0
BEGIN
    SELECT N'PREVIEW ONLY: no persistent data changed. Set @Apply=1 only for the separate test database.' AS Result;
    RETURN;
END;

BEGIN TRY
    BEGIN TRANSACTION;
    -- Same product-before-size lock order as application inventory operations.
    DECLARE @LockedProduct int;
    SELECT @LockedProduct=p.Id FROM dbo.Products p WITH (UPDLOCK,HOLDLOCK)
    WHERE p.Id IN (SELECT ProductId FROM @Wanted) ORDER BY p.Id;

    INSERT dbo.ProductSizeStocks(ProductId,Size,Quantity)
    SELECT w.ProductId,w.Size,w.DemoQuantity FROM @Wanted w
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.ProductSizeStocks s WITH (UPDLOCK,HOLDLOCK)
        WHERE s.ProductId=w.ProductId AND s.Size=w.Size
    );

    UPDATE p SET StockQuantity=t.SizeTotal
    FROM dbo.Products p
    CROSS APPLY (SELECT SUM(s.Quantity) AS SizeTotal FROM dbo.ProductSizeStocks s WHERE s.ProductId=p.Id) t
    WHERE p.Id IN (SELECT ProductId FROM @Wanted) AND p.StockQuantity<>t.SizeTotal;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

SELECT p.Id,p.[Name],s.Size,s.Quantity,p.StockQuantity
FROM dbo.ProductSizeStocks s JOIN dbo.Products p ON p.Id=s.ProductId
WHERE p.Id IN (SELECT ProductId FROM @Wanted)
ORDER BY p.Id,CASE s.Size WHEN N'XS' THEN 0 WHEN N'S' THEN 1 WHEN N'M' THEN 2 WHEN N'L' THEN 3 ELSE 4 END;
