@echo off
setlocal

set "path=TraSuaApp.Domain\Entities"

if not exist %path% mkdir %path%

dotnet new class -n KhachHang -o %path%
dotnet new class -n ShippingAddress -o %path%
dotnet new class -n CustomerPhoneNumber -o %path%
dotnet new class -n TaiKhoan -o %path%
dotnet new class -n SanPham -o %path%
dotnet new class -n SanPhamBienThe -o %path%
dotnet new class -n Topping -o %path%
dotnet new class -n TuyChinhMon -o %path%
dotnet new class -n ChiTietTuyChinhMon -o %path%
dotnet new class -n HoaDon -o %path%
dotnet new class -n ChiTietHoaDon -o %path%
dotnet new class -n ChiTietHoaDonTopping -o %path%
dotnet new class -n NguyenLieu -o %path%
dotnet new class -n CongThuc -o %path%
dotnet new class -n SuDungNguyenLieu -o %path%
dotnet new class -n HoaDonNhap -o %path%
dotnet new class -n ChiTietHoaDonNhap -o %path%
dotnet new class -n LichSuNhapXuatKho -o %path%
dotnet new class -n Payment -o %path%
dotnet new class -n PaymentMethod -o %path%
dotnet new class -n NoHoaDon -o %path%
dotnet new class -n CustomerPoint -o %path%
dotnet new class -n CustomerPointLog -o %path%
dotnet new class -n Voucher -o %path%
dotnet new class -n VoucherLog -o %path%
dotnet new class -n CongViecNoiBo -o %path%
dotnet new class -n LichSuChinhSua -o %path%

echo Tạo file trống hoàn tất!
pause