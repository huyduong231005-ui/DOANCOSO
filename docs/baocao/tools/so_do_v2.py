"""Sơ đồ của báo cáo sau góp ý tuần 6 của GVHD (24/11/2026), dựng bằng StarUML.

Chạy: python docs/baocao/tools/so_do_v2.py
  -> docs/baocao/staruml/BaoCao_DACN_v2.mdj   (file StarUML gốc)
  -> docs/baocao/images_v2/Hinh x.y ....png   (ảnh xuất từ StarUML, nền trắng)

Quy cách theo góp ý:
- Sơ đồ trải theo chiều ngang, đặt trên trang ngang của báo cáo (không xoay ảnh).
- Sơ đồ hoạt động: mỗi ô là một hành động cụ thể (động từ + đối tượng, không diễn giải); trước mỗi nút
  quyết định là một hành động kiểm tra, nút quyết định là một câu hỏi; nhiều luồng vào thì qua nút gộp.
- Sơ đồ tuần tự: thanh kích hoạt kéo dài tới lúc trả về, mọi mũi tên (trừ mũi tên đầu) đi ra từ thanh kích hoạt.
- ERD: thực thể (khoá chính gạch chân), mối kết hợp hình thoi, bản số (min,max) đặt cạnh thực thể;
  người dùng và cư dân là một (NGUOI_DUNG), cư trú là mối kết hợp chứ không phải hợp thành.
- Tên use case là mục tiêu của tác nhân; không vẽ bước nội bộ của hệ thống thành use case.
"""
import json
from pathlib import Path

from PIL import ImageFont

import staruml_cli
import uml_spec_ch3  # noqa: F401  (nạp trước để tránh vòng import uml_spec <-> uml_spec_ch3)
import uml_spec

HERE = Path(__file__).resolve().parent
ROOT = HERE.parent
MDJ = ROOT / "staruml" / "BaoCao_DACN_v2.mdj"
IMG = ROOT / "images_v2"
WORK = ROOT / "staruml" / "_work_v2"

F13 = ImageFont.truetype(r"C:\Windows\Fonts\times.ttf", 13)
F13B = ImageFont.truetype(r"C:\Windows\Fonts\timesbd.ttf", 13)


# ======================================================================= sơ đồ hoạt động
class Act:
    """Sơ đồ hoạt động làn dọc. Toạ độ theo (làn, độ lệch trong làn, hàng). Mọi nút được ghi lại kích thước
    để tính điểm gấp của luồng."""

    LW, ROW, AW, AH = 270, 62, 170, 40

    def __init__(self, lanes, title, name, model, widths=None):
        self.lanes, self.title, self.name, self.model = lanes, title, name, model
        self.widths = widths or [self.LW] * len(lanes)
        self.nodes, self.edges, self.g = [], [], {}
        self.x0 = [20 + sum(self.widths[:i]) for i in range(len(lanes))]

    def cx(self, lane, dx=0):
        return self.x0[lane] + self.widths[lane] // 2 + dx

    def y(self, row):
        return 60 + row * self.ROW

    def _add(self, key, kind, lane, row, w, h, dx=0, name=None):
        x, y = self.cx(lane, dx) - w // 2, self.y(row) - h // 2
        n = {"key": key, "kind": kind, "x": x, "y": y, "w": w, "h": h}
        if name:
            n["name"] = name
        self.nodes.append(n)
        self.g[key] = (x, y, w, h)

    def act(self, key, lane, row, name, dx=0, w=None):
        self._add(key, "action", lane, row, w or self.AW, self.AH, dx, name)

    def dec(self, key, lane, row, question, dx=0, left=False):
        """Nút quyết định; câu hỏi ghi ngay trên hình thoi, lệch sang phía không có nhãn nhánh."""
        self._add(key, "decision", lane, row, 34, 34, dx)
        x, y, w, h = self.g[key]
        tw = int(F13.getlength(question)) + 10
        qx = x + w // 2 - 6 - tw if left else x + w // 2 + 6
        self.nodes.append({"key": key + "_q", "kind": "text", "name": question, "x": qx, "y": y - 20,
                           "w": tw, "h": 18})

    def merge(self, key, lane, row, dx=0):
        self._add(key, "merge", lane, row, 34, 34, dx)

    def start(self, key, lane, row, dx=0):
        self._add(key, "initial", lane, row, 20, 20, dx)

    def final(self, key, lane, row, dx=0):
        self._add(key, "final", lane, row, 26, 26, dx)

    # điểm neo
    def T(self, k): x, y, w, h = self.g[k]; return [x + w // 2, y]
    def B(self, k): x, y, w, h = self.g[k]; return [x + w // 2, y + h]
    def L(self, k): x, y, w, h = self.g[k]; return [x, y + h // 2]
    def R(self, k): x, y, w, h = self.g[k]; return [x + w, y + h // 2]

    def flow(self, a, b, guard=None, points=None):
        e = {"kind": "flow", "from": a, "to": b}
        if guard:
            e["guard"] = guard
        if points is None:
            points = self.auto(a, b)
        if points:
            e["points"] = points
        self.edges.append(e)

    def auto(self, a, b):
        """Thẳng nếu thẳng hàng; ngược lại đi ngang rồi xuống (hoặc xuống rồi ngang)."""
        ax, ay, aw, ah = self.g[a]
        bx, by, bw, bh = self.g[b]
        acx, acy, bcx, bcy = ax + aw // 2, ay + ah // 2, bx + bw // 2, by + bh // 2
        if abs(acx - bcx) < 4:
            return [self.B(a), self.T(b)] if bcy > acy else [self.T(a), self.B(b)]
        if abs(acy - bcy) < 4:
            return [self.R(a), self.L(b)] if bcx > acx else [self.L(a), self.R(b)]
        # ngang từ cạnh bên của a tới trục của b, rồi dọc vào đỉnh (hoặc đáy) của b
        start = self.R(a) if bcx > acx else self.L(a)
        end = self.T(b) if bcy > acy else self.B(b)
        return [start, [bcx, start[1]], end]

    def spec(self):
        bottom = max(n["y"] + n["h"] for n in self.nodes) + 30
        parts = [{"key": f"lane{i}", "kind": "partition", "name": n, "x": self.x0[i], "y": 10,
                  "w": self.widths[i], "h": bottom - 10} for i, n in enumerate(self.lanes)]
        return {"kind": "activity", "model": self.model, "owner": self.title, "name": self.name,
                "title": self.title, "fontSize": 13, "nodes": parts + self.nodes, "edges": self.edges}


def as_is():
    a = Act(["Cư dân", "Lễ tân", "Kỹ thuật trưởng", "Kỹ thuật viên"], "Tiếp nhận và xử lý sự cố (AS-IS)",
            "Hinh 1.1 Quy trinh AS-IS", "Quy trình AS-IS")
    CD, LT, KT, TV = 0, 1, 2, 3
    a.start("i", CD, 0)
    a.act("a1", CD, 1, "Phát hiện sự cố")
    a.act("a2", CD, 2, "Báo sự cố cho lễ tân")
    a.act("b1", LT, 2, "Ghi sổ tiếp nhận")
    a.act("b2", LT, 3, "Báo kỹ thuật trưởng")
    a.act("c1", KT, 3, "Đánh giá mức độ sự cố")
    a.merge("m1", KT, 4)
    a.act("c2", KT, 5, "Kiểm tra thợ đang rảnh")
    a.dec("d1", KT, 6, "Có thợ phù hợp?")
    a.act("c3", KT, 7, "Hẹn lại cư dân", dx=-62, w=118)
    a.act("c4", KT, 7, "Gọi thợ giao việc", dx=62, w=118)
    a.act("t1", TV, 7, "Đến căn hộ kiểm tra")
    a.act("t2", TV, 8, "Sửa chữa sự cố")
    a.act("t3", TV, 9, "Báo đã xử lý xong")
    a.act("b3", LT, 9, "Cập nhật sổ tiếp nhận")
    a.final("f", LT, 10)
    for x, y in [("i", "a1"), ("a1", "a2"), ("a2", "b1"), ("b1", "b2"), ("b2", "c1"), ("c1", "m1"), ("m1", "c2"),
                 ("c2", "d1"), ("c4", "t1"), ("t1", "t2"), ("t2", "t3"), ("t3", "b3"), ("b3", "f")]:
        a.flow(x, y)
    a.flow("d1", "c3", "không", [a.L("d1"), [a.T("c3")[0], a.L("d1")[1]], a.T("c3")])
    a.flow("d1", "c4", "có", [a.R("d1"), [a.T("c4")[0], a.R("d1")[1]], a.T("c4")])
    xl = a.x0[KT] + 12
    a.flow("c3", "m1", points=[a.L("c3"), [xl, a.L("c3")[1]], [xl, a.L("m1")[1]], a.L("m1")])
    return a.spec()


def to_be():
    a = Act(["Cư dân", "Hệ thống", "Quản lý BQL", "Kỹ thuật viên"], "Tiếp nhận và xử lý sự cố (TO-BE)",
            "Hinh 1.4 Quy trinh TO-BE", "Quy trình TO-BE")
    CD, HT, QL, TV = 0, 1, 2, 3
    a.start("i", CD, 0)
    a.merge("m0", CD, 1)
    a.act("a1", CD, 2, "Gửi yêu cầu sự cố")
    a.act("s1", HT, 2, "Kiểm tra thông tin yêu cầu")
    a.dec("d0", HT, 3, "Thông tin hợp lệ?")
    a.act("s0", HT, 4, "Thông báo lỗi nhập liệu", dx=-62, w=118)
    a.act("s2", HT, 4, "Phân loại sự cố bằng AI", dx=62, w=118)
    a.act("s3", HT, 5, "Lưu yêu cầu", dx=62, w=118)
    a.act("s4", HT, 6, "Kiểm tra thợ phù hợp", dx=62, w=118)
    a.dec("d1", HT, 7, "Có thợ phù hợp?", dx=62, left=True)
    a.act("s5", HT, 8, "Giao việc cho thợ", dx=62, w=118)
    a.act("q1", QL, 7, "Kiểm tra nội dung yêu cầu")
    a.dec("d2", QL, 8, "Chấp nhận yêu cầu?")
    a.act("q2", QL, 9, "Từ chối yêu cầu", dx=-62, w=118)
    a.final("f1", QL, 10, dx=-62)
    a.act("q3", QL, 9, "Phân công kỹ thuật viên", dx=62, w=118)
    a.merge("m2", TV, 10)
    a.act("t1", TV, 11, "Nhận việc được giao")
    a.merge("m3", TV, 12)
    a.act("t2", TV, 13, "Xử lý sự cố")
    a.act("t3", TV, 14, "Cập nhật kết quả xử lý")
    a.act("c1", CD, 14, "Kiểm tra kết quả xử lý")
    a.dec("d3", CD, 15, "Đã khắc phục?")
    a.act("c2", CD, 16, "Báo chưa khắc phục", dx=-62, w=118)
    a.act("c3", CD, 16, "Xác nhận và đánh giá", dx=62, w=118)
    a.act("s6", HT, 16, "Đóng yêu cầu")
    a.final("f", HT, 17)
    for x, y in [("i", "m0"), ("m0", "a1"), ("a1", "s1"), ("s1", "d0"), ("s2", "s3"), ("s3", "s4"), ("s4", "d1"),
                 ("q1", "d2"), ("q2", "f1"), ("m2", "t1"), ("t1", "m3"), ("m3", "t2"), ("t2", "t3"), ("t3", "c1"),
                 ("c1", "d3"), ("c3", "s6"), ("s6", "f")]:
        a.flow(x, y)
    a.flow("d0", "s0", "không", [a.L("d0"), [a.T("s0")[0], a.L("d0")[1]], a.T("s0")])
    a.flow("d0", "s2", "có", [a.R("d0"), [a.T("s2")[0], a.R("d0")[1]], a.T("s2")])
    yl, xl = a.B("s0")[1] + 14, a.x0[CD] + 14
    a.flow("s0", "m0", points=[a.B("s0"), [a.B("s0")[0], yl], [xl, yl], [xl, a.L("m0")[1]], a.L("m0")])
    a.flow("d1", "s5", "có")
    a.flow("d1", "q1", "không", [a.R("d1"), [a.T("q1")[0], a.R("d1")[1]], a.T("q1")])
    a.flow("d2", "q2", "không", [a.L("d2"), [a.T("q2")[0], a.L("d2")[1]], a.T("q2")])
    a.flow("d2", "q3", "có", [a.R("d2"), [a.T("q3")[0], a.R("d2")[1]], a.T("q3")])
    y10 = a.y(10)
    a.flow("s5", "m2", points=[a.B("s5"), [a.B("s5")[0], y10 + 40], [a.cx(TV) - 40, y10 + 40],
                               [a.cx(TV) - 40, y10], a.L("m2")])
    a.flow("q3", "m2", points=[a.R("q3"), [a.T("m2")[0], a.R("q3")[1]], a.T("m2")])
    a.flow("d3", "c2", "không", [a.L("d3"), [a.T("c2")[0], a.L("d3")[1]], a.T("c2")])
    a.flow("d3", "c3", "có", [a.R("d3"), [a.T("c3")[0], a.R("d3")[1]], a.T("c3")])
    yb = a.y(17) + 34
    xk = a.cx(TV) + 112
    a.flow("c2", "m3", points=[a.B("c2"), [a.B("c2")[0], yb], [xk, yb], [xk, a.R("m3")[1]], a.R("m3")])
    return a.spec()


def activity_ai():
    a = Act(["Cư dân", "Hệ thống web", "Dịch vụ phân loại AI", "Dịch vụ email"],
            "Báo sự cố, AI phân loại và điều phối", "Hinh 3.7 Hoat dong bao su co AI dieu phoi",
            "Hoạt động báo sự cố – AI – điều phối")
    CD, WB, AI, EM = 0, 1, 2, 3
    a.start("i", CD, 0)
    a.merge("m0", CD, 1)
    a.act("a1", CD, 2, "Nhập mô tả sự cố")
    a.act("a2", CD, 3, "Gửi yêu cầu")
    a.act("w1", WB, 3, "Kiểm tra dữ liệu nhập")
    a.dec("d1", WB, 4, "Dữ liệu hợp lệ?")
    a.act("w0", WB, 5, "Hiển thị lỗi nhập liệu", dx=-62, w=118)
    a.act("w2", WB, 5, "Kiểm tra AI có bật", dx=62, w=118)
    a.dec("d2", WB, 6, "AI đang bật?", dx=62, left=True)
    a.act("x1", AI, 6, "Phân loại mô tả")
    a.act("x2", WB, 7, "Kiểm tra kết quả phân loại", dx=62, w=118)
    a.dec("d3", WB, 8, "Kết quả hợp lệ?", dx=62)
    a.act("x3", EM, 8, "Báo quản trị AI lỗi")
    a.act("w3", WB, 9, "Gộp lựa chọn cư dân", dx=62, w=118)
    a.act("w9", WB, 8, "Giữ lựa chọn cư dân", dx=-62, w=118)
    a.merge("m1", WB, 10, dx=62)
    a.act("w4", WB, 11, "Tính hạn xử lý", dx=62, w=118)
    a.act("w5", WB, 12, "Lưu yêu cầu", dx=62, w=118)
    a.act("w6", WB, 13, "Kiểm tra mô tả", dx=62, w=118)
    a.dec("d4", WB, 14, "Mô tả rõ ràng?", dx=62)
    a.act("e1", EM, 14, "Báo quản lý hỏi lại")
    a.act("w7", WB, 15, "Kiểm tra thợ phù hợp", dx=62, w=118)
    a.dec("d5", WB, 16, "Có thợ phù hợp?", dx=62)
    a.act("e2", EM, 16, "Báo quản lý phân công")
    a.act("w8", WB, 17, "Giao việc cho thợ", dx=62, w=118)
    a.act("e3", EM, 17, "Gửi thư giao việc")
    a.merge("m2", WB, 18, dx=62)
    a.act("a3", CD, 18, "Xem chi tiết yêu cầu")
    a.final("f", CD, 19)
    for x, y in [("i", "m0"), ("m0", "a1"), ("a1", "a2"), ("a2", "w1"), ("w1", "d1"), ("x2", "d3"),
                 ("m1", "w4"), ("w4", "w5"), ("w5", "w6"), ("w6", "d4"), ("w7", "d5"), ("w8", "e3"),
                 ("a3", "f")]:
        a.flow(x, y)
    a.flow("d1", "w0", "không", [a.L("d1"), [a.T("w0")[0], a.L("d1")[1]], a.T("w0")])
    a.flow("d1", "w2", "có", [a.R("d1"), [a.T("w2")[0], a.R("d1")[1]], a.T("w2")])
    yl, xl = a.B("w0")[1] + 14, a.x0[CD] + 14
    a.flow("w0", "m0", points=[a.B("w0"), [a.B("w0")[0], yl], [xl, yl], [xl, a.L("m0")[1]], a.L("m0")])
    a.flow("w2", "d2")
    a.flow("d2", "x1", "có", [a.R("d2"), a.L("x1")])
    # Nhánh AI tắt cũng phải qua một hành động: hai hình thoi không nối thẳng vào nhau.
    a.flow("d2", "w9", "không", [a.L("d2"), [a.T("w9")[0], a.L("d2")[1]], a.T("w9")])
    a.flow("w9", "m1", points=[a.B("w9"), [a.B("w9")[0], a.L("m1")[1]], a.L("m1")])
    a.flow("x1", "x2", points=[a.B("x1"), [a.B("x1")[0], a.R("x2")[1]], a.R("x2")])
    a.flow("d3", "x3", "không", [a.R("d3"), a.L("x3")])
    a.flow("d3", "w3", "có")
    a.flow("w3", "m1")
    a.flow("x3", "m1", points=[a.B("x3"), [a.B("x3")[0], a.R("m1")[1]], a.R("m1")])
    xe = a.cx(EM) + 112
    a.flow("d4", "e1", "không", [a.R("d4"), a.L("e1")])
    a.flow("d4", "w7", "có")
    a.flow("d5", "e2", "không", [a.R("d5"), a.L("e2")])
    a.flow("d5", "w8", "có")
    for e in ("e1", "e2", "e3"):
        a.flow(e, "m2", points=[a.R(e), [xe, a.R(e)[1]], [xe, a.R("m2")[1]], a.R("m2")])
    a.flow("m2", "a3")
    return a.spec()


# ======================================================================= ERD (thực thể – mối kết hợp)
ATTR = {
    "KHU_VUC": ["KhuVucId*", "TenKhuVuc", "AnhKhuVuc"],
    "DU_AN": ["DuAnId*", "TenDuAn", "DiaChiDuAn", "AnhDaiDienDuAn", "MoTaDuAn"],
    "TOA_NHA": ["ToaNhaId*", "MaToaNha", "TenToaNha", "AnhDaiDienToaNha",
                "MoTaToaNha", "TrangThaiToaNha", "NgayChotKy", "SoNgayAnHan", "PhanTramPhiTre",
                "DonGiaPhiQuanLy"],
    "TANG": ["TangId*", "SoTang"],
    "CAN_HO": ["CanHoId*", "MaCanHo", "DienTich", "SoPhongNgu", "SoPhongTam", "HuongNha", "TinhTrangCanHo", "MoTaCanHo"],
    "NGUOI_DUNG": ["NguoiDungId*", "TenDangNhap", "Email", "MatKhauBam", "HoTen", "SoDienThoai", "SoGiayTo", "AnhDaiDien",
                   "NgayDangNhapCuoi"],
    "VAI_TRO": ["VaiTroId*", "TenVaiTro"],
    "QUYEN": ["QuyenId*", "MaQuyen", "PhanHe", "TenQuyen"],
    "NHAT_KY_HE_THONG": ["NhatKyId*", "TenBang", "KhoaDong", "HanhDong", "NguoiGhiId", "TenNguoiGhi", "ThoiGianGhi",
                         "GiaTriCu", "GiaTriMoi", "CotThayDoi", "DiaChiIp", "TrinhDuyet"],
    "MUC_UU_TIEN": ["MucUuTienId*", "TenMucUuTien", "PhutPhanHoi", "PhutHoanTat"],
    "YEU_CAU_BAO_TRI": ["YeuCauBaoTriId*", "SoYeuCau", "ViTri", "TieuDeYeuCau", "MoTaYeuCau", "LoaiSuCo",
                        "TrangThaiYeuCau", "KetQuaXuLy", "PhanLoaiAi", "ChiPhiUocTinh", "ChiPhiThucTe",
                        "TinhPhiCuDan", "NgayBaoCao", "HanXuLy",
                        "DiemDanhGia", "NhanXetCuDan"],
    "ANH_YEU_CAU": ["AnhYeuCauId*", "DuongDanAnh", "GiaiDoanAnh", "NgayTaiLen"],
    "LICH_SU_YEU_CAU": ["LichSuYeuCauId*", "TrangThaiTruoc", "TrangThaiSau", "ThoiDiemChuyen",
                                 "GhiChuChuyen"],
    "LOAI_DICH_VU": ["LoaiDichVuId*", "MaLoaiDichVu", "TenLoaiDichVu", "DonViTinh", "DonGia"],
    "CHI_SO_DICH_VU": ["ChiSoDichVuId*", "KyChiSo", "ChiSoCu", "ChiSoMoi",
                       "NgayDocChiSo", "GhiChuChiSo"],
    "PHI_DINH_KY": ["PhiDinhKyId*", "TenKhoanPhi", "SoTienKhoanPhi", "ThuTuKhoanPhi", "DangApDung"],
    "HOA_DON": ["HoaDonId*", "SoHoaDon", "LoaiHoaDon", "KyHoaDon", "NgayPhatHanh", "NgayDenHan",
                "TamTinh", "GiamGia", "Thue", "PhiTreHan", "TongTien", "DaThanhToan", "ConLai", "TrangThaiHoaDon",
                "GhiChuHoaDon"],
    "CHI_TIET_HOA_DON": ["ChiTietHoaDonId*", "LoaiKhoan", "NoiDungKhoan", "SoLuongKhoan", "DonGiaKhoan",
                         "ThanhTienKhoan", "ThuTuKhoan"],
    "THANH_TOAN": ["ThanhToanId*", "SoThanhToan", "SoTienThanhToan", "PhuongThuc", "TrangThaiThanhToan", "MaGiaoDich",
                   "NhaCungCap", "NgayThanhToan", "NgayHoanTien", "SoTienHoanTra", "GhiChuThanhToan"],
}
CU_TRU_ATTR = ["VaiTroTrongHo", "LaDaiDien", "NgayVaoO", "NgayChuyenDi", "NgayXacNhan", "GhiChuCuTru"]


def ent_size(name, attrs):
    w = max([F13B.getlength(name)] + [F13.getlength(a.rstrip("*")) for a in attrs]) + 22
    return int(w), int(34 + 16.4 * len(attrs))


class Er:
    """Thực thể đặt theo toạ độ; hình thoi mặc định ở giữa hai thực thể (có thể đặt tay)."""

    DW, DH = 112, 54

    def __init__(self, name, title, model):
        self.name, self.title, self.model = name, title, model
        self.nodes, self.edges, self.box = [], [], {}

    def ent(self, key, x, y, attrs=None, short=False):
        attrs = ATTR[key] if attrs is None else attrs
        if short:   # thực thể đã mô tả đủ ở hình khác: chỉ ghi khoá chính
            attrs = [attrs[0]]
        w, h = ent_size(key, attrs)
        self.box[key] = (x, y, w, h)
        self.nodes.append({"key": key, "kind": "class", "name": key, "x": x, "y": y, "w": w, "h": h, "autosize": True,
                           "hideVisibility": True,
                           "attrs": [{"name": a.rstrip("*"), "static": a.endswith("*"), "vis": "public"} for a in attrs]})

    def c(self, k):
        x, y, w, h = self.box[k]
        return x + w / 2, y + h / 2

    def rel(self, key, name, a, ca, b, cb, at=None, attrs=None):
        if at is None:
            (ax, ay), (bx, by) = self.c(a), self.c(b)
            at = ((ax + bx) / 2, (ay + by) / 2)
        dw = max(self.DW, int(F13.getlength(name)) + 48)
        x, y = int(at[0] - dw / 2), int(at[1] - self.DH / 2)
        self.nodes.append({"key": key, "kind": "nary", "name": name, "x": x, "y": y, "w": dw, "h": self.DH})
        self.box[key] = (x, y, dw, self.DH)
        for ent, card in ((a, ca), (b, cb)):
            self.edges.append({"kind": "assoc", "from": ent, "to": key, "mult1": card, "lineStyle": 1})
        if attrs:   # thuộc tính của mối kết hợp: ghi trong khung chú thích nối vào hình thoi
            text = "\n".join(attrs)
            w = int(max(F13.getlength(t) for t in attrs)) + 24
            h = int(18 * len(attrs)) + 14
            nx, ny = x + dw // 2 - w // 2, y + self.DH + 40
            self.nodes.append({"key": key + "_a", "kind": "note", "name": text, "x": nx, "y": ny, "w": w, "h": h})
            self.edges.append({"kind": "notelink", "from": key + "_a", "to": key})

    def spec(self):
        return {"kind": "class", "model": self.model, "name": self.name, "title": self.title, "fontSize": 13,
                "nodes": self.nodes, "edges": self.edges}


def erd_co_cau():
    e = Er("Hinh 2.1 ERD co cau chung cu va cu tru", "ERD cơ cấu chung cư và cư trú", "ERD – cơ cấu chung cư")
    e.ent("KHU_VUC", 20, 40)
    e.ent("DU_AN", 330, 20)
    e.ent("TOA_NHA", 680, 20)
    e.ent("TANG", 1080, 40)
    e.ent("CAN_HO", 1060, 330)
    e.ent("NGUOI_DUNG", 330, 360)
    e.rel("r1", "có dự án", "KHU_VUC", "(0,n)", "DU_AN", "(1,1)")
    e.rel("r2", "gồm toà", "DU_AN", "(0,n)", "TOA_NHA", "(1,1)")
    e.rel("r3", "có tầng", "TOA_NHA", "(1,n)", "TANG", "(1,1)")
    e.rel("r4", "có căn", "TANG", "(0,n)", "CAN_HO", "(1,1)")
    e.rel("r5", "quản lý toà", "NGUOI_DUNG", "(0,n)", "TOA_NHA", "(0,1)", at=(560, 300))
    e.rel("r6", "cư trú", "NGUOI_DUNG", "(0,n)", "CAN_HO", "(0,n)", at=(800, 440), attrs=CU_TRU_ATTR)
    return e.spec()


def erd_tai_khoan():
    e = Er("Hinh 2.2 ERD tai khoan va phan quyen", "ERD tài khoản và phân quyền", "ERD – tài khoản, phân quyền")
    e.ent("NGUOI_DUNG", 20, 20)
    e.ent("VAI_TRO", 400, 50)
    e.ent("QUYEN", 700, 20)
    e.ent("NHAT_KY_HE_THONG", 960, 20)
    e.rel("r1", "đóng vai", "NGUOI_DUNG", "(1,n)", "VAI_TRO", "(0,n)")
    e.rel("r2", "được cấp", "VAI_TRO", "(0,n)", "QUYEN", "(0,n)")
    return e.spec()


def erd_su_co():
    e = Er("Hinh 2.3 ERD su co va bao tri", "ERD sự cố và bảo trì", "ERD – sự cố")
    e.ent("CAN_HO", 20, 30, short=True)
    e.ent("TOA_NHA", 20, 250, short=True)
    e.ent("MUC_UU_TIEN", 20, 420)
    e.ent("YEU_CAU_BAO_TRI", 420, 20)
    e.ent("NGUOI_DUNG", 900, 250, short=True)
    e.ent("ANH_YEU_CAU", 1100, 20)
    e.ent("LICH_SU_YEU_CAU", 430, 500)
    e.rel("r1", "xảy ra tại căn", "CAN_HO", "(0,n)", "YEU_CAU_BAO_TRI", "(0,1)", at=(280, 60))
    e.rel("r2", "ở khu vực chung", "TOA_NHA", "(0,n)", "YEU_CAU_BAO_TRI", "(0,1)", at=(280, 270))
    e.rel("r3", "có mức ưu tiên", "MUC_UU_TIEN", "(0,n)", "YEU_CAU_BAO_TRI", "(1,1)", at=(280, 470))
    e.rel("r4", "báo sự cố", "NGUOI_DUNG", "(0,n)", "YEU_CAU_BAO_TRI", "(1,1)", at=(730, 185))
    e.rel("r5", "phụ trách", "NGUOI_DUNG", "(0,n)", "YEU_CAU_BAO_TRI", "(0,1)", at=(735, 300))
    e.rel("r6", "có ảnh", "YEU_CAU_BAO_TRI", "(0,n)", "ANH_YEU_CAU", "(1,1)", at=(880, 50))
    e.rel("r7", "tải ảnh", "NGUOI_DUNG", "(0,n)", "ANH_YEU_CAU", "(0,1)", at=(1120, 190))
    e.rel("r8", "có lịch sử", "YEU_CAU_BAO_TRI", "(1,n)", "LICH_SU_YEU_CAU", "(1,1)", at=(540, 432))
    e.rel("r9", "thực hiện", "NGUOI_DUNG", "(0,n)", "LICH_SU_YEU_CAU", "(0,1)", at=(820, 470))
    return e.spec()


def erd_thu_phi():
    e = Er("Hinh 2.4 ERD thu phi", "ERD phân hệ thu phí", "ERD – thu phí")
    e.ent("LOAI_DICH_VU", 20, 20)
    e.ent("CHI_SO_DICH_VU", 20, 300)
    e.ent("CAN_HO", 420, 30, short=True)
    e.ent("PHI_DINH_KY", 420, 300)
    e.ent("HOA_DON", 760, 20)
    e.ent("CHI_TIET_HOA_DON", 760, 450)
    e.ent("YEU_CAU_BAO_TRI", 1170, 470, short=True)
    e.ent("THANH_TOAN", 1130, 20)
    e.rel("r1", "thuộc loại", "LOAI_DICH_VU", "(0,n)", "CHI_SO_DICH_VU", "(1,1)", at=(100, 235))
    e.rel("r2", "ghi chỉ số", "CAN_HO", "(0,n)", "CHI_SO_DICH_VU", "(1,1)", at=(330, 170))
    e.rel("r3", "áp phí", "CAN_HO", "(0,n)", "PHI_DINH_KY", "(1,1)", at=(490, 200))
    e.rel("r4", "được lập", "CAN_HO", "(0,n)", "HOA_DON", "(1,1)", at=(640, 60))
    e.rel("r5", "gồm khoản", "HOA_DON", "(1,n)", "CHI_TIET_HOA_DON", "(1,1)", at=(850, 392))
    e.rel("r6", "lập từ chỉ số", "CHI_SO_DICH_VU", "(0,n)", "CHI_TIET_HOA_DON", "(0,1)", at=(400, 540))
    e.rel("r7", "lập từ phí", "PHI_DINH_KY", "(0,n)", "CHI_TIET_HOA_DON", "(0,1)", at=(640, 470))
    e.rel("r8", "tính phí sửa chữa", "YEU_CAU_BAO_TRI", "(0,n)", "CHI_TIET_HOA_DON", "(0,1)", at=(1080, 520))
    e.rel("r9", "được thanh toán", "HOA_DON", "(0,n)", "THANH_TOAN", "(1,1)", at=(1030, 90))
    return e.spec()


# ======================================================================= lược đồ quan hệ (từ CSDL thật)
SCHEMA = json.loads((HERE / "luoc_do.json").read_text(encoding="utf8"))
MANY_MIN1 = {("ToaNha", "Tang"), ("HoaDon", "ChiTietHoaDon"), ("YeuCauBaoTri", "LichSuYeuCau")}


def rd_cols(table, short=False):
    cols = SCHEMA[table]["columns"]
    order = [c for c in cols if c["pk"]] + [c for c in cols if c["fk"] and not c["pk"]] + \
            [c for c in cols if not c["pk"] and not c["fk"]]
    if short:
        order = [c for c in order if c["pk"]]
    return [{"name": c["name"], "type": c["type"], "pk": c["pk"], "fk": bool(c["fk"]),
             "nullable": c["null"], "unique": c["unique"]} for c in order]


class Rd:
    def __init__(self, name, title, model, skip=()):
        self.name, self.title, self.model = name, title, model
        self.nodes, self.keys, self.skip = [], set(), set(skip)

    def t(self, table, x, y, short=False):
        cols = rd_cols(table, short)
        w, h = uml_spec.entity_size(table, cols)
        self.nodes.append({"key": table, "kind": "entity", "name": table, "x": x, "y": y, "w": w, "h": h,
                           "columns": cols})
        self.keys.add(table)

    def spec(self):
        edges = []
        for child in sorted(self.keys):
            for c in SCHEMA[child]["columns"]:
                parent = c["fk"]
                if parent and parent in self.keys and (parent, child) not in self.skip:
                    many = "1..*" if (parent, child) in MANY_MIN1 else "0..*"
                    edges.append({"kind": "relation", "from": parent, "to": child, "lineStyle": 1,
                                  "card1": "0..1" if c["null"] else "1", "card2": many})
        return {"kind": "erd", "model": self.model, "name": self.name, "title": self.title, "fontSize": 14,
                "nodes": self.nodes, "edges": edges}


def rd_co_cau():
    r = Rd("Hinh 2.5 Luoc do quan he co cau chung cu", "Lược đồ quan hệ cơ cấu chung cư và cư trú", "RD – cơ cấu")
    r.t("KhuVuc", 20, 20)
    r.t("DuAn", 330, 20)
    r.t("ToaNha", 700, 20)
    r.t("Tang", 1120, 40)
    r.t("CanHo", 1080, 300)
    r.t("CuTru", 520, 360)
    r.t("NguoiDung", 20, 400, short=True)
    return r.spec()


def rd_tai_khoan():
    r = Rd("Hinh 2.6 Luoc do quan he tai khoan", "Lược đồ quan hệ tài khoản và phân quyền", "RD – tài khoản")
    r.t("NguoiDung", 20, 20)
    r.t("NguoiDung_VaiTro", 400, 40)
    r.t("VaiTro", 760, 40)
    r.t("VaiTro_Quyen", 760, 230)
    r.t("Quyen", 400, 260)
    r.t("NhatKyHeThong", 1080, 20)
    return r.spec()


def rd_su_co():
    r = Rd("Hinh 2.7 Luoc do quan he su co", "Lược đồ quan hệ sự cố và bảo trì", "RD – sự cố",
           skip=[("NguoiDung", "ToaNha")])
    r.t("CanHo", 20, 20, short=True)
    r.t("ToaNha", 20, 200, short=True)
    r.t("MucUuTien", 20, 380)
    r.t("YeuCauBaoTri", 380, 20)
    r.t("NguoiDung", 1150, 290, short=True)
    r.t("AnhYeuCau", 1100, 20)
    r.t("LichSuYeuCau", 1060, 450)
    return r.spec()


def rd_thu_phi():
    r = Rd("Hinh 2.8 Luoc do quan he thu phi", "Lược đồ quan hệ phân hệ thu phí", "RD – thu phí",
           skip=[("CanHo", "YeuCauBaoTri")])
    r.t("LoaiDichVu", 20, 20)
    r.t("ChiSoDichVu", 20, 300)
    r.t("CanHo", 420, 20, short=True)
    r.t("PhiDinhKy", 420, 250)
    r.t("HoaDon", 800, 20)
    r.t("ChiTietHoaDon", 800, 480)
    r.t("ThanhToan", 1200, 20)
    r.t("YeuCauBaoTri", 1220, 520, short=True)
    return r.spec()


# ======================================================================= use case
def state_machine():
    d = uml_spec.states()
    d["name"] = "Hinh 1.5 So do trang thai yeu cau su co"
    return d


def deployment():
    d = uml_spec_ch3.deployment()
    d["name"] = "Hinh 3.1 Kien truc trien khai"
    return d


UW, UH = 200, 52


def _uc(key, name, cx, cy):
    return {"key": key, "kind": "usecase", "name": name, "x": cx - UW // 2, "y": cy - UH // 2, "w": UW, "h": UH}


def _actor(key, name, cx, top):
    return {"key": key, "kind": "actor", "name": name, "x": cx - 45, "y": top, "w": 90, "h": 80}


def use_case_overview():
    c1, c2, c3, c4 = 340, 560, 900, 1130            # tâm bốn cột use case
    cm = (c2 + c3) // 2
    nodes = [{"key": "sub", "kind": "subject", "name": "Hệ thống quản lý vận hành chung cư", "x": 200, "y": 70,
              "w": 1060, "h": 760}]
    cd = [("uc02", "UC02 Kích hoạt tài khoản"), ("uc03", "UC03 Báo sự cố"), ("uc04", "UC04 Theo dõi sự cố"),
          ("uc05", "UC05 Huỷ yêu cầu sự cố"), ("uc06", "UC06 Xác nhận kết quả xử lý"),
          ("uc06b", "UC06b Báo sự cố chưa khắc phục"), ("uc19", "UC19 Thanh toán hoá đơn")]
    ql = [("uc09", "UC09 Quản lý cơ cấu khu – toà – tầng – căn"), ("uc10", "UC10 Quản lý cư dân"),
          ("uc11", "UC11 Phân công xử lý sự cố"), ("uc12", "UC12 Xem báo cáo sự cố"),
          ("uc17", "UC17 Ghi chỉ số điện nước"), ("uc18", "UC18 Lập hoá đơn"), ("uc20", "UC20 Xử lý thanh toán"),
          ("uc21", "UC21 Xem báo cáo tài chính")]
    for i, (k, n) in enumerate(cd):
        nodes.append(_uc(k, n, c1, 135 + i * 78))
    for i, (k, n) in enumerate(ql):
        nodes.append(_uc(k, n, c4, 115 + i * 72))
    nodes += [_uc("uc07", "UC07 Phân loại sự cố", c2, 115), _uc("uc08", "UC08 Điều phối sự cố", c3, 115),
              _uc("uc13", "UC13 Quản lý người dùng và phân quyền", c4, 700), _uc("uc14", "UC14 Xem nhật ký hệ thống", c4, 772),
              _uc("uc15", "UC15 Xem việc được giao", c2, 620), _uc("uc16", "UC16 Cập nhật tiến độ xử lý", c3, 620),
              _uc("uc01", "UC01 Đăng nhập", c1, 710)]
    nodes += [_actor("cd", "Cư dân", 60, 360), _actor("ql", "Quản lý BQL", 1380, 300),
              _actor("ai", "Mô hình AI", c2, -50), _actor("mail", "Dịch vụ email", c3, -50),
              _actor("qt", "Quản trị viên", 1380, 700), _actor("ktv", "Kỹ thuật viên", cm, 880),
              _actor("nd", "Người dùng", c1, 880)]
    a = lambda x, y: {"kind": "assoc", "from": x, "to": y, "lineStyle": 1}
    edges = [a("cd", k) for k, _ in cd] + [a("ql", k) for k, _ in ql]
    edges += [a("ai", "uc07"), a("mail", "uc08"), a("qt", "uc13"), a("qt", "uc14"), a("ktv", "uc15"),
              a("ktv", "uc16"), a("nd", "uc01")]
    edges += [{"kind": "include", "from": "uc03", "to": "uc07", "lineStyle": 1},
              {"kind": "include", "from": "uc03", "to": "uc08", "lineStyle": 1}]
    # Cư dân, quản lý, kỹ thuật viên đều là người dùng (tổng quát hoá) nên cùng dùng UC01;
    # quản trị viên có mọi quyền của quản lý.
    edges += [{"kind": "generalization", "from": "cd", "to": "nd", "lineStyle": 0,
               "points": [[60, 440], [60, 920], [c1 - 45, 920]]},
              {"kind": "generalization", "from": "ql", "to": "nd", "lineStyle": 0,
               "points": [[1405, 340], [1480, 340], [1480, 1000], [c1 + 10, 1000], [c1 + 10, 960]]},
              {"kind": "generalization", "from": "ktv", "to": "nd", "lineStyle": 0,
               "points": [[cm - 45, 920], [c1 + 45, 920]]},
              {"kind": "generalization", "from": "qt", "to": "ql", "lineStyle": 0,
               "points": [[1380, 700], [1380, 380]]}]
    dy, dx = 70, 170   # dời cả khung sang phải: tác nhân Cư dân ở xa bên trái để các đường nối không cắt use case khác
    for n in nodes:
        n["y"] += dy
        if n["key"] != "cd":
            n["x"] += dx
    for e in edges:
        for i, p in enumerate(e.get("points", [])):
            p[1] += dy
            if not (e["from"] == "cd" and i < 2):
                p[0] += dx
    return {"kind": "usecase", "model": "Use case tổng quát", "name": "Hinh 1.6 So do use case tong quat",
            "fontSize": 13, "nodes": nodes, "edges": edges}


def uc_detail(name, title, ucs, rels, actors, links, w, h):
    nodes = [{"key": "sub", "kind": "subject", "name": title, "x": 160, "y": 10, "w": w, "h": h}]
    nodes += [_uc(k, n, x, y) for k, (n, x, y) in ucs.items()]
    nodes += [_actor(k, n, x, y) for k, (n, x, y) in actors.items()]
    edges = [{"kind": kind, "from": a, "to": b, "lineStyle": 1} for kind, a, b in rels]
    edges += [{"kind": "assoc", "from": a, "to": u, "lineStyle": 1} for a, u in links]
    return {"kind": "usecase", "model": title, "name": name, "title": title, "fontSize": 13,
            "nodes": nodes, "edges": edges}


def uc03():
    ucs = {"root": ("UC03 Báo sự cố", 420, 160),
           "u7": ("UC07 Phân loại sự cố", 720, 80), "u8": ("UC08 Điều phối sự cố", 720, 240),
           "anh": ("Đính kèm ảnh hiện trạng", 420, 320)}
    rels = [("include", "root", "u7"), ("include", "root", "u8"), ("extend", "anh", "root")]
    actors = {"cd": ("Cư dân", 70, 120), "ai": ("Mô hình AI", 1000, 40), "mail": ("Dịch vụ email", 1000, 200)}
    return uc_detail("Hinh 3.2 Use case UC03 Bao su co", "UC03 – Báo sự cố", ucs, rels, actors,
                     [("cd", "root"), ("ai", "u7"), ("mail", "u8")], 720, 380)


def uc11():
    ucs = {"root": ("UC11 Phân công xử lý sự cố", 470, 70),
           "xem": ("Xem danh sách sự cố", 300, 200), "giao": ("Giao kỹ thuật viên", 640, 200),
           "pl": ("Phân loại lại sự cố", 300, 330), "tc": ("Từ chối yêu cầu sự cố", 640, 330)}
    rels = [("include", "root", "xem"), ("include", "root", "giao"), ("extend", "pl", "root"),
            ("extend", "tc", "root")]
    actors = {"ql": ("Quản lý BQL", 70, 30), "mail": ("Dịch vụ email", 980, 160)}
    return uc_detail("Hinh 3.3 Use case UC11 Phan cong xu ly su co", "UC11 – Phân công xử lý sự cố", ucs, rels, actors,
                     [("ql", "root"), ("mail", "giao")], 700, 390)


def uc16():
    ucs = {"root": ("UC16 Cập nhật tiến độ xử lý", 470, 70),
           "u15": ("UC15 Xem việc được giao", 300, 200), "kq": ("Ghi kết quả xử lý", 640, 200),
           "td": ("Tạm dừng xử lý", 300, 330), "anh": ("Tải ảnh nghiệm thu", 640, 330)}
    rels = [("include", "root", "u15"), ("include", "root", "kq"), ("extend", "td", "root"), ("extend", "anh", "kq")]
    return uc_detail("Hinh 3.4 Use case UC16 Cap nhat tien do xu ly", "UC16 – Cập nhật tiến độ xử lý", ucs, rels,
                     {"ktv": ("Kỹ thuật viên", 70, 30)}, [("ktv", "root")], 700, 390)


def uc06():
    ucs = {"u6": ("UC06 Xác nhận kết quả xử lý", 400, 80), "u6b": ("UC06b Báo sự cố chưa khắc phục", 400, 300),
           "xem": ("Xem kết quả xử lý", 720, 190), "dg": ("Đánh giá mức hài lòng", 720, 60)}
    rels = [("include", "u6", "xem"), ("include", "u6b", "xem"), ("extend", "dg", "u6")]
    return uc_detail("Hinh 3.5 Use case UC06 Xac nhan ket qua xu ly", "UC06, UC06b – Xác nhận kết quả xử lý", ucs,
                     rels, {"cd": ("Cư dân", 70, 140)}, [("cd", "u6"), ("cd", "u6b")], 720, 370)


def uc20():
    ucs = {"root": ("UC20 Xử lý thanh toán", 520, 70),
           "xem": ("Xem thanh toán chờ duyệt", 300, 200), "duyet": ("Duyệt thanh toán", 540, 200),
           "tc": ("Từ chối thanh toán", 780, 200), "gn": ("Thu tiền trực tiếp", 380, 330),
           "ht": ("Hoàn tiền", 700, 330)}
    rels = [("include", "root", "xem"), ("extend", "duyet", "root"), ("extend", "tc", "root"),
            ("extend", "gn", "root"), ("extend", "ht", "root")]
    return uc_detail("Hinh 3.6 Use case UC20 Xu ly thanh toan", "UC20 – Xử lý thanh toán", ucs, rels,
                     {"ql": ("Quản lý BQL", 70, 30)}, [("ql", "root")], 780, 390)


# ======================================================================= sơ đồ lớp
def _p(name, typ):
    return {"name": name, "type": typ, "vis": "private"}


def _o(name, params=(), ret=None):
    return {"name": name, "params": [[p, ""] for p in params], "ret": ret, "vis": "public"}


CLS = {
    "KhuVuc": ([_p("tenKhuVuc", "string"), _p("anhKhuVuc", "string")], [_o("dsDuAn")]),
    "DuAn": ([_p("tenDuAn", "string"), _p("diaChiDuAn", "string"), _p("moTaDuAn", "string")],
             [_o("taoMoi"), _o("capNhat")]),
    "ToaNha": ([_p("maToaNha", "string"), _p("tenToaNha", "string"),
                _p("trangThaiToaNha", "int"), _p("donGiaPhiQuanLy", "decimal"), _p("ngayChotKy", "int")],
               [_o("taoMoi"), _o("capNhat")]),
    "Tang": ([_p("soTang", "int")], [_o("dsCanCuaTang")]),
    "CanHo": ([_p("maCanHo", "string"), _p("dienTich", "double"), _p("soPhongNgu", "int"), _p("tinhTrangCanHo", "int")],
              [_o("taoMoi"), _o("doiTinhTrang"), _o("dangCoNguoiO", ret="bool"), _o("layNguoiDaiDien", ret="CuTru")]),
    "CuTru": ([_p("vaiTroTrongHo", "int"), _p("laDaiDien", "bool"), _p("ngayVaoO", "datetime"),
               _p("ngayChuyenDi", "datetime"), _p("ngayXacNhan", "datetime")],
              [_o("themNguoiCuTru"), _o("xacNhanQuaThuMoi"), _o("chuyenDi"), _o("dangO", ret="bool")]),
    "NguoiDung": ([_p("tenDangNhap", "string"), _p("email", "string"), _p("hoTen", "string"),
                   _p("soDienThoai", "string"), _p("soGiayTo", "string")],
                  [_o("dangNhap"), _o("kichHoat", ["matKhau"]), _o("khoaTaiKhoan"), _o("xemViecDuocGiao")]),
    "VaiTro": ([_p("tenVaiTro", "string")], [_o("luuPhanQuyen")]),
    "Quyen": ([_p("maQuyen", "string"), _p("phanHe", "string"), _p("tenQuyen", "string")], [_o("dsVaiTro")]),
    "YeuCauBaoTri": ([_p("soYeuCau", "string"), _p("tieuDeYeuCau", "string"), _p("moTaYeuCau", "string"),
                      _p("viTri", "string"), _p("loaiSuCo", "LoaiSuCo"), _p("trangThaiYeuCau", "TrangThaiYeuCau"),
                      _p("ngayBaoCao", "datetime"), _p("hanXuLy", "datetime"), _p("diemDanhGia", "int")],
                     [_o("guiYeuCau"), _o("phanLoaiBangAI"), _o("tinhHanXuLy"), _o("giaoViec", ["kyThuatVien"]),
                      _o("chuyenTrangThai", ["trangThaiMoi", "ghiChu"]), _o("hoanTat", ["ketQuaXuLy"]),
                      _o("xacNhanVaDanhGia", ["diem"]), _o("quaHan", ret="bool")]),
    "MucUuTien": ([_p("tenMucUuTien", "string"), _p("phutPhanHoi", "int"), _p("phutHoanTat", "int")],
                  [_o("tinhHan", ["ngayBaoCao"], "datetime")]),
    "AnhYeuCau": ([_p("duongDanAnh", "string"), _p("giaiDoanAnh", "int"), _p("ngayTaiLen", "datetime")], [_o("luuAnh")]),
    "LichSuYeuCau": ([_p("trangThaiTruoc", "TrangThaiYeuCau"), _p("trangThaiSau", "TrangThaiYeuCau"),
                             _p("thoiDiemChuyen", "datetime"), _p("ghiChuChuyen", "string")], [_o("ghiLichSu")]),
    "NhatKyHeThong": ([_p("tenBang", "string"), _p("hanhDong", "int"), _p("tenNguoiGhi", "string"),
                       _p("thoiGianGhi", "datetime")], [_o("ghiNhatKy"), _o("xemNhatKy")]),
    "LoaiDichVu": ([_p("maLoaiDichVu", "string"), _p("tenLoaiDichVu", "string"), _p("donViTinh", "string"),
                    _p("donGia", "decimal")], [_o("capNhatDonGia")]),
    "ChiSoDichVu": ([_p("kyChiSo", "int"), _p("chiSoCu", "decimal"), _p("chiSoMoi", "decimal")],
                    [_o("ghiChiSo"), _o("nhapTheoToa"), _o("tieuThu", ret="decimal"), _o("daXuatHoaDon", ret="bool")]),
    "PhiDinhKy": ([_p("tenKhoanPhi", "string"), _p("soTienKhoanPhi", "decimal"), _p("dangApDung", "bool")],
                  [_o("themKhoan"), _o("batTat")]),
    "HoaDon": ([_p("soHoaDon", "string"), _p("kyHoaDon", "int"), _p("ngayPhatHanh", "datetime"),
                _p("ngayDenHan", "datetime"), _p("tongTien", "decimal"), _p("daThanhToan", "decimal"),
                _p("conLai", "decimal"), _p("trangThaiHoaDon", "TrangThaiHoaDon")],
               [_o("lapHoaDonThang", ["canHo", "ky"]), _o("lapHoaDonPhatSinh"), _o("congPhiTre"), _o("huy", ["lyDo"])]),
    "ChiTietHoaDon": ([_p("loaiKhoan", "LoaiKhoan"), _p("noiDungKhoan", "string"), _p("soLuongKhoan", "decimal"),
                       _p("donGiaKhoan", "decimal"), _p("thanhTienKhoan", "decimal")], [_o("tinhThanhTien")]),
    "ThanhToan": ([_p("soThanhToan", "string"), _p("soTienThanhToan", "decimal"), _p("phuongThuc", "int"),
                   _p("trangThaiThanhToan", "TrangThaiThanhToan"), _p("soTienHoanTra", "decimal")],
                  [_o("guiXacNhan"), _o("ghiNhanTrucTiep"), _o("duyet"), _o("tuChoi", ["lyDo"]), _o("hoanTien", ["soTien"])]),
}
ENUMS = {
    "TrangThaiYeuCau": ["MoiGui", "DaTiepNhan", "DangXuLy", "DaXuLy", "DaDong", "TuChoi", "TamDung", "DaHuy"],
    "LoaiSuCo": ["Nuoc", "Dien", "ThietBi", "KetCau", "VeSinh", "ConTrung", "Mang", "Khac"],
    "TrangThaiHoaDon": ["BanNhap", "DaPhatHanh", "TraMotPhan", "DaThanhToan", "QuaHan", "DaHuy", "DaHoanTien"],
    "TrangThaiThanhToan": ["ChoXacNhan", "ThanhCong", "ThatBai", "DaHoan", "HoanMotPhan", "DaHuy"],
    "LoaiKhoan": ["PhiQuanLy", "DienNuoc", "PhiDinhKy", "SuaChua", "Khac"],
}


class Cls:
    def __init__(self, name, title):
        self.name, self.title, self.nodes, self.edges = name, title, [], []

    def c(self, key, x, y, short=False):
        if key in ENUMS:
            n = {"key": key, "kind": "enum", "name": key, "x": x, "y": y, "literals": ENUMS[key]}
        else:
            attrs, ops = CLS[key]
            n = {"key": key, "kind": "class", "name": key, "x": x, "y": y,
                 "attrs": [] if short else attrs, "ops": [] if short else ops}
        w, h = uml_spec_ch3.estimate(n)
        n.update(w=w, h=h, autosize=True)
        self.nodes.append(n)

    def box(self, key):
        n = next(n for n in self.nodes if n["key"] == key)
        return n["x"], n["y"], n["w"], n["h"]

    def a(self, x, y, m1, m2, name=None, comp=False, pts=None):
        e = {"kind": "assoc", "from": x, "to": y, "mult1": m1, "mult2": m2, "lineStyle": 1}
        if pts:
            e["points"] = pts
        if name:
            e["name"] = name
        if comp:
            e["agg1"] = "composite"
        self.edges.append(e)

    def spec(self):
        return {"kind": "class", "model": self.title, "name": self.name, "title": self.title, "fontSize": 12,
                "nodes": self.nodes, "edges": self.edges}


def class_co_cau():
    k = Cls("Hinh 3.8 So do lop co cau nguoi dung phan quyen", "Sơ đồ lớp nơi chốn, cư trú, người dùng và phân quyền")
    k.c("KhuVuc", 20, 20)
    k.c("DuAn", 280, 20)
    k.c("ToaNha", 540, 20)
    k.c("Tang", 900, 40)
    k.c("CanHo", 860, 300)
    k.c("CuTru", 520, 330)
    k.c("NguoiDung", 120, 330)
    k.c("VaiTro", 20, 600)
    k.c("Quyen", 360, 600)
    k.a("KhuVuc", "DuAn", "1", "0..*")
    k.a("DuAn", "ToaNha", "1", "0..*")
    k.a("ToaNha", "Tang", "1", "1..*")
    k.a("Tang", "CanHo", "1", "0..*")
    k.a("CanHo", "CuTru", "1", "0..*")
    k.a("NguoiDung", "CuTru", "1", "0..*")
    k.a("NguoiDung", "ToaNha", "0..1", "0..*", name="quanLy")
    k.a("NguoiDung", "VaiTro", "0..*", "1..*")
    k.a("VaiTro", "Quyen", "0..*", "0..*")
    return k.spec()


def class_su_co():
    k = Cls("Hinh 3.9 So do lop su co", "Sơ đồ lớp sự cố và bảo trì")
    k.c("CanHo", 20, 30, short=True)
    k.c("ToaNha", 20, 200, short=True)
    k.c("MucUuTien", 20, 380)
    k.c("YeuCauBaoTri", 360, 20)
    k.c("NguoiDung", 470, 380, short=True)
    k.c("AnhYeuCau", 1060, 20)
    k.c("LichSuYeuCau", 1020, 380)
    k.c("NhatKyHeThong", 20, 560)
    k.c("TrangThaiYeuCau", 640, 470)
    k.c("LoaiSuCo", 800, 470)
    k.a("CanHo", "YeuCauBaoTri", "0..1", "0..*")
    k.a("ToaNha", "YeuCauBaoTri", "0..1", "0..*", name="toaNha")
    k.a("MucUuTien", "YeuCauBaoTri", "1", "0..*")
    # Hai liên kết giữa NguoiDung (đặt dưới) và YeuCauBaoTri: một đường dọc, một đường vòng sang cạnh phải.
    ytop = 290        # đáy thật của hộp YeuCauBaoTri (StarUML tự co hộp theo nội dung)
    k.a("NguoiDung", "YeuCauBaoTri", "1", "0..*", name="nguoiBaoCao", pts=[[500, 380], [500, ytop]])
    k.a("NguoiDung", "YeuCauBaoTri", "0..1", "0..*", name="nguoiPhuTrach",
        pts=[[538, 395], [650, 395], [650, 268], [566, 268]])
    k.edges[-1]["lineStyle"] = k.edges[-2]["lineStyle"] = 0
    k.a("YeuCauBaoTri", "AnhYeuCau", "1", "0..*", comp=True)
    k.a("YeuCauBaoTri", "LichSuYeuCau", "1", "1..*", comp=True)
    k.a("NguoiDung", "AnhYeuCau", "0..1", "0..*", name="nguoiTaiLen")
    k.a("NguoiDung", "LichSuYeuCau", "0..1", "0..*", name="nguoiThucHien")
    return k.spec()


def class_thu_phi():
    k = Cls("Hinh 3.10 So do lop thu phi", "Sơ đồ lớp phân hệ thu phí")
    k.c("LoaiDichVu", 20, 20)
    k.c("ChiSoDichVu", 20, 260)
    k.c("CanHo", 400, 40, short=True)
    k.c("PhiDinhKy", 380, 250)
    k.c("HoaDon", 720, 20)
    k.c("ThanhToan", 1100, 20)
    k.c("ChiTietHoaDon", 720, 420)
    k.c("YeuCauBaoTri", 1120, 470, short=True)
    k.c("TrangThaiHoaDon", 1100, 260)
    k.c("TrangThaiThanhToan", 1280, 260)
    k.c("LoaiKhoan", 380, 470)
    k.a("LoaiDichVu", "ChiSoDichVu", "1", "0..*")
    k.a("CanHo", "ChiSoDichVu", "1", "0..*")
    k.a("CanHo", "PhiDinhKy", "1", "0..*")
    k.a("CanHo", "HoaDon", "1", "0..*")
    k.a("HoaDon", "ChiTietHoaDon", "1", "1..*", comp=True)
    k.a("HoaDon", "ThanhToan", "1", "0..*")
    k.a("ChiSoDichVu", "ChiTietHoaDon", "0..1", "0..*")
    k.a("PhiDinhKy", "ChiTietHoaDon", "0..1", "0..*")
    k.a("YeuCauBaoTri", "ChiTietHoaDon", "0..1", "0..*")
    return k.spec()


# ======================================================================= sơ đồ tuần tự (có thanh kích hoạt)
class Seq(uml_spec_ch3.Seq):
    """Mỗi lời gọi mở một thanh kích hoạt ở đối tượng nhận, kéo dài tới lần trả về tương ứng; mọi lời gọi tiếp theo
    của đối tượng đó nằm trong thanh này. Trả về sớm trong nhánh opt/alt (close=False) không đóng thanh."""

    def __init__(self, lifelines):
        super().__init__(lifelines)
        self.open = []          # (chỉ số cạnh, bên gọi, bên nhận)

    def m(self, a, c, name, sort="synchCall", close=True):
        super().m(a, c, name, sort)
        e = self.edges[-1]
        if sort == "reply":
            if not close:
                return
            for i in range(len(self.open) - 1, -1, -1):
                idx, caller, callee = self.open[i]
                if callee == a and caller == c:
                    self.edges[idx]["activationHeight"] = e["y"] - self.edges[idx]["y"]
                    del self.open[i]
                    break
        elif a != c:
            self.open.append((len(self.edges) - 1, a, c))


def seq_bao_su_co():
    s = Seq([("cd", "Cư dân"), ("tn", ":Tiếp nhận sự cố"), ("pl", ":Phân loại sự cố"), ("ai", ":Mô hình AI"),
             ("qt", ":Quy trình sự cố"), ("db", ":CSDL"), ("dp", ":Điều phối sự cố")])
    s.m("cd", "tn", "gửi yêu cầu(mô tả, ảnh)")
    s.m("tn", "tn", "kiểm tra căn đang ở, dữ liệu nhập")
    s.begin("alt", "không hợp lệ", "cd", "dp")
    s.m("tn", "cd", "báo lỗi nhập liệu", "reply", close=False)
    s.other("hợp lệ")
    s.m("tn", "pl", "phân loại(mô tả)")
    s.begin("opt", "AI đang bật", "pl", "ai")
    s.m("pl", "ai", "phân loại(mô tả)")
    s.m("ai", "pl", "loại, mức ưu tiên, câu hỏi lại", "reply")
    s.end()
    s.m("pl", "pl", "gộp với lựa chọn của cư dân")
    s.m("pl", "tn", "loại, mức ưu tiên", "reply")
    s.m("tn", "qt", "tạo yêu cầu(loại, mức ưu tiên)")
    s.m("qt", "db", "đọc thời hạn của mức ưu tiên")
    s.m("db", "qt", "thời hạn SLA", "reply")
    s.m("qt", "qt", "tính hạn xử lý")
    s.m("qt", "db", "lưu yêu cầu, dòng lịch sử đầu")
    s.m("db", "qt", "đã lưu", "reply")
    s.m("qt", "tn", "yêu cầu đã tạo", "reply")
    s.m("tn", "dp", "điều phối(yêu cầu)")
    s.m("dp", "tn", "đã điều phối", "reply")
    s.m("tn", "cd", "trang chi tiết yêu cầu", "reply")
    s.end()
    return s.diagram("Hinh 3.11 Tuan tu bao su co AI phan loai", "Báo sự cố và AI phân loại")


def seq_dieu_phoi():
    s = Seq([("tn", ":Tiếp nhận sự cố"), ("dp", ":Điều phối sự cố"), ("qt", ":Quy trình sự cố"), ("db", ":CSDL"),
             ("mail", ":Dịch vụ email")])
    s.m("tn", "dp", "điều phối(yêu cầu)")
    s.begin("alt", "mô tả rõ và có thợ phù hợp", "dp", "mail")
    s.m("dp", "db", "đếm việc đang mở của từng thợ")
    s.m("db", "dp", "số việc theo thợ", "reply")
    s.m("dp", "dp", "chọn thợ ít việc nhất")
    s.m("dp", "qt", "giao việc(yêu cầu, thợ)")
    s.m("qt", "db", "lưu người phụ trách, lịch sử")
    s.m("db", "qt", "đã lưu", "reply")
    s.m("qt", "dp", "đã giao", "reply")
    s.m("dp", "mail", "gửi thư giao việc(thợ)")
    s.m("mail", "dp", "đã gửi", "reply")
    s.begin("opt", "mức Khẩn cấp", "dp", "mail")
    s.m("dp", "mail", "gửi thư theo dõi(quản lý)")
    s.m("mail", "dp", "đã gửi", "reply")
    s.end()
    s.other("mô tả mơ hồ hoặc không có thợ")
    s.m("dp", "mail", "gửi thư nhờ phân công(quản lý)")
    s.m("mail", "dp", "đã gửi", "reply")
    s.end()
    s.m("dp", "tn", "đã điều phối", "reply")
    return s.diagram("Hinh 3.12 Tuan tu dieu phoi sau khi luu", "Điều phối sau khi lưu yêu cầu")


def seq_hoan_tat():
    s = Seq([("ktv", "Kỹ thuật viên"), ("xl", ":Xử lý việc được giao"), ("qt", ":Quy trình sự cố"),
             ("db", ":CSDL")])
    s.m("ktv", "xl", "hoàn tất(yêu cầu, kết quả, ảnh)")
    s.m("xl", "db", "tìm yêu cầu được giao cho mình")
    s.m("db", "xl", "yêu cầu hoặc rỗng", "reply")
    s.begin("alt", "không tìm thấy", "ktv", "db")
    s.m("xl", "ktv", "báo không tìm thấy", "reply", close=False)
    s.other("tìm thấy")
    s.m("xl", "qt", "chuyển sang Đã xử lý(kết quả, ảnh)")
    s.m("qt", "qt", "kiểm tra bảng chuyển, kết quả")
    s.begin("alt", "vi phạm quy tắc", "xl", "db")
    s.m("qt", "xl", "lỗi vi phạm", "reply", close=False)
    s.other("hợp lệ")
    s.m("qt", "db", "lưu kết quả, ảnh, lịch sử")
    s.m("db", "qt", "đã lưu", "reply")
    s.m("qt", "xl", "thành công", "reply")
    s.end()
    s.m("xl", "ktv", "trang chi tiết kèm thông báo", "reply")
    s.end()
    return s.diagram("Hinh 3.13 Tuan tu ky thuat vien hoan tat", "Kỹ thuật viên hoàn tất xử lý")


def seq_duyet_thanh_toan():
    s = Seq([("ql", "Quản lý BQL"), ("xl", ":Xử lý thanh toán"), ("db", ":CSDL")])
    s.m("ql", "xl", "duyệt(thanh toán)")
    s.m("xl", "db", "đọc thanh toán, hoá đơn")
    s.m("db", "xl", "thanh toán, hoá đơn", "reply")
    s.m("xl", "xl", "kiểm tra trạng thái, số còn lại")
    s.begin("alt", "không còn chờ, hoá đơn đã huỷ hoặc vượt số còn lại", "ql", "db")
    s.m("xl", "ql", "báo lỗi, không lưu", "reply", close=False)
    s.other("hợp lệ")
    s.m("xl", "db", "lưu thanh toán Thành công, số đã trả, trạng thái hoá đơn")
    s.m("db", "xl", "đã lưu", "reply")
    s.m("xl", "ql", "đã duyệt", "reply")
    s.end()
    return s.diagram("Hinh 3.14 Tuan tu quan ly duyet thanh toan", "Quản lý duyệt xác nhận chuyển khoản")


# ======================================================================= dựng và xuất ảnh
def build(diagrams, only=None):
    """Dựng và xuất ảnh. only: danh sách tiền tố tên hình (vd "Hinh 3.9") để chỉ dựng lại các hình đó vào file
    tạm (nhanh); bỏ trống thì dựng cả bộ vào BaoCao_DACN_v2.mdj."""
    WORK.mkdir(parents=True, exist_ok=True)
    IMG.mkdir(parents=True, exist_ok=True)
    if only:
        diagrams = [d for d in diagrams if any(d["name"].startswith(o) for o in only)]
    mdj = WORK / "partial.mdj" if only else MDJ
    spec = {"project": "Hệ thống quản lý vận hành chung cư tích hợp AI phân loại và điều phối sự cố",
            "output": str(mdj), "diagrams": diagrams}
    spec_path = WORK / "spec.json"
    spec_path.write_text(json.dumps(spec, ensure_ascii=False, indent=1), encoding="utf8")
    staruml_cli.build(spec_path, WORK)
    print((WORK / "spec.json.log").read_text(encoding="utf8"))
    names = {d["name"] for d in diagrams}
    for old in IMG.glob("Hinh *.png"):
        if old.stem in names:
            old.unlink()
    staruml_cli.export(mdj, IMG)
    for p in IMG.glob("Hinh *.png"):
        if p.stem in names:
            uml_spec.flatten(p)


def diagrams():
    return [as_is(), to_be(), state_machine(), use_case_overview(), deployment(), uc03(), uc11(), uc16(), uc06(), uc20(),
            activity_ai(), erd_co_cau(), erd_tai_khoan(), erd_su_co(), erd_thu_phi(),
            rd_co_cau(), rd_tai_khoan(), rd_su_co(), rd_thu_phi(), class_co_cau(), class_su_co(), class_thu_phi(),
            seq_bao_su_co(), seq_dieu_phoi(), seq_hoan_tat(), seq_duyet_thanh_toan()]


if __name__ == "__main__":
    import sys
    build(diagrams(), sys.argv[1:])
