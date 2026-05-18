$path = "C:\Users\N\Downloads\Cairn.ico"
$bytes = [System.IO.File]::ReadAllBytes($path)
$count = [BitConverter]::ToUInt16($bytes, 4)
Write-Output "File: $path"
Write-Output "Size: $($bytes.Length) bytes"
Write-Output "Images: $count"
for ($i = 0; $i -lt $count; $i++) {
    $offset = 6 + $i * 16
    $w = $bytes[$offset]
    $h = $bytes[$offset+1]
    if ($w -eq 0) { $w = 256 }
    if ($h -eq 0) { $h = 256 }
    $bpp = [BitConverter]::ToUInt16($bytes, $offset+6)
    $imgSize = [BitConverter]::ToUInt32($bytes, $offset+8)
    $imgOffset = [BitConverter]::ToUInt32($bytes, $offset+12)
    # Detect format by reading magic bytes
    $magic1 = $bytes[$imgOffset]
    $magic2 = $bytes[$imgOffset+1]
    if ($magic1 -eq 0x89 -and $magic2 -eq 0x50) { $fmt = "PNG" } else { $fmt = "BMP" }
    Write-Output ("  {0}x{1}  {2}bpp  {3}  {4} bytes" -f $w, $h, $bpp, $fmt, $imgSize)
}
