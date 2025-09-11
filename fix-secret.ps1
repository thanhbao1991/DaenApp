# ===============================
# Script: fix-secret.ps1
# Xóa toàn bộ file Config.cs khỏi lịch sử Git và force push lại
# ===============================

# 1. Tải git-filter-repo.exe nếu chưa có
$filterRepoUrl = "https://github.com/newren/git-filter-repo/releases/latest/download/git-filter-repo.exe"
$gitBinPath = "C:\Program Files\Git\mingw64\bin"
$filterRepoExe = Join-Path $gitBinPath "git-filter-repo.exe"

if (-Not (Test-Path $filterRepoExe)) {
    Write-Host "🟟 Đang tải git-filter-repo.exe ..."
    Invoke-WebRequest -Uri $filterRepoUrl -OutFile $filterRepoExe
    Write-Host "✅ Đã tải git-filter-repo.exe vào $gitBinPath"
} else {
    Write-Host "✅ Đã có git-filter-repo.exe trong $gitBinPath"
}

# 2. Kiểm tra git-filter-repo hoạt động
git filter-repo --help | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Lỗi: git-filter-repo chưa chạy được. Kiểm tra PATH hoặc cài đặt Git lại."
    exit 1
}

# 3. Chạy filter-repo để xoá Config.cs khỏi lịch sử
Write-Host "🟟 Đang xoá TraSuaApp.Shared/Config.cs khỏi toàn bộ lịch sử Git..."
git filter-repo --path TraSuaApp.Shared/Config.cs --invert-paths

# 4. Xoá cache cũ và force push lại
Write-Host "🟟 Đang force push toàn bộ lịch sử lên GitHub..."
git push origin --force --all
git push origin --force --tags

Write-Host "✅ Hoàn tất! File Config.cs đã được xoá khỏi lịch sử và repo đã được cập nhật."
Write-Host "⚠️ Đừng quên regenerate OpenAI API Key trên https://platform.openai.com/account/api-keys và set vào biến môi trường:"
Write-Host '   setx OPENAI_API_KEY "sk-xxxxx"'