using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TraSuaApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TenMigrationMoiqqaddd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HoanTat",
                table: "HoaDons");

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayRa",
                table: "HoaDons",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NgayRa",
                table: "HoaDons");

            migrationBuilder.AddColumn<bool>(
                name: "HoanTat",
                table: "HoaDons",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
