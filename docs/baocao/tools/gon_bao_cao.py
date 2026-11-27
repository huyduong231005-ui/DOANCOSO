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
    "UC03": "Hình 3.2: cư dân luôn nhập mô tả và chọn nơi xảy ra sự cố (khu vực chung thì ghi thêm vị trí); chọn "
            "loại, mức ưu tiên và đính kèm ảnh là tuỳ chọn. UC03 luôn bao gồm UC07 Phân loại sự cố, trong đó Mô hình "
            "AI xác định loại, mức ưu tiên và nhận diện sự cố nguy hiểm, và UC08 Điều phối sự cố, trong đó hệ thống "
            "luôn tìm kỹ thuật viên phù hợp. Giao việc là phần mở rộng, chỉ xảy ra khi có thợ phù hợp và mô tả rõ; đã "
            "giao thì luôn gửi thư thông báo giao việc qua Dịch vụ email. Không giao được thì báo quản lý phân công; "
            "sự cố khẩn cấp thì báo thêm quản lý.",
    "UC16": "Hình 3.4: kỹ thuật viên xem việc được giao (có thể mở chi tiết từng yêu cầu), bắt đầu rồi hoàn tất xử "
            "lý; hoàn tất luôn ghi kết quả, có thể kèm ảnh nghiệm thu. Tạm dừng (luôn ghi lý do) và tiếp tục xử lý chỉ "
            "xảy ra khi phải chờ vật tư hoặc hẹn lịch.",
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
        ("Luồng thay thế", ["Ở bước 2, nếu AI không phản hồi trong thời gian chờ: giữ lựa chọn của cư dân, cảnh báo quản trị "
                            "viên (BR-SC-10).",
                            "Ở bước 4, nếu mô tả mơ hồ: không giao thợ; cư dân thấy câu hỏi lại, quản lý nhận thư “Cần hỏi lại "
                            "cư dân”.",
                            "Ở bước 4, nếu không có thợ chuyên trách: quản lý nhận thư “Cần phân công” kèm nhóm xử lý gợi ý.",
                            "Ở bước 4, nếu sự cố ở mức Khẩn cấp: quản lý nhận thêm thư “Cần theo dõi”."]),
        ("Ngoại lệ", ["Cư dân chưa cư trú ở căn nào: báo lỗi, không cho tạo.",
                      "Chọn khu vực chung nhưng không ghi vị trí: báo lỗi tại ô Vị trí.",
                      "Hai người gửi cùng lúc bị trùng số yêu cầu: cấp lại số rồi lưu.",
                      "Gửi thư lỗi: chỉ ghi lại lỗi, yêu cầu đã lưu không bị ảnh hưởng."]),
    ],
    "UC16": [
        ("Tiền điều kiện", ["Yêu cầu được giao cho chính kỹ thuật viên đang đăng nhập (BR-SC-04)."]),
        ("Hậu điều kiện", ["Trạng thái mới được lưu, có dòng lịch sử; nếu hoàn tất thì có kết quả xử lý."]),
        ("Luồng chính", ["1. Kỹ thuật viên chọn một yêu cầu trong danh sách việc được giao.",
                         "2. Bấm Bắt đầu xử lý (Đã tiếp nhận → Đang xử lý).",
                         "3. Sửa xong, ghi nguyên nhân và cách xử lý, bấm Hoàn tất (Đang xử lý → Đã xử lý).",
                         "4. Hệ thống lưu kết quả và dòng lịch sử; cư dân thấy nút xác nhận."]),
        ("Luồng thay thế", ["Giữa bước 2 và 3, nếu phải chờ vật tư hoặc hẹn lịch: bấm Tạm dừng và ghi lý do; khi tiếp tục, hạn SLA dời "
                            "thêm đúng thời gian đã dừng (BR-SC-07)."]),
        ("Ngoại lệ", ["Mở yêu cầu không giao cho mình: trả về không tìm thấy.",
                      "Hoàn tất khi chưa ghi kết quả: bị chặn (BR-SC-06).",
                      "Hai người cùng sửa một yêu cầu: người lưu sau được yêu cầu tải lại (mục 4.5.4)."]),
    ],
}
DAN_1_8_2 = ("Hai use case trọng tâm được vẽ chi tiết rồi đặc tả. Bảng đặc tả ghi điều kiện, các bước và ngoại lệ mà "
             "hình không thể hiện.")
DAN_3_2 = ("Sơ đồ use case tổng quát (Hình 1.6) cho biết tác nhân nào dùng chức năng nào; UC03 và UC16 đã được vẽ chi "
           "tiết và đặc tả ở mục 1.8.2, cùng cách đọc «include», «extend». Mục này chi tiết hoá ba use case còn lại: "
           "UC11, UC06 của quy trình sự cố và UC20 của phân hệ thu phí.")


def _p_bat_dau(body, prefix, style=None):
    for p in body.iter(qn("w:p")):
        if _text(p).startswith(prefix) and not _la_muc_luc(p):
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


# ====================================================================== use case chỉ ở một chỗ (mục 1.8)
DOAN_UC_MOI = {   # đoạn bắt đầu bằng ... -> nội dung mới (số hình theo bản gốc, đánh số lại sau)
    "Chương này là sản phẩm tuần 4, gồm bộ mô hình thiết kế":
        "Chương này là sản phẩm tuần 4, gồm bộ mô hình thiết kế: kiến trúc, sơ đồ hoạt động, sơ đồ lớp và sơ đồ tuần "
        "tự; use case đã trình bày ở mục 1.8. Mục 3.5 trình bày cách nhóm rà soát sự nhất quán giữa các mô hình.",
    "Đến tuần 4, bộ mô hình thiết kế gồm":
        "Đến tuần 4, bộ mô hình thiết kế gồm: sơ đồ kiến trúc; sơ đồ use case tổng quát, hai sơ đồ use case chi tiết "
        "kèm đặc tả (mục 1.8); ba sơ đồ hoạt động; sơ đồ trạng thái; sơ đồ lớp; bốn sơ đồ tuần tự. ERD và lược đồ "
        "quan hệ ở Chương 2. Các mô hình đã được đối chiếu với nhau theo mục 3.5.",
    "Tuần 6 bổ sung phân hệ thu phí vào bộ mô hình thiết kế":
        "Tuần 6 bổ sung phân hệ thu phí vào bộ mô hình thiết kế: các lớp thu phí trong sơ đồ lớp (Hình 3.8) và sơ đồ tuần tự cư dân "
        "gửi xác nhận chuyển khoản, quản lý duyệt (Hình 3.14). Chương 4 trình bày việc xây dựng và kiểm thử theo quy "
        "trình nghiệp vụ.",
    "–Chương 3 – Thiết kế hệ thống (tuần 4)":
        "–Chương 3 – Thiết kế hệ thống (tuần 4): kiến trúc, sơ đồ hoạt động, lớp, tuần tự và rà soát nhất quán giữa "
        "các mô hình.",
    "–Chương 1 – Tổng quan và phân tích nghiệp vụ":
        "–Chương 1 – Tổng quan và phân tích nghiệp vụ: bối cảnh, vấn đề, AS-IS/TO-BE, quy tắc nghiệp vụ, KPI, SRS, use "
        "case tổng quát và đặc tả hai use case trọng tâm.",
    "–Chương 2 – Phân tích và thiết kế dữ liệu":
        "–Chương 2 – Phân tích và thiết kế dữ liệu: thực thể, ERD, lược đồ quan hệ, chuẩn hoá và ràng buộc toàn vẹn.",
    "1.8. Use case sơ bộ": "1.8. Use case",
    "1.8.2. Đặc tả use case trọng tâm": "1.8.2. Use case trọng tâm",
    # 26/11: mục 4.6 chỉ nói kết quả đối chiếu; ba lỗi tìm được khi đối chiếu chuyển sang đoạn lỗi của mục 4.7
    "Hệ thống có bảng điều khiển cho quản lý (Hình 4.5)":
        "Hệ thống có bảng điều khiển cho quản lý (Hình 4.5) và bốn báo cáo: tình trạng căn theo toà, tài "
        "chính, công nợ phân tuổi (Hình 4.6) và KPI sự cố (Hình 4.7). Báo cáo tình trạng căn đếm căn đang ở, "
        "cho thuê, trống, chưa bàn giao, đang sửa chữa và số còn phải thu; báo cáo tài chính gồm số phát "
        "hành, đã thu, công nợ, thanh toán chờ duyệt và doanh thu sáu tháng. Mỗi số trên báo cáo được tính "
        "lại bằng câu SQL viết riêng, chạy thẳng trên CSDL ngày 25/11/2026 (Bảng 4.7); tất cả khớp. Đã thu "
        "tháng 11 bằng 0 vì dữ liệu mẫu chưa có khoản thanh toán nào trong tháng này.",
}


SUA_SAU = [   # thay chuỗi con, chạy sau cùng trên mọi đoạn (kể cả ô bảng), trừ mục lục
    # Tuần 9: bỏ cột quản lý toà và người tải ảnh (không quy tắc nào dùng)
    # Rà sơ đồ 27/11: tầng đã gộp vào căn; chữ theo các hình đã sửa
    # Hoá đơn phí sửa chữa nối với yêu cầu sự cố (khoá ngoại YeuCauBaoTriId của HoaDon)
    ("Một yêu cầu xảy ra tại một căn hoặc ở khu vực chung của một toà, không đồng thời cả hai.",
     "Một yêu cầu xảy ra tại một căn hoặc ở khu vực chung của một toà, không đồng thời cả hai. Hoá đơn phí sửa chữa "
     "ghi yêu cầu sinh ra nó (mối kết hợp “phát sinh phí”); hoá đơn tháng không gắn yêu cầu nào nên phía hoá đơn "
     "là (0,1)."),
    ("Liên kết với ThanhToan không phải hợp thành vì hoá đơn đã có thanh toán thì không xoá được (BR-TP-05).",
     "Liên kết với ThanhToan không phải hợp thành vì hoá đơn đã có thanh toán thì không xoá được (BR-TP-05). Hoá "
     "đơn phí sửa chữa nối với YeuCauBaoTri sinh ra nó (phiSuaChua)."),
    ("Đánh giá từ 1 đến 5; chi phí sửa chữa không âm; số yêu cầu không trùng.",
     "Đánh giá từ 1 đến 5; chi phí sửa chữa không âm; số yêu cầu không trùng; mỗi yêu cầu có nhiều nhất một hoá "
     "đơn phí sửa chữa chưa huỷ, nên mở lại rồi hoàn tất lần nữa không thu tiền hai lần."),
    ("CHECK (RB05), UNIQUE (SoYeuCau)", "CHECK (RB05), UNIQUE (SoYeuCau), UNIQUE có điều kiện (YeuCauBaoTriId)"),
    ("Khu – Toà – Tầng – Căn", "Khu – Toà – Căn hộ"),
    ("UC08 “Điều phối sự cố” (tác nhân phụ Dịch vụ email).",
     "UC08 “Điều phối sự cố” (tác nhân phụ Dịch vụ email). UC11 “Phân công xử lý sự cố” cũng gửi thư giao việc qua "
     "Dịch vụ email khi quản lý giao tay; UC16 luôn bao gồm UC15 “Xem việc được giao”."),
    ("Không giao được thì báo quản lý phân công; sự cố khẩn cấp thì báo thêm quản lý.",
     "Mô tả mơ hồ thì báo quản lý hỏi lại cư dân; không có thợ phù hợp thì báo quản lý phân công; sự cố khẩn cấp "
     "thì báo thêm quản lý."),
    ("kỹ thuật viên xem việc được giao (có thể mở chi tiết từng yêu cầu), bắt đầu rồi hoàn tất xử lý; hoàn tất luôn",
     "kỹ thuật viên luôn xem việc được giao (có thể mở chi tiết từng yêu cầu); bắt đầu và hoàn tất xử lý là phần mở "
     "rộng vì mỗi lần cập nhật chỉ làm một bước; hoàn tất luôn"),
    ("vì nhật ký phải giữ cả thao tác trên dòng đã bị xoá.",
     "vì nhật ký phải giữ cả thao tác trên dòng đã bị xoá; người thao tác chỉ được ghi mã (NguoiThaoTacId), không "
     "thành khoá ngoại."),
    ("khu vực chung của 0..1 toà (toaNha), có người báo",
     "khu vực chung của 0..1 toà (toaNha), đúng một trong hai nơi (ghi chú {xor}), có người báo"),
    ("NhatKyHeThong ghi mọi thay đổi dữ liệu; không có khoá ngoại", "NhatKyHeThong ghi mọi thay đổi dữ liệu và mã người thao tác; không có khoá ngoại"),
    ("gồm bốn làn: cư dân, hệ thống web, dịch vụ phân loại AI và dịch vụ email.",
     "gồm bốn làn: cư dân, hệ thống web, dịch vụ phân loại AI và dịch vụ email. Đã giao thợ mà sự cố ở mức Khẩn "
     "cấp thì thư được gửi cho cả thợ và quản lý để theo dõi."),
    (" Một người dùng có thể là quản lý của nhiều toà (quan hệ quanLy) và có thể có nhiều lần cư trú (CuTru).",
     " Một người dùng có thể có nhiều lần cư trú (CuTru)."),
    ("; mỗi ảnh, mỗi dòng lịch sử ghi người tải lên hoặc người thực hiện.", "; mỗi dòng lịch sử ghi người thực hiện."),
    ("yêu cầu và use case sơ bộ.", "yêu cầu và use case."),
    ("ràng buộc toàn vẹn (CHECK, trigger, hàm); kiểu dữ liệu và bảng mã", "ràng buộc toàn vẹn (CHECK, trigger, hàm)"),
    (" Đây là kiểm tra thiết kế, việc chạy thử trên cơ sở dữ liệu sau khi đồng bộ lược đồ mới thuộc phần việc tiếp "
     "theo.", ""),
    # Chương 1: tên trạng thái đúng như Bảng 1.7 và giao diện; câu và công thức KPI bằng lời, không tên cột
    ("Đang mở (Mới, Tiếp nhận, Đang xử lý, Tạm dừng) và HanXuLy < hiện tại",
     "Đang mở (Mới gửi, Đã tiếp nhận, Đang xử lý, Tạm dừng) và đã qua hạn xử lý"),
    ("(bảng NguoiDung, quan hệ cư trú CuTru)", "(tài khoản người dùng và thông tin cư trú)"),
    ("có thời điểm giải quyết ≤ HanXuLy /", "giải quyết không muộn hơn hạn xử lý /"),
    ("(thời điểm giải quyết − NgayBao)", "(thời điểm giải quyết − thời điểm báo)"),
    ("(thời điểm tiếp nhận − NgayBao)", "(thời điểm tiếp nhận − thời điểm báo)"),
    ("tiếp nhận trong PhutPhanHoi của mức ưu tiên", "tiếp nhận trong thời hạn phản hồi của mức ưu tiên"),
    ("Trung bình DiemDanhGia (1–5)", "Trung bình điểm đánh giá (1–5)"),
    ("phần có HanThanhToan trước hôm nay", "phần có hạn thanh toán trước hôm nay"),
    # Ký hiệu, chữ người đọc khó hiểu (rà 26/11)
    ("lý do lựa chọn được giải thích ở mục 2.5.", "lý do lựa chọn được giải thích ở mục 2.5. Các mã RB, LB (ràng buộc), "
     "TG (trigger) và FN (hàm) trong cột này được liệt kê ở mục 2.5."),
    ("trả lời 6 câu hỏi Bước 1;", "trả lời 6 câu hỏi nghiệp vụ (Bảng 1.1);"),
    ("Lập hoá đơn tháng theo ngày lập phí của toà, chuyển hoá đơn quá hạn, cộng phí trễ",
     "Lập hoá đơn tháng theo ngày lập phí của toà, chuyển hoá đơn quá hạn, cộng phí trễ; báo quản lý yêu cầu sự cố "
     "quá hạn"),
    ("Lập hoá đơn tháng, chuyển hoá đơn quá hạn, cộng phí trễ; nhắc hạn",
     "Lập hoá đơn tháng, chuyển hoá đơn quá hạn, cộng phí trễ; báo quản lý yêu cầu sự cố quá hạn"),
    ("định dạng GGUF q4_k_m", "nén 4-bit, định dạng GGUF"),
    ("SMTP của Gmail; chế độ ghi log khi chưa cấu hình", "SMTP của Gmail"),
    ("khi chưa có SMTP thì chỉ ghi log để kiểm thử", "khi chưa cấu hình SMTP thì chỉ ghi nội dung thư ra nhật ký chạy "
     "để kiểm thử"),
    ("lỗi gửi thư chỉ ghi log, không làm hỏng", "lỗi gửi thư chỉ được ghi lại, không làm hỏng"),
    ("Cư dân, kỹ thuật viên vào khu quản trị nhận mã 403 và vẫn giữ phiên.", "Cư dân, kỹ thuật viên vào khu quản trị "
     "nhận mã 403 (không có quyền) và vẫn giữ phiên."),
    ("đều nhận mã 404.", "đều nhận mã 404 (không tìm thấy), để không lộ là dữ liệu đó có tồn tại."),
    # Gộp Tầng vào Căn hộ (26/11): bỏ bảng Tang ở mọi chương
    ("Thiết kế CSDL đạt 3NF cho cơ cấu Khu – Toà – Tầng – Căn,", "Thiết kế CSDL đạt 3NF cho cơ cấu Khu – Toà – Căn,"),
    ("hệ thống tự sinh tầng và căn với mã dạng", "hệ thống tự sinh căn cho từng tầng với mã dạng"),
    ("Mã căn trùng trong toà, tầng của toà khác, diện tích âm", "Mã căn trùng, số tầng 0, diện tích âm"),
    # Bản nháp không có chức năng nào tạo ra: không nhắc trong báo cáo
    (" Trạng thái Bản nháp có trong mã nguồn nhưng hiện không có chức năng nào tạo hoá đơn nháp.", ""),
    ("hoá đơn đã phát hành (trừ Bản nháp, Đã huỷ)", "hoá đơn đã phát hành (trừ hoá đơn đã huỷ)"),
    # Chương 4 (rà 25/11): bỏ câu thừa, bỏ thuật ngữ kỹ thuật, khớp mã nguồn và CSDL
    (" Các tài khoản thử dùng email thuộc một tên miền thử của dữ liệu mẫu; Bảng 4.1 chỉ ghi phần tên trước @.", ""),
    ("Mã quyền theo phân hệ và quyền của Quản lý BQL", "Quyền theo phân hệ và quyền của Quản lý BQL"),
    ("projects.view", "Xem khu, toà nhà"), ("projects.manage", "Quản lý khu, toà nhà"),
    ("apartments.view", "Xem căn hộ"), ("apartments.update", "Cập nhật căn hộ"),
    ("residents.view", "Xem cư dân"), ("residents.manage", "Đăng ký, chuyển đi cư dân"),
    ("invoices.view", "Xem hoá đơn"), ("invoices.issue", "Phát hành hoá đơn"),
    ("payments.view", "Xem thanh toán"), ("payments.refund", "Hoàn tiền"),
    ("maintenance.manage", "Xử lý sự cố"), ("users.view", "Xem người dùng"), ("users.manage", "Quản lý người dùng"),
    ("roles.manage", "Quản lý phân quyền"), ("audit.view", "Xem nhật ký"),
    ("chỉ số điện nước chưa có mã quyền riêng", "chỉ số điện nước chưa có quyền riêng"),
    ("mật khẩu và tem bảo mật được che", "mật khẩu và mã bảo mật của tài khoản được che"),
    ("Lưu mật khẩu đã băm và tem bảo mật", "Lưu mật khẩu đã băm và mã bảo mật của tài khoản"),
    ("Mô hình trả về loại Cấp/thoát nước, mức Trung bình, không nguy hiểm; hệ thống đổi loại thành Cấp / thoát nước "
     "và tự giao", "Mô hình trả về loại Cấp / thoát nước, mức Trung bình, không nguy hiểm; hệ thống ghi loại này cho "
     "yêu cầu và tự giao"),
    ("Kết quả ghi ở KetQuaXuLy", "Ghi kết quả xử lý"),
    ("Sau đó nhóm thêm dịch vụ nền nạp sẵn mô hình khi ứng dụng khởi động, không đổi prompt hay tham số sinh; khi mô "
     "hình đã nạp sẵn, một lần phân loại với prompt thật mất 6,2–6,5 giây, dùng 756 đến 1.356 trong 2.048 token ngữ "
     "cảnh.", "Sau đó nhóm cho ứng dụng nạp sẵn mô hình ngay khi khởi động; khi mô hình đã nạp sẵn, một lần phân loại "
     "mất 6,2–6,5 giây."),
    ("Lưu loại mới; hạn xử lý tính lại từ lúc báo", "Lưu loại mới; mức ưu tiên không đổi nên hạn xử lý giữ nguyên"),
    ("Mới gửi → Đã từ chối", "Mới gửi → Từ chối"),
    ("dùng khoá lạc quan theo cột hệ thống xmin của PostgreSQL: lệnh", "dùng khoá lạc quan: lệnh"),
    ("chạy thẳng trên CSDL ngày 18/11/2026", "chạy thẳng trên CSDL ngày 25/11/2026"),
    ("Đối chiếu các báo cáo với SQL ngày 18/11/2026", "Đối chiếu các báo cáo với SQL ngày 25/11/2026"),
    ("Trong lúc đối chiếu, nhóm phát hiện và sửa thêm ba lỗi.", "Lần đối chiếu đầu (18/11) giúp nhóm phát hiện và sửa "
     "thêm ba lỗi."),
    ("706.065.000 / 316.048.734 / 0 / 53.398.530 / 69.174.666", "706.065.000 / 0 / 316.048.734 / 53.398.530 / 69.174.666"),
    ("tính số ngày trễ theo ngày UTC", "tính số ngày trễ theo giờ quốc tế (UTC) thay vì giờ Việt Nam"),
    ("tính mốc theo giờ UTC", "tính mốc theo giờ quốc tế (UTC) thay vì giờ Việt Nam"),
    ("báo cáo tài chính, bảng điều khiển và nhật ký tính mốc theo giờ quốc tế (UTC) thay vì giờ Việt Nam. "
     "Sau khi sửa,",
     "báo cáo tài chính, bảng điều khiển và nhật ký tính mốc theo giờ quốc tế (UTC) thay vì giờ Việt Nam. "
     "Lần đối chiếu báo cáo ngày 18/11 (mục 4.6) tìm thêm ba lỗi, cũng đã sửa: trang hoá đơn của quản lý "
     "không hiện dòng phí trễ nên các dòng cộng không ra tổng; báo cáo tuổi nợ tính số ngày trễ theo giờ "
     "UTC; ô số tiền trên báo cáo tuổi nợ bị cắt khi số dài. Sau khi sửa,"),
    # Người dùng tự sửa trong Word 26/11: cột Hình thức của Bảng 1.6 bỏ mã bất cập (P1)…(P8)
    (" (P1)", ""),
    (" (P2)", ""),
    (" (P3)", ""),
    (" (P4)", ""),
    (" (P5)", ""),
    (" (P6)", ""),
    (" (P7)", ""),
    (" (P8)", ""),
]


def _la_muc_luc(p):
    """Dòng mục lục: có liên kết/trường, hoặc có tab và kết thúc bằng số trang (gạch đầu dòng cũng có tab)."""
    if any(p.find(".//" + qn(t)) is not None for t in ("w:hyperlink", "w:fldChar", "w:instrText")):
        return True
    return p.find(".//" + qn("w:tab")) is not None and re.search(r"[0-9ivx]$", _text(p).strip()) is not None


def bo_muc_uc_chuong3(body, set_text):
    """Use case chỉ trình bày một lần ở mục 1.8 (UC03, UC16 là trọng tâm): bỏ mục 3.2 Use case chi tiết (ba sơ đồ,
    sáu bảng đặc tả), đánh số lại các mục sau của Chương 3, sửa các câu nhắc tới use case."""
    dau = next(p for p in body.iter(qn("w:p")) if _text(p).startswith("3.2. Use case chi tiết") and not _la_muc_luc(p))
    el = dau
    while el is not None and not (el.tag == qn("w:p") and re.match(r"3\.3\. ", _text(el)) and not _la_muc_luc(el)):
        nxt = el.getnext()
        el.getparent().remove(el)
        el = nxt
    for p in list(body.iter(qn("w:p"))):
        if _la_muc_luc(p):
            continue
        t = _text(p)
        m = re.match(r"3\.([3-9])\. ", t)
        if m:
            set_text(p, f"3.{int(m.group(1)) - 1}. " + t[len(m.group(0)):])
            continue
        for k, v in DOAN_UC_MOI.items():
            if t.startswith(k):
                set_text(p, v)
                break
        else:
            n = t
            for x, y in SUA_SAU:
                n = n.replace(x, y)
            if n != t:
                set_text(p, n)


# ====================================================================== một sơ đồ lớp tổng
DOAN_LOP = ("Hình 3.8 là sơ đồ lớp của toàn hệ thống ở mức phân tích, cùng bố cục với ERD. Mỗi lớp là một đối tượng nghiệp "
            "vụ, hộp lớp chia ba ngăn: tên lớp, thuộc tính (dấu - là riêng tư) và thao tác (dấu + là công khai). Mỗi lớp "
            "chỉ ghi những thuộc tính chính. Giá trị tính ra từ dữ liệu khác (tên toà, trạng thái trước, tổng tiền, còn "
            "nợ, thành tiền) ghi thành thao tác trả về giá trị, không phải thuộc tính.")


def gop_so_do_lop(body, set_text):
    """Ba sơ đồ lớp theo nhóm gộp thành một (Hình 3.8): bỏ hình 3.9, 3.10 cũ và câu dẫn tới chúng."""
    for p in list(body.iter(qn("w:p"))):
        if _la_muc_luc(p):
            continue
        t = _text(p)
        if t.startswith("Hình 3.8 là sơ đồ lớp"):
            set_text(p, DOAN_LOP)
        elif (t.startswith("Hình 3.9 gồm các lớp") or t.startswith("Hình 3.10 vẽ riêng")
              or t.startswith("Bội số ghi ở hai đầu mỗi liên kết")):
            p.getparent().remove(p)
        elif (t.startswith("Hình 3.9.") or t.startswith("Hình 3.10.")) and _kieu(p) == "ChuthichHinh":
            prev = p.getprevious()
            while prev is not None and prev.tag == qn("w:p") and prev.find(".//" + qn("w:sectPr")) is None and (
                    prev.find(".//" + qn("w:drawing")) is not None
                    or prev.find(".//{urn:schemas-microsoft-com:vml}imagedata") is not None or not _text(prev).strip()):
                nxt = prev.getprevious()
                prev.getparent().remove(prev)
                prev = nxt
            p.getparent().remove(p)


VIET_TAT_THUA = {"GUID", "HTTT", "LLM", "RD"}   # không còn dùng trong thân bài


def bo_viet_tat_thua(body):
    """Bỏ các dòng của danh mục chữ viết tắt mà thân bài không còn dùng."""
    for tr in list(body.iter(qn("w:tr"))):
        tcs = tr.findall(qn("w:tc"))
        if len(tcs) == 2 and _text(tcs[0]).strip() in VIET_TAT_THUA:
            tr.getparent().remove(tr)


TRANG_THAI_TUAN = {"6": "Hoàn thành", "7": "Đang thực hiện"}   # bản nộp sau tuần 6 (25/11)


def sua_trang_thai_tuan(body, set_text):
    """Cột Trạng thái của bảng kế hoạch theo tuần: tuần đã xong ghi Hoàn thành, tuần đang chạy ghi Đang thực hiện."""
    cap = next(p for p in body.iter(qn("w:p")) if "Kế hoạch thực hiện theo tuần" in _text(p) and not _la_muc_luc(p))
    tbl = cap.getnext()
    assert tbl.tag == qn("w:tbl")
    for tr in tbl.iter(qn("w:tr")):
        tcs = tr.findall(qn("w:tc"))
        tuan = _text(tcs[0]).strip()
        if tuan in TRANG_THAI_TUAN:
            set_text(tcs[-1].find(qn("w:p")), TRANG_THAI_TUAN[tuan])


def sua_gach_dau_dong(body):
    """Đoạn gạch đầu dòng bị viết lại bằng set_text mất ký tự tab sau dấu '–' nên chữ dính vào gạch, lệch lề so với
    các dòng khác. Tách lại thành '–', tab, nội dung (giữ định dạng chữ của run đầu)."""
    sua = 0
    for p in body.iter(qn("w:p")):
        runs = p.findall(qn("w:r"))
        if not runs or p.find(".//" + qn("w:tab")) is not None or _la_muc_luc(p):
            continue
        t0 = runs[0].find(qn("w:t"))
        if t0 is None or not t0.text or not re.match(r"–\s*\w", t0.text):
            continue
        nd = t0.text[1:].lstrip()
        t0.text = "–"
        r_tab, r_nd = copy.deepcopy(runs[0]), copy.deepcopy(runs[0])
        r_tab.remove(r_tab.find(qn("w:t")))
        r_tab.append(r_tab.makeelement(qn("w:tab"), {}))
        r_nd.find(qn("w:t")).text = nd
        r_nd.find(qn("w:t")).set("{http://www.w3.org/XML/1998/namespace}space", "preserve")
        runs[0].addnext(r_tab)
        r_tab.addnext(r_nd)
        sua += 1
    return sua


# ====================================================================== 26/11: gọn Chương 3
# Chỉ giữ ba sơ đồ tuần tự của chức năng chính (báo sự cố có AI, kỹ thuật viên hoàn tất, duyệt chuyển khoản); bước
# điều phối đã có ở sơ đồ hoạt động. Bỏ mục tổng kết tuần cũ ở cuối Chương 3 (chỉ giữ tổng kết tuần mới ở Chương 4);
# câu kiểm tra đủ 56 cặp trạng thái là kết quả kiểm thử nên chuyển sang mục 4.5.2.
DOAN_TUAN_TU = ("Ba sơ đồ tuần tự mô tả ba chức năng chính: cư dân báo sự cố có AI phân loại, kỹ thuật viên hoàn tất "
                "xử lý, và quản lý duyệt xác nhận chuyển khoản của cư dân. Bước điều phối sau khi lưu yêu cầu đã thể "
                "hiện ở sơ đồ hoạt động (Hình 3.7). Khung opt là đoạn chỉ chạy khi điều kiện đúng, khung alt là các "
                "nhánh loại trừ nhau; thông điệp nét đứt là giá trị trả về.")
CAU_56_CAP = (" Ngoài ra, bảng chuyển trạng thái được kiểm tra trên đủ 56 cặp trạng thái (từ, sang): 11 cặp được phép, "
              "45 cặp bị chặn; bắt đầu xử lý khi chưa có người phụ trách, hoặc hoàn tất khi chưa ghi kết quả, cũng bị "
              "chặn.")


def gon_chuong3(body, set_text):
    """Chạy sau bo_muc_uc_chuong3 (mục Chương 3 đã đánh số lại), trước khi đánh số lại bảng, hình."""
    ps = [p for p in body.iter(qn("w:p")) if not _la_muc_luc(p)]
    # 1. Bỏ sơ đồ tuần tự điều phối: đoạn mô tả, hình, chú thích đứng liền nhau.
    mo_ta = next(p for p in ps if _text(p).startswith("Hình 3.12: chi tiết bước điều phối"))
    hinh = mo_ta.getnext()
    cap = hinh.getnext()
    assert hinh.find(".//" + qn("w:drawing")) is not None and _text(cap).startswith("Hình 3.12."), "sai vị trí hình 3.12"
    for el in (mo_ta, hinh, cap):
        el.getparent().remove(el)
    set_text(next(p for p in ps if _text(p).startswith("Bốn sơ đồ tuần tự mô tả")), DOAN_TUAN_TU)
    # 2. Bỏ mục "Kết quả tuần 4 và việc tiếp theo" (từ tiêu đề tới trước CHƯƠNG 4).
    dau = next(p for p in ps if re.match(r"3\.\d\. Kết quả tuần 4", _text(p)))
    el = dau
    while not _text(el).startswith("CHƯƠNG 4."):
        nxt = el.getnext()
        el.getparent().remove(el)
        el = nxt
    # 3. Câu 56 cặp trạng thái sang mục 4.5.2.
    p452 = next(p for p in ps if _text(p).startswith("Các chuyển trạng thái ngoại lệ ở Bảng"))
    if CAU_56_CAP.strip() not in _text(p452):
        set_text(p452, _text(p452).rstrip() + CAU_56_CAP)


# ====================================================================== 26/11: dẫn số mọi bảng, hình trong bài
# Chạy SAU khi đánh số lại bảng, hình (số ở đây là số cuối cùng). Mỗi bảng, hình đều được một câu văn nhắc tới.
DAN_THAY = [   # (chuỗi cũ, chuỗi mới) trong đoạn văn, áp một lần
    ("Hai use case trọng tâm được vẽ chi tiết rồi đặc tả.",
     "Hai use case trọng tâm được vẽ chi tiết (Hình 1.7, 1.8) rồi đặc tả (Bảng 1.13, 1.14)."),
    ("Bảng dưới đây là phân công theo năng lực", "Bảng 1.15 là phân công theo năng lực"),
    ("Kế hoạch thực hiện bám theo phiếu theo dõi tiến độ đã đăng ký với giảng viên hướng dẫn:",
     "Kế hoạch thực hiện (Bảng 1.16) bám theo phiếu theo dõi tiến độ đã đăng ký với giảng viên hướng dẫn."),
    ("Won't-have là chưa làm trong kỳ đồ án.", "Won't-have là chưa làm trong kỳ đồ án. Bảng 1.11 liệt kê các yêu cầu "
     "chức năng kèm mức ưu tiên và tiêu chí nghiệm thu."),
    ("Để ERD vừa một hình, mỗi thực thể chỉ ghi khoá và các thuộc tính chính.",
     "Hình 2.1 là ERD của toàn hệ thống; để vừa một hình, mỗi thực thể chỉ ghi khoá và các thuộc tính chính."),
    ("nên ứng dụng vẫn chạy khi máy AI tắt.", "nên ứng dụng vẫn chạy khi máy AI tắt. Bảng 3.1 nêu công nghệ và trách "
     "nhiệm của từng thành phần."),
    ("được xoá sau khi kiểm tra xong.", "được xoá sau khi kiểm tra xong. Bảng 4.1 liệt kê các tài khoản thử theo vai "
     "trò."),
    ("đúng như thiết kế ở mục 2.3.", "đúng như thiết kế ở mục 2.3. Bảng 4.2 liệt kê quyền của từng phân hệ và các "
     "quyền Quản lý BQL được cấp."),
    ("chỉ số điện nước, sự cố của căn.", "chỉ số điện nước, sự cố của căn. Kết quả thử của phân hệ ở Bảng 4.3."),
    ("ghi nhận tiền mặt, hoàn tiền khi cần.", "ghi nhận tiền mặt, hoàn tiền khi cần. Kết quả thử của phân hệ ở Bảng "
     "4.4."),
    ("được thử với yêu cầu YC-202611-0002", "được thử (Bảng 4.6, Hình 4.4) với yêu cầu YC-202611-0002"),
    ("Nhóm ghi nhận 9 lỗi mức cao,", "Nhóm ghi nhận 9 lỗi mức cao (Bảng 4.8),"),
    # Bảng 3.1 và Bảng 1.3 khớp sơ đồ triển khai (dịch vụ nền có bốn việc, tầng nghiệp vụ có lập hoá đơn)
    ("Các lớp xử lý quy trình sự cố, điều phối và tính KPI", "Các lớp xử lý quy trình sự cố, điều phối, tính KPI và "
     "lập hoá đơn"),
    ("Hai tác vụ chạy định kỳ", "Bốn tác vụ chạy nền"),
    ("Lập hoá đơn tháng, chuyển hoá đơn quá hạn, cộng phí trễ; báo quản lý yêu cầu sự cố quá hạn",
     "Lập hoá đơn tháng, chuyển hoá đơn quá hạn, cộng phí trễ; nhắc hạn thanh toán; báo quản lý yêu cầu sự cố quá hạn; "
     "nạp sẵn mô hình AI khi khởi động"),
    ("chuyển hoá đơn quá hạn, cộng phí trễ; báo quản lý yêu cầu sự cố quá hạn",
     "chuyển hoá đơn quá hạn, cộng phí trễ; nhắc hạn thanh toán; báo quản lý yêu cầu sự cố quá hạn"),
    # Rà lần 4 (26/11): câu sai so với hình, ký hiệu chưa giải thích, câu lặp
    ("Bước ghi [AI] là bước do mô hình AI hỗ trợ; bước ghi [Tự động] là bước tự động hoá bằng quy tắc nghiệp vụ "
     "(không phải AI).", "Các bước ở làn Hệ thống đều chạy tự động; trong đó chỉ bước phân loại sự cố dùng AI, các "
     "bước còn lại chạy theo quy tắc nghiệp vụ."),
    ("Mỗi mũi tên ghi tác nhân được phép thực hiện;", "Mỗi mũi tên ghi tác nhân được phép thực hiện (CD: cư dân, QL: "
     "quản lý, KTV: kỹ thuật viên, HT: hệ thống);"),
    ("lý do lựa chọn được giải thích ở mục 2.5. Các mã RB, LB (ràng buộc), TG (trigger) và FN (hàm) trong cột này được "
     "liệt kê ở mục 2.5.", "các mã RB, LB (ràng buộc), TG (trigger), FN (hàm) và lý do chọn cơ chế được trình bày ở mục "
     "2.5."),
    ("Thời gian phản hồi TB (phút)", "Thời gian phản hồi trung bình (phút)"),
    # Rà lần 6 (27/11): câu khẳng định về mô hình phải đúng với hình
    ("Các mô hình được đối chiếu chéo: mỗi use case có đặc tả và luồng xử lý tương ứng; mỗi lớp ứng với một bảng, mỗi "
     "liên kết ứng với một khoá ngoại; mỗi thông điệp tuần tự gọi thao tác có trong lớp nhận; mỗi quy tắc nghiệp vụ có "
     "ràng buộc hoặc bước kiểm tra tương ứng.",
     "Các mô hình được đối chiếu chéo: hai use case trọng tâm có đặc tả (Bảng 1.13, 1.14) và sơ đồ tuần tự tương ứng "
     "(Hình 3.4, 3.5), các bước của hai sơ đồ này khớp với luồng chính trong đặc tả và sơ đồ hoạt động; mỗi lớp nghiệp vụ "
     "ứng với một bảng, mỗi liên kết ứng với một khoá ngoại; mỗi quy tắc nghiệp vụ có ràng buộc hoặc bước kiểm tra "
     "tương ứng (Bảng 1.8)."),
    ("Khung opt là đoạn chỉ chạy khi điều kiện đúng,", "Đối tượng có dấu “:” là bộ phận xử lý của ứng dụng (Hình "
     "3.1), không phải bảng dữ liệu. Khung opt là đoạn chỉ chạy khi điều kiện đúng,"),
    # Rà lần 5 (27/11): nội dung trong bảng
    ("Không tạo được hai căn cùng mã trong một toà", "Không tạo được hai căn trùng mã; số tầng phải từ 1 trở lên"),
    ("use case chi tiết và đặc tả, activity, class, sequence (AI phân loại, điều phối, hoàn tất)",
     "use case chi tiết và đặc tả, sơ đồ hoạt động, lớp, tuần tự (AI phân loại, điều phối, hoàn tất)"),
    ("ba trigger cho ràng buộc cần dữ liệu nhiều dòng", "ba trigger cho ràng buộc liên bộ, liên quan hệ phải đúng cả khi "
     "sửa thẳng cơ sở dữ liệu"),
    ("Ba trigger và một hàm ở Bảng 2.6 chỉ kiểm tra rồi chấp nhận hoặc từ chối lệnh ghi, không tự ghi thêm dữ liệu; mỗi "
     "cái đã được thử bằng lệnh vi phạm cố ý.", "Ba trigger ở Bảng 2.6 chỉ kiểm tra rồi chấp nhận hoặc từ chối lệnh ghi, "
     "không tự ghi thêm dữ liệu; hàm FN1 chỉ tính hạn xử lý. Mỗi trigger đã được thử bằng lệnh vi phạm cố ý."),
    ("–Chương 5 – Kết luận và kiến nghị (tuần 9–10).", "–Chương 5 – Kết luận và kiến nghị (tuần 9–10, chưa có trong "
     "báo cáo này)."),
]
DAN_CHEN = {   # tiêu đề mục -> câu chèn ngay sau tiêu đề (mục chỉ có bảng, chưa có câu dẫn)
    "1.3.2. Tác nhân": "Bảng 1.3 liệt kê các tác nhân, vai trò của từng tác nhân trong quy trình và trên hệ thống.",
    "1.4.2. So sánh AS-IS và TO-BE": "Bảng 1.6 đối chiếu từng bước của quy trình hiện tại và quy trình đề xuất.",
    "1.7.2. Yêu cầu phi chức năng": "Bảng 1.12 nêu các yêu cầu phi chức năng ở dạng kiểm tra được.",
}


def dan_bang_hinh(body, set_text, new_p):
    ps = [p for p in body.iter(qn("w:p")) if not _la_muc_luc(p)]
    con = {a for a, _ in DAN_THAY}
    for p in ps:
        t = _text(p)
        n = t
        for a, b in DAN_THAY:
            if a in n and b not in n:
                n = n.replace(a, b, 1)
                con.discard(a)
        if n != t:
            set_text(p, n)
        if t in DAN_CHEN and not _text(p.getnext()).startswith(DAN_CHEN[t][:20]):
            p.addnext(new_p("Noidung", DAN_CHEN[t]))
    return sorted(con)


# Bảng 2.7: cột Mục trỏ đúng nơi trình bày (khoá lạc quan ở 4.5.4, lớp ánh xạ ở 3.1). Ô "2.3" có ở nhiều dòng nên sửa
# theo dòng, không thay chuỗi.
MUC_TINH_CHAT = {"Tính nhất quán": "2.4, 2.5, 4.5.4", "Tính độc lập": "3.1"}


def sua_muc_tinh_chat(body, set_text):
    cap = next(p for p in body.iter(qn("w:p")) if _text(p).startswith("Bảng ") and "tính chất của cơ sở dữ liệu" in _text(p)
               and not _la_muc_luc(p))
    tbl = cap.getnext()
    for tr in tbl.iter(qn("w:tr")):
        tcs = tr.findall(qn("w:tc"))
        ten = _text(tcs[0]).strip()
        if ten in MUC_TINH_CHAT:
            set_text(tcs[-1].find(qn("w:p")), MUC_TINH_CHAT[ten])


# Sửa một ô theo (bảng, dòng, cột) khi chữ trong ô lặp ở nhiều dòng, không thay chuỗi được.
O_THEO_DONG = [   # (chữ trong chú thích bảng, ô đầu của dòng, số thứ tự cột, chữ mới)
    ("Yêu cầu chức năng, mức ưu tiên", "FR-03", 3, "Quản lý, Cư dân"),   # UC02 (kích hoạt tài khoản) do cư dân làm
]


def sua_o_theo_dong(body, set_text):
    for cap_chu, dong, cot, moi in O_THEO_DONG:
        cap = next(p for p in body.iter(qn("w:p")) if _text(p).startswith("Bảng ") and cap_chu in _text(p)
                   and not _la_muc_luc(p))
        for tr in cap.getnext().iter(qn("w:tr")):
            tcs = tr.findall(qn("w:tc"))
            if _text(tcs[0]).strip() == dong:
                set_text(tcs[cot].find(qn("w:p")), moi)


# ====================================================================== tài liệu tham khảo
# Xếp tài liệu tiếng Việt trước, tiếng nước ngoài sau, mỗi nhóm theo ABC tên tác giả; đánh số lại mọi trích dẫn [n].
TAC_GIA_VIET = ("Bộ Xây dựng", "Quốc hội")


def xep_tai_lieu(body, set_text):
    ps = [p for p in body.iter(qn("w:p")) if not _la_muc_luc(p)]
    dau = next(i for i, p in enumerate(ps) if _text(p).strip() == "TÀI LIỆU THAM KHẢO")
    refs = [p for p in ps[dau + 1:] if re.match(r"\[\d+\] ", _text(p))]
    cu = {int(re.match(r"\[(\d+)\]", _text(p)).group(1)): _text(p).split("] ", 1)[1] for p in refs}

    def khoa(so):
        nd = cu[so]
        return (0 if nd.startswith(TAC_GIA_VIET) else 1, nd.lower())

    thu_tu = sorted(cu, key=khoa)
    moi = {old: i + 1 for i, old in enumerate(thu_tu)}
    if all(moi[k] == k for k in moi):
        return 0
    for p, old in zip(refs, thu_tu):
        set_text(p, f"[{moi[old]}] {cu[old]}")
    doi = 0
    for p in ps[:dau]:
        t = _text(p)
        n = re.sub(r"\[(\d+)\]", lambda m: f"[{moi.get(int(m.group(1)), int(m.group(1)))}]", t)
        if n != t:
            set_text(p, n)
            doi += 1
    return doi
