# Full diagnostic: check ICO, published exe, installed exe, MSI Icon stream, shortcuts

Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName PresentationCore

function Show-Section($title) {
    Write-Output ""
    Write-Output ("=" * 60)
    Write-Output $title
    Write-Output ("=" * 60)
}

function Extract-IconsFromExe($exePath) {
    if (-not (Test-Path $exePath)) { Write-Output ("  not found: " + $exePath); return }
    Write-Output ("  Path: " + $exePath)
    Write-Output ("  Modified: " + (Get-Item $exePath).LastWriteTime)
    Write-Output ("  Size: " + (Get-Item $exePath).Length + " bytes")

    # Use Win32 EnumResourceNames to list RT_ICON entries
    $sig = @"
using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
public class IconEnum {
    [DllImport("kernel32.dll", SetLastError=true, CharSet=CharSet.Auto)]
    public static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hReservedNull, uint dwFlags);
    [DllImport("kernel32.dll", SetLastError=true)]
    public static extern bool FreeLibrary(IntPtr hModule);
    public delegate bool EnumResNameProc(IntPtr hModule, IntPtr lpType, IntPtr lpName, IntPtr lParam);
    [DllImport("kernel32.dll", SetLastError=true, CharSet=CharSet.Auto)]
    public static extern bool EnumResourceNames(IntPtr hModule, IntPtr lpType, EnumResNameProc lpEnumFunc, IntPtr lParam);
    [DllImport("kernel32.dll", SetLastError=true)]
    public static extern IntPtr FindResource(IntPtr hModule, IntPtr lpName, IntPtr lpType);
    [DllImport("kernel32.dll", SetLastError=true)]
    public static extern uint SizeofResource(IntPtr hModule, IntPtr hResInfo);
    [DllImport("kernel32.dll", SetLastError=true)]
    public static extern IntPtr LoadResource(IntPtr hModule, IntPtr hResInfo);
    [DllImport("kernel32.dll", SetLastError=true)]
    public static extern IntPtr LockResource(IntPtr hResData);
    public const uint LOAD_LIBRARY_AS_DATAFILE = 0x00000002;
    public static IntPtr RT_ICON = (IntPtr)3;
    public static IntPtr RT_GROUP_ICON = (IntPtr)14;
    public static List<string> entries = new List<string>();
    public static bool Callback(IntPtr hModule, IntPtr lpType, IntPtr lpName, IntPtr lParam) {
        IntPtr hRes = FindResource(hModule, lpName, lpType);
        uint sz = SizeofResource(hModule, hRes);
        IntPtr hData = LoadResource(hModule, hRes);
        IntPtr p = LockResource(hData);
        byte[] data = new byte[(int)sz];
        Marshal.Copy(p, data, 0, (int)sz);
        // PNG signature
        bool isPng = (sz>=8 && data[0]==0x89 && data[1]==0x50 && data[2]==0x4E && data[3]==0x47);
        int w=-1, h=-1;
        if (isPng) {
            w = (data[16]<<24)|(data[17]<<16)|(data[18]<<8)|data[19];
            h = (data[20]<<24)|(data[21]<<16)|(data[22]<<8)|data[23];
            entries.Add("  RT_ICON #" + (long)lpName + ": PNG " + w + "x" + h + " (" + sz + " bytes)");
        } else if (sz >= 16) {
            // BMP DIB: BITMAPINFOHEADER
            w = BitConverter.ToInt32(data, 4);
            h = BitConverter.ToInt32(data, 8) / 2; // includes mask, halve
            entries.Add("  RT_ICON #" + (long)lpName + ": BMP " + w + "x" + h + " (" + sz + " bytes)");
        } else {
            entries.Add("  RT_ICON #" + (long)lpName + ": (unknown, " + sz + " bytes)");
        }
        return true;
    }
}
"@
    Add-Type -TypeDefinition $sig -ErrorAction SilentlyContinue

    [IconEnum]::entries.Clear()
    $hLib = [IconEnum]::LoadLibraryEx($exePath, [IntPtr]::Zero, [IconEnum]::LOAD_LIBRARY_AS_DATAFILE)
    if ($hLib -eq [IntPtr]::Zero) { Write-Output "  failed to load exe as data"; return }
    $cb = [IconEnum+EnumResNameProc]{ param($m,$t,$n,$l) [IconEnum]::Callback($m,$t,$n,$l) }
    $ok = [IconEnum]::EnumResourceNames($hLib, [IconEnum]::RT_ICON, $cb, [IntPtr]::Zero)
    [IconEnum]::FreeLibrary($hLib) | Out-Null

    if ([IconEnum]::entries.Count -eq 0) {
        Write-Output "  No RT_ICON entries found in exe"
    } else {
        foreach ($e in [IconEnum]::entries) { Write-Output $e }
    }
}

Show-Section "1. Source ICO"
$icoPath = "C:\Users\N\Desktop\SessionRecorder_v2\SessionRecorder\SessionRecorder.App\dev_record_icon.ico"
if (Test-Path $icoPath) {
    $bytes = [System.IO.File]::ReadAllBytes($icoPath)
    $count = [BitConverter]::ToUInt16($bytes, 4)
    Write-Output ("  Path: " + $icoPath)
    Write-Output ("  Size: " + $bytes.Length + " bytes")
    Write-Output ("  Images in ICO: " + $count)
    for ($i=0; $i -lt $count; $i++) {
        $offset = 6 + $i * 16
        $w = $bytes[$offset]; if ($w -eq 0) { $w = 256 }
        $h = $bytes[$offset+1]; if ($h -eq 0) { $h = 256 }
        Write-Output ("    - {0}x{1}" -f $w, $h)
    }
}

Show-Section "2. Built (publish) EXE icons"
Extract-IconsFromExe "C:\Users\N\Desktop\SessionRecorder_v2\SessionRecorder\publish\SessionRecorder.App.exe"

Show-Section "3. Installed EXE icons (if installed)"
$installedExes = @(
    "C:\Program Files\Cairn\SessionRecorder.App.exe",
    "C:\Program Files\SessionRecorder\SessionRecorder.App.exe",
    "C:\Program Files\Nakayama\DevelopmentalSupportApp\SessionRecorder.App.exe",
    "C:\Program Files\Nakayama\Cairn\SessionRecorder.App.exe"
)
foreach ($p in $installedExes) {
    if (Test-Path $p) {
        Write-Output ""
        Extract-IconsFromExe $p
    }
}

Show-Section "4. Desktop shortcuts"
$desktop = [Environment]::GetFolderPath("Desktop")
Get-ChildItem -Path $desktop -Filter "*.lnk" -ErrorAction SilentlyContinue | ForEach-Object {
    $sh = New-Object -ComObject WScript.Shell
    $lnk = $sh.CreateShortcut($_.FullName)
    if ($lnk.TargetPath -match "(Cairn|SessionRecorder|Developmental)") {
        Write-Output ""
        Write-Output ("  Shortcut: " + $_.Name)
        Write-Output ("    Target  : " + $lnk.TargetPath)
        Write-Output ("    IconLoc : " + $lnk.IconLocation)
        Write-Output ("    Created : " + (Get-Item $_.FullName).LastWriteTime)
    }
}

Show-Section "5. Installed MSI products matching app"
$reg = "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
       "HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
foreach ($r in $reg) {
    Get-ChildItem $r -ErrorAction SilentlyContinue | ForEach-Object {
        $p = Get-ItemProperty $_.PSPath -ErrorAction SilentlyContinue
        if ($p.DisplayName -match "(Cairn|SessionRecorder|発達支援|Developmental)") {
            Write-Output ""
            Write-Output ("  DisplayName    : " + $p.DisplayName)
            Write-Output ("    Publisher    : " + $p.Publisher)
            Write-Output ("    InstallLoc   : " + $p.InstallLocation)
            Write-Output ("    DisplayIcon  : " + $p.DisplayIcon)
            Write-Output ("    UninstallStr : " + $p.UninstallString)
        }
    }
}
