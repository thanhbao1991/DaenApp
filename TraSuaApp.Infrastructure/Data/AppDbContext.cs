using Microsoft.EntityFrameworkCore;
using TraSuaApp.Domain.Entities;

namespace TraSuaApp.Infrastructure.Data;

public partial class AppDbContext : DbContext
{
    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<ChiTietHoaDon> ChiTietHoaDons { get; set; }

    public virtual DbSet<ChiTietHoaDonNhap> ChiTietHoaDonNhaps { get; set; }

    public virtual DbSet<ChiTietHoaDonTopping> ChiTietHoaDonToppings { get; set; }

    public virtual DbSet<ChiTietTuyChinhMon> ChiTietTuyChinhMons { get; set; }

    public virtual DbSet<CongThuc> CongThucs { get; set; }

    public virtual DbSet<CongViecNoiBo> CongViecNoiBos { get; set; }

    public virtual DbSet<CustomerPoint> CustomerPoints { get; set; }

    public virtual DbSet<CustomerPointLog> CustomerPointLogs { get; set; }

    public virtual DbSet<HoaDon> HoaDons { get; set; }

    public virtual DbSet<HoaDonNhap> HoaDonNhaps { get; set; }

    public virtual DbSet<KhachHang> KhachHangs { get; set; }

    public virtual DbSet<KhachHangAddress> KhachHangAddresses { get; set; }

    public virtual DbSet<KhachHangPhone> KhachHangPhones { get; set; }

    public virtual DbSet<LichSuChinhSua> LichSuChinhSuas { get; set; }

    public virtual DbSet<LichSuNhapXuatKho> LichSuNhapXuatKhos { get; set; }

    public virtual DbSet<Log> Logs { get; set; }

    public virtual DbSet<NguyenLieu> NguyenLieus { get; set; }

    public virtual DbSet<NhomSanPham> NhomSanPhams { get; set; }

    public virtual DbSet<NoHoaDon> NoHoaDons { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<PaymentMethod> PaymentMethods { get; set; }

    public virtual DbSet<SanPham> SanPhams { get; set; }

    public virtual DbSet<SanPhamBienThe> SanPhamBienThes { get; set; }

    public virtual DbSet<SuDungNguyenLieu> SuDungNguyenLieus { get; set; }

    public virtual DbSet<TaiKhoan> TaiKhoans { get; set; }

    public virtual DbSet<Topping> Toppings { get; set; }

    public virtual DbSet<TuyChinhMon> TuyChinhMons { get; set; }

    public virtual DbSet<Voucher> Vouchers { get; set; }

    public virtual DbSet<VoucherLog> VoucherLogs { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlServer("Server=.;Database=TraSuaAppDb;Trusted_Connection=True;TrustServerCertificate=True");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ChiTietHoaDon>(entity =>
        {
            entity.HasIndex(e => e.HoaDonId, "IX_ChiTietHoaDons_HoaDonId");

            entity.HasIndex(e => e.SanPhamBienTheId, "IX_ChiTietHoaDons_SanPhamBienTheId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.DonGia).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ThanhTien).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.HoaDon).WithMany(p => p.ChiTietHoaDons).HasForeignKey(d => d.HoaDonId);

            entity.HasOne(d => d.SanPhamBienThe).WithMany(p => p.ChiTietHoaDons).HasForeignKey(d => d.SanPhamBienTheId);
        });

        modelBuilder.Entity<ChiTietHoaDonNhap>(entity =>
        {
            entity.HasIndex(e => e.HoaDonNhapId, "IX_ChiTietHoaDonNhaps_HoaDonNhapId");

            entity.HasIndex(e => e.NguyenLieuId, "IX_ChiTietHoaDonNhaps_NguyenLieuId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.DonGia).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.SoLuong).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.HoaDonNhap).WithMany(p => p.ChiTietHoaDonNhaps).HasForeignKey(d => d.HoaDonNhapId);

            entity.HasOne(d => d.NguyenLieu).WithMany(p => p.ChiTietHoaDonNhaps).HasForeignKey(d => d.NguyenLieuId);
        });

        modelBuilder.Entity<ChiTietHoaDonTopping>(entity =>
        {
            entity.HasIndex(e => e.HoaDonId, "IX_ChiTietHoaDonToppings_HoaDonId");

            entity.HasIndex(e => e.ToppingId, "IX_ChiTietHoaDonToppings_ToppingId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Gia).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.HoaDon).WithMany(p => p.ChiTietHoaDonToppings).HasForeignKey(d => d.HoaDonId);

            entity.HasOne(d => d.Topping).WithMany(p => p.ChiTietHoaDonToppings).HasForeignKey(d => d.ToppingId);
        });

        modelBuilder.Entity<ChiTietTuyChinhMon>(entity =>
        {
            entity.HasIndex(e => e.TuyChinhMonId, "IX_ChiTietTuyChinhMons_TuyChinhMonId");

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.HasOne(d => d.TuyChinhMon).WithMany(p => p.ChiTietTuyChinhMons).HasForeignKey(d => d.TuyChinhMonId);
        });

        modelBuilder.Entity<CongThuc>(entity =>
        {
            entity.HasIndex(e => e.SanPhamBienTheId, "IX_CongThucs_SanPhamBienTheId");

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.HasOne(d => d.SanPhamBienThe).WithMany(p => p.CongThucs).HasForeignKey(d => d.SanPhamBienTheId);
        });

        modelBuilder.Entity<CongViecNoiBo>(entity =>
        {
            entity.HasIndex(e => e.NguoiTaoId, "IX_CongViecNoiBos_NguoiTaoId");

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.HasOne(d => d.NguoiTao).WithMany(p => p.CongViecNoiBos).HasForeignKey(d => d.NguoiTaoId);
        });

        modelBuilder.Entity<CustomerPoint>(entity =>
        {
            entity.HasIndex(e => e.KhachHangId, "IX_CustomerPoints_KhachHangId");

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.HasOne(d => d.KhachHang).WithMany(p => p.CustomerPoints).HasForeignKey(d => d.KhachHangId);
        });

        modelBuilder.Entity<CustomerPointLog>(entity =>
        {
            entity.HasIndex(e => e.KhachHangId, "IX_CustomerPointLogs_KhachHangId");

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.HasOne(d => d.KhachHang).WithMany(p => p.CustomerPointLogs).HasForeignKey(d => d.KhachHangId);
        });

        modelBuilder.Entity<HoaDon>(entity =>
        {
            entity.HasIndex(e => e.KhachHangId, "IX_HoaDons_KhachHangId");

            entity.HasIndex(e => e.TaiKhoanId, "IX_HoaDons_TaiKhoanId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.GiamGia).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ThanhTien).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TongTien).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.KhachHang).WithMany(p => p.HoaDons).HasForeignKey(d => d.KhachHangId);

            entity.HasOne(d => d.TaiKhoan).WithMany(p => p.HoaDons).HasForeignKey(d => d.TaiKhoanId);
        });

        modelBuilder.Entity<HoaDonNhap>(entity =>
        {
            entity.HasIndex(e => e.TaiKhoanId, "IX_HoaDonNhaps_TaiKhoanId");

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.HasOne(d => d.TaiKhoan).WithMany(p => p.HoaDonNhaps).HasForeignKey(d => d.TaiKhoanId);
        });

        modelBuilder.Entity<KhachHang>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();
        });

        modelBuilder.Entity<KhachHangAddress>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_ShippingAddresses");

            entity.HasIndex(e => e.IdKhachHang, "IX_ShippingAddress_Default")
                .IsUnique()
                .HasFilter("([IsDefault]=(1))");

            entity.HasIndex(e => e.KhachHangId, "IX_ShippingAddresses_KhachHangId");

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.HasOne(d => d.KhachHang).WithMany(p => p.KhachHangAddresses)
                .HasForeignKey(d => d.KhachHangId)
                .HasConstraintName("FK_ShippingAddresses_KhachHangs_KhachHangId");
        });

        modelBuilder.Entity<KhachHangPhone>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_CustomerPhoneNumbers");

            entity.HasIndex(e => e.IdKhachHang, "IX_CustomerPhoneNumber_Default")
                .IsUnique()
                .HasFilter("([IsDefault]=(1))");

            entity.HasIndex(e => e.KhachHangId, "IX_CustomerPhoneNumbers_KhachHangId");

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.HasOne(d => d.KhachHang).WithMany(p => p.KhachHangPhones)
                .HasForeignKey(d => d.KhachHangId)
                .HasConstraintName("FK_CustomerPhoneNumbers_KhachHangs_KhachHangId");
        });

        modelBuilder.Entity<LichSuChinhSua>(entity =>
        {
            entity.HasIndex(e => e.TaiKhoanId, "IX_LichSuChinhSuas_TaiKhoanId");

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.HasOne(d => d.TaiKhoan).WithMany(p => p.LichSuChinhSuas).HasForeignKey(d => d.TaiKhoanId);
        });

        modelBuilder.Entity<LichSuNhapXuatKho>(entity =>
        {
            entity.HasIndex(e => e.NguyenLieuId, "IX_LichSuNhapXuatKhos_NguyenLieuId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.SoLuong).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.NguyenLieu).WithMany(p => p.LichSuNhapXuatKhos).HasForeignKey(d => d.NguyenLieuId);
        });

        modelBuilder.Entity<Log>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Logs__3214EC07710A63D8");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Ip)
                .HasMaxLength(100)
                .HasColumnName("IP");
            entity.Property(e => e.Method).HasMaxLength(10);
            entity.Property(e => e.Path).HasMaxLength(500);
            entity.Property(e => e.UserId).HasMaxLength(100);
            entity.Property(e => e.UserName).HasMaxLength(200);
        });

        modelBuilder.Entity<NguyenLieu>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.GiaNhap).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TonKho).HasColumnType("decimal(18, 2)");
        });

        modelBuilder.Entity<NhomSanPham>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__NhomSanP__3214EC07C10A2B21");

            entity.HasIndex(e => e.Ten, "IX_NhomSanPham_Ten").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Ten).HasMaxLength(100);
        });

        modelBuilder.Entity<NoHoaDon>(entity =>
        {
            entity.HasIndex(e => e.HoaDonId, "IX_NoHoaDons_HoaDonId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.SoTienDaTra).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.SoTienNo).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.HoaDon).WithMany(p => p.NoHoaDons).HasForeignKey(d => d.HoaDonId);
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasIndex(e => e.HoaDonId, "IX_Payments_HoaDonId");

            entity.HasIndex(e => e.PaymentMethodId, "IX_Payments_PaymentMethodId");

            entity.HasIndex(e => e.TaiKhoanThucHienId, "IX_Payments_TaiKhoanThucHienId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.SoTien).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.HoaDon).WithMany(p => p.Payments).HasForeignKey(d => d.HoaDonId);

            entity.HasOne(d => d.PaymentMethod).WithMany(p => p.Payments).HasForeignKey(d => d.PaymentMethodId);

            entity.HasOne(d => d.TaiKhoanThucHien).WithMany(p => p.Payments).HasForeignKey(d => d.TaiKhoanThucHienId);
        });

        modelBuilder.Entity<PaymentMethod>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();
        });

        modelBuilder.Entity<SanPham>(entity =>
        {
            entity.HasIndex(e => e.Ten, "IX_SanPham_Ten").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.IdOld).HasColumnName("IdOLD");
            entity.Property(e => e.Ten).HasMaxLength(255);
            entity.Property(e => e.TichDiem).HasDefaultValue(true);

            entity.HasOne(sp => sp.IdNhomSanPhamNavigation)
         .WithMany(nsp => nsp.SanPhams)
         .HasForeignKey(sp => sp.IdNhomSanPham)
         .HasConstraintName("FK_SanPhams_NhomSanPhams");
        });

        modelBuilder.Entity<SanPhamBienThe>(entity =>
        {
            entity.HasIndex(e => e.IdSanPham, "IX_BienThe_MacDinh")
                .IsUnique()
                .HasFilter("([MacDinh]=(1))");

            entity.HasIndex(e => new { e.TenBienThe, e.IdSanPham }, "IX_BienThe_Ten_IdSanPham").IsUnique();

            entity.HasIndex(e => e.SanPhamId, "IX_SanPhamBienThes_SanPhamId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.GiaBan).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TenBienThe).HasMaxLength(255);

            entity.HasOne(d => d.SanPham).WithMany(p => p.SanPhamBienThes).HasForeignKey(d => d.SanPhamId);
        });

        modelBuilder.Entity<SuDungNguyenLieu>(entity =>
        {
            entity.HasIndex(e => e.CongThucId, "IX_SuDungNguyenLieus_CongThucId");

            entity.HasIndex(e => e.NguyenLieuId, "IX_SuDungNguyenLieus_NguyenLieuId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.SoLuong).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.CongThuc).WithMany(p => p.SuDungNguyenLieus).HasForeignKey(d => d.CongThucId);

            entity.HasOne(d => d.NguyenLieu).WithMany(p => p.SuDungNguyenLieus).HasForeignKey(d => d.NguyenLieuId);
        });

        modelBuilder.Entity<TaiKhoan>(entity =>
        {
            entity.HasIndex(e => e.TenDangNhap, "IX_TaiKhoan_TenDangNhap").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.TenDangNhap).HasMaxLength(255);
        });

        modelBuilder.Entity<Topping>(entity =>
        {
            entity.HasIndex(e => e.Ten, "IX_Topping_Ten").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Gia).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Ten).HasMaxLength(100);

            entity.HasMany(d => d.IdNhomSanPhams).WithMany(p => p.IdToppings)
                .UsingEntity<Dictionary<string, object>>(
                    "ToppingNhomSanPham",
                    r => r.HasOne<NhomSanPham>().WithMany()
                        .HasForeignKey("IdNhomSanPham")
                        .HasConstraintName("FK_ToppingNhomSanPham_NhomSanPham"),
                    l => l.HasOne<Topping>().WithMany()
                        .HasForeignKey("IdTopping")
                        .HasConstraintName("FK_ToppingNhomSanPham_Topping"),
                    j =>
                    {
                        j.HasKey("IdTopping", "IdNhomSanPham").HasName("PK_ToppingNhomSanPham");
                        j.ToTable("ToppingNhomSanPhams");
                    });
        });

        modelBuilder.Entity<TuyChinhMon>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();
        });

        modelBuilder.Entity<Voucher>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.GiaTri).HasColumnType("decimal(18, 2)");
        });

        modelBuilder.Entity<VoucherLog>(entity =>
        {
            entity.HasIndex(e => e.HoaDonId, "IX_VoucherLogs_HoaDonId");

            entity.HasIndex(e => e.VoucherId, "IX_VoucherLogs_VoucherId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.GiaTriApDung).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.HoaDon).WithMany(p => p.VoucherLogs).HasForeignKey(d => d.HoaDonId);

            entity.HasOne(d => d.Voucher).WithMany(p => p.VoucherLogs).HasForeignKey(d => d.VoucherId);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
