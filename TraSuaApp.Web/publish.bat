@echo off
setlocal

:: ===== KIỂM TRA QUYỀN ADMIN =====
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo ⚠ Yêu cầu quyền Administrator. Đang chạy lại...
    powershell -Command "Start-Process '%~f0' -Verb runAs"
    exit /b
)

:: ===== CẤU HÌNH =====
set APP_NAME=TraSuaApp
set APP_POOL=TraSuaApp
set PUBLISH_TEMP=D:\TempPublish\%APP_NAME%
set IIS_PATH=C:\inetpub\wwwroot\%APP_NAME%
set CSPROJ_PATH=D:\DennWebApp\DennWebApp.csproj

:: ===== STOP APP POOL =====
echo 🟟 Đang dừng App Pool "%APP_POOL%"...
%windir%\system32\inetsrv\appcmd stop apppool /apppool.name:"%APP_POOL%"

:: ===== PUBLISH =====
echo 🟟 Đang publish dự án...
dotnet publish "%CSPROJ_PATH%" -c Release -o "%PUBLISH_TEMP%"
IF ERRORLEVEL 1 (
    echo ❌ Lỗi khi publish. Dừng lại.
    pause
    exit /b 1
)

:: ===== XOÁ DỮ LIỆU CŨ TRONG IIS =====
echo 🟟 Đang xoá thư mục IIS cũ: "%IIS_PATH%"
rmdir /S /Q "%IIS_PATH%"
mkdir "%IIS_PATH%"

:: ===== COPY FILE MỚI =====
echo 🟟 Đang sao chép đến IIS folder...
xcopy /E /Y /I "%PUBLISH_TEMP%\*" "%IIS_PATH%\"

:: ===== START APP POOL =====
echo 🟟 Khởi động lại App Pool "%APP_POOL%"...
%windir%\system32\inetsrv\appcmd start apppool /apppool.name:"%APP_POOL%"

echo ✅ Deploy hoàn tất.
endlocal
