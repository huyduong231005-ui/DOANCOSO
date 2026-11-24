"""Dữ liệu dùng chung của phân hệ thu phí: từ điển dữ liệu mục 2.6 (kiem_tra_uml.py đối chiếu với CSDL) và quy
tắc nghiệp vụ BR-TP (Chương 1, 2). Một nguồn duy nhất để báo cáo và công cụ kiểm tra không lệch nhau."""

# (thuộc tính, kiểu dữ liệu, ràng buộc, mô tả) - cùng dạng với dd trong build_report.py
DD_THU_PHI = {
    "HoaDon": [
        ("Id", "integer identity", "PK", "Khoá thay thế"),
        ("SoHoaDon", "varchar(40)", "NOT NULL, UNIQUE", "Số hoá đơn, dạng INV-<năm tháng>-<số thứ tự>"),
        ("CanHoId", "integer", "NOT NULL, FK → CanHo, RESTRICT", "Căn phải trả"),
        ("LoaiHoaDon", "integer", "NOT NULL", "0 Phí dịch vụ tháng, 3 Phát sinh (2 Phí trễ hạn để dự phòng, phí trễ "
                                              "ghi ở cột PhiTreHan)"),
        ("ThangTinhPhi", "integer", "NOT NULL; UNIQUE có lọc (RB18)", "Kỳ tính phí = năm × 100 + tháng, ví dụ 202611"),
        ("DinhKy", "boolean", "NOT NULL", "Hoá đơn do hệ thống tự lập theo kỳ"),
        ("NgayPhatHanh, NgayDenHan", "timestamp", "NOT NULL", "Ngày lập; hạn thanh toán = ngày lập + 7 ngày"),
        ("TamTinh", "numeric(18,0)", "NOT NULL", "Tổng ThanhTien của các dòng chi tiết"),
        ("GiamGia, Thue", "numeric(18,0)", "NOT NULL", "Giảm giá, thuế (hiện đều bằng 0)"),
        ("PhiTreHan", "numeric(18,0)", "NOT NULL", "Phí trễ hạn, cộng một lần (BR-TP-04)"),
        ("TongTien", "numeric(18,0)", "NOT NULL, CHECK ≥ 0", "TamTinh − GiamGia + Thue + PhiTreHan"),
        ("DaThanhToan", "numeric(18,0)", "NOT NULL, CHECK ≥ 0", "Tổng thanh toán thành công trừ phần đã hoàn"),
        ("ConLai", "numeric(18,0)", "NOT NULL, CHECK 0 ≤ ConLai ≤ TongTien", "TongTien − DaThanhToan"),
        ("TrangThai", "integer", "NOT NULL", "0 Bản nháp, 1 Đã phát hành, 2 Trả một phần, 3 Đã thanh toán, 4 Quá hạn, "
                                             "5 Đã huỷ, 6 Đã hoàn tiền"),
        ("DonViTienTe", "varchar(8)", "NOT NULL", "VND"),
        ("GhiChu", "text", "NULL", "Ghi chú, lý do huỷ"),
    ],
    "ChiTietHoaDon": [
        ("Id", "integer identity", "PK", "Khoá thay thế"),
        ("HoaDonId", "integer", "NOT NULL, FK CASCADE", "Hoá đơn"),
        ("MoTa", "varchar(500)", "NOT NULL", "Nội dung dòng, ví dụ “Phí quản lý A-01.01 – 08/2026 (95,5 m²)”"),
        ("SoLuong", "numeric(18,2)", "NOT NULL", "Diện tích, lượng tiêu thụ hoặc 1 với khoản cố định"),
        ("DonGia", "numeric(18,0)", "NOT NULL", "Đơn giá chốt lúc lập hoá đơn"),
        ("ThanhTien", "numeric(18,0)", "NOT NULL", "SoLuong × DonGia, làm tròn đồng"),
        ("ThuTu", "integer", "NOT NULL", "Thứ tự dòng trên hoá đơn"),
    ],
    "ThanhToan": [
        ("Id", "integer identity", "PK", "Khoá thay thế"),
        ("SoThanhToan", "varchar(40)", "NOT NULL, UNIQUE", "Số chứng từ, dạng PMT-<ngày>-<số thứ tự>"),
        ("HoaDonId", "integer", "NOT NULL, FK → HoaDon, RESTRICT", "Hoá đơn được trả"),
        ("SoTien", "numeric(18,0)", "NOT NULL, CHECK > 0", "Số tiền; không vượt ConLai khi ghi nhận, duyệt"),
        ("DonViTienTe", "varchar(8)", "NOT NULL", "VND"),
        ("PhuongThuc", "integer", "NOT NULL", "0 Tiền mặt, 1 Chuyển khoản, 2 Thẻ tín dụng, 3 Ví điện tử, 4 Quét QR"),
        ("TrangThai", "integer", "NOT NULL", "0 Chờ xác nhận, 1 Thành công, 2 Thất bại (bị từ chối), 3 Đã hoàn, "
                                             "4 Hoàn một phần, 5 Đã huỷ"),
        ("MaGiaoDich, NhaCungCap", "varchar(120/80)", "NULL", "Mã giao dịch ngân hàng, ngân hàng hoặc ví"),
        ("NgayThanhToan", "timestamp", "NULL", "Thời điểm thanh toán được ghi nhận thành công"),
        ("NgayHoanTien", "timestamp", "NULL", "Thời điểm hoàn tiền"),
        ("SoTienHoanTra", "numeric(18,0)", "NOT NULL", "Đã hoàn, không vượt SoTien (BR-TP-05)"),
        ("GhiChu", "text", "NULL", "Nội dung chuyển khoản, lý do từ chối"),
    ],
    "ChiSoDichVu": [
        ("Id", "integer identity", "PK", "Khoá thay thế"),
        ("CanHoId", "integer", "NOT NULL, FK → CanHo, CASCADE", "Căn"),
        ("LoaiDichVuId", "integer", "NOT NULL, FK → LoaiDichVu, RESTRICT", "Điện hoặc nước"),
        ("ThangTinhPhi", "integer", "NOT NULL; UNIQUE (CanHoId, LoaiDichVuId, ThangTinhPhi)", "Kỳ tính phí, ví dụ 202611"),
        ("ChiSoCu, ChiSoMoi", "numeric(18,2)", "NOT NULL, CHECK ChiSoMoi ≥ ChiSoCu", "Chỉ số đầu kỳ, cuối kỳ"),
        ("TieuThu", "numeric(18,2)", "NOT NULL", "ChiSoMoi − ChiSoCu"),
        ("DonGia", "numeric(18,2)", "NOT NULL", "Đơn giá chốt lúc ghi chỉ số"),
        ("ThanhTien", "numeric(18,0)", "NOT NULL", "TieuThu × DonGia, làm tròn đồng"),
        ("DaXuatHoaDon", "boolean", "NOT NULL", "Đã đưa vào hoá đơn thì không sửa, xoá (BR-TP-02)"),
        ("NgayDocChiSo", "timestamp", "NULL", "Ngày đọc đồng hồ"),
        ("GhiChu", "text", "NULL", "Ghi chú"),
    ],
    "LoaiDichVu": [
        ("Id", "integer identity", "PK", "Khoá thay thế"),
        ("Ma", "varchar(40)", "NOT NULL, UNIQUE", "Mã loại: ELEC, WATER"),
        ("Ten, DonViTinh", "varchar(120/20)", "NOT NULL", "Tên và đơn vị tính (kWh, m³)"),
        ("CheDoTinhPhi", "integer", "NOT NULL", "0 Theo đồng hồ, 1 Cố định"),
        ("DonGiaMacDinh", "numeric(18,2)", "NOT NULL", "Đơn giá đang áp dụng cho lần ghi chỉ số tiếp theo"),
        ("Icon", "text", "NULL", "Biểu tượng hiển thị"),
    ],
    "PhiDinhKy": [
        ("Id", "integer identity", "PK", "Khoá thay thế"),
        ("CanHoId", "integer", "NOT NULL, FK → CanHo, CASCADE", "Căn áp phí"),
        ("MoTa", "varchar(250)", "NOT NULL", "Nội dung, ví dụ gửi xe máy"),
        ("SoTien", "numeric(18,0)", "NOT NULL, CHECK ≥ 0", "Số tiền mỗi tháng"),
        ("ThuTu", "integer", "NOT NULL", "Thứ tự dòng trên hoá đơn"),
        ("DangApDung", "boolean", "NOT NULL", "Chỉ khoản đang áp dụng được đưa vào hoá đơn tháng"),
    ],
}

# Quy tắc nghiệp vụ thu phí, đã đối chiếu mã nguồn (InvoiceGenerator, Admin/InvoicesController, PaymentsController,
# UtilitiesController) ngày 18/11/2026.
BR_TP = [
    ("BR-TP-01", "Mỗi căn, mỗi kỳ chỉ có một hoá đơn phí tháng chưa huỷ; căn chưa bàn giao không lập. Hoá đơn gồm phí "
                 "quản lý = diện tích × đơn giá/m² của toà, tiền điện nước theo chỉ số của kỳ và các khoản phí định kỳ "
                 "đang áp dụng; hạn thanh toán = ngày lập + 7 ngày."),
    ("BR-TP-02", "Chỉ số mới không nhỏ hơn chỉ số cũ; mỗi căn, mỗi loại dịch vụ, mỗi kỳ một dòng chỉ số; chỉ số đã "
                 "đưa vào hoá đơn không được sửa, xoá hay tính lại."),
    ("BR-TP-03", "Số tiền thanh toán lớn hơn 0 và không vượt số còn lại của hoá đơn. Xác nhận chuyển khoản của cư dân "
                 "ở trạng thái Chờ xác nhận; chỉ khi BQL duyệt mới cộng vào số đã trả."),
    ("BR-TP-04", "Hoá đơn còn nợ, quá hạn đủ số ngày ân hạn của toà thì được cộng phí trễ một lần = tỉ lệ phí trễ "
                 "của toà × số còn lại."),
    ("BR-TP-05", "Số hoàn không vượt số đã thu của thanh toán; hoá đơn đã có thanh toán thành công phải hoàn tiền "
                 "trước khi huỷ; huỷ hoá đơn thì các thanh toán đang chờ bị huỷ theo."),
    ("BR-TP-06", "Số hoá đơn, số thanh toán không trùng."),
    ("BR-TP-07", "TongTien = TamTinh − GiamGia + Thue + PhiTreHan; ConLai = TongTien − DaThanhToan và "
                 "0 ≤ ConLai ≤ TongTien."),
]
