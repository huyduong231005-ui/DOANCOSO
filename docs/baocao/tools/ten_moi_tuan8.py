"""Đổi tên cột trong phần chữ của nhóm (ngoài Chương 2) sang tên sau đợt gọn CSDL 25/11/2026.

Khoá là tên trong văn bản gốc; giá trị là tên cột hiện tại. Tên gốc đã ngắn, dễ đọc (NgayVaoO, HanXuLy, TongTien…)
thì giữ nguyên, không có trong bảng này. SUA_CUOI sửa các tham chiếu mục và câu nói về bảng, cột đã bỏ.
"""
import re

DOI = {
    # HoaDon
    "NgayPhatHanh": "NgayLap", "NgayDenHan": "HanThanhToan", "TamTinh": "TienPhi",
    "DaThanhToan": "DaThu", "ConLai": "ConNo",
    # YeuCauBaoTri, LichSuYeuCau, AnhYeuCau
    "ChiPhiThucTe": "ChiPhiSuaChua", "TrangThaiSau": "TrangThaiMoi", "NgayTaiLen": "NgayTai",
    # ToaNha, ChiSoDichVu
    "NgayChotKy": "NgayLapPhi", "PhanTramPhiTre": "TiLePhiTre", "KyChiSo": "Ky",
}
DOI_CAMEL = {k[0].lower() + k[1:]: v[0].lower() + v[1:] for k, v in DOI.items()}

_PAT = re.compile(r"\b(" + "|".join(sorted(DOI, key=len, reverse=True)) + r")\b")
_PAT_CAMEL = re.compile(r"\b(" + "|".join(sorted(DOI_CAMEL, key=len, reverse=True)) + r")\b")

# Sửa sau cùng (chuỗi con của chữ đã đổi tên): tham chiếu mục của Chương 2 mới và câu về bảng, cột đã bỏ.
SUA_CUOI = [
    ("đủ cột xem ở từ điển dữ liệu mục 2.6. Các thuộc tính đánh dấu {dẫn xuất} không có cột trong cơ sở dữ liệu mà "
     "được tính ra (mục 2.4.3 d).",
     "đủ cột xem ở lược đồ quan hệ mục 2.3. Giá trị tính ra từ dữ liệu khác (tên toà, trạng thái trước, tổng tiền, "
     "còn nợ, thành tiền) ghi thành thao tác trả về giá trị, không phải thuộc tính."),
    ("KhuVuc có nhiều DuAn; DuAn có nhiều ToaNha;", "Khu có nhiều ToaNha;"),
    ("khu vực chung của 0..1 toà (toaNhaKhuVucChung), có người báo (nguoiBaoCao)",
     "khu vực chung của 0..1 toà (toaNha), có người báo (nguoiBao)"),
    ("đúng như thiết kế ở mục 2.3.4.", "đúng như thiết kế ở mục 2.3."),
    ("ngày chốt kỳ", "ngày lập phí"),
    # Đợt bảng giá dịch vụ: bỏ mục kiểu dữ liệu, phí định kỳ thành đăng ký dịch vụ, phí quản lý theo bảng giá.
    ("đúng thứ tự giá trị lưu trong cơ sở dữ liệu (Hình 1.5 và mục 2.6).",
     "đúng thứ tự giá trị lưu trong cơ sở dữ liệu (Hình 1.5)."),
    ("đúng thứ tự giá trị lưu trong cơ sở dữ liệu (mục 2.6).", "đúng thứ tự giá trị lưu trong cơ sở dữ liệu."),
    ("phí quản lý bằng diện tích nhân đơn giá của toà, cộng tiền điện nước theo chỉ số của kỳ và các phí cố định "
     "(gửi xe…).", "mỗi loại dịch vụ một dòng gồm phí quản lý theo diện tích, điện nước theo chỉ số của kỳ và các "
     "dịch vụ căn đăng ký (gửi xe…); đơn giá lấy từ bảng giá."),
    ("Hệ thống tự lập hoá đơn tháng từ diện tích, chỉ số, phí định kỳ",
     "Hệ thống tự lập hoá đơn tháng từ diện tích, chỉ số và dịch vụ căn đăng ký"),
    ("ghi chỉ số điện nước, phí định kỳ của căn", "ghi chỉ số điện nước, đăng ký dịch vụ của căn"),
    ("Hoá đơn = diện tích × đơn giá + điện nước + phí cố định",
     "Hoá đơn = phí quản lý theo diện tích + điện nước theo chỉ số + dịch vụ đăng ký, đơn giá theo bảng giá"),
    ("thu phí (chỉ số, phí định kỳ,", "thu phí (chỉ số, đăng ký dịch vụ,"),
    # Rà lại 25/11 chiều: bỏ câu quy ước (đặt tên, luật vẽ), sửa chữ còn theo thiết kế cũ.
    ("Mỗi ô là một hành động cụ thể; bất cập của từng bước", "Bất cập của từng bước"),
    (" Mã quy tắc được dùng lại nhất quán trong tài liệu thiết kế, mã nguồn (chú thích) và ca kiểm thử.", ""),
    ("chi phí ước tính, thực tế không âm", "chi phí sửa chữa không âm"),
    ("Cư dân đã đăng nhập, tài khoản gắn với một bản ghi cư dân đang ở (BR-SC-01).",
     "Cư dân đã đăng nhập và đang cư trú tại căn (BR-SC-01)."),
    ("ràng buộc toàn vẹn (CHECK, trigger, hàm); kiểu dữ liệu và bảng mã", "ràng buộc toàn vẹn (CHECK, trigger, hàm)"),
    ("chuẩn hoá, ràng buộc toàn vẹn, kiểu dữ liệu và bảng mã.", "chuẩn hoá và ràng buộc toàn vẹn."),
    (" Các sơ đồ ở mức phân tích và thiết kế nên chỉ dùng tên nghiệp vụ; tên lớp và thuộc tính khớp với tên bảng và "
     "tên cột ở Chương 2.", ""),
    (" Tên lớp và thuộc tính đặt theo tên bảng, tên cột ở Chương 2; mỗi lớp chỉ ghi những thuộc tính chính, đủ cột xem "
     "ở lược đồ quan hệ mục 2.3.", " Mỗi lớp chỉ ghi những thuộc tính chính."),
    (" Tên quan hệ (quanLy, nguoiBaoCao…) là tên cột khoá ngoại bỏ hậu tố Id.", ""),
    ("(ràng buộc miền giá trị MG05 chặn ở cơ sở dữ liệu)", "(ràng buộc RB05 chặn ở cơ sở dữ liệu)"),
    (" Mỗi ô là một hành động cụ thể; trước mỗi nút quyết định là một hành động kiểm tra và nút quyết định ghi câu hỏi. "
     "Mỗi nút quyết định có đúng một luồng vào và các nhánh ra đều có điều kiện; nơi nhiều luồng gặp nhau luôn có nút "
     "gộp, để không hành động nào phải chờ nhiều luồng cùng lúc.", ""),
    (" Mỗi lời gọi mở một thanh kích hoạt ở đối tượng nhận, kéo dài tới lúc trả về; mọi mũi tên, trừ mũi tên đầu tiên "
     "của tác nhân, đi ra từ thanh kích hoạt.", ""),
    ("Đơn giá âm, ngày chốt 40, phí trễ −5%, số tầng 0", "Ngày lập phí 40, phí trễ −5%, số tầng 0"),
    ("Căn 65 m², đơn giá 15.000 đ/m², phí gửi xe 120.000 đ", "Căn 65 m², phí quản lý 15.000 đ/m², đăng ký 1 xe máy "
     "120.000 đ"),
    ("Hoá đơn 1.095.000 đ = 975.000 + 120.000", "Hoá đơn hai dòng: 975.000 + 120.000 = 1.095.000 đ"),
    ("toà chốt ngày 1", "toà lập phí ngày 1"),
    ("Nếu máy chủ tắt đúng ngày chốt,", "Nếu máy chủ tắt đúng ngày lập phí,"),
    ("các toà đã qua ngày chốt", "các toà đã qua ngày lập phí"),
    ("cố định theo ngày chốt nên", "cố định theo ngày lập phí nên"),
    ("Kết quả ghi ở GhiChuXuLy", "Kết quả ghi ở KetQuaXuLy"),
    ("mục 2.5.7", "mục 2.5.3"),
    ("(khu vực của toà, xem mục 2.4.3 b)", "(địa chỉ của toà, xem mục 2.4)"),
    ("Số liệu trùng với truy vấn SQL đối chiếu (mục 2.7)", "Số liệu trùng với truy vấn SQL đối chiếu"),
    ("lược đồ quan hệ, phụ thuộc hàm, 3NF; danh mục ràng buộc (PK, FK, UNIQUE, CHECK; giao dịch, trigger, hàm); từ "
     "điển dữ liệu; khung nhìn dẫn xuất và truy vấn KPI",
     "lược đồ quan hệ, chuẩn hoá; ràng buộc toàn vẹn (CHECK, trigger, hàm); kiểu dữ liệu và bảng mã"),
    ("chuẩn hoá 3NF, ràng buộc toàn vẹn, từ điển dữ liệu, giá trị dẫn xuất và KPI.",
     "chuẩn hoá, ràng buộc toàn vẹn, kiểu dữ liệu và bảng mã."),
]


def doi_ten(s, camel=False):
    """Đổi tên theo ranh giới từ (không đụng tên dài hơn có chứa tên cũ, như TamTinhTinhLai)."""
    s = _PAT.sub(lambda m: DOI[m.group(1)], s)
    s = _PAT_CAMEL.sub(lambda m: DOI_CAMEL[m.group(1)], s) if camel else s
    for a, b in SUA_CUOI:
        s = s.replace(a, b)
    return s


if __name__ == "__main__":
    assert doi_ten("TongTien = TamTinh + PhiTreHan") == "TongTien = TienPhi + PhiTreHan"
    assert doi_ten("TamTinhTinhLai, NgayDenHan") == "TamTinhTinhLai, HanThanhToan"
    assert doi_ten("trigger TG1 (mục 2.5.7)") == "trigger TG1 (mục 2.5.3)"
    print("ok", len(DOI), "tên")
