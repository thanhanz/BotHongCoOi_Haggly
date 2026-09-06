using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Haggly.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CreateProductStalls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "product_stalls",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StallId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SellingUnit = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    MinimumOrderQuantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    DefaultUnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    IsNegotiable = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_stalls", x => x.Id);
                    table.ForeignKey(
                        name: "FK_product_stalls_products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "catalog",
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_product_stalls_stalls_StallId",
                        column: x => x.StallId,
                        principalSchema: "markets",
                        principalTable: "stalls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_product_stalls_ProductId",
                schema: "catalog",
                table: "product_stalls",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_product_stalls_StallId_ProductId",
                schema: "catalog",
                table: "product_stalls",
                columns: new[] { "StallId", "ProductId" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "product_stalls",
                schema: "catalog");
        }
    }
}
