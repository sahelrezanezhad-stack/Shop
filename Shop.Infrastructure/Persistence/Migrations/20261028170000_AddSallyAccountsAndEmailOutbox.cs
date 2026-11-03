using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Shop.Infrastructure.Persistence;

#nullable disable
namespace Shop.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ShopDbContext))]
[Migration("20261028170000_AddSallyAccountsAndEmailOutbox")]
public sealed class AddSallyAccountsAndEmailOutbox : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(name: "AccountId", table: "Orders", type: "uniqueidentifier", nullable: true);
        migrationBuilder.CreateIndex(name: "IX_Orders_AccountId", table: "Orders", column: "AccountId");
        migrationBuilder.CreateTable(name: "SallyAccounts", columns: table => new {
            Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
            DisplayName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
            Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
            NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
            PasswordHash = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
            EmailVerified = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
            VerificationTokenHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
            VerificationExpiresUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
            CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
        }, constraints: table => table.PrimaryKey("PK_SallyAccounts", x => x.Id));
        migrationBuilder.CreateIndex(name: "IX_SallyAccounts_NormalizedEmail", table: "SallyAccounts", column: "NormalizedEmail", unique: true);
        migrationBuilder.CreateTable(name: "SallyEmailOutbox", columns: table => new {
            Id = table.Column<long>(type: "bigint", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
            AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
            OrderId = table.Column<int>(type: "int", nullable: true),
            Kind = table.Column<string>(type: "nvarchar(24)", maxLength:24, nullable:false),
            ProtectedToken = table.Column<string>(type: "nvarchar(2048)", maxLength:2048, nullable: true),
            CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable:false),
            SentUtc = table.Column<DateTime>(type: "datetime2", nullable:true),
            Attempts = table.Column<int>(type:"int", nullable:false, defaultValue:0),
            LastError = table.Column<string>(type:"nvarchar(250)", maxLength:250, nullable:true)
        }, constraints: table => table.PrimaryKey("PK_SallyEmailOutbox", x=>x.Id));
        migrationBuilder.CreateIndex(name: "IX_SallyEmailOutbox_Kind_OrderId",table:"SallyEmailOutbox",columns:new[]{"Kind","OrderId"},unique:true,filter:"[OrderId] IS NOT NULL");
        migrationBuilder.CreateIndex(name: "IX_SallyEmailOutbox_SentUtc",table:"SallyEmailOutbox",column:"SentUtc");
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("SallyEmailOutbox");
        migrationBuilder.DropTable("SallyAccounts");
        migrationBuilder.DropIndex("IX_Orders_AccountId", "Orders");
        migrationBuilder.DropColumn("AccountId", "Orders");
    }
}
