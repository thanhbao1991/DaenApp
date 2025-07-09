using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TraSuaApp.Infrastructure.Migrations
{
    public partial class Remove_IdNhom_From_SanPhams : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Xoá ràng buộc FK nếu có
            migrationBuilder.DropForeignKey(
                name: "FK_SanPhams_NhomSanPhams",
                table: "SanPhams");

            // Xoá cột IdNhom
            migrationBuilder.DropColumn(
                name: "IdNhom",
                table: "SanPhams");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Thêm lại cột IdNhom
            migrationBuilder.AddColumn<Guid>(
                name: "IdNhom",
                table: "SanPhams",
                type: "uniqueidentifier",
                nullable: true);

            // Thêm lại FK nếu cần
            migrationBuilder.AddForeignKey(
                name: "FK_SanPhams_NhomSanPhams",
                table: "SanPhams",
                column: "IdNhom",
                principalTable: "NhomSanPhams",
                principalColumn: "Id");
        }
    }
}