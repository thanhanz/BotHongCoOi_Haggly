using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Haggly.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CreatePaymentTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "payment_transactions",
                schema: "payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    TransactionType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ProviderTransactionId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ProviderResponseCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ProviderResponseData = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FailureReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_transactions", x => x.Id);
                    table.CheckConstraint("CK_payment_transactions_amount", "\"Amount\" > 0");
                    table.ForeignKey(
                        name: "FK_payment_transactions_payments_PaymentId",
                        column: x => x.PaymentId,
                        principalSchema: "payments",
                        principalTable: "payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_payment_transactions_PaymentId_CreatedAt",
                schema: "payments",
                table: "payment_transactions",
                columns: new[] { "PaymentId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_payment_transactions_ProviderTransactionId",
                schema: "payments",
                table: "payment_transactions",
                column: "ProviderTransactionId",
                unique: true,
                filter: "\"ProviderTransactionId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "payment_transactions",
                schema: "payments");
        }
    }
}
