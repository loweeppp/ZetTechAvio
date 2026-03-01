using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZetTechAvio1._0.Migrations
{
    /// <inheritdoc />
    public partial class AddPassportToTickets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PassportNumber",
                table: "Tickets",
                type: "varchar(6)",
                maxLength: 6,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "PassportSeries",
                table: "Tickets",
                type: "varchar(4)",
                maxLength: 4,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PassportNumber",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "PassportSeries",
                table: "Tickets");
        }
    }
}
