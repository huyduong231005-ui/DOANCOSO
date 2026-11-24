"""Mô tả 6 sơ đồ của báo cáo (tuần 1-3) để StarUML dựng, rồi xuất ảnh.

Chạy: python docs/baocao/tools/uml_spec.py
  -> docs/baocao/staruml/BaoCao_DACN.mdj   (file StarUML gốc, mở được bằng StarUML)
  -> docs/baocao/images/<tên sơ đồ>.png    (ảnh xuất từ StarUML, nền trắng)
Mọi hình đều do StarUML vẽ qua extension staruml_ext (lệnh baocao:build); file này chỉ mô tả
phần tử, vị trí và đường gấp của các cạnh.
"""
import json
from pathlib import Path

from PIL import Image, ImageFont

import staruml_cli
import uml_spec_ch3

HERE = Path(__file__).resolve().parent
ROOT = HERE.parent
MDJ = ROOT / "staruml" / "BaoCao_DACN.mdj"
IMG = ROOT / "images"
WORK = ROOT / "staruml" / "_work"

# ================================================================ sơ đồ hoạt động (swimlane)
FS_ACT = 14
LANE_W, ACT_W, ACT_H = 230, 190, 60


def lane_x(i):
    return 20 + i * LANE_W


def cx(lane):
    return lane_x(lane) + LANE_W // 2


def act(key, lane, y, name):
    return {"key": key, "kind": "action", "name": name, "x": cx(lane) - ACT_W // 2, "y": y, "w": ACT_W, "h": ACT_H}


def dot(key, lane, y, kind="initial"):
    s = 20 if kind == "initial" else 26
    return {"key": key, "kind": kind, "x": cx(lane) - s // 2, "y": y, "w": s, "h": s}


def diamond(key, lane, y):
    return {"key": key, "kind": "decision", "x": cx(lane) - 16, "y": y, "w": 32, "h": 40}


def lanes(names, height):
    return [{"key": f"lane{i}", "kind": "partition", "name": n, "x": lane_x(i), "y": 10, "w": LANE_W, "h": height}
            for i, n in enumerate(names)]


def flow(a, b, guard=None, points=None):
    e = {"kind": "flow", "from": a, "to": b}
    if guard:
        e["guard"] = guard
    if points:
        e["points"] = points
    return e


def as_is():
    nodes = lanes(["Cư dân", "Lễ tân / Nhân viên BQL", "Kỹ thuật trưởng BQL", "Kỹ thuật viên (thợ)"], 960)
    bar = 150  # thanh fork / join
    nodes += [
        dot("i", 0, 50),
        act("a1", 0, 95, "Phát hiện sự cố (điện, nước, thiết bị, cửa...)"),
        act("a2", 0, 190, "Gọi hotline, nhắn Zalo hoặc đến quầy lễ tân"),
        act("b1", 1, 190, "Ghi vào sổ tiếp nhận / file Excel [P1]"),
        act("c1", 2, 285, "Đánh giá mức độ, chọn thợ theo kinh nghiệm [P2]"),
        {"key": "m", "kind": "merge", "x": cx(2) - 16, "y": 370, "w": 32, "h": 40},
        diamond("d", 2, 435),
        act("c2", 2, 530, "Hẹn lại hoặc liên hệ nhà thầu ngoài"),
        act("t1", 3, 425, "Nhận cuộc gọi giao việc [P3]"),
        act("t2", 3, 530, "Đến căn hộ kiểm tra, sửa chữa"),
        act("t3", 3, 625, "Báo miệng / nhắn tin là đã xong [P4]"),
        act("b2", 1, 625, "Cập nhật sổ: đã xử lý [P5]"),
        {"key": "fk", "kind": "fork", "x": cx(1) - bar // 2, "y": 715, "w": bar, "h": 8},
        act("a3", 0, 745, "Biết kết quả nếu được báo (không nghiệm thu, đánh giá)"),
        act("c3", 2, 745, "Cuối tháng tổng hợp báo cáo thủ công từ sổ [P6]"),
        {"key": "jn", "kind": "join", "x": cx(1) - bar // 2, "y": 845, "w": bar, "h": 8},
        dot("f", 1, 885, "final"),
    ]
    left = cx(2) - ACT_W // 2
    edges = [
        flow("i", "a1"), flow("a1", "a2"), flow("a2", "b1"), flow("b1", "c1"), flow("c1", "m"), flow("m", "d"),
        flow("d", "t1", "có thợ rảnh"), flow("d", "c2", "không"),
        # Hẹn lại xong quay về gộp trước bước kiểm tra thợ (nút quyết định chỉ có một luồng vào).
        flow("c2", "m", points=[[left, 560], [left - 18, 560], [left - 18, 390], [cx(2) - 16, 390]]),
        flow("t1", "t2"), flow("t2", "t3"), flow("t3", "b2"), flow("b2", "fk"),
        # Song song: kết quả có thể tới cư dân, còn sổ được tổng hợp vào cuối tháng.
        flow("fk", "a3", points=[[cx(1) - 50, 723], [cx(1) - 50, 733], [cx(0), 733], [cx(0), 745]]),
        flow("fk", "c3", points=[[cx(1) + 50, 723], [cx(1) + 50, 733], [cx(2), 733], [cx(2), 745]]),
        flow("a3", "jn", points=[[cx(0), 805], [cx(0), 849], [cx(1) - bar // 2, 849]]),
        flow("c3", "jn", points=[[cx(2), 805], [cx(2), 849], [cx(1) + bar // 2, 849]]),
        flow("jn", "f"),
    ]
    return {"kind": "activity", "model": "Quy trình AS-IS", "owner": "Tiếp nhận và xử lý sự cố (AS-IS)",
            "name": "Hinh 1.1 Quy trinh AS-IS", "fontSize": FS_ACT, "nodes": nodes, "edges": edges}


def to_be():
    nodes = lanes(["Cư dân", "Hệ thống (web + AI)", "Quản lý BQL", "Kỹ thuật viên"], 1270)

    def merge(key, lane, y):
        return {"key": key, "kind": "merge", "x": cx(lane) - 16, "y": y, "w": 32, "h": 40}

    nodes += [
        dot("i", 0, 50),
        act("a1", 0, 95, "Đăng nhập, tạo yêu cầu: mô tả, căn hộ / khu vực chung, ảnh"),
        act("s1", 1, 95, "Kiểm tra cư dân đang ở và nơi xảy ra (BR-SC-01, 02)"),
        act("s2", 1, 195, "[AI] Phân loại: loại, mức ưu tiên, nguy hiểm, câu hỏi lại"),
        act("s3", 1, 295, "Chỉ nâng ưu tiên; tính hạn SLA; lưu yêu cầu và lịch sử"),
        diamond("d1", 1, 395),
        act("m1", 2, 385, "Nhận email: cần hỏi lại / cần phân công (kèm nhóm gợi ý)"),
        act("s4", 1, 495, "[Tự động] Giao thợ đúng chuyên môn, ít việc nhất; email giao việc"),
        act("m2", 2, 495, "Hỏi lại cư dân nếu cần; quyết định phân công hoặc từ chối"),
        diamond("d3", 2, 580),
        dot("f0", 2, 650, "final"),
        merge("g1", 3, 690),
        act("t1", 3, 760, "Xem việc được giao, bắt đầu xử lý"),
        merge("g2", 3, 845),
        act("t2", 3, 910, "Xử lý; tạm dừng khi chờ vật tư (không tính vào SLA)"),
        act("t3", 3, 1005, "Hoàn tất: ghi kết quả xử lý, ảnh nghiệm thu"),
        diamond("d2", 0, 1015),
        act("a2", 0, 1105, "Xác nhận đã xong, đánh giá 1–5"),
        act("s5", 1, 1105, "Đóng yêu cầu, ghi lịch sử, cập nhật số liệu KPI"),
        act("m3", 2, 1105, "Theo dõi KPI: đúng hạn SLA, MTTR, CSAT, quá hạn"),
        dot("f", 2, 1210, "final"),
    ]
    edges = [
        flow("i", "a1"), flow("a1", "s1"), flow("s1", "s2"), flow("s2", "s3"), flow("s3", "d1"),
        flow("d1", "s4", "rõ và có thợ"), flow("d1", "m1", "không"), flow("m1", "m2"), flow("m2", "d3"),
        # Hai nguồn giao việc (tự động / quản lý) gộp lại trước khi kỹ thuật viên nhận việc.
        flow("s4", "g1", points=[[cx(1), 555], [cx(1), 710], [cx(3) - 16, 710]]),
        flow("d3", "g1", "phân công", points=[[cx(2) + 16, 600], [cx(3), 600], [cx(3), 690]]),
        flow("d3", "f0", "từ chối (ghi lý do)"),
        flow("g1", "t1"), flow("t1", "g2"), flow("g2", "t2"), flow("t2", "t3"), flow("t3", "d2"),
        flow("d2", "g2", "chưa khắc phục", points=[[cx(0), 1015], [cx(0), 865], [cx(3) - 16, 865]]),
        flow("d2", "a2", "đã khắc phục"), flow("a2", "s5"), flow("s5", "m3"), flow("m3", "f"),
    ]
    return {"kind": "activity", "model": "Quy trình TO-BE", "owner": "Tiếp nhận và xử lý sự cố (TO-BE)",
            "name": "Hinh 1.2 Quy trinh TO-BE", "fontSize": FS_ACT, "nodes": nodes, "edges": edges}


# ================================================================ sơ đồ trạng thái
def states():
    W, H, X, RX = 160, 44, 420, 760
    ys = {"open": 60, "ack": 190, "prog": 320, "res": 450, "closed": 580}
    names = {"open": "Mới gửi", "ack": "Đã tiếp nhận", "prog": "Đang xử lý", "res": "Đã xử lý", "closed": "Đã đóng"}
    nodes = [{"key": "i", "kind": "pseudo", "x": X + W // 2 - 10, "y": 10, "w": 20, "h": 20}]
    nodes += [{"key": k, "kind": "state", "name": names[k], "x": X, "y": y, "w": W, "h": H} for k, y in ys.items()]
    nodes += [
        {"key": "rej", "kind": "state", "name": "Từ chối", "x": RX, "y": ys["open"], "w": W, "h": H},
        {"key": "can", "kind": "state", "name": "Đã huỷ", "x": RX, "y": ys["ack"], "w": W, "h": H},
        {"key": "hold", "kind": "state", "name": "Tạm dừng", "x": RX, "y": ys["prog"], "w": W, "h": H},
        {"key": "f1", "kind": "finalstate", "x": X + W // 2 - 13, "y": 690, "w": 26, "h": 26},
        {"key": "f2", "kind": "finalstate", "x": RX + W + 60, "y": ys["open"] + 9, "w": 26, "h": 26},
        {"key": "f3", "kind": "finalstate", "x": RX + W + 60, "y": ys["ack"] + 9, "w": 26, "h": 26},
    ]

    def tr(a, b, name=None, points=None, ls=0):
        e = {"kind": "transition", "from": a, "to": b, "lineStyle": ls}
        if name:
            e["name"] = name
        if points:
            e["points"] = points
        return e

    p, r = ys["prog"], ys["res"]
    edges = [
        tr("i", "open"), tr("open", "ack", "QL/HT: tiếp nhận, giao việc"), tr("ack", "prog", "KTV/QL: bắt đầu"),
        tr("prog", "res", "KTV/QL: hoàn tất [có kết quả]"), tr("res", "closed", "CD/QL: xác nhận, đánh giá"),
        tr("res", "prog", "CD/QL: chưa khắc phục [có lý do]",
           points=[[X, r + 22], [X - 150, r + 22], [X - 150, p + 22], [X, p + 22]]),
        tr("prog", "hold", "KTV/QL: tạm dừng [có lý do]", points=[[X + W, p + 12], [RX, p + 12]]),
        tr("hold", "prog", "KTV/QL: tiếp tục (dời hạn SLA)", points=[[RX, p + 34], [X + W, p + 34]]),
        tr("open", "rej", "QL: từ chối [có lý do]"), tr("ack", "can", "CD/QL: huỷ"),
        tr("open", "can", ls=1), tr("ack", "rej", ls=1),
        tr("closed", "f1"), tr("rej", "f2"), tr("can", "f3"),
    ]
    return {"kind": "state", "model": "Vòng đời yêu cầu sự cố", "owner": "Vòng đời yêu cầu sự cố",
            "name": "Hinh 1.3 So do trang thai yeu cau su co", "fontSize": 14, "nodes": nodes, "edges": edges}


# ================================================================ use case
def use_case():
    UW, UH = 235, 60

    def uc(key, name, x, y):
        return {"key": key, "kind": "usecase", "name": name, "x": x, "y": y, "w": UW, "h": UH}

    def actor(key, name, x, y):
        return {"key": key, "kind": "actor", "name": name, "x": x, "y": y, "w": 100, "h": 90}

    L, M, R = 190, 440, 700
    # Cột phải: 8 use case của quản lý (4 vận hành + 4 thu phí), rồi quản trị, kỹ thuật viên; mỗi ô cách 72.
    right = [("uc09", "UC09 Quản lý Khu – Toà – Tầng – Căn"), ("uc10", "UC10 Quản lý cư dân và thư mời"),
             ("uc11", "UC11 Tiếp nhận, phân công, từ chối sự cố"), ("uc12", "UC12 Xem báo cáo KPI sự cố"),
             ("uc17", "UC17 Ghi chỉ số điện nước"), ("uc18", "UC18 Lập hoá đơn tháng và phát sinh"),
             ("uc20", "UC20 Ghi nhận, duyệt thanh toán, hoàn tiền"), ("uc21", "UC21 Xem báo cáo tài chính, công nợ"),
             ("uc13", "UC13 Quản lý người dùng và phân quyền"), ("uc14", "UC14 Xem nhật ký hệ thống"),
             ("uc15", "UC15 Xem việc được giao"), ("uc16", "UC16 Cập nhật xử lý: bắt đầu, tạm dừng, hoàn tất")]
    ry = {k: 50 + i * 72 for i, (k, _) in enumerate(right)}
    mid = lambda *ks: (ry[ks[0]] + ry[ks[-1]] + UH) // 2 - 45      # tâm dọc của nhóm use case, trừ nửa cao tác nhân
    nodes = [
        {"key": "sub", "kind": "subject", "name": "Hệ thống quản lý vận hành chung cư", "x": 170, "y": 10, "w": 780,
         "h": ry["uc16"] + UH + 30},
        uc("uc01", "UC01 Đăng nhập", L, 50), uc("uc02", "UC02 Kích hoạt tài khoản qua thư mời", L, 128),
        uc("uc03", "UC03 Gửi yêu cầu sự cố", L, 206), uc("uc04", "UC04 Theo dõi yêu cầu của mình", L, 284),
        uc("uc05", "UC05 Huỷ yêu cầu", L, 362), uc("uc06", "UC06 Xác nhận hoàn tất và đánh giá", L, 440),
        uc("uc19", "UC19 Xem hoá đơn, gửi xác nhận thanh toán", L, 518),
        uc("uc08", "UC08 Điều phối và gửi email", M, 310), uc("uc07", "UC07 Phân loại sự cố bằng AI", M, 530),
        *[uc(k, name, R, ry[k]) for k, name in right],
        actor("cd", "Cư dân", 30, (50 + 518 + UH) // 2 - 45), actor("ql", "Quản lý BQL", 1010, mid("uc09", "uc21")),
        actor("qt", "Quản trị viên", 1010, mid("uc13", "uc14")), actor("ktv", "Kỹ thuật viên", 1010, mid("uc15", "uc16")),
        actor("ai", "Mô hình AI (Ollama)", 500, ry["uc16"] + UH + 70),
        actor("mail", "Dịch vụ email (SMTP)", 760, -160),
    ]

    def a(x, y):
        return {"kind": "assoc", "from": x, "to": y, "lineStyle": 1}

    pos = {n["key"]: n for n in nodes}

    def bus(actor_key, uc_key):
        """Đường vuông góc: từ tác nhân ra trục dọc giữa tác nhân và cột use case, rồi rẽ vào use case; tránh cắt qua
        các use case khác khi một tác nhân có nhiều use case xếp chồng."""
        ac, u = pos[actor_key], pos[uc_key]
        ya, yu = ac["y"] + 45, u["y"] + UH // 2
        if ac["x"] > u["x"]:                                     # tác nhân bên phải
            xa, xu = ac["x"] + 25, u["x"] + UW
        else:
            xa, xu = ac["x"] + ac["w"] - 25, u["x"]
        xb = (xa + xu) // 2
        if abs(ya - yu) < UH // 2:                               # ngang tầm tác nhân: đường thẳng (StarUML tự đi
            return {"kind": "assoc", "from": actor_key, "to": uc_key, "lineStyle": 1,  # vòng nếu để vuông góc)
                    "points": [[xa, yu], [xu, yu]]}
        return {"kind": "assoc", "from": actor_key, "to": uc_key, "lineStyle": 0,
                "points": [[xa, ya], [xb, ya], [xb, yu], [xu, yu]]}

    edges = [bus("cd", u) for u in ("uc01", "uc02", "uc03", "uc04", "uc05", "uc06", "uc19")]
    edges += [bus("ql", u) for u in ("uc09", "uc10", "uc11", "uc12", "uc17", "uc18", "uc20", "uc21")]
    edges += [bus("qt", u) for u in ("uc13", "uc14")] + [bus("ktv", u) for u in ("uc15", "uc16")]
    edges += [a("ai", "uc07"), a("mail", "uc08")]
    edges += [{"kind": "include", "from": "uc03", "to": "uc07", "lineStyle": 1},
              {"kind": "include", "from": "uc03", "to": "uc08", "lineStyle": 1},
              {"kind": "generalization", "from": "qt", "to": "ql", "lineStyle": 1}]
    # StarUML không hiển thị phần tử ở toạ độ âm: dời cả sơ đồ xuống để tác nhân email (đặt trên khung) nằm trong vùng vẽ.
    dy = 20 - min(n["y"] for n in nodes)
    for n in nodes:
        n["y"] += dy
    return {"kind": "usecase", "model": "Use case", "name": "Hinh 1.4 So do use case tong quat", "fontSize": 14,
            "nodes": nodes, "edges": edges}


# ================================================================ ERD và lược đồ quan hệ
FS_ER = 14
_F = ImageFont.truetype(r"C:\Windows\Fonts\times.ttf", FS_ER)
_FB = ImageFont.truetype(r"C:\Windows\Fonts\timesbd.ttf", FS_ER)


def keys(c):
    k = [("PK", c.get("pk")), ("FK", c.get("fk")), ("N", c.get("nullable") and not c.get("pk")), ("U", c.get("unique"))]
    return ",".join(t for t, on in k if on)


def entity_size(name, cols):
    """Ước lượng khung giống ERDEntityView của StarUML (cột khoá | tên | kiểu), cộng thêm lề an toàn."""
    kw = max([_F.getlength(keys(c)) for c in cols] + [0])
    nw = max(_F.getlength(c["name"]) for c in cols)
    tw = max([_F.getlength(c.get("type", "")) for c in cols] + [0])
    item = kw + 20 + nw + (20 + tw if tw else 0)
    w = max(item + 10, _FB.getlength(name) + 10) + 44
    n = len(cols)
    h = 5 + FS_ER + 5 + 5 + n * FS_ER + (n - 1) * 2 + 5 + 4 + 8
    return int(w), int(h)


class Boxes(dict):
    def L(self, n): return self[n][0]
    def T(self, n): return self[n][1]
    def R(self, n): return self[n][0] + self[n][2]
    def B(self, n): return self[n][1] + self[n][3]


CARD = {"(0,n)": "0..*", "(1,n)": "1..*", "(1,1)": "1", "(0,1)": "0..1"}

# Thực thể khái niệm: khoá chính (*) và thuộc tính mô tả, không có khoá ngoại.
ENT = {
    "KHU_VUC": ["Id*", "Ten", "Slug"],
    "KHU": ["Id*", "Ten", "Slug", "DiaChi"],
    "TOA_NHA": ["Id*", "Ma", "Ten", "DiaChi", "SoLuongTang", "TrangThai"],
    "TANG": ["Id*", "SoTang", "NhanTang"],
    "CAN_HO": ["Id*", "MaCanHo", "DienTich", "SoPhongNgu", "TinhTrangThue"],
    "CU_DAN": ["Id*", "HoTen", "SoDienThoai", "VaiTro", "NgayVaoO", "NgayChuyenDi"],
    "NGUOI_DUNG": ["Id*", "UserName", "Email", "HoTen"],
    "VAI_TRO": ["Id*", "Name"],
    "QUYEN": ["Id*", "Ma", "Module"],
    "YEU_CAU_SU_CO": ["Id*", "SoYeuCau", "TieuDe", "MoTa", "LoaiBaoTri", "TrangThai", "NgayBaoCao", "HanXuLy", "DanhGia"],
    "MUC_SLA": ["MucDoUuTien*", "PhutPhanHoi", "PhutHoanTat"],
    "ANH_SU_CO": ["Id*", "DuongDan", "GiaiDoan"],
    "LICH_SU_TRANG_THAI": ["Id*", "TuTrangThai", "SangTrangThai", "ThoiDiem", "GhiChu"],
    "NHAT_KY_HE_THONG": ["Id*", "TenBang", "HanhDong", "ThoiGian"],
    # Phân hệ thu phí
    "LOAI_DICH_VU": ["Id*", "Ma", "Ten", "DonViTinh", "DonGiaMacDinh"],
    "CHI_SO_DICH_VU": ["Id*", "ThangTinhPhi", "ChiSoCu", "ChiSoMoi", "TieuThu", "DonGia", "ThanhTien"],
    "HOA_DON": ["Id*", "SoHoaDon", "LoaiHoaDon", "ThangTinhPhi", "NgayPhatHanh", "NgayDenHan", "TamTinh", "PhiTreHan",
                "TongTien", "DaThanhToan", "ConLai", "TrangThai"],
    "CHI_TIET_HOA_DON": ["Id*", "MoTa", "SoLuong", "DonGia", "ThanhTien"],
    "THANH_TOAN": ["Id*", "SoThanhToan", "SoTien", "PhuongThuc", "TrangThai", "NgayThanhToan", "SoTienHoanTra"],
    "PHI_DINH_KY": ["Id*", "MoTa", "SoTien", "DangApDung"],
}
# Hai cột thu phí đặt bên trái, nối vào cạnh trái của CAN_HO; phần còn lại dời sang phải.
BILL_COLS = [["LOAI_DICH_VU", "CHI_TIET_HOA_DON", "THANH_TOAN"], ["CHI_SO_DICH_VU", "HOA_DON", "PHI_DINH_KY"]]
BILL_Y = {"LOAI_DICH_VU": 20, "CHI_SO_DICH_VU": 20, "HOA_DON": 330, "CHI_TIET_HOA_DON": 330, "THANH_TOAN": 480,
          "PHI_DINH_KY": 620}
ENT_POS = {
    "KHU_VUC": (20, 20), "KHU": (330, 20), "VAI_TRO": (680, 20), "QUYEN": (1010, 20),
    "TOA_NHA": (330, 220), "NGUOI_DUNG": (680, 220), "TANG": (20, 270),
    "CAN_HO": (20, 470), "YEU_CAU_SU_CO": (330, 470), "LICH_SU_TRANG_THAI": (680, 470), "NHAT_KY_HE_THONG": (1010, 470),
    "ANH_SU_CO": (680, 680), "CU_DAN": (20, 720), "MUC_SLA": (330, 775),
}
# (A, B, tên, bản số Chen phía A, phía B) - khớp Bảng 2.3
REL = [
    ("KHU_VUC", "KHU", "có", "(0,n)", "(1,1)"),
    ("KHU_VUC", "TOA_NHA", "chứa", "(0,n)", "(1,1)"),
    ("KHU", "TOA_NHA", "gồm", "(0,n)", "(0,1)"),
    ("TOA_NHA", "TANG", "có", "(1,n)", "(1,1)"),
    ("TANG", "CAN_HO", "có", "(1,n)", "(1,1)"),
    ("CAN_HO", "CU_DAN", "có", "(0,n)", "(1,1)"),
    ("NGUOI_DUNG", "CU_DAN", "là tài khoản của", "(0,n)", "(0,1)"),
    ("NGUOI_DUNG", "TOA_NHA", "quản lý", "(0,n)", "(0,1)"),
    ("NGUOI_DUNG", "VAI_TRO", "đóng vai", "(1,n)", "(0,n)"),
    ("VAI_TRO", "QUYEN", "được cấp", "(0,n)", "(0,n)"),
    ("CAN_HO", "YEU_CAU_SU_CO", "phát sinh tại", "(0,n)", "(0,1)"),
    ("TOA_NHA", "YEU_CAU_SU_CO", "khu vực chung", "(0,n)", "(0,1)"),
    ("NGUOI_DUNG", "YEU_CAU_SU_CO", "báo", "(0,n)", "(1,1)"),
    ("NGUOI_DUNG", "YEU_CAU_SU_CO", "phụ trách", "(0,n)", "(0,1)"),
    ("MUC_SLA", "YEU_CAU_SU_CO", "áp dụng", "(0,n)", "(1,1)"),
    ("YEU_CAU_SU_CO", "ANH_SU_CO", "có ảnh", "(0,n)", "(1,1)"),
    ("YEU_CAU_SU_CO", "LICH_SU_TRANG_THAI", "ghi nhận", "(1,n)", "(1,1)"),
    ("NGUOI_DUNG", "LICH_SU_TRANG_THAI", "thực hiện", "(0,n)", "(0,1)"),
    ("CAN_HO", "HOA_DON", "được lập", "(0,n)", "(1,1)"),
    ("HOA_DON", "CHI_TIET_HOA_DON", "gồm", "(1,n)", "(1,1)"),
    ("HOA_DON", "THANH_TOAN", "được trả bằng", "(0,n)", "(1,1)"),
    ("CAN_HO", "CHI_SO_DICH_VU", "ghi chỉ số", "(0,n)", "(1,1)"),
    ("LOAI_DICH_VU", "CHI_SO_DICH_VU", "thuộc loại", "(0,n)", "(1,1)"),
    ("CAN_HO", "PHI_DINH_KY", "áp phí", "(0,n)", "(1,1)"),
]


def erd_routes(b):
    """Đường gấp vuông góc của từng mối kết hợp, cùng thứ tự với REL (điểm đầu ở A, điểm cuối ở B)."""
    kv, kh, tn, tg, ch, cd = "KHU_VUC", "KHU", "TOA_NHA", "TANG", "CAN_HO", "CU_DAN"
    nd, vt, q, yc, ms, an, ls = "NGUOI_DUNG", "VAI_TRO", "QUYEN", "YEU_CAU_SU_CO", "MUC_SLA", "ANH_SU_CO", "LICH_SU_TRANG_THAI"
    bottom = max(b.B(n) for n in b) + 40
    right = b.L("NHAT_KY_HE_THONG") - 40
    return [
        [[b.R(kv), 55], [b.L(kh), 55]],
        [[b.R(kv) - 25, b.B(kv)], [b.R(kv) - 25, b.T(tn) + 22], [b.L(tn), b.T(tn) + 22]],
        [[b.L(kh) + 70, b.B(kh)], [b.L(kh) + 70, b.T(tn)]],
        [[b.L(tn), b.T(tg) + 45], [b.R(tg), b.T(tg) + 45]],
        [[b.L(tg) + 80, b.B(tg)], [b.L(tg) + 80, b.T(ch)]],
        [[b.L(ch) + 80, b.B(ch)], [b.L(ch) + 80, b.T(cd)]],
        [[b.R(nd), b.T(nd) + 70], [right, b.T(nd) + 70], [right, bottom], [b.L(cd) + 110, bottom],
         [b.L(cd) + 110, b.B(cd)]],
        [[b.L(nd), b.T(nd) + 40], [b.R(tn), b.T(nd) + 40]],
        [[b.L(nd) + 70, b.T(nd)], [b.L(nd) + 70, b.B(vt)]],
        [[b.R(vt), 50], [b.L(q), 50]],
        [[b.R(ch), b.T(yc) + 45], [b.L(yc), b.T(yc) + 45]],
        [[b.L(tn) + 60, b.B(tn)], [b.L(tn) + 60, b.T(yc)]],
        [[b.L(nd) + 20, b.B(nd)], [b.L(nd) + 20, b.T(yc) - 60], [b.R(yc) - 50, b.T(yc) - 60],
         [b.R(yc) - 50, b.T(yc)]],
        [[b.L(nd) + 45, b.B(nd)], [b.L(nd) + 45, b.T(yc) - 30], [b.R(yc) - 20, b.T(yc) - 30],
         [b.R(yc) - 20, b.T(yc)]],
        [[b.L(yc) + 90, b.T(ms)], [b.L(yc) + 90, b.B(yc)]],
        [[b.R(yc), b.T(an) + 30], [b.L(an), b.T(an) + 30]],
        [[b.R(yc), b.T(ls) + 50], [b.L(ls), b.T(ls) + 50]],
        [[b.L(nd) + 120, b.B(nd)], [b.L(nd) + 120, b.T(ls)]],
        *bill_routes(b, ch, *BILL_COLS[0], *BILL_COLS[1]),
    ]


def bill_routes(b, ch, ldv, ct, tt, cs, hd, pdk):
    """Thu phí, cùng thứ tự với 6 dòng cuối REL / rd_relations: căn → hoá đơn, hoá đơn → chi tiết, hoá đơn → thanh
    toán, căn → chỉ số, loại dịch vụ → chỉ số, căn → phí định kỳ. Đường dọc chạy giữa cột thu phí và cột căn hộ."""
    xm = b.L(ch) - 60
    y_hd = max(b.T(ch), b.T(hd)) + 50
    y_tt = (max(b.T(tt), b.T(hd)) + min(b.B(tt), b.B(hd))) // 2
    return [
        [[b.L(ch), y_hd], [b.R(hd), y_hd]],
        [[b.L(hd), b.T(ct) + 40], [b.R(ct), b.T(ct) + 40]],
        [[b.L(hd), y_tt], [b.R(tt), y_tt]],
        [[b.L(ch), b.T(ch) + 20], [xm, b.T(ch) + 20], [xm, b.T(cs) + 60], [b.R(cs), b.T(cs) + 60]],
        [[b.R(ldv), b.T(ldv) + 40], [b.L(cs), b.T(ldv) + 40]],
        [[b.L(ch), b.B(ch) - 20], [xm, b.B(ch) - 20], [xm, b.T(pdk) + 50], [b.R(pdk), b.T(pdk) + 50]],
    ]


def erd():
    cols = {n: [{"name": c.rstrip("*"), "pk": c.endswith("*")} for c in ENT[n]] for n in ENT}
    b = Boxes()
    xs = [20]
    for col in BILL_COLS:
        xs.append(xs[-1] + max(entity_size(n, cols[n])[0] for n in col) + 90)
    dx = xs[-1] + 30 - 20
    pos = {n: (xs[i], BILL_Y[n]) for i, col in enumerate(BILL_COLS) for n in col}
    pos.update({n: (x + dx, y) for n, (x, y) in ENT_POS.items()})
    for n, (x, y) in pos.items():
        w, h = entity_size(n, cols[n])
        b[n] = (x, y, w, max(h, 250) if n == "YEU_CAU_SU_CO" else h)
    nodes = [{"key": n, "kind": "entity", "name": n, "x": x, "y": y, "w": w, "h": h, "columns": cols[n]}
             for n, (x, y, w, h) in b.items()]
    # Chân gà ở đầu B thể hiện số B ứng với một A, tức bản số Chen phía A (và ngược lại).
    edges = [{"kind": "relation", "from": a, "to": bb, "name": name, "card1": CARD[cb], "card2": CARD[ca], "points": pts}
             for (a, bb, name, ca, cb), pts in zip(REL, erd_routes(b))]
    full = {"kind": "erd", "model": "ERD - Mô hình thực thể kết hợp", "name": "Hinh 2.1 ERD", "fontSize": FS_ER,
            "nodes": nodes, "edges": edges}
    bill = {n for col in BILL_COLS for n in col}
    return [part_of(full, set(ENT_POS), "Hinh 2.1 ERD", "ERD - Mô hình thực thể kết hợp"),
            part_of(full, bill | {"CAN_HO"}, "Hinh 2.1b ERD thu phi", "ERD - Phân hệ thu phí")]


def part_of(full, keep, name, model):
    """Tách một phần của sơ đồ đầy đủ (để in đủ cỡ chữ): giữ các bảng trong keep và các đường nối giữa chúng,
    dời toạ độ cho sát lề trái. Bố cục, đường gấp giữ nguyên như sơ đồ đầy đủ."""
    nodes = [dict(n) for n in full["nodes"] if n["key"] in keep]
    edges = [dict(e) for e in full["edges"] if e["from"] in keep and e["to"] in keep]
    dx = min([n["x"] for n in nodes] + [p[0] for e in edges for p in e.get("points", [])]) - 20
    for n in nodes:
        n["x"] -= dx
    for e in edges:
        e["points"] = [[x - dx, y] for x, y in e.get("points", [])]
    return {**full, "name": name, "model": model, "nodes": nodes, "edges": edges}


def T(name, *flags):
    return {"name": name, "pk": "pk" in flags, "fk": "fk" in flags, "nullable": "null" in flags, "unique": "uq" in flags}


# Lược đồ quan hệ: đủ cột (trừ 8 cột kiểm toán); kiểu dữ liệu ở từ điển dữ liệu mục 2.6.
TABLES = {
    "KhuVuc": [T("Id", "pk"), T("Ten"), T("Slug", "uq"), T("UrlAnh", "null")],
    "DuAn": [T("Id", "pk"), T("KhuVucId", "fk"), T("Ten"), T("Slug", "uq"), T("DiaChi"), T("MoTaNgan")],
    "ToaNha": [T("Id", "pk"), T("KhuVucId", "fk"), T("DuAnId", "fk", "null"), T("QuanLyId", "fk", "null"), T("Ma"),
               T("Ten"), T("Slug", "uq"), T("DiaChi"), T("SoLuongTang"), T("TrangThai")],
    "Tang": [T("Id", "pk"), T("ToaNhaId", "fk"), T("SoTang"), T("NhanTang")],
    "CanHo": [T("Id", "pk"), T("ToaNhaId", "fk"), T("TangId", "fk"), T("MaCanHo"), T("DienTich"), T("SoPhongNgu"),
              T("SoPhongTam"), T("HuongNha", "null"), T("TinhTrangThue")],
    "CuDan": [T("Id", "pk"), T("CanHoId", "fk"), T("NguoiDungId", "fk", "null"), T("HoTen"), T("SoDienThoai", "null"),
              T("Email", "null"), T("VaiTro"), T("DaiDienCanHo"), T("NgayVaoO"), T("NgayChuyenDi", "null")],
    "NguoiDung": [T("Id", "pk"), T("UserName", "uq", "null"), T("Email", "null"), T("PasswordHash", "null"), T("HoTen"),
                  T("SoDienThoai", "null")],
    "VaiTro": [T("Id", "pk"), T("Name", "uq", "null")],
    "NguoiDung_VaiTro": [T("NguoiDungId", "pk", "fk"), T("VaiTroId", "pk", "fk")],
    "Quyen": [T("Id", "pk"), T("Ma", "uq"), T("Module"), T("TenHienThi")],
    "VaiTro_Quyen": [T("VaiTroId", "pk", "fk"), T("QuyenId", "pk", "fk")],
    "YeuCauBaoTri": [T("Id", "pk"), T("SoYeuCau", "uq"), T("CanHoId", "fk", "null"), T("ToaNhaKhuVucChungId", "fk", "null"),
                     T("ViTri", "null"), T("NguoiBaoCaoId", "fk", "null"), T("NguoiPhuTrachId", "fk", "null"),
                     T("TieuDe"), T("MoTa"), T("LoaiBaoTri"), T("MucDoUuTien", "fk"), T("TrangThai"),
                     T("AiTriageJson", "null"), T("NgayBaoCao"), T("NgayTiepNhan", "null"), T("NgayGiaiQuyet", "null"),
                     T("NgayDongLai", "null"), T("HanXuLy", "null"), T("GhiChuXuLy", "null"), T("DanhGia", "null"),
                     T("NhanXet", "null")],
    "MucSla": [T("MucDoUuTien", "pk"), T("PhutPhanHoi"), T("PhutHoanTat")],
    "AnhSuCo": [T("Id", "pk"), T("YeuCauBaoTriId", "fk"), T("DuongDan"), T("GiaiDoan"), T("NguoiTaiLenId", "fk", "null"),
                T("NgayTaiLen")],
    "LichSuTrangThaiSuCo": [T("Id", "pk"), T("YeuCauBaoTriId", "fk"), T("TuTrangThai", "null"), T("SangTrangThai"),
                            T("NguoiThucHienId", "fk", "null"), T("ThoiDiem"), T("GhiChu", "null")],
    "NhatKyHeThong": [T("Id", "pk"), T("TenBang"), T("KhoaBang", "null"), T("HanhDong"), T("NguoiDungId", "null"),
                      T("ThoiGian"), T("GiaTriCu", "null"), T("GiaTriMoi", "null")],
    # Phân hệ thu phí
    "LoaiDichVu": [T("Id", "pk"), T("Ma", "uq"), T("Ten"), T("DonViTinh"), T("CheDoTinhPhi"), T("DonGiaMacDinh"),
                   T("Icon", "null")],
    "ChiSoDichVu": [T("Id", "pk"), T("CanHoId", "fk"), T("LoaiDichVuId", "fk"), T("ThangTinhPhi"), T("ChiSoCu"),
                    T("ChiSoMoi"), T("TieuThu"), T("DonGia"), T("ThanhTien"), T("DaXuatHoaDon"),
                    T("NgayDocChiSo", "null"), T("GhiChu", "null")],
    "HoaDon": [T("Id", "pk"), T("SoHoaDon", "uq"), T("CanHoId", "fk"), T("LoaiHoaDon"), T("ThangTinhPhi"), T("DinhKy"),
               T("NgayPhatHanh"), T("NgayDenHan"), T("TamTinh"), T("GiamGia"), T("Thue"), T("PhiTreHan"), T("TongTien"),
               T("DaThanhToan"), T("ConLai"), T("TrangThai"), T("DonViTienTe"), T("GhiChu", "null")],
    "ChiTietHoaDon": [T("Id", "pk"), T("HoaDonId", "fk"), T("MoTa"), T("SoLuong"), T("DonGia"), T("ThanhTien"),
                      T("ThuTu")],
    "ThanhToan": [T("Id", "pk"), T("SoThanhToan", "uq"), T("HoaDonId", "fk"), T("SoTien"), T("DonViTienTe"),
                  T("PhuongThuc"), T("TrangThai"), T("MaGiaoDich", "null"), T("NhaCungCap", "null"),
                  T("NgayThanhToan", "null"), T("NgayHoanTien", "null"), T("SoTienHoanTra"), T("GhiChu", "null")],
    "PhiDinhKy": [T("Id", "pk"), T("CanHoId", "fk"), T("MoTa"), T("SoTien"), T("ThuTu"), T("DangApDung")],
}
RD_BILL = [["LoaiDichVu", "ChiTietHoaDon", "ThanhToan"], ["ChiSoDichVu", "HoaDon", "PhiDinhKy"]]
RD_COLS = [*RD_BILL, ["KhuVuc", "Tang", "CanHo", "CuDan"], ["DuAn", "ToaNha", "MucSla"], ["YeuCauBaoTri"],
           ["NguoiDung_VaiTro", "NguoiDung", "LichSuTrangThaiSuCo", "AnhSuCo"],
           ["VaiTro", "VaiTro_Quyen", "Quyen", "NhatKyHeThong"]]
GAP = 110


def rd_boxes():
    size = {n: entity_size(n, TABLES[n]) for n in TABLES}
    xs = [20]
    for col in RD_COLS[:-1]:
        xs.append(xs[-1] + max(size[n][0] for n in col) + GAP)
    col_of = {n: i for i, col in enumerate(RD_COLS) for n in col}
    b = Boxes()

    def put(n, y):
        b[n] = (xs[col_of[n]], y, *size[n])

    put("KhuVuc", 20), put("DuAn", 20), put("NguoiDung_VaiTro", 20), put("VaiTro", 20)
    put("VaiTro_Quyen", b.B("VaiTro") + 50), put("Quyen", b.B("VaiTro_Quyen") + 50)
    put("NguoiDung", b.B("NguoiDung_VaiTro") + 110)
    put("ToaNha", b.B("DuAn") + 90), put("Tang", b.T("ToaNha") + 50)
    top2 = max(b.B("ToaNha"), b.B("NguoiDung"), b.B("Quyen")) + 90
    put("CanHo", top2), put("YeuCauBaoTri", top2), put("LichSuTrangThaiSuCo", top2), put("NhatKyHeThong", top2)
    put("CuDan", b.B("CanHo") + 90), put("MucSla", b.B("YeuCauBaoTri") + 40)
    put("AnhSuCo", b.B("LichSuTrangThaiSuCo") + 90)
    put("ChiSoDichVu", 20), put("LoaiDichVu", 20)
    put("HoaDon", top2 - 80), put("ChiTietHoaDon", top2 - 80)
    put("ThanhToan", b.B("ChiTietHoaDon") + 50), put("PhiDinhKy", b.B("HoaDon") + 60)
    return b


# (bảng cha, bảng con, bản số phía cha, phía con, đường gấp từ cha sang con)
def rd_relations(b):
    kv, da, tn, tg, ch, cd = "KhuVuc", "DuAn", "ToaNha", "Tang", "CanHo", "CuDan"
    nd, vt, ndvt, vtq, q = "NguoiDung", "VaiTro", "NguoiDung_VaiTro", "VaiTro_Quyen", "Quyen"
    yc, ms, an, ls = "YeuCauBaoTri", "MucSla", "AnhSuCo", "LichSuTrangThaiSuCo"
    r3 = max(b.R(n) for n in RD_COLS[-2])
    l4 = min(b.L(n) for n in RD_COLS[-1])
    xr1, xr2 = r3 + 25, l4 - 25
    bottom = max(b.B(n) for n in b) + 40
    return [
        (kv, da, "1", "0..*", [[b.R(kv), 55], [b.L(da), 55]]),
        (kv, tn, "1", "0..*", [[b.R(kv) - 30, b.B(kv)], [b.R(kv) - 30, b.T(tn) + 25], [b.L(tn), b.T(tn) + 25]]),
        (da, tn, "0..1", "0..*", [[b.L(da) + 120, b.B(da)], [b.L(da) + 120, b.T(tn)]]),
        (nd, tn, "0..1", "0..*", [[b.L(nd), b.T(nd) + 60], [b.R(tn), b.T(nd) + 60]]),
        (tn, tg, "1", "1..*", [[b.L(tn), b.T(tg) + 50], [b.R(tg), b.T(tg) + 50]]),
        (tg, ch, "1", "1..*", [[b.L(tg) + 110, b.B(tg)], [b.L(tg) + 110, b.T(ch)]]),
        (tn, ch, "1", "0..*", [[b.L(tn) + 60, b.B(tn)], [b.L(tn) + 60, b.T(ch) + 50], [b.R(ch), b.T(ch) + 50]]),
        (ch, cd, "1", "0..*", [[b.L(ch) + 110, b.B(ch)], [b.L(ch) + 110, b.T(cd)]]),
        (nd, cd, "0..1", "0..*", [[b.R(nd), b.T(nd) + 30], [xr2, b.T(nd) + 30], [xr2, bottom], [b.L(cd) + 140, bottom],
                                  [b.L(cd) + 140, b.B(cd)]]),
        (nd, ndvt, "1", "0..*", [[b.L(nd) + 100, b.T(nd)], [b.L(nd) + 100, b.B(ndvt)]]),
        (vt, ndvt, "1", "0..*", [[b.L(vt), 50], [b.R(ndvt), 50]]),
        (vt, vtq, "1", "0..*", [[b.L(vt) + 90, b.B(vt)], [b.L(vt) + 90, b.T(vtq)]]),
        (q, vtq, "1", "0..*", [[b.L(q) + 90, b.T(q)], [b.L(q) + 90, b.B(vtq)]]),
        (ch, yc, "0..1", "0..*", [[b.R(ch), b.T(ch) + 120], [b.L(yc), b.T(ch) + 120]]),
        (tn, yc, "0..1", "0..*", [[b.R(tn) - 40, b.B(tn)], [b.R(tn) - 40, b.T(yc) + 30], [b.L(yc), b.T(yc) + 30]]),
        (nd, yc, "0..1", "0..*", [[b.L(nd) + 40, b.B(nd)], [b.L(nd) + 40, b.T(yc) - 45], [b.R(yc) - 40, b.T(yc) - 45],
                                  [b.R(yc) - 40, b.T(yc)]]),
        (ms, yc, "1", "0..*", [[b.R(ms), b.T(ms) + 40], [b.L(yc) + 50, b.T(ms) + 40], [b.L(yc) + 50, b.B(yc)]]),
        (yc, an, "1", "0..*", [[b.R(yc), b.T(an) + 50], [b.L(an), b.T(an) + 50]]),
        (yc, ls, "1", "1..*", [[b.R(yc), b.T(ls) + 50], [b.L(ls), b.T(ls) + 50]]),
        (nd, ls, "0..1", "0..*", [[b.L(nd) + 160, b.B(nd)], [b.L(nd) + 160, b.T(ls)]]),
        (nd, an, "0..1", "0..*", [[b.R(nd), b.T(nd) + 90], [xr1, b.T(nd) + 90], [xr1, b.T(an) + 90], [b.R(an), b.T(an) + 90]]),
        *[(p, c, "1", card, pts) for (p, c, card), pts in zip(
            [(ch, "HoaDon", "0..*"), ("HoaDon", "ChiTietHoaDon", "1..*"), ("HoaDon", "ThanhToan", "0..*"),
             (ch, "ChiSoDichVu", "0..*"), ("LoaiDichVu", "ChiSoDichVu", "0..*"), (ch, "PhiDinhKy", "0..*")],
            bill_routes(b, ch, *RD_BILL[0], *RD_BILL[1]))],
    ]


def rd():
    b = rd_boxes()
    nodes = [{"key": n, "kind": "entity", "name": n, "x": x, "y": y, "w": w, "h": h, "columns": TABLES[n]}
             for n, (x, y, w, h) in b.items()]
    edges = [{"kind": "relation", "from": p, "to": c, "card1": cp, "card2": cc, "points": pts}
             for p, c, cp, cc, pts in rd_relations(b)]
    full = {"kind": "erd", "model": "RD - Lược đồ quan hệ", "name": "Hinh 2.2 Luoc do quan he", "fontSize": FS_ER,
            "nodes": nodes, "edges": edges}
    bill = {n for col in RD_BILL for n in col}
    return [part_of(full, set(TABLES) - bill, "Hinh 2.2 Luoc do quan he", "RD - Lược đồ quan hệ"),
            part_of(full, bill | {"CanHo"}, "Hinh 2.2b Luoc do quan he thu phi", "RD - Phân hệ thu phí")]


# ================================================================
def flatten(png):
    """StarUML xuất PNG nền trong suốt: ghép lên nền trắng để in trắng đen."""
    im = Image.open(png).convert("RGBA")
    bg = Image.new("RGBA", im.size, "white")
    bg.alpha_composite(im)
    bg.convert("RGB").save(png, dpi=(300, 300))


def main():
    WORK.mkdir(parents=True, exist_ok=True)
    spec = {"project": "Hệ thống quản lý vận hành chung cư tích hợp AI phân loại và điều phối sự cố",
            "output": str(MDJ),
            "diagrams": [as_is(), to_be(), states(), use_case(), *erd(), *rd(), *uml_spec_ch3.diagrams()]}
    spec_path = WORK / "spec.json"
    spec_path.write_text(json.dumps(spec, ensure_ascii=False, indent=1), encoding="utf8")
    _, out = staruml_cli.build(spec_path, WORK)
    print(out.strip())
    print((WORK / "spec.json.log").read_text(encoding="utf8"))
    for old in IMG.glob("Hinh *.png"):
        old.unlink()
    _, out = staruml_cli.export(MDJ, IMG)
    print(out.strip())
    for p in IMG.glob("Hinh *.png"):
        flatten(p)


if __name__ == "__main__":
    main()
