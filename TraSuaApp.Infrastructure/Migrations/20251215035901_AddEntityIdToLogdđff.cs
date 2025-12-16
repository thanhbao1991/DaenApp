using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TraSuaApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEntityIdToLogdđff : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Bỏ FK cũ: SuDungNguyenLieus -> NguyenLieus
            migrationBuilder.DropForeignKey(
                name: "FK_SuDungNguyenLieus_NguyenLieus_NguyenLieuId",
                table: "SuDungNguyenLieus");

            // 2. Thêm cột backup giữ lại link cũ sang NguyenLieus
            migrationBuilder.AddColumn<Guid>(
                name: "NguyenLieuId1",
                table: "SuDungNguyenLieus",
                type: "uniqueidentifier",
                nullable: true);

            // 2.1. Copy toàn bộ giá trị hiện tại của NguyenLieuId sang NguyenLieuId1
            migrationBuilder.Sql(@"
                UPDATE s
                SET NguyenLieuId1 = NguyenLieuId
                FROM SuDungNguyenLieus s
            ");

            // 3. Thêm cột mới trên NguyenLieus + ChiTietHoaDonThanhToans
            migrationBuilder.AddColumn<decimal>(
                name: "HeSoQuyDoiBanHang",
                table: "NguyenLieus",
                type: "decimal(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "NguyenLieuBanHangId",
                table: "NguyenLieus",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "NguyenLieuBanHangId",
                table: "ChiTietHoaDonThanhToans",
                type: "uniqueidentifier",
                nullable: true);

            // 4. Tạo bảng NguyenLieuBanHangs
            migrationBuilder.CreateTable(
                name: "NguyenLieuBanHangs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Ten = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TenPhienDich = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DangSuDung = table.Column<bool>(type: "bit", nullable: false),
                    DonViTinh = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TonKho = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NguyenLieuBanHangs", x => x.Id);
                });

            // 4.1 COPY DATA từ NguyenLieus sang NguyenLieuBanHangs, giữ NGUYÊN Id
            migrationBuilder.Sql(@"
                INSERT INTO NguyenLieuBanHangs
                    (Id, Ten, TenPhienDich, DangSuDung, DonViTinh, TonKho, CreatedAt, DeletedAt, IsDeleted, LastModified)
                SELECT
                    Id,
                    Ten,           -- tạm: Ten và TenPhienDich giống nhau, sau này anh chỉnh lại
                    Ten,
                    DangSuDung,
                    DonViTinh,
                    TonKho,
                    CreatedAt,
                    DeletedAt,
                    IsDeleted,
                    LastModified
                FROM NguyenLieus
            ");

            // 5. Index
            migrationBuilder.CreateIndex(
                name: "IX_SuDungNguyenLieus_NguyenLieuId1",
                table: "SuDungNguyenLieus",
                column: "NguyenLieuId1");

            migrationBuilder.CreateIndex(
                name: "IX_NguyenLieus_NguyenLieuBanHangId",
                table: "NguyenLieus",
                column: "NguyenLieuBanHangId");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietHoaDonThanhToans_NguyenLieuBanHangId",
                table: "ChiTietHoaDonThanhToans",
                column: "NguyenLieuBanHangId");

            // 6. FK mới
            migrationBuilder.AddForeignKey(
                name: "FK_ChiTietHoaDonThanhToans_NguyenLieuBanHangs_NguyenLieuBanHangId",
                table: "ChiTietHoaDonThanhToans",
                column: "NguyenLieuBanHangId",
                principalTable: "NguyenLieuBanHangs",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_NguyenLieus_NguyenLieuBanHangs_NguyenLieuBanHangId",
                table: "NguyenLieus",
                column: "NguyenLieuBanHangId",
                principalTable: "NguyenLieuBanHangs",
                principalColumn: "Id");

            // SuDungNguyenLieus.NguyenLieuId giờ trỏ sang NguyenLieuBanHangs
            migrationBuilder.AddForeignKey(
                name: "FK_SuDungNguyenLieus_NguyenLieuBanHangs_NguyenLieuId",
                table: "SuDungNguyenLieus",
                column: "NguyenLieuId",
                principalTable: "NguyenLieuBanHangs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // SuDungNguyenLieus.NguyenLieuId1 giữ link cũ sang NguyenLieus
            migrationBuilder.AddForeignKey(
                name: "FK_SuDungNguyenLieus_NguyenLieus_NguyenLieuId1",
                table: "SuDungNguyenLieus",
                column: "NguyenLieuId1",
                principalTable: "NguyenLieus",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChiTietHoaDonThanhToans_NguyenLieuBanHangs_NguyenLieuBanHangId",
                table: "ChiTietHoaDonThanhToans");

            migrationBuilder.DropForeignKey(
                name: "FK_NguyenLieus_NguyenLieuBanHangs_NguyenLieuBanHangId",
                table: "NguyenLieus");

            migrationBuilder.DropForeignKey(
                name: "FK_SuDungNguyenLieus_NguyenLieuBanHangs_NguyenLieuId",
                table: "SuDungNguyenLieus");

            migrationBuilder.DropForeignKey(
                name: "FK_SuDungNguyenLieus_NguyenLieus_NguyenLieuId1",
                table: "SuDungNguyenLieus");

            // (optional) copy lại data về NguyenLieuId cũ trước khi drop cột backup
            migrationBuilder.Sql(@"
                UPDATE s
                SET NguyenLieuId = COALESCE(NguyenLieuId, NguyenLieuId1)
                FROM SuDungNguyenLieus s
            ");

            migrationBuilder.DropTable(
                name: "NguyenLieuBanHangs");

            migrationBuilder.DropIndex(
                name: "IX_SuDungNguyenLieus_NguyenLieuId1",
                table: "SuDungNguyenLieus");

            migrationBuilder.DropIndex(
                name: "IX_NguyenLieus_NguyenLieuBanHangId",
                table: "NguyenLieus");

            migrationBuilder.DropIndex(
                name: "IX_ChiTietHoaDonThanhToans_NguyenLieuBanHangId",
                table: "ChiTietHoaDonThanhToans");

            migrationBuilder.DropColumn(
                name: "NguyenLieuId1",
                table: "SuDungNguyenLieus");

            migrationBuilder.DropColumn(
                name: "HeSoQuyDoiBanHang",
                table: "NguyenLieus");

            migrationBuilder.DropColumn(
                name: "NguyenLieuBanHangId",
                table: "NguyenLieus");

            migrationBuilder.DropColumn(
                name: "NguyenLieuBanHangId",
                table: "ChiTietHoaDonThanhToans");

            migrationBuilder.AddForeignKey(
                name: "FK_SuDungNguyenLieus_NguyenLieus_NguyenLieuId",
                table: "SuDungNguyenLieus",
                column: "NguyenLieuId",
                principalTable: "NguyenLieus",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}