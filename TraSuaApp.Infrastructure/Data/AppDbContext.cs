using Microsoft.EntityFrameworkCore;
using TraSuaApp.Domain.Entities;

namespace TraSuaApp.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        //ToppingNhomSanPham
        //       modelBuilder.Entity<KhachHangAddress>()
        //.ToTable("KhachHangAddresses");
        //    modelBuilder.Entity<ToppingNhomSanPham>()
        //.ToTable("ToppingNhomSanPhams");


        modelBuilder.Entity<ToppingNhomSanPham>()
    .HasKey(x => new { x.IdTopping, x.IdNhomSanPham });
        modelBuilder.Entity<ToppingNhomSanPham>()
            .HasOne(x => x.Topping)
            .WithMany(x => x.DanhSachNhomSanPham)
            .HasForeignKey(x => x.IdTopping)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ToppingNhomSanPham>()
            .HasOne(x => x.NhomSanPham)
            .WithMany(x => x.DanhSachTopping)
            .HasForeignKey(x => x.IdNhomSanPham)
            .OnDelete(DeleteBehavior.Cascade);

        NewMethod(modelBuilder);
    }

    private static void NewMethod(ModelBuilder modelBuilder)
    {
        // Topping
        modelBuilder.Entity<Topping>().Property(x => x.Gia).HasPrecision(18, 2);

        // Voucher
        modelBuilder.Entity<Voucher>().Property(x => x.GiaTri).HasPrecision(18, 2);

        // VoucherLog
        modelBuilder.Entity<VoucherLog>().Property(x => x.GiaTriApDung).HasPrecision(18, 2);
        // ChiTietHoaDon
        modelBuilder.Entity<ChiTietHoaDon>().Property(x => x.DonGia).HasPrecision(18, 2);
        modelBuilder.Entity<ChiTietHoaDon>().Property(x => x.ThanhTien).HasPrecision(18, 2);

        // ChiTietHoaDonNhap
        modelBuilder.Entity<ChiTietHoaDonNhap>().Property(x => x.DonGia).HasPrecision(18, 2);
        modelBuilder.Entity<ChiTietHoaDonNhap>().Property(x => x.SoLuong).HasPrecision(18, 2);

        // ChiTietHoaDonTopping
        modelBuilder.Entity<ChiTietHoaDonTopping>().Property(x => x.Gia).HasPrecision(18, 2);

        // HoaDon
        modelBuilder.Entity<HoaDon>().Property(x => x.GiamGia).HasPrecision(18, 2);
        modelBuilder.Entity<HoaDon>().Property(x => x.ThanhTien).HasPrecision(18, 2);
        modelBuilder.Entity<HoaDon>().Property(x => x.TongTien).HasPrecision(18, 2);

        // LichSuNhapXuatKho
        modelBuilder.Entity<LichSuNhapXuatKho>().Property(x => x.SoLuong).HasPrecision(18, 2);

        // NguyenLieu
        modelBuilder.Entity<NguyenLieu>().Property(x => x.GiaNhap).HasPrecision(18, 2);
        modelBuilder.Entity<NguyenLieu>().Property(x => x.TonKho).HasPrecision(18, 2);

        // NoHoaDon
        modelBuilder.Entity<NoHoaDon>().Property(x => x.SoTienNo).HasPrecision(18, 2);
        modelBuilder.Entity<NoHoaDon>().Property(x => x.SoTienDaTra).HasPrecision(18, 2);

        // Payment
        modelBuilder.Entity<Payment>().Property(x => x.SoTien).HasPrecision(18, 2);

        // SanPhamBienThe
        modelBuilder.Entity<SanPhamBienThe>().Property(x => x.GiaBan).HasPrecision(18, 2);

        // SuDungNguyenLieu
        modelBuilder.Entity<SuDungNguyenLieu>().Property(x => x.SoLuong).HasPrecision(18, 2);
    }

    // DbSet cho toàn bộ entity đã khai báo
    public DbSet<LogEntry> Logs => Set<LogEntry>();
    public DbSet<ToppingNhomSanPham> ToppingNhomSanPhams => Set<ToppingNhomSanPham>();
    public DbSet<KhachHang> KhachHangs => Set<KhachHang>();
    public DbSet<KhachHangAddress> KhachHangAddresses => Set<KhachHangAddress>();
    public DbSet<KhachHangPhone> KhachHangPhones => Set<KhachHangPhone>();
    public DbSet<TaiKhoan> TaiKhoans => Set<TaiKhoan>();
    public DbSet<NhomSanPham> NhomSanPhams { get; set; }
    public DbSet<SanPham> SanPhams => Set<SanPham>();
    public DbSet<SanPhamBienThe> SanPhamBienThes => Set<SanPhamBienThe>();
    public DbSet<Topping> Toppings => Set<Topping>();
    public DbSet<TuyChinhMon> TuyChinhMons => Set<TuyChinhMon>();
    public DbSet<ChiTietTuyChinhMon> ChiTietTuyChinhMons => Set<ChiTietTuyChinhMon>();

    public DbSet<HoaDon> HoaDons => Set<HoaDon>();
    public DbSet<ChiTietHoaDon> ChiTietHoaDons => Set<ChiTietHoaDon>();
    public DbSet<ChiTietHoaDonTopping> ChiTietHoaDonToppings => Set<ChiTietHoaDonTopping>();

    public DbSet<NguyenLieu> NguyenLieus => Set<NguyenLieu>();
    public DbSet<CongThuc> CongThucs => Set<CongThuc>();
    public DbSet<SuDungNguyenLieu> SuDungNguyenLieus => Set<SuDungNguyenLieu>();
    public DbSet<HoaDonNhap> HoaDonNhaps => Set<HoaDonNhap>();
    public DbSet<ChiTietHoaDonNhap> ChiTietHoaDonNhaps => Set<ChiTietHoaDonNhap>();
    public DbSet<LichSuNhapXuatKho> LichSuNhapXuatKhos => Set<LichSuNhapXuatKho>();

    public DbSet<PaymentMethod> PaymentMethods => Set<PaymentMethod>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<NoHoaDon> NoHoaDons => Set<NoHoaDon>();

    public DbSet<CustomerPoint> CustomerPoints => Set<CustomerPoint>();
    public DbSet<CustomerPointLog> CustomerPointLogs => Set<CustomerPointLog>();
    public DbSet<Voucher> Vouchers => Set<Voucher>();
    public DbSet<VoucherLog> VoucherLogs => Set<VoucherLog>();

    public DbSet<CongViecNoiBo> CongViecNoiBos => Set<CongViecNoiBo>();
    public DbSet<LichSuChinhSua> LichSuChinhSuas => Set<LichSuChinhSua>();


}