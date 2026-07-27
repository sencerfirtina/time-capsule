using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeCapsule.API.Migrations
{
    /// <inheritdoc />
    public partial class AddedSpotifyTrackIdtoCapsuleEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SpotifyTrackId",
                table: "TimeCapsules",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SpotifyTrackId",
                table: "TimeCapsules");
        }
    }
}
