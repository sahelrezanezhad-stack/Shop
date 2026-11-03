/* Run only against the existing database selected in SSMS. This script reads metadata
   and aggregate counts. It does not migrate, seed, create, update, or delete persistent data. */
SELECT DB_NAME() AS CurrentDatabase, CAST(SERVERPROPERTY('ProductVersion') AS nvarchar(100)) AS SqlVersion;

IF OBJECT_ID(N'dbo.__EFMigrationsHistory',N'U') IS NOT NULL
    EXEC(N'SELECT MigrationId,ProductVersion FROM dbo.__EFMigrationsHistory ORDER BY MigrationId;');
ELSE SELECT N'Migration history table absent; inspect existing schema before proposing any migration.' AS ReviewRequired;

;WITH RequiredColumns AS (
    SELECT * FROM (VALUES
      (N'Products',N'Id'),(N'Products',N'Name'),(N'Products',N'Price'),(N'Products',N'StockQuantity'),
      (N'Customers',N'Id'),(N'Customers',N'Email'),
      (N'Orders',N'AccountId'),(N'Orders',N'SourceCartId'),(N'Orders',N'ShippingCity'),
      (N'Orders',N'ShippingAddress'),(N'Orders',N'ShippingPostalCode'),
      (N'OrderItems',N'Size'),(N'OrderItems',N'ProductId'),(N'OrderItems',N'Quantity'),
      (N'ShoppingCarts',N'Id'),(N'ShoppingCarts',N'UpdatedAtUtc'),
      (N'ShoppingCartLines',N'ShoppingCartId'),(N'ShoppingCartLines',N'ProductId'),
      (N'ShoppingCartLines',N'Size'),(N'ShoppingCartLines',N'Quantity'),
      (N'ProductSizeStocks',N'ProductId'),(N'ProductSizeStocks',N'Size'),(N'ProductSizeStocks',N'Quantity'),
      (N'SallyAccounts',N'Id'),(N'SallyAccounts',N'NormalizedEmail'),(N'SallyAccounts',N'PasswordHash'),
      (N'SallyAccounts',N'EmailVerified'),(N'SallyAccounts',N'VerificationTokenHash'),(N'SallyAccounts',N'VerificationExpiresUtc'),
      (N'SallyEmailOutbox',N'Id'),(N'SallyEmailOutbox',N'AccountId'),(N'SallyEmailOutbox',N'OrderId'),
      (N'SallyEmailOutbox',N'Kind'),(N'SallyEmailOutbox',N'ProtectedToken'),(N'SallyEmailOutbox',N'SentUtc'),
      (N'SallyEmailOutbox',N'Attempts'),(N'SallyEmailOutbox',N'LastError'),(N'SallyEmailOutbox',N'CreatedUtc')
    ) v(TableName,ColumnName)
)
SELECT r.TableName,r.ColumnName,CASE WHEN c.column_id IS NULL THEN N'MISSING' ELSE N'PRESENT' END AS Result,
    t.name AS DataType,c.max_length,c.is_nullable
FROM RequiredColumns r
LEFT JOIN sys.columns c ON c.object_id=OBJECT_ID(N'dbo.'+r.TableName) AND c.name=r.ColumnName
LEFT JOIN sys.types t ON t.user_type_id=c.user_type_id
ORDER BY r.TableName,r.ColumnName;

SELECT fk.name,OBJECT_NAME(fk.parent_object_id) AS ChildTable,OBJECT_NAME(fk.referenced_object_id) AS ParentTable,
    fk.delete_referential_action_desc AS DeleteBehavior
FROM sys.foreign_keys fk
WHERE fk.referenced_object_id IN (OBJECT_ID(N'dbo.Products'),OBJECT_ID(N'dbo.Orders'),OBJECT_ID(N'dbo.Customers'));

SELECT OBJECT_NAME(i.object_id) AS TableName,i.name,i.is_unique,i.filter_definition
FROM sys.indexes i WHERE i.object_id IN (OBJECT_ID(N'dbo.Orders'),OBJECT_ID(N'dbo.ProductSizeStocks'),
    OBJECT_ID(N'dbo.ShoppingCartLines'),OBJECT_ID(N'dbo.SallyAccounts'),OBJECT_ID(N'dbo.SallyEmailOutbox')) AND i.name IS NOT NULL
ORDER BY TableName,i.name;

IF OBJECT_ID(N'dbo.ProductSizeStocks',N'U') IS NOT NULL AND OBJECT_ID(N'dbo.Products',N'U') IS NOT NULL
    EXEC(N'SELECT p.Id AS ProductId,p.StockQuantity,COALESCE(SUM(CAST(s.Quantity AS bigint)),0) AS SizeTotal
        FROM dbo.Products p LEFT JOIN dbo.ProductSizeStocks s ON s.ProductId=p.Id
        GROUP BY p.Id,p.StockQuantity HAVING p.StockQuantity<>COALESCE(SUM(CAST(s.Quantity AS bigint)),0);
        SELECT COUNT_BIG(*) AS NegativeSizeRows FROM dbo.ProductSizeStocks WHERE Quantity<0;');

IF OBJECT_ID(N'dbo.SallyEmailOutbox',N'U') IS NOT NULL
    EXEC(N'SELECT Kind,COUNT_BIG(*) AS PendingRows FROM dbo.SallyEmailOutbox WHERE SentUtc IS NULL AND Attempts<10 GROUP BY Kind;');
