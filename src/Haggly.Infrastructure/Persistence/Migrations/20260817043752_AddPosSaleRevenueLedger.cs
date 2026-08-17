using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Haggly.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPosSaleRevenueLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "finance");

            migrationBuilder.AddColumn<decimal>(
                name: "AmountPaid",
                schema: "sales",
                table: "pos_sales",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "PaymentMethod",
                schema: "sales",
                table: "pos_sales",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "CASH");

            migrationBuilder.AddColumn<string>(
                name: "PaymentStatus",
                schema: "sales",
                table: "pos_sales",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "PAID");

            migrationBuilder.Sql("""
                UPDATE sales.pos_sales
                SET "AmountPaid" = "TotalAmount",
                    "PaymentMethod" = 'CASH',
                    "PaymentStatus" = 'PAID';
                """);

            migrationBuilder.CreateTable(
                name: "revenue_ledgers",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StallId = table.Column<Guid>(type: "uuid", nullable: false),
                    StallFulfillmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    PosSaleId = table.Column<Guid>(type: "uuid", nullable: true),
                    PaymentAllocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    EntryType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    GrossAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    RefundAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    NetAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReferenceType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ReferenceId = table.Column<Guid>(type: "uuid", nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_revenue_ledgers", x => x.Id);
                    table.CheckConstraint("CK_revenue_ledgers_amount_bounds", "\"GrossAmount\" >= 0 AND \"RefundAmount\" >= 0 AND \"NetAmount\" = \"GrossAmount\" - \"RefundAmount\"");
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_pos_sales_amount_paid_bounds",
                schema: "sales",
                table: "pos_sales",
                sql: "\"AmountPaid\" >= 0 AND \"AmountPaid\" = \"TotalAmount\"");

            migrationBuilder.CreateIndex(
                name: "IX_revenue_ledgers_PosSaleId_EntryType",
                schema: "finance",
                table: "revenue_ledgers",
                columns: new[] { "PosSaleId", "EntryType" },
                unique: true,
                filter: "\"PosSaleId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_revenue_ledgers_StallId_OccurredAt_Id",
                schema: "finance",
                table: "revenue_ledgers",
                columns: new[] { "StallId", "OccurredAt", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "revenue_ledgers",
                schema: "finance");

            migrationBuilder.DropCheckConstraint(
                name: "CK_pos_sales_amount_paid_bounds",
                schema: "sales",
                table: "pos_sales");

            migrationBuilder.DropColumn(
                name: "AmountPaid",
                schema: "sales",
                table: "pos_sales");

            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                schema: "sales",
                table: "pos_sales");

            migrationBuilder.DropColumn(
                name: "PaymentStatus",
                schema: "sales",
                table: "pos_sales");
        }
    }
}
