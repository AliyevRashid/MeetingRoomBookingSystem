using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace core_service.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddSubjectToBooking : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "Role",
            table: "Users",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<string>(
            name: "Subject",
            table: "Bookings",
            type: "character varying(128)",
            maxLength: 128,
            nullable: false,
            defaultValue: "");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "Role",
            table: "Users");

        migrationBuilder.DropColumn(
            name: "Subject",
            table: "Bookings");
    }
}
