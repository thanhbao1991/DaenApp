# BackupBeforePublish.ps1

$source = "$PSScriptRoot"
$timestamp = Get-Date -Format "yyyy-MM-dd_HH-mm-ss"
$dest = "D:\Dropbox\Code\Backup\$timestamp"

Write-Host "→ Đang backup source từ $source đến $dest..."

robocopy $source $dest /E /XD bin obj .vs node_modules /XF *.dll *.pdb *.exe *.user *.suo > $null

Write-Host "✔ Backup hoàn tất."