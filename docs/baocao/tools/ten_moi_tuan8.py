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
