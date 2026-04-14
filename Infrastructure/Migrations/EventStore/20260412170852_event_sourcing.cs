using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShoppingCartService.Infrastructure.Migrations.EventStore
{
    /// <inheritdoc />
    public partial class event_sourcing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "public");

            migrationBuilder.CreateTable(
                name: "shopping_cart_events",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CartId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Payload = table.Column<string>(type: "jsonb", nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shopping_cart_events", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_shopping_cart_events_CartId_OccurredAtUtc",
                schema: "public",
                table: "shopping_cart_events",
                columns: new[] { "CartId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_shopping_cart_events_CustomerId_OccurredAtUtc",
                schema: "public",
                table: "shopping_cart_events",
                columns: new[] { "CustomerId", "OccurredAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "shopping_cart_events",
                schema: "public");
        }
    }
}
