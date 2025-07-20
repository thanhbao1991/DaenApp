using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using Dapper;
using Microsoft.Data.SqlClient;
using TraSuaApp.WpfClient.Apis;

public class OldKhachHang
{
    public int IdKhachHang { get; set; }
    public string TenKhachHang { get; set; } = "";
    public string DienThoai { get; set; } = "";
    public string DiaChi { get; set; } = "";
    public bool Voucher { get; set; }
}

public class OldHoaDon
{
    public int IdHoaDon { get; set; }
    public int IdKhachHang { get; set; }
    public string DienThoaiGiao { get; set; } = "";
    public string DiaChiGiaoHang { get; set; } = "";
}

public class KhachHangImporter
{
    private readonly string _oldConn;
    private readonly IKhachHangApi _api;

    public KhachHangImporter(string oldConnectionString, IKhachHangApi api)
    {
        _oldConn = oldConnectionString;
        _api = api;
    }

    public async Task ImportAsync()
    {
        using var db = new SqlConnection(_oldConn);

        var olds = (await db.QueryAsync<OldKhachHang>(@"
            SELECT IdKhachHang, TenKhachHang, DienThoai, DiaChi, Voucher
            FROM dbo.KhachHang
            WHERE LEN(DienThoai) > 0
        ")).ToList();

        var orders = (await db.QueryAsync<OldHoaDon>(@"
            SELECT IdHoaDon, IdKhachHang, DienThoaiShip AS DienThoaiGiao, DiaChiShip AS DiaChiGiaoHang
            FROM dbo.HoaDon
            WHERE LEN(DienThoaiShip) > 0 OR LEN(DiaChiShip) > 0
        ")).ToList();

        var phonesByKh = orders
            .Where(o => !string.IsNullOrWhiteSpace(o.DienThoaiGiao))
            .GroupBy(o => o.IdKhachHang)
            .ToDictionary(
                g => g.Key,
                g => g.SelectMany(o => ParsePhones(o.DienThoaiGiao))
                      .Distinct(StringComparer.OrdinalIgnoreCase)
                      .ToList()
            );

        var addrsByKh = orders
            .Where(o => !string.IsNullOrWhiteSpace(o.DiaChiGiaoHang))
            .GroupBy(o => o.IdKhachHang)
            .ToDictionary(
                g => g.Key,
                g => g.Select(o => o.DiaChiGiaoHang.Trim())
                      .Distinct(StringComparer.OrdinalIgnoreCase)
                      .ToList()
            );

        var existing = await _api.GetAllAsync();
        var existPhones = existing.IsSuccess
            ? existing.Data!.SelectMany(k => k.Phones)
                             .Select(p => p.SoDienThoai.Trim())
                             .ToHashSet(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        string Normalize(string input) =>
            string.Concat(input.Where(c => !char.IsWhiteSpace(c))).ToLowerInvariant();

        int added = 0;
        foreach (var o in olds)
        {
            var phoneList = ParsePhones(o.DienThoai);
            if (phoneList.Count == 0)
                continue;

            if (existPhones.Contains(phoneList[0]))
                continue;

            var dto = new KhachHangDto
            {
                Ten = o.TenKhachHang.Trim(),
                DuocNhanVoucher = !o.Voucher,
                OldId = o.IdKhachHang,
                Phones = new List<KhachHangPhoneDto>
                {
                    new KhachHangPhoneDto {
                        SoDienThoai = phoneList[0],
                        IsDefault   = true
                    }
                },
                Addresses = new List<KhachHangAddressDto>
                {
                    new KhachHangAddressDto {
                        DiaChi    = o.DiaChi.Trim(),
                        IsDefault = true
                    }
                }
            };

            // Thêm các số phụ từ chuỗi bị tách
            foreach (var p in phoneList.Skip(1))
            {
                dto.Phones.Add(new KhachHangPhoneDto
                {
                    SoDienThoai = p,
                    IsDefault = false
                });
            }

            // Thêm điện thoại phụ từ hóa đơn
            if (phonesByKh.TryGetValue(o.IdKhachHang, out var extraPhones))
            {
                foreach (var p in extraPhones)
                {
                    if (!dto.Phones.Any(x => string.Equals(x.SoDienThoai, p, StringComparison.OrdinalIgnoreCase)))
                    {
                        dto.Phones.Add(new KhachHangPhoneDto
                        {
                            SoDienThoai = p,
                            IsDefault = false
                        });
                    }
                }
            }

            // Thêm địa chỉ phụ
            if (addrsByKh.TryGetValue(o.IdKhachHang, out var extraAddrs))
            {
                var knownAddrs = dto.Addresses
                    .Select(a => Normalize(a.DiaChi))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                foreach (var a in extraAddrs)
                {
                    var norm = Normalize(a);
                    if (knownAddrs.Contains(norm))
                        continue;

                    dto.Addresses.Add(new KhachHangAddressDto
                    {
                        DiaChi = a,
                        IsDefault = false
                    });
                    knownAddrs.Add(norm);
                }
            }

            var res = await _api.CreateAsync(dto);
            if (res.IsSuccess)
            {
                added++;
                foreach (var p in dto.Phones)
                    existPhones.Add(p.SoDienThoai);
            }
            else
            {
                Debug.WriteLine($"Import lỗi {dto.Ten}: {res.Message}");
            }
        }

        MessageBox.Show($"Import hoàn thành: tổng {olds.Count} khách, thêm mới {added} khách.");
    }

    private static List<string> ParsePhones(string raw)
    {
        return Regex.Split(raw, @"[ ,./\\\-]+")
                    .Select(p => p.Trim())
                    .Where(p => Regex.IsMatch(p, @"^0\d{8,10}$"))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
    }
}