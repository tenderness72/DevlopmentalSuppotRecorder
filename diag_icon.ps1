# Diagnose why desktop shortcut icon is not showing.

Add-Type -AssemblyName System.Drawing

$desktop = [Environment]::GetFolderPath("Desktop")
Write-Output "=== Desktop shortcuts ==="
Get-ChildItem -Path $desktop -Filter "*.lnk" -ErrorAction SilentlyContinue | ForEach-Object {
    $sh = New-Object -ComObject WScript.Shell
    $lnk = $sh.CreateShortcut($_.FullName)
    Write-Output ""
    Write-Output ("Shortcut    : " + $_.Name)
    Write-Output ("  TargetPath: " + $lnk.TargetPath)
    Write-Output ("  IconLoc   : " + $lnk.IconLocation)
    Write-Output ("  WorkingDir: " + $lnk.WorkingDirectory)
    Write-Output ("  TargetExist: " + (Test-Path $lnk.TargetPath))
}

Write-Output ""
Write-Output "=== Built exe icon check ==="
$exePaths = @(
    "C:\Users\N\Desktop\SessionRecorder_v2\SessionRecorder\SessionRecorder.App\bin\Release\net8.0-windows\SessionRecorder.App.exe",
    "C:\Users\N\Desktop\SessionRecorder_v2\SessionRecorder\SessionRecorder.App\bin\Debug\net8.0-windows\SessionRecorder.App.exe"
)
foreach ($p in $exePaths) {
    if (Test-Path $p) {
        $info = Get-Item $p
        Write-Output ("EXE: " + $p)
        Write-Output ("  Size       : " + $info.Length + " bytes")
        Write-Output ("  Modified   : " + $info.LastWriteTime)
        try {
            $ico = [System.Drawing.Icon]::ExtractAssociatedIcon($p)
            Write-Output ("  Icon size  : " + $ico.Width + "x" + $ico.Height)
            $ico.Dispose()
        } catch {
            Write-Output ("  Icon extract failed: " + $_.Exception.Message)
        }
    } else {
        Write-Output ("EXE not found: " + $p)
    }
}

Write-Output ""
Write-Output "=== Source ICO check ==="
$icoPath = "C:\Users\N\Desktop\SessionRecorder_v2\SessionRecorder\SessionRecorder.App\dev_record_icon.ico"
if (Test-Path $icoPath) {
    $bytes = [System.IO.File]::ReadAllBytes($icoPath)
    $count = [BitConverter]::ToUInt16($bytes, 4)
    Write-Output ("ICO: " + $icoPath)
    Write-Output ("  Size       : " + $bytes.Length + " bytes")
    Write-Output ("  Images     : " + $count)
}
