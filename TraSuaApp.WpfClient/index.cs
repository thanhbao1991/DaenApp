<!DOCTYPE html>
<html lang="vi">
<head>
  <meta charset="UTF-8">
  <title>Danh sách hoá đơn shipper</title>
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css" rel="stylesheet">
  <link href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.css" rel="stylesheet">
</head>
<body class="p-3 bg-light">

  <!-- Card tổng kết -->
  <div class="card mb-4 shadow-sm">
    <div class="card-body">
      <h5 class="card-title text-center fw-bold mb-3">Tổng kết hôm nay</h5>
      <ul class="list-group">
        <li class="list-group-item d-flex justify-content-between align-items-center">
          <span class="fw-bold text-success">Tiền mặt</span>
          <span id="tongTienMat">0 ₫</span>
        </li>
        <li class="list-group-item d-flex justify-content-between align-items-center">
          <span class="fw-bold text-primary">Chuyển khoản</span>
          <span id="tongTienCK">0 ₫</span>
        </li>
        <li class="list-group-item d-flex justify-content-between align-items-center">
          <span class="fw-bold text-info">Tí Nữa Chuyển Khoản</span>
          <span id="listTiNuaCK">—</span>
        </li>
        <li class="list-group-item d-flex justify-content-between align-items-center">
          <span class="fw-bold text-danger">Ghi nợ</span>
          <span id="tongGhiNo">0 ₫</span>
        </li>
        <li class="list-group-item d-flex justify-content-between align-items-center">
          <span class="fw-bold text-warning">Trả nợ</span>
          <span id="tongTraNo">0 ₫</span>
        </li>
        <li class="list-group-item d-flex justify-content-between align-items-center">
          <span class="fw-bold text-secondary">Chưa chọn</span>
          <span id="tongChuaBaoCao">—</span>
        </li>
      </ul>
    </div>
  </div>

  <!-- Danh sách hoá đơn -->
  <div id="hoaDonList" class="d-flex flex-column gap-3"></div>

  <script>
    const API_BASE = "http://api.denncoffee.uk";

    async function loadHoaDon() {
      try {
        const res = await fetch(`${API_BASE}/api/hoadon/shipper`);
        const resData = await res.json();
        const list = document.getElementById("hoaDonList");

        list.innerHTML = "";
        let tongTienMat = 0;
        let tongTienCK = 0;
        let tongGhiNo = 0;
        let tongTraNo = 0;
        let khachChuaBaoCao = [];
        let listTiNuaCK = [];

        const hoaDons = resData.data || [];

        hoaDons.forEach(hd => {
          const tenKhach = hd.tenKhachHangText || "Khách lẻ";
          const diaChi = hd.diaChiText || "";
          const sdt = hd.soDienThoaiText || "";
          const conLai = hd.conLai || 0;
          const thanhTien = hd.thanhTien || 0;
          const tongNoKH = hd.tongNoKhachHang || 0;
          const tongCaNo = conLai + tongNoKH;
          const id = hd.id;
          const ghiChuShipper = (hd.ghiChuShipper || "").toLowerCase();

          // ✅ Tính tổng hoặc gom tên
          if (ghiChuShipper.includes("đưa tiền mặt")) {
            tongTienMat += thanhTien;
          } else if (ghiChuShipper.includes("chuyển khoản")) {
            tongTienCK += thanhTien;
          } else if (ghiChuShipper.includes("tí nữa chuyển khoản")) {
            listTiNuaCK.push(tenKhach);
          } else if (ghiChuShipper.includes("nói nợ")) {
            tongGhiNo += conLai;
          } else if (ghiChuShipper.includes("trả nợ")) {
            const m = ghiChuShipper.match(/trả\s*nợ\s*:\s*([\d.,\s]+)/i);
            const soTraNo = m ? parseInt(m[1].replace(/\D/g, "")) : 0;
            tongTraNo += (soTraNo || 0);
          } else {
            khachChuaBaoCao.push(tenKhach);
          }

          const showTongCaNo = tongNoKH > 0;

          const card = document.createElement("div");
          card.className = "card shadow-sm";

          card.innerHTML = `
            <div class="card-body">
              <h5 class="card-title mb-2 fw-bold">${tenKhach}</h5>

              <p class="mb-1" style="font-size: 1rem;">
                ${diaChi}
                <a href="tel:${sdt}" class="text-decoration-none text-primary fw-bold ms-2">
                  ${sdt}
                </a>
              </p>

              <p class="mb-2" style="font-size: 1rem;">
                <span class="text-danger fw-bold">${conLai.toLocaleString()} ₫</span>
                <span class="ms-3" style="${showTongCaNo ? '' : 'display:none'}">Tổng cả nợ:
                  <span class="${tongCaNo !== 0 ? 'text-warning fw-bold' : 'text-success'}">
                    ${tongCaNo.toLocaleString()} ₫
                  </span>
                </span>
              </p>
              <div class="row g-2 mb-2">
                <div class="col-6">
                  <button class="btn btn-success w-100" onclick="thuTienMat('${id}','${tenKhach}')">
                    <i class="bi bi-cash-stack"></i> Tiền mặt
                  </button>
                </div>
                <div class="col-6">
                  <button class="btn btn-success w-100" onclick="traNo('${id}','${tenKhach}', ${conLai}, ${tongNoKH})">
                    <i class="bi bi-check-circle-fill"></i> Trả nợ
                  </button>
                </div>
                <div class="col-6">
                  <button class="btn btn-warning w-100" onclick="thuChuyenKhoan('${id}','${tenKhach}')">
                    <i class="bi bi-bank"></i> Ch.Khoản
                  </button>
                </div>
                <div class="col-6">
                  <button class="btn btn-danger w-100" onclick="ghiNo('${id}','${tenKhach}')">
                    <i class="bi bi-exclamation-triangle-fill"></i> Ghi nợ
                  </button>
                </div>
                <!-- ✅ Nút Tí Nữa Chuyển Khoản -->
                <div class="col-12">
                  <button class="btn btn-info w-100" onclick="tiNuaChuyenKhoan('${id}','${tenKhach}')">
                    <i class="bi bi-clock"></i> Tí Nữa Chuyển Khoản
                  </button>
                </div>
              </div>
              ${
                ghiChuShipper.includes("đưa tiền mặt") ? 
                  `<p class="text-success fw-bold mb-0" style="font-size:0.95rem;">
                     Tiền mặt: ${thanhTien.toLocaleString()} ₫
                   </p>` :
                ghiChuShipper.includes("chuyển khoản") ? 
                  `<p class="text-primary fw-bold mb-0" style="font-size:0.95rem;">
                     Chuyển khoản: ${thanhTien.toLocaleString()} ₫
                   </p>` :
                ghiChuShipper.includes("tí nữa chuyển khoản") ? 
                  `<p class="text-info fw-bold mb-0" style="font-size:0.95rem;">
                     Tí nữa chuyển khoản: ${thanhTien.toLocaleString()} ₫
                   </p>` :
                ghiChuShipper.includes("nói nợ") ? 
                  `<p class="text-danger fw-bold mb-0" style="font-size:0.95rem;">
                     Ghi nợ: ${conLai.toLocaleString()} ₫
                   </p>` :
                ghiChuShipper.includes("trả nợ") ?
                  (() => {
                    const m = ghiChuShipper.match(/trả\s*nợ\s*:\s*([\d.,\s]+)/i);
                    const soTraNo = m ? parseInt(m[1].replace(/\D/g, "")) : 0;
                    return `<p class="mb-0" style="font-size:0.95rem;">
                              <span class="text-success fw-bold">Tiền mặt: ${thanhTien.toLocaleString()} ₫</span><br>
                              <span class="text-warning fw-bold">Trả nợ: ${soTraNo.toLocaleString()} ₫</span>
                            </p>`;
                  })() :
                  `<p class="text-muted fst-italic mb-0" style="font-size:0.9rem;">
                     Chưa chọn nè a Khánh đẹp trai ơi
                   </p>`
              }
            </div>
          `;
          list.appendChild(card);
        });

        // ✅ Cập nhật tổng kết
        document.getElementById("tongTienMat").textContent = `${tongTienMat.toLocaleString()} ₫`;
        document.getElementById("tongTienCK").textContent = `${tongTienCK.toLocaleString()} ₫`;
        document.getElementById("tongGhiNo").textContent = `${tongGhiNo.toLocaleString()} ₫`;
        document.getElementById("tongTraNo").textContent = `${tongTraNo.toLocaleString()} ₫`;
        document.getElementById("listTiNuaCK").textContent =
          listTiNuaCK.length > 0 ? listTiNuaCK.join(", ") : "—";
        document.getElementById("tongChuaBaoCao").textContent =
          khachChuaBaoCao.length > 0 ? khachChuaBaoCao.join(", ") : "—";

      } catch (err) {
        alert("Lỗi tải dữ liệu: " + err);
      }
    }

    async function thuTienMat(id, name) {
      if (!confirm(`${name} đưa tiền mặt, xác nhận?`)) return;
      await goApi(`${API_BASE}/api/hoadon/shipperf1/${id}`,
        `✅ ${name} đã đưa tiền mặt`,
        `❌ Thu tiền mặt cho ${name} thất bại`);
    }

    async function thuChuyenKhoan(id, name) {
      if (!confirm(`${name} nói là chuyển khoản, xác nhận?`)) return;
      await goApi(`${API_BASE}/api/hoadon/shipperf4/${id}`,
        `✅ ${name} nói là chuyển khoản`,
        `❌ Thu chuyển khoản cho ${name} thất bại`);
    }

    async function ghiNo(id, name) {
      if (!confirm(`${name} nói nợ, xác nhận?`)) return;
      await goApi(`${API_BASE}/api/hoadon/shipper12/${id}`,
        `✅ ${name} đã ghi nợ`,
        `❌ Ghi nợ cho ${name} thất bại`);
    }

    async function traNo(id, name, conLai, tongNoKH) {
      if (conLai > 0) {
        alert(`Đơn còn lại ${conLai.toLocaleString()} ₫.\nHãy bấm nút "Tiền mặt" trước khi trả nợ.`);
        return;
      }

      let soTienStr = prompt(`Khách ${name} đưa bao nhiêu tiền trả nợ? (500.000đ nhập 500)`);
      if (!soTienStr) return;

      let soTienNguyen = parseInt(soTienStr.replace(/\D/g, "")); // đơn vị: nghìn
      if (isNaN(soTienNguyen) || soTienNguyen <= 0) {
        alert("Số tiền không hợp lệ");
        return;
      }

      try {
        const res = await fetch(`${API_BASE}/api/hoadon/shipper99/${id}`, {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify(soTienNguyen) // backend tự nhân *1000
        });

        const payload = await res.json().catch(() => null);
        if (!res.ok) {
          alert(payload?.message || `❌ Cập nhật trả nợ cho ${name} thất bại`);
          return;
        }

        alert(`✅ ${name} đã trả nợ ${soTienNguyen.toLocaleString()}k`);
        loadHoaDon();
      } catch (err) {
        alert("Lỗi: " + err);
      }
    }

    async function tiNuaChuyenKhoan(id, name) {
      if (!confirm(`${name} hẹn tí nữa chuyển khoản, xác nhận?`)) return;
      await goApi(`${API_BASE}/api/hoadon/shipper55/${id}`,
        `✅ ${name} đã hẹn tí nữa chuyển khoản`,
        `❌ Cập nhật Tí Nữa Chuyển Khoản cho ${name} thất bại`);
    }

    async function goApi(url, successMsg, errorMsg) {
      try {
        const res = await fetch(url, { method: "POST" });
        if (res.ok) {
          loadHoaDon();
        } else {
          alert(errorMsg);
        }
      } catch (err) {
        alert("Lỗi: " + err);
      }
    }

    loadHoaDon();
  </script>
</body>
</html>