"""Dựng báo cáo sau góp ý tuần 6 từ bản _sua của nhóm:
  - Chương 2 thay bằng nội dung sinh từ CSDL thật (noi_dung_ch2.py);
  - thay 26 sơ đồ bằng bản StarUML mới (images_v2), hình rộng đặt trên trang ngang, không xoay ảnh;
  - sửa các đoạn Chương 1, 3, 4 còn nhắc tên/mã cũ;
  - ghi ra BaoCao_DACN_Tuan1-6_sua_v2.docx (không ghi đè bản _sua).
Sau đó chạy cap_nhat_word.ps1 để Word cập nhật mục lục, danh mục bảng/hình và xuất PDF.
"""
import copy
import os
import re
import sys
from pathlib import Path

import docx
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.oxml.ns import qn
from docx.shared import Pt
from PIL import Image

import noi_dung_ch2
import so_do_v2 as sd

SRC = Path(r"C:\Users\ACER\Downloads\BaoCao_DACN_Tuan1-6_sua.docx")
OUT = Path(os.environ.get("BAOCAO_OUT", r"C:\Users\ACER\Downloads\BaoCao_DACN_Tuan1-6_sua_v2.docx"))
IMG = sd.IMG

PORTRAIT = (453, 610)     # vùng ảnh tối đa trên trang dọc (pt), chừa chỗ chú thích
LANDSCAPE = (700, 430)    # vùng ảnh tối đa trên trang ngang (pt): 700 x 468 trừ một dòng chú thích

FIG_FILE = {
    "Hình 1.1.": "Hinh 1.1 Quy trinh AS-IS", "Hình 1.4.": "Hinh 1.4 Quy trinh TO-BE",
    "Hình 1.5.": "Hinh 1.5 So do trang thai yeu cau su co", "Hình 1.6.": "Hinh 1.6 So do use case tong quat",
    "Hình 3.1.": "Hinh 3.1 Kien truc trien khai", "Hình 3.2.": "Hinh 3.2 Use case UC03 Bao su co",
    "Hình 3.3.": "Hinh 3.3 Use case UC11 Phan cong xu ly su co",
    "Hình 3.4.": "Hinh 3.4 Use case UC16 Cap nhat tien do xu ly",
    "Hình 3.5.": "Hinh 3.5 Use case UC06 Xac nhan ket qua xu ly", "Hình 3.6.": "Hinh 3.6 Use case UC20 Xu ly thanh toan",
    "Hình 3.7.": "Hinh 3.7 Hoat dong bao su co AI dieu phoi",
    "Hình 3.8.": "Hinh 3.8 So do lop tong", "Hình 3.11.": "Hinh 3.11 Tuan tu bao su co AI phan loai",
    "Hình 3.12.": "Hinh 3.12 Tuan tu dieu phoi sau khi luu", "Hình 3.13.": "Hinh 3.13 Tuan tu ky thuat vien hoan tat",
    "Hình 3.14.": "Hinh 3.14 Tuan tu quan ly duyet thanh toan",
}
FIG_FILE.update({  # ảnh chụp màn hình Chương 4, chụp lại bằng chup_man_hinh.py
    "Hình 4.1.": "Hinh 4.1 Trang chi tiet can ho", "Hình 4.2.": "Hinh 4.2 Hoa don qua han co phi tre",
    "Hình 4.3.": "Hinh 4.3 Cu dan theo doi yeu cau", "Hình 4.4.": "Hinh 4.4 Lich su xu ly ngoai le",
    "Hình 4.5.": "Hinh 4.5 Bang dieu khien", "Hình 4.6.": "Hinh 4.6 Cong no phan tuoi",
    "Hình 4.7.": "Hinh 4.7 Bao cao KPI su co",
})
NEW_CAPTION = {
    "Hình 4.5.": "Hình 4.5. Bảng điều khiển của quản lý ngày 25/11/2026",
    "Hình 4.6.": "Hình 4.6. Báo cáo công nợ phân tuổi ngày 25/11/2026",
    "Hình 4.7.": "Hình 4.7. Báo cáo KPI sự cố từ 01/11/2026 đến 25/11/2026",
    "Hình 3.8.": "Hình 3.8. Sơ đồ lớp của hệ thống",
    "Hình 3.14.": "Hình 3.14. Sơ đồ tuần tự quản lý duyệt xác nhận chuyển khoản",
}

doc = docx.Document(str(SRC))
body = doc.element.body


def text(el):
    return "".join(t.text or "" for t in el.iter(qn("w:t")))


def paras():
    return [el for el in body.iterchildren() if el.tag == qn("w:p")]


def find_p(prefix, style=None, start=0):
    for el in list(body.iterchildren())[start:]:
        if el.tag == qn("w:p") and text(el).startswith(prefix):
            if style is None or style_of(el) == style:
                return el
    raise KeyError(prefix)


def style_of(el):
    ps = el.find(qn("w:pPr") + "/" + qn("w:pStyle"))
    return ps.get(qn("w:val")) if ps is not None else ""


# Mẫu đoạn lấy từ thân Chương 1 (đoạn cùng kiểu ở trang bìa / nhận xét có căn lề khác).
TEMPLATE = {st: find_p(prefix, st) for st, prefix in [
    ("Noidung", "Chương này trả lời câu hỏi"), ("Heading21", "1.1. Giới thiệu đề tài"),
    ("Heading31", "1.1.1. Bối cảnh"), ("Gachdaudong", "–Mô hình hoá quy trình"),
    ("ChuthichBang", "Bảng 1.1."), ("ChuthichHinh", "Hình 1.1.")]}


def set_text(el, s, runs=None):
    """Thay chữ của đoạn, giữ định dạng run đầu. runs: [(chữ, gạch chân, nghiêng)]."""
    rs = el.findall(qn("w:r"))
    rpr = rs[0].find(qn("w:rPr")) if rs else None
    for child in list(el):
        if child.tag != qn("w:pPr"):
            el.remove(child)
    for t, u, i in runs or [(s, False, False)]:
        r = docx.oxml.OxmlElement("w:r")
        rp = copy.deepcopy(rpr) if rpr is not None else docx.oxml.OxmlElement("w:rPr")
        for tag, on in (("w:u", u), ("w:i", i)):
            old = rp.find(qn(tag))
            if old is not None:
                rp.remove(old)
            if on:
                x = docx.oxml.OxmlElement(tag)
                if tag == "w:u":
                    x.set(qn("w:val"), "single")
                rp.append(x)
        if len(rp):
            r.append(rp)
        for k, part in enumerate(t.split("\t")):
            if k:
                r.append(docx.oxml.OxmlElement("w:tab"))
            te = docx.oxml.OxmlElement("w:t")
            te.text = part
            te.set("{http://www.w3.org/XML/1998/namespace}space", "preserve")
            r.append(te)
        el.append(r)
    return el


def new_p(style, s="", runs=None):
    el = copy.deepcopy(TEMPLATE[style])
    sect = el.find(qn("w:pPr") + "/" + qn("w:sectPr"))
    if sect is not None:
        sect.getparent().remove(sect)
    return set_text(el, s, runs)


# Mẫu bảng: bảng ngay sau chú thích “Bảng 1.1.” (có hàng tiêu đề và hàng dữ liệu).
TBL_TEMPLATE = next(el for el in body.iterchildren() if el.tag == qn("w:p") and text(el).startswith("Bảng 1.1.")
                    and style_of(el) == "ChuthichBang").getnext()


def new_table(header, rows, widths):
    """Bảng trắng đen theo mẫu bảng của báo cáo; widths là tỉ lệ cột, tổng quy về 9071 twip."""
    tbl = copy.deepcopy(TBL_TEMPLATE)
    for tr in tbl.findall(qn("w:tr")):
        tbl.remove(tr)
    grid = tbl.find(qn("w:tblGrid"))
    for g in list(grid):
        grid.remove(g)
    total = sum(widths)
    tw = [int(9071 * w / total) for w in widths]
    for w in tw:
        g = docx.oxml.OxmlElement("w:gridCol")
        g.set(qn("w:w"), str(w))
        grid.append(g)
    tr_head = TBL_TEMPLATE.findall(qn("w:tr"))[0]
    tr_body = TBL_TEMPLATE.findall(qn("w:tr"))[1]
    for k, row in enumerate([header] + rows):
        tr = copy.deepcopy(tr_head if k == 0 else tr_body)
        tcs = tr.findall(qn("w:tc"))
        tc0 = tcs[0]
        for tc in tcs:
            tr.remove(tc)
        for j, val in enumerate(row):
            tc = copy.deepcopy(tc0)
            tc.find(qn("w:tcPr") + "/" + qn("w:tcW")).set(qn("w:w"), str(tw[j]))
            ps = tc.findall(qn("w:p"))
            for p in ps[1:]:
                tc.remove(p)
            p = ps[0]
            jc = p.find(qn("w:pPr") + "/" + qn("w:jc"))
            if k > 0 and jc is not None:
                jc.set(qn("w:val"), "center" if len(str(val)) <= 6 else "left")
            set_text(p, str(val))
            if k == 0:
                for r in p.findall(qn("w:r")):
                    rp = r.find(qn("w:rPr"))
                    if rp is None:
                        rp = docx.oxml.OxmlElement("w:rPr")
                        r.insert(0, rp)
                    if rp.find(qn("w:b")) is None:
                        rp.append(docx.oxml.OxmlElement("w:b"))
            tr.append(tc)
        tbl.append(tr)
    return tbl


# Ảnh màn hình đứng liền nhau (Hình 4.5, 4.6) giới hạn cao 10 cm để hai hình chung một trang, không bỏ trống nửa trang.
CAO_TOI_DA = {"Hinh 4.5": Pt(283), "Hinh 4.6": Pt(283), "Hinh 4.7": Pt(300)}   # 4.7 vừa nửa trang dưới 4.6
# Sơ đồ duyệt chuyển khoản (Hình 3.6 trong báo cáo) thấp lại 2 dòng để đoạn cuối Chương 3 không rơi sang trang riêng.
CAO_TOI_DA["Hinh 3.14"] = Pt(450)
CAO_TOI_DA["Hinh 3.11"] = Pt(370)   # Hình 3.4 vừa nửa dưới trang có đoạn mở mục 3.4


def fig_size(png):
    w, h = Image.open(png).size
    sp = min(PORTRAIT[0] / w, PORTRAIT[1] / h)
    sl = min(LANDSCAPE[0] / w, LANDSCAPE[1] / h)
    landscape = sl > sp * 1.15
    s = sl if landscape else sp
    cao = next((v for k, v in CAO_TOI_DA.items() if png.name.startswith(k)), None)
    if cao is not None and not landscape:
        s = min(s, cao / Pt(h))
    return landscape, Pt(w * s), Pt(h * s)


def fig_size_portrait(png):
    w, h = Image.open(png).size
    s = min(PORTRAIT[0] / w, PORTRAIT[1] / h)
    cao = next((v for k, v in CAO_TOI_DA.items() if png.name.startswith(k)), None)
    if cao is not None:
        s = min(s, cao / Pt(h))
    return Pt(w * s), Pt(h * s)


def new_figure(png):
    landscape, w, h = fig_size(png)
    if png.name.startswith("Hinh 4."):      # ảnh màn hình giữ trang dọc như bản của nhóm
        landscape, w, h = False, *fig_size_portrait(png)
    el = docx.oxml.OxmlElement("w:p")
    para = docx.text.paragraph.Paragraph(el, doc._body)
    para.alignment = WD_ALIGN_PARAGRAPH.CENTER
    para.paragraph_format.keep_with_next = True
    para.add_run().add_picture(str(png), width=w, height=h)
    el.set("data-landscape", "1" if landscape else "0")
    return el


# ======================================================================= Chương 2
def rebuild_chapter2():
    start = find_p("CHƯƠNG 2. PHÂN TÍCH VÀ THIẾT KẾ DỮ LIỆU", "Heading11")
    end = find_p("CHƯƠNG 3. THIẾT KẾ HỆ THỐNG", "Heading11")
    el = start.getnext()
    while el is not end:
        nxt = el.getnext()
        body.remove(el)
        el = nxt
    anchor = start
    counter = [100]   # số tạm, không trùng số viết tay; gon_bao_cao.danh_so_lai đánh lại liên tục

    def put(x):
        nonlocal anchor
        anchor.addnext(x)
        anchor = x

    for blk in noi_dung_ch2.blocks():
        kind = blk[0]
        if kind == "h2":
            put(new_p("Heading21", blk[1]))
        elif kind == "h3":
            put(new_p("Heading31", blk[1]))
        elif kind == "p":
            put(new_p("Noidung", blk[1]))
        elif kind == "bullet":
            put(new_p("Gachdaudong", "–\t" + blk[1]))
        elif kind == "rd":
            put(new_p("Noidung", runs=noi_dung_ch2.rd_line(blk[1])))
        elif kind == "table":
            _, cap, head, rows, widths = blk
            if "{n}" in cap:
                counter[0] += 1
                cap = cap.replace("{n}", f"2.{counter[0]}")
            put(new_p("ChuthichBang", cap))
            put(new_table(head, rows, widths))
        elif kind == "fig":
            put(new_figure(blk[2]))
            put(new_p("ChuthichHinh", blk[1]))


# ======================================================================= Chương 1, 3: thay hình
def replace_figures():
    for prefix, name in FIG_FILE.items():
        cap = find_p(prefix, "ChuthichHinh")
        if prefix in NEW_CAPTION:
            set_text(cap, NEW_CAPTION[prefix])
        prev = cap.getprevious()
        if prev is not None and prev.tag == qn("w:p") and (
                prev.find(".//{urn:schemas-microsoft-com:vml}imagedata") is not None
                or prev.find(".//" + qn("w:drawing")) is not None):
            body.remove(prev)
        fig = new_figure(IMG / (name + ".png"))
        num = prefix[5:-1]
        if num.startswith("3."):
            # Chương 3: đặt hình ngay sau đoạn mô tả nó (bản _sua đặt lệch chỗ).
            body.remove(cap)
            ref = next((p for p in paras() if style_of(p) in ("Noidung", "Gachdaudong")
                        and re.match(rf"Hình {re.escape(num)}(\D|$)", text(p))), None)
            if ref is None:
                raise KeyError("không thấy đoạn mô tả " + prefix)
            ref.addnext(fig)
            fig.addnext(cap)
        else:
            cap.addprevious(fig)


# ======================================================================= trang ngang
def landscape_sections():
    main = body.find(qn("w:sectPr"))

    def sect(orient, keep_num=False):
        s = copy.deepcopy(main)
        pg = s.find(qn("w:pgSz"))
        w, h = int(pg.get(qn("w:w"))), int(pg.get(qn("w:h")))
        if orient == "landscape":
            pg.set(qn("w:w"), str(max(w, h)))
            pg.set(qn("w:h"), str(min(w, h)))
            pg.set(qn("w:orient"), "landscape")
        num = s.find(qn("w:pgNumType"))
        if num is not None and not keep_num:
            s.remove(num)       # số trang chạy tiếp
        return s

    figs = [p for p in paras() if p.get("data-landscape") == "1"]
    groups, cur = [], []
    for f in figs:
        cap = f.getnext()
        if cur and cur[-1].getnext().getnext() is f:
            cur.append(f)
        else:
            if cur:
                groups.append(cur)
            cur = [f]
    if cur:
        groups.append(cur)
    for i, g in enumerate(groups):
        brk = docx.oxml.OxmlElement("w:p")
        ppr = docx.oxml.OxmlElement("w:pPr")
        brk.append(ppr)
        # Mục "đánh số lại từ 1" của thân bài nằm ở sectPr cuối; khi thân bài bị chia thành
        # nhiều mục, nó phải chuyển sang mục đầu tiên, nếu không số trang bắt đầu lại ở mục cuối.
        ppr.append(sect("portrait", keep_num=(i == 0)))
        g[0].addprevious(brk)
        for f in g[1:]:
            docx.text.paragraph.Paragraph(f, doc._body).paragraph_format.page_break_before = True
        last_cap = g[-1].getnext()
        last_cap.get_or_add_pPr().append(sect("landscape"))
    if groups:
        num = main.find(qn("w:pgNumType"))
        if num is not None:
            main.remove(num)
    for p in paras():
        if p.get("data-landscape") is not None:
            del p.attrib["data-landscape"]
    return len(groups), len(figs)


# ======================================================================= sửa chữ Chương 1, 3, 4
REPL = [
    ("Các ký hiệu [P1] – [P6] ghi trong bước là bất cập được phân tích ở mục 1.3.5.",
     "Mỗi ô là một hành động cụ thể; bất cập của từng bước được phân tích ở mục 1.3.5 (Bảng 1.5)."),
    ("(bảng CuDan)", "(bảng NguoiDung, quan hệ cư trú CuTru)"),
    ("(bảng MucSla)", "(bảng MucUuTien)"),
    ("MucSla.PhutPhanHoi", "MucUuTien.PhutPhanHoi"),
    ("trong MucSla", "trong MucUuTien"),
    ("MucSla", "MucUuTien"),
    ("ràng buộc miền giá trị MG06", "ràng buộc miền giá trị MG05"),
    ("Bảng 2.16", "Bảng 1.7"),   # bảng chuyển trạng thái chỉ còn ở Chương 1
    ("mỗi nhóm một hình để vừa trang dọc", "mỗi nhóm một hình đặt trên trang ngang"),
    ("Mỗi nút quyết định có đúng một luồng vào", "Mỗi ô là một hành động cụ thể; trước mỗi nút quyết định là một "
     "hành động kiểm tra và nút quyết định ghi câu hỏi. Mỗi nút quyết định có đúng một luồng vào"),
    ("áp dụng đúng một MucSla", "có đúng một mức ưu tiên (MucUuTien)"),
    ("–TrangThaiSuCo, MucDoUuTien, LoaiBaoTri là các kiểu liệt kê",
     "–TrangThaiYeuCau, LoaiSuCo là các kiểu liệt kê (mức ưu tiên là lớp MucUuTien)"),
    ("–TrangThaiHoaDon, TrangThaiThanhToan là các kiểu liệt kê",
     "–TrangThaiHoaDon, TrangThaiThanhToan, CachTinh là các kiểu liệt kê"),
    ("là tài khoản của bản ghi cư dân", "có thể có nhiều lần cư trú (CuTru)"),
]
# Câu viết lại cho gọn (so khớp chữ đã qua các phép thay ở trên), rồi mới đổi tên bảng.
from cau_viet_lai import CAU, TEN_MOI  # noqa: E402
import gon_bao_cao  # noqa: E402
import ten_moi_tuan8  # noqa: E402
REPL = REPL + CAU + TEN_MOI
REWRITE = {  # đoạn bắt đầu bằng ... -> nội dung mới
    "Về kỹ thuật, phân loại văn bản tự do":
        "Về kỹ thuật, mô tả sự cố do cư dân tự viết thường ngắn, viết tắt, không dấu và gộp nhiều ý, nên khó phân loại "
        "bằng từ khoá; mô hình ngôn ngữ lớn hiểu được kiểu văn bản này. Đề tài dùng mô hình mở Qwen3-4B [12] và dạy "
        "thêm cho mô hình trên bộ mô tả sự cố do nhóm tự xây dựng. Cách dạy thêm là QLoRA [5], một cách tinh chỉnh tốn "
        "ít bộ nhớ dựa trên LoRA [6]. Sau khi tinh chỉnh, mô hình trả lời theo một khuôn cố định gồm loại sự cố, mức ưu "
        "tiên, có nguy hiểm hay không, câu hỏi cần hỏi lại cư dân và nhóm thợ xử lý. Mô hình chạy ngay trên máy chủ "
        "của Ban quản lý bằng phần mềm Ollama [9], nên mô tả của cư dân không phải gửi ra dịch vụ bên ngoài. Phần thiết "
        "kế dữ liệu dựa trên mô hình quan hệ của Codd [3] và lý thuyết chuẩn hoá trong giáo trình của Elmasri và "
        "Navathe [4]. Phần đặc tả yêu cầu theo hướng dẫn của Wiegers và Beatty [13]; các sơ đồ vẽ theo chuẩn UML [8].",
    "Hình 1.6 là sơ đồ use case tổng quát.":
        "Hình 1.6 là sơ đồ use case tổng quát. Cư dân, quản lý BQL và kỹ thuật viên đều là người dùng (quan hệ tổng "
        "quát hoá) nên cùng dùng UC01 Đăng nhập; quản trị viên có mọi quyền của quản lý. UC03 “Báo sự cố” luôn bao "
        "gồm UC07 “Phân loại sự cố” (tác nhân phụ Mô hình AI) và UC08 “Điều phối sự cố” (tác nhân phụ Dịch vụ email). "
        "UC06b “Báo sự cố chưa khắc phục” là use case riêng của cư dân. UC17 – UC21 thuộc phân hệ thu phí; UC18 còn "
        "được tác vụ nền thực hiện tự động vào ngày lập phí hằng tháng của toà.",
    "Hình 3.2:":
        "Hình 3.2: UC03 – Báo sự cố luôn bao gồm UC07 Phân loại sự cố (tác nhân phụ Mô hình AI) và UC08 Điều phối sự "
        "cố (tác nhân phụ Dịch vụ email); cư dân có thể đính kèm ảnh hiện trạng (mở rộng). Hỏi lại khi mô tả mơ hồ, "
        "cảnh báo sự cố khẩn cấp và cảnh báo khi AI lỗi là bước xử lý bên trong UC07, UC08 nên được mô tả ở luồng "
        "thay thế của đặc tả (Bảng 3.2, 3.3), không vẽ thành use case.",
    "Hình 3.3:":
        "Hình 3.3: khi phân công, quản lý luôn xem danh sách sự cố và giao kỹ thuật viên (thư giao việc gửi qua Dịch "
        "vụ email); phân loại lại và từ chối yêu cầu là phần mở rộng, chỉ làm khi cần.",
    "Hình 3.4:":
        "Hình 3.4: cập nhật tiến độ luôn gồm xem việc được giao (UC15) và ghi kết quả xử lý; tạm dừng xử lý (khi chờ "
        "vật tư, lịch hẹn) và tải ảnh nghiệm thu là phần mở rộng.",
    "Hình 3.6:":
        "Hình 3.6: quản lý luôn xem các thanh toán chờ duyệt; duyệt, từ chối, thu tiền trực tiếp và hoàn tiền là phần "
        "mở rộng, chỉ làm khi có việc tương ứng.",
    "–Nơi chốn:":
        "–Nơi chốn và cư trú: Khu có nhiều ToaNha; ToaNha có nhiều CanHo, mỗi căn ghi số tầng của mình (tầng "
        "không có thông tin riêng nên không thành lớp). NguoiDung và CanHo liên kết qua lớp CuTru (người nào ở căn nào, từ "
        "ngày nào đến ngày nào). Đây là liên kết thường, không phải hợp thành: xoá căn không xoá người.",
    "–CanHo gồm các PhiDinhKy và ChiSoDichVu":
        "–LoaiDichVu là bảng giá: mỗi khoản thu (phí quản lý, điện, nước, gửi xe, sửa chữa) là một loại dịch vụ "
        "có đơn giá và cách tính. CanHo có các ChiSoDichVu cho dịch vụ tính theo chỉ số và các DangKyDichVu cho dịch "
        "vụ căn đăng ký (chỉ ghi số lượng; xoá căn thì đăng ký xoá theo nên là hợp thành). Mỗi ChiTietHoaDon là một "
        "loại dịch vụ trên một hoá đơn: số lượng là diện tích, lượng tiêu thụ hoặc số đăng ký, đơn giá lấy từ bảng "
        "giá lúc lập (BR-TP-01).",
    "Hình 3.11:":
        "Hình 3.11: cư dân gửi yêu cầu; hệ thống kiểm tra cư dân đang ở căn đó và dữ liệu nhập. Không hợp lệ thì báo "
        "lỗi và dừng. Hợp lệ thì phân loại: chỉ khi AI bật mới gọi mô hình AI, kết quả được gộp với lựa chọn của cư "
        "dân. Quy trình sự cố tính hạn xử lý theo mức ưu tiên rồi lưu yêu cầu cùng dòng lịch sử đầu. Lưu xong mới "
        "điều phối, nên lỗi điều phối không làm mất yêu cầu.",
    "Hình 3.12:":
        "Hình 3.12: chi tiết bước điều phối. Mô tả rõ và có thợ phù hợp thì chọn thợ ít việc nhất, giao việc (yêu cầu "
        "sang Đã tiếp nhận) và gửi thư giao việc; mức Khẩn cấp thì gửi thêm thư theo dõi cho quản lý. Ngược lại, hệ "
        "thống gửi thư nhờ quản lý phân công.",
    "Hình 3.13:":
        "Hình 3.13: kỹ thuật viên chỉ tìm được yêu cầu được giao cho mình; không thấy thì báo không tìm thấy "
        "(BR-SC-04). Vi phạm bảng chuyển trạng thái hoặc thiếu kết quả xử lý thì báo lỗi, không lưu gì; hợp lệ thì "
        "lưu kết quả, ảnh và dòng lịch sử trong cùng một giao dịch.",
    "Hình 3.14:":
        "Hình 3.14: cư dân đã gửi xác nhận chuyển khoản, thanh toán đang Chờ xác nhận. Khi quản lý duyệt, hệ thống đọc "
        "lại thanh toán và hoá đơn, kiểm tra trạng thái và số còn lại tại lúc duyệt. Không hợp lệ thì báo lỗi, không "
        "lưu; hợp lệ thì lưu thanh toán Thành công, số đã trả và trạng thái hoá đơn trong cùng một giao dịch.",
    "Bốn sơ đồ tuần tự mô tả":
        "Bốn sơ đồ tuần tự mô tả ba luồng: cư dân báo sự cố có AI phân loại (tách làm hai hình: phân loại và lưu, "
        "điều phối sau khi lưu), kỹ thuật viên hoàn tất xử lý, và quản lý duyệt xác nhận chuyển khoản của cư dân. Khung "
        "opt là đoạn chỉ chạy khi điều kiện đúng, khung alt là các nhánh loại trừ nhau; thông điệp nét đứt là giá trị "
        "trả về.",
}
BR_MECH = {  # mã ràng buộc theo Chương 2 bản gọn (RB: trên một bảng, LB: liên bộ, liên quan hệ)
    "BR-SC-01": "Ứng dụng, kiểm tra trước khi lưu (LB01)", "BR-SC-02": "CHECK (RB06)",
    "BR-SC-03": "Ứng dụng và trigger TG1 (LB03)", "BR-SC-05": "Ứng dụng; trigger TG1 (LB03)",
    "BR-SC-06": "CHECK (RB07) và ứng dụng", "BR-SC-07": "Ứng dụng, hàm FN1 (LB05) và CHECK (RB05)",
    "BR-SC-08": "Khoá ngoại tới MucUuTien, chặn xoá (mục 2.3)", "BR-SC-09": "Ứng dụng: bước gộp kết quả AI",
    "BR-SC-13": "Bảng LichSuYeuCau (LB04)", "BR-SC-14": "CHECK (RB05), UNIQUE (SoYeuCau)",
    "BR-CH-01": "UNIQUE (MaCanHo), CHECK (RB01)", "BR-CH-02": "CHECK (RB01)",
    "BR-CD-01": "CHECK (RB03), UNIQUE có điều kiện và ứng dụng (LB13)",
    "BR-CD-02": "Ứng dụng: mã mời ký số (LB14); email duy nhất",
    "BR-TP-01": "UNIQUE có điều kiện, khoá chính (HoaDonId, LoaiDichVuId) và ứng dụng (LB10, LB11)",
    "BR-TP-02": "CHECK (RB10), UNIQUE (CanHoId, LoaiDichVuId, Ky), trigger TG2 (LB08), ứng dụng (LB16)",
    "BR-TP-03": "CHECK (RB13), trigger TG3 (LB06) và ứng dụng", "BR-TP-05": "CHECK (RB13), ứng dụng (LB12)",
    "BR-TP-06": "UNIQUE (SoHoaDon), UNIQUE (SoThanhToan)",
    "BR-TP-07": "Cột tự tính TongTien, ConNo; CHECK (RB11); trigger TG3; khung nhìn đối chiếu (LB07)",
}
BR_TEXT = {
    "BR-SC-01": "Chỉ cư dân đang ở (cư trú chưa chuyển đi, đã xác nhận qua thư mời) mới được báo sự cố, cho căn của "
                "mình hoặc khu vực chung của toà mình.",
    "BR-TP-01": "Mỗi căn, mỗi kỳ chỉ có một hoá đơn phí tháng chưa huỷ; căn chưa bàn giao không lập. Hoá đơn có mỗi "
                "loại dịch vụ một dòng: phí quản lý (số lượng là diện tích căn), điện nước (lượng tiêu thụ theo chỉ "
                "số của kỳ), dịch vụ căn đăng ký (số lượng đăng ký); đơn giá lấy từ bảng giá và chốt trên dòng. Hạn "
                "thanh toán = ngày lập + 7 ngày.",
    "BR-CH-01": "Mã căn không trùng; mỗi căn thuộc một toà và ghi số tầng từ 1 trở lên.",
    "BR-CD-02": "Mỗi người một tài khoản (NguoiDung). Cư trú chỉ có hiệu lực trên cổng cư dân khi người đó bấm thư mời "
                "do BQL gửi (hạn 14 ngày) để kích hoạt tài khoản hoặc xác nhận căn.",
    "BR-TP-07": "Tổng tiền và còn nợ do cơ sở dữ liệu tự tính: tổng tiền = tiền phí + phí trễ hạn; còn nợ = tổng tiền − "
                "đã thu, không âm (mục 2.4).",
}
DECISIONS_38 = [
    ["Dữ liệu hợp lệ?", "có / không (quay lại nhập)", "Màn hình báo sự cố: kiểm tra biểu mẫu và vị trí khu vực chung"],
    ["AI đang bật?", "có / không", "Dịch vụ phân loại: kiểm tra cấu hình máy chủ AI"],
    ["Kết quả hợp lệ?", "có / không (lỗi, hết thời gian chờ)", "Hệ thống: kiểm tra kết quả AI trả về; lỗi thì báo quản trị"],
    ["Mô tả rõ ràng?", "có / không (AI hỏi lại)", "Bước điều phối: dựa vào câu hỏi làm rõ của AI"],
    ["Có thợ phù hợp?", "có / không", "Bước điều phối: nhóm thợ theo loại sự cố, chọn người ít việc nhất"],
]
OPS_DESC = {
    "dsDuAn": "Liệt kê các dự án thuộc khu vực", "taoMoi": "Tạo mới", "capNhat": "Sửa thông tin",
    "doiTinhTrang": "Đổi tình trạng bàn giao, sửa chữa",
    "dangCoNguoiO": "Có người cư trú chưa chuyển đi hay không",
    "layNguoiDaiDien": "Lấy lần cư trú của người đại diện đang ở", "themNguoiCuTru": "Thêm người ở căn, gửi thư mời",
    "xacNhanQuaThuMoi": "Người ở xác nhận qua thư mời", "chuyenDi": "Ghi ngày chuyển đi",
    "dangO": "Còn đang ở hay không", "dangNhap": "Xác thực tài khoản", "kichHoat": "Đặt mật khẩu để kích hoạt",
    "khoaTaiKhoan": "Khoá hoặc mở khoá tài khoản", "xemViecDuocGiao": "Liệt kê yêu cầu được giao cho mình",
    "luuPhanQuyen": "Lưu tập quyền của vai trò", "dsVaiTro": "Liệt kê vai trò đang giữ quyền",
    "tinhHan": "Tính hạn hoàn tất từ ngày báo", "guiYeuCau": "Gửi yêu cầu, ghi dòng lịch sử đầu tiên",
    "phanLoaiBangAI": "Gọi mô hình AI, gộp với lựa chọn của cư dân", "tinhHanXuLy": "Chốt hạn xử lý theo mức ưu tiên",
    "giaoViec": "Giao hoặc giao lại người phụ trách", "chuyenTrangThai": "Chuyển trạng thái theo bảng chuyển",
    "hoanTat": "Ghi kết quả xử lý, chuyển Đã xử lý", "xacNhanVaDanhGia": "Cư dân xác nhận, chấm điểm, đóng yêu cầu",
    "quaHan": "Cho biết yêu cầu đã quá hạn chưa", "luuAnh": "Lưu một ảnh hiện trạng hoặc nghiệm thu",
    "ghiLichSu": "Ghi một dòng cho mỗi lần đổi trạng thái hoặc giao việc", "ghiNhatKy": "Ghi thao tác thay đổi dữ liệu",
    "xemNhatKy": "Tra cứu nhật ký", "capNhatDonGia": "Đổi đơn giá", "ghiChiSo": "Ghi chỉ số của căn trong kỳ",
    "nhapTheoToa": "Nhập chỉ số nhiều căn của một toà",
    "daXuatHoaDon": "Căn đã có hoá đơn phí tháng chưa huỷ của kỳ này hay không",
    "tieuThu": "Chỉ số mới trừ chỉ số cũ",
    "dangKy": "Đăng ký dịch vụ cho căn hoặc đổi số lượng", "huyDangKy": "Huỷ đăng ký dịch vụ của căn",
    "lapHoaDonThang": "Lập hoá đơn tháng: phí quản lý, điện nước theo chỉ số, dịch vụ đã đăng ký",
    "lapHoaDonPhatSinh": "Lập hoá đơn phát sinh (chi phí sửa chữa…)", "congPhiTre": "Cộng phí trễ hạn theo tỉ lệ của toà",
    "huy": "Huỷ hoá đơn chưa có thanh toán thành công", "tinhThanhTien": "Số lượng nhân đơn giá",
    "guiXacNhan": "Cư dân gửi xác nhận đã chuyển khoản", "ghiNhanTrucTiep": "Quản lý thu tiền mặt hoặc chuyển khoản",
    "duyet": "Duyệt khoản chờ xác nhận", "tuChoi": "Từ chối khoản chờ xác nhận", "hoanTien": "Hoàn một phần hoặc toàn bộ",
}


def table_after(cap_prefix):
    cap = find_p(cap_prefix, "ChuthichBang")
    t = cap.getnext()
    assert t.tag == qn("w:tbl"), cap_prefix
    return t


def fix_text():
    ch2 = gon_bao_cao.ngoai_chuong2(body)       # Chương 2 do noi_dung_ch2 sinh: không áp phép thay chữ của nhóm
    for el in paras():
        if el in ch2:
            continue
        s = text(el)
        for k, v in REWRITE.items():
            if s.startswith(k) and style_of(el) in ("Noidung", "Gachdaudong"):
                set_text(el, v)
                break
        else:
            new = s
            for a, b in REPL:
                new = new.replace(a, b)
            new = ten_moi_tuan8.doi_ten(new)
            if new != s:
                set_text(el, new)
    for tbl in body.iter(qn("w:tbl")):
        for p in tbl.iter(qn("w:p")):
            if p in ch2:
                continue
            s = text(p)
            new = s
            for a, b in REPL:
                new = new.replace(a, b)
            new = ten_moi_tuan8.doi_ten(new)
            if new != s:
                set_text(p, new)
    # Bảng 1.8: cơ chế đảm bảo theo mã ràng buộc mới
    t18 = table_after("Bảng 1.8.")
    for tr in t18.findall(qn("w:tr"))[1:]:
        tcs = tr.findall(qn("w:tc"))
        code = text(tcs[0]).strip()
        if code in BR_MECH:
            set_text(tcs[2].find(qn("w:p")), BR_MECH[code])
        if code in BR_TEXT:
            set_text(tcs[1].find(qn("w:p")), BR_TEXT[code])
    gon_bao_cao.sua_o(body, set_text)


def main():
    rebuild_chapter2()
    replace_figures()
    fix_text()
    gon_bao_cao.gop_uc_vao_fr(body, new_table, set_text)
    gon_bao_cao.bo_bang_chuong3(body, set_text)
    gon_bao_cao.dua_uc_ve_chuong1(body, set_text, new_p)
    gon_bao_cao.bo_muc_uc_chuong3(body, set_text)
    gon_bao_cao.gop_so_do_lop(body, set_text)
    gon_bao_cao.gon_chuong3(body, set_text)
    gon_bao_cao.bo_viet_tat_thua(body)
    gon_bao_cao.sua_trang_thai_tuan(body, set_text)
    gon_bao_cao.sua_muc_tinh_chat(body, set_text)
    gon_bao_cao.sua_o_theo_dong(body, set_text)
    print(f"gạch đầu dòng sửa lại tab: {gon_bao_cao.sua_gach_dau_dong(body)}")
    treo, doi = gon_bao_cao.danh_so_lai(body, set_text)
    print(f"đánh số lại {doi} bảng; tham chiếu treo: {treo or 'không'}")
    treo, doi = gon_bao_cao.danh_so_hinh(body, set_text, {"3.2": "1", "3.4": "1"})
    print(f"dẫn bảng, hình: không áp được {gon_bao_cao.dan_bang_hinh(body, set_text, new_p) or 'không'}")
    print(f"đánh số lại {doi} hình; tham chiếu hình treo: {treo or 'không'}")
    groups, nland = landscape_sections()
    doc.save(str(OUT))
    leftovers = [w for w in ("MucSla", "CuDan", "MUC_SLA", "YEU_CAU_SU_CO", "[P1]", "NgayTiepNhan", "SoLuongTang",
                             "DaKichHoat", "KhuVuc", "DuAn", "GiamGia", "ThueHoaDon", "TrangThaiTruoc", "NgayBaoSuCo",
                             "HanXuLyYeuCau", "TongTienHoaDon", "ConLaiHoaDon", "DaThanhToanHoaDon", "SoPhongTam", "HuongNha",
                             "ChiPhiUocTinh", "TenNguoiGhi", "TamTinh", "mục 2.5.7", "mục 2.4.3", "mục 2.3.4",
                             "PhiDinhKy", "PHI_DINH_KY", "phí định kỳ", "LoaiKhoan", "PhiQuanLyM2", "đơn giá của toà",
                             "mục 2.7", "Hình 2.3", "Hình 2.8", "bốn ERD", "Bảng 2.8", "Bảng 2.9", "quy ước đặt tên") if w in "".join(text(p) for p in body.iter(qn("w:p")))]
    print(f"đã ghi {OUT}; {nland} hình trên trang ngang ({groups} nhóm); chữ cũ còn sót: {leftovers}")


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8")
    main()
