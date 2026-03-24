using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DriverService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUserId1Shadow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Rides_Drivers_DriverId1",
                table: "Rides");

            migrationBuilder.DropForeignKey(
                name: "FK_Rides_Users_UserId1",
                table: "Rides");

            migrationBuilder.DropIndex(
                name: "IX_Rides_DriverId1",
                table: "Rides");

            migrationBuilder.DropColumn(
                name: "DriverId1",
                table: "Rides");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId1",
                table: "Rides",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddForeignKey(
                name: "FK_Rides_Users_UserId1",
                table: "Rides",
                column: "UserId1",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Rides_Users_UserId1",
                table: "Rides");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId1",
                table: "Rides",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DriverId1",
                table: "Rides",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Rides_DriverId1",
                table: "Rides",
                column: "DriverId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Rides_Drivers_DriverId1",
                table: "Rides",
                column: "DriverId1",
                principalTable: "Drivers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Rides_Users_UserId1",
                table: "Rides",
                column: "UserId1",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
