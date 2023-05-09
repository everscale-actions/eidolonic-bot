using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EidolonicBot.Migrations
{
    /// <inheritdoc />
    public partial class UniqueSubscriptionAddress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Subscription_Address",
                table: "Subscription",
                column: "Address",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Subscription_Address",
                table: "Subscription");
        }
    }
}
