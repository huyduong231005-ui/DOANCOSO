"""Cắt gọn báo cáo theo RaSoat_DoDai_BaoCao_20261125.md: các bước chạy sau fix_text của sua_bao_cao_v2.

- gop_uc_vao_fr: thêm cột Use case vào Bảng 1.11 rồi bỏ Bảng 1.13 (và mục 1.8.2) vì trùng Hình 1.6 + Bảng 1.11.
- bo_bang_chuong3: bỏ Bảng 3.8 (chép lại Hình 3.7), 3.9 (giải thích tên thao tác hiển nhiên), 3.10 (mô tả việc làm),
  rút mục 3.6 còn một đoạn.
- danh_so_lai: đánh số lại bảng theo thứ tự xuất hiện trong từng chương, sửa mọi tham chiếu "Bảng x.y" theo.
"""
import re

from docx.oxml.ns import qn

# Sửa nguyên ô (không phải chuỗi con) trong các bảng của nhóm: tên mã tiếng Anh, tên cột cũ.
O_SUA = {
    "Bảng 1.3.": {"Tenant": "Cư dân", "Manager": "Quản lý", "Admin": "Quản trị",
                  "Electrician, Plumber, AcTechnician, Carpenter, Glazier":
                      "Thợ điện, thợ nước, thợ máy lạnh, thợ mộc, thợ nhôm kính",
                  "InvoiceGenerationService": "Tác vụ nền theo lịch"},
    "Bảng 1.10.": {"DanhGia": "DiemDanhGia", "CanHo.TinhTrangThue": "CuTru, CanHo"},
}

DOAN_3_6 = ("Các mô hình được đối chiếu chéo: mỗi use case có đặc tả và luồng xử lý tương ứng; mỗi lớp ứng với một "
            "bảng, mỗi liên kết ứng với một khoá ngoại; mỗi thông điệp tuần tự gọi thao tác có trong lớp nhận; mỗi quy "
            "tắc nghiệp vụ có ràng buộc hoặc bước kiểm tra tương ứng. Phần đối chiếu ERD, sơ đồ lớp với cơ sở dữ liệu và "
            "các luật vẽ được chạy tự động trước khi đưa hình vào báo cáo.")


def _text(el):
    return "".join(t.text or "" for t in el.iter(qn("w:t")))


def _caption(body, prefix):
    for p in body.iter(qn("w:p")):
        st = p.find(qn("w:pPr") + "/" + qn("w:pStyle"))
        if st is not None and st.get(qn("w:val")) == "ChuthichBang" and _text(p).startswith(prefix):
            return p
    raise KeyError(prefix)


def _bo(el):
    """Bỏ một phần tử; nếu ngay sau là đoạn trống thì bỏ luôn để không thừa khoảng trắng."""
    nxt = el.getnext()
    el.getparent().remove(el)
    if nxt is not None and nxt.tag == qn("w:p") and not _text(nxt).strip() and nxt.find(".//" + qn("w:drawing")) is None:
        nxt.getparent().remove(nxt)


def _bo_bang(body, prefix):
    cap = _caption(body, prefix)
    tbl = cap.getnext()
    assert tbl.tag == qn("w:tbl"), prefix
    cap.getparent().remove(cap)
    _bo(tbl)


def ngoai_chuong2(body):
    """Các đoạn không thuộc Chương 2 (Chương 2 do noi_dung_ch2 sinh, không áp phép thay chữ của nhóm)."""
    trong, ch2 = False, set()
    for el in body.iterchildren():
        t = _text(el)
        if el.tag == qn("w:p") and t.startswith("CHƯƠNG 2."):
            trong = True
        elif el.tag == qn("w:p") and t.startswith("CHƯƠNG 3."):
            trong = False
        if trong:
            ch2.update(el.iter(qn("w:p")))
    return ch2


def sua_o(body, set_text):
    for prefix, m in O_SUA.items():
        tbl = _caption(body, prefix).getnext()
        for p in tbl.iter(qn("w:p")):
            if _text(p).strip() in m:
                set_text(p, m[_text(p).strip()])


def gop_uc_vao_fr(body, new_table, set_text):
    t13 = _caption(body, "Bảng 1.13.").getnext()
    fr_uc = {}
    for tr in t13.findall(qn("w:tr"))[1:]:
        uc, _, _, frs = [_text(tc).strip() for tc in tr.findall(qn("w:tc"))]
        for fr in re.findall(r"FR-\d+", frs):
            fr_uc.setdefault(fr, []).append(uc)
    t11 = _caption(body, "Bảng 1.11.").getnext()
    rows = [[_text(tc).strip() for tc in tr.findall(qn("w:tc"))] for tr in t11.findall(qn("w:tr"))]
    head, rows = rows[0], rows[1:]
    head = head[:2] + ["Use case"] + head[2:]
    rows = [r[:2] + [", ".join(fr_uc.get(r[0], [])) or "—"] + r[2:] for r in rows]
    t11.addprevious(new_table(head, rows, [1.3, 3.1, 1.6, 1.5, 1.1, 2.8]))
    body.remove(t11)
    # Bỏ mục 1.8.2 (tiêu đề + Bảng 1.13); mục 1.8.3 thành 1.8.2.
    for p in list(body.iter(qn("w:p"))):
        t = _text(p)
        if t.startswith("1.8.2. Danh sách use case") and "\t" not in t:
            body.remove(p)
        elif t.startswith("1.8.3. ") and "\t" not in t:
            set_text(p, "1.8.2. " + t[len("1.8.3. "):])
    _bo_bang(body, "Bảng 1.13.")


def bo_bang_chuong3(body, set_text):
    for prefix in ("Bảng 3.8.", "Bảng 3.9.", "Bảng 3.10."):
        _bo_bang(body, prefix)
    for p in list(body.iter(qn("w:p"))):
        t = _text(p)
        if t.startswith("Yêu cầu của giai đoạn này là mô hình phải nhất quán"):
            set_text(p, DOAN_3_6)
        elif t.startswith("Qua lần đối chiếu cuối, nhóm đã sửa"):
            _bo(p)


def danh_so_lai(body, set_text):
    """Đánh số lại bảng theo thứ tự trong từng chương và sửa mọi tham chiếu. Trả về số tham chiếu treo."""
    caps = []
    for p in body.iter(qn("w:p")):
        st = p.find(qn("w:pPr") + "/" + qn("w:pStyle"))
        if st is not None and st.get(qn("w:val")) == "ChuthichBang":
            m = re.match(r"Bảng (\d+)\.(\d+)\.", _text(p))
            if m:
                caps.append((p, m.group(1), m.group(2)))
    moi, dem = {}, {}
    for p, c, n in caps:
        dem[c] = dem.get(c, 0) + 1
        moi[f"{c}.{n}"] = f"{c}.{dem[c]}"
    pat = re.compile(r"Bảng (\d+\.\d+)(?!\d)")
    treo = set()

    def doi(m):
        if m.group(1) not in moi:
            treo.add(m.group(1))
            return m.group(0)
        return "Bảng " + moi[m.group(1)]

    muc_luc = (qn("w:hyperlink"), qn("w:fldChar"), qn("w:instrText"), qn("w:tab"))
    for p in body.iter(qn("w:p")):
        t = _text(p)
        if "Bảng " in t and not any(p.find(".//" + tag) is not None for tag in muc_luc):
            # dòng mục lục có hyperlink/trường/tab: Word tự sinh lại, không được ghi đè
            n = pat.sub(doi, t)
            if n != t:
                set_text(p, n)
    return sorted(treo), sum(1 for k, v in moi.items() if k != v)
