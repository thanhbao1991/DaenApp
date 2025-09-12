using Microsoft.EntityFrameworkCore;
using TraSuaAppWeb.Models;

namespace TraSuaAppWeb.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }
        public DbSet<KhachHang> KhachHang { get; set; }
        public DbSet<SanPham> SanPham { get; set; }
        public DbSet<HoaDon> HoaDon { get; set; }
        public DbSet<ChiTietHoaDon> ChiTietHoaDon { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ChiTietHoaDon>()
                .HasOne(c => c.HoaDon)
                .WithMany(h => h.ChiTietHoaDon)
                .HasForeignKey(c => c.IdHoaDon);
        }
    }
}