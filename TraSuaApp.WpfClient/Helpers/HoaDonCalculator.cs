using TraSuaApp.Shared.Dtos;

public static class HoaDonCalculator
{
    private static readonly HashSet<string> ExcludedGroups = new()
    {
        "Thuốc lá",
        "Nước lon",
        "Ăn vặt"
    };

    public static int TinhTongSoSanPham(
        IEnumerable<ChiTietHoaDonDto> chiTiet,
        IEnumerable<SanPhamDto> sanPhamList)
    {
        if (chiTiet == null) return 0;

        var bienTheDict = sanPhamList
            .SelectMany(x => x.BienThe)
            .ToDictionary(x => x.Id, x => x.SanPhamId);

        var sanPhamDict = sanPhamList.ToDictionary(x => x.Id);

        int tong = 0;

        foreach (var ct in chiTiet)
        {
            if (!bienTheDict.TryGetValue(ct.SanPhamIdBienThe, out var spId)) continue;
            if (!sanPhamDict.TryGetValue(spId, out var sp)) continue;

            if (!ExcludedGroups.Contains(sp.TenNhomSanPham))
                tong += ct.SoLuong;
        }

        return tong;
    }
}