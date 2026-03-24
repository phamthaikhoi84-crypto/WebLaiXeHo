using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DriverService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleAndLicenseType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("8e16c31e-4426-46b6-8d5f-e0c3932c9306"));

            migrationBuilder.AddColumn<int>(
                name: "TransmissionType",
                table: "Rides",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "VehicleType",
                table: "Rides",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LicenseType",
                table: "Drivers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.InsertData(
                table: "Drivers",
                columns: new[] { "Id", "AvatarUrl", "CurrentLocation", "IsOnline", "LicenseType", "UserId" },
                values: new object[] { new Guid("14991985-5a6b-4f5f-bc50-5b968d29b71f"), null, "10.7, 106.6", true, 0, new Guid("33333333-3333-3333-3333-333333333333") });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("14991985-5a6b-4f5f-bc50-5b968d29b71f"));

            migrationBuilder.DropColumn(
                name: "TransmissionType",
                table: "Rides");

            migrationBuilder.DropColumn(
                name: "VehicleType",
                table: "Rides");

            migrationBuilder.DropColumn(
                name: "LicenseType",
                table: "Drivers");

            migrationBuilder.InsertData(
                table: "Drivers",
                columns: new[] { "Id", "AvatarUrl", "CurrentLocation", "IsOnline", "UserId" },
                values: new object[] { new Guid("8e16c31e-4426-46b6-8d5f-e0c3932c9306"), null, "10.7, 106.6", true, new Guid("33333333-3333-3333-3333-333333333333") });
        }
    }
}
