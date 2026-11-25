"""Cắt gọn báo cáo theo RaSoat_DoDai_BaoCao_20261125.md: các bước chạy sau fix_text của sua_bao_cao_v2.

- gop_uc_vao_fr: thêm cột Use case vào Bảng 1.11 rồi bỏ Bảng 1.13 (và mục 1.8.2) vì trùng Hình 1.6 + Bảng 1.11.
- bo_bang_chuong3: bỏ Bảng 3.8 (chép lại Hình 3.7), 3.9 (giải thích tên thao tác hiển nhiên), 3.10 (mô tả việc làm),
  rút mục 3.6 còn một đoạn.
- danh_so_lai: đánh số lại bảng theo thứ tự xuất hiện trong từng chương, sửa mọi tham chiếu "Bảng x.y" theo.
"""
import copy
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
            "tắc nghiệp vụ có ràng buộc hoặc bước kiểm tra tương ứng. ERD, lược đồ quan hệ và sơ đồ lớp được đối chiếu với "
            "cơ sở dữ liệu bằng chương trình kiểm tra trước khi đưa vào báo cáo.")


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


# ====================================================================== UC03, UC16 về Chương 1
# Hình use case chi tiết đặt ngay trên bảng đặc tả; đặc tả bỏ phần hình đã thể hiện (tác nhân, quan hệ include/extend).
MO_TA_UC = {
    "UC03": "Hình 3.2: UC03 luôn bao gồm UC07 Phân loại sự cố (tác nhân phụ Mô hình AI) và UC08 Điều phối sự cố (tác "
            "nhân phụ Dịch vụ email); đính kèm ảnh hiện trạng là phần mở rộng. Hỏi lại khi mô tả mơ hồ và các thư cảnh "
            "báo là bước bên trong UC07, UC08 nên ghi ở luồng thay thế của bảng đặc tả.",
    "UC16": "Hình 3.4: cập nhật tiến độ luôn gồm xem việc được giao (UC15) và ghi kết quả xử lý; tạm dừng xử lý (khi "
            "chờ vật tư, hẹn lịch) và tải ảnh nghiệm thu là phần mở rộng.",
}
DAC_TA_UC = {
    "UC03": [
        ("Tiền điều kiện", ["Cư dân đã đăng nhập và đang cư trú tại căn (BR-SC-01)."]),
        ("Hậu điều kiện", ["Có một yêu cầu trạng thái Mới gửi hoặc Đã tiếp nhận, có hạn SLA, có dòng lịch sử “Tạo yêu "
                           "cầu”."]),
        ("Luồng chính", ["1. Cư dân nhập tiêu đề, mô tả; chọn trong căn hoặc khu vực chung; có thể chọn loại và mức "
                         "ưu tiên.",
                         "2. Hệ thống phân loại (UC07) và gộp với lựa chọn của cư dân (BR-SC-09).",
                         "3. Hệ thống cấp số yêu cầu, tính hạn SLA, lưu yêu cầu, ảnh và dòng lịch sử đầu.",
                         "4. Hệ thống điều phối (UC08): giao thợ đúng chuyên môn ít việc nhất, gửi thư giao việc "
                         "(BR-SC-12).",
                         "5. Cư dân thấy chi tiết yêu cầu."]),
        ("Luồng thay thế", ["2a. AI không phản hồi trong thời gian chờ: giữ lựa chọn của cư dân, cảnh báo quản trị "
                            "viên (BR-SC-10).",
                            "4a. Mô tả mơ hồ: không giao thợ; cư dân thấy câu hỏi lại, quản lý nhận thư “Cần hỏi lại "
                            "cư dân”.",
                            "4b. Không có thợ chuyên trách: quản lý nhận thư “Cần phân công” kèm nhóm xử lý gợi ý.",
                            "4c. Sự cố Khẩn cấp: quản lý nhận thêm thư “Cần theo dõi”."]),
        ("Ngoại lệ", ["E1. Cư dân chưa cư trú ở căn nào: báo lỗi, không cho tạo.",
                      "E2. Chọn khu vực chung nhưng không ghi vị trí: báo lỗi tại ô Vị trí.",
                      "E3. Hai người gửi cùng lúc bị trùng số yêu cầu: cấp lại số rồi lưu.",
                      "E4. Gửi thư lỗi: chỉ ghi log, yêu cầu đã lưu không bị ảnh hưởng."]),
    ],
    "UC16": [
        ("Tiền điều kiện", ["Yêu cầu được giao cho chính kỹ thuật viên đang đăng nhập (BR-SC-04)."]),
        ("Hậu điều kiện", ["Trạng thái mới được lưu, có dòng lịch sử; nếu hoàn tất thì có kết quả xử lý."]),
        ("Luồng chính", ["1. Kỹ thuật viên chọn một yêu cầu trong danh sách việc được giao.",
                         "2. Bấm Bắt đầu xử lý (Đã tiếp nhận → Đang xử lý).",
                         "3. Sửa xong, ghi nguyên nhân và cách xử lý, bấm Hoàn tất (Đang xử lý → Đã xử lý).",
                         "4. Hệ thống lưu kết quả và dòng lịch sử; cư dân thấy nút xác nhận."]),
        ("Luồng thay thế", ["2a. Chờ vật tư hoặc hẹn lịch: bấm Tạm dừng và ghi lý do; khi tiếp tục, hạn SLA dời "
                            "thêm đúng thời gian đã dừng (BR-SC-07)."]),
        ("Ngoại lệ", ["E1. Mở yêu cầu không giao cho mình: trả về không tìm thấy.",
                      "E2. Hoàn tất khi chưa ghi kết quả: bị chặn (BR-SC-06).",
                      "E3. Hai người cùng sửa một yêu cầu: người lưu sau được yêu cầu tải lại (mục 4.5.4)."]),
    ],
}
DAN_1_8_2 = ("Hai use case trọng tâm được vẽ chi tiết rồi đặc tả. Use case gốc nối với tác nhân chính; «include» là "
             "phần luôn chạy mỗi khi use case gốc chạy, mũi tên đi từ use case gốc; «extend» là phần chỉ chạy khi thoả "
             "điều kiện, mũi tên đi về use case được mở rộng. Bảng đặc tả ghi điều kiện, các bước và ngoại lệ mà hình "
             "không thể hiện.")
DAN_3_2 = ("Sơ đồ use case tổng quát (Hình 1.6) cho biết tác nhân nào dùng chức năng nào; UC03 và UC16 đã được vẽ chi "
           "tiết và đặc tả ở mục 1.8.2, cùng cách đọc «include», «extend». Mục này chi tiết hoá ba use case còn lại: "
           "UC11, UC06 của quy trình sự cố và UC20 của phân hệ thu phí.")


def _p_bat_dau(body, prefix, style=None):
    for p in body.iter(qn("w:p")):
        muc_luc = any(p.find(".//" + qn(t)) is not None for t in ("w:hyperlink", "w:tab", "w:fldChar", "w:instrText"))
        if _text(p).startswith(prefix) and not muc_luc:
            st = p.find(qn("w:pPr") + "/" + qn("w:pStyle"))
            if style is None or (st is not None and st.get(qn("w:val")) == style):
                return p
    raise KeyError(prefix)


def _kieu(p):
    st = p.find(qn("w:pPr") + "/" + qn("w:pStyle"))
    return st.get(qn("w:val")) if st is not None else ""


def _dat_o(tc, lines, set_text):
    ps = tc.findall(qn("w:p"))
    for p in ps[1:]:
        tc.remove(p)
    set_text(ps[0], lines[0])
    prev = ps[0]
    for s in lines[1:]:
        p = copy.deepcopy(ps[0])
        set_text(p, s)
        prev.addnext(p)
        prev = p


def dua_uc_ve_chuong1(body, set_text, new_p):
    """Chuyển hình UC03, UC16 từ Chương 3 về ngay trên bảng đặc tả ở mục 1.8.2, gọn lại đặc tả, sửa lời dẫn hai mục."""
    for so, ma in (("3.2", "UC03"), ("3.4", "UC16")):
        cap = _p_bat_dau(body, f"Hình {so}.", "ChuthichHinh")
        fig = cap.getprevious()
        assert fig.find(".//" + qn("w:drawing")) is not None, so
        mo_ta_cu = _p_bat_dau(body, f"Hình {so}:", "Noidung")
        mo_ta_cu.getparent().remove(mo_ta_cu)
        tcap = next(p for p in body.iter(qn("w:p")) if "Đặc tả " + ma + " –" in _text(p) and _kieu(p) == "ChuthichBang")
        tcap.addprevious(new_p("Noidung", MO_TA_UC[ma]))
        tcap.addprevious(fig)
        tcap.addprevious(cap)
        tbl = tcap.getnext()
        assert tbl.tag == qn("w:tbl"), ma
        moi = dict(DAC_TA_UC[ma])
        for tr in tbl.findall(qn("w:tr"))[1:]:
            tcs = tr.findall(qn("w:tc"))
            muc = _text(tcs[0]).strip()
            if muc in moi:
                _dat_o(tcs[1], moi[muc], set_text)
            else:          # dòng Tác nhân: hình đã thể hiện
                tbl.remove(tr)
    _p_bat_dau(body, "1.8.2. ").addnext(new_p("Noidung", DAN_1_8_2))
    dan = _p_bat_dau(body, "Sơ đồ use case tổng quát (Hình 1.6) cho biết")
    set_text(dan, DAN_3_2)
    nxt = dan.getnext()      # hai gạch đầu dòng «include», «extend» và câu điều kiện mở rộng: đã nêu ở mục 1.8.2
    while nxt is not None and (_text(nxt).startswith("–«") or _text(nxt).startswith("Điều kiện chạy")):
        after = nxt.getnext()
        body.remove(nxt)
        nxt = after


def danh_so_hinh(body, set_text, doi_chuong):
    """Đánh số lại hình theo thứ tự trong từng chương (hình chuyển chương lấy chương mới), sửa mọi tham chiếu."""
    caps = []
    for p in body.iter(qn("w:p")):
        st = p.find(qn("w:pPr") + "/" + qn("w:pStyle"))
        if st is not None and st.get(qn("w:val")) == "ChuthichHinh":
            m = re.match(r"Hình (\d+\.\d+)\.", _text(p))
            if m:
                caps.append(m.group(1))
    moi, dem = {}, {}
    for so in caps:
        c = doi_chuong.get(so, so.split(".")[0])
        dem[c] = dem.get(c, 0) + 1
        moi[so] = f"{c}.{dem[c]}"
    pat = re.compile(r"Hình (\d+\.\d+)(?!\d)")
    treo = set()

    def doi(m):
        if m.group(1) not in moi:
            treo.add(m.group(1))
            return m.group(0)
        return "Hình " + moi[m.group(1)]

    muc_luc = (qn("w:hyperlink"), qn("w:fldChar"), qn("w:instrText"), qn("w:tab"))
    for p in body.iter(qn("w:p")):
        t = _text(p)
        if "Hình " in t and not any(p.find(".//" + tag) is not None for tag in muc_luc):
            n = pat.sub(doi, t)
            if n != t:
                set_text(p, n)
    return sorted(treo), sum(1 for k, v in moi.items() if k != v)
