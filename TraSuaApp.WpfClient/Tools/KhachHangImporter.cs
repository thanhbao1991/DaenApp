using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using Dapper;
using Microsoft.Data.SqlClient;

public class OldKhachHang
{
    public int IdKhachHang { get; set; }
    public string TenKhachHang { get; set; } = "";
    public string DienThoai { get; set; } = "";
    public string DiaChi { get; set; } = "";
    public bool Voucher { get; set; }
}

public class KhachHangImporter
{
    private readonly string _oldConn;
    private readonly string _newConn;
    private int _fakePhoneCounter = 1;

    public KhachHangImporter(string oldConnectionString, string newConnectionString)
    {
        _oldConn = oldConnectionString;
        _newConn = newConnectionString;
    }

    public async Task ImportAsync()
    {
        using var oldDb = new SqlConnection(_oldConn);
        using var newDb = new SqlConnection(_newConn);

        // 🟟 Lấy OldId lớn nhất hiện tại
        var lastOldId = await newDb.ExecuteScalarAsync<int?>(@"
            SELECT ISNULL(MAX(OldId), 0) FROM KhachHangs
        ") ?? 0;

        // 🟟 Lấy danh sách KH mới
        var olds = (await oldDb.QueryAsync<OldKhachHang>(@"
            SELECT IdKhachHang, TenKhachHang, DienThoai, DiaChi, Voucher
            FROM dbo.KhachHang
            WHERE IdKhachHang > @LastOldId
        ", new { LastOldId = lastOldId })).ToList();

        if (olds.Count == 0)
        {
            MessageBox.Show("✅ Không có khách hàng mới để import.");
            return;
        }

        // 🟟 Lấy số điện thoại đã tồn tại
        var existPhones = (await newDb.QueryAsync<string>(@"
            SELECT SoDienThoai FROM KhachHangPhones
        ")).ToHashSet(StringComparer.OrdinalIgnoreCase);

        int added = 0, skipped = 0;
        var logMessages = new List<string>();

        foreach (var o in olds)
        {
            try
            {
                // ✅ Danh sách số điện thoại
                var phoneList = ParsePhones(o.DienThoai);

                // Nếu không có số hợp lệ -> tạo số ảo
                if (phoneList.Count == 0)
                {
                    string fakePhone = GenerateFakePhone(existPhones);
                    phoneList.Add(fakePhone);
                }

                // Tên khách hàng fallback theo số ĐT
                string ten = string.IsNullOrWhiteSpace(o.TenKhachHang)
                    ? phoneList[0]
                    : o.TenKhachHang.Trim();

                Guid khId = Guid.NewGuid();

                // ✅ Thêm khách hàng
                await newDb.ExecuteAsync(@"
                    INSERT INTO KhachHangs(Id, Ten, DuocNhanVoucher, OldId, IsDeleted, LastModified, CreatedAt)
                    VALUES (@Id, @Ten, @Voucher, @OldId, 0, GETDATE(), GETDATE())
                ", new
                {
                    Id = khId,
                    Ten = ten,
                    Voucher = !o.Voucher,
                    OldId = o.IdKhachHang
                });

                // ✅ Thêm số điện thoại
                foreach (var p in phoneList)
                {
                    if (!existPhones.Contains(p))
                    {
                        await newDb.ExecuteAsync(@"
                            INSERT INTO KhachHangPhones(Id, KhachHangId, SoDienThoai, IsDefault)
                            VALUES (@Id, @KhId, @Phone, @IsDefault)
                        ", new
                        {
                            Id = Guid.NewGuid(),
                            KhId = khId,
                            Phone = p,
                            IsDefault = (p == phoneList[0])
                        });
                        existPhones.Add(p);
                    }
                }

                // ✅ Thêm địa chỉ (nếu có)
                if (!string.IsNullOrWhiteSpace(o.DiaChi))
                {
                    string addr = o.DiaChi.Trim();
                    await newDb.ExecuteAsync(@"
                        INSERT INTO KhachHangAddresses(Id, KhachHangId, DiaChi, IsDefault)
                        VALUES (@Id, @KhId, @DiaChi, 1)
                    ", new
                    {
                        Id = Guid.NewGuid(),
                        KhId = khId,
                        DiaChi = addr
                    });
                }

                added++;
            }
            catch (Exception ex)
            {
                skipped++;
                logMessages.Add($"❌ Lỗi import KH {o.IdKhachHang} ({o.TenKhachHang}): {ex.Message}");
            }
        }

        // ✅ Ghi log
        var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ImportKhachHang_Simple.txt");
        File.WriteAllLines(logPath, logMessages);

        string report =
            $"🟟 Kết quả import (chỉ KH mới, có địa chỉ, không xem hóa đơn):\n" +
            $"- KH mới cần import: {olds.Count}\n" +
            $"- Thêm mới thành công: {added}\n" +
            $"- Lỗi/Bỏ qua: {skipped}\n" +
            $"- Log: {logPath}";

        MessageBox.Show(report);
    }

    // ==== Helper methods ====
    private string GenerateFakePhone(HashSet<string> existPhones)
    {
        string fake;
        do
        {
            fake = _fakePhoneCounter.ToString("D10");
            _fakePhoneCounter++;
        } while (existPhones.Contains(fake));

        return fake;
    }

    private static string CleanPhone(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "";
        var p = input.Trim();
        p = p.StartsWith("+84") ? "0" + p[3..] :
            p.StartsWith("84") ? "0" + p[2..] : p;
        p = Regex.Replace(p, @"[^\d]", "");
        return p;
    }

    private static List<string> ParsePhones(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return new List<string>();

        return Regex.Split(raw, @"[ ,./\\\-]+")
                    .Select(CleanPhone)
                    .Where(p => !string.IsNullOrEmpty(p))
                    .Where(p => p.StartsWith("0"))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
    }
}