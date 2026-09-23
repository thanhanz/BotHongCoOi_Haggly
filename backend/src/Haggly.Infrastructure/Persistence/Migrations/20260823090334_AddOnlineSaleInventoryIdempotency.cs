using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Haggly.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOnlineSaleInventoryIdempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_inventory_ledgers_InventoryItemId_ReferenceType_ReferenceId",
                schema: "inventory",
                table: "inventory_ledgers",
                columns: new[] { "InventoryItemId", "ReferenceType", "ReferenceId" },
                unique: true,
                filter: "\"ReferenceType\" = 'PAYMENT_TRANSACTION' AND \"ReferenceId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_inventory_ledgers_InventoryItemId_ReferenceType_ReferenceId",
                schema: "inventory",
                table: "inventory_ledgers");
        }
    }
}
