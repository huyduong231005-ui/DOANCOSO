"""Hàm dựng file Word theo quy định trình bày đồ án chuyên ngành (TNR 13, giãn dòng 1.5,
lề trên 2 / dưới 2.5 / trái 3 / phải 2 cm, chương 16 đậm, mục 14 đậm, tiểu mục 14 nghiêng, tiểu mục con 13 gạch chân).
"""
import copy
import re
from pathlib import Path

from docx.enum.section import WD_ORIENT, WD_SECTION
from docx.enum.style import WD_STYLE_TYPE
from docx.enum.table import WD_TABLE_ALIGNMENT
from docx.enum.text import WD_ALIGN_PARAGRAPH, WD_BREAK, WD_LINE_SPACING
from docx.oxml import OxmlElement
from docx.oxml.ns import qn
from docx.shared import Cm, Pt, RGBColor

FONT = "Times New Roman"


# ---------------------------------------------------------------- styles
def _font(style, size, bold=None, italic=None, underline=None):
    f = style.font
    f.name, f.size = FONT, Pt(size)
    f.bold, f.italic, f.underline = bold, italic, underline
    f.color.rgb = RGBColor(0, 0, 0)
    rpr = style.element.get_or_add_rPr()
    rf = rpr.find(qn("w:rFonts"))
    if rf is None:
        rf = OxmlElement("w:rFonts")
        rpr.insert(0, rf)
    for k in ("w:ascii", "w:hAnsi", "w:eastAsia", "w:cs"):
        rf.set(qn(k), FONT)


def _style(doc, name, base=None, kind=WD_STYLE_TYPE.PARAGRAPH):
    try:
        return doc.styles[name]
    except KeyError:
        s = doc.styles.add_style(name, kind)
        if base:
            s.base_style = doc.styles[base]
        return s


def _outline(style, level):
    ppr = style.element.get_or_add_pPr()
    for old in ppr.findall(qn("w:outlineLvl")):
        ppr.remove(old)
    el = OxmlElement("w:outlineLvl")
    el.set(qn("w:val"), str(level))
    ppr.append(el)


def setup_styles(doc):
    body = _style(doc, "Noi dung")
    _font(body, 13)
    pf = body.paragraph_format
    pf.line_spacing_rule = WD_LINE_SPACING.ONE_POINT_FIVE
    pf.space_before, pf.space_after = Pt(0), Pt(6)
    pf.first_line_indent = Cm(1)
    pf.alignment = WD_ALIGN_PARAGRAPH.JUSTIFY

    bullet = _style(doc, "Gach dau dong", "Noi dung")
    bpf = bullet.paragraph_format
    bpf.first_line_indent, bpf.left_indent = Cm(-0.6), Cm(1.6)
    bpf.tab_stops.add_tab_stop(Cm(1.6))
    bpf.space_after = Pt(3)

    for lvl, (size, b, i, u, before, after) in enumerate(
            [(16, True, None, None, 0, 18), (14, True, None, None, 12, 6),
             (14, None, True, None, 8, 4), (13, None, None, True, 6, 3)], start=1):
        st = _style(doc, f"Heading {lvl}")
        st.base_style = None
        _font(st, size, b, i, u)
        p = st.paragraph_format
        p.line_spacing_rule = WD_LINE_SPACING.ONE_POINT_FIVE
        p.space_before, p.space_after = Pt(before), Pt(after)
        p.keep_with_next = True
        p.first_line_indent = Cm(0)
        p.alignment = WD_ALIGN_PARAGRAPH.CENTER if lvl == 1 else WD_ALIGN_PARAGRAPH.LEFT
        p.page_break_before = lvl == 1
        _outline(st, lvl - 1)

    plain = _style(doc, "Tieu de khong muc luc")
    _font(plain, 16, True)
    plain.paragraph_format.alignment = WD_ALIGN_PARAGRAPH.CENTER
    plain.paragraph_format.space_after = Pt(18)
    plain.paragraph_format.page_break_before = True

    for name, keep in (("Chu thich Bang", True), ("Chu thich Hinh", False)):
        st = _style(doc, name)
        _font(st, 13, None, True)
        p = st.paragraph_format
        p.alignment = WD_ALIGN_PARAGRAPH.CENTER
        p.space_before, p.space_after = Pt(6), Pt(10)
        p.line_spacing = 1.15
        p.keep_with_next = keep

    cell = _style(doc, "O bang")
    _font(cell, 12)
    cp = cell.paragraph_format
    cp.space_before, cp.space_after = Pt(2), Pt(2)
    cp.line_spacing = 1.1

    code = _style(doc, "Ma nguon")
    _font(code, 10)
    code.font.name = "Consolas"
    rf = code.element.get_or_add_rPr().find(qn("w:rFonts"))
    for k in ("w:ascii", "w:hAnsi", "w:cs"):
        rf.set(qn(k), "Consolas")
    code.paragraph_format.space_before = code.paragraph_format.space_after = Pt(0)
    code.paragraph_format.line_spacing = 1.0

    for lvl in (1, 2, 3):
        st = _style(doc, f"TOC {lvl}")
        _font(st, 13, True if lvl == 1 else None)
        st.paragraph_format.left_indent = Cm(0.6 * (lvl - 1))
        st.paragraph_format.space_after = Pt(3)
        st.paragraph_format.line_spacing = 1.2
    fig = _style(doc, "table of figures")
    _font(fig, 13)
    fig.paragraph_format.space_after = Pt(3)
    fig.paragraph_format.line_spacing = 1.2


# ---------------------------------------------------------------- văn bản
_INLINE = re.compile(r"(~~.+?~~|__.+?__|\*\*\*.+?\*\*\*|\*\*.+?\*\*|\*.+?\*|`.+?`)")


def runs(p, text, size=None, bold=False):
    """Thêm chữ có định dạng nhúng: __gạch dưới__, ~~gạch dưới nghiêng~~, *nghiêng*, `mã`.
    Báo cáo không in đậm chữ để nhấn mạnh trong thân bài, nên **x** / ***x*** chỉ in thường / nghiêng."""
    for part in _INLINE.split(text):
        if not part:
            continue
        if part.startswith("~~"):
            r = p.add_run(part[2:-2]); r.underline = True; r.italic = True
        elif part.startswith("__"):
            r = p.add_run(part[2:-2]); r.underline = True
        elif part.startswith("***"):
            r = p.add_run(part[3:-3]); r.italic = True
        elif part.startswith("**"):
            r = p.add_run(part[2:-2])
        elif part.startswith("`"):
            r = p.add_run(part[1:-1]); r.font.name = "Consolas"; r.font.size = Pt((size or 13) - 2)
            continue
        elif part.startswith("*"):
            r = p.add_run(part[1:-1]); r.italic = True
        else:
            r = p.add_run(part)
        if bold:
            r.bold = True
        if size:
            r.font.size = Pt(size)
    return p


class Report:
    def __init__(self, doc):
        self.doc = doc
        self.chapter = 0
        self.fig = 0
        self.tab = 0

    # tiêu đề
    def h1(self, text, chapter=None):
        if chapter is not None:
            self.chapter, self.fig, self.tab = chapter, 0, 0
        return self.doc.add_paragraph(text, style="Heading 1")

    def h2(self, text):
        return self.doc.add_paragraph(text, style="Heading 2")

    def h3(self, text):
        return self.doc.add_paragraph(text, style="Heading 3")

    def h4(self, text):
        return self.doc.add_paragraph(text, style="Heading 4")

    def plain_title(self, text):
        return self.doc.add_paragraph(text, style="Tieu de khong muc luc")

    def p(self, text, align=None, indent=True):
        para = runs(self.doc.add_paragraph(style="Noi dung"), text)
        if align:
            para.alignment = align
        if not indent:
            para.paragraph_format.first_line_indent = Cm(0)
        return para

    def bullets(self, items, mark="–"):
        for it in items:
            runs(self.doc.add_paragraph(style="Gach dau dong"), f"{mark}\t{it}")

    def numbered(self, items):
        for i, it in enumerate(items, 1):
            runs(self.doc.add_paragraph(style="Gach dau dong"), f"{i})\t{it}")

    def page_break(self):
        self.doc.add_paragraph().add_run().add_break(WD_BREAK.PAGE)

    # bảng
    def table(self, caption, headers, rows, widths, size=12, align_cols=None):
        self.tab += 1
        num = f"Bảng {self.chapter}.{self.tab}"
        self.doc.add_paragraph(f"{num}. {caption}", style="Chu thich Bang")
        t = self.doc.add_table(rows=1 + len(rows), cols=len(headers))
        t.alignment = WD_TABLE_ALIGNMENT.CENTER
        _borders(t)
        for ci, h in enumerate(headers):
            _cell(t.cell(0, ci), h, size, bold=True, center=True)
            for para in t.cell(0, ci).paragraphs:  # tiêu đề bảng không bị bỏ lại cuối trang
                para.paragraph_format.keep_with_next = True
        _repeat_header(t.rows[0])
        for ri, row in enumerate(rows, 1):
            for ci, val in enumerate(row):
                center = align_cols is not None and ci in align_cols
                _cell(t.cell(ri, ci), str(val), size, center=center)
        _fixed_widths(t, widths)
        for row in t.rows:
            row._tr.get_or_add_trPr().append(OxmlElement("w:cantSplit"))
        self.doc.add_paragraph(style="Noi dung").paragraph_format.space_after = Pt(0)
        return num

    def code(self, text, size=8.5):
        t = self.doc.add_table(rows=1, cols=1)
        t.alignment = WD_TABLE_ALIGNMENT.CENTER
        _borders(t, color="999999")
        _fixed_widths(t, [16])
        c = t.cell(0, 0)
        c.paragraphs[0].style = self.doc.styles["Ma nguon"]
        lines = text.strip("\n").split("\n")
        first = True
        for ln in lines:
            para = c.paragraphs[0] if first else c.add_paragraph(style="Ma nguon")
            first = False
            r = para.add_run(ln)
            r.font.size = Pt(size)
        self.doc.add_paragraph(style="Noi dung").paragraph_format.space_after = Pt(0)

    def figure(self, path, caption, width_cm=16, rotate=False):
        """rotate=True: hình rộng (ERD, RD) được xoay 90° ngược chiều kim đồng hồ để nằm trong trang dọc;
        báo cáo không dùng trang ngang để mọi trang cùng khổ."""
        self.fig += 1
        num = f"Hình {self.chapter}.{self.fig}"
        if rotate:
            from PIL import Image
            rotated = Path(path).with_name("_xoay_" + Path(path).name)
            Image.open(path).rotate(90, expand=True).save(rotated, dpi=(300, 300))
            path = rotated
        para = self.doc.add_paragraph()
        para.alignment = WD_ALIGN_PARAGRAPH.CENTER
        para.paragraph_format.keep_with_next = True
        para.paragraph_format.space_before = Pt(6)
        # Hình dọc rất cao thì thu bề rộng để vừa một trang (vùng in cao ~22,5 cm sau khi trừ chú thích).
        from PIL import Image
        w_px, h_px = Image.open(path).size
        width_cm = min(width_cm, 22.5 * w_px / h_px)
        para.add_run().add_picture(str(path), width=Cm(width_cm))
        if rotate:
            Path(path).unlink()
        self.doc.add_paragraph(f"{num}. {caption}", style="Chu thich Hinh")
        return num

    def next_fig(self):
        return f"Hình {self.chapter}.{self.fig + 1}"

    def next_tab(self):
        return f"Bảng {self.chapter}.{self.tab + 1}"

    # mục lục, danh mục
    def field(self, instr, placeholder="Nhấn chuột phải → Update Field để cập nhật."):
        para = self.doc.add_paragraph(style="Noi dung")
        para.paragraph_format.first_line_indent = Cm(0)
        _field(para, instr, placeholder)
        return para


def _field(para, instr, placeholder):
    def fc(kind):
        r = OxmlElement("w:r")
        f = OxmlElement("w:fldChar")
        f.set(qn("w:fldCharType"), kind)
        r.append(f)
        return r
    para._p.append(fc("begin"))
    r = OxmlElement("w:r")
    it = OxmlElement("w:instrText")
    it.set(qn("xml:space"), "preserve")
    it.text = f" {instr} "
    r.append(it)
    para._p.append(r)
    para._p.append(fc("separate"))
    para.add_run(placeholder)
    para._p.append(fc("end"))


def _shade(cell, color):
    tcpr = cell._tc.get_or_add_tcPr()
    shd = OxmlElement("w:shd")
    shd.set(qn("w:val"), "clear")
    shd.set(qn("w:color"), "auto")
    shd.set(qn("w:fill"), color)
    tcpr.append(shd)


def _cell(cell, text, size, bold=False, shade=None, center=False):
    cell.paragraphs[0].style = cell.part.document.styles["O bang"]
    parts = text.split("\n")
    for i, part in enumerate(parts):
        para = cell.paragraphs[0] if i == 0 else cell.add_paragraph(style="O bang")
        runs(para, part, size=size, bold=bold)
        if center:
            para.alignment = WD_ALIGN_PARAGRAPH.CENTER
    if shade:
        _shade(cell, shade)


def _borders(table, color="000000"):
    tblpr = table._tbl.tblPr
    b = OxmlElement("w:tblBorders")
    for edge in ("top", "left", "bottom", "right", "insideH", "insideV"):
        e = OxmlElement(f"w:{edge}")
        e.set(qn("w:val"), "single")
        e.set(qn("w:sz"), "6")
        e.set(qn("w:space"), "0")
        e.set(qn("w:color"), color)
        b.append(e)
    tblpr.append(b)
    mar = OxmlElement("w:tblCellMar")
    for edge, w in (("left", 90), ("right", 90)):
        e = OxmlElement(f"w:{edge}")
        e.set(qn("w:w"), str(w))
        e.set(qn("w:type"), "dxa")
        mar.append(e)
    tblpr.append(mar)


def _fixed_widths(table, widths):
    """Cố định độ rộng cột (Word bỏ qua độ rộng ô nếu bảng để tự co giãn)."""
    table.autofit = False
    layout = OxmlElement("w:tblLayout")
    layout.set(qn("w:type"), "fixed")
    table._tbl.tblPr.append(layout)
    for gc, w in zip(table._tbl.tblGrid.findall(qn("w:gridCol")), widths):
        gc.set(qn("w:w"), str(int(w * 567)))
    for row in table.rows:
        for cell, w in zip(row.cells, widths):
            cell.width = Cm(w)


def _repeat_header(row):
    trpr = row._tr.get_or_add_trPr()
    h = OxmlElement("w:tblHeader")
    h.set(qn("w:val"), "true")
    trpr.append(h)


# ---------------------------------------------------------------- section
def page_setup(section, landscape=False):
    section.orientation = WD_ORIENT.LANDSCAPE if landscape else WD_ORIENT.PORTRAIT
    w, h = Cm(21.0), Cm(29.7)
    section.page_width, section.page_height = (h, w) if landscape else (w, h)
    section.top_margin, section.bottom_margin = Cm(2), Cm(2.5)
    section.left_margin, section.right_margin = Cm(3), Cm(2)
    section.footer_distance = Cm(1.2)
    sp = section._sectPr
    for el in sp.findall(qn("w:pgBorders")):
        sp.remove(el)


def page_numbers(section, fmt=None, start=None):
    sp = section._sectPr
    for el in sp.findall(qn("w:pgNumType")):
        sp.remove(el)
    if fmt or start:
        el = OxmlElement("w:pgNumType")
        if fmt:
            el.set(qn("w:fmt"), fmt)
        if start:
            el.set(qn("w:start"), str(start))
        # pgNumType phải đứng sau pgBorders/lnNumType và trước cols theo schema
        cols = sp.find(qn("w:cols"))
        if cols is not None:
            cols.addprevious(el)
        else:
            sp.append(el)


def footer_page_field(section):
    section.footer.is_linked_to_previous = False
    para = section.footer.paragraphs[0]
    para.alignment = WD_ALIGN_PARAGRAPH.CENTER
    _field(para, "PAGE", "1")
    for r in para.runs:
        r.font.name, r.font.size = FONT, Pt(12)


def new_section(doc, landscape=False):
    sec = doc.add_section(WD_SECTION.NEW_PAGE)
    page_setup(sec, landscape)
    page_numbers(sec)
    return sec


__all__ = ["Report", "setup_styles", "page_setup", "page_numbers", "footer_page_field", "new_section", "runs", "copy"]
