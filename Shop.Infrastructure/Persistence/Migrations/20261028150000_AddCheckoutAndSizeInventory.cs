using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Shop.Infrastructure.Persistence;

#nullable disable
namespace Shop.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ShopDbContext))]
[Migration("20261028150000_AddCheckoutAndSizeInventory")]
public sealed class AddCheckoutAndSizeInventory : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(name: "SourceCartId", table: "Orders",
            type: "uniqueidentifier", nullable: true);
        migrationBuilder.AddColumn<string>(name: "ShippingCity", table: "Orders",
            type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<string>(name: "ShippingAddress", table: "Orders",
            type: "nvarchar(300)", maxLength: 300, nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<string>(name: "ShippingPostalCode", table: "Orders",
            type: "nvarchar(20)", maxLength: 20, nullable: true);
        migrationBuilder.AddColumn<string>(name: "Size", table: "OrderItems",
            type: "nvarchar(4)", maxLength: 4, nullable: false, defaultValue: "");
        migrationBuilder.CreateIndex(name: "IX_Orders_SourceCartId", table: "Orders",
            column: "SourceCartId", unique: true, filter: "[SourceCartId] IS NOT NULL");
        migrationBuilder.CreateTable(name: "ProductSizeStocks", columns: table => new
        {
            Id = table.Column<int>(type: "int", nullable: false)
                .Annotation("SqlServer:Identity", "1, 1"),
            ProductId = table.Column<int>(type: "int", nullable: false),
            Size = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: false),
            Quantity = table.Column<int>(type: "int", nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_ProductSizeStocks", x => x.Id);
            table.ForeignKey("FK_ProductSizeStocks_Products_ProductId", x => x.ProductId,
                "Products", "Id", onDelete: ReferentialAction.Cascade);
            table.CheckConstraint("CK_ProductSizeStocks_Quantity", "[Quantity] >= 0");
        });
        migrationBuilder.CreateIndex(name: "IX_ProductSizeStocks_ProductId_Size",
            table: "ProductSizeStocks", columns: new[] { "ProductId", "Size" }, unique: true);
        // Do not invent/assign real size inventory in a schema migration.
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("ProductSizeStocks");
        migrationBuilder.DropIndex("IX_Orders_SourceCartId", "Orders");
        migrationBuilder.DropColumn("SourceCartId", "Orders");
        migrationBuilder.DropColumn("ShippingCity", "Orders");
        migrationBuilder.DropColumn("ShippingAddress", "Orders");
        migrationBuilder.DropColumn("ShippingPostalCode", "Orders");
        migrationBuilder.DropColumn("Size", "OrderItems");
    }
}
