using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Haggly.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OptimizeOutboxPendingIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_outbox_messages_pending",
                schema: "messaging",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "messaging",
                table: "outbox_messages");

            migrationBuilder.Sql(
                """
                CREATE INDEX "IX_outbox_messages_unprocessed"
                ON messaging.outbox_messages ("OccurredAt", "Id")
                INCLUDE ("EventType", "Payload")
                WHERE "ProcessedAt" IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_outbox_messages_unprocessed",
                schema: "messaging",
                table: "outbox_messages");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                schema: "messaging",
                table: "outbox_messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE messaging.outbox_messages
                SET "CreatedAt" = "OccurredAt";
                """);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "CreatedAt",
                schema: "messaging",
                table: "outbox_messages",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_pending",
                schema: "messaging",
                table: "outbox_messages",
                columns: new[] { "CreatedAt", "Id" },
                filter: "\"ProcessedAt\" IS NULL");
        }
    }
}
