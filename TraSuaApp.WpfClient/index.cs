< !DOCTYPE html >
< html lang = "vi" >
< head >
  < meta charset = "UTF-8" >
  < title > Danh sách hoá đơn shipper</title>
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css" rel="stylesheet">
  <link href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.css" rel="stylesheet">
</head>
<body class= "p-3 bg-light" >

  < !--Card tổng kết -->
  <div class= "card mb-4 shadow-sm" >
    < div class= "card-body" >
      < h5 class= "card-title text-center fw-bold mb-3" > Tổng kết hôm nay</h5>
      <ul class= "list-group" >
        < li class= "list-group-item d-flex justify-content-between align-items-center" >
          < span class= "fw-bold text-success" > Tiền mặt </ span >
          < span id = "tongTienMat" > 0 ₫</ span >
        </ li >
        < li class= "list-group-item d-flex justify-content-between align-items-center" >
          < span class= "fw-bold text-primary" > Chuyển khoản </ span >
          < span id = "tongTienCK" > 0 ₫</ span >
        </ li >
        < li class= "list-group-item d-flex justify-content-between align-items-center" >
          < span class= "fw-bold text-info" > Chưa Ch.khoản </ span >
          < span id = "listTiNuaCK" >—</ span >
        </ li >
        < li class= "list-group-item d-flex justify-content-between align-items-center" id = "liTongNoKH" >
          < span class= "fw-bold text-danger" > Ghi nợ </ span >
          < span id = "tongGhiNo" > 0 ₫</ span >
        </ li >
        < li class= "list-group-item d-flex justify-content-between align-items-center" id = "liTongTraNo" >
          < span class= "fw-bold text-warning" > Trả nợ </ span >
          < span id = "tongTraNo" > 0 ₫</ span >
        </ li >
        < li class= "list-group-item d-flex justify-content-between align-items-center" >
          < span class= "fw-bold text-secondary" > Chưa chọn </ span >
          < span id = "tongChuaBaoCao" >—</ span >
        </ li >
      </ ul >
    </ div >
  </ div >

  < !--Spinner khi đang tải -->
  <div id="loadingSpinner" class= "text-center my-3" style = "display:none;" >
    < div class= "spinner-border text-primary" role = "status" >
      < span class= "visually-hidden" > Đang tải...</ span >
    </ div >
    < p class= "mt-2 text-muted" > Đang tải dữ liệu, vui lòng chờ...</p>
  </div>

  <!-- Danh sách hoá đơn -->
  <div id="hoaDonList" class= "d-flex flex-column gap-3" ></ div >

  < script >
    const API_BASE = "http://api.denncoffee.uk";

async function loadHoaDon()
{
    try
    {
        document.getElementById("loadingSpinner").style.display = "block";

        const res = await fetch(`${ API_BASE}/ api / hoadon / shipper`);
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
        const id = hd.id;

        const noteRaw = hd.ghiChuShipper ?? "";
        const ghiChuShipper = noteRaw.toLowerCase();
        const ghiChuShipperClean = noteRaw.trim().toLowerCase();

        let detailHtml = `< p class= "text-muted fst-italic mb-0" > Chưa chọn nè a Khánh đẹp trai ơi</p>`;

// trạng thái nút
let btnTienMatDisabled = "";
let btnCKDisabled = "";
let btnGhiNoDisabled = "";
let btnTraNoDisabled = "";
let btnTiNuaCKDisabled = "";

if (/ trả\s* nợ/i.test(ghiChuShipper)) {
            // --- Trả nợ (có thể kèm "Tiền mặt") ---
            // Cộng tổng tất cả "Trả nợ: ..."
            let soTraNo = 0;
for (const m of ghiChuShipper.matchAll(/ trả\s * nợ\s *:\s * ([\d.,] +) / gi)) {
    soTraNo += parseInt((m[1] || "").replace(/\D / g, "")) || 0;
}

// Nếu ghi chú có "Tiền mặt: ..." thì cộng đúng số đó vào tổng tiền mặt
let soTienMat = 0;
for (const m of ghiChuShipper.matchAll(/ tiền\s * mặt\s *:\s * ([\d.,] +) / gi)) {
    soTienMat += parseInt((m[1] || "").replace(/\D / g, "")) || 0;
}

tongTraNo += soTraNo;
tongTienMat += soTienMat; // ❗ không cộng thanhTien nữa

// Hiển thị chi tiết
const lines = [];
if (soTienMat > 0)
    lines.push(`< span class= "text-success fw-bold" > Tiền mặt: ${ soTienMat.toLocaleString()} ₫</ span >`);
lines.push(`< span class= "text-warning fw-bold" > Trả nợ: ${ soTraNo.toLocaleString()} ₫</ span >`);
detailHtml = lines.join("<br>");

// giữ nguyên logic disable nút
btnTienMatDisabled = "disabled";
btnCKDisabled = "disabled";
btnGhiNoDisabled = "disabled";
btnTiNuaCKDisabled = "disabled";
btnTraNoDisabled = (hd.tongNoKhachHang > 0) ? "" : "disabled";
          }
          else if (ghiChuShipper.startsWith("tiền mặt"))
{
    // --- Tiền mặt ---
    const m = ghiChuShipper.match(/ tiền\s * mặt\s *:\s * ([\d.,] +) / i);
    const soTien = m ? parseInt(m[1].replace(/\D / g, "")) : thanhTien;
    tongTienMat += soTien;
    detailHtml = `< p class= "text-success fw-bold mb-0" style = "font-size:0.9rem;" >
                    Tiền mặt: ${ soTien.toLocaleString()} ₫
                          </ p >`;

// đã có ghi chú tiền mặt → chỉ cho phép bấm Trả nợ
btnTienMatDisabled = "disabled";
btnCKDisabled = "disabled";
btnGhiNoDisabled = "disabled";
btnTiNuaCKDisabled = "disabled";
btnTraNoDisabled = (hd.tongNoKhachHang > 0) ? "" : "disabled";
          }
          else if (ghiChuShipper.startsWith("chuyển khoản"))
{
    // --- Chuyển khoản ---
    const m = ghiChuShipper.match(/ chuyển\s * khoản\s *:\s * ([\d.,] +) / i);
    const soTien = m ? parseInt(m[1].replace(/\D / g, "")) : thanhTien;
    tongTienCK += soTien;
    detailHtml = `< p class= "text-primary fw-bold mb-0" style = "font-size:0.9rem;" >
                    Chuyển khoản: ${ soTien.toLocaleString()} ₫
                          </ p >`;

btnTienMatDisabled = "disabled";
btnCKDisabled = "disabled";
btnGhiNoDisabled = "disabled";
btnTiNuaCKDisabled = "disabled";
btnTraNoDisabled = "disabled";
          }
          else if (ghiChuShipper.startsWith("tí nữa chuyển khoản"))
{
    // --- Tí nữa CK ---
    const m = ghiChuShipper.match(/ tí\s * nữa\s * chuyển\s * khoản\s *:\s * ([\d.,] +) / i);
    const soTien = m ? parseInt(m[1].replace(/\D / g, "")) : conLai;
    listTiNuaCK.push(`${ tenKhach} (${ soTien.toLocaleString()} ₫)`);
    detailHtml = `< p class= "text-info fw-bold mb-0" style = "font-size:0.9rem;" >
                            ${ noteRaw}
                          </ p >`;

btnTienMatDisabled = "disabled";
btnCKDisabled = "disabled";
btnGhiNoDisabled = "disabled";
btnTiNuaCKDisabled = "disabled";
btnTraNoDisabled = "disabled";
          }
          else if (ghiChuShipper.startsWith("ghi nợ"))
{
    // --- Ghi nợ ---
    const m = ghiChuShipper.match(/ ghi\s * nợ\s *:\s * ([\d.,] +) / i);
    const soTien = m ? parseInt(m[1].replace(/\D / g, "")) : conLai;
    tongGhiNo += soTien;
    detailHtml = `< p class= "text-danger fw-bold mb-0" style = "font-size:0.9rem;" >
                    Ghi nợ: ${ soTien.toLocaleString()} ₫
                          </ p >`;

btnTienMatDisabled = "disabled";
btnCKDisabled = "disabled";
btnGhiNoDisabled = "disabled";
btnTiNuaCKDisabled = "disabled";
btnTraNoDisabled = "disabled";
          }
          else
{
    // Chỉ tính "Chưa chọn" khi GHI CHÚ TRỐNG (null/empty/space)
    if (ghiChuShipperClean === "")
    {
        khachChuaBaoCao.push(tenKhach);
        // giữ nguyên detailHtml mặc định
    }
}

const tongCaNo = (hd.tongNoKhachHang || 0) + conLai;
const showTongCaNo = (hd.tongNoKhachHang > 0 && hd.tongNoKhachHang !== conLai);

const card = document.createElement("div");
card.className = "card shadow-sm";

card.innerHTML = `
            < div class= "card-body" >
              < h6 class= "fw-bold mb-2" >${ tenKhach}</ h6 >

              < p class= "mb-1" style = "font-size: 0.9rem;" >
                ${ diaChi}
                < a href = "tel:${sdt}" class= "text-decoration-none text-primary fw-bold ms-2" >
                  ${ sdt}
                </ a >
              </ p >

              < p class= "mb-2" style = "font-size: 0.9rem;" >
                < span class= "text-danger fw-bold" >${ conLai.toLocaleString()} ₫</ span >
                < span class= "ms-3" style = "${showTongCaNo ? '' : 'display:none'}" >
                  Tổng cả nợ: 
                  < span class= "${tongCaNo !== 0 ? 'text-warning fw-bold' : 'text-success'}" >
                    ${ tongCaNo.toLocaleString()} ₫
                  </ span >
                </ span >
              </ p >

              < !--Ghi chú chi tiết -->
              ${detailHtml}

              < !--Các nút thao tác -->
              <div class= "row g-2 mt-2" >
                < div class= "col-6" >
                  < button class= "btn btn-success w-100" ${ btnTienMatDisabled}
onclick = "thuTienMat('${id}','${tenKhach}')" >

< i class= "bi bi-cash-stack" ></ i > Tiền mặt
</ button >

</ div >

< div class= "col-6" >

< button class= "btn btn-success w-100" ${ btnTraNoDisabled}
onclick = "traNo('${id}','${tenKhach}', ${conLai}, ${hd.tongNoKhachHang || 0})" >

< i class= "bi bi-check-circle-fill" ></ i > Trả nợ
</ button >

</ div >

< div class= "col-6" >

< button class= "btn btn-warning w-100" ${ btnCKDisabled}
onclick = "thuChuyenKhoan('${id}','${tenKhach}')" >

< i class= "bi bi-bank" ></ i > Ch.Khoản
</ button >

</ div >

< div class= "col-6" >

< button class= "btn btn-danger w-100" ${ btnGhiNoDisabled}
onclick = "ghiNo('${id}','${tenKhach}')" >

< i class= "bi bi-exclamation-triangle-fill" ></ i > Ghi nợ
</ button >

</ div >

< div class= "col-12" >

< button class= "btn btn-info w-100" ${ btnTiNuaCKDisabled}
onclick = "tiNuaChuyenKhoan('${id}','${tenKhach}')" >

< i class= "bi bi-clock" ></ i > Tí Nữa Chuyển Khoản
                  </button>
                </div>
              </div>
            </div>
          `;
list.appendChild(card);
        });

// ✅ Cập nhật tổng kết
document.getElementById("tongTienMat").textContent = `${ tongTienMat.toLocaleString()} ₫`;
document.getElementById("tongTienCK").textContent = `${ tongTienCK.toLocaleString()} ₫`;
document.getElementById("tongGhiNo").textContent = `${ tongGhiNo.toLocaleString()} ₫`;

if (tongTraNo === 0)
{
    document.getElementById("liTongTraNo").style.display = "none";
}
else
{
    document.getElementById("liTongTraNo").style.display = "";
    document.getElementById("tongTraNo").textContent = `${ tongTraNo.toLocaleString()} ₫`;
}

document.getElementById("listTiNuaCK").textContent =
  listTiNuaCK.length > 0 ? listTiNuaCK.join(", ") : "—";

document.getElementById("tongChuaBaoCao").textContent =
  khachChuaBaoCao.length > 0 ? khachChuaBaoCao.join(", ") : "—";

      } catch (err) {
    alert("Lỗi tải dữ liệu: " + err);
} finally {
    // 🟟 Ẩn spinner sau khi xong
    document.getElementById("loadingSpinner").style.display = "none";
}
    }

    async function thuTienMat(id, name)
{
    if (!confirm(`${ name}
    đưa tiền mặt, xác nhận?`)) return;
    await goApi(`${ API_BASE}/ api / hoadon / shipperf1 /${ id}`,
        `✅ ${ name}
    đã đưa tiền mặt`,
        `❌ Thu tiền mặt cho ${ name}
    thất bại`);
}

async function thuChuyenKhoan(id, name)
{
    if (!confirm(`${ name}
    nói là chuyển khoản, xác nhận ?`)) return;
    await goApi(`${ API_BASE}/ api / hoadon / shipperf4 /${ id}`,
        `✅ ${ name}
    đã chuyển khoản`,
        `❌ Thu chuyển khoản cho ${ name}
    thất bại`);
}

async function ghiNo(id, name)
{
    if (!confirm(`${ name}
    ghi nợ, xác nhận ?`)) return;
    await goApi(`${ API_BASE}/ api / hoadon / shipper12 /${ id}`,
        `✅ ${ name}
    đã ghi nợ`,
        `❌ Ghi nợ cho ${ name}
    thất bại`);
}

async function traNo(id, name, conLai, tongNoKH)
{
    if (conLai > 0)
    {
        alert(`Đơn còn lại ${ conLai.toLocaleString()} ₫.\nHãy bấm nút "Tiền mặt" trước khi trả nợ.`);
        return;
    }

    let soTienStr = prompt(`Khách ${ name}
    đưa bao nhiêu tiền trả nợ? (500.000đ nhập 500)`);
    if (!soTienStr) return;

    let soTienNguyen = parseInt(soTienStr.replace(/\D / g, "")); // đơn vị: nghìn
    if (isNaN(soTienNguyen) || soTienNguyen <= 0)
    {
        alert("Số tiền không hợp lệ");
        return;
    }

    try
    {
        const res = await fetch(`${ API_BASE}/ api / hoadon / shipper99 /${ id}`, {
        method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify(soTienNguyen) // backend tự nhân *1000
        });

const payload = await res.json().catch(() => null);
if (!res.ok)
{
    alert(payload?.message || `❌ Cập nhật trả nợ cho ${ name}
    thất bại`);
    return;
}

loadHoaDon();
      } catch (err) {
    alert("Lỗi: " + err);
}
    }

    async function tiNuaChuyenKhoan(id, name)
{
    if (!confirm(`${ name}
    hẹn tí nữa chuyển khoản, xác nhận?`)) return;
    await goApi(`${ API_BASE}/ api / hoadon / shipper55 /${ id}`,
        `✅ ${ name}
    đã hẹn tí nữa CK`,
        `❌ Cập nhật Tí Nữa Chuyển Khoản cho ${ name}
    thất bại`);
}

async function goApi(url, successMsg, errorMsg)
{
    try
    {
        const res = await fetch(url, { method: "POST" });
        if (res.ok)
        {
            loadHoaDon();
        }
        else
        {
            const payload = await res.json().catch (() => null);
    alert(payload?.message || errorMsg);
    }
} catch (err) {
    alert("Lỗi: " + err);
}
    }

    loadHoaDon();
  </ script >
</ body >
</ html >