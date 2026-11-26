# Danh sách yêu cầu sửa báo cáo DACN (tài liệu theo dõi nội bộ, không đưa vào báo cáo)

Mỗi lần sửa báo cáo: đọc lại toàn bộ danh sách này, rà **từng chương** theo từng dòng, ghi kết quả vào cột Trạng thái.
Một yêu cầu nêu cho một chỗ (ví dụ "bỏ cột biểu diễn ở Bảng 2.3") phải áp cho **mọi chỗ cùng loại** nếu lý do của
người dùng áp được (ở đây: "quá trừu tượng, ưu tiên dễ hiểu").

## A. Nguyên tắc chung (áp mọi chương)

| # | Yêu cầu | Nguồn |
|---|---|---|
| A1 | Báo cáo là nhóm báo cáo với thầy (người với người): không đưa quy ước, thoả thuận giữa người dùng và Claude, quy trình làm việc nội bộ, câu "theo nhận xét của giảng viên…" vào báo cáo | Người dùng 25/11 |
| A2 | Ưu tiên dễ hiểu: bỏ biểu diễn hình thức (∀, ∃, ⇒…), bỏ chi tiết kỹ thuật trừu tượng khi lời thường nói được | Người dùng 25/11 |
| A3 | Không còn quy ước đặt tên trong báo cáo (bảng quy ước, câu "tên… là…", "bỏ tiền tố CK_", "bỏ hậu tố Id"…) | Người dùng 25/11 |
| A4 | Văn phong: ngắn gọn, giống người viết, một ngôi "nhóm"; câu nào bỏ đi mà không mất nghĩa thì bỏ | Người dùng 24/11 |
| A5 | Tên trong bảng, sơ đồ đọc hiểu ngay; tên CSDL ngắn, không hậu tố máy móc | Người dùng 25/11 |
| A6 | Không ghi "Luxe Haven" trong báo cáo | Người dùng |
| A7 | Chuyên ngành HTTT: bớt phần CSDL không cần thiết | Người dùng 25/11 |
| A8 | Trích dẫn dễ hiểu: không dồn số [x], [y] liền nhau; số đi kèm tên tài liệu | Người dùng 25/11 |
| A9 | Đồng bộ mọi chỗ liên quan khi xoá/sửa một phần (tham chiếu mục, bảng, hình; kế hoạch; cấu trúc đồ án; Chương 1, 3, 4) | Người dùng 25/11 |

## B. Góp ý tuần 6 của thầy (vẫn giữ)

| # | Yêu cầu |
|---|---|
| B1 | Sơ đồ đặt trang ngang, không xoay ảnh; không ghi "vẽ bằng StarUML" ở chú thích |
| B2 | Người dùng và cư dân là một; căn hộ – người là quan hệ cư trú, không phải hợp thành |
| B3 | Activity: mỗi ô một hành động cụ thể; trước nút quyết định là hành động kiểm tra; nút quyết định là câu hỏi |
| B4 | Sequence: mọi mũi tên (trừ mũi tên đầu) đi ra từ thanh kích hoạt |
| B5 | Use case đặt tên theo mục tiêu tác nhân; không có UC kiểu "hỏi lại AI", "ghi nhận yêu cầu"; không kèm bảng quan hệ UC |
| B6 | Không đưa mã trigger, mã AI; không minh hoạ dữ liệu |
| B7 | Một thông tin chỉ lưu ở một bảng; không trùng tên thuộc tính máy móc |

## C. Cơ sở dữ liệu

| # | Yêu cầu |
|---|---|
| C1 | Lọc toàn bộ CSDL: bỏ bảng/cột thừa, lặp; tên cột ngắn, dễ đọc |
| C2 | LoaiDichVu, ChiSoDichVu, PhiDinhKy không trùng thông tin; phân rõ vai trò từng bảng thu phí |
| C3 | ChiTietHoaDon là bảng chi tiết chuẩn (khoá ghép hoá đơn + loại dịch vụ) |
| C4 | Phí quản lý một giá chung trong bảng giá (bỏ đơn giá theo toà) |
| C5 | Không gộp cư trú vào người dùng (đã trả lời lý do) |

## D. Chương 2

| # | Yêu cầu |
|---|---|
| D1 | Bỏ Bảng 2.2 "Ba quy ước đặt tên" |
| D2 | Một ERD tổng, một RD tổng; cột rút gọn; RD không ghi kiểu dữ liệu; RD vẽ rộng |
| D3 | Bỏ cột Biểu diễn ở bảng ràng buộc (Bảng 2.3 và cả Bảng 2.4) |
| D4 | Bỏ mục 2.6 "Kiểu dữ liệu và bảng mã" (giữ Bảng trigger và hàm) |
| D5 | Bảng 2.1: tên các dòng thống nhất một kiểu |

## E. Use case, sơ đồ lớp, Chương 1, 3

| # | Yêu cầu |
|---|---|
| E1 | Use case chỉ trình bày một lần (mục 1.8); chỉ use case trọng tâm UC03, UC16 có hình chi tiết và đặc tả |
| E2 | Hình UC03, UC16 phân rã tới chức năng lá; đặc tả bỏ phần hình đã thể hiện |
| E3 | Một sơ đồ lớp tổng |
| E4 | Đoạn kỹ thuật mục 1.1.3 viết lại dễ hiểu |

## Kết quả rà ngày 25/11/2026 (tối)

Đã đọc toàn văn từng chương theo danh sách; mọi mục A1–E4 đạt trên bản dựng mới. Chỗ sót đã sửa trong lượt này:
- D3: Bảng 2.4 còn cột Biểu diễn → bỏ; câu mở mục 2.5 không còn nhắc "biểu diễn hình thức".
- A2: Bảng 2.6 viết lại bằng lời (bỏ "BEFORE UPDATE OF", tên hàm, ký hiệu ≤).
- D4/A7: mục 2.3 còn câu kiểu dữ liệu (numeric, varchar…) và câu khoá số nguyên tự tăng → bỏ.
- D5: Bảng 2.1 dòng gộp "VAI_TRO, QUYEN" → tách hai dòng.
- A1: đoạn sơ đồ tuần tự còn luật "thanh kích hoạt"; câu "bội số lấy theo khoá ngoại" → bỏ.
- A9: câu "chạy thử … thuộc phần việc tiếp theo" (Chương 3) đã cũ → bỏ; danh mục viết tắt bỏ GUID, HTTT, LLM, RD (không còn dùng).

## Kết quả rà Chương 4 ngày 25/11/2026 (đêm)

Đọc toàn văn Chương 4, đối chiếu từng câu với mã nguồn, từng số với CSDL (truy vấn chỉ đọc), chụp lại và xem từng hình.
- Số liệu đúng: 5 toà, 538 căn, 748 cư dân, kỳ 8–11/2026; giá quản lý 15.000 đ/m², xe máy 120.000 đ; hạn = ngày lập + 7;
  khoá 5 lần/5 phút; phiên hết hiệu lực sau 1 phút; quét quá hạn 15 phút; Bảng 4.5 khớp lịch sử YC-202611-0001.
- Bảng 4.7 đổi sang ngày 25/11 (khớp ngày chụp Hình 4.5–4.7); nhóm tuổi nợ đổi: 1–30 ngày = 0, 31–60 = 316.048.734.
  Mọi số trên app (bảng điều khiển, tình trạng căn, tài chính, tuổi nợ, KPI) khớp SQL.
- Sai với mã nguồn → sửa: phân loại lại chỉ tính lại hạn khi đổi mức ưu tiên (Bảng 4.6).
- A2/A5: bỏ prompt, tham số sinh, token, xmin, KetQuaXuLy, "tem bảo mật"; Bảng 4.2 ghi tên quyền tiếng Việt thay mã
  projects.view…; UTC giải thích bằng lời; bỏ câu thừa về tên miền email thử; câu "đổi loại thành" lặp → gọn.
- Đồng bộ tên trạng thái: giao diện đổi theo báo cáo (Đang xử lý, Đã xử lý, Từ chối); Bảng 1.10 "Mới gửi, Đã tiếp nhận".
- Giao diện (thấy khi xem ảnh): "Quản lí"→"Quản lý", "tòa"→"toà", dòng phí quản lý lên đầu hoá đơn, dòng tổng thẳng cột,
  kỳ ghi thống nhất 8/2026, nhãn trạng thái không xuống dòng, trục biểu đồ "600tr" (bỏ ".0"), "còn dư/Số dư"→"còn nợ",
  câu báo lỗi "đã Đã huỷ"; Hình 4.1 chụp khung rộng hơn cho đủ cột; ô ngày Hình 4.7 hiện dd/mm/yyyy.

## Rà toàn báo cáo ngày 26/11/2026

Quét tự động mọi mục A–E và đọc toàn văn Chương 1–4; xem lại các trang sơ đồ trên PDF. Sửa thêm:
- A5: Chương 1 còn tên bảng máy trong câu (NguoiDung, CuTru) và trong cột công thức Bảng KPI (HanXuLy, NgayBao,
  PhutPhanHoi, DiemDanhGia, HanThanhToan) → viết bằng lời; cột "Dữ liệu dùng" vẫn ghi tên bảng.
- A9: bảng kế hoạch theo tuần: tuần 6 → Hoàn thành, tuần 7 → Đang thực hiện.
- A9: Chương 3 nói "Chương 4 trình bày đánh giá AI trên 120 ca" trong khi Chương 4 ghi việc này làm ở tuần 7–9 → bỏ.
Không còn: Luxe Haven, quy ước đặt tên, cột Biểu diễn, mã trigger/SQL, ký hiệu ∀∃⇒, trích dẫn dồn số, chữ "lí/tòa".

## Rà mọi chương lần 2 ngày 26/11/2026 (sau câu hỏi về 2a, E1)

Tiêu chí thêm: mọi ký hiệu, mã số người đọc phải hiểu ngay, không cần biết quy ước riêng. Xem cả 80 trang PDF bằng mắt.
- Đặc tả UC03, UC16: "2a, 4a…" → "Ở bước 2, nếu…"; "E1–E4" → bỏ mã, ghi thẳng tình huống lỗi.
- Bảng 1.8: thêm câu nói rõ mã RB, LB, TG, FN là ràng buộc, trigger, hàm ở mục 2.5.
- Bảng kế hoạch "câu hỏi Bước 1" → "câu hỏi nghiệp vụ (Bảng 1.1)"; Bảng 3.1 "q4_k_m", "ghi log", "nhắc hạn" → lời thường;
  mã 403, 404 giải thích lần đầu; tác vụ nền ở Bảng 1.3 thêm việc báo yêu cầu quá hạn (khớp mục 4.5.3).
- Trình bày: 11 dòng gạch đầu dòng mất tab (chữ dính vào dấu gạch, lệch lề) → sửa trong pipeline (sua_gach_dau_dong);
  Hình 4.5, 4.6 giới hạn cao 10 cm để chung một trang (trước mỗi hình bỏ trống nửa trang). Báo cáo còn 79 trang.
