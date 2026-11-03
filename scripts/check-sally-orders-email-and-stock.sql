-- READ-ONLY diagnostics for the local/test ShopDb database.
-- This script does not insert, update, delete, or execute migrations.
SET NOCOUNT ON;

SELECT TOP (30)
    e.Id,
    e.Kind,
    e.AccountId,
    a.Email AS RecipientEmail,
    a.EmailVerified,
    e.OrderId,
    e.CreatedUtc,
    e.SentUtc,
    e.Attempts,
    e.LastError
FROM dbo.SallyEmailOutbox AS e
LEFT JOIN dbo.SallyAccounts AS a ON a.Id = e.AccountId
ORDER BY e.Id DESC;

SELECT
    p.Id AS ProductId,
    p.Name,
    p.StockQuantity AS ProductStock,
    s.Size,
    s.Quantity AS SizeStock
FROM dbo.Products AS p
INNER JOIN dbo.ProductSizeStocks AS s ON s.ProductId = p.Id
WHERE p.Name IN (
    N'LABEL PATCH WORK JACKET',
    N'CAMEL SUEDE JACKET',
    N'TEXTURED BLACK PUFFER',
    N'WASHED GREY HOODIE',
    N'REVERSIBLE SHREDDED SEAM DROP NECK SHIRT',
    N'COUNTRYSIDE STORY KNIT',
    N'GREY UTILITY VEST',
    N'GREY CREWNECK SWEATSHIRT'
)
ORDER BY p.Id, s.Size;

SELECT TOP (30)
    o.Id AS OrderId,
    o.AccountId,
    o.OrderDate,
    o.Status,
    o.TotalAmount,
    o.SourceCartId
FROM dbo.Orders AS o
ORDER BY o.Id DESC;
