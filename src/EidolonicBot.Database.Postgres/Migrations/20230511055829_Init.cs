using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EidolonicBot.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:CollationDefinition:case_insensitive", "en-u-ks-primary,en-u-ks-primary,icu,False");

            migrationBuilder.CreateTable(
                name: "Subscription",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Address = table.Column<string>(type: "text", nullable: false, collation: "case_insensitive")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subscription", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionByChat",
                columns: table => new
                {
                    ChatId = table.Column<long>(type: "bigint", nullable: false),
                    SubscriptionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionByChat", x => new { x.ChatId, x.SubscriptionId });
                    table.ForeignKey(
                        name: "FK_SubscriptionByChat_Subscription_SubscriptionId",
                        column: x => x.SubscriptionId,
                        principalTable: "Subscription",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Subscription_Address",
                table: "Subscription",
                column: "Address",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionByChat_SubscriptionId",
                table: "SubscriptionByChat",
                column: "SubscriptionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SubscriptionByChat");

            migrationBuilder.DropTable(
                name: "Subscription");
        }
    }
}
