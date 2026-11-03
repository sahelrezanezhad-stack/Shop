using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Shop.Infrastructure.Persistence;

#nullable disable
namespace Shop.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ShopDbContext))]
[Migration("20261028120000_AddShoppingCarts")]
public sealed class AddShoppingCarts : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ShoppingCarts",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_ShoppingCarts", x => x.Id));

        migrationBuilder.CreateTable(
            name: "ShoppingCartLines",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                ShoppingCartId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ProductId = table.Column<int>(type: "int", nullable: false),
                Size = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: false),
                Quantity = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ShoppingCartLines", x => x.Id);
                table.ForeignKey("FK_ShoppingCartLines_ShoppingCarts_ShoppingCartId", x => x.ShoppingCartId,
                    "ShoppingCarts", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_ShoppingCartLines_Products_ProductId", x => x.ProductId,
                    "Products", "Id", onDelete: ReferentialAction.Restrict);
            });
        migrationBuilder.CreateIndex(name: "IX_ShoppingCartLines_ProductId", table: "ShoppingCartLines", column: "ProductId");
        migrationBuilder.CreateIndex(name: "IX_ShoppingCartLines_ShoppingCartId_ProductId_Size",
            table: "ShoppingCartLines", columns: new[]{"ShoppingCartId","ProductId","Size"}, unique: true);
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("ShoppingCartLines");
        migrationBuilder.DropTable("ShoppingCarts");
    }
}
