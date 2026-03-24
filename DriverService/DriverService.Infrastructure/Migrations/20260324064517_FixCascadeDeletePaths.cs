using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DriverService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixCascadeDeletePaths : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Rides_Drivers_DriverId",
                table: "Rides");

            migrationBuilder.DropForeignKey(
                name: "FK_Rides_Users_UserId",
                table: "Rides");

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("ab545dae-39d9-4374-90d2-5bb71c99a9f9"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"));

            migrationBuilder.AddColumn<Guid>(
                name: "DriverId1",
                table: "Rides",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserId1",
                table: "Rides",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Drivers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Rides_DriverId1",
                table: "Rides",
                column: "DriverId1");

            migrationBuilder.CreateIndex(
                name: "IX_Rides_UserId1",
                table: "Rides",
                column: "UserId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Rides_Drivers_DriverId",
                table: "Rides",
                column: "DriverId",
                principalTable: "Drivers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Rides_Drivers_DriverId1",
                table: "Rides",
                column: "DriverId1",
                principalTable: "Drivers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Rides_Users_UserId",
                table: "Rides",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Rides_Users_UserId1",
                table: "Rides",
                column: "UserId1",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Rides_Drivers_DriverId",
                table: "Rides");

            migrationBuilder.DropForeignKey(
                name: "FK_Rides_Drivers_DriverId1",
                table: "Rides");

            migrationBuilder.DropForeignKey(
                name: "FK_Rides_Users_UserId",
                table: "Rides");

            migrationBuilder.DropForeignKey(
                name: "FK_Rides_Users_UserId1",
                table: "Rides");

            migrationBuilder.DropIndex(
                name: "IX_Rides_DriverId1",
                table: "Rides");

            migrationBuilder.DropIndex(
                name: "IX_Rides_UserId1",
                table: "Rides");

            migrationBuilder.DropColumn(
                name: "DriverId1",
                table: "Rides");

            migrationBuilder.DropColumn(
                name: "UserId1",
                table: "Rides");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Drivers");

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "Email", "Name", "PasswordHash", "Role" },
                values: new object[,]
                {
                    { new Guid("22222222-2222-2222-2222-222222222222"), "user@test.com", "Khách Hàng Test", "123", "User" },
                    { new Guid("33333333-3333-3333-3333-333333333333"), "driver@test.com", "Tài Xế Test", "123", "Driver" }
                });

            migrationBuilder.InsertData(
                table: "Drivers",
                columns: new[] { "Id", "AvatarUrl", "CurrentLocation", "IsOnline", "LicenseType", "UserId" },
                values: new object[] { new Guid("ab545dae-39d9-4374-90d2-5bb71c99a9f9"), null, "10.7, 106.6", true, 0, new Guid("33333333-3333-3333-3333-333333333333") });

            migrationBuilder.AddForeignKey(
                name: "FK_Rides_Drivers_DriverId",
                table: "Rides",
                column: "DriverId",
                principalTable: "Drivers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Rides_Users_UserId",
                table: "Rides",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
