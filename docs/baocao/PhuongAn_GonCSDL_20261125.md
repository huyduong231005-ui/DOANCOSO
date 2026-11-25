# Phương án gọn CSDL và Chương 2 (25/11/2026) — chờ duyệt

## Nguyên tắc đặt tên (thay cho cách gắn hậu tố bảng lần trước)

- Tên ngắn, đọc như cụm tiếng Việt tự nhiên: `NgayVaoO`, `HanXuLy`, `TongTien`.
- Không gắn tên bảng vào cuối một cách máy móc (bỏ kiểu `DaThanhToanHoaDon`, `TrangThaiTruocChuyen`, `DiaChiIpNhatKy`).
- Chỉ thêm đối tượng khi bỏ đi thì tên trùng bảng khác hoặc mơ hồ: `TrangThaiHoaDon`, `TrangThaiYeuCau`.
- Khoá: tên bảng + `Id`. Khoá ngoại theo vai trò khi có nhiều khoá tới cùng một bảng: `NguoiBaoId`, `NguoiPhuTrachId`.

## Bảng: 22 → 21

| Bảng | Quyết định | Lý do |
|---|---|---|
| KhuVuc | **Bỏ** | Danh mục tỉnh/thành của website cũ; BQL chỉ quản lý các khu trong một thành phố |
| DuAn | **Đổi tên → Khu** | Đúng từ nghiệp vụ "Khu – Toà – Tầng – Căn" dùng trong báo cáo |
| Tang | Giữ | Sơ đồ toà theo tầng; căn thuộc tầng |
| Còn lại | Giữ | Đều có nghiệp vụ dùng |

Bảng kỹ thuật của ASP.NET Identity (claim, token, đăng nhập ngoài) vẫn có trong CSDL nhưng không đưa vào báo cáo.

## Cột: 169 → 136 (bỏ 33)

Ký hiệu: **bỏ**, ~~tên cũ~~ → tên mới; cột không ghi là giữ nguyên tên.

| Bảng | Cột sau khi gọn | Bỏ và lý do |
|---|---|---|
| Khu | KhuId, TenKhu, DiaChi | **bỏ** KhuVucId, ảnh, mô tả (trình bày website) |
| ToaNha | ToaNhaId, KhuId, MaToaNha, QuanLyId, TrangThaiToaNha, ~~NgayChotKyToaNha~~ → NgayLapPhi, ~~DonGiaPhiQuanLy~~ → PhiQuanLyM2, ~~SoNgayAnHanToaNha~~ → SoNgayAnHan, ~~PhanTramPhiTreToaNha~~ → TiLePhiTre | **bỏ** TenToaNha (luôn = "Toà " + mã), ảnh, mô tả |
| Tang | TangId, ToaNhaId, SoTang | — |
| CanHo | CanHoId, TangId, MaCanHo, DienTich, SoPhongNgu, TinhTrangCanHo | **bỏ** mô tả (luôn rỗng), số phòng tắm, hướng nhà (không quy tắc nào dùng) |
| CuTru | CuTruId, CanHoId, NguoiDungId, VaiTroTrongHo, ~~LaDaiDienCanHo~~ → LaDaiDien, ~~NgayBatDauCuTru~~ → NgayVaoO, ~~NgayKetThucCuTru~~ → NgayChuyenDi, ~~NgayXacNhanCuTru~~ → NgayXacNhan | **bỏ** ghi chú (luôn rỗng) |
| NguoiDung | NguoiDungId, HoTen, Email, SoDienThoai, SoGiayTo, TenDangNhap, MatKhauBam | **bỏ** ảnh đại diện, ngày đăng nhập cuối (luôn rỗng) |
| VaiTro, NguoiDung_VaiTro, VaiTro_Quyen | giữ nguyên | — |
| Quyen | QuyenId, MaQuyen, TenQuyen, ~~PhanHeQuyen~~ → PhanHe | — |
| MucUuTien | MucUuTienId, TenMucUuTien, PhutPhanHoi, PhutHoanTat | — |
| YeuCauBaoTri | YeuCauBaoTriId, SoYeuCau, CanHoId, ToaNhaId, ~~ViTriKhuVucChung~~ → ViTri, ~~NguoiBaoCaoId~~ → NguoiBaoId, NguoiPhuTrachId, MucUuTienId, LoaiSuCo, TieuDe, MoTa, TrangThaiYeuCau, ~~NgayBaoSuCo~~ → NgayBao, ~~HanXuLyYeuCau~~ → HanXuLy, ~~KetQuaXuLyYeuCau~~ → KetQuaXuLy, ~~ChiPhiThucTeYeuCau~~ → ChiPhiSuaChua, TinhPhiCuDan, ~~DiemDanhGiaYeuCau~~ → DiemDanhGia, NhanXet, PhanLoaiAi | **bỏ** chi phí ước tính (trùng vai trò với chi phí sửa chữa) |
| AnhYeuCau | AnhYeuCauId, YeuCauBaoTriId, ~~DuongDanAnh~~ → DuongDan, ~~GiaiDoanAnh~~ → GiaiDoan, ~~NguoiTaiLenId~~ → NguoiTaiId, ~~NgayTaiLenAnh~~ → NgayTai | — |
| LichSuYeuCau | LichSuYeuCauId, YeuCauBaoTriId, ~~TrangThaiSauChuyen~~ → TrangThaiMoi, NguoiThucHienId, ~~ThoiDiemChuyen~~ → ThoiDiem, ~~GhiChuChuyen~~ → NoiDung | **bỏ** trạng thái trước (luôn = trạng thái mới của dòng liền trước, 30/30) |
| NhatKyHeThong | NhatKyHeThongId, ~~TenBangNhatKy~~ → TenBang, ~~KhoaDongNhatKy~~ → KhoaDong, LoaiThaoTac, ~~NguoiGhiId~~ → NguoiThaoTacId, ~~ThoiGianGhi~~ → ThoiDiem, ~~GiaTriCuNhatKy~~ → GiaTriCu, ~~GiaTriMoiNhatKy~~ → GiaTriMoi, ~~DiaChiIpNhatKy~~ → DiaChiIp | **bỏ** tên người ghi (luôn = họ tên người dùng, 175/175), cột thay đổi (suy ra từ giá trị cũ/mới), trình duyệt |
| LoaiDichVu | LoaiDichVuId, MaLoaiDichVu, TenLoaiDichVu, ~~DonViTinhDichVu~~ → DonViTinh, ~~DonGiaDichVu~~ → DonGia | — |
| ChiSoDichVu | ChiSoDichVuId, CanHoId, LoaiDichVuId, ~~KyChiSo~~ → Ky, ChiSoCu, ChiSoMoi, ~~NgayDocChiSo~~ → NgayDoc | **bỏ** ghi chú (luôn rỗng). Giữ ChiSoCu: kỳ đầu và khi thay đồng hồ không lấy được từ kỳ trước |
| PhiDinhKy | PhiDinhKyId, CanHoId, ~~TenKhoanPhi~~ → TenPhi, ~~SoTienKhoanPhi~~ → SoTien, ~~DangApDungKhoanPhi~~ → DangApDung | **bỏ** thứ tự hiển thị |
| HoaDon | HoaDonId, SoHoaDon, CanHoId, LoaiHoaDon, ~~KyHoaDon~~ → Ky, ~~NgayPhatHanhHoaDon~~ → NgayLap, ~~NgayDenHanHoaDon~~ → HanThanhToan, ~~TamTinhHoaDon~~ → TienPhi, ~~PhiTreHanHoaDon~~ → PhiTreHan, ~~TongTienHoaDon~~ → TongTien *(tự tính)*, ~~DaThanhToanHoaDon~~ → DaThu, ~~ConLaiHoaDon~~ → ConNo *(tự tính)*, TrangThaiHoaDon, ~~GhiChuHoaDon~~ → LyDoHuy | **bỏ** giảm giá, thuế (luôn 0, không có chức năng nhập) |
| ChiTietHoaDon | ChiTietHoaDonId, HoaDonId, LoaiKhoan, ~~NoiDungKhoan~~ → NoiDung, ~~SoLuongKhoan~~ → SoLuong, ~~DonGiaKhoan~~ → DonGia, ~~ThanhTienKhoan~~ → ThanhTien *(tự tính)*, ChiSoDichVuId, PhiDinhKyId, YeuCauBaoTriId | **bỏ** thứ tự dòng |
| ThanhToan | ThanhToanId, SoThanhToan, HoaDonId, ~~SoTienThanhToan~~ → SoTien, ~~PhuongThucThanhToan~~ → PhuongThuc, TrangThaiThanhToan, ~~MaGiaoDichThanhToan~~ → MaGiaoDich, NgayThanhToan, NgayHoanTien, SoTienHoanTra, ~~GhiChuThanhToan~~ → NoiDung | **bỏ** đơn vị thanh toán (trùng ý với phương thức, luôn rỗng) |

*(tự tính)*: cột do CSDL tự tính (generated column) từ các cột khác, không ai ghi tay nên không thể lệch:
- `TongTien = TienPhi + PhiTreHan`
- `ConNo = TienPhi + PhiTreHan − DaThu`
- `ThanhTien = SoLuong × DonGia`

Nhờ vậy bỏ được các ràng buộc CHECK "phải đúng công thức".

## Ràng buộc

- Bỏ các CHECK gắn với cột đã bỏ. Bỏ 3 CHECK công thức, vì cột đã tự tính.
- Đặt lại tên CHECK cho khớp nội dung (ví dụ `CK_ThanhToan_MaSo` thực ra kiểm phương thức và trạng thái).
- Giữ: khoá, khoá ngoại, UNIQUE, CHECK miền giá trị, 3 trigger, hàm hạn xử lý.

## Chương 2 cho chuyên ngành HTTT: khoảng 48 → 22 trang

| Mục | Giữ | Bỏ / gộp |
|---|---|---|
| 2.1 Thực thể | Bảng thực thể – nghiệp vụ; quy ước tên (3 dòng) | — |
| 2.2 ERD | 4 hình ERD; một đoạn nêu các bản số đặc biệt | 4 bảng liệt kê mối kết hợp (bản số đã ghi trên hình) |
| 2.3 Lược đồ quan hệ | 4 hình RD; lý do bảng trung gian | Danh sách lược đồ dạng chữ (trùng hình RD) |
| 2.4 Chuẩn hoá | Kết luận 3NF/BCNF và 4 quyết định chuẩn hoá chính | Bảng khoá ứng viên 22 dòng |
| 2.5 Ràng buộc | Một bảng ràng buộc trên một bảng (gộp miền giá trị + liên thuộc tính); bảng ràng buộc liên bảng kèm tầm ảnh hưởng; bảng trigger | Bảng ràng buộc duy nhất, bảng khoá ngoại (đã thể hiện trên RD bằng U, FK) |
| 2.6 Từ điển dữ liệu | Quy ước kiểu dữ liệu; bảng giải thích các mã (trạng thái, loại…) | 21 bảng cấu trúc từng cột (RD đã có cột, kiểu, khoá) |
| 2.7 + 2.8 | Gộp: tổng hợp, KPI dẫn tới Bảng 1.10; 5 tính chất CSDL | — |
