using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DriverService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRatingToRide : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("14991985-5a6b-4f5f-bc50-5b968d29b71f"));

            migrationBuilder.AddColumn<string>(
                name: "Comment",
                table: "Rides",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Rating",
                table: "Rides",
                type: "int",
                nullable: true);

            migrationBuilder.InsertData(
                table: "Drivers",
                columns: new[] { "Id", "AvatarUrl", "CurrentLocation", "IsOnline", "LicenseType", "UserId" },
                values: new object[] { new Guid("ab545dae-39d9-4374-90d2-5bb71c99a9f9"), null, "10.7, 106.6", true, 0, new Guid("33333333-3333-3333-3333-333333333333") });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("ab545dae-39d9-4374-90d2-5bb71c99a9f9"));

            migrationBuilder.DropColumn(
                name: "Comment",
                table: "Rides");

            migrationBuilder.DropColumn(
                name: "Rating",
                table: "Rides");

            migrationBuilder.InsertData(
                table: "Drivers",
                columns: new[] { "Id", "AvatarUrl", "CurrentLocation", "IsOnline", "LicenseType", "UserId" },
                values: new object[] { new Guid("14991985-5a6b-4f5f-bc50-5b968d29b71f"), null, "10.7, 106.6", true, 0, new Guid("33333333-3333-3333-3333-333333333333") });
        }
    }
}
