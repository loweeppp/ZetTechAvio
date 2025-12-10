using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZetTechAvio1._0.Migrations
{
    /// <inheritdoc />
    public partial class FixPriceDecimalPrecision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "Price",
                table: "Fares",
                type: "decimal(12,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(65,30)");

            migrationBuilder.AlterColumn<string>(
                name: "Class",
                table: "Fares",
                type: "varchar(255)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "idx_class",
                table: "Fares",
                column: "Class");

            migrationBuilder.CreateIndex(
                name: "idx_flight_id",
                table: "Fares",
                column: "FlightId");

            migrationBuilder.AddForeignKey(
                name: "FK_Fares_Flights_FlightId",
                table: "Fares",
                column: "FlightId",
                principalTable: "Flights",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Fares_Flights_FlightId",
                table: "Fares");

            migrationBuilder.DropIndex(
                name: "idx_class",
                table: "Fares");

            migrationBuilder.DropIndex(
                name: "idx_flight_id",
                table: "Fares");

            migrationBuilder.AlterColumn<decimal>(
                name: "Price",
                table: "Fares",
                type: "decimal(65,30)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(12,2)");

            migrationBuilder.AlterColumn<int>(
                name: "Class",
                table: "Fares",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}
