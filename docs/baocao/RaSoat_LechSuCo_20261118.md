# Rà soát báo cáo: các chỗ đang lệch về phân hệ sự cố (18/11/2026)

Bản rà: `BaoCao_DACN_Tuan1-6.docx` (101 trang). Yêu cầu: đồ án là **toàn bộ hệ thống quản lý vận hành chung cư** (khu – toà – căn, cư dân, tài khoản, phân quyền, thu phí, công nợ, điện nước, sự cố, báo cáo), sự cố + AI chỉ là một phân hệ.

## 1. Đếm số lần nhắc tới từng phân hệ theo mục

| Mục | Sự cố | Thu phí | Căn / cư dân | Tài khoản |
|---|---:|---:|---:|---:|
| 1.1 Giới thiệu | 35 | 0 | 42 | 0 |
| 1.3 AS-IS | 35 | 1 | 44 | 3 |
| 1.4 TO-BE | 33 | 0 | 12 | 0 |
| 1.6 KPI | 10 | 1 | 1 | 0 |
| 1.8 Use case | 32 | 0 | 24 | 10 |
| 2.2 ERD | 12 | **0** | 31 | 8 |
| 2.7 Minh hoạ nghiệp vụ | 23 | 0 | 20 | 7 |
| 2.8 Truy vấn KPI | 23 | 1 | 5 | 0 |
| 3.2 Use case chi tiết | 73 | 4 | 21 | 2 |
| 3.3, 3.5 Hoạt động, tuần tự | 31 | 0 | 5 | 0 |
| 4.3 / 4.4 / 4.5 (đã viết lại) | 2 / 0 / 38 | 7 / 38 / 0 | 61 / 19 / 19 | 0 |

Chương 4 đã cân bằng; Chương 1–3 gần như không có thu phí.

## 2. Gốc của độ lệch: phạm vi ở mục 1.2

- **1.2.2 Mục tiêu tổng quát**: "xây dựng hệ thống thông tin quản lý **quy trình sự cố** của BQL…"; 5 mục tiêu cụ thể đều về sự cố (KPI sự cố, AI phân loại).
- **1.2.3 Phạm vi**: "Trong phạm vi: một quy trình trọng tâm … là quy trình sự cố, cùng các dữ liệu nền…". "**Ngoài phạm vi đánh giá: phân hệ phí dịch vụ – hoá đơn – thanh toán VietQR … không phân tích sâu trong đồ án**".
- **1.1.2** (sau Bảng 1.1): "trọng tâm vẫn là quy trình nghiệp vụ và dữ liệu sự cố".
- Lý do khi viết: file yêu cầu của thầy khuyến nghị "tập trung vào **một quy trình nghiệp vụ trọng tâm** có thể vận hành từ đầu đến cuối". Nay yêu cầu là toàn hệ thống nên phải sửa phạm vi; nếu không, **Chương 4 (đánh giá thu phí) mâu thuẫn với 1.2.3 (thu phí ngoài phạm vi đánh giá)**.

## 3. Các chỗ cụ thể

### Chương 1
**Đã làm 18/11/2026**:
- Bảng 1.3 đổi thành "Tác nhân của hệ thống", thêm việc thu phí của cư dân và quản lý, thêm tác nhân "Tác vụ nền".
- Bảng 1.4 thêm 3 chứng từ thu phí, Bảng 1.5 thêm P7, P8. Cả hai ghi rõ là giả định.
- Bảng 1.6 thêm 3 bước TO-BE thu phí. Mục 1.4.3 thêm vòng đời hoá đơn.
- Bảng 1.8 thêm BR-TP-01..07. Bảng 1.10 đổi thành "KPI của hệ thống" và thêm 4 KPI.
- Tách FR-13 thành FR-13..16. Thêm UC17–UC21 vào Bảng 1.13 và Hình 1.6 (StarUML).
- Sửa "Chương 3" thành "Chương 4" trong câu mục tiêu KPI.
- AS-IS/TO-BE dạng sơ đồ hoạt động vẫn chỉ vẽ cho sự cố (quy trình trọng tâm).

| Vị trí | Hiện trạng | Thiếu |
|---|---|---|
| Bảng 1.1 (sáu câu hỏi) | Cả 6 câu trả lời chỉ về sự cố | Bất cập thu phí: tính phí thủ công, đối soát chuyển khoản, nhắc nợ, theo dõi công nợ |
| 1.3.2 Bảng 1.3 "Tác nhân của quy trình sự cố" | Không có kế toán / thu ngân | Vai trò thu phí (hiện do Quản lý BQL làm) |
| 1.3.3–1.3.5 AS-IS | Một quy trình: tiếp nhận sự cố; bất cập P1–P6 đều về sự cố | Quy trình thu phí hiện tại + bất cập |
| 1.4 TO-BE, 1.4.3 vòng đời | Chỉ trạng thái yêu cầu sự cố | TO-BE thu phí; vòng đời hoá đơn (Nháp → Phát hành → Trả một phần → Đã thanh toán / Quá hạn / Huỷ / Hoàn tiền) |
| 1.5 Quy tắc nghiệp vụ | 14 BR-SC (sự cố), 2 BR-CH, 2 BR-CD, 1 BR-PQ; **không có BR thu phí** | Lập hoá đơn theo diện tích × đơn giá, không trùng kỳ, hạn = ngày lập + 7, phí trễ sau ân hạn, không thu vượt số còn lại, duyệt xác nhận chuyển khoản, hoàn tiền trước khi huỷ, chỉ số mới ≥ cũ… |
| 1.6 KPI | 7 KPI đều về sự cố | Tỉ lệ thu, công nợ quá hạn, tuổi nợ, tỉ lệ lấp đầy căn |
| 1.7.1 FR | FR-13 "Phí dịch vụ, hoá đơn, thanh toán VietQR" một dòng, ghi là ngoài phạm vi | Tách FR thu phí: lập hoá đơn, ghi nhận / duyệt thanh toán, hoàn tiền, chỉ số điện nước, báo cáo tài chính, tuổi nợ |
| 1.8 Use case (UC01–UC16) | 8/16 UC là sự cố; **không UC nào cho thu phí, điện nước, báo cáo tài chính** | UC lập hoá đơn, thanh toán, duyệt thanh toán, nhập chỉ số, xem báo cáo tài chính / tuổi nợ |
| 1.8.3 Đặc tả UC trọng tâm | Toàn bộ là UC sự cố | Ít nhất một UC thu phí |

### Chương 2
**Đã làm 18/11/2026**:
- Thêm 6 bảng thu phí vào 2.1–2.6 và 2.8: Bảng 2.1, 2.3, 2.4, 2.5 (RB18–RB26) và 2.6 (BR-TP-01..05).
- Viết mục 2.4.3 f về cột dẫn xuất: HoaDon, ChiTietHoaDon, ChiSoDichVu chỉ đạt 2NF, giữ có chủ đích. Đã đo 0 dòng lệch.
- Thêm từ điển Bảng 2.16–2.21, SQL công nợ phân tuổi và tỉ lệ thu.
- Vẽ lại ERD và RD bằng StarUML. `kiem_tra_uml.py` báo 0 lỗi.
- Từ điển và quy tắc BR-TP nằm ở `tools/thu_phi_data.py`.
- **Còn nợ**: Chương 1 chưa định nghĩa BR-TP-01..07, dù Chương 2 đã tham chiếu tới.

| Vị trí | Hiện trạng | Thiếu |
|---|---|---|
| 2.1.1 Danh sách thực thể | 17 thực thể, **không có bảng thu phí** | HoaDon, ChiTietHoaDon, ThanhToan, ChiSoDichVu, LoaiDichVu, PhiDinhKy (đều đang có trong CSDL) |
| 2.2 ERD, 2.3 RD | Không có 6 bảng trên | Thêm vào ERD/RD (StarUML) cùng bản số |
| 2.4 3NF | Không phân tích bảng thu phí | Phụ thuộc hàm của HoaDon (TongTien, ConLai là thuộc tính dẫn xuất — cần giải thích), ChiTietHoaDon, ThanhToan |
| 2.5 Ràng buộc | Không có ràng buộc thu phí | UX_HoaDon_PhiThang_MoiKy (một hoá đơn tháng mỗi căn mỗi kỳ), UNIQUE chỉ số theo (căn, loại, kỳ), CHECK số tiền… |
| 2.6 Từ điển dữ liệu | Không có bảng thu phí | 6 bảng |
| 2.7 Minh hoạ nghiệp vụ qua dữ liệu | Một nghiệp vụ: sự cố | Có thể thêm nghiệp vụ thu phí: chỉ số → hoá đơn → thanh toán → công nợ |
| 2.8 Truy vấn | Chỉ KPI sự cố | Truy vấn công nợ, tuổi nợ, tỉ lệ thu |

### Chương 3
**Đã làm 18/11/2026**:
- Thêm Hình 3.6 và Bảng 3.6: use case chi tiết UC20 kèm bảng quan hệ. Thêm Bảng 3.12: đặc tả UC20.
- Thêm Hình 3.9: sơ đồ lớp thu phí, gồm 8 lớp và 2 enum. Bảng 3.14 thêm 19 thao tác cùng hàm cài đặt.
- Thêm Hình 3.13: tuần tự cư dân gửi xác nhận chuyển khoản, quản lý duyệt.
- Đánh lại số Hình 3.6–3.10 và Bảng 3.6–3.14.
- `kiem_tra_uml.py`: 0 lỗi, 14/14 lỗi cấy bị phát hiện.
- Sơ đồ hoạt động thu phí chưa vẽ: chỉ quy trình trọng tâm (sự cố) có sơ đồ hoạt động.

| Vị trí | Hiện trạng | Thiếu |
|---|---|---|
| 3.2 Use case chi tiết | 4 sơ đồ (UC03, UC11, UC16, UC06) + 5 đặc tả đều sự cố; UC11 ghi "Sinh hoá đơn chi phí sửa chữa (phân hệ hỗ trợ, ngoài phạm vi đánh giá)" | UC chi tiết thu phí (vd. lập hoá đơn tháng, thanh toán – duyệt) |
| 3.3 Sơ đồ hoạt động | Chỉ luồng gửi sự cố – AI | Hoạt động lập hoá đơn / thu tiền |
| 3.4 Sơ đồ lớp | Không có lớp thu phí | HoaDon, ChiTietHoaDon, ThanhToan, ChiSoDichVu, LoaiDichVu, PhiDinhKy |
| 3.5 Sơ đồ tuần tự | 3 sơ đồ đều sự cố | Tuần tự cư dân gửi xác nhận chuyển khoản → quản lý duyệt |

### Chương 4
Đã viết theo phân hệ. Chỉ còn mâu thuẫn với 1.2.3 nêu ở mục 2.

### Khác
- Lời mở đầu: "đưa một mô hình ngôn ngữ vào một quy trình nghiệp vụ" — nhấn AI.
- Phiếu tiến độ tuần 5–6 cũng chỉ ghi luồng sự cố.

## 4. Đề xuất

Thứ tự theo mức ảnh hưởng (thầy chấm nặng phần dữ liệu và tính nhất quán):

1. **Sửa phạm vi (1.2.2, 1.2.3, 1.1.2, Bảng 1.1)**: hệ thống vận hành chung cư gồm các phân hệ; hai quy trình nghiệp vụ trọng tâm chạy trọn: **thu phí** và **sự cố** (AI là điểm cộng trong quy trình sự cố). Bỏ câu "thu phí ngoài phạm vi đánh giá".
2. **Chương 2: thêm 6 bảng thu phí** vào thực thể, ERD, RD (vẽ lại StarUML), 3NF, ràng buộc, từ điển — đây là phần thầy "kiểm soát chặt chẽ", và hiện đang thiếu so với CSDL thật; chạy lại `kiem_tra_uml.py` (hiện công cụ này cũng chỉ đối chiếu các bảng đã có trong báo cáo).
3. **Chương 1**: AS-IS/TO-BE thu phí, BR thu phí, KPI thu phí, FR và UC thu phí.
4. **Chương 3**: một UC chi tiết + đặc tả, sơ đồ lớp bổ sung lớp thu phí, một sơ đồ tuần tự thanh toán.

Khối lượng lớn nhất là mục 2 và 4 (vẽ StarUML, chạy công cụ kiểm tra nhất quán).
