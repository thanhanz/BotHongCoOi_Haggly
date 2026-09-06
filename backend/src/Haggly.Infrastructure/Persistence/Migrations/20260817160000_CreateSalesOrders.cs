using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Haggly.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CreateSalesOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(name: "sales");

            migrationBuilder.CreateTable(
                name: "orders",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderNo = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    BuyerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    TotalToCharge = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalPaid = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    PlacedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PaymentDueAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CancelledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CancellationReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orders", x => x.Id);
                    table.CheckConstraint("CK_orders_amount_bounds", "\"TotalToCharge\" >= 0 AND \"TotalPaid\" >= 0 AND \"TotalPaid\" <= \"TotalToCharge\"");
                    table.ForeignKey(
                        name: "FK_orders_buyer_profiles_BuyerId",
                        column: x => x.BuyerId,
                        principalSchema: "identity",
                        principalTable: "buyer_profiles",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stall_fulfillments",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    StallId = table.Column<Guid>(type: "uuid", nullable: false),
                    FulfillmentNo = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Subtotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    FinalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PaidAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PickupCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    PreparedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReadyAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PickedUpAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PickupConfirmedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CancelledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CancellationReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stall_fulfillments", x => x.Id);
                    table.CheckConstraint("CK_stall_fulfillments_amount_bounds", "\"Subtotal\" >= 0 AND \"FinalAmount\" >= 0 AND \"PaidAmount\" >= 0 AND \"PaidAmount\" <= \"FinalAmount\"");
                    table.ForeignKey(
                        name: "FK_stall_fulfillments_orders_OrderId",
                        column: x => x.OrderId,
                        principalSchema: "sales",
                        principalTable: "orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_stall_fulfillments_stalls_StallId",
                        column: x => x.StallId,
                        principalSchema: "markets",
                        principalTable: "stalls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "order_items",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StallFulfillmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    InventoryItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductNameSnapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SellingUnitSnapshot = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PublicUnitPriceSnapshot = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    FinalUnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    FinalQuantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    LineTotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    IsNegotiated = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_items", x => x.Id);
                    table.CheckConstraint("CK_order_items_amount_bounds", "\"PublicUnitPriceSnapshot\" >= 0 AND \"FinalUnitPrice\" >= 0 AND \"FinalQuantity\" > 0 AND \"LineTotal\" >= 0");
                    table.ForeignKey(
                        name: "FK_order_items_inventory_items_InventoryItemId",
                        column: x => x.InventoryItemId,
                        principalSchema: "inventory",
                        principalTable: "inventory_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_order_items_stall_fulfillments_StallFulfillmentId",
                        column: x => x.StallFulfillmentId,
                        principalSchema: "sales",
                        principalTable: "stall_fulfillments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_orders_BuyerId_PlacedAt",
                schema: "sales",
                table: "orders",
                columns: new[] { "BuyerId", "PlacedAt" });
            migrationBuilder.CreateIndex(
                name: "IX_orders_OrderNo",
                schema: "sales",
                table: "orders",
                column: "OrderNo",
                unique: true);
            migrationBuilder.CreateIndex(
                name: "IX_order_items_StallFulfillmentId_InventoryItemId",
                schema: "sales",
                table: "order_items",
                columns: new[] { "StallFulfillmentId", "InventoryItemId" },
                unique: true);
            migrationBuilder.CreateIndex(
                name: "IX_stall_fulfillments_FulfillmentNo",
                schema: "sales",
                table: "stall_fulfillments",
                column: "FulfillmentNo",
                unique: true);
            migrationBuilder.CreateIndex(
                name: "IX_stall_fulfillments_OrderId_StallId",
                schema: "sales",
                table: "stall_fulfillments",
                columns: new[] { "OrderId", "StallId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "order_items", schema: "sales");
            migrationBuilder.DropTable(name: "stall_fulfillments", schema: "sales");
            migrationBuilder.DropTable(name: "orders", schema: "sales");
        }
    }
}
