using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Haggly.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CreateDishDiscovery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "discovery");

            migrationBuilder.CreateTable(
                name: "canonical_ingredients",
                schema: "discovery",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    NormalizedName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_canonical_ingredients", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "common_dishes",
                schema: "discovery",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    NormalizedName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_common_dishes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "product_ingredient_mappings",
                schema: "discovery",
                columns: table => new
                {
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    CanonicalIngredientId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    MappingMethod = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_ingredient_mappings", x => x.ProductId);
                    table.ForeignKey(
                        name: "FK_product_ingredient_mappings_canonical_ingredients_Canonical~",
                        column: x => x.CanonicalIngredientId,
                        principalSchema: "discovery",
                        principalTable: "canonical_ingredients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_product_ingredient_mappings_products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "catalog",
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "common_dish_ingredients",
                schema: "discovery",
                columns: table => new
                {
                    DishId = table.Column<Guid>(type: "uuid", nullable: false),
                    CanonicalIngredientId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_common_dish_ingredients", x => new { x.DishId, x.CanonicalIngredientId });
                    table.ForeignKey(
                        name: "FK_common_dish_ingredients_canonical_ingredients_CanonicalIngr~",
                        column: x => x.CanonicalIngredientId,
                        principalSchema: "discovery",
                        principalTable: "canonical_ingredients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_common_dish_ingredients_common_dishes_DishId",
                        column: x => x.DishId,
                        principalSchema: "discovery",
                        principalTable: "common_dishes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_canonical_ingredients_Code",
                schema: "discovery",
                table: "canonical_ingredients",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_canonical_ingredients_IsActive_Name",
                schema: "discovery",
                table: "canonical_ingredients",
                columns: new[] { "IsActive", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_canonical_ingredients_NormalizedName",
                schema: "discovery",
                table: "canonical_ingredients",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_common_dish_ingredients_CanonicalIngredientId",
                schema: "discovery",
                table: "common_dish_ingredients",
                column: "CanonicalIngredientId");

            migrationBuilder.CreateIndex(
                name: "IX_common_dishes_ExternalId",
                schema: "discovery",
                table: "common_dishes",
                column: "ExternalId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_common_dishes_IsActive_Name",
                schema: "discovery",
                table: "common_dishes",
                columns: new[] { "IsActive", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_common_dishes_NormalizedName",
                schema: "discovery",
                table: "common_dishes",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_ingredient_mappings_CanonicalIngredientId_Status",
                schema: "discovery",
                table: "product_ingredient_mappings",
                columns: new[] { "CanonicalIngredientId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "common_dish_ingredients",
                schema: "discovery");

            migrationBuilder.DropTable(
                name: "product_ingredient_mappings",
                schema: "discovery");

            migrationBuilder.DropTable(
                name: "common_dishes",
                schema: "discovery");

            migrationBuilder.DropTable(
                name: "canonical_ingredients",
                schema: "discovery");
        }
    }
}
