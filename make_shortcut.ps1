# Create / refresh desktop shortcut "Cairn" pointing to the freshly built exe.

$desktop  = [Environment]::GetFolderPath("Desktop")
$exePath  = "C:\Users\N\Desktop\SessionRecorder_v2\SessionRecorder\SessionRecorder.App\bin\Release\net8.0-windows\SessionRecorder.App.exe"
$workDir  = Split-Path $exePath
$lnkPath  = Join-Path $desktop "Cairn.lnk"

# Remove old shortcuts pointing at the old MSI install
$oldNames = @(
    "Cairn.lnk",
    "developmental support recorder.lnk"
)
# Add the garbled-name match by detecting any lnk whose target is the MSI cache
Get-ChildItem -Path $desktop -Filter "*.lnk" -ErrorAction SilentlyContinue | ForEach-Object {
    $sh = New-Object -ComObject WScript.Shell
    $lnk = $sh.CreateShortcut($_.FullName)
    if ($lnk.TargetPath -like "*Microsoft\Installer\{0F7CC48D*") {
        Write-Output ("Removing old MSI shortcut: " + $_.Name)
        Remove-Item $_.FullName -Force
    }
}

if (-not (Test-Path $exePath)) {
    Write-Output ("ERROR: exe not found: " + $exePath)
    exit 1
}

$sh  = New-Object -ComObject WScript.Shell
$lnk = $sh.CreateShortcut($lnkPath)
$lnk.TargetPath       = $exePath
$lnk.WorkingDirectory = $workDir
$lnk.IconLocation     = "$exePath,0"
$lnk.Description      = "Cairn - therapy session recorder"
$lnk.Save()

Write-Output ""
Write-Output ("Created: " + $lnkPath)
Write-Output ("  -> "   + $exePath)

# Clear icon cache and restart explorer
Write-Output ""
Write-Output "Clearing icon cache..."
taskkill /IM explorer.exe /F | Out-Null
Start-Sleep -Milliseconds 800
Remove-Item "$env:LOCALAPPDATA\IconCache.db" -Force -ErrorAction SilentlyContinue
Remove-Item "$env:LOCALAPPDATA\Microsoft\Windows\Explorer\iconcache_*.db" -Force -ErrorAction SilentlyContinue
Remove-Item "$env:LOCALAPPDATA\Microsoft\Windows\Explorer\thumbcache_*.db" -Force -ErrorAction SilentlyContinue
Start-Process explorer.exe
Write-Output "Done. Desktop should refresh in a moment."
