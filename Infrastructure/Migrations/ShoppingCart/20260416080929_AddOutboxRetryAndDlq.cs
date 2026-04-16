using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShoppingCartService.Infrastructure.Migrations.ShoppingCart
{
    /// <inheritdoc />
    public partial class AddOutboxRetryAndDlq : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_outbox_messages_ProcessedAtUtc",
                schema: "public",
                table: "outbox_messages");

            migrationBuilder.AddColumn<bool>(
                name: "IsDeadLetter",
                schema: "public",
                table: "outbox_messages",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_ProcessedAtUtc",
                schema: "public",
                table: "outbox_messages",
                column: "ProcessedAtUtc",
                filter: "\"ProcessedAtUtc\" IS NULL AND \"RetryCount\" < 3 AND \"IsDeadLetter\" = FALSE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_outbox_messages_ProcessedAtUtc",
                schema: "public",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "IsDeadLetter",
                schema: "public",
                table: "outbox_messages");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_ProcessedAtUtc",
                schema: "public",
                table: "outbox_messages",
                column: "ProcessedAtUtc",
                filter: "\"ProcessedAtUtc\" IS NULL AND \"RetryCount\" < 3");
        }
    }
}
