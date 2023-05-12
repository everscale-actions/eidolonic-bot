using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EidolonicBot.Migrations
{
    /// <inheritdoc />
    public partial class AddThreadIdToSubscription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_SubscriptionByChat",
                table: "SubscriptionByChat");

            migrationBuilder.AddColumn<int>(
                name: "MessageThreadId",
                table: "SubscriptionByChat",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_SubscriptionByChat",
                table: "SubscriptionByChat",
                columns: new[] { "ChatId", "MessageThreadId", "SubscriptionId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_SubscriptionByChat",
                table: "SubscriptionByChat");

            migrationBuilder.DropColumn(
                name: "MessageThreadId",
                table: "SubscriptionByChat");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SubscriptionByChat",
                table: "SubscriptionByChat",
                columns: new[] { "ChatId", "SubscriptionId" });
        }
    }
}
