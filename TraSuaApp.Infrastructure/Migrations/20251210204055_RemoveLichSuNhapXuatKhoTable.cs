using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TraSuaApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLichSuNhapXuatKhoTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LichSuNhapXuatKhos");

            migrationBuilder.DropColumn(
                name: "IdCongThuc",
                table: "SuDungNguyenLieus");

            migrationBuilder.DropColumn(
                name: "IdNguyenLieu",
                table: "SuDungNguyenLieus");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "IdCongThuc",
                table: "SuDungNguyenLieus",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "IdNguyenLieu",
                table: "SuDungNguyenLieus",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "LichSuNhapXuatKhos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NguyenLieuId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GhiChu = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IdNguyenLieu = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Loai = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SoLuong = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ThoiGian = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LichSuNhapXuatKhos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LichSuNhapXuatKhos_NguyenLieus_NguyenLieuId",
                        column: x => x.NguyenLieuId,
                        principalTable: "NguyenLieus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LichSuNhapXuatKhos_IsDeleted_LastModified",
                table: "LichSuNhapXuatKhos",
                columns: new[] { "IsDeleted", "LastModified" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_LichSuNhapXuatKhos_NguyenLieuId",
                table: "LichSuNhapXuatKhos",
                column: "NguyenLieuId");
        }
    }
}
