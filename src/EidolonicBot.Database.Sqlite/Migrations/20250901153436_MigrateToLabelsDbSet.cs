using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EidolonicBot.Migrations
{
    /// <inheritdoc />
    public partial class MigrateToLabelsDbSet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LabelByChat",
                columns: table => new
                {
                    ChatId = table.Column<long>(type: "INTEGER", nullable: false),
                    MessageThreadId = table.Column<int>(type: "INTEGER", nullable: false),
                    Address = table.Column<string>(type: "TEXT", maxLength: 66, nullable: false),
                    Label = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabelByChat", x => new { x.ChatId, x.MessageThreadId, x.Address });
                });

            migrationBuilder.Sql("""
                                 INSERT INTO LabelByChat
                                 SELECT sc.ChatId, sc.MessageThreadId, s.Address, sc.Label
                                 FROM SubscriptionByChat sc
                                 JOIN Subscription s ON sc.SubscriptionId = s.Id
                                 WHERE sc.Label is not null 
                                 """);
            
            migrationBuilder.DropColumn(
                name: "Label",
                table: "SubscriptionByChat");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LabelByChat");

            migrationBuilder.AddColumn<string>(
                name: "Label",
                table: "SubscriptionByChat",
                type: "TEXT",
                maxLength: 1000,
                nullable: true);
        }
    }
}
