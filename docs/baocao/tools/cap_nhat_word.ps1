# Mở báo cáo bằng Word, cập nhật mục lục, danh mục bảng, danh mục hình và mọi trường (số trang), lưu và xuất PDF.
param([string]$Docx = "C:\Users\ACER\Downloads\BaoCao_DACN_Tuan1-6_sua_v2.docx")
$pdf = [System.IO.Path]::ChangeExtension($Docx, ".pdf")
$truoc = @(Get-Process WINWORD -ErrorAction SilentlyContinue | ForEach-Object Id)
$batDau = Get-Date
$word = New-Object -ComObject Word.Application
$word.Visible = $false
$word.DisplayAlerts = 0
try {
    $doc = $word.Documents.Open($Docx)
    $doc.Repaginate()
    foreach ($toc in $doc.TablesOfContents) { $toc.Update() }
    $doc.Fields.Update() | Out-Null
    foreach ($toc in $doc.TablesOfContents) { $toc.UpdatePageNumbers() }
    $doc.Save()
    $doc.SaveAs([ref]$pdf, [ref]17)
    "trang: " + $doc.ComputeStatistics(2)
    $doc.Close([ref]0)
} finally {
    try { $word.Quit() } catch { }
    # Quit hay lỗi RPC và để lại Word chạy ngầm giữ khoá PDF; chỉ tắt tiến trình Word do script này mở
    # (không có trong danh sách trước khi chạy, không có cửa sổ), không đụng Word người dùng đang mở.
    Get-Process WINWORD -ErrorAction SilentlyContinue |
        Where-Object { $truoc -notcontains $_.Id -and -not $_.MainWindowTitle -and $_.StartTime -ge $batDau.AddSeconds(-5) } |
        Stop-Process -Force -Confirm:$false
}
$tep = Get-Item $pdf
if ($tep.LastWriteTime -lt $batDau) { throw "PDF không được ghi đè (đang mở ở chương trình khác?): $pdf" }
"pdf: $pdf ($($tep.LastWriteTime))"
# Lưu mốc chữ của bản vừa dựng: lần dựng sau sẽ dừng nếu file Word bị sửa tay khác mốc này.
$env:BAOCAO_OUT = $Docx
$env:PYTHONIOENCODING = "utf-8"
& python (Join-Path $PSScriptRoot "kiem_loi_bao_cao.py") --chup
Remove-Item Env:BAOCAO_OUT
