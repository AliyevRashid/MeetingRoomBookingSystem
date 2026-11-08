using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace core_service.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTaxIdToOrganization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TaxId",
                table: "Organizations",
                type: "character varying(8)",
                maxLength: 8,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_TaxId",
                table: "Organizations",
                column: "TaxId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Organizations_TaxId",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "TaxId",
                table: "Organizations");
        }
    }
}
