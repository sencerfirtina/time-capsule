using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeCapsule.API.Migrations
{
    /// <inheritdoc />
    public partial class SpotifyTrackIdcolumnremovedfromCapsuleEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SpotifyTrackId",
                table: "TimeCapsules");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SpotifyTrackId",
                table: "TimeCapsules",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
