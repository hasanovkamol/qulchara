using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenBudget.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInitiativeCodeToTelegramGroup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InitiativeCode",
                table: "TelegramGroups",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InitiativeCode",
                table: "TelegramGroups");
        }
    }
}
