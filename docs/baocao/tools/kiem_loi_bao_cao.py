"""Kiểm lỗi toàn bộ báo cáo DACN: sơ đồ, CSDL và chữ trong file Word. Liệt kê lỗi kèm hướng sửa.

Dùng:
    python kiem_loi_bao_cao.py            chạy một lần, mã thoát = số lỗi
    python kiem_loi_bao_cao.py --lap      chạy lại mỗi khi nguồn (công cụ, ảnh, lược đồ, file Word) thay đổi,
                                          dừng khi không còn lỗi
    python kiem_loi_bao_cao.py --chup     lưu bản chữ của file Word hiện tại làm mốc (chạy ngay sau khi dựng)

Quy trình sửa lỗi: xem docs/baocao/QuyTrinh_SuaLoi.md.
"""
import glob
import json
import os
import re
import subprocess
import sys
import time
from pathlib import Path

from docx import Document
from docx.oxml.ns import qn

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
APP = ROOT / "t"
DOCX = Path(os.environ.get("BAOCAO_OUT", r"C:\Users\ACER\Downloads\BaoCao_DACN_Tuan1-6_sua_v2.docx"))
MOC = HERE.parent / "_ban_dung_gan_nhat.txt"          # bản chữ lúc dựng gần nhất (không đưa vào git)
SCHEMA = json.loads((HERE / "luoc_do.json").read_text(encoding="utf8"))

# Chữ không được xuất hiện trong báo cáo (sổ yêu cầu A1–A8 và các lần rà sau).
CAM = [
    (r"Luxe|luxehaven", "A6: tên ứng dụng nội bộ"),
    (r"quy ước đặt tên|tiền tố|hậu tố", "A3: quy ước đặt tên"),
    (r"[Bb]iểu diễn", "D3: cột Biểu diễn"),
    (r"[∀∃⇒⇔∧∨∈∉⊆]", "A2: ký hiệu hình thức"),
    (r"\b(quản lí|xử lí|Quản lí|Xử lí)\b|tòa|Tòa|hóa đơn|Hóa đơn", "chính tả (dùng 'lý', 'toà', 'hoá')"),
    (r"\bghi log\b|q4_k_m|\bTB\b", "chữ tắt, chữ kỹ thuật khó hiểu"),
    (r"(^|\s)[24][a-c]\. |(^|\s)E[1-4]\. ", "ký hiệu luồng thay thế, ngoại lệ (2a, E1)"),
    (r"\bCREATE |\bSELECT |RAISE |plpgsql", "B6: mã SQL trong bài"),
    (r"giảng viên.*nhận xét|theo góp ý của|Claude", "A1: nội dung trao đổi nội bộ"),
]
TAC_GIA_VIET = ("Bộ Xây dựng", "Quốc hội")
KHONG_PHAI_TEN_CSDL = {"ASP", "NET", "PostgreSQL", "VietQR", "GitHub", "JavaScript", "QLoRA", "LoRA", "Qwen3",
                       "Ollama", "Npgsql", "NeurIPS", "Unsloth", "Kaggle", "LichSu", "MoSCoW", "TrangThaiYeuCau",
                       "LoaiSuCo", "TrangThaiHoaDon", "TrangThaiThanhToan", "CachTinh", "ToaNha", "CanHo", "IoT"}


class Loi(list):
    def them(self, nhom, noi, viec, sua):
        self.append((nhom, noi, viec, sua))


# ----------------------------------------------------------------------------------------- sơ đồ
def kiem_so_do(loi):
    r = subprocess.run([sys.executable, str(HERE / "kiem_tra_so_do.py")], capture_output=True, text=True,
                       encoding="utf-8", cwd=HERE)
    for dong in r.stdout.splitlines():
        m = re.match(r"\[(.+?)\] (.*)", dong.strip())
        if m:
            loi.them("Sơ đồ", m.group(1), m.group(2), "sửa so_do_v2.py rồi chạy lại so_do_v2.py")
    if r.returncode and not any(x[0] == "Sơ đồ" for x in loi):
        loi.them("Sơ đồ", "kiem_tra_so_do", (r.stdout + r.stderr).strip()[-300:], "sửa so_do_v2.py")


# ----------------------------------------------------------------------------------------- CSDL
def anh_xa_cot():
    """(bảng, cột) -> thuộc tính C# đọc từ AppDbContext."""
    src = (APP / "Data" / "AppDbContext.cs").read_text(encoding="utf8")
    out, bang = {}, None
    for dong in src.splitlines():
        m = re.search(r'ToTable\("(\w+)"', dong)
        if m:
            bang = m.group(1)
        m = re.search(r"Property\(\w+ => \w+\.(\w+)\)\.HasColumnName\(\"(\w+)\"\)", dong)
        if m and bang:
            out[(bang, m.group(2))] = m.group(1)
    return out


def dong_ma_nguon():
    bo = ("AppDbContext.cs", "SeedData.cs", os.sep + "Migrations" + os.sep, os.sep + "obj" + os.sep,
          os.sep + "Entities" + os.sep)
    for f in glob.glob(str(APP / "**" / "*.cs"), recursive=True) + glob.glob(str(APP / "**" / "*.cshtml"),
                                                                             recursive=True):
        if any(b in f for b in bo):
            continue
        for d in open(f, encoding="utf-8-sig", errors="ignore"):
            yield f, d


def kiem_csdl(loi):
    """Khoá ngoại phải mang nghĩa nghiệp vụ: được dùng để lọc, so sánh, kiểm tra ở đâu đó, không chỉ nhập và hiển thị."""
    ax = anh_xa_cot()
    dong = list(dong_ma_nguon())
    dung = re.compile(r"Where\(|Any\(|All\(|Count\(|==|!=|GroupBy|Join\(|\.Contains\(|First\w*\(|Single\w*\(")
    for bang, t in SCHEMA.items():
        for c in t["columns"]:
            if not c.get("fk") or c.get("pk"):
                continue
            prop = ax.get((bang, c["name"]))
            if not prop:
                continue
            nghiep_vu = [f for f, d in dong if re.search(rf"\b{prop}\b", d) and dung.search(d)]
            if not nghiep_vu:
                loi.them("CSDL", f"{bang}.{c['name']}",
                         "khoá ngoại chỉ được nhập và hiển thị, không quy tắc, truy vấn nào dùng",
                         "bỏ cột (migration) và bỏ mối kết hợp ở ERD, RD, sơ đồ lớp, Bảng 2.1–2.5; hoặc thêm quy "
                         "tắc dùng nó rồi ghi quy tắc vào Bảng 1.8")


# ----------------------------------------------------------------------------------------- chữ trong Word
def doan(doc):
    """Các đoạn (bỏ mục lục), kèm đánh dấu đoạn nằm trong bảng."""
    out = []
    for p in doc.element.body.iter(qn("w:p")):
        if p.find(".//" + qn("w:hyperlink")) is not None or p.find(".//" + qn("w:instrText")) is not None:
            continue
        t = "".join(x.text or "" for x in p.iter(qn("w:t"))).strip()
        if t:
            out.append(t)
    return out


def bang_theo_chu_thich(doc):
    """{chú thích bảng: [[ô...], ...]}"""
    out, cap = {}, None
    T = lambda e: "".join(x.text or "" for x in e.iter(qn("w:t"))).strip()  # noqa: E731
    for el in doc.element.body.iterchildren():
        if el.tag == qn("w:p"):
            t = T(el)
            if re.match(r"Bảng \d+\.\d+\. ", t) and el.find(".//" + qn("w:hyperlink")) is None \
                    and el.find(".//" + qn("w:instrText")) is None:
                cap = t
        elif el.tag == qn("w:tbl") and cap:
            out[cap] = [[T(tc) for tc in tr.findall(qn("w:tc"))] for tr in el.iter(qn("w:tr"))]
            cap = None
    return out


def kiem_chu(loi):
    if not DOCX.exists():
        loi.them("Chữ", str(DOCX), "chưa có file Word", "chạy sua_bao_cao_v2.py")
        return
    doc = Document(str(DOCX))
    L = doan(doc)
    BANG = bang_theo_chu_thich(doc)
    c1 = next(i for i, t in enumerate(L) if t.startswith("CHƯƠNG 1."))
    r0 = max(i for i, t in enumerate(L) if t == "TÀI LIỆU THAM KHẢO")
    than = L[c1:r0]

    # người dùng sửa tay
    if MOC.exists():
        cu = MOC.read_text(encoding="utf8").split("\n")
        if cu != L:
            khac = [x for x in set(L) ^ set(cu)][:5]
            loi.them("Word", "file Word", f"file đã bị sửa tay sau lần dựng gần nhất ({len(set(L) ^ set(cu))} đoạn "
                     f"khác), ví dụ: {[k[:50] for k in khac]}",
                     "đưa chỗ sửa vào công cụ (gon_bao_cao) TRƯỚC khi dựng lại, nếu không sẽ bị đè")

    # chữ cấm
    for i, t in enumerate(than):
        for mau, ly_do in CAM:
            if re.search(mau, t):
                loi.them("Chữ", t[:60], ly_do, "sửa chữ qua gon_bao_cao.SUA_SAU / DAN_THAY")

    # tham chiếu hình, bảng, mục
    cap = {(m.group(1), m.group(2)) for t in than for m in [re.match(r"(Hình|Bảng) (\d+\.\d+)\. ", t)] if m}
    muc = {m.group(1) for t in than for m in [re.match(r"(\d+\.\d+(?:\.\d+)?)\. \S", t)] if m}
    for t in than:
        for m in re.finditer(r"(Hình|Bảng) (\d+\.\d+)(?!\d)", t):
            if (m.group(1), m.group(2)) not in cap:
                loi.them("Tham chiếu", t[:60], f"{m.group(0)} không tồn tại", "sửa số trong câu")
        for m in re.finditer(r"\b(Bảng|Hình) (\d+)\.(\d+), (\d+)\.(\d+)", t):
            for a, b in ((m.group(2), m.group(3)), (m.group(4), m.group(5))):
                if (m.group(1), f"{a}.{b}") not in cap:
                    loi.them("Tham chiếu", t[:60], f"{m.group(1)} {a}.{b} không tồn tại", "sửa số trong câu")
        for m in re.finditer(r"mục (\d+\.\d+(?:\.\d+)?)", t):
            if m.group(1) not in muc:
                loi.them("Tham chiếu", t[:60], f"mục {m.group(1)} không tồn tại", "sửa số mục")
    for loai, so in sorted(cap):
        ten = f"{loai} {so}"
        if not any(re.search(rf"{loai} (\d+\.\d+, )*{re.escape(so)}(?!\d)", t) and not t.startswith(ten + ".")
                   for t in than):
            loi.them("Tham chiếu", ten, "bảng/hình không được câu văn nào nhắc tới",
                     "thêm câu dẫn (gon_bao_cao.DAN_THAY / DAN_CHEN)")

    # mã được định nghĩa
    dinh_nghia = set()
    for c, rows in BANG.items():
        for r in rows[1:]:
            if r and re.fullmatch(r"(BR-\w+-\d+|FR-\d+|NFR-\d+|RB\d+|LB\d+|TG\d+|FN\d+)", r[0]):
                dinh_nghia.add(r[0])
    sys.path.insert(0, str(HERE))
    import so_do_v2 as sd   # noqa: E402
    uc = {re.match(r"(UC\d+b?)", n["name"]).group(1) for n in sd.use_case_overview()["nodes"]
          if n.get("kind") == "usecase"}
    dinh_nghia |= uc
    for t in than:
        for m in re.finditer(r"\b(BR-[A-Z]+-\d+|FR-\d+|NFR-\d+|RB\d+|LB\d+|TG\d+|FN\d+|UC\d+b?)\b", t):
            if m.group(1) not in dinh_nghia:
                loi.them("Mã", t[:60], f"{m.group(1)} chưa được định nghĩa ở bảng hay sơ đồ nào", "sửa mã")

    # danh mục viết tắt: mọi chữ trong danh mục đều được dùng
    i0 = L.index("DANH MỤC CÁC KÝ HIỆU, CÁC CHỮ VIẾT TẮT")
    i1 = L.index("DANH MỤC CÁC BẢNG")
    noi = " ".join(than)
    for viet_tat in L[i0 + 3:i1:2]:
        for x in re.split(r"\s*/\s*", viet_tat):
            if not re.search(rf"(?<![\w-]){re.escape(x)}", noi):
                loi.them("Viết tắt", viet_tat, f"{x} có trong danh mục nhưng không dùng trong bài", "bỏ khỏi danh mục")

    # tài liệu tham khảo
    refs = [t for t in L[r0 + 1:] if re.match(r"\[\d+\] ", t)]
    so = [int(re.match(r"\[(\d+)\]", t).group(1)) for t in refs]
    if so != list(range(1, len(refs) + 1)):
        loi.them("Tài liệu", "danh sách", "số thứ tự không liên tục", "gon_bao_cao.xep_tai_lieu")
    khoa = [(0 if t.split("] ", 1)[1].startswith(TAC_GIA_VIET) else 1, t.split("] ", 1)[1].lower()) for t in refs]
    if khoa != sorted(khoa):
        loi.them("Tài liệu", "danh sách", "chưa xếp tiếng Việt trước, ABC", "gon_bao_cao.xep_tai_lieu")
    trich = {int(x) for x in re.findall(r"\[(\d+)\]", noi)}
    for n in range(1, len(refs) + 1):
        if n not in trich:
            loi.them("Tài liệu", f"[{n}]", "không được trích trong bài", "trích hoặc bỏ tài liệu")
    for n in trich - set(range(1, len(refs) + 1)):
        loi.them("Tài liệu", f"[{n}]", "trích tới tài liệu không có", "sửa số trích dẫn")

    # số bảng, số thực thể khớp CSDL; Bảng 2.1 khớp các bảng nghiệp vụ
    nghiep_vu = {k for k in SCHEMA}
    n_n = {k for k, v in SCHEMA.items() if sum(1 for c in v["columns"] if c.get("pk")) > 1 or k == "CuTru"}
    for t in than:
        for m in re.finditer(r"(\d+) bảng\b", t):
            if "thu phí" not in t[max(0, m.start() - 40):m.start()] and int(m.group(1)) not in (len(nghiep_vu), 6):
                loi.them("Số liệu", t[:60], f"{m.group(0)} khác số bảng trong CSDL ({len(nghiep_vu)})", "sửa số")
        for m in re.finditer(r"(\d+) thực thể", t):
            if int(m.group(1)) != len(nghiep_vu) - len(n_n):
                loi.them("Số liệu", t[:60], f"{m.group(0)} khác {len(nghiep_vu) - len(n_n)}", "sửa số")
    b21 = next((v for k, v in BANG.items() if "Thực thể, mối kết hợp" in k), [])
    ten21 = {r[0] for r in b21[1:]}
    ten_csdl = {re.sub(r"(?<!^)(?=[A-Z])", "_", k).upper().replace("__", "_") for k in nghiep_vu}
    for x in ten_csdl - ten21:
        loi.them("Số liệu", "Bảng 2.1", f"thiếu {x}", "thêm dòng ở noi_dung_ch2.py")
    for x in ten21 - ten_csdl:
        loi.them("Số liệu", "Bảng 2.1", f"{x} không còn trong CSDL", "bỏ dòng ở noi_dung_ch2.py")

    # tên bảng, cột viết liền trong bài phải có trong CSDL
    cot = {c["name"] for v in SCHEMA.values() for c in v["columns"]} | set(SCHEMA)
    for t in than:
        for m in re.finditer(r"\b([A-Z][a-z]+(?:[A-Z][a-z]*)+(?:_[A-Z][a-zA-Z]+)?)\b", t):
            w = m.group(1)
            if w not in cot and w not in KHONG_PHAI_TEN_CSDL and not re.search(r"[À-ỹ]", w):
                loi.them("Tên CSDL", t[:60], f"{w} không phải bảng hay cột trong CSDL", "sửa tên")


def chay():
    loi = Loi()
    kiem_so_do(loi)
    kiem_csdl(loi)
    kiem_chu(loi)
    return loi


def in_loi(loi):
    if not loi:
        print("SẠCH: 0 lỗi")
        return
    nhom = {}
    for x in loi:
        nhom.setdefault(x[0], []).append(x)
    for k, v in nhom.items():
        print(f"\n== {k}: {len(v)} lỗi")
        for _, noi, viec, sua in v:
            print(f"  - [{noi}] {viec}\n      → {sua}")
    print(f"\nTỔNG: {len(loi)} lỗi")


def dau_vet():
    files = [DOCX, HERE / "luoc_do.json"] + list(HERE.glob("*.py")) + list((HERE.parent / "images_v2").glob("*.png"))
    return tuple(f.stat().st_mtime if f.exists() else 0 for f in files)


def main():
    sys.stdout.reconfigure(encoding="utf-8")
    if "--chup" in sys.argv:
        MOC.write_text("\n".join(doan(Document(str(DOCX)))), encoding="utf8")
        print("đã lưu mốc", MOC)
        return 0
    if "--lap" not in sys.argv:
        loi = chay()
        in_loi(loi)
        return len(loi)
    vong = 0
    while True:
        vong += 1
        print(f"\n######## vòng {vong} – {time.strftime('%H:%M:%S')}")
        loi = chay()
        in_loi(loi)
        if not loi:
            return 0
        moc = dau_vet()
        print("… chờ sửa (công cụ chạy lại khi file nguồn hoặc file Word thay đổi)")
        while dau_vet() == moc:
            time.sleep(3)


if __name__ == "__main__":
    sys.exit(main())
