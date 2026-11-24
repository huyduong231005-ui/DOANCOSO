# Mở báo cáo bằng Word, cập nhật mục lục / danh mục bảng, hình / số trang rồi lưu và xuất PDF.
# Chạy: powershell -File docs/baocao/tools/update_fields.ps1
$ErrorActionPreference = 'Stop'
$docx = Resolve-Path (Join-Path $PSScriptRoot '..\BaoCao_DACN_Tuan1-4.docx')
$pdf = [IO.Path]::ChangeExtension($docx, '.pdf')

$word = New-Object -ComObject Word.Application
$word.Visible = $false
$word.DisplayAlerts = 0
try {
    $doc = $word.Documents.Open([string]$docx)
    # Hai lượt: lượt đầu đánh lại số trang sau khi mục lục dài ra, lượt hai cập nhật số trang trong mục lục.
    for ($i = 0; $i -lt 2; $i++) {
        $doc.Fields.Update() | Out-Null
        foreach ($toc in $doc.TablesOfContents) { $toc.Update() }
    }
    $doc.Save()
    $doc.ExportAsFixedFormat([string]$pdf, 17)   # 17 = wdExportFormatPDF
    "Pages: " + $doc.ComputeStatistics(2)        # 2 = wdStatisticPages
    $doc.Close($false)
}
finally {
    # Word đôi khi đã tự thoát sau ExportAsFixedFormat; lỗi RPC lúc Quit không ảnh hưởng file đã lưu.
    try { $word.Quit() } catch { }
}
"PDF: $pdf"
