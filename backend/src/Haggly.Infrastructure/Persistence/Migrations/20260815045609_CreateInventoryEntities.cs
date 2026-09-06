using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Haggly.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CreateInventoryEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "inventory");

            migrationBuilder.CreateTable(
                name: "inventory_sessions",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StallId = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessDate = table.Column<DateOnly>(type: "date", nullable: false),
                    OpenedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    OpenedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    ClosedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ClosedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_sessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_inventory_sessions_stalls_StallId",
                        column: x => x.StallId,
                        principalSchema: "markets",
                        principalTable: "stalls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "daily_product_listings",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InventorySessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductStallId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductNameSnapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SellingUnitSnapshot = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PublicUnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    OpeningQuantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    CurrentQuantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    ReservedQuantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    AvailableQuantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_daily_product_listings", x => x.Id);
                    table.CheckConstraint("CK_daily_product_listings_available_quantity_bounds", "\"AvailableQuantity\" >= 0 AND \"AvailableQuantity\" = \"CurrentQuantity\" - \"ReservedQuantity\"");
                    table.CheckConstraint("CK_daily_product_listings_quantity_bounds", "\"OpeningQuantity\" >= 0 AND \"CurrentQuantity\" >= 0 AND \"ReservedQuantity\" >= 0 AND \"ReservedQuantity\" <= \"CurrentQuantity\"");
                    table.ForeignKey(
                        name: "FK_daily_product_listings_inventory_sessions_InventorySessionId",
                        column: x => x.InventorySessionId,
                        principalSchema: "inventory",
                        principalTable: "inventory_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_daily_product_listings_product_stalls_ProductStallId",
                        column: x => x.ProductStallId,
                        principalSchema: "catalog",
                        principalTable: "product_stalls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inventory_ledgers",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DailyProductListingId = table.Column<Guid>(type: "uuid", nullable: false),
                    InventorySessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TransactionType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    QuantityDelta = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    QuantityBefore = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    QuantityAfter = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    UnitPriceBefore = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    UnitPriceAfter = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    ReferenceType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ReferenceId = table.Column<Guid>(type: "uuid", nullable: true),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PerformedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_ledgers", x => x.Id);
                    table.CheckConstraint("CK_inventory_ledgers_price_bounds", "(\"UnitPriceBefore\" IS NULL OR \"UnitPriceBefore\" >= 0) AND (\"UnitPriceAfter\" IS NULL OR \"UnitPriceAfter\" >= 0)");
                    table.CheckConstraint("CK_inventory_ledgers_quantity_bounds", "\"QuantityBefore\" >= 0 AND \"QuantityAfter\" >= 0");
                    table.ForeignKey(
                        name: "FK_inventory_ledgers_daily_product_listings_DailyProductListin~",
                        column: x => x.DailyProductListingId,
                        principalSchema: "inventory",
                        principalTable: "daily_product_listings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_ledgers_inventory_sessions_InventorySessionId",
                        column: x => x.InventorySessionId,
                        principalSchema: "inventory",
                        principalTable: "inventory_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_daily_product_listings_InventorySessionId_ProductStallId",
                schema: "inventory",
                table: "daily_product_listings",
                columns: new[] { "InventorySessionId", "ProductStallId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_daily_product_listings_ProductStallId",
                schema: "inventory",
                table: "daily_product_listings",
                column: "ProductStallId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_ledgers_DailyProductListingId_OccurredAt_Id",
                schema: "inventory",
                table: "inventory_ledgers",
                columns: new[] { "DailyProductListingId", "OccurredAt", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_ledgers_InventorySessionId_OccurredAt_Id",
                schema: "inventory",
                table: "inventory_ledgers",
                columns: new[] { "InventorySessionId", "OccurredAt", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_sessions_StallId_BusinessDate",
                schema: "inventory",
                table: "inventory_sessions",
                columns: new[] { "StallId", "BusinessDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inventory_ledgers",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "daily_product_listings",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "inventory_sessions",
                schema: "inventory");
        }
    }
}
