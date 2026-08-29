using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace quanlykhachsan.Migrations
{
    /// <inheritdoc />
    public partial class AddHourlyPriceToRoom : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "HourlyPrice",
                table: "Rooms",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HourlyPrice",
                table: "Rooms");
        }
    }
}
