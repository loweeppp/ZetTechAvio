using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZetTechAvio1._0.Migrations
{
    /// <inheritdoc />
    public partial class FaresInFlights : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FlightId1",
                table: "Fares",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Fares_FlightId1",
                table: "Fares",
                column: "FlightId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Fares_Flights_FlightId1",
                table: "Fares",
                column: "FlightId1",
                principalTable: "Flights",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Fares_Flights_FlightId1",
                table: "Fares");

            migrationBuilder.DropIndex(
                name: "IX_Fares_FlightId1",
                table: "Fares");

            migrationBuilder.DropColumn(
                name: "FlightId1",
                table: "Fares");
        }
    }
}
