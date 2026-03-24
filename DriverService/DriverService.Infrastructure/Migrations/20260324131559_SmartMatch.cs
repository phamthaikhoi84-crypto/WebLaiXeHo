using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DriverService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SmartMatch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "PickupLat",
                table: "Rides",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "PickupLng",
                table: "Rides",
                type: "float",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PickupLat",
                table: "Rides");

            migrationBuilder.DropColumn(
                name: "PickupLng",
                table: "Rides");
        }
    }
}
