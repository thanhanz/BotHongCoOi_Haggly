using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Haggly.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CreateCarts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "carts",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BuyerId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_carts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_carts_buyer_profiles_BuyerId",
                        column: x => x.BuyerId,
                        principalSchema: "identity",
                        principalTable: "buyer_profiles",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cart_items",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CartId = table.Column<Guid>(type: "uuid", nullable: false),
                    InventoryItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cart_items", x => x.Id);
                    table.CheckConstraint("CK_cart_items_quantity_bounds", "\"Quantity\" > 0");
                    table.ForeignKey(
                        name: "FK_cart_items_carts_CartId",
                        column: x => x.CartId,
                        principalSchema: "sales",
                        principalTable: "carts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_cart_items_inventory_items_InventoryItemId",
                        column: x => x.InventoryItemId,
                        principalSchema: "inventory",
                        principalTable: "inventory_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_stall_fulfillments_StallId",
                schema: "sales",
                table: "stall_fulfillments",
                column: "StallId");

            migrationBuilder.CreateIndex(
                name: "IX_order_items_InventoryItemId",
                schema: "sales",
                table: "order_items",
                column: "InventoryItemId");

            migrationBuilder.CreateIndex(
                name: "IX_cart_items_CartId_InventoryItemId",
                schema: "sales",
                table: "cart_items",
                columns: new[] { "CartId", "InventoryItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cart_items_InventoryItemId",
                schema: "sales",
                table: "cart_items",
                column: "InventoryItemId");

            migrationBuilder.CreateIndex(
                name: "IX_carts_BuyerId",
                schema: "sales",
                table: "carts",
                column: "BuyerId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cart_items",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "carts",
                schema: "sales");

            migrationBuilder.DropIndex(
                name: "IX_stall_fulfillments_StallId",
                schema: "sales",
                table: "stall_fulfillments");

            migrationBuilder.DropIndex(
                name: "IX_order_items_InventoryItemId",
                schema: "sales",
                table: "order_items");
        }
    }
}
