using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TraSuaAppWeb.Data;
using TraSuaAppWeb.Models;


namespace TraSuaAppWeb.Pages
{
    public class ShipperModel : PageModel
    {
        private readonly AppDbContext _context;
        public ShipperModel(AppDbContext context)
        {
            _context = context;
        }
        public List<HoaDon> HoaDonList { get; set; }
        public async Task OnGetAsync()
        {
            HoaDonList = await _context.HoaDon
                .Where(x => x.NgayHoaDon.Date == DateTime.Now.Date
                && x.IdNhomHoaDon == 3 && x.DaThanhToan == false
                )
                .OrderByDescending(x => x.NgayHoaDon)
                .ToListAsync();
        }
    }
}