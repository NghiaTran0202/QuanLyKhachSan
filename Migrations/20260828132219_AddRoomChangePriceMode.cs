using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace quanlykhachsan.Migrations
{
    /// <inheritdoc />
    public partial class AddRoomChangePriceMode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AppliedPrice",
                table: "RoomChangeHistories",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "PriceMode",
                table: "RoomChangeHistories",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "KeepCurrent");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AppliedPrice",
                table: "RoomChangeHistories");

            migrationBuilder.DropColumn(
                name: "PriceMode",
                table: "RoomChangeHistories");
        }
    }
}