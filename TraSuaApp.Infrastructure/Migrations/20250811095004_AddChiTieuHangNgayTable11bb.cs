using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TraSuaApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddChiTieuHangNgayTable11bb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "KhachHangId",
                table: "ChiTietHoaDonThanhToans",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietHoaDonThanhToans_KhachHangId",
                table: "ChiTietHoaDonThanhToans",
                column: "KhachHangId");

            migrationBuilder.AddForeignKey(
                name: "FK_ChiTietHoaDonThanhToans_KhachHangs_KhachHangId",
                table: "ChiTietHoaDonThanhToans",
                column: "KhachHangId",
                principalTable: "KhachHangs",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChiTietHoaDonThanhToans_KhachHangs_KhachHangId",
                table: "ChiTietHoaDonThanhToans");

            migrationBuilder.DropIndex(
                name: "IX_ChiTietHoaDonThanhToans_KhachHangId",
                table: "ChiTietHoaDonThanhToans");

            migrationBuilder.DropColumn(
                name: "KhachHangId",
                table: "ChiTietHoaDonThanhToans");
        }
    }
}
