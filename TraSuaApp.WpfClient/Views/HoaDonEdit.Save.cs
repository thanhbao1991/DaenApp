using System.Windows;
using System.Windows.Input;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.WpfClient.HoaDonViews
{
    public partial class HoaDonEdit
    {
        private bool _isSaving;
        private bool newKhachHang;

        private void DongBoTatCaTopping()
        {
            if (Model.ChiTietHoaDonToppings == null)
                Model.ChiTietHoaDonToppings = new List<ChiTietHoaDonToppingDto>();

            ToppingSync.SyncAll(Model.ChiTietHoaDons, Model.ChiTietHoaDonToppings);
            UpdateTotals();
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isSaving) return;

            FrameworkElement? btn = sender as FrameworkElement;

            try
            {
                _isSaving = true;

                if (btn != null)
                    btn.IsEnabled = false;

                ErrorTextBlock.Text = "";
                Mouse.OverrideCursor = Cursors.Wait;

                bool isNew = Model.Id == Guid.Empty;

                //----------------------------------------
                // 1️⃣ Đồng bộ loại đơn
                //----------------------------------------
                if (TaiChoRadio.IsChecked == true)
                    Model.PhanLoai = "Tại Chỗ";
                else if (MuaVeRadio.IsChecked == true)
                    Model.PhanLoai = "Mv";
                else if (ShipRadio.IsChecked == true)
                    Model.PhanLoai = "Ship";
                else if (AppRadio.IsChecked == true)
                    Model.PhanLoai = "App";

                //----------------------------------------
                // 2️⃣ Đồng bộ bàn
                //----------------------------------------
                Model.TenBan = TenBanComboBox.SelectedItem?.ToString();

                if (TaiChoRadio.IsChecked == true && string.IsNullOrWhiteSpace(Model.TenBan))
                {
                    ErrorTextBlock.Text = "Tên bàn không được để trống.";
                    TenBanComboBox.IsDropDownOpen = true;
                    return;
                }

                //----------------------------------------
                // 3️⃣ Tạo khách mới nếu user nhập tay
                //----------------------------------------
                var tenKhach = KhachHangSearchBox.SearchTextBox.Text?.Trim();

                if (!string.IsNullOrWhiteSpace(tenKhach) && Model.KhachHangId == null)
                {
                    var existing = _khachHangsList
                        .FirstOrDefault(x => x.Ten.EqualsNormalized(tenKhach));

                    if (existing != null)
                    {
                        Model.KhachHangId = existing.Id;
                        Model.TenKhachHangText = existing.Ten;
                    }
                    else
                    {
                        var kh = new KhachHangDto
                        {
                            Id = Guid.NewGuid(),
                            Ten = tenKhach,
                            CreatedAt = DateTime.Now,
                            LastModified = DateTime.Now,
                            Addresses = new List<KhachHangAddressDto>(),
                            Phones = new List<KhachHangPhoneDto>()
                        };

                        _khachHangsList.Add(kh);

                        Model.KhachHangId = kh.Id;
                        Model.TenKhachHangText = kh.Ten;
                        newKhachHang = true;
                    }
                }

                //----------------------------------------
                // 4️⃣ Đồng bộ địa chỉ + điện thoại
                //----------------------------------------
                if (Model.KhachHangId != null)
                {
                    if (string.IsNullOrWhiteSpace(DiaChiComboBox.Text))
                    {
                        ErrorTextBlock.Text = "Địa chỉ khách hàng không được để trống.";
                        DiaChiComboBox.IsDropDownOpen = true;
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(DienThoaiComboBox.Text))
                    {
                        ErrorTextBlock.Text = "Số điện thoại khách hàng không được để trống.";
                        DienThoaiComboBox.IsDropDownOpen = true;
                        return;
                    }

                    Model.DiaChiText = DiaChiComboBox.Text.Trim();
                    Model.SoDienThoaiText = DienThoaiComboBox.Text.Trim();

                    //----------------------------------------
                    // Lưu địa chỉ + điện thoại mới vào khách
                    //----------------------------------------
                    var kh = _khachHangsList.FirstOrDefault(x => x.Id == Model.KhachHangId);

                    if (kh != null)
                    {
                        if (!kh.Addresses.Any(a => a.DiaChi.EqualsNormalized(Model.DiaChiText)))
                        {
                            kh.Addresses.Add(new KhachHangAddressDto
                            {
                                DiaChi = Model.DiaChiText,
                                IsDefault = false
                            });
                        }

                        if (!kh.Phones.Any(p => p.SoDienThoai == Model.SoDienThoaiText))
                        {
                            kh.Phones.Add(new KhachHangPhoneDto
                            {
                                SoDienThoai = Model.SoDienThoaiText,
                                IsDefault = false
                            });
                        }
                    }
                }

                //----------------------------------------
                // 5️⃣ Xóa dòng sản phẩm số lượng 0
                //----------------------------------------
                foreach (var item in Model.ChiTietHoaDons.Where(x => x.SoLuong <= 0).ToList())
                    Model.ChiTietHoaDons.Remove(item);

                //----------------------------------------
                // 6️⃣ Không cho lưu hóa đơn rỗng
                //----------------------------------------
                if (!Model.ChiTietHoaDons.Any())
                {
                    ErrorTextBlock.Text = "Hóa đơn chưa có sản phẩm.";
                    return;
                }

                //----------------------------------------
                // 7️⃣ Đồng bộ topping
                //----------------------------------------
                DongBoTatCaTopping();

                //----------------------------------------
                // 8️⃣ Gọi API
                //----------------------------------------
                Result<HoaDonDto> result;

                if (isNew)
                {
                    result = await _api.CreateAsync(Model);

                    if (result.IsSuccess && result.Data?.KhachHangId != null)
                        await AppProviders.KhachHangs.ReloadAsync();
                }
                else if (Model.IsDeleted)
                {
                    result = await _api.RestoreAsync(Model.Id);
                }
                else
                {
                    result = await _api.UpdateAsync(Model.Id, Model);
                    if (newKhachHang)
                        await AppProviders.KhachHangs.ReloadAsync();

                }

                //----------------------------------------
                // 9️⃣ Kiểm tra lỗi
                //----------------------------------------
                if (!result.IsSuccess)
                {
                    ErrorTextBlock.Text = result.Message;
                    return;
                }

                //----------------------------------------
                // 🟟 Lưu Id
                //----------------------------------------
                SavedHoaDonId = result.Data?.Id ?? Model.Id;

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                ErrorTextBlock.Text = ex.Message;
            }
            finally
            {
                _isSaving = false;
                Mouse.OverrideCursor = null;

                if (btn != null)
                    btn.IsEnabled = true;
            }
        }
    }
}