// wwwroot/js/denn.modals.js
(function (w) {
    function getPageBase() {
        const p = location.pathname.toLowerCase();
        if (p.includes('/doanhthuthang')) return '/DoanhThuThang';
        return '/DoanhThuNgay';
    }

    function getModal(id) {
        const el = document.getElementById(id);
        return bootstrap.Modal.getOrCreateInstance(el);
    }

    function okFlag(json) { return json && (json.isSuccess === true || json.IsSuccess === true); }

    function getData(json) {
        return Array.isArray(json?.data) ? json.data : (Array.isArray(json?.Data) ? json.Data : []);
    }

    function redirectToLogin() {
        const returnUrl = location.pathname + location.search;
        location.href = `/Login?returnUrl=${encodeURIComponent(returnUrl)}`;
    }

    // =============================
    // DS hoá đơn theo khách
    // =============================
    w.xemHoaDonKhachHang = function (khachHangId) {
        if (!khachHangId) return;

        const container = document.getElementById('hoaDonContent');
        if (container) container.innerHTML = "<p>Đang tải danh sách...</p>";
        getModal('hoaDonModal').show();

        const url = `${getPageBase()}?handler=DanhSach&khachHangId=${encodeURIComponent(khachHangId)}`;
        fetch(url, { credentials: 'same-origin' })
            .then(async res => {
                if (res.status === 401) { redirectToLogin(); return; }
                if (!res.ok) throw new Error(`HTTP ${res.status}`);
                return res.json();
            })
            .then(json => {
                if (!json || !container) return;
                if (!okFlag(json)) {
                    container.innerHTML = "<p class='text-danger'>Không tải được danh sách hóa đơn</p>";
                    return;
                }

                const rows = getData(json)
                    .sort((a, b) => new Date(b.ngayHoaDon ?? b.NgayHoaDon) - new Date(a.ngayHoaDon ?? a.NgayHoaDon));

                let html = `
          <table class="table table-sm table-bordered">
            <thead class="table-light">
              <tr>
                <th>Ngày giờ</th>
                <th>Khách hàng</th>
                <th class="text-end">Còn lại</th>
              </tr>
            </thead>
            <tbody>`;

                rows.forEach(item => {
                    const ngay = item.ngayHoaDon ?? item.NgayHoaDon;
                    const tenKh = item.tenKhachHangText ?? item.TenKhachHangText;
                    const thongTin = item.thongTinHoaDon ?? item.ThongTinHoaDon;
                    const ngayGio = new Date(ngay).toLocaleString('vi-VN', {
                        day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit'
                    });
                    const tong = Number(item.tongTien ?? item.TongTien ?? 0);
                    const daThu = Number(item.daThu ?? item.DaThu ?? 0);
                    const conLai = Number(item.conLai ?? item.ConLai ?? (tong - daThu));
                    const id = item.id ?? item.Id;

                    html += `
            <tr>
              <td>${ngayGio}</td>
              <td>${[tenKh, thongTin].filter(Boolean).join(" - ")}</td>
              <td class="text-end">
                <a href="javascript:void(0)" onclick="xemChiTiet('${id}')">
                  ${conLai.toLocaleString('vi-VN')} đ
                </a>
              </td>
            </tr>`;
                });

                container.innerHTML = html + "</tbody></table>";
            })
            .catch(err => {
                console.error(err);
                if (container) container.innerHTML = `<p class='text-danger'>Lỗi tải dữ liệu</p>`;
            });
    };

    // =============================
    // Chi tiết hoá đơn
    // =============================
    w.xemChiTiet = function (id) {
        if (!id) return;

        const container = document.getElementById('chiTietContent');
        if (container) container.innerHTML = "<p>Đang tải dữ liệu...</p>";
        getModal('chiTietModal').show();

        const url = `${getPageBase()}?handler=ChiTiet&hoaDonId=${encodeURIComponent(id)}`;
        fetch(url, { credentials: 'same-origin' })
            .then(async res => {
                if (res.status === 401) { redirectToLogin(); return; }
                if (!res.ok) throw new Error(`HTTP ${res.status}`);
                return res.json();
            })
            .then(json => {
                if (!json || !container) return;
                if (!okFlag(json)) {
                    container.innerHTML = "<p class='text-danger'>Không tải được chi tiết hóa đơn</p>";
                    return;
                }

                const list = getData(json);
                const icons = ["0️⃣", "1️⃣", "2️⃣", "3️⃣", "4️⃣", "5️⃣", "6️⃣", "7️⃣", "8️⃣", "9️⃣"];
                const getIcon = (num) => {
                    if (num === 10) return "🟟";
                    if (num < 10) return icons[num] ?? num;
                    return String(num).split("").map(c => icons[parseInt(c)] ?? c).join("");
                };

                let html = "";
                list.forEach(item => {
                    const soLuong = item.soLuong ?? item.SoLuong ?? 0;
                    const ten = item.tenSanPham ?? item.TenSanPham ?? "";
                    const ghiChu = item.ghiChu ?? item.GhiChu;
                    const tt = Number(item.thanhTien ?? item.ThanhTien ?? 0);
                    const iconSo = getIcon(soLuong);

                    html += `
            <div class="mx-3 d-flex justify-content-between align-items-center item-row py-2 ${tt == 0 ? "opacity-25" : ""}">
              <div>
                <span class="item-text">${iconSo} ${ten}</span>
                ${ghiChu ? `<div class="item-note">${ghiChu}</div>` : ""}
              </div>
              <span class="fw-bold text-danger">${tt.toLocaleString('vi-VN')}</span>
            </div>`;
                });

                const tong = list.reduce((s, x) => s + Number(x.thanhTien ?? x.ThanhTien ?? 0), 0);
                html += `
          <div class="border-top mt-4 pt-2 d-flex justify-content-between fw-bold text-danger">
            <span>Tổng</span>
            <span>${tong.toLocaleString('vi-VN')} đ</span>
          </div>`;

                container.innerHTML = html;
            })
            .catch(err => {
                console.error(err);
                if (container) container.innerHTML = `<p class='text-danger'>Lỗi tải dữ liệu</p>`;
            });
    };
})(window);