"""Kiểm tra mô hình StarUML (BaoCao_DACN.mdj) nhất quán với mã nguồn t/ và giữa các sơ đồ với nhau.

Chạy: python kiem_tra_uml.py      (thoát mã 1 nếu có lỗi; kết quả ghi thêm vào ../kiem_tra_uml.txt)

Nhóm kiểm tra:
  1. Sơ đồ lớp (mức phân tích, tên tiếng Việt): lớp mang tên bảng, thuộc tính là cột của bảng; mỗi thao tác nghiệp vụ
     ánh xạ (OPS_MAP) tới hàm cài đặt có thật; enum trạng thái khớp thứ tự trong mã và nhãn ở từ điển dữ liệu; mỗi liên
     kết ứng với một khoá ngoại (vai trò = tên cột khoá ngoại, bội số, hợp thành ↔ xoá dây chuyền); mọi khoá ngoại giữa
     các lớp có trên sơ đồ đều được vẽ.
  2. Sơ đồ tuần tự: đường đời là lớp có trong mã; thông điệp gọi phương thức có thật ở lớp nhận.
  3. Sơ đồ trạng thái: tập chuyển trạng thái đúng bằng MaintenanceWorkflow.Rules, vai trò ghi trên nhãn đúng,
     nhãn "[có lý do]" đúng bằng NoteRequired; trigger SQL trong báo cáo dùng đúng tập chuyển đó.
  4. ERD / lược đồ quan hệ: bảng, cột, PK, FK, NULL có trong snapshot; mỗi quan hệ ứng với một khoá ngoại
     (hoặc bảng nối nhiều – nhiều), bản số đầu bảng cha khớp khoá ngoại bắt buộc / cho phép NULL;
     từ điển dữ liệu mục 2.6: NULL, bảng đích và hành vi xoá của khoá ngoại.
  5. Sơ đồ hoạt động: một nút đầu, mọi nút đi tới được từ nút đầu và tới được nút kết thúc; nút quyết định
     có 1 vào, ≥ 2 ra, mọi nhánh có điều kiện; nút gộp ≥ 2 vào, 1 ra; hành động đúng 1 ra.
  6. Use case: mã UC chi tiết (UC11a…) có use case gốc trên sơ đồ tổng quát.
"""
import json
import re
import sys
from collections import defaultdict
from pathlib import Path

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
SRC = ROOT / "t"
MDJ = HERE.parent / "staruml" / "BaoCao_DACN.mdj"
SNAPSHOT = SRC / "Migrations" / "AppDbContextModelSnapshot.cs"
REPORT = HERE / "build_report.py"
OUT = HERE.parent / "kiem_tra_uml.txt"

KEYWORDS = {"if", "for", "foreach", "while", "switch", "catch", "using", "lock", "return", "new", "nameof", "typeof",
            "static", "public", "private", "protected", "internal", "async", "readonly", "override", "virtual",
            "abstract", "sealed", "partial", "const", "await", "base", "this", "default", "sizeof", "when"}
# Thành viên kế thừa từ lớp ngoài thư viện (ASP.NET Identity).
EXTERNAL = {"IdentityUser": {"Id", "UserName", "Email", "PhoneNumber", "PasswordHash"}}

errors, notes = [], []


def err(group, msg):
    errors.append(f"[{group}] {msg}")


# ================================================================ đọc mã C#
def blank(src):
    """Thay nội dung chuỗi và chú thích bằng khoảng trắng (giữ nguyên vị trí) để đếm ngoặc an toàn."""
    out, i, n = list(src), 0, len(src)

    def wipe(a, b):
        for k in range(a, b):
            if out[k] != "\n":
                out[k] = " "

    while i < n:
        c = src[i]
        if src.startswith("//", i):
            j = src.find("\n", i)
            j = n if j < 0 else j
            wipe(i, j)
            i = j
        elif src.startswith("/*", i):
            j = src.find("*/", i + 2) + 2
            wipe(i, j)
            i = j
        elif src.startswith('"""', i):
            j = src.find('"""', i + 3) + 3
            wipe(i + 1, j - 1)
            i = j
        elif c == '"' or (c in "@$" and i + 1 < n and src[i + 1] in '"@$'):
            j = i
            verbatim = False
            while src[j] in "@$":
                verbatim |= src[j] == "@"
                j += 1
            j += 1
            while j < n:
                if src[j] == "\\" and not verbatim:
                    j += 2
                    continue
                if src[j] == '"':
                    if verbatim and j + 1 < n and src[j + 1] == '"':
                        j += 2
                        continue
                    break
                j += 1
            wipe(i + 1, j)
            i = j + 1
        elif c == "'" and i + 2 < n and (src[i + 2] == "'" or src[i + 1] == "\\"):
            j = src.find("'", i + 2 if src[i + 1] == "\\" else i + 1)
            wipe(i + 1, j)
            i = j + 1
        else:
            i += 1
    return "".join(out)


def match_brace(s, i):
    """s[i] == '{' → vị trí '}' tương ứng."""
    depth = 0
    for j in range(i, len(s)):
        if s[j] == "{":
            depth += 1
        elif s[j] == "}":
            depth -= 1
            if depth == 0:
                return j
    raise ValueError("ngoặc không cân")


def split_top(s, sep=","):
    parts, depth, cur = [], 0, ""
    for c in s:
        if c in "(<[{":
            depth += 1
        elif c in ")>]}":
            depth -= 1
        if c == sep and depth == 0:
            parts.append(cur)
            cur = ""
        else:
            cur += c
    if cur.strip():
        parts.append(cur)
    return parts


def param_names(plist):
    names = []
    for p in split_top(plist):
        p = re.sub(r"\[[^\]]*\]", " ", p).split("=")[0].strip()
        if not p or re.search(r"\bCancellationToken\b", p):
            continue
        names.append(re.findall(r"\w+", p)[-1])
    return names


def flatten(body):
    """Thân lớp chỉ giữ cấp ngoài cùng: mọi khối {...} lồng bên trong thay bằng '{}'."""
    out, i = [], 0
    while i < len(body):
        if body[i] == "{":
            j = match_brace(body, i)
            out.append(" {} ")
            i = j + 1
        else:
            out.append(body[i])
            i += 1
    return "".join(out)


def parse_member(chunk, cls, t):
    chunk = re.sub(r"^\s*(\[[^\]]*\]\s*)+", "", chunk).strip()
    if not chunk or chunk.startswith(("=", "where")):
        return
    depth, i, n = 0, 0, len(chunk)
    while i < n:
        c = chunk[i]
        if c in "<[":
            depth += 1
        elif c in ">]" and not chunk.startswith("=>", i - 1):
            depth -= 1
        elif depth == 0 and chunk.startswith("=>", i):
            break                                       # thuộc tính biểu thức
        elif depth == 0 and c == "=":
            break                                       # trường / thuộc tính có giá trị khởi tạo
        elif depth == 0 and c == "(":
            before = chunk[:i].rstrip()
            name = re.findall(r"(\w+)\s*(?:<[^<>]*>)?$", before)
            if name and name[0] not in KEYWORDS and before.split()[-1] != "operator":
                j, d = i, 0
                while j < n:
                    d += {"(": 1, ")": -1}.get(chunk[j], 0)
                    if d == 0:
                        break
                    j += 1
                if name[0] != cls:                      # bỏ hàm dựng
                    t["methods"][name[0]].append(param_names(chunk[i + 1:j]))
                return
            # '(' là kiểu trả về dạng tuple: bỏ qua cả nhóm
            d = 0
            while i < n:
                d += {"(": 1, ")": -1}.get(chunk[i], 0)
                if d == 0:
                    break
                i += 1
        i += 1
    decl = chunk[:i].strip()
    m = re.match(r"(?:(?:public|private|protected|internal|static|readonly|const|required|override|virtual|new|"
                 r"abstract|volatile|event)\s+)*(.+?)\s+(\w+)$", decl, re.S)
    if m:
        t["members"][m.group(2)] = m.group(1).strip()


def parse_cs():
    types = defaultdict(list)
    type_re = re.compile(r"\b(class|interface|enum|record|struct)\s+(\w+)\s*(?:<[^>{(]*>)?\s*(\([^)]*\))?\s*"
                         r"(?::\s*([^{;]+?))?\s*(?:where[^{;]*)?([{;])", re.S)
    for f in SRC.rglob("*.cs"):
        if {"bin", "obj", "Migrations"} & set(f.parts):
            continue
        raw = f.read_text(encoding="utf-8-sig")
        src = blank(raw)
        ns = re.search(r"namespace\s+([\w.]+)", src)
        area = re.search(r"\.Areas\.(\w+)\.", (ns.group(1) + ".") if ns else "")
        for m in type_re.finditer(src):
            kind, name, prim, bases, end = m.groups()
            t = {"kind": kind, "name": name, "file": f, "area": area.group(1) if area else None,
                 "bases": [re.sub(r"<.*", "", b).strip() for b in split_top(bases or "")],
                 "members": {}, "methods": defaultdict(list), "literals": [], "text": ""}
            if prim:                                    # record dạng vị trí: tham số là thuộc tính
                for p in split_top(prim[1:-1]):
                    p = p.split("=")[0].strip()
                    typ, pname = p.rsplit(None, 1)
                    t["members"][pname] = typ
            if end == "{":
                a = m.end() - 1
                body = src[a + 1:match_brace(src, a)]
                t["text"] = body
                if kind == "enum":
                    t["literals"] = [x.split("=")[0].strip() for x in body.split(",") if x.strip()]
                else:
                    for chunk in re.split(r";|\{\}", flatten(body)):
                        parse_member(chunk, name, t)
            types[name].append(t)
            if t["area"]:
                types[f"{t['area']}.{name}"].append(t)
    return types


def lookup(types, name):
    found = types.get(name, [])
    if len(found) == 1:
        return found[0]
    # cùng tên (lớp partial) thì gộp; khác vùng (Admin/Tenant) phải ghi rõ tiền tố trên sơ đồ
    if found and len({t["file"].parent for t in found}) == 1:
        merged = dict(found[0], members={}, methods=defaultdict(list), text="")
        for t in found:
            merged["members"].update(t["members"])
            for k, v in t["methods"].items():
                merged["methods"][k] += v
            merged["text"] += t["text"]
        return merged
    return None


def ancestry(types, t):
    """Lớp và các lớp cha trong dự án (không đi vào interface)."""
    out, seen = [], set()
    while t and t["name"] not in seen:
        seen.add(t["name"])
        out.append(t)
        nxt = None
        for b in t["bases"]:
            cand = lookup(types, b)
            if cand and cand["kind"] in ("class", "record"):
                nxt = cand
                break
        t = nxt
    return out


def has_member(types, t, name):
    for a in ancestry(types, t):
        if name in a["members"] or name in a["methods"]:
            return True
        if any(name in EXTERNAL.get(b, ()) for b in a["bases"]):
            return True
    return False


def method_params(types, t, name):
    for a in ancestry(types, t):
        if name in a["methods"]:
            return a["methods"][name]
    return None


# ================================================================ đọc model snapshot EF
def parse_snapshot():
    src = SNAPSHOT.read_text(encoding="utf-8-sig")
    ents = defaultdict(lambda: {"table": None, "cols": {}, "keys": set(), "fks": []})
    for m in re.finditer(r'modelBuilder\.Entity\("([^"]+)", b =>\s*\{', src):
        e = ents[m.group(1).split(".")[-1]]
        body = src[m.end():match_brace(src, m.end() - 1)]
        tb = re.search(r'b\.ToTable\("([^"]+)"', body)
        if tb:
            e["table"] = tb.group(1)
        for stmt in body.split(";"):
            p = re.search(r'b\.Property<([^>]+(?:>[^>(]*)?)>\("(\w+)"\)', stmt)
            if p:
                typ, prop = p.groups()
                col = re.search(r'HasColumnName\("(\w+)"\)', stmt)
                nullable = typ.endswith("?") or (typ in ("string", "byte[]") and ".IsRequired()" not in stmt)
                e["cols"][prop] = {"col": col.group(1) if col else prop, "null": nullable}
            k = re.search(r"b\.HasKey\(([^)]*)\)", stmt)
            if k:
                e["keys"] |= set(re.findall(r'"(\w+)"', k.group(1)))
            r = re.search(r'b\.HasOne\("([^"]+)", (?:"(\w+)"|null)\)', stmt)
            if r:
                # Mọi cột của khoá ngoại (kể cả khoá ngoại ghép); đối số có dấu chấm là tên lớp (WithOne).
                args = re.search(r'HasForeignKey\(([^)]*)\)', stmt)
                fk = [a for a in re.findall(r'"([^"]+)"', args.group(1)) if "." not in a] if args else []
                many = re.search(r'\.WithMany\((?:"(\w+)")?\)', stmt)
                one = re.search(r'\.WithOne\((?:"(\w+)")?\)', stmt)
                delete = re.search(r"DeleteBehavior\.(\w+)", stmt)
                e["fks"].append({"principal": r.group(1).split(".")[-1].split("<")[0], "nav": r.group(2),
                                 "fk": fk[0] if fk else None, "cols": fk, "many": bool(many),
                                 "inverse": (many or one).group(1) if (many or one) else None,
                                 "required": ".IsRequired()" in stmt,
                                 "delete": delete.group(1) if delete else "Cascade"})
    # Bảng Identity: tên lớp kiểu IdentityUserRole<string> → khoá theo tên bảng
    return dict(ents)


# ================================================================ đọc mô hình StarUML
def load_mdj():
    idx = {}

    def walk(o, parent=None):
        if isinstance(o, dict):
            if "_id" in o:
                idx[o["_id"]] = o
                o["__parent"] = parent
                parent = o
            for k, v in o.items():
                if k != "__parent":
                    walk(v, parent)
        elif isinstance(o, list):
            for v in o:
                walk(v, parent)

    walk(json.loads(MDJ.read_text(encoding="utf8")))
    return idx


def ref(idx, r):
    return idx.get((r or {}).get("$ref")) if isinstance(r, dict) else None


def diagrams(idx, typ):
    return [d for d in idx.values() if d.get("_type") == typ]


def shown(idx, d, typ):
    out = []
    for v in d.get("ownedViews", []):
        m = ref(idx, v.get("model"))
        if m and m.get("_type") in (typ if isinstance(typ, tuple) else (typ,)):
            out.append(m)
    return out


def code_name(name):
    return name.split(".")[-1]


# ================================================================ 1. sơ đồ lớp
# Sơ đồ lớp mức phân tích dùng tên bảng / cột tiếng Việt của CSDL; thao tác nghiệp vụ ánh xạ sang hàm cài đặt.
OPS_MAP = {
    ("KhuVuc", "dsDuAn"): ("Region", "Projects"),
    ("DuAn", "taoMoi"): ("ProjectsController", "Create"),
    ("DuAn", "capNhat"): ("ProjectsController", "Edit"),
    ("ToaNha", "taoMoi"): ("BuildingsController", "Create"),
    ("ToaNha", "capNhat"): ("BuildingsController", "Edit"),
    ("Tang", "dsTangCuaToa"): ("ApartmentsController", "FloorsOf"),
    ("CanHo", "taoMoi"): ("ApartmentsController", "Create"),
    ("CanHo", "doiTinhTrang"): ("ApartmentsController", "ChangeStatus"),
    ("CanHo", "layCuDanDaiDien"): ("Apartment", "PrimaryResident"),
    ("CuDan", "themVaoCan"): ("ApartmentsController", "AddResident"),
    ("CuDan", "chuyenDi"): ("ApartmentsController", "MoveOut"),
    ("CuDan", "guiLaiThuMoi"): ("ApartmentsController", "ResendInvite"),
    ("CuDan", "dangO"): ("ApartmentResident", "IsActive"),
    ("NguoiDung", "dangNhap"): ("HomeController", "Login"),
    ("NguoiDung", "kichHoatQuaThuMoi"): ("HomeController", "Invite"),
    ("NguoiDung", "khoaTaiKhoan"): ("UsersController", "ToggleLock"),
    ("NguoiDung", "xemViecDuocGiao"): ("JobsController", "Index"),
    ("VaiTro", "luuPhanQuyen"): ("RolesController", "Save"),
    ("Quyen", "dsVaiTro"): ("Permission", "RolePermissions"),
    ("YeuCauBaoTri", "guiYeuCau"): ("Tenant.MaintenanceController", "Create"),
    ("YeuCauBaoTri", "phanLoaiBangAI"): ("MaintenanceTriageService", "TriageAsync"),
    ("YeuCauBaoTri", "tinhHanXuLy"): ("MaintenanceWorkflow", "RecalculateDueAsync"),
    ("YeuCauBaoTri", "phanLoaiLai"): ("Admin.MaintenanceController", "Reclassify"),
    ("YeuCauBaoTri", "giaoViec"): ("MaintenanceWorkflow", "AssignAsync"),
    ("YeuCauBaoTri", "chuyenTrangThai"): ("MaintenanceWorkflow", "TransitionAsync"),
    ("YeuCauBaoTri", "hoanTat"): ("JobsController", "Resolve"),
    ("YeuCauBaoTri", "xacNhanVaDanhGia"): ("Tenant.MaintenanceController", "ChangeStatus"),
    ("YeuCauBaoTri", "quaHan"): ("MaintenanceWorkflow", "IsOverdue"),
    ("MucSla", "tinhHan"): ("MaintenanceWorkflow", "RecalculateDueAsync"),
    ("AnhSuCo", "tachDanhSachAnh"): ("MaintenanceWorkflow", "PhotosFrom"),
    ("LichSuTrangThaiSuCo", "ghiLichSu"): ("MaintenanceWorkflow", "AddHistory"),
    ("NhatKyHeThong", "ghiNhatKy"): ("AppDbContext", "ApplyAuditAndCollectLogs"),
    ("NhatKyHeThong", "xemNhatKy"): ("AuditLogsController", "Index"),
    # Phân hệ thu phí (Hình 3.7b)
    ("PhiDinhKy", "themKhoan"): ("ApartmentsController", "AddCharge"),
    ("PhiDinhKy", "batTat"): ("ApartmentsController", "ToggleCharge"),
    ("LoaiDichVu", "capNhatDonGia"): ("Admin.UtilitiesController", "EditType"),
    ("ChiSoDichVu", "ghiChiSo"): ("Admin.UtilitiesController", "EditReading"),
    ("ChiSoDichVu", "nhapTheoToa"): ("Admin.UtilitiesController", "Bulk"),
    ("ChiSoDichVu", "xoa"): ("Admin.UtilitiesController", "DeleteReading"),
    ("HoaDon", "lapHoaDonThang"): ("InvoiceGenerator", "GenerateMonthlyInvoiceAsync"),
    ("HoaDon", "lapHoaDonPhatSinh"): ("InvoiceGenerator", "GenerateOneOffInvoiceAsync"),
    ("HoaDon", "suaDong"): ("Admin.InvoicesController", "EditItems"),
    ("HoaDon", "tinhLaiTong"): ("Admin.InvoicesController", "RecomputeTotals"),
    ("HoaDon", "danhDauQuaHan"): ("InvoiceGenerator", "MarkOverdueInvoicesAsync"),
    ("HoaDon", "congPhiTre"): ("InvoiceGenerator", "ApplyLateFeesAsync"),
    ("HoaDon", "capNhatCongNo"): ("Admin.InvoicesController", "ApplyPaymentSuccess"),
    ("HoaDon", "huy"): ("Admin.InvoicesController", "Cancel"),
    ("ThanhToan", "guiXacNhan"): ("Tenant.InvoicesController", "SubmitPayment"),
    ("ThanhToan", "ghiNhanTrucTiep"): ("Admin.InvoicesController", "RecordPayment"),
    ("ThanhToan", "duyet"): ("Admin.InvoicesController", "ApprovePayment"),
    ("ThanhToan", "tuChoi"): ("Admin.InvoicesController", "RejectPayment"),
    ("ThanhToan", "hoanTien"): ("PaymentsController", "Refund"),
}
# Enum tiếng Việt ↔ enum trong mã; tên giá trị so với nhãn ở báo cáo (từ điển dữ liệu mục 2.6) theo đúng thứ tự.
# Cột dạng "Bảng.Cột" lấy nhãn ở từ điển thu phí (thu_phi_data.DD_THU_PHI).
ENUM_MAP = {"TrangThaiSuCo": ("MaintenanceStatus", None), "MucDoUuTien": ("MaintenancePriority", "MucDoUuTien"),
            "LoaiBaoTri": ("MaintenanceCategory", "LoaiBaoTri"),
            "TrangThaiHoaDon": ("InvoiceStatus", "HoaDon.TrangThai"),
            "TrangThaiThanhToan": ("PaymentStatus", "ThanhToan.TrangThai")}


def ascii_name(label):
    """"Đã tiếp nhận" → "DaTiepNhan"."""
    import unicodedata
    s = unicodedata.normalize("NFD", label.replace("đ", "d").replace("Đ", "D"))
    s = "".join(ch for ch in s if unicodedata.category(ch) != "Mn")
    return "".join(w[:1].upper() + w[1:] for w in s.split())


def legend_labels(column):
    """Nhãn theo thứ tự giá trị: trạng thái lấy ở câu chú thích trigger, các cột khác ở mô tả từ điển dữ liệu."""
    import ast
    report = REPORT.read_text(encoding="utf8")
    if column is None:
        text = re.search(r"từ điển dữ liệu: (0 [^)]*)\)", report.replace('"\n        "', "")).group(1)
    elif "." in column:
        from thu_phi_data import DD_THU_PHI
        table, col = column.split(".")
        text = next(r[3] for r in DD_THU_PHI[table] if r[0] == col)
    else:
        a = report.index("{", report.index("    dd = {"))
        depth = 0
        for b in range(a, len(report)):
            depth += {"{": 1, "}": -1}.get(report[b], 0)
            if depth == 0:
                break
        rows = ast.literal_eval(report[a:b + 1])["YeuCauBaoTri"]
        text = next(r[3] for r in rows if r[0] == column)
    pairs = sorted((int(n), lbl) for n, lbl in re.findall(r"(\d+) ([^,]+)", text))
    return [ascii_name(re.sub(r"\s*\(.*?\)", "", lbl).strip()) for _, lbl in pairs]   # bỏ phần giải thích (…)


def check_table_class(g, c, ents, types, by_table):
    """Lớp mang tên bảng: thuộc tính là cột của bảng, thao tác có ánh xạ tới hàm cài đặt có thật."""
    e = ents[by_table[c["name"]]]
    cols = {x["col"].lower() for x in e["cols"].values()}
    for a in c.get("attributes", []):
        if a["name"].lower() not in cols:
            err(g, f"{c['name']}.{a['name']}: không có cột tương ứng trong bảng")
    for op in c.get("operations", []):
        target = OPS_MAP.get((c["name"], op["name"]))
        if not target:
            err(g, f"{c['name']}.{op['name']}(): chưa ánh xạ sang hàm cài đặt")
            continue
        t = lookup(types, target[0])
        if not t or not has_member(types, t, target[1]):
            err(g, f"{c['name']}.{op['name']}(): {target[0]}.{target[1]} không có trong mã")


def fmt_params(op):
    return [p["name"] for p in op.get("parameters", []) if p.get("direction") != "return"]


def check_class_diagram(idx, d, types, ents):
    g = d["documentation"]
    classes = shown(idx, d, ("UMLClass", "UMLInterface", "UMLEnumeration"))
    by_table = {e["table"]: n for n, e in ents.items() if e["table"]}
    entity = lambda name: by_table.get(name, name)  # noqa: E731
    names = {entity(c["name"]) for c in classes}
    for c in classes:
        if c["name"] in by_table:
            check_table_class(g, c, ents, types, by_table)
            continue
        if c["name"] in ENUM_MAP:
            lits = [x["name"] for x in c.get("literals", [])]
            code_enum, column = ENUM_MAP[c["name"]]
            want = legend_labels(column)
            if len(want) != len(lookup(types, code_enum)["literals"]):
                err(g, f"enum {c['name']}: báo cáo ghi {len(want)} giá trị, mã có "
                       f"{len(lookup(types, code_enum)['literals'])}")
            if lits != want:
                err(g, f"enum {c['name']}: {lits} ≠ thứ tự trong mã / báo cáo {want}")
            continue
        t = lookup(types, c["name"])
        if not t:
            err(g, f"lớp {c['name']} không có (hoặc trùng tên, cần tiền tố vùng) trong mã")
            continue
        want = {"UMLClass": ("class", "record"), "UMLInterface": ("interface",), "UMLEnumeration": ("enum",)}[c["_type"]]
        if t["kind"] not in want:
            err(g, f"{c['name']}: mã là {t['kind']}, sơ đồ vẽ {c['_type']}")
        for a in c.get("attributes", []):
            if not has_member(types, t, a["name"]):
                err(g, f"{c['name']}.{a['name']}: thuộc tính không có trong mã")
        for op in c.get("operations", []):
            found = method_params(types, t, op["name"])
            if found is None:
                err(g, f"{c['name']}.{op['name']}(): phương thức không có trong mã")
            elif fmt_params(op) not in found:
                err(g, f"{c['name']}.{op['name']}{tuple(fmt_params(op))}: tham số khác mã {found}")
        if c["_type"] == "UMLEnumeration":
            lits = [x["name"] for x in c.get("literals", [])]
            if lits != t["literals"]:
                err(g, f"enum {c['name']}: {lits} ≠ mã {t['literals']}")

    rels = [m for v in d["ownedViews"] if (m := ref(idx, v.get("model")))
            and m.get("_type") in ("UMLAssociation", "UMLGeneralization", "UMLDependency", "UMLInterfaceRealization")]
    drawn_fk = set()
    for r in rels:
        if r["_type"] == "UMLAssociation":
            a, b = ref(idx, r["end1"]["reference"]), ref(idx, r["end2"]["reference"])
            ea, eb = entity(a["name"]), entity(b["name"])
            if ea in ents or eb in ents:
                vn = a["name"] in by_table or b["name"] in by_table
                drawn_fk |= check_entity_assoc(g, r, ea, eb, ents, vn)
            else:
                check_field_assoc(g, r, a["name"], b["name"], types)
        else:
            src, dst = ref(idx, r["source"]), ref(idx, r["target"])
            t = lookup(types, src["name"])
            if not t:
                continue
            if r["_type"] in ("UMLGeneralization", "UMLInterfaceRealization"):
                if code_name(dst["name"]) not in t["bases"]:
                    err(g, f"{src['name']} không kế thừa/hiện thực {dst['name']} trong mã")
            elif not re.search(rf"\b{code_name(dst['name'])}\b", t["text"] + " ".join(t["members"].values())):
                err(g, f"«use» {src['name']} → {dst['name']}: lớp nguồn không tham chiếu lớp đích")
    # Đủ: mọi khoá ngoại giữa hai lớp cùng có trên sơ đồ đều phải được vẽ.
    for e_name, e in ents.items():
        for fk in e["fks"]:
            if e_name in names and fk["principal"] in names and (e_name, fk["fk"]) not in drawn_fk:
                err(g, f"thiếu quan hệ {fk['principal']} – {e_name} (khoá ngoại {fk['fk']})")


def check_entity_assoc(g, r, a, b, ents, vn=False):
    """Một liên kết giữa hai lớp thực thể phải ứng với đúng một khoá ngoại; trả về {(lớp phụ thuộc, cột FK)}.
    vn: sơ đồ tên tiếng Việt, vai trò phía bảng cha là tên cột khoá ngoại bỏ "Id" (nguoiBaoCao ↔ NguoiBaoCaoId)."""
    e1, e2 = r["end1"], r["end2"]
    for dep, prin, end_p, end_d in ((b, a, e1, e2), (a, b, e2, e1)):
        cands = [fk for fk in ents.get(dep, {}).get("fks", []) if fk["principal"] == prin]
        role = end_p.get("name") or (r.get("name") if vn else None)  # sơ đồ tiếng Việt: tên quan hệ = vai trò
        if role:
            if vn:
                col = lambda fk: ents[dep]["cols"].get(fk["fk"], {}).get("col", "")  # noqa: E731
                cands = [fk for fk in cands if col(fk).lower() == role.lower() + "id"]
            else:
                cands = [fk for fk in cands if fk["nav"] == end_p["name"]]
        if len(cands) > 1:
            err(g, f"{a} – {b}: nhiều khoá ngoại, cần ghi vai trò để phân biệt")
            return set()
        if cands:
            fk = cands[0]
            want_p = "1" if fk["required"] else "0..1"
            if end_p.get("multiplicity") != want_p:
                err(g, f"{prin} – {dep} ({fk['fk']}): bội số phía {prin} là {end_p.get('multiplicity')}, mã là {want_p}")
            if fk["many"] and end_d.get("multiplicity") not in ("0..*", "1..*"):
                err(g, f"{prin} – {dep} ({fk['fk']}): phía {dep} phải là nhiều")
            if end_d.get("name") and not vn and end_d["name"] != fk["inverse"]:
                err(g, f"{prin} – {dep}: vai trò {end_d['name']} ≠ thuộc tính điều hướng {fk['inverse']}")
            if end_p.get("aggregation") == "composite" and not (fk["required"] and fk["delete"] == "Cascade"):
                err(g, f"{prin} ◆– {dep}: hợp thành nhưng khoá ngoại không bắt buộc / không xoá dây chuyền "
                       f"({fk['delete']})")
            if end_p.get("aggregation") != "composite" and fk["required"] and fk["delete"] == "Cascade" \
                    and end_d.get("aggregation") != "composite":
                notes.append(f"[{g}] {prin} – {dep}: khoá ngoại xoá dây chuyền, có thể vẽ hợp thành")
            return {(dep, fk["fk"])}
    many = ("0..*", "1..*")
    if e1.get("multiplicity") in many and e2.get("multiplicity") in many:
        for j, e in ents.items():
            fa = [fk for fk in e["fks"] if fk["principal"] == a]
            fb = [fk for fk in e["fks"] if fk["principal"] == b]
            if fa and fb and j not in (a, b):
                return {(j, fa[0]["fk"]), (j, fb[0]["fk"])}   # bảng nối (ví dụ NguoiDung_VaiTro)
    err(g, f"liên kết {a} – {b} không ứng với khoá ngoại nào trong snapshot")
    return set()


def check_field_assoc(g, r, a, b, types):
    for src, dst, end in ((a, b, r["end2"]), (b, a, r["end1"])):
        if end.get("navigable") == "navigable":
            t = lookup(types, src)
            role = end.get("name")
            typ = (t or {}).get("members", {}).get(role) if t else None
            if not typ:
                err(g, f"{src}.{role}: trường không có trong mã")
            elif code_name(dst) not in typ:
                err(g, f"{src}.{role} có kiểu {typ}, sơ đồ nối tới {dst}")


# ================================================================ 2. sơ đồ tuần tự
def check_sequences(idx, types):
    for d in diagrams(idx, "UMLSequenceDiagram"):
        g = d["documentation"]
        for v in d["ownedViews"]:
            msg = ref(idx, v.get("model"))
            if not msg or msg.get("_type") != "UMLMessage":
                continue
            if msg.get("messageSort") == "reply":
                continue
            src, dst = ref(idx, msg["source"]), ref(idx, msg["target"])
            if not dst["name"].startswith(":"):
                continue                                # gọi ra ngoài hệ thống (Ollama, người dùng)
            cls = dst["name"][1:]
            m = re.match(r"(\w+)(\(|\.)", msg["name"])
            if not m:
                continue                                # thông điệp mô tả (truy vấn), không phải lời gọi hàm
            head = m.group(1)
            t = lookup(types, cls)
            if not t:
                err(g, f"lớp {cls} không có trong mã")
                continue
            caller = lookup(types, src["name"][1:]) if src["name"].startswith(":") else None
            ok = has_member(types, t, head) or (m.group(2) == "." and caller and has_member(types, caller, head))
            if not ok:
                err(g, f"'{msg['name']}': {cls} không có {head}")
                continue


# ================================================================ 3. trạng thái + trigger
def check_states(idx, types):
    wf = lookup(types, "MaintenanceWorkflow")
    src = blank((SRC / "Infrastructure" / "Maintenance" / "MaintenanceWorkflow.cs").read_text(encoding="utf-8-sig"))
    raw = (SRC / "Infrastructure" / "Maintenance" / "MaintenanceWorkflow.cs").read_text(encoding="utf-8-sig")
    rules = {}
    for a, b, actors in re.findall(r"\[\(MaintenanceStatus\.(\w+), MaintenanceStatus\.(\w+)\)\] = new\[\] \{([^}]*)\}",
                                   raw):
        rules[(a, b)] = set(re.findall(r"WorkflowActor\.(\w+)", actors))
    note_block = raw[raw.index("NoteRequired"):]
    note_block = note_block[:note_block.index("};")]
    note_req = set(re.findall(r"\(MaintenanceStatus\.(\w+), MaintenanceStatus\.(\w+)\)", note_block))
    assert wf and rules and src

    status = lookup(types, "MaintenanceStatus")["literals"]
    report = REPORT.read_text(encoding="utf8")
    legend = re.search(r"từ điển dữ liệu: (0 [^)]*)\)", report.replace('"\n        "', ""))
    num_of = {lbl.strip(): int(n) for n, lbl in re.findall(r"(\d) ([^,]+)", legend.group(1))}
    enum_of = {lbl: status[n] for lbl, n in num_of.items()}
    abbr = {"QL": "Manager", "HT": "System", "KTV": "Technician", "CD": "Resident"}

    d = next(x for x in diagrams(idx, "UMLStatechartDiagram") if "1.3" in x["documentation"])
    g = d["documentation"]
    drawn = {}
    for v in d["ownedViews"]:
        tr = ref(idx, v.get("model"))
        if not tr or tr.get("_type") != "UMLTransition":
            continue
        s, t = ref(idx, tr["source"]), ref(idx, tr["target"])
        if s.get("_type") != "UMLState" or t.get("_type") != "UMLState":
            continue
        for x in (s, t):
            if x["name"] not in enum_of:
                err(g, f"trạng thái '{x['name']}' không có trong từ điển dữ liệu của báo cáo")
        pair = (enum_of.get(s["name"]), enum_of.get(t["name"]))
        drawn[pair] = tr.get("name", "")
    if set(drawn) != set(rules):
        err(g, f"chuyển trạng thái thừa {set(drawn) - set(rules)}, thiếu {set(rules) - set(drawn)}")
    for pair, label in drawn.items():
        if pair not in rules or not label:
            continue
        who = label.split(":")[0]
        actors = {abbr[x] for x in who.split("/") if x in abbr}
        if actors != rules[pair]:
            err(g, f"{pair}: nhãn '{who}' ≠ quy tắc {sorted(rules[pair])}")
        if ("lý do" in label) != (pair in note_req):
            err(g, f"{pair}: nhãn '[có lý do]' không khớp NoteRequired")
    unlabeled = [p for p, lbl in drawn.items() if not lbl]
    if unlabeled:
        notes.append(f"[{g}] {len(unlabeled)} chuyển không ghi nhãn (dùng chung nhãn với cạnh song song): {unlabeled}")

    sql = re.search(r"NOT IN\s*\(\((.*?)\)\) THEN", report, re.S).group(1)
    sql_pairs = {(status[int(a)], status[int(b)]) for a, b in re.findall(r"(\d),(\d)", sql)}
    if sql_pairs != set(rules):
        err("Trigger 2.5.3", f"tập chuyển trong fn_kiem_tra_chuyen_trang_thai khác Rules: "
                             f"thừa {sql_pairs - set(rules)}, thiếu {set(rules) - sql_pairs}")
    return len(rules)


# ================================================================ 4. ERD / lược đồ quan hệ / từ điển dữ liệu
# ERD mức khái niệm (Hình 2.1) đặt tên thực thể theo nghiệp vụ; ánh xạ sang bảng thật để đối chiếu.
CONCEPT = {"KHU_VUC": "KhuVuc", "KHU": "DuAn", "VAI_TRO": "VaiTro", "QUYEN": "Quyen", "TOA_NHA": "ToaNha",
           "NGUOI_DUNG": "NguoiDung", "TANG": "Tang", "CAN_HO": "CanHo", "CU_DAN": "CuDan",
           "YEU_CAU_SU_CO": "YeuCauBaoTri", "MUC_SLA": "MucSla", "ANH_SU_CO": "AnhSuCo",
           "LICH_SU_TRANG_THAI": "LichSuTrangThaiSuCo", "NHAT_KY_HE_THONG": "NhatKyHeThong",
           "HOA_DON": "HoaDon", "CHI_TIET_HOA_DON": "ChiTietHoaDon", "THANH_TOAN": "ThanhToan",
           "CHI_SO_DICH_VU": "ChiSoDichVu", "LOAI_DICH_VU": "LoaiDichVu", "PHI_DINH_KY": "PhiDinhKy"}


def fks_between(ents, ca, cb):
    """Khoá ngoại nối hai lớp: trực tiếp (cb → ca hoặc ca → cb) hoặc qua bảng nối có khoá ngoại tới cả hai."""
    direct = [(cb, fk) for fk in ents.get(cb, {}).get("fks", []) if fk["principal"] == ca] + \
             [(ca, fk) for fk in ents.get(ca, {}).get("fks", []) if fk["principal"] == cb]
    junction = [n for n, e in ents.items() if {ca, cb} <= {fk["principal"] for fk in e["fks"]} and n not in (ca, cb)]
    return direct, junction


def check_erd(idx, ents):
    by_table = {e["table"]: (name, e) for name, e in ents.items() if e["table"]}
    n = 0
    for d in diagrams(idx, "ERDDiagram"):
        g = d["documentation"]
        conceptual = "2.1" in g
        table_of = (lambda x: CONCEPT.get(x, x)) if conceptual else (lambda x: x)
        for ent in shown(idx, d, "ERDEntity"):
            n += 1
            tname = table_of(ent["name"])
            if tname not in by_table:
                err(g, f"bảng {ent['name']} ({tname}) không có trong CSDL")
                continue
            cname, e = by_table[tname]
            cols = {c["col"]: c for c in e["cols"].values()}
            fk_cols = {e["cols"][c]["col"] for fk in e["fks"] for c in fk["cols"] if c in e["cols"]}
            pk_cols = {e["cols"][k]["col"] for k in e["keys"] if k in e["cols"]}
            for c in ent.get("columns", []):
                if c["name"] not in cols:
                    err(g, f"{ent['name']}.{c['name']}: cột không có")
                    continue
                if bool(c.get("primaryKey")) != (c["name"] in pk_cols):
                    err(g, f"{ent['name']}.{c['name']}: cờ PK sai")
                if conceptual:
                    continue                            # ERD khái niệm không ghi FK / NULL
                if bool(c.get("foreignKey")) != (c["name"] in fk_cols):
                    err(g, f"{ent['name']}.{c['name']}: cờ FK sai")
                if not c.get("primaryKey") and bool(c.get("nullable")) != cols[c["name"]]["null"]:
                    err(g, f"{ent['name']}.{c['name']}: NULL trên sơ đồ = {bool(c.get('nullable'))}, "
                           f"CSDL = {cols[c['name']]['null']}")
        for v in d["ownedViews"]:
            r = ref(idx, v.get("model"))
            if not r or r.get("_type") != "ERDRelationship":
                continue
            ends = {}
            for end in (r["end1"], r["end2"]):
                t = table_of(ref(idx, end["reference"])["name"])
                ends[by_table.get(t, (None,))[0]] = end
            (ca, ea), (cb, eb) = ends.items()
            direct, junction = fks_between(ents, ca, cb)
            if not direct and not junction:
                err(g, f"quan hệ {ca} – {cb} không có khoá ngoại tương ứng")
                continue
            if junction and not direct:
                if ea.get("cardinality", "1") not in ("0..*", "1..*") or eb.get("cardinality", "1") not in ("0..*", "1..*"):
                    err(g, f"quan hệ {ca} – {cb} qua bảng nối {junction} phải là nhiều – nhiều")
                continue
            # Đầu bảng cha: 1 nếu khoá ngoại bắt buộc, 0..1 nếu cho phép NULL. Nhiều FK cùng cặp
            # (Reporter / AssignedTo) thì chỉ cần khớp một.
            oks = []
            for dep, fk in direct:
                pe, de = ends[fk["principal"]], ends[dep]
                want = "1" if fk["required"] else "0..1"
                oks.append(pe.get("cardinality", "1") == want)
            if not any(oks):
                msg = (f"{ca} – {cb}: bản số {ea.get('cardinality', '1')} / {eb.get('cardinality', '1')} không khớp khoá ngoại "
                       f"{[(dep, fk['fk'], 'bắt buộc' if fk['required'] else 'NULL') for dep, fk in direct]}")
                if conceptual:
                    notes.append(f"[{g}] {msg} (quy tắc nghiệp vụ chặt hơn CSDL)")
                else:
                    err(g, msg)
    return n


def check_dictionary(ents):
    """Mục 2.6 của báo cáo: NULL / NOT NULL, bảng đích và hành vi xoá của khoá ngoại khớp CSDL."""
    import ast
    src = REPORT.read_text(encoding="utf8")
    a = src.index("{", src.index("    dd = {"))
    depth = 0
    for b in range(a, len(src)):
        depth += {"{": 1, "}": -1}.get(src[b], 0)
        if depth == 0:
            break
    dd = ast.literal_eval(src[a:b + 1])
    from thu_phi_data import DD_THU_PHI
    dd.update(DD_THU_PHI)                               # 6 bảng thu phí bổ sung tuần 6, viết thẳng vào file Word
    by_table = {e["table"]: e for e in ents.values() if e["table"]}
    table_of = {name: e["table"] for name, e in ents.items()}
    delete_vi = {"CASCADE": "Cascade", "RESTRICT": "Restrict", "SET NULL": "SetNull", "NO ACTION": "NoAction"}
    n = 0
    for table, rows in dd.items():
        e = by_table.get(table)
        if not e:
            continue                                    # nhóm cột kiểm toán chung, không phải bảng thật
        cols = {c["col"]: (p, c) for p, c in e["cols"].items()}
        for row in rows:
            names, cons = row[0], row[2]
            for col in [x.strip() for x in names.split(",")]:
                if col not in cols:
                    err("Từ điển 2.6", f"{table}.{col}: cột không có trong CSDL")
                    continue
                n += 1
                prop, c = cols[col]
                says_null = "NOT NULL" not in cons and bool(re.search(r"\bNULL\b", cons))
                if "PK" not in cons and says_null != c["null"]:
                    err("Từ điển 2.6", f"{table}.{col}: ghi '{cons}' nhưng CSDL "
                                       f"{'cho phép NULL' if c['null'] else 'NOT NULL'}")
                fk = next((f for f in e["fks"] if prop in f["cols"]), None)
                if "FK" in cons and not fk:
                    err("Từ điển 2.6", f"{table}.{col}: ghi FK nhưng CSDL không có khoá ngoại")
                if fk:
                    tgt = re.search(r"→ (\w+)", cons)
                    if tgt and tgt.group(1) != table_of.get(fk["principal"]):
                        err("Từ điển 2.6", f"{table}.{col}: FK → {tgt.group(1)}, CSDL → {table_of.get(fk['principal'])}")
                    for vi, en in delete_vi.items():
                        if vi in cons.replace("NOT NULL", "") and fk["delete"] != en:
                            err("Từ điển 2.6", f"{table}.{col}: ghi {vi}, CSDL xoá {fk['delete']}")
    return n


# ================================================================ 5. hoạt động
def check_activities(idx):
    n = 0
    for d in diagrams(idx, "UMLActivityDiagram"):
        g = d["documentation"]
        nodes = {m["_id"]: m for m in shown(idx, d, ("UMLInitialNode", "UMLActivityFinalNode", "UMLAction",
                                                      "UMLDecisionNode", "UMLMergeNode", "UMLForkNode",
                                                      "UMLJoinNode", "UMLFlowFinalNode"))}
        out, inc = defaultdict(list), defaultdict(list)
        for f in shown(idx, d, "UMLControlFlow"):
            s, t = f["source"]["$ref"], f["target"]["$ref"]
            out[s].append(f)
            inc[t].append(f)
        n += 1
        inits = [k for k, m in nodes.items() if m["_type"] == "UMLInitialNode"]
        finals = {k for k, m in nodes.items() if m["_type"] in ("UMLActivityFinalNode", "UMLFlowFinalNode")}
        if len(inits) != 1:
            err(g, f"có {len(inits)} nút bắt đầu")
            continue
        seen, stack = set(), [inits[0]]
        while stack:
            k = stack.pop()
            if k not in seen:
                seen.add(k)
                stack += [f["target"]["$ref"] for f in out[k]]
        back, stack = set(), list(finals)
        while stack:
            k = stack.pop()
            if k not in back:
                back.add(k)
                stack += [f["source"]["$ref"] for f in inc[k]]
        for k, m in nodes.items():
            label = m.get("name") or m["_type"]
            if k not in seen:
                err(g, f"'{label}' không đi tới được từ nút bắt đầu")
            if k not in back:
                err(g, f"'{label}' không dẫn tới nút kết thúc")
            if m["_type"] == "UMLDecisionNode":
                if len(inc[k]) != 1 or len(out[k]) < 2 or not all(f.get("guard") for f in out[k]):
                    err(g, f"nút quyết định cần 1 vào, ≥ 2 ra có điều kiện (vào {len(inc[k])}, ra "
                           f"{[f.get('guard') for f in out[k]]})")
            elif m["_type"] == "UMLMergeNode":
                if len(inc[k]) < 2 or len(out[k]) != 1:
                    err(g, f"nút gộp cần ≥ 2 vào, 1 ra (vào {len(inc[k])}, ra {len(out[k])})")
            elif m["_type"] == "UMLAction":
                if len(out[k]) != 1 or len(inc[k]) != 1:
                    err(g, f"'{label}': hành động cần đúng 1 vào và 1 ra (vào {len(inc[k])}, ra {len(out[k])}); "
                           f"nhiều luồng phải qua nút gộp/quyết định")
    return n


# ================================================================ 6. use case
def check_use_cases(idx):
    codes = defaultdict(set)
    for d in diagrams(idx, "UMLUseCaseDiagram"):
        for u in shown(idx, d, "UMLUseCase"):
            m = re.match(r"(UC\d+)([a-z]?)\b", u["name"])
            if m:
                codes[d["documentation"]].add(m.group(1) + m.group(2))
    overview = next(v for k, v in codes.items() if "1.4" in k)
    for g, cs in codes.items():
        for c in cs:
            if re.sub(r"[a-z]$", "", c) not in overview:
                err(g, f"{c}: không có use case gốc trên sơ đồ tổng quát")
    return sum(len(v) for v in codes.values())


def main():
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8")
    types = parse_cs()
    ents = parse_snapshot()
    idx = load_mdj()
    model_ops = {}
    for d in diagrams(idx, "UMLClassDiagram"):
        for c in shown(idx, d, ("UMLClass", "UMLInterface", "UMLEnumeration")):
            model_ops[c["name"]] = {o["name"] for o in c.get("operations", [])} | \
                                   {a["name"] for a in c.get("attributes", [])}
        check_class_diagram(idx, d, types, ents)
    check_sequences(idx, types)
    n_rules = check_states(idx, types)
    n_tables = check_erd(idx, ents)
    n_dd = check_dictionary(ents)
    n_act = check_activities(idx)
    n_uc = check_use_cases(idx)

    n_cls = len(model_ops)
    n_msg = sum(1 for m in idx.values() if m.get("_type") == "UMLMessage")
    lines = [f"Lớp/enum/interface trên sơ đồ lớp: {n_cls}; thông điệp tuần tự: {n_msg}; chuyển trạng thái: {n_rules}; "
             f"bảng trên ERD/RD: {n_tables}; cột trong từ điển dữ liệu: {n_dd}; sơ đồ hoạt động: {n_act}; use case có mã: {n_uc}",
             f"LỖI: {len(errors)}"] + errors + [f"GHI CHÚ: {len(notes)}"] + notes
    text = "\n".join(lines)
    print(text)
    OUT.write_text(text + "\n", encoding="utf8")
    return 1 if errors else 0


if __name__ == "__main__":
    sys.exit(main())
