# ================================================================
# SessionRecorder インストーラービルドスクリプト
# 使用方法: .\build-installer.ps1
# 成果物:   SessionRecorder.Installer\bin\Release\x64\SessionRecorder_Setup.msi
# ================================================================

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$publishDir   = Join-Path $root "publish"
$installerDir = Join-Path $root "SessionRecorder.Installer"
$componentsWxs = Join-Path $installerDir "Components.wxs"

# ----------------------------------------------------------------
# Step 1: アプリを発行
# ----------------------------------------------------------------
Write-Host "=== Step 1: アプリを発行 (self-contained, win-x64) ===" -ForegroundColor Cyan

if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }

dotnet publish "$root\SessionRecorder.App\SessionRecorder.App.csproj" `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    --output $publishDir `
    /p:PublishReadyToRun=true `
    /p:PublishSingleFile=false

if ($LASTEXITCODE -ne 0) { Write-Error "発行に失敗しました"; exit 1 }
Write-Host "発行完了: $publishDir" -ForegroundColor Green

# ----------------------------------------------------------------
# Step 2: Components.wxs を自動生成
# ----------------------------------------------------------------
Write-Host ""
Write-Host "=== Step 2: Components.wxs を生成 ===" -ForegroundColor Cyan

function New-WixId([string]$path) {
    # ファイルパスから安全なWiX IDを生成（英数字とアンダースコアのみ）
    $rel = $path.Replace($publishDir, "").TrimStart("\", "/")
    $id  = "file_" + ($rel -replace '[^a-zA-Z0-9]', '_')
    return $id
}

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

    # サブディレクトリがある場合のディレクトリ参照
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

# サブディレクトリの定義
$dirs = $files | ForEach-Object {
    $rel = $_.FullName.Substring($publishDir.Length).TrimStart('\')
    Split-Path $rel -Parent
} | Where-Object { $_ } | Select-Object -Unique | Sort-Object

if ($dirs) {
    $null = $xml.AppendLine('    <DirectoryRef Id="INSTALLFOLDER">')
    foreach ($dir in $dirs) {
        $parts  = $dir -split '\\'
        $dirId  = "dir_" + ($dir -replace '[^a-zA-Z0-9]', '_')
        $name   = $parts[-1]
        $null = $xml.AppendLine("      <Directory Id=""$dirId"" Name=""$name"" />")
    }
    $null = $xml.AppendLine('    </DirectoryRef>')
}

$null = $xml.AppendLine('  </Fragment>')
$null = $xml.AppendLine('</Wix>')

[System.IO.File]::WriteAllText($componentsWxs, $xml.ToString(), [System.Text.Encoding]::UTF8)
Write-Host "生成完了: $componentsWxs ($($files.Count) ファイル)" -ForegroundColor Green

# ----------------------------------------------------------------
# Step 3: WiX インストーラーをビルド
# ----------------------------------------------------------------
Write-Host ""
Write-Host "=== Step 3: MSI をビルド ===" -ForegroundColor Cyan

dotnet build "$installerDir\SessionRecorder.Installer.wixproj" --configuration Release

if ($LASTEXITCODE -ne 0) { Write-Error "インストーラーのビルドに失敗しました"; exit 1 }

$msiPath = "$installerDir\bin\x64\Release\SessionRecorder_Setup.msi"
if (Test-Path $msiPath) {
    $size = [math]::Round((Get-Item $msiPath).Length / 1MB, 1)
    Write-Host ""
    Write-Host "=== 完了 ===" -ForegroundColor Green
    Write-Host "MSI: $msiPath ($size MB)" -ForegroundColor Yellow
} else {
    Write-Error "MSIファイルが見つかりません: $msiPath"
}
