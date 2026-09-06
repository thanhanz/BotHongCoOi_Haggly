using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Haggly.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentAllocationsAndRevenueLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "payment_allocations",
                schema: "payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentTransactionId = table.Column<Guid>(type: "uuid", nullable: false),
                    StallFulfillmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    StallId = table.Column<Guid>(type: "uuid", nullable: false),
                    AllocationType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    AllocatedAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    AllocatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_allocations", x => x.Id);
                    table.CheckConstraint("CK_payment_allocations_amount", "\"AllocatedAmount\" > 0");
                    table.ForeignKey(
                        name: "FK_payment_allocations_payment_transactions_PaymentTransaction~",
                        column: x => x.PaymentTransactionId,
                        principalSchema: "payments",
                        principalTable: "payment_transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_payment_allocations_stall_fulfillments_StallFulfillmentId",
                        column: x => x.StallFulfillmentId,
                        principalSchema: "sales",
                        principalTable: "stall_fulfillments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_revenue_ledgers_PaymentAllocationId_EntryType",
                schema: "finance",
                table: "revenue_ledgers",
                columns: new[] { "PaymentAllocationId", "EntryType" },
                unique: true,
                filter: "\"PaymentAllocationId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_revenue_ledgers_StallFulfillmentId",
                schema: "finance",
                table: "revenue_ledgers",
                column: "StallFulfillmentId");

            migrationBuilder.CreateIndex(
                name: "IX_payment_allocations_PaymentTransactionId_StallFulfillmentId",
                schema: "payments",
                table: "payment_allocations",
                columns: new[] { "PaymentTransactionId", "StallFulfillmentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payment_allocations_StallFulfillmentId",
                schema: "payments",
                table: "payment_allocations",
                column: "StallFulfillmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_revenue_ledgers_payment_allocations_PaymentAllocationId",
                schema: "finance",
                table: "revenue_ledgers",
                column: "PaymentAllocationId",
                principalSchema: "payments",
                principalTable: "payment_allocations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_revenue_ledgers_stall_fulfillments_StallFulfillmentId",
                schema: "finance",
                table: "revenue_ledgers",
                column: "StallFulfillmentId",
                principalSchema: "sales",
                principalTable: "stall_fulfillments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_revenue_ledgers_payment_allocations_PaymentAllocationId",
                schema: "finance",
                table: "revenue_ledgers");

            migrationBuilder.DropForeignKey(
                name: "FK_revenue_ledgers_stall_fulfillments_StallFulfillmentId",
                schema: "finance",
                table: "revenue_ledgers");

            migrationBuilder.DropTable(
                name: "payment_allocations",
                schema: "payments");

            migrationBuilder.DropIndex(
                name: "IX_revenue_ledgers_PaymentAllocationId_EntryType",
                schema: "finance",
                table: "revenue_ledgers");

            migrationBuilder.DropIndex(
                name: "IX_revenue_ledgers_StallFulfillmentId",
                schema: "finance",
                table: "revenue_ledgers");
        }
    }
}
