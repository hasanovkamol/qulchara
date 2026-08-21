using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenBudget.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVoteConfirmations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VoteConfirmations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LastNDigits = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TargetTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AdminId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    MatchedVoteId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VoteConfirmations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VoteConfirmations_Users_AdminId",
                        column: x => x.AdminId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VoteConfirmations_Votes_MatchedVoteId",
                        column: x => x.MatchedVoteId,
                        principalTable: "Votes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_VoteConfirmations_AdminId",
                table: "VoteConfirmations",
                column: "AdminId");

            migrationBuilder.CreateIndex(
                name: "IX_VoteConfirmations_MatchedVoteId",
                table: "VoteConfirmations",
                column: "MatchedVoteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VoteConfirmations");
        }
    }
}
