using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TraSuaApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddColumnsToNhomSanPha : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IdOld",
                table: "NhomSanPhams");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastModified",
                table: "NhomSanPhams",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "STT",
                table: "NhomSanPhams",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastModified",
                table: "NhomSanPhams");

            migrationBuilder.DropColumn(
                name: "STT",
                table: "NhomSanPhams");

            migrationBuilder.AddColumn<int>(
                name: "IdOld",
                table: "NhomSanPhams",
                type: "int",
                nullable: true);
        }
    }
}
