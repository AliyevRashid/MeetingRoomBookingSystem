using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace core_service.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Extending : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_BookingParticipants_UserId",
                table: "BookingParticipants",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_BookingParticipants_Users_UserId",
                table: "BookingParticipants",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BookingParticipants_Users_UserId",
                table: "BookingParticipants");

            migrationBuilder.DropIndex(
                name: "IX_BookingParticipants_UserId",
                table: "BookingParticipants");
        }
    }
}
