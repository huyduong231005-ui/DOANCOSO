# Mở báo cáo bằng Word, cập nhật mục lục, danh mục bảng, danh mục hình và mọi trường (số trang), lưu và xuất PDF.
param([string]$Docx = "C:\Users\ACER\Downloads\BaoCao_DACN_Tuan1-6_sua_v2.docx")
$pdf = [System.IO.Path]::ChangeExtension($Docx, ".pdf")
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
} finally { $word.Quit() }
"pdf: $pdf"
