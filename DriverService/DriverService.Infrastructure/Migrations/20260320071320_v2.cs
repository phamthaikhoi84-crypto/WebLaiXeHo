using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DriverService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class v2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("bd42977b-9968-4cd7-8b6f-1853ec81e0ef"));

            migrationBuilder.InsertData(
                table: "Drivers",
                columns: new[] { "Id", "AvatarUrl", "CurrentLocation", "IsOnline", "UserId" },
                values: new object[] { new Guid("8e16c31e-4426-46b6-8d5f-e0c3932c9306"), null, "10.7, 106.6", true, new Guid("33333333-3333-3333-3333-333333333333") });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("8e16c31e-4426-46b6-8d5f-e0c3932c9306"));

            migrationBuilder.InsertData(
                table: "Drivers",
                columns: new[] { "Id", "AvatarUrl", "CurrentLocation", "IsOnline", "UserId" },
                values: new object[] { new Guid("bd42977b-9968-4cd7-8b6f-1853ec81e0ef"), null, "10.7, 106.6", true, new Guid("33333333-3333-3333-3333-333333333333") });
        }
    }
}
