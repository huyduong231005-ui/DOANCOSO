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
