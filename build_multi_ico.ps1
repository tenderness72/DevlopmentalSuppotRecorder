# Build a hybrid multi-resolution ICO: BMP DIB for small sizes + PNG for 256x256.
# This format is the most compatible with all Windows shell components,
# especially MSI advertised shortcuts.

Add-Type -AssemblyName System.Drawing

$srcPath    = "C:\Users\N\Desktop\SessionRecorder_v2\SessionRecorder\SessionRecorder.App\dev_record_icon.ico"
$dstPath    = $srcPath
$backupPath = "$srcPath.single256.bak"

if (Test-Path $backupPath) {
    $srcBytes = [System.IO.File]::ReadAllBytes($backupPath)
    Write-Output "Using backup as source: $backupPath"
} else {
    $srcBytes = [System.IO.File]::ReadAllBytes($srcPath)
    Copy-Item $srcPath $backupPath
    Write-Output "Backed up original to: $backupPath"
}

# Extract source PNG from ICO (assume single 256 entry in backup)
$count     = [BitConverter]::ToUInt16($srcBytes, 4)
$imgSize   = [BitConverter]::ToUInt32($srcBytes, 14)
$imgOffset = [BitConverter]::ToUInt32($srcBytes, 18)
$pngBytes  = New-Object byte[] $imgSize
[Array]::Copy($srcBytes, $imgOffset, $pngBytes, 0, $imgSize)
$pngStream = New-Object System.IO.MemoryStream(,$pngBytes)
$srcBmp    = [System.Drawing.Image]::FromStream($pngStream)
Write-Output ("Source image: {0}x{1}" -f $srcBmp.Width, $srcBmp.Height)

# Convert source Image to Bitmap (so we can use SetResolution / pixel access)
$srcBitmap = New-Object System.Drawing.Bitmap $srcBmp

# Function to make a BMP DIB ICO entry for the given size.
# Returns a byte array with: BITMAPINFOHEADER + XOR (color) + AND (mask).
function Build-DibEntry($size, $bmpSrc) {
    $bmp = New-Object System.Drawing.Bitmap $size, $size, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.InterpolationMode  = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.SmoothingMode      = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $g.PixelOffsetMode    = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
    $g.Clear([System.Drawing.Color]::Transparent)
    $g.DrawImage($bmpSrc, 0, 0, $size, $size)
    $g.Dispose()

    # Read pixels: get BGRA bottom-up
    $rect    = New-Object System.Drawing.Rectangle 0, 0, $size, $size
    $data    = $bmp.LockBits($rect, [System.Drawing.Imaging.ImageLockMode]::ReadOnly, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $stride  = $data.Stride
    $buf     = New-Object byte[] ($stride * $size)
    [System.Runtime.InteropServices.Marshal]::Copy($data.Scan0, $buf, 0, $buf.Length)
    $bmp.UnlockBits($data)
    $bmp.Dispose()

    # ICO DIB requires bottom-up rows
    $rowSize     = $size * 4
    $colorBottom = New-Object byte[] ($rowSize * $size)
    for ($y = 0; $y -lt $size; $y++) {
        $srcRow = ($size - 1 - $y) * $stride
        $dstRow = $y * $rowSize
        [Array]::Copy($buf, $srcRow, $colorBottom, $dstRow, $rowSize)
    }

    # AND mask (1bpp): row-padded to 4 bytes
    $maskRow    = [Math]::Floor(($size + 31) / 32) * 4
    $maskBytes  = New-Object byte[] ($maskRow * $size)
    # All pixels visible (alpha used for transparency in 32bpp DIB icons)
    # Mask should be all zeros (transparent where alpha=0, otherwise opaque)
    # Modern Windows uses the alpha channel; mask is mostly ignored but must exist.

    # Build BITMAPINFOHEADER (40 bytes)
    $bih = New-Object byte[] 40
    [BitConverter]::GetBytes([uint32]40).CopyTo($bih, 0)              # biSize
    [BitConverter]::GetBytes([int32]$size).CopyTo($bih, 4)            # biWidth
    [BitConverter]::GetBytes([int32]($size * 2)).CopyTo($bih, 8)      # biHeight (image+mask)
    [BitConverter]::GetBytes([uint16]1).CopyTo($bih, 12)              # biPlanes
    [BitConverter]::GetBytes([uint16]32).CopyTo($bih, 14)             # biBitCount
    [BitConverter]::GetBytes([uint32]0).CopyTo($bih, 16)              # biCompression
    [BitConverter]::GetBytes([uint32]($colorBottom.Length + $maskBytes.Length)).CopyTo($bih, 20) # biSizeImage
    # rest left as zero

    $total = New-Object byte[] ($bih.Length + $colorBottom.Length + $maskBytes.Length)
    [Array]::Copy($bih,         0, $total, 0,                                            $bih.Length)
    [Array]::Copy($colorBottom, 0, $total, $bih.Length,                                  $colorBottom.Length)
    [Array]::Copy($maskBytes,   0, $total, $bih.Length + $colorBottom.Length,            $maskBytes.Length)
    return ,$total
}

function Build-PngEntry($size, $bmpSrc) {
    $bmp = New-Object System.Drawing.Bitmap $size, $size
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.InterpolationMode  = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.SmoothingMode      = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $g.PixelOffsetMode    = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
    $g.Clear([System.Drawing.Color]::Transparent)
    $g.DrawImage($bmpSrc, 0, 0, $size, $size)
    $g.Dispose()
    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $bytes = $ms.ToArray()
    $bmp.Dispose()
    $ms.Dispose()
    return ,$bytes
}

# Build entries: BMP for <= 64, PNG for 128 and 256 (smaller file size)
$entries = @()
foreach ($size in @(16, 24, 32, 48, 64)) {
    $bytes = Build-DibEntry $size $srcBitmap
    $entries += [PSCustomObject]@{ Size = $size; Bytes = $bytes; IsPng = $false }
    Write-Output ("  BMP {0}x{0}: {1} bytes" -f $size, $bytes.Length)
}
foreach ($size in @(128, 256)) {
    $bytes = Build-PngEntry $size $srcBitmap
    $entries += [PSCustomObject]@{ Size = $size; Bytes = $bytes; IsPng = $true }
    Write-Output ("  PNG {0}x{0}: {1} bytes" -f $size, $bytes.Length)
}

# Assemble ICO
$out = New-Object System.IO.MemoryStream
$bw  = New-Object System.IO.BinaryWriter $out

$bw.Write([uint16]0)
$bw.Write([uint16]1)
$bw.Write([uint16]$entries.Count)

$headerSize = 6 + $entries.Count * 16
$dataOffset = $headerSize
foreach ($e in $entries) {
    if ($e.Size -ge 256) { $w = [byte]0; $h = [byte]0 } else { $w = [byte]$e.Size; $h = [byte]$e.Size }
    $bw.Write([byte]$w)
    $bw.Write([byte]$h)
    $bw.Write([byte]0)              # ColorCount
    $bw.Write([byte]0)              # Reserved
    $bw.Write([uint16]1)            # ColorPlanes
    $bw.Write([uint16]32)           # BitsPerPixel
    $bw.Write([uint32]$e.Bytes.Length)
    $bw.Write([uint32]$dataOffset)
    $dataOffset += $e.Bytes.Length
}
foreach ($e in $entries) {
    $bw.Write($e.Bytes)
}

$bw.Flush()
$bytes = $out.ToArray()
[System.IO.File]::WriteAllBytes($dstPath, $bytes)
$bw.Dispose()
$out.Dispose()
$srcBmp.Dispose()
$srcBitmap.Dispose()
$pngStream.Dispose()

Write-Output ""
Write-Output ("Wrote {0} ({1} bytes, {2} entries: 5 BMP + 2 PNG)" -f $dstPath, $bytes.Length, $entries.Count)
