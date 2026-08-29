using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace quanlykhachsan.Migrations
{
    /// <inheritdoc />
    public partial class AddRoomChangeHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RoomChangeHistories",
                columns: table => new
                {
                    RoomChangeHistoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BookingId = table.Column<int>(type: "int", nullable: false),
                    OldRoomId = table.Column<int>(type: "int", nullable: false),
                    NewRoomId = table.Column<int>(type: "int", nullable: false),
                    OldRoomPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NewRoomPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomChangeHistories", x => x.RoomChangeHistoryId);
                    table.ForeignKey(
                        name: "FK_RoomChangeHistories_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "BookingId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoomChangeHistories_Rooms_NewRoomId",
                        column: x => x.NewRoomId,
                        principalTable: "Rooms",
                        principalColumn: "RoomId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoomChangeHistories_Rooms_OldRoomId",
                        column: x => x.OldRoomId,
                        principalTable: "Rooms",
                        principalColumn: "RoomId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RoomChangeHistories_BookingId",
                table: "RoomChangeHistories",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomChangeHistories_NewRoomId",
                table: "RoomChangeHistories",
                column: "NewRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomChangeHistories_OldRoomId",
                table: "RoomChangeHistories",
                column: "OldRoomId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RoomChangeHistories");
        }
    }
}
