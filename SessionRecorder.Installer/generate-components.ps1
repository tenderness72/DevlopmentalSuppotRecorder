# Components.wxs 生成スクリプト（SessionRecorder.Installer.wixproj から呼び出される）
# SESSION_RECORDER_ROOT 環境変数、またはスクリプト位置から自動解決
$RootDir = if ($env:SESSION_RECORDER_ROOT) { $env:SESSION_RECORDER_ROOT } else { Split-Path -Parent $PSScriptRoot }
$publishDir    = Join-Path $RootDir "publish"
$componentsWxs = Join-Path $RootDir "SessionRecorder.Installer\Components.wxs"

# ── Step A: dotnet publish ──────────────────────────────────────
Write-Host "  [Installer Pre-Build] Publishing app..." -ForegroundColor Cyan
if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }

dotnet publish "$RootDir\SessionRecorder.App\SessionRecorder.App.csproj" `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    --output $publishDir `
    /p:PublishReadyToRun=true `
    /p:PublishSingleFile=false `
    --nologo -v quiet

if ($LASTEXITCODE -ne 0) { Write-Error "dotnet publish failed"; exit 1 }

# ── Step B: Components.wxs を生成 ──────────────────────────────
Write-Host "  [Installer Pre-Build] Generating Components.wxs..." -ForegroundColor Cyan

$files = Get-ChildItem $publishDir -Recurse -File | Sort-Object FullName
$xml   = [System.Text.StringBuilder]::new()

$null = $xml.AppendLine('<?xml version="1.0" encoding="utf-8"?>')
$null = $xml.AppendLine('<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs">')
$null = $xml.AppendLine('  <Fragment>')
$null = $xml.AppendLine('    <ComponentGroup Id="AppFiles" Directory="INSTALLFOLDER">')

foreach ($file in $files) {
    $rel    = $file.FullName.Substring($publishDir.Length).TrimStart('\')
    $fileId = "file_" + ($rel -replace '[^a-zA-Z0-9]', '_')
    $compId = "comp_" + ($rel -replace '[^a-zA-Z0-9]', '_')
    $subDir = Split-Path $rel -Parent

    if ($subDir) {
        $dirId = "dir_" + ($subDir -replace '[^a-zA-Z0-9]', '_')
        $null = $xml.AppendLine("      <Component Id=""$compId"" Directory=""$dirId"" Guid=""*"">")
    } else {
        $null = $xml.AppendLine("      <Component Id=""$compId"" Guid=""*"">")
    }
    $null = $xml.AppendLine("        <File Id=""$fileId"" Source=""$($file.FullName)"" />")
    $null = $xml.AppendLine("      </Component>")
}

$null = $xml.AppendLine('    </ComponentGroup>')

$dirs = $files | ForEach-Object {
    $rel = $_.FullName.Substring($publishDir.Length).TrimStart('\')
    Split-Path $rel -Parent
} | Where-Object { $_ } | Select-Object -Unique | Sort-Object

if ($dirs) {
    $null = $xml.AppendLine('    <DirectoryRef Id="INSTALLFOLDER">')
    foreach ($dir in $dirs) {
        $parts = $dir -split '\\'
        $dirId = "dir_" + ($dir -replace '[^a-zA-Z0-9]', '_')
        $name  = $parts[-1]
        $null = $xml.AppendLine("      <Directory Id=""$dirId"" Name=""$name"" />")
    }
    $null = $xml.AppendLine('    </DirectoryRef>')
}

$null = $xml.AppendLine('  </Fragment>')
$null = $xml.AppendLine('</Wix>')

[System.IO.File]::WriteAllText($componentsWxs, $xml.ToString(), [System.Text.Encoding]::UTF8)
Write-Host "  [Installer Pre-Build] Done: $($files.Count) files harvested." -ForegroundColor Green
