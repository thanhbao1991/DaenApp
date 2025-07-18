# Chuyển đến thư mục gốc dự án
Set-Location -Path "D:\New folder"

# Đường dẫn file xuất kết quả
$outputPath = "DanhSachFile.txt"

# Các mẫu cần loại bỏ
$excludedDirs = @("bin", "obj", ".vs", "node_modules", ".git")
$excludedExts = @(".dll", ".pdb", ".exe", ".cache", ".user", ".log", ".zip", ".png", ".jpg")

# Lấy toàn bộ file, loại bỏ theo điều kiện
$files = Get-ChildItem -Recurse -File | Where-Object {
    $path = $_.FullName.ToLower()

    # Không chứa các thư mục loại trừ
    $notInExcludedDirs = $true
    foreach ($dir in $excludedDirs) {
        if ($path -like "*\$dir\*") {
            $notInExcludedDirs = $false
            break
        }
    }

    # Không có phần mở rộng bị loại trừ
    $notInExcludedExts = $true
    foreach ($ext in $excludedExts) {
        if ($path.EndsWith($ext)) {
            $notInExcludedExts = $false
            break
        }
    }

    return $notInExcludedDirs -and $notInExcludedExts
}

# Ghi kết quả ra file
$files | ForEach-Object { $_.FullName } | Out-File -FilePath $outputPath -Encoding UTF8

Write-Host "`n✅ Đã tạo xong: $outputPath"
Pause