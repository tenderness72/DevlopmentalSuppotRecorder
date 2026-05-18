$path = "C:\Users\N\Desktop\SessionRecorder_v2\SessionRecorder\SessionRecorder.App\dev_record_icon.ico"
$bytes = [System.IO.File]::ReadAllBytes($path)
$count = [BitConverter]::ToUInt16($bytes, 4)
Write-Output "Images in ICO: $count"
for ($i = 0; $i -lt $count; $i++) {
    $offset = 6 + $i * 16
    $w = $bytes[$offset]
    $h = $bytes[$offset+1]
    if ($w -eq 0) { $w = 256 }
    if ($h -eq 0) { $h = 256 }
    $bpp = [BitConverter]::ToUInt16($bytes, $offset+6)
    $size = [BitConverter]::ToUInt32($bytes, $offset+8)
    Write-Output ("  {0}x{1}  {2}bpp  {3} bytes" -f $w, $h, $bpp, $size)
}
