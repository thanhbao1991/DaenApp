using Microsoft.EntityFrameworkCore;
using TraSuaApp.Applicationn.Interfaces;
using TraSuaApp.Domain.Entities;
using TraSuaApp.Domain.Interfaces;
using TraSuaApp.Infrastructure.Repositories;
using TraSuaApp.Shared.Config;
namespace TraSuaApp.Infrastructure;

public partial class AppDbContext : DbContext, IAppDbContext
{
    public AppDbContext()
    {
    }
    // ✅ Hàm bắt buộc để implement IAppDbContext
    public IRepository<T> GetRepository<T>() where T : class
    {
        return new EfRepository<T>(this);
    }


    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<ChiTieuHangNgay> ChiTieuHangNgays { get; set; }
    public virtual DbSet<ChiTietHoaDonEntity> ChiTietHoaDons { get; set; }

    public virtual DbSet<ChiTietHoaDonNhapEntity> ChiTietHoaDonNhaps { get; set; }

    public virtual DbSet<ChiTietHoaDonNo> ChiTietHoaDonNos { get; set; }

    public virtual DbSet<ChiTietHoaDonThanhToan> ChiTietHoaDonThanhToans { get; set; }

    public virtual DbSet<ChiTietHoaDonTopping> ChiTietHoaDonToppings { get; set; }

    public virtual DbSet<ChiTietHoaDonVoucher> ChiTietHoaDonVouchers { get; set; }

    public virtual DbSet<ChiTietTuyChinhMon> ChiTietTuyChinhMons { get; set; }

    public virtual DbSet<CongThuc> CongThucs { get; set; }

    public virtual DbSet<CongViecNoiBo> CongViecNoiBos { get; set; }

    public virtual DbSet<ChiTietHoaDonPoint> ChiTietHoaDonPoints { get; set; }

    public virtual DbSet<HoaDon> HoaDons { get; set; }

    public virtual DbSet<HoaDonNhap> HoaDonNhaps { get; set; }

    public virtual DbSet<KhachHang> KhachHangs { get; set; }

    public virtual DbSet<KhachHangAddress> KhachHangAddresses { get; set; }

    public virtual DbSet<KhachHangPhone> KhachHangPhones { get; set; }

    public virtual DbSet<LichSuNhapXuatKho> LichSuNhapXuatKhos { get; set; }

    public virtual DbSet<Log> Logs { get; set; }

    public virtual DbSet<NguyenLieu> NguyenLieus { get; set; }

    public virtual DbSet<NhomSanPham> NhomSanPhams { get; set; }

    public virtual DbSet<PhuongThucThanhToan> PhuongThucThanhToans { get; set; }
    public virtual DbSet<KhachHangGiaBan> KhachHangGiaBans { get; set; }

    public virtual DbSet<SanPham> SanPhams { get; set; }

    public virtual DbSet<SanPhamBienThe> SanPhamBienThes { get; set; }

    public virtual DbSet<SuDungNguyenLieu> SuDungNguyenLieus { get; set; }

    public virtual DbSet<TaiKhoan> TaiKhoans { get; set; }

    public virtual DbSet<Topping> Toppings { get; set; }

    public virtual DbSet<TuyChinhMon> TuyChinhMons { get; set; }

    public virtual DbSet<Voucher> Vouchers { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlServer(Config.ConnectionString);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ChiTietHoaDonEntity>(entity =>
        {
            entity.HasIndex(e => e.HoaDonId, "IX_ChiTietHoaDons_HoaDonId");

            entity.HasIndex(e => new { e.IsDeleted, e.LastModified }, "IX_ChiTietHoaDons_IsDeleted_LastModified").IsDescending(false, true);

            entity.HasIndex(e => e.SanPhamBienTheId, "IX_ChiTietHoaDons_SanPhamBienTheId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.DonGia).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.NoteText).HasDefaultValue("");
            entity.Property(e => e.TenBienThe).HasDefaultValue("");
            entity.Property(e => e.TenSanPham).HasDefaultValue("");
            entity.Property(e => e.ThanhTien).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ToppingText).HasDefaultValue("");

            entity.HasOne(d => d.HoaDon).WithMany(p => p.ChiTietHoaDons).HasForeignKey(d => d.HoaDonId);

            entity.HasOne(d => d.SanPhamBienThe).WithMany(p => p.ChiTietHoaDons).HasForeignKey(d => d.SanPhamBienTheId);
        });

        modelBuilder.Entity<ChiTietHoaDonNhapEntity>(entity =>
        {
            entity.HasIndex(e => e.HoaDonNhapId, "IX_ChiTietHoaDonNhaps_HoaDonNhapId");

            entity.HasIndex(e => new { e.IsDeleted, e.LastModified }, "IX_ChiTietHoaDonNhaps_IsDeleted_LastModified").IsDescending(false, true);

            entity.HasIndex(e => e.NguyenLieuId, "IX_ChiTietHoaDonNhaps_NguyenLieuId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.DonGia).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.SoLuong).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.HoaDonNhap).WithMany(p => p.ChiTietHoaDonNhaps).HasForeignKey(d => d.HoaDonNhapId);

            entity.HasOne(d => d.NguyenLieu).WithMany(p => p.ChiTietHoaDonNhaps).HasForeignKey(d => d.NguyenLieuId);
        });

        modelBuilder.Entity<ChiTietHoaDonNo>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_NoHoaDons");

            entity.HasIndex(e => e.HoaDonId, "IX_NoHoaDons_HoaDonId");

            entity.HasIndex(e => new { e.IsDeleted, e.LastModified }, "IX_NoHoaDons_IsDeleted_LastModified").IsDescending(false, true);

            entity.Property(e => e.Id).ValueGeneratedNever();
            //entity.Property(e => e.SoTienDaTra).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.SoTienNo).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.HoaDon).WithMany(p => p.ChiTietHoaDonNos)
                .HasForeignKey(d => d.HoaDonId)
                .HasConstraintName("FK_NoHoaDons_HoaDons_HoaDonId");
        });

        modelBuilder.Entity<ChiTietHoaDonThanhToan>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_Payments");

            entity.HasIndex(e => e.HoaDonId, "IX_Payments_HoaDonId");

            entity.HasIndex(e => e.PhuongThucThanhToanId, "IX_Payments_PaymentMethodId");

            entity.HasIndex(e => new { e.IsDeleted, e.LastModified }, "IX_ThanhToans_IsDeleted_LastModified").IsDescending(false, true);

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.SoTien).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.HoaDon).WithMany(p => p.ChiTietHoaDonThanhToans)
                .HasForeignKey(d => d.HoaDonId)
                .HasConstraintName("FK_Payments_HoaDons_HoaDonId");

            entity.HasOne(d => d.PhuongThucThanhToan).WithMany(p => p.ChiTietHoaDonThanhToans)
                .HasForeignKey(d => d.PhuongThucThanhToanId)
                .HasConstraintName("FK_Payments_PaymentMethods_PaymentMethodId");
        });

        modelBuilder.Entity<ChiTietHoaDonTopping>(entity =>
        {
            entity.HasIndex(e => e.HoaDonId, "IX_ChiTietHoaDonToppings_HoaDonId");

            entity.HasIndex(e => new { e.IsDeleted, e.LastModified }, "IX_ChiTietHoaDonToppings_IsDeleted_LastModified").IsDescending(false, true);

            entity.HasIndex(e => e.ToppingId, "IX_ChiTietHoaDonToppings_ToppingId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Gia).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TenTopping).HasDefaultValue("");

            entity.HasOne(d => d.HoaDon).WithMany(p => p.ChiTietHoaDonToppings).HasForeignKey(d => d.HoaDonId);

            entity.HasOne(d => d.Topping).WithMany(p => p.ChiTietHoaDonToppings).HasForeignKey(d => d.ToppingId);
        });

        modelBuilder.Entity<ChiTietHoaDonVoucher>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_VoucherLogs");

            entity.HasIndex(e => e.HoaDonId, "IX_VoucherLogs_HoaDonId");

            entity.HasIndex(e => new { e.IsDeleted, e.LastModified }, "IX_VoucherLogs_IsDeleted_LastModified").IsDescending(false, true);

            entity.HasIndex(e => e.VoucherId, "IX_VoucherLogs_VoucherId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.GiaTriApDung).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TenVoucher).HasDefaultValue("");

            entity.HasOne(d => d.HoaDon).WithMany(p => p.ChiTietHoaDonVouchers)
                .HasForeignKey(d => d.HoaDonId)
                .HasConstraintName("FK_VoucherLogs_HoaDons_HoaDonId");

            entity.HasOne(d => d.Voucher).WithMany(p => p.ChiTietHoaDonVouchers)
                .HasForeignKey(d => d.VoucherId)
                .HasConstraintName("FK_VoucherLogs_Vouchers_VoucherId");
        });

        modelBuilder.Entity<ChiTietTuyChinhMon>(entity =>
        {
            entity.HasIndex(e => new { e.IsDeleted, e.LastModified }, "IX_ChiTietTuyChinhMons_IsDeleted_LastModified").IsDescending(false, true);

            entity.HasIndex(e => e.TuyChinhMonId, "IX_ChiTietTuyChinhMons_TuyChinhMonId");

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.HasOne(d => d.TuyChinhMon).WithMany(p => p.ChiTietTuyChinhMons).HasForeignKey(d => d.TuyChinhMonId);
        });

        modelBuilder.Entity<CongThuc>(entity =>
        {
            entity.HasIndex(e => new { e.IsDeleted, e.LastModified }, "IX_CongThucs_IsDeleted_LastModified").IsDescending(false, true);

            entity.HasIndex(e => e.SanPhamBienTheId, "IX_CongThucs_SanPhamBienTheId");

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.HasOne(d => d.SanPhamBienThe).WithMany(p => p.CongThucs).HasForeignKey(d => d.SanPhamBienTheId);
        });

        modelBuilder.Entity<CongViecNoiBo>(entity =>
        {
            entity.HasIndex(e => new { e.IsDeleted, e.LastModified }, "IX_CongViecNoiBos_IsDeleted_LastModified").IsDescending(false, true);

            entity.Property(e => e.Id).ValueGeneratedNever();
        });


        modelBuilder.Entity<ChiTietHoaDonPoint>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_CustomerPointLogs");

            entity.HasIndex(e => e.KhachHangId, "IX_CustomerPointLogs_KhachHangId");

            entity.HasIndex(e => new { e.IsDeleted, e.LastModified }, "IX_ChiTietHoaDonPoints_IsDeleted_LastModified").IsDescending(false, true);

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.HasOne(d => d.KhachHang).WithMany(p => p.ChiTietHoaDonPoints)
                .HasForeignKey(d => d.KhachHangId)
                .HasConstraintName("FK_CustomerPointLogs_KhachHangs_KhachHangId");
        });

        modelBuilder.Entity<HoaDon>(entity =>
        {
            entity.HasIndex(e => new { e.IsDeleted, e.LastModified }, "IX_HoaDons_IsDeleted_LastModified").IsDescending(false, true);

            entity.HasIndex(e => e.KhachHangId, "IX_HoaDons_KhachHangId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.GiamGia).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ThanhTien).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TongTien).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.KhachHang).WithMany(p => p.HoaDons).HasForeignKey(d => d.KhachHangId);
        });

        modelBuilder.Entity<HoaDonNhap>(entity =>
        {
            entity.HasIndex(e => new { e.IsDeleted, e.LastModified }, "IX_HoaDonNhaps_IsDeleted_LastModified").IsDescending(false, true);

            entity.Property(e => e.Id).ValueGeneratedNever();
        });

        modelBuilder.Entity<KhachHang>(entity =>
        {
            entity.HasIndex(e => new { e.IsDeleted, e.LastModified }, "IX_KhachHangs_IsDeleted_LastModified").IsDescending(false, true);

            entity.Property(e => e.Id).ValueGeneratedNever();
        });

        modelBuilder.Entity<KhachHangAddress>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_ShippingAddresses");

            entity.HasIndex(e => new { e.KhachHangId, e.DiaChi }, "IX_KhachHangAddresses_IdKhachHang_TenDiaChi").IsUnique();

            entity.HasIndex(e => new { e.IsDeleted, e.LastModified }, "IX_KhachHangAddresses_IsDeleted_LastModified").IsDescending(false, true);

            entity.HasIndex(e => e.KhachHangId, "IX_ShippingAddresses_KhachHangId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.DiaChi).HasMaxLength(255);

            entity.HasOne(d => d.KhachHang).WithMany(p => p.KhachHangAddresses)
                .HasForeignKey(d => d.KhachHangId)
                .HasConstraintName("FK_KhachHangAddresses_KhachHangs_IdKhachHang");
        });

        modelBuilder.Entity<KhachHangPhone>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_CustomerPhoneNumbers");

            entity.HasIndex(e => e.KhachHangId, "IX_CustomerPhoneNumbers_KhachHangId");

            entity.HasIndex(e => new { e.KhachHangId, e.SoDienThoai }, "IX_KhachHangPhones_IdKhachHang_SoDienThoai").IsUnique();

            entity.HasIndex(e => new { e.IsDeleted, e.LastModified }, "IX_KhachHangPhones_IsDeleted_LastModified").IsDescending(false, true);

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.KhachHangId).HasColumnName("KhachHangID");
            entity.Property(e => e.SoDienThoai).HasMaxLength(10);

            entity.HasOne(d => d.KhachHang).WithMany(p => p.KhachHangPhones)
                .HasForeignKey(d => d.KhachHangId)
                .HasConstraintName("FK_KhachHangPhones_KhachHangs_IdKhachHang");
        });

        modelBuilder.Entity<LichSuNhapXuatKho>(entity =>
        {
            entity.HasIndex(e => new { e.IsDeleted, e.LastModified }, "IX_LichSuNhapXuatKhos_IsDeleted_LastModified").IsDescending(false, true);

            entity.HasIndex(e => e.NguyenLieuId, "IX_LichSuNhapXuatKhos_NguyenLieuId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.SoLuong).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.NguyenLieu).WithMany(p => p.LichSuNhapXuatKhos).HasForeignKey(d => d.NguyenLieuId);
        });

        modelBuilder.Entity<Log>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Logs__3214EC07710A63D8");

            entity.HasIndex(e => new { e.IsDeleted, e.LastModified }, "IX_Logs_IsDeleted_LastModified").IsDescending(false, true);

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
            entity.HasIndex(e => new { e.IsDeleted, e.LastModified }, "IX_NguyenLieus_IsDeleted_LastModified").IsDescending(false, true);

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.GiaNhap).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TonKho).HasColumnType("decimal(18, 2)");
        });

        modelBuilder.Entity<NhomSanPham>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__NhomSanP__3214EC07C10A2B21");

            entity.HasIndex(e => e.Ten, "IX_NhomSanPham_Ten").IsUnique();

            entity.HasIndex(e => new { e.IsDeleted, e.LastModified }, "IX_NhomSanPhams_IsDeleted_LastModified").IsDescending(false, true);

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Ten).HasMaxLength(100);
        });

        modelBuilder.Entity<PhuongThucThanhToan>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_PaymentMethods");

            entity.HasIndex(e => e.Ten, "IX_PaymentMethods_Ten").IsUnique();

            entity.HasIndex(e => new { e.IsDeleted, e.LastModified }, "IX_PhuongThucThanhToans_IsDeleted_LastModified").IsDescending(false, true);

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Ten).HasMaxLength(255);
        });

        modelBuilder.Entity<SanPham>(entity =>
        {
            entity.HasIndex(e => e.Ten, "IX_SanPham_Ten").IsUnique();

            entity.HasIndex(e => new { e.IsDeleted, e.LastModified }, "IX_SanPhams_IsDeleted_LastModified").IsDescending(false, true);

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.NhomSanPhamId).HasColumnName("NhomSanPhamID");
            entity.Property(e => e.Ten).HasMaxLength(255);
            entity.Property(e => e.TichDiem).HasDefaultValue(true);

            entity.HasOne(d => d.NhomSanPham).WithMany(p => p.SanPhams)
                .HasForeignKey(d => d.NhomSanPhamId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_SanPhams_NhomSanPhams_NhomSanPhamId");
        });

        modelBuilder.Entity<SanPhamBienThe>(entity =>
        {
            entity.HasIndex(e => new { e.IsDeleted, e.LastModified }, "IX_SanPhamBienThes_IsDeleted_LastModified").IsDescending(false, true);

            entity.HasIndex(e => e.SanPhamId, "IX_SanPhamBienThes_SanPhamId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.GiaBan).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TenBienThe).HasMaxLength(255);

            entity.HasOne(d => d.SanPham).WithMany(p => p.SanPhamBienThes).HasForeignKey(d => d.SanPhamId);
        });

        modelBuilder.Entity<SuDungNguyenLieu>(entity =>
        {
            entity.HasIndex(e => e.CongThucId, "IX_SuDungNguyenLieus_CongThucId");

            entity.HasIndex(e => new { e.IsDeleted, e.LastModified }, "IX_SuDungNguyenLieus_IsDeleted_LastModified").IsDescending(false, true);

            entity.HasIndex(e => e.NguyenLieuId, "IX_SuDungNguyenLieus_NguyenLieuId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.SoLuong).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.CongThuc).WithMany(p => p.SuDungNguyenLieus).HasForeignKey(d => d.CongThucId);

            entity.HasOne(d => d.NguyenLieu).WithMany(p => p.SuDungNguyenLieus).HasForeignKey(d => d.NguyenLieuId);
        });

        modelBuilder.Entity<TaiKhoan>(entity =>
        {
            entity.HasIndex(e => e.TenDangNhap, "IX_TaiKhoan_TenDangNhap").IsUnique();

            entity.HasIndex(e => new { e.IsDeleted, e.LastModified }, "IX_TaiKhoans_IsDeleted_LastModified").IsDescending(false, true);

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.TenDangNhap).HasMaxLength(255);
        });

        modelBuilder.Entity<Topping>(entity =>
        {
            entity.HasIndex(e => e.Ten, "IX_Topping_Ten").IsUnique();

            entity.HasIndex(e => new { e.IsDeleted, e.LastModified }, "IX_Toppings_IsDeleted_LastModified").IsDescending(false, true);

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Gia).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Ten).HasMaxLength(100);

            entity.HasMany(d => d.NhomSanPhams).WithMany(p => p.Toppings)
                .UsingEntity<Dictionary<string, object>>(
                    "ToppingNhomSanPhamLink",
                    r => r.HasOne<NhomSanPham>().WithMany()
                        .HasForeignKey("NhomSanPhamId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_ToppingNhomSanPhamLinks_NhomSanPham"),
                    l => l.HasOne<Topping>().WithMany()
                        .HasForeignKey("ToppingId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_ToppingNhomSanPhamLinks_Topping"),
                    j =>
                    {
                        j.HasKey("ToppingId", "NhomSanPhamId");
                        j.ToTable("ToppingNhomSanPhamLinks");
                        j.IndexerProperty<Guid>("ToppingId").HasColumnName("ToppingID");
                        j.IndexerProperty<Guid>("NhomSanPhamId").HasColumnName("NhomSanPhamID");
                    });
        });

        modelBuilder.Entity<TuyChinhMon>(entity =>
        {
            entity.HasIndex(e => new { e.IsDeleted, e.LastModified }, "IX_TuyChinhMons_IsDeleted_LastModified").IsDescending(false, true);

            entity.Property(e => e.Id).ValueGeneratedNever();
        });

        modelBuilder.Entity<Voucher>(entity =>
        {
            entity.HasIndex(e => new { e.IsDeleted, e.LastModified }, "IX_Vouchers_IsDeleted_LastModified").IsDescending(false, true);

            entity.HasIndex(e => e.Ten, "IX_Vouchers_Ten").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.DieuKienToiThieu).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.GiaTri).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Ten).HasMaxLength(255);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
