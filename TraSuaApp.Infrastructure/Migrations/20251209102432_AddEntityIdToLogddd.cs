using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TraSuaApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEntityIdToLogddd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NguyenLieuTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NguyenLieuId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NgayGio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Loai = table.Column<int>(type: "int", nullable: false),
                    SoLuong = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DonGia = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    GhiChu = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChiTieuHangNgayId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    HoaDonId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NguyenLieuTransactions", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NguyenLieuTransactions");
        }
    }
}
