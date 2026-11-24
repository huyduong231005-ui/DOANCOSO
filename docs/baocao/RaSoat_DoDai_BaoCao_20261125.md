# Rà soát độ dài và bảng thừa — `BaoCao_DACN_Tuan1-6_sua_v2` (25/11/2026)

Hiện trạng: 138 trang, 77 bảng (khoảng 76 nghìn ký tự, hơn nửa nội dung). Chương 1: 27 trang, Chương 2: 48, Chương 3: 33,
Chương 4: 16, phần đầu khoảng 13.

Căn cứ để quyết định giữ hay bỏ là tài liệu "Các nội dung cần nghiêm túc bám sát" của thầy:
- Thầy nói rõ: "không cố gắng vẽ nhiều mô hình để tăng số trang báo cáo"; Use Case và Class Diagram là ưu tiên.
- Các sản phẩm bắt buộc ở bước 4: ERD, lược đồ quan hệ, thuyết minh 3NF, danh mục ràng buộc toàn vẹn.

Một nội dung bắt buộc chỉ cần xuất hiện **một lần**.

## A. Lỗi sai so với CSDL mới (sửa trong mọi trường hợp)

| Chỗ | Sai | Sửa thành |
|---|---|---|
| Bảng 1.10 (KPI) | `CanHo.TinhTrangThue`, `DanhGia`, "mốc giải quyết" | `CuTru`, `DiemDanhGia`, "lần chuyển sang Đã xử lý trong LichSuYeuCau" |
| Bảng 1.7 | "ghi mốc tiếp nhận" | "ghi dòng lịch sử" |
| Đặc tả UC16, UC11, UC06 | "ghi mốc giải quyết / tiếp nhận", "bỏ mốc giải quyết" | ghi dòng lịch sử (mốc không còn là cột) |
| Bảng 1.3, cột "Vai trò trên hệ thống" | `Tenant`, `Manager`, `Electrician…`, `InvoiceGenerationService` | tên vai trò tiếng Việt; tác vụ nền bỏ tên lớp |
| Bảng 3.8 | "AI đang bật? — Dịch vụ phân loại" | đã chuyển sang làn Hệ thống (nếu bảng được giữ) |
| 4.5.1 | "loại plumbing, mức medium" | "loại Cấp/thoát nước, mức Trung bình" (giữ giá trị thô trong ngoặc nếu cần) |

## B. Trùng lặp: cùng một nội dung xuất hiện nhiều lần

| # | Nội dung trùng | Xuất hiện ở | Đề xuất | Tiết kiệm |
|---|---|---|---|---|
| B1 | Bảng chuyển trạng thái sự cố | Hình 1.5 + Bảng 1.7 + bảng chuyển ở 2.5.7 | Giữ Hình 1.5 + Bảng 1.7 (có cột "ai được làm"); 2.5.7 chỉ dẫn tới Bảng 1.7 | ~0,5 tr |
| B2 | KPI và cách tính | Bảng 1.10 + bảng "Cách tính KPI" ở 2.7 (cùng 10 dòng, cùng công thức) | Giữ một bảng ở 1.6 (đã sửa lỗi A); mục 2.7 chỉ dẫn tới | ~1 tr |
| B3 | Thông tin đã bỏ khỏi thiết kế cũ | Bảng 2.3 + đoạn "Nhóm cũng bỏ…" ở 2.1.3, trùng ý với 2.4.3 a–h | Bỏ Bảng 2.3 và mục 2.1.3: báo cáo trình bày thiết kế cuối, lịch sử sửa đã có ở file rà soát góp ý | ~1,5 tr |
| B4 | Lược đồ quan hệ vẽ 3 lần | Danh sách dạng chữ 2.3.2 + Hình 2.5–2.8 (4 trang ngang) + từ điển dữ liệu 2.6 | Giữ 2.3.2 (dạng chuẩn môn học) và từ điển; bỏ 4 hình RD và mục 2.3.3 | ~4,5 tr |
| B5 | Use case và FR | Hình 1.6 + Bảng 1.11 (FR) + Bảng 1.13 (UC ↔ FR, tác nhân) | Thêm cột "Use case" vào Bảng 1.11, bỏ Bảng 1.13 | ~1 tr |
| B6 | Khoá ngoại | Bảng 2.13 (30 dòng) trùng RD và từ điển, chỉ thêm hành vi khi xoá | Thu thành một câu + bảng chỉ gồm các khoá CASCADE / SET NULL (~8 dòng), còn lại RESTRICT | ~1 tr |

## C. Bảng ít giá trị: nội dung hiển nhiên hoặc chỉ mô tả lại hình

| # | Bảng | Vì sao thừa | Đề xuất | Tiết kiệm |
|---|---|---|---|---|
| C1 | Bảng 3.9 — 51 thao tác trên sơ đồ lớp | Tên thao tác đã là tiếng Việt dễ hiểu ("taoMoi: Tạo mới") | Bỏ | ~2,5 tr |
| C2 | Bảng 2.9 — khoá ứng viên 22 quan hệ | Phần lớn các dòng giống hệt nhau ("XId → mọi thuộc tính còn lại, BCNF") | Một câu kết luận + bảng chỉ các quan hệ có khoá tự nhiên (~9 dòng) | ~1 tr |
| C3 | Bảng 2.4 — ký hiệu bản số | Kiến thức giáo trình | Một câu trong đoạn ký hiệu | ~0,3 tr |
| C4 | Bảng 3.8 — các nút quyết định | Sơ đồ 3.7 đã có đủ câu hỏi, nhánh, làn | Bỏ | ~0,5 tr |
| C5 | Bảng 3.10 + mục 3.6 — các nhóm đối chiếu | Mô tả việc làm, không phải thiết kế | Một đoạn 2–3 câu | ~0,7 tr |
| C6 | Cột "T/X/S" ở Bảng 2.10, 2.11 | Mọi dòng cùng là "+/–/+" | Bỏ cột, nói một lần trong đoạn mở | ~0,5 tr |
| C7 | Cột "STT" ở 20 bảng từ điển dữ liệu | Không mang thông tin | Bỏ cột (bảng hẹp lại, ít dòng xuống dòng) | ~0,5 tr |

## D. Giữ nguyên (bắt buộc hoặc có giá trị riêng)

- **Chương 1:**
  - Bảng 1.1 (phiếu đề xuất sáu câu hỏi, sản phẩm bước 1);
  - Bảng 1.2–1.5 (AS-IS: nguồn, tác nhân, chứng từ, bất cập);
  - Bảng 1.6 (AS-IS ↔ TO-BE);
  - Bảng 1.8 (quy tắc nghiệp vụ, có truy vết tới ràng buộc);
  - Bảng 1.9 (SLA, ghi rõ giả định);
  - Bảng 1.11–1.12 (FR, NFR, MoSCoW, tiêu chí nghiệm thu);
  - Bảng 1.16–1.17 (phân công, kế hoạch).
- **Chương 2:**
  - ERD 2.1–2.4 kèm diễn giải bản số (thầy hỏi "bản số bao nhiêu");
  - Bảng 2.2 (quy ước tên);
  - ràng buộc miền giá trị, liên thuộc tính, duy nhất, liên bộ và bảng tầm ảnh hưởng (thầy yêu cầu RBTV chi tiết);
  - trigger;
  - từ điển dữ liệu.
- **Chương 3:** kiến trúc, use case chi tiết kèm đặc tả, sơ đồ hoạt động, sơ đồ lớp, sơ đồ tuần tự.
- **Chương 4:** toàn bộ. Đây là bằng chứng sản phẩm chạy và kiểm thử, chiếm 50% điểm.

## E. Ước lượng

Làm đủ B và C: bớt khoảng **15–16 trang** (138 → khoảng 122), bớt khoảng 14 bảng. Danh mục bảng ở phần đầu ngắn đi tương ứng.
Riêng B4 (bỏ 4 hình RD) là quyết định lớn nhất: nếu thầy muốn có sơ đồ RD thì giữ lại, khi đó bớt khoảng 11 trang.

## F. Kết quả (đã làm hết A–C, 25/11/2026)

- 138 → **124 trang**; 77 → 70 bảng (kể cả 22 bảng từ điển dữ liệu); bỏ 4 hình RD. Chương 2: 48 → khoảng 37 trang.
- Đã sửa cả 6 chỗ sai ở mục A. Quét lại không còn tên cột cũ, mã tiếng Anh hay "mốc giải quyết/tiếp nhận".
- Bảng được đánh số lại tự động theo thứ tự trong từng chương (`tools/gon_bao_cao.py`, hàm `danh_so_lai`), mọi tham
  chiếu "Bảng x.y" đổi theo. Kiểm trên PDF: không có số trùng, không có tham chiếu tới bảng hay hình không tồn tại.
- Sửa thêm một lỗi có sẵn: phép thay chữ dành cho phần chữ gốc của nhóm từng chạy cả lên Chương 2, làm câu "Ba trigger
  và một hàm ở Bảng …" trỏ nhầm bảng. Nay Chương 2 không bị áp các phép thay này.

## G. Đưa RD trở lại sau khi rà theo chuẩn (25/11/2026) — 124 → 128 trang

Lỗi tìm thấy ở 4 hình RD cũ và cách sửa:

| Lỗi | Ở đâu | Sửa |
|---|---|---|
| Đường nối cắt ngang qua bảng khác | 2.5 (QuanLyId qua CuTru), 2.8 (CanHo → ChiSoDichVu qua PhiDinhKy) | Đường đi vòng có điểm giữa chỉ định |
| Hai khoá ngoại tới cùng bảng vẽ chồng thành một đường | 2.7 (NguoiBaoCaoId, NguoiPhuTrachId → NguoiDung) | Hai đường riêng, ghi tên khoá |
| Hai đường nối cắt nhau | 2.7 (lịch sử cắt đường người phụ trách) | Đường lịch sử đi vòng phía dưới |
| Bản số "ít nhất một" lệch ERD | 2.6 (người dùng có ít nhất một vai trò) | Suy từ mọi đầu (1,n) trên ERD, bỏ danh sách gõ tay |
| Thiếu dấu U của khoá tự nhiên | MaToaNha, MaCanHo, Email, TenDangNhap, TenVaiTro | Đánh dấu U cho khoá duy nhất đơn; khoá ghép nêu ở mục 2.5.4 |

Bộ kiểm tra thêm luật cho RD: đường không cắt qua bảng, không cắt nhau (trừ khi gặp sát bảng chung), khoá trùng
cặp bảng phải tách và có tên; lỗi cấy tương ứng bị bắt (8/8).
