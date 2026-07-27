using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeCapsule.API.Migrations
{
    /// <inheritdoc />
    public partial class AddedUserAndUserSpotifyTokenTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UserID",
                table: "TimeCapsules",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserSpotifyTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccessToken = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RefreshToken = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExpirationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSpotifyTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSpotifyTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TimeCapsules_UserID",
                table: "TimeCapsules",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_UserSpotifyTokens_UserId",
                table: "UserSpotifyTokens",
                column: "UserId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TimeCapsules_Users_UserID",
                table: "TimeCapsules",
                column: "UserID",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TimeCapsules_Users_UserID",
                table: "TimeCapsules");

            migrationBuilder.DropTable(
                name: "UserSpotifyTokens");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropIndex(
                name: "IX_TimeCapsules_UserID",
                table: "TimeCapsules");

            migrationBuilder.DropColumn(
                name: "UserID",
                table: "TimeCapsules");
        }
    }
}
