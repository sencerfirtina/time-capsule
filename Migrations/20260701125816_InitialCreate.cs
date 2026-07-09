using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeCapsule.API.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TimeCapsules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EncryptedContent = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Category = table.Column<int>(type: "int", nullable: false),
                    Operator = table.Column<int>(type: "int", nullable: false),
                    TargetValue = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MetaData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsOpened = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimeCapsules", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TimeCapsules");
        }
    }
}
