$desktop = [Environment]::GetFolderPath('Desktop')
$startMenu = "$env:APPDATA\Microsoft\Windows\Start Menu\Programs"
$startMenuAll = "$env:ProgramData\Microsoft\Windows\Start Menu\Programs"

function Show-Lnk($file) {
    $sh = New-Object -ComObject WScript.Shell
    $lnk = $sh.CreateShortcut($file.FullName)
    Write-Output "---"
    Write-Output ("Name    : " + $file.Name)
    Write-Output ("Folder  : " + $file.DirectoryName)
    Write-Output ("Target  : " + $lnk.TargetPath)
    Write-Output ("IconLoc : " + $lnk.IconLocation)
    Write-Output ("WorkDir : " + $lnk.WorkingDirectory)
    Write-Output ("Modified: " + $file.LastWriteTime)
}

Write-Output "=== Desktop ==="
Get-ChildItem $desktop -Filter "*.lnk" -ErrorAction SilentlyContinue | Where-Object {
    (Get-Item $_.FullName).Name -match "(Cairn|airn|発達|Sess|Developmental)" -or $true
} | ForEach-Object {
    $sh = New-Object -ComObject WScript.Shell
    $lnk = $sh.CreateShortcut($_.FullName)
    if ($lnk.TargetPath -match "(Cairn|Sess|Developmental|Installer)" -or $_.Name -match "(Cairn|airn)") {
        Show-Lnk $_
    }
}

Write-Output ""
Write-Output "=== Start Menu (User) ==="
Get-ChildItem $startMenu -Filter "*.lnk" -Recurse -ErrorAction SilentlyContinue | ForEach-Object {
    $sh = New-Object -ComObject WScript.Shell
    $lnk = $sh.CreateShortcut($_.FullName)
    if ($lnk.TargetPath -match "(Cairn|Sess|Developmental|Installer)" -or $_.Name -match "(Cairn|airn)") {
        Show-Lnk $_
    }
}

Write-Output ""
Write-Output "=== Start Menu (All Users) ==="
Get-ChildItem $startMenuAll -Filter "*.lnk" -Recurse -ErrorAction SilentlyContinue | ForEach-Object {
    $sh = New-Object -ComObject WScript.Shell
    $lnk = $sh.CreateShortcut($_.FullName)
    if ($lnk.TargetPath -match "(Cairn|Sess|Developmental|Installer)" -or $_.Name -match "(Cairn|airn)") {
        Show-Lnk $_
    }
}

Write-Output ""
Write-Output "=== Files in install folder ==="
$installDir = "C:\Program Files\Nakayama\Cairn"
if (Test-Path $installDir) {
    Get-ChildItem $installDir -Filter "*.ico" -ErrorAction SilentlyContinue | ForEach-Object {
        Write-Output ("ICO file: " + $_.FullName + " (" + $_.Length + " bytes)")
    }
    Get-ChildItem $installDir -Filter "*.exe" -ErrorAction SilentlyContinue | ForEach-Object {
        Write-Output ("EXE file: " + $_.FullName + " (" + $_.Length + " bytes)")
    }
}
