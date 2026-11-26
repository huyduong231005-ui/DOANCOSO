"""Sơ đồ của Chương 3 (thiết kế hệ thống, tuần 4): kiến trúc, use case chi tiết, hoạt động luồng AI,
một sơ đồ lớp và ba sơ đồ tuần tự. Tên lớp, phương thức, tham số, nhánh rẽ lấy đúng theo mã nguồn trong t/
(kiểm tra tự động bằng kiem_tra_uml.py).

Hộp lớp do StarUML tự co theo nội dung, nên đường nối được tính theo kích thước đo ở lần dựng trước
(đọc từ BaoCao_DACN.mdj). Đổi nội dung lớp thì chạy uml_spec.py hai lần.
"""
import json
from pathlib import Path

from PIL import ImageFont

from uml_spec import act, cx, diamond, dot, flow, lanes

MDJ = Path(__file__).resolve().parent.parent / "staruml" / "BaoCao_DACN.mdj"
FONT = ImageFont.truetype("times.ttf", 12)
FONT_B = ImageFont.truetype("timesbd.ttf", 12)


def N(key, kind, name, x, y, w=None, h=None, **kw):
    n = {"key": key, "kind": kind, "x": x, "y": y, **kw}
    if name is not None:
        n["name"] = name
    if w:
        n["w"], n["h"] = w, h
    return n


def E(kind, a, b, **kw):
    return {"kind": kind, "from": a, "to": b, **kw}


def A(name, typ, vis="public", static=False):
    return {"name": name, "type": typ, "vis": vis, "static": static}


def O(name, params=(), ret=None, vis="public", static=False):
    """Tham số chỉ ghi tên (UML cho phép ẩn kiểu) để hộp lớp đủ hẹp khi in; kiểu trả về giữ nguyên."""
    return {"name": name, "params": [[p, ""] for p in params], "ret": ret, "vis": vis, "static": static}


# ------------------------------------------------------------------ đo kích thước hộp
def measured(doc):
    """{tên lớp: (rộng, cao)} của sơ đồ có documentation = doc trong lần dựng trước."""
    if not MDJ.exists():
        return {}
    idx = {}

    def walk(o):
        if isinstance(o, dict):
            if "_id" in o:
                idx[o["_id"]] = o
            for v in o.values():
                walk(v)
        elif isinstance(o, list):
            for v in o:
                walk(v)

    walk(json.loads(MDJ.read_text(encoding="utf8")))
    out = {}
    for d in idx.values():
        if d.get("_type") == "UMLClassDiagram" and d.get("documentation") == doc:
            for v in d["ownedViews"]:
                m = idx.get((v.get("model") or {}).get("$ref"), {})
                if v["_type"] in ("UMLClassView", "UMLInterfaceView", "UMLEnumerationView") and m.get("name"):
                    out[m["name"]] = (round(v["width"]), round(v["height"]))
    return out


def estimate(n):
    """Ước lượng khi chưa có lần dựng trước (sai số vài px, lần dựng thứ hai sẽ đúng)."""
    lines = [f"+{a['name']}: {a['type']}" for a in n.get("attrs", [])]
    lines += [f"+{o['name']}({', '.join(p for p, _ in o['params'])}): {o['ret']}" for o in n.get("ops", [])]
    lines += n.get("literals", [])
    w = max([FONT_B.getlength(n["name"])] + [FONT.getlength(s) for s in lines]) + 14
    h = 32 + 14 * len(lines) + (8 if n.get("attrs") and n.get("ops") else 0) + (12 if n["kind"] != "class" else 0)
    return round(w), h


class Boxes:
    """Toạ độ các hộp lớp: đặt theo kích thước thật để tính điểm nối."""

    def __init__(self, doc, nodes):
        size = measured(doc)
        self.n = {n["key"]: n for n in nodes}
        self.wh = {k: size.get(n["name"]) or estimate(n) for k, n in self.n.items()}

    def put(self, k, x, y):
        self.n[k]["x"], self.n[k]["y"] = x, y

    def w(self, k):
        return self.wh[k][0]

    def h(self, k):
        return self.wh[k][1]

    def L(self, k):
        return self.n[k]["x"]

    def T(self, k):
        return self.n[k]["y"]

    def R(self, k):
        return self.L(k) + self.w(k)

    def B(self, k):
        return self.T(k) + self.h(k)

    def CX(self, k):
        return self.L(k) + self.w(k) // 2

    def CY(self, k):
        return self.T(k) + self.h(k) // 2


# ------------------------------------------------------------------ Hình 3.1 kiến trúc triển khai
def deployment():
    nodes = [
        N("web", "node", "Máy người dùng: trình duyệt web (cư dân, quản lý, kỹ thuật viên, quản trị)", 130, 20, 300, 80,
          stereotype="device"),
        N("app", "node", "Máy chủ ứng dụng: .NET 10, ASP.NET Core MVC", 20, 150, 520, 470, stereotype="executionEnvironment"),
        N("pres", "component", "Tầng trình bày: giao diện web ba vùng (cư dân, kỹ thuật viên, quản lý – quản trị), đăng nhập ASP.NET Identity",
          60, 205, 440, 62),
        N("biz", "component", "Tầng nghiệp vụ: quy trình sự cố, điều phối thợ, tính KPI, lập hoá đơn", 40, 315, 220, 72),
        N("aic", "component", "Dịch vụ phân loại AI (gọi mô hình)", 290, 315, 210, 72),
        N("data", "component", "Tầng dữ liệu: ánh xạ đối tượng – bảng (EF Core, Npgsql)", 40, 435, 220, 62),
        N("mail", "component", "Gửi email (SMTP)", 290, 435, 210, 62),
        N("bg", "component", "Dịch vụ nền: lập hoá đơn tháng, nhắc hạn thanh toán, báo sự cố quá hạn, nạp sẵn mô hình AI", 40, 545, 460, 52),
        N("smtp", "node", "Máy chủ email SMTP (Gmail)", 620, 150, 230, 80, stereotype="device"),
        N("ai", "node", "Máy chủ AI: Ollama (GPU)", 620, 270, 230, 180, stereotype="device"),
        N("model", "artifact", "qwen3-suco4 (Qwen3-4B tinh chỉnh QLoRA, nén 4-bit)", 640, 355, 190, 75),
        N("pg", "node", "Máy chủ CSDL PostgreSQL", 620, 500, 230, 90, stereotype="device"),
    ]
    edges = [
        E("commpath", "web", "app", name="HTTPS", points=[[280, 100], [280, 150]]),
        E("commpath", "app", "smtp", name="SMTP 587", points=[[540, 190], [620, 190]]),
        E("commpath", "app", "ai", name="HTTP", points=[[540, 360], [620, 360]]),
        E("commpath", "app", "pg", name="TCP 5432", points=[[540, 545], [620, 545]]),
        E("dependency", "pres", "biz", stereotype="use", points=[[150, 267], [150, 315]]),
        E("dependency", "pres", "aic", stereotype="use", points=[[395, 267], [395, 315]]),
        E("dependency", "biz", "data", stereotype="use", points=[[150, 387], [150, 435]]),
        E("dependency", "biz", "mail", stereotype="use", points=[[260, 350], [275, 350], [275, 466], [290, 466]]),
        E("dependency", "bg", "data", stereotype="use", points=[[150, 545], [150, 497]]),
        E("dependency", "bg", "mail", stereotype="use", points=[[395, 545], [395, 497]]),
    ]
    return {"kind": "deployment", "model": "Kiến trúc triển khai", "name": "Hinh 3.1 Kien truc trien khai",
            "title": "Kiến trúc triển khai", "fontSize": 13, "nodes": nodes, "edges": edges}


# ------------------------------------------------------------------ Hình 3.2 – 3.5 use case chi tiết
# Mỗi use case lớn của luồng chính là một sơ đồ: use case gốc ở trên nối tác nhân chính, tách xuống các nhánh,
# mỗi nhánh có lá. «include» = phần luôn thực hiện (mũi tên từ use case gốc tới phần được gộp);
# «extend» = phần chỉ thực hiện khi có điều kiện (mũi tên từ phần mở rộng về use case được mở rộng).
UC_W, UC_H = 165, 62


def uc_diagram(name, title, ucs, rels, actors, links, box):
    """ucs: {key: (tên, tâm x, tâm y)}; rels: [(include|extend, từ, tới)]; actors: {key: (tên, tâm x, đỉnh y)};
    links: [(actor, use case)]; box: (trái, phải, dưới) của khung hệ thống."""
    left, right, bottom = box
    nodes = [N("sub", "subject", title, left, 10, right - left, bottom - 10)]
    nodes += [N(k, "usecase", n, x - UC_W // 2, y - UC_H // 2, UC_W, UC_H) for k, (n, x, y) in ucs.items()]
    nodes += [N(k, "actor", n, x - 50, y, 100, 90) for k, (n, x, y) in actors.items()]
    edges = [E(kind, a, b, lineStyle=1) for kind, a, b in rels]
    edges += [E("assoc", a, u, lineStyle=1) for a, u in links]
    return {"kind": "usecase", "model": title, "name": name, "title": title, "fontSize": 13,
            "nodes": nodes, "edges": edges}


def uc_gui_yeu_cau():
    A, B, C = 330, 710, 1090          # tâm ba nhánh
    r0, r1, r2, r3 = 75, 205, 335, 575  # lá giữa hạ thấp: nhãn «include» nằm dưới hàng lá hai bên
    ucs = {
        "root": ("UC03 Gửi yêu cầu sự cố", B, r0),
        "a": ("Ghi nhận yêu cầu", A, r1),
        "a1": ("Kiểm tra cư dân đang ở và nơi xảy ra", A - 100, r2),
        "a2": ("Tính hạn xử lý theo SLA", A + 100, r2),
        "a3": ("Áp dụng kết quả AI (chỉ nâng mức ưu tiên)", A, r3),
        "a1x": ("Ghi vị trí khu vực chung", A - 100, r3 + 115),
        "b": ("UC07 Phân loại sự cố bằng AI", B, r1),
        "b1": ("Hỏi lại khi mô tả mơ hồ", B - 100, r2),
        "b2": ("Giữ lựa chọn của cư dân khi AI lỗi", B + 100, r2),
        "c": ("UC08 Điều phối và gửi thông báo", C, r1),
        "c1": ("Tự giao thợ đúng chuyên môn, ít việc nhất", C - 100, r2),
        "c2": ("Cảnh báo quản trị viên khi AI không phản hồi", C + 100, r2),
        "c3": ("Gửi thư theo vai trò", C, r3),
    }
    rels = [("include", "root", "a"), ("include", "root", "b"), ("include", "root", "c"),
            ("include", "a", "a1"), ("include", "a", "a2"), ("include", "a", "a3"), ("extend", "a1x", "a1"),
            ("extend", "b1", "b"), ("extend", "b2", "b"),
            ("extend", "c1", "c"), ("extend", "c2", "c"), ("include", "c", "c3")]
    actors = {"cd": ("Cư dân", 60, 40), "ai": ("Mô hình AI", B, r3 + 230), "mail": ("Dịch vụ email", 1340, r1 - 45)}
    links = [("cd", "root"), ("ai", "b"), ("mail", "c")]
    return uc_diagram("Hinh 3.2 Use case gui yeu cau su co", "Gửi yêu cầu sự cố", ucs, rels, actors, links,
                      (140, 1280, r3 + 195))


def uc_tiep_nhan():
    xs = [250, 460, 670, 880]
    r0, r1, r2 = 75, 205, 335
    ucs = {
        "root": ("UC11 Tiếp nhận và phân công sự cố", 700, r0),
        "a": ("Xem và lọc yêu cầu", xs[0], r1), "a1": ("Lọc yêu cầu quá hạn", xs[0], r2),
        "b": ("Giao / giao lại kỹ thuật viên", xs[1], r1), "b1": ("Ghi lịch sử giao việc", xs[1], r2),
        "c": ("Phân loại lại yêu cầu", xs[2], r1), "c1": ("Tính lại hạn xử lý", xs[2], r2),
        "d": ("Từ chối yêu cầu", xs[3], r1), "d1": ("Ghi lý do từ chối", xs[3], r2),
        "e": ("Hoàn tất thay kỹ thuật viên", 1160, r1),
        "e1": ("Ghi kết quả xử lý", 1060, r2), "e2": ("Sinh hoá đơn chi phí sửa chữa", 1260, r2),
    }
    rels = [("include", "root", "a"), ("include", "root", "b"), ("extend", "c", "root"), ("extend", "d", "root"),
            ("extend", "e", "root"), ("extend", "a1", "a"), ("include", "b", "b1"), ("extend", "c1", "c"),
            ("include", "d", "d1"), ("include", "e", "e1"), ("extend", "e2", "e")]
    actors = {"ql": ("Quản lý BQL", 60, 40)}
    return uc_diagram("Hinh 3.3 Use case tiep nhan phan cong", "Tiếp nhận và phân công sự cố", ucs, rels, actors,
                      [("ql", "root")], (140, 1360, r2 + 75))


def uc_cap_nhat_xu_ly():
    r0, r1, r2 = 75, 205, 335
    ucs = {
        "root": ("UC16 Cập nhật xử lý sự cố", 640, r0),
        "a": ("UC15 Xem việc được giao", 250, r1), "a1": ("Chỉ thấy việc giao cho mình", 250, r2),
        "b": ("Bắt đầu xử lý", 460, r1), "b1": ("Kiểm tra quy tắc chuyển trạng thái", 460, r2),
        "c": ("Hoàn tất xử lý", 745, r1),
        "c1": ("Ghi kết quả xử lý", 645, r2), "c2": ("Tải ảnh nghiệm thu", 845, r2),
        "d": ("Tạm dừng / tiếp tục xử lý", 1120, r1),
        "d1": ("Ghi lý do tạm dừng", 1020, r2), "d2": ("Dời hạn SLA khi tiếp tục", 1220, r2),
    }
    rels = [("include", "root", "a"), ("include", "root", "b"), ("include", "root", "c"), ("extend", "d", "root"),
            ("include", "a", "a1"), ("include", "b", "b1"), ("include", "c", "c1"), ("extend", "c2", "c"),
            ("include", "d", "d1"), ("include", "d", "d2")]
    return uc_diagram("Hinh 3.4 Use case cap nhat xu ly", "Cập nhật xử lý sự cố", ucs, rels,
                      {"ktv": ("Kỹ thuật viên", 60, 40)}, [("ktv", "root")], (140, 1320, r2 + 75))


def uc_xac_nhan():
    # "Báo chưa khắc phục" là use case riêng (UC06b), không phải phần mở rộng của UC06: UC06 luôn đóng yêu cầu,
    # còn UC06b đưa yêu cầu về Đang xử lý.
    r0, r1, r2 = 75, 205, 335
    ucs = {
        "root": ("UC06 Xác nhận hoàn tất và đánh giá", 600, r0),
        "a": ("Xem kết quả và ảnh nghiệm thu", 420, r1),
        "b": ("Đóng yêu cầu, ghi lịch sử", 630, r1),
        "c": ("Chấm điểm 1–5 và nhận xét", 840, r1),
        "u6b": ("UC06b Báo chưa khắc phục", 260, 330), "d1": ("Ghi lý do chưa khắc phục", 260, 460),
    }
    rels = [("include", "root", "a"), ("include", "root", "b"), ("extend", "c", "root"), ("include", "u6b", "d1")]
    return uc_diagram("Hinh 3.5 Use case xac nhan hoan tat", "Xác nhận kết quả xử lý", ucs, rels,
                      {"cd": ("Cư dân", 60, 120)}, [("cd", "root"), ("cd", "u6b")], (140, 960, 520))


# ------------------------------------------------------------------ Hình 3.6 hoạt động luồng AI
def activity_ai():
    def act_wh(key, lane, y, name, w, h):
        n = act(key, lane, y, name)
        n.update(x=cx(lane) - w // 2, w=w, h=h)
        return n

    def merge(key, lane, y):
        return {"key": key, "kind": "merge", "x": cx(lane) - 16, "y": y, "w": 32, "h": 40}

    x0, x1, x2, x3 = cx(0), cx(1), cx(2), cx(3)
    nodes = lanes(["Cư dân", "Hệ thống web", "Dịch vụ phân loại AI", "Dịch vụ email"], 1440)
    nodes += [
        dot("i", 0, 45),
        act("a1", 0, 85, "Nhập tiêu đề, mô tả, nơi xảy ra (căn / khu vực chung), ảnh; gửi"),
        diamond("d1", 1, 95),
        diamond("d2", 1, 185),
        act("ai1", 2, 170, "Ollama phân loại: loại, ưu tiên, nguy hiểm, câu hỏi lại, nhóm xử lý"),
        diamond("d3", 2, 265),
        act_wh("ai2", 2, 335, "Trả về null (giữ lựa chọn của cư dân)", 170, 60),
        merge("m1", 1, 440),
        act_wh("s1", 1, 490, "Gộp với kết quả AI nếu có: loại AI chỉ dùng khi cư dân chọn Khác; chỉ nâng ưu tiên; "
                             "nguy hiểm → Khẩn cấp", 200, 80),
        act("s2", 1, 600, "Cấp số yêu cầu, tính hạn SLA, ghi lịch sử, lưu CSDL"),
        diamond("d4", 1, 690),
        act("e1", 3, 680, "Gửi thư quản lý: Cần hỏi lại cư dân"),
        diamond("d5", 1, 770),
        act("e2", 3, 760, "Gửi thư quản lý: Cần phân công (kèm nhóm gợi ý)"),
        act("s3", 1, 850, "Giao thợ ít việc nhất; Mới gửi → Đã tiếp nhận; ghi lịch sử"),
        act("e3", 3, 850, "Gửi thư giao việc cho thợ"),
        diamond("d6", 3, 935),
        act("e4", 3, 1000, "Gửi thư quản lý: Cần theo dõi"),
        merge("m2", 1, 1090),
        diamond("d7", 1, 1165),
        act("e5", 3, 1155, "Gửi thư quản trị: mô hình AI không phản hồi"),
        merge("m3", 1, 1245),
        act("a2", 0, 1300, "Xem chi tiết yêu cầu (kèm câu hỏi lại nếu có)"),
        dot("f", 0, 1395, "final"),
    ]
    half = 190 // 2
    bus = x3 + half + 12  # đường gom các nhánh gửi thư về nút m2
    edges = [
        flow("a1", "d1"),
        flow("d1", "d2", "hợp lệ"),
        flow("d2", "ai1", "AI được bật"),
        flow("d2", "m1", "AI tắt"),
        flow("ai1", "d3"),
        flow("d3", "ai2", "lỗi / hết thời gian chờ"),
        # Hai nhánh từ dịch vụ AI dùng chung một đường vào cạnh phải của m1.
        flow("d3", "m1", "JSON hợp lệ", points=[[x2 - 16, 285], [x1 + 65, 285], [x1 + 65, 460], [x1 + 16, 460]]),
        flow("ai2", "m1", points=[[x2, 395], [x2, 460], [x1 + 16, 460]]),
        flow("m1", "s1"), flow("s1", "s2"), flow("s2", "d4"),
        flow("d4", "e1", "AI hỏi lại: mô tả mơ hồ"), flow("d4", "d5", "mô tả rõ"),
        flow("d5", "e2", "không có thợ phù hợp"), flow("d5", "s3", "có thợ phù hợp"),
        flow("s3", "e3"), flow("e3", "d6"),
        flow("d6", "e4", "Khẩn cấp"),
        # Đi sang làn AI (trống ở đoạn này) rồi xuống gộp vào đường chung, không cắt hộp "Cần theo dõi".
        flow("d6", "m2", "mức khác", points=[[x3 - 16, 955], [x2, 955], [x2, 1110], [x1 + 16, 1110]]),
        flow("e1", "m2", points=[[x3 + half, 710], [bus, 710], [bus, 1110], [x1 + 16, 1110]]),
        flow("e2", "m2", points=[[x3 + half, 790], [bus, 790], [bus, 1110], [x1 + 16, 1110]]),
        flow("e4", "m2", points=[[x3, 1060], [x3, 1110], [x1 + 16, 1110]]),
        flow("m2", "d7"),
        flow("d7", "e5", "AI không phản hồi, chưa báo trong 30 phút"), flow("d7", "m3", "ngược lại"),
        flow("e5", "m3", points=[[x3, 1215], [x3, 1265], [x1 + 16, 1265]]),
        flow("m3", "a2", points=[[x1 - 16, 1265], [x0, 1265], [x0, 1300]]),
        flow("a2", "f"),
    ]
    # Chừa chỗ cho nút gộp m0 trước a1 (a1 nhận hai luồng: bắt đầu và nhập lại khi chưa hợp lệ):
    # mọi thứ dưới nút bắt đầu dời xuống 50px.
    shift = 50
    for n in nodes:
        if n["kind"] == "partition":
            n["h"] += shift
        elif n["key"] != "i":
            n["y"] += shift
    for e in edges:
        for p in e.get("points", []):
            p[1] += shift
    nodes.append(merge("m0", 0, 80))
    edges += [flow("i", "m0"), flow("m0", "a1"),
              flow("d1", "m0", "chưa hợp lệ", points=[[x1, 145], [x1, 100], [x0 + 16, 100]])]
    return {"kind": "activity", "model": "Hoạt động gửi yêu cầu – AI – điều phối",
            "owner": "Gửi yêu cầu sự cố, AI phân loại và điều phối", "name": "Hinh 3.6 Hoat dong gui yeu cau AI dieu phoi",
            "title": "Gửi yêu cầu sự cố, AI phân loại và điều phối", "fontSize": 13, "nodes": nodes, "edges": edges}


# ------------------------------------------------------------------ Hình 3.7 sơ đồ lớp đầy đủ
# Lớp nghiệp vụ đặt tên theo bảng, thuộc tính theo cột của CSDL (khớp ERD/RD ở Chương 2); thao tác là nghiệp vụ,
# ánh xạ sang hàm cài đặt ở kiem_tra_uml.OPS_MAP. Bảng nối NguoiDung_VaiTro, VaiTro_Quyen thành liên kết nhiều–nhiều.
def P(name, typ):
    return A(name, typ, vis="private")


def V(name, params=(), ret=None):
    return O(name, params, ret)


FULL = {
    "KhuVuc": ("class", [P("ten", "string"), P("slug", "string"), P("urlAnh", "string")], [V("dsDuAn")]),
    "DuAn": ("class", [P("ten", "string"), P("slug", "string"), P("diaChi", "string"), P("moTaNgan", "string")],
             [V("taoMoi"), V("capNhat")]),
    "ToaNha": ("class", [P("ma", "string"), P("ten", "string"), P("diaChi", "string"), P("soLuongTang", "int"),
                         P("trangThai", "int")], [V("taoMoi"), V("capNhat")]),
    "Tang": ("class", [P("soTang", "int"), P("nhanTang", "string")], [V("dsTangCuaToa")]),
    "CanHo": ("class", [P("maCanHo", "string"), P("dienTich", "double"), P("soPhongNgu", "int"),
                        P("tinhTrangThue", "int")], [V("taoMoi"), V("doiTinhTrang"), V("layCuDanDaiDien", ret="CuDan")]),
    "CuDan": ("class", [P("hoTen", "string"), P("soDienThoai", "string"), P("vaiTro", "int"), P("ngayVaoO", "datetime"),
                        P("ngayChuyenDi", "datetime")],
              [V("themVaoCan"), V("chuyenDi"), V("guiLaiThuMoi"), V("dangO", ret="bool")]),
    "NguoiDung": ("class", [P("userName", "string"), P("email", "string"), P("hoTen", "string"),
                            P("soDienThoai", "string")],
                  [V("dangNhap"), V("kichHoatQuaThuMoi"), V("khoaTaiKhoan"), V("xemViecDuocGiao")]),
    "VaiTro": ("class", [P("name", "string")], [V("luuPhanQuyen")]),
    "Quyen": ("class", [P("ma", "string"), P("module", "string"), P("tenHienThi", "string")], [V("dsVaiTro")]),
    "YeuCauBaoTri": ("class", [
        P("soYeuCau", "string"), P("tieuDe", "string"), P("moTa", "string"), P("viTri", "string"),
        P("loaiBaoTri", "LoaiBaoTri"), P("mucDoUuTien", "MucDoUuTien"), P("trangThai", "TrangThaiSuCo"),
        P("ngayBaoCao", "datetime"), P("hanXuLy", "datetime"), P("danhGia", "int")], [
        V("guiYeuCau"), V("phanLoaiBangAI"), V("tinhHanXuLy"), V("phanLoaiLai"), V("giaoViec", ["kyThuatVien"]),
        V("chuyenTrangThai", ["trangThaiMoi", "ghiChu"]), V("hoanTat", ["ketQuaXuLy", "anhNghiemThu"]),
        V("xacNhanVaDanhGia", ["diem", "nhanXet"]), V("quaHan", ret="bool")]),
    "MucSla": ("class", [P("mucDoUuTien", "MucDoUuTien"), P("phutPhanHoi", "int"), P("phutHoanTat", "int")],
               [V("tinhHan", ["ngayBaoCao"], "datetime")]),
    "AnhSuCo": ("class", [P("duongDan", "string"), P("giaiDoan", "int"), P("ngayTaiLen", "datetime")],
                [V("tachDanhSachAnh", ["urls"])]),
    "LichSuTrangThaiSuCo": ("class", [P("tuTrangThai", "TrangThaiSuCo"), P("sangTrangThai", "TrangThaiSuCo"),
                                      P("thoiDiem", "datetime"), P("ghiChu", "string")], [V("ghiLichSu")]),
    "NhatKyHeThong": ("class", [P("tenBang", "string"), P("hanhDong", "int"), P("thoiGian", "datetime"),
                                P("giaTriCu", "string"), P("giaTriMoi", "string")], [V("ghiNhatKy"), V("xemNhatKy")]),
    "TrangThaiSuCo": ("enum", ["MoiGui", "DaTiepNhan", "DangXuLy", "DaXuLy", "DaDong", "TuChoi", "TamDung", "DaHuy"],
                      []),
    "MucDoUuTien": ("enum", ["Thap", "TrungBinh", "Cao", "KhanCap"], []),
    "LoaiBaoTri": ("enum", ["Nuoc", "Dien", "ThietBi", "KetCau", "VeSinh", "ConTrung", "Mang", "Khac"], []),
}
FULL_DOC = "Hinh 3.7 So do lop"


def class_full():
    nodes = []
    for k, (kind, attrs, ops) in FULL.items():
        if kind == "enum":
            nodes.append(N(k, "enum", k, 0, 0, literals=attrs))
        else:
            nodes.append(N(k, kind, k, 0, 0, attrs=attrs, ops=ops))
    b = Boxes(FULL_DOC, nodes)
    kv, da, tn, tg, ch, cd = "KhuVuc", "DuAn", "ToaNha", "Tang", "CanHo", "CuDan"
    nd, vt, qu, yc, sla, anh, ls, nk = ("NguoiDung", "VaiTro", "Quyen", "YeuCauBaoTri", "MucSla", "AnhSuCo",
                                        "LichSuTrangThaiSuCo", "NhatKyHeThong")
    top = 40
    # Cột 0: khu vực → toà → căn → cư dân; cột 1: dự án, tầng; cột 2: người dùng, yêu cầu, mức SLA;
    # cột 3: ảnh, lịch sử; cột 4: vai trò, quyền, nhật ký. Đường nối chạy trong các khe giữa cột.
    x0 = 50
    b.put(kv, x0, top)
    b.put(tn, x0, b.B(kv) + 80)
    w0 = max(b.w(k) for k in (kv, tn, ch, cd))
    x1 = x0 + w0 + 110
    b.put(da, x1, top)
    b.put(tg, x1, b.B(tn) + 25)
    b.put(ch, x0, max(b.B(tn), b.B(tg)) + 60)
    b.put(cd, x0, b.B(ch) + 80)
    x2 = x1 + max(b.w(da), b.w(tg)) + 120
    w2 = max(b.w(nd), b.w(yc))
    b.put(nd, x2 + (w2 - b.w(nd)) // 2, top)
    b.put(yc, x2 + (w2 - b.w(yc)) // 2, b.B(nd) + 80)
    b.put(sla, b.CX(yc) - b.w(sla) // 2, b.B(yc) + 70)
    x3 = x2 + w2 + 120
    b.put(anh, x3, b.T(yc) + 15)
    b.put(ls, x3, b.B(anh) + 50)
    x4 = x3 + max(b.w(anh), b.w(ls)) + 120
    b.put(vt, x4, top)
    b.put(qu, x4, b.B(vt) + 80)
    b.put(nk, x4, b.B(qu) + 80)
    ex, ey = x3, max(b.B(ls), b.B(nk)) + 70     # ba enum ở góc phải dưới, cạnh mức SLA
    for e in ("TrangThaiSuCo", "MucDoUuTien", "LoaiBaoTri"):
        b.put(e, ex, ey)
        ex = b.R(e) + 40

    def asc(a, c, m1, m2, pts, name=None, kind=None):
        e = E("assoc", a, c, mult1=m1, mult2=m2, points=pts)
        if name:
            e["name"] = name   # tên quan hệ = cột khoá ngoại bỏ "Id"
        if kind:
            e["agg1"] = kind
        return e

    v0 = x0 + 40                       # trục dọc cột 0
    xq = b.L(nd) - 40                  # khe dọc bên trái người dùng
    xr = max(b.R(anh), b.R(ls)) + 35   # khe dọc bên phải ảnh / lịch sử
    t = b.T(tn)
    edges = [
        asc(kv, da, "1", "0..*", [[b.R(kv), b.T(kv) + 30], [b.L(da), b.T(kv) + 30]]),
        asc(kv, tn, "1", "0..*", [[v0, b.B(kv)], [v0, b.T(tn)]]),
        asc(da, tn, "0..1", "0..*", [[b.L(da) + 40, b.B(da)], [b.L(da) + 40, t + 12], [b.R(tn), t + 12]]),
        asc(nd, tn, "0..1", "0..*", [[b.L(nd), b.B(nd) - 20], [xq, b.B(nd) - 20], [xq, t + 40], [b.R(tn), t + 40]],
            name="quanLy"),
        asc(tn, yc, "0..1", "0..*", [[b.R(tn), t + 95], [b.L(yc), t + 95]], name="toaNhaKhuVucChung"),
        asc(tn, tg, "1", "1..*", [[b.R(tn) - 30, b.B(tn)], [b.R(tn) - 30, b.CY(tg)], [b.L(tg), b.CY(tg)]],
            kind="composite"),
        asc(tg, ch, "1", "0..*", [[b.L(tg) + 40, b.B(tg)], [b.L(tg) + 40, b.T(ch) - 25], [b.R(ch) - 30, b.T(ch) - 25],
                                  [b.R(ch) - 30, b.T(ch)]]),
        asc(tn, ch, "1", "0..*", [[v0, b.B(tn)], [v0, b.T(ch)]]),
        asc(ch, yc, "0..1", "0..*", [[b.R(ch), b.T(ch) + 25], [b.L(yc), b.T(ch) + 25]]),
        asc(ch, cd, "1", "0..*", [[v0, b.B(ch)], [v0, b.T(cd)]], kind="composite"),
        asc(nd, cd, "0..1", "0..*", [[b.L(nd) + 20, b.T(nd)], [b.L(nd) + 20, 12], [14, 12], [14, b.CY(cd)],
                                     [b.L(cd), b.CY(cd)]]),
        asc(nd, yc, "0..1", "0..*", [[b.CX(nd) - 50, b.B(nd)], [b.CX(nd) - 50, b.T(yc)]], name="nguoiBaoCao"),
        asc(nd, yc, "0..1", "0..*", [[b.CX(nd) + 50, b.B(nd)], [b.CX(nd) + 50, b.T(yc)]], name="nguoiPhuTrach"),
        asc(sla, yc, "1", "0..*", [[b.CX(sla), b.T(sla)], [b.CX(sla), b.B(yc)]]),
        asc(yc, anh, "1", "0..*", [[b.R(yc), b.CY(anh)], [b.L(anh), b.CY(anh)]], kind="composite"),
        asc(yc, ls, "1", "0..*", [[b.R(yc), b.CY(ls)], [b.L(ls), b.CY(ls)]], kind="composite"),
        asc(nd, anh, "0..1", "0..*", [[b.R(nd), b.B(nd) - 15], [b.CX(anh), b.B(nd) - 15], [b.CX(anh), b.T(anh)]],
            name="nguoiTaiLen"),
        asc(nd, ls, "0..1", "0..*", [[b.R(nd), b.B(nd) - 45], [xr, b.B(nd) - 45], [xr, b.CY(ls)], [b.R(ls), b.CY(ls)]],
            name="nguoiThucHien"),
        asc(nd, vt, "0..*", "0..*", [[b.R(nd), b.T(nd) + 25], [b.L(vt), b.T(nd) + 25]]),
        asc(vt, qu, "0..*", "0..*", [[b.CX(vt), b.B(vt)], [b.CX(vt), b.T(qu)]]),
    ]
    for n in nodes:
        n["w"], n["h"] = b.wh[n["key"]]
        n["autosize"] = True
    return {"kind": "class", "model": "Sơ đồ lớp", "name": FULL_DOC, "title": "Sơ đồ lớp của hệ thống",
            "fontSize": 12, "nodes": nodes, "edges": edges}


# ------------------------------------------------------------------ Hình 3.8 – 3.10 tuần tự
class Seq:
    """Xếp thông điệp từ trên xuống theo khoảng trống tối thiểu để nhãn không chồng nhau,
    và tính khung alt/opt (kể cả đường phân cách nhánh) theo thông điệp nằm trong."""

    TOP = 125

    def __init__(self, lifelines):
        self.keys = [k for k, _ in lifelines]
        self.nodes, self.X, x = [], {}, 20
        for k, name in lifelines:
            w = max(120, round(FONT_B.getlength(name)) + 24)
            self.nodes.append(N(k, "lifeline", name, x, 40, w, 0))
            self.X[k] = x + w // 2
            x += w + 30
        self.right = x - 30
        self.edges, self.bottom, self.stack, self.frags = [], self.TOP - 30, [], []

    def m(self, a, c, name, sort="synchCall"):
        if a == c:
            y, low = self.bottom + 24, 44          # thông điệp tự gọi: nhãn trên, vòng lặp và thanh kích hoạt ~50px
        elif sort == "reply":
            y, low = self.bottom + 8, 19           # trả về: nhãn nằm dưới mũi tên
        else:
            y, low = self.bottom + 21, 5           # gọi: nhãn nằm trên mũi tên
        self.edges.append(E("message", a, c, name=name, y=y, sort=sort))
        self.bottom = y + low

    def begin(self, op, guard, a, c):
        y = self.bottom + 12
        # Khung lồng nhau thụt vào 15px mỗi cấp; không vượt khung "sd" của sơ đồ.
        inset = 15 * len(self.stack)
        lo, hi = 25 + inset, self.right - inset
        if self.stack:
            lo, hi = max(lo, self.stack[-1]["x1"] + 15), min(hi, self.stack[-1]["x2"] - 15)
        self.stack.append({"op": op, "guards": [guard], "y": y, "x1": max(lo, self.X[a] - 95),
                           "x2": min(hi, self.X[c] + 95), "splits": []})
        self.bottom = y + 50                       # nhãn toán tử + điều kiện nhánh đầu (~47px)

    def other(self, guard):
        f = self.stack[-1]
        sep = self.bottom + 10
        f["splits"].append(sep)
        f["guards"].append(guard)
        self.bottom = sep + 32

    def end(self):
        f = self.stack.pop()
        h = self.bottom + 12 - f["y"]
        n = N(f"f{len(self.frags)}", "fragment", None, f["x1"], f["y"], f["x2"] - f["x1"], h,
              operator=f["op"], guards=f["guards"])
        if f["splits"]:
            n["splits"] = f["splits"]
        self.frags.append(n)
        self.bottom = f["y"] + h + 4

    def diagram(self, name, title):
        length = self.bottom + 30 - 40
        for n in self.nodes:
            n["h"] = length
        return {"kind": "sequence", "model": title, "owner": title, "name": name, "title": title, "fontSize": 12,
                "frame": {"left": 5, "top": 5, "width": self.right + 15, "height": self.bottom + 45},
                "nodes": self.nodes + self.frags, "edges": self.edges}


def seq_report():
    s = Seq([("cd", "Cư dân"), ("ctl", ":Tenant.MaintenanceController"), ("tri", ":MaintenanceTriageService"),
             ("oll", "Ollama: qwen3-suco4"), ("wf", ":MaintenanceWorkflow"), ("db", ":AppDbContext"),
             ("dis", ":MaintenanceDispatcher")])
    s.m("cd", "ctl", "Create(input)")
    s.m("ctl", "ctl", "GetMyApartmentAsync()")
    s.begin("opt", "chưa gắn căn hộ hoặc dữ liệu chưa hợp lệ", "cd", "ctl")
    s.m("ctl", "cd", "trả lại biểu mẫu kèm thông báo lỗi", "reply")
    s.end()
    s.m("ctl", "tri", "TriageAsync(description)")
    s.begin("opt", "IsEnabled (Ollama:BaseUrl có giá trị)", "tri", "oll")
    s.m("tri", "oll", "POST /api/chat (mô tả, temperature 0)")
    s.m("oll", "tri", "JSON phân loại", "reply")
    s.m("tri", "tri", "Parse(raw)")
    s.end()
    s.m("tri", "ctl", "MaintenanceTriage hoặc null", "reply")
    s.m("ctl", "tri", "Apply(tenantCategory, tenantPriority, triage)")
    s.m("tri", "ctl", "(loại, ưu tiên)", "reply")
    s.m("ctl", "wf", "PhotosFrom(urls, Report, userId)")
    s.m("wf", "ctl", "ảnh hiện trạng", "reply")
    s.m("ctl", "wf", "InitializeNewAsync(r, userId)")
    s.m("wf", "wf", "RecalculateDueAsync(r)")
    s.m("wf", "db", "SlaPolicies.FindAsync(priority)")
    s.m("db", "wf", "SlaPolicy hoặc null", "reply")
    s.m("wf", "ctl", "DueAt, lịch sử \"Tạo yêu cầu\"", "reply")
    s.m("ctl", "db", "SaveChangesAsync()")
    s.m("ctl", "dis", "DispatchAsync(req, triage, aiUnavailable)")
    s.m("dis", "ctl", "xong (lỗi email chỉ ghi log)", "reply")
    s.m("ctl", "cd", "chuyển tới Details (kèm câu hỏi lại nếu có)", "reply")
    return s.diagram("Hinh 3.8 Tuan tu gui yeu cau AI phan loai", "Gửi yêu cầu sự cố và AI phân loại")


def seq_dispatch():
    s = Seq([("ctl", ":Tenant.MaintenanceController"), ("dis", ":MaintenanceDispatcher"), ("wf", ":MaintenanceWorkflow"),
             ("db", ":AppDbContext"), ("mail", ":IEmailSender")])
    s.m("ctl", "dis", "DispatchAsync(req, triage, aiUnavailable)")
    s.begin("alt", "mô tả rõ, loại có thợ chuyên trách và có thợ", "dis", "mail")
    s.m("dis", "dis", "RoleFor(category, description)")
    s.m("dis", "dis", "LeastBusyAsync(role)")
    s.m("dis", "db", "đếm việc đang mở của từng thợ")
    s.m("db", "dis", "số việc theo thợ", "reply")
    s.m("dis", "wf", "AssignAsync(r, assignee, System, null, AutoAssignNote)")
    s.m("wf", "wf", "TransitionAsync(r, Acknowledged, System, ...)")
    s.m("wf", "db", "SaveChangesAsync()")
    s.m("wf", "dis", "null (giao thành công)", "reply")
    s.m("dis", "mail", "SendAsync(thợ, \"Giao việc ...\")")
    s.begin("opt", "Priority = Urgent", "dis", "mail")
    s.m("dis", "mail", "SendAsync(quản lý, \"Cần theo dõi ...\")")
    s.end()
    s.other("mô tả mơ hồ hoặc không có thợ phù hợp")
    s.m("dis", "mail", "SendAsync(quản lý, \"Cần hỏi lại cư dân\" / \"Cần phân công\")")
    s.end()
    s.begin("opt", "aiUnavailable và đã quá 30 phút từ thư trước", "dis", "mail")
    s.m("dis", "mail", "SendAsync(quản trị, \"Mô hình ... không phản hồi\")")
    s.end()
    s.m("dis", "ctl", "xong (lỗi email chỉ ghi log)", "reply")
    return s.diagram("Hinh 3.9 Tuan tu dieu phoi sau khi luu", "Điều phối sau khi lưu yêu cầu")


def seq_resolve():
    s = Seq([("ktv", "Kỹ thuật viên"), ("ctl", ":JobsController"), ("wf", ":MaintenanceWorkflow"),
             ("db", ":AppDbContext")])
    s.m("ktv", "ctl", "Resolve(id, resolutionNote, photoUrls)")
    s.m("ctl", "ctl", "MyJobs()")
    s.m("ctl", "db", "MaintenanceRequests.Where(AssignedToId = Me).FirstOrDefaultAsync(id)")
    s.m("db", "ctl", "yêu cầu hoặc null", "reply")
    s.begin("opt", "null: không phải việc của mình", "ktv", "ctl")
    s.m("ctl", "ktv", "404 Not Found", "reply")
    s.end()
    s.m("ctl", "wf", "PhotosFrom(urls, Result, userId)")
    s.m("wf", "ctl", "ảnh nghiệm thu", "reply")
    s.m("ctl", "wf", "TransitionAsync(r, Resolved, Technician, userId, resolutionNote)")
    s.m("wf", "wf", "Check(r, Resolved, Technician, ...)")
    s.begin("alt", "vi phạm quy tắc", "ctl", "db")
    s.m("wf", "ctl", "thông báo lỗi", "reply")
    s.other("hợp lệ")
    s.m("wf", "wf", "ResolvedAt, ResolutionNote, lịch sử")
    s.m("wf", "db", "SaveChangesAsync()")
    s.m("wf", "ctl", "null (thành công)", "reply")
    s.end()
    s.m("ctl", "ktv", "chuyển tới Details (kèm thông báo)", "reply")
    return s.diagram("Hinh 3.10 Tuan tu ky thuat vien hoan tat", "Kỹ thuật viên hoàn tất xử lý")


# ------------------------------------------------------------------ Phân hệ thu phí (bổ sung tuần 6)
def uc_thanh_toan():
    """UC20: quản lý ghi nhận, duyệt, từ chối thanh toán và hoàn tiền."""
    r0, r1, r2 = 75, 205, 335
    xs = [250, 460, 670, 880, 1090]
    ucs = {
        "root": ("UC20 Ghi nhận, duyệt thanh toán, hoàn tiền", 670, r0),
        "f": ("Xem hoá đơn và thanh toán chờ duyệt", xs[0], r1),
        "a": ("Ghi nhận thanh toán trực tiếp", xs[1], r1),
        "b": ("Duyệt xác nhận của cư dân", xs[2], r1),
        "k": ("Kiểm tra không vượt số còn lại", (xs[1] + xs[2]) // 2, r2),
        "c": ("Từ chối xác nhận", xs[3], r1),
        "d": ("Hoàn tiền", xs[4], r1), "d1": ("Kiểm tra không vượt số đã thu", xs[4], r2),
    }
    rels = [("include", "root", "f"), ("extend", "a", "root"), ("extend", "b", "root"), ("extend", "c", "root"),
            ("extend", "d", "root"), ("include", "a", "k"), ("include", "b", "k"), ("include", "d", "d1")]
    return uc_diagram("Hinh 3.5b Use case ghi nhan duyet thanh toan", "Ghi nhận, duyệt thanh toán, hoàn tiền", ucs,
                      rels, {"ql": ("Quản lý BQL", 60, 40)}, [("ql", "root")], (140, 1240, r2 + 75))


FEE = {
    "ToaNha": ("class", [P("donGiaPhiQuanLy", "decimal"), P("ngayChotKyMacDinh", "int"),
                         P("soNgayTreHanMacDinh", "int"), P("phanTramPhiTreMacDinh", "decimal")], [V("capNhat")]),
    "CanHo": ("class", [P("maCanHo", "string"), P("dienTich", "double"), P("tinhTrangThue", "int")], []),
    "PhiDinhKy": ("class", [P("moTa", "string"), P("soTien", "decimal"), P("dangApDung", "bool")],
                  [V("themKhoan"), V("batTat")]),
    "LoaiDichVu": ("class", [P("ma", "string"), P("ten", "string"), P("donViTinh", "string"), P("cheDoTinhPhi", "int"),
                             P("donGiaMacDinh", "decimal")], [V("capNhatDonGia")]),
    "ChiSoDichVu": ("class", [P("thangTinhPhi", "int"), P("chiSoCu", "decimal"), P("chiSoMoi", "decimal"),
                              P("tieuThu", "decimal"), P("donGia", "decimal"), P("thanhTien", "decimal"),
                              P("daXuatHoaDon", "bool")], [V("ghiChiSo"), V("nhapTheoToa"), V("xoa")]),
    "HoaDon": ("class", [
        P("soHoaDon", "string"), P("thangTinhPhi", "int"), P("ngayPhatHanh", "datetime"), P("ngayDenHan", "datetime"),
        P("tamTinh", "decimal"), P("phiTreHan", "decimal"), P("tongTien", "decimal"), P("daThanhToan", "decimal"),
        P("conLai", "decimal"), P("trangThai", "TrangThaiHoaDon")], [
        V("lapHoaDonThang", ["canHo", "ky"]), V("lapHoaDonPhatSinh", ["canHo", "cacDong"]), V("suaDong"),
        V("tinhLaiTong"), V("danhDauQuaHan"), V("congPhiTre"), V("capNhatCongNo", ["soTien"]), V("huy", ["lyDo"])]),
    "ChiTietHoaDon": ("class", [P("moTa", "string"), P("soLuong", "decimal"), P("donGia", "decimal"),
                                P("thanhTien", "decimal"), P("thuTu", "int")], []),
    "ThanhToan": ("class", [
        P("soThanhToan", "string"), P("soTien", "decimal"), P("phuongThuc", "int"),
        P("trangThai", "TrangThaiThanhToan"), P("maGiaoDich", "string"), P("ngayThanhToan", "datetime"),
        P("soTienHoanTra", "decimal")], [
        V("guiXacNhan", ["soTien", "maGiaoDich"]), V("ghiNhanTrucTiep", ["soTien", "phuongThuc"]), V("duyet"),
        V("tuChoi", ["lyDo"]), V("hoanTien", ["soTien"])]),
    "TrangThaiHoaDon": ("enum", ["BanNhap", "DaPhatHanh", "TraMotPhan", "DaThanhToan", "QuaHan", "DaHuy", "DaHoanTien"],
                        []),
    "TrangThaiThanhToan": ("enum", ["ChoXacNhan", "ThanhCong", "ThatBai", "DaHoan", "HoanMotPhan", "DaHuy"], []),
}
FEE_DOC = "Hinh 3.7b So do lop thu phi"


def class_fee():
    nodes = []
    for k, (kind, attrs, ops) in FEE.items():
        nodes.append(N(k, "enum", k, 0, 0, literals=attrs) if kind == "enum"
                     else N(k, kind, k, 0, 0, attrs=attrs, ops=ops))
    b = Boxes(FEE_DOC, nodes)
    tn, ch, pdk, ldv, cs, hd, ct, tt = ("ToaNha", "CanHo", "PhiDinhKy", "LoaiDichVu", "ChiSoDichVu", "HoaDon",
                                        "ChiTietHoaDon", "ThanhToan")
    top, x0 = 40, 50
    # Cột 0: toà → căn → phí định kỳ; cột 1: chỉ số, hoá đơn; cột 2: loại dịch vụ, chi tiết, thanh toán; cột 3: enum.
    b.put(tn, x0, top)
    b.put(ch, x0, b.B(tn) + 70)
    b.put(pdk, x0, b.B(ch) + 70)
    x1 = x0 + max(b.w(k) for k in (tn, ch, pdk)) + 120
    b.put(cs, x1, top)
    b.put(hd, x1, max(b.B(cs) + 60, b.T(ch) - 20))
    x2 = x1 + max(b.w(cs), b.w(hd)) + 110
    b.put(ldv, x2, top)
    b.put(ct, x2, b.T(hd))
    b.put(tt, x2, b.B(ct) + 50)
    x3 = x2 + max(b.w(k) for k in (ldv, ct, tt)) + 70
    b.put("TrangThaiHoaDon", x3, b.T(hd))
    b.put("TrangThaiThanhToan", x3, b.B("TrangThaiHoaDon") + 40)

    def asc(a, c, m1, m2, pts, kind=None):
        e = E("assoc", a, c, mult1=m1, mult2=m2, points=pts)
        if kind:
            e["agg1"] = kind
        return e

    v0 = x0 + 40
    xa, xb = x1 - 80, x1 - 45                              # hai trục dọc trong khe giữa cột 0 và cột 1
    y_up, y_dn = b.T(ch) + 15, b.B(ch) - 15                # ra từ cạnh phải căn hộ: lên chỉ số, xuống hoá đơn
    y_tt = (max(b.T(tt), b.T(hd)) + min(b.B(tt), b.B(hd))) // 2
    edges = [
        asc(tn, ch, "1", "0..*", [[v0, b.B(tn)], [v0, b.T(ch)]]),
        asc(ch, pdk, "1", "0..*", [[v0, b.B(ch)], [v0, b.T(pdk)]], kind="composite"),
        asc(ch, cs, "1", "0..*", [[b.R(ch), y_up], [xa, y_up], [xa, b.T(cs) + 30], [b.L(cs), b.T(cs) + 30]],
            kind="composite"),
        asc(ch, hd, "1", "0..*", [[b.R(ch), y_dn], [xb, y_dn], [xb, b.T(hd) + 30], [b.L(hd), b.T(hd) + 30]]),
        asc(ldv, cs, "1", "0..*", [[b.L(ldv), b.T(cs) + 30], [b.R(cs), b.T(cs) + 30]]),
        asc(hd, ct, "1", "1..*", [[b.R(hd), b.T(hd) + 30], [b.L(ct), b.T(hd) + 30]], kind="composite"),
        asc(hd, tt, "1", "0..*", [[b.R(hd), y_tt], [b.L(tt), y_tt]]),
    ]
    for n in nodes:
        n["w"], n["h"] = b.wh[n["key"]]
        n["autosize"] = True
    return {"kind": "class", "model": "Sơ đồ lớp thu phí", "name": FEE_DOC, "title": "Sơ đồ lớp phân hệ thu phí",
            "fontSize": 12, "nodes": nodes, "edges": edges}


def seq_thanh_toan():
    # Quản lý đứng giữa để mọi lời gọi đi từ trái sang phải (nhãn lời gọi dài không đè lên nhãn trả về).
    s = Seq([("cd", "Cư dân"), ("tc", ":Tenant.InvoicesController"), ("ql", "Quản lý BQL"),
             ("ac", ":Admin.InvoicesController"), ("db", ":AppDbContext")])
    s.m("cd", "tc", "SubmitPayment(input)")
    s.m("tc", "tc", "MyApartmentIds()")
    s.m("tc", "db", "Invoices.Include(Payments).FirstOrDefaultAsync(id)")
    s.m("db", "tc", "hoá đơn của căn mình hoặc null", "reply")
    s.begin("alt", "hoá đơn không nhận tiền, số tiền ≤ 0 hoặc tổng chờ duyệt vượt ConLai", "cd", "db")
    s.m("tc", "cd", "thông báo lỗi, không lưu", "reply")
    s.other("hợp lệ")
    s.m("tc", "db", "Payments.Add(payment: Chờ xác nhận)")
    s.m("tc", "db", "SaveChangesAsync()")
    s.m("tc", "cd", "\"Đã gửi, Ban quản lý sẽ xác nhận\"", "reply")
    s.end()
    s.m("ql", "ac", "PendingPayments()")
    s.m("ac", "db", "Payments.Where(Status = Chờ xác nhận)")
    s.m("db", "ac", "danh sách chờ duyệt", "reply")
    s.m("ql", "ac", "ApprovePayment(paymentId)")
    s.m("ac", "db", "Payments.Include(Invoice).FirstOrDefaultAsync(paymentId)")
    s.m("db", "ac", "thanh toán và hoá đơn", "reply")
    s.begin("alt", "không còn chờ, hoá đơn đã huỷ / hoàn, hoặc SoTien > ConLai", "ql", "db")
    s.m("ac", "ql", "thông báo lỗi, không lưu", "reply")
    s.other("hợp lệ")
    s.m("ac", "ac", "Status = Thành công, PaidAt")
    s.m("ac", "ac", "ApplyPaymentSuccess(inv, amount)")
    s.m("ac", "db", "SaveChangesAsync()")
    s.m("ac", "ql", "\"Đã duyệt\", trạng thái hoá đơn mới", "reply")
    s.end()
    return s.diagram("Hinh 3.10b Tuan tu duyet thanh toan", "Cư dân gửi xác nhận chuyển khoản, quản lý duyệt")


def diagrams():
    return [deployment(), uc_gui_yeu_cau(), uc_tiep_nhan(), uc_cap_nhat_xu_ly(), uc_xac_nhan(), uc_thanh_toan(),
            activity_ai(), class_full(), class_fee(), seq_report(), seq_dispatch(), seq_resolve(), seq_thanh_toan()]
