"""Rà văn phong file báo cáo .docx: tên cột đã bỏ còn sót, ngôi xưng lẫn lộn, câu quá dài.

Dùng: python ra_van_phong.py <file.docx> [--dai 40]
Bỏ qua trang bìa, lời cảm ơn, lời cam đoan (nơi "chúng em" là đúng thể thức) và các đoạn giải thích việc bỏ cột.
"""
import re
import sys

import docx

OLD_COLS = ["TinhTrangThue", "SlugKhuVuc", "SlugDuAn", "SlugToaNha", "DiaChiToaNha", "NhanTang", "MoTaQuyen",
            "BieuTuong", "CheDoTinhPhi", "DonGiaMacDinh", "NgayChotKyMacDinh", "TieuThu ", "DonGiaChiSo",
            "ThanhTienChiSo", "LaHoaDonDinhKy", "DonViTienTe", "PhienBan", "AnhSuCo", "LichSuTrangThaiSuCo",
            "MucSla", "MucDoUuTien", "CuDanId", "ToaNhaKhuVucChungId", "KetQuaAiJson",
            "TinhPhiKhachThue", "LoaiBaoTri", "TrangThaiSuCo", "KhoaBang", "SoNgayTreHan", ".DanhGia", "ELEC", "WATER",
            "Controller", "Async(", "AppDbContext"]
# Nơi được phép nhắc tên cũ: bảng/đoạn giải thích thay đổi (mục 2.1.3).
ALLOW = ("Từng lưu ở", "Nhóm cũng bỏ các cột", "Bỏ CuDan", "Bỏ ToaNha", "Bỏ cả hai", "Suy ra từ CuTru",
         "ChiSoDichVu chỉ giữ", "Bỏ LaHoaDonDinhKy", "Bỏ CheDoTinhPhi", "Bỏ ở cả hai")
PRONOUN = re.compile(r"\b(chúng em|chúng tôi|chúng ta|bọn em|tôi|em|mình)\b", re.I)
SKIP_UNTIL = re.compile(r"^(MỤC LỤC|CHƯƠNG 1)")


def paragraphs(d):
    for p in d.paragraphs:
        yield "p", p.text
    for t in d.tables:
        for r in t.rows:
            for c in r.cells:
                yield "t", c.text


def main():
    path = sys.argv[1]
    limit = int(sys.argv[sys.argv.index("--dai") + 1]) if "--dai" in sys.argv else 40
    d = docx.Document(path)
    body = False
    seen = set()
    old, pron, long_ = [], [], []
    for kind, text in paragraphs(d):
        if not body and kind == "p" and text.startswith("CHƯƠNG 1"):
            body = True
        if not body or text in seen or not text.strip():
            continue
        seen.add(text)
        if not any(a in text for a in ALLOW):
            old += [(w.strip(), text) for w in OLD_COLS if w in text]
        pron += [(m.group(0), text) for m in PRONOUN.finditer(text)]
        if kind == "p":
            for s in re.split(r"(?<=[.!?])\s+", text):
                if len(s.split()) > limit:
                    long_.append(s)
    print(f"Tên cột cũ còn sót: {len(old)}")
    for w, t in old:
        print("  ", w, "|", t[:160])
    print(f"Ngôi xưng khác 'nhóm': {len(pron)}")
    for w, t in pron:
        print("  ", w, "|", t[:160])
    print(f"Câu dài hơn {limit} chữ: {len(long_)}")
    for s in long_:
        print("  ", len(s.split()), "|", s)


if __name__ == "__main__":
    main()
