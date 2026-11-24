# Rà soát toàn bộ báo cáo (19/11/2026)

Bản rà soát: `BaoCao_DACN_Tuan1-6.docx` (117 trang).

Cách làm:
- Kiểm tra tự động bằng `ra_soat.py` (scratchpad): đánh số, tham chiếu, đề mục, tài liệu tham khảo, quy ước trình bày.
- Đọc lại phần mở đầu, mục 1.9–1.10 và Chương 4.
- Đối chiếu các số liệu và hằng số với code, kiểm thử, CSDL.

## A. Lỗi nên sửa: ĐÃ SỬA 19/11/2026

Script `sua_nhom_A.py` (scratchpad); bản sao lưu `backup_Tuan1-6_truoc_sua_nhom_A.docx`; PDF 118 trang.
Sửa thêm: trạng thái tuần 4 ở Bảng 1.17 ("CSDL đang làm") thành "Hoàn thành" (mục 3.7 đã ghi xong).
Chạy lại `ra_soat.py`: không còn lỗi; PDF không còn "luxehaven".

| # | Vị trí | Vấn đề | Đề xuất sửa |
|---|---|---|---|
| A1 | Bảng 4.1 (tài khoản thử) | Email demo có đuôi `@luxehaven.vn`, trái quy ước không đưa tên này vào báo cáo | Ghi tên tài khoản (admin, quanly, tho.dien…), bỏ đuôi; thêm câu "đuôi email là tên miền thử của dữ liệu mẫu" |
| A2 | NFR-04 (Bảng 1.12) với 4.5.1 | NFR-04 đặt mục tiêu dưới 5 giây khi mô hình đã nạp, nhưng 4.5.1 đo được 6,2–6,5 giây. Báo cáo chưa nói là chưa đạt | Ghi ở 4.5.1 rằng chưa đạt NFR-04, đo trung bình ở tuần 7. Không tự hạ chỉ tiêu |
| A3 | 4.8 | Ghi tuần 5 đã phân quyền "theo VaiTro_Quyen", thực tế làm ngày 18/11 (tuần 6) | Chuyển ý này sang câu tuần 6 |
| A4 | Bảng 1.17 (kế hoạch theo tuần) | Câu dẫn nói "bám theo phiếu theo dõi tiến độ", nhưng bảng chưa theo phiếu mới (19/11): tuần 5–6 vẫn ghi "Kế hoạch", tuần 5–10 chỉ nói về sự cố | Ghi nội dung tuần 5–10 như phiếu mới; trạng thái tuần 5 là Hoàn thành, tuần 6 là Đang thực hiện |
| A5 | 4.1 với 3.1 / Hình 3.1 | 4.1 ghi Ollama "chạy cùng máy chủ", 3.1 và Hình 3.1 vẽ máy AI riêng có GPU | Ghi ở 4.1: thiết kế cho phép tách máy AI, môi trường thử chạy chung một máy (GTX 1650) |
| A6 | Danh mục chữ viết tắt | Thiếu chữ có dùng: API, BCNF, CMMS, CSRF, GUID, ITIL, MVC, MVP, SQL, URL, UTC, GVHD. Có "UQ" nhưng báo cáo dùng "U" | Thêm các dòng còn thiếu; đổi UQ thành U |

## B. Điểm do nhóm quyết định: ĐÃ SỬA 19/11/2026 theo yêu cầu

- Script `sua_nhom_B.py` (scratchpad). Bản sao lưu `backup_Tuan1-6_truoc_sua_nhom_B.docx`. PDF 121 trang.
- B1: Bảng 1.16 thêm khu – toà – căn, cư dân, thu phí – công nợ vào hạng mục ứng dụng (cùng người phụ trách). "AI Agent" đổi thành "Mô hình AI phân loại sự cố".
- B2: lời cam đoan ghi ngày 19/11/2026. Khi nộp thì đổi theo ngày nộp.
- B3: câu đầu 2.1.1 viết gọn.
- B4: ERD và RD tách mỗi loại thành hai hình:
  - Hình 2.1 ERD lõi, Hình 2.2 ERD thu phí, Hình 2.3 RD lõi, Hình 2.4 RD thu phí.
  - Hình lõi trở lại cỡ chữ cũ; hình thu phí in khoảng 8pt.
  - Trong `uml_spec.py`, hàm `part_of` tách từ sơ đồ đầy đủ.
  - `kiem_tra_uml.py` 0 lỗi, đếm được 44 bảng trên ERD/RD.
- B5: mục 2.7 thêm minh hoạ thu phí kỳ 10/2026 của căn A-12.05 (Bảng 2.25, số liệu thật trong CSDL). Tiêu đề đổi thành "Minh hoạ nghiệp vụ hoàn chỉnh qua dữ liệu".

Nội dung ban đầu của nhóm B:

| # | Vị trí | Vấn đề |
|---|---|---|
| B1 | Bảng 1.16 (phân công) | Không có hạng mục thu phí. Còn tên "AI Agent" theo tên đề tài cũ |
| B2 | Lời cam đoan | Ghi ngày 01/11/2026, sớm hơn nội dung tuần 6 trong báo cáo; nên để ngày nộp |
| B3 | 2.1.1 | Câu "chứng từ ở Bảng 1.4 và các chứng từ thu phí…" nay lặp ý, vì Bảng 1.4 đã có chứng từ thu phí |
| B4 | Hình 2.1, 2.2 | Chữ trên sơ đồ khi in chỉ khoảng 5pt. Có thể tách ERD/RD theo phân hệ (phải đánh lại số hình) |
| B5 | 2.7 | Minh hoạ nghiệp vụ qua dữ liệu chỉ có sự cố (đúng phạm vi đã chốt, không bắt buộc thêm) |

## C. Đã kiểm, đạt

- Đánh số hình, bảng, đề mục liên tục, không trùng:
  - 28 hình, 65 bảng, 85 đề mục.
  - Mọi tham chiếu "Hình x.y", "Bảng x.y", "mục x.y" đều trỏ tới chỗ có thật.
  - 13 tài liệu tham khảo, đánh số liên tục; tài liệu nào cũng được trích, không có trích dẫn trỏ tới tài liệu không có.
- Trình bày:
  - Không ô hay chữ nào tô màu, không có trang ngang.
  - Chữ đậm chỉ có ở trang bìa.
- Số liệu:
  - `dotnet test` ngày 19/11: 116/116 đạt, khớp số ghi ở 4.7 (68 `[Fact]` và 48 dòng dữ liệu của `[Theory]`).
  - `kiem_tra_uml.py`: 0 lỗi. `test_kiem_tra_uml.py`: 14/14.
- Hằng số trong báo cáo khớp code:
  - thời gian chờ AI 20 giây;
  - thư mời hết hạn sau 14 ngày;
  - sai 5 lần khoá 5 phút (`lockoutOnFailure: true`, mặc định Identity);
  - thư cảnh báo AI tối đa một lần mỗi 30 phút;
  - quét quá hạn 15 phút một lần;
  - tên năm vai trò thợ.
- Số liệu Chương 2.8 và 4.6 ghi rõ là đối chiếu ngày 18/11. Nhóm nợ theo tuổi thay đổi theo ngày nên không chạy lại.
