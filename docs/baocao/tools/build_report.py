"""Sinh báo cáo đồ án chuyên ngành (nội dung tuần 1-4) thành file Word.

Chạy: python docs/baocao/tools/build_report.py  ->  docs/baocao/BaoCao_DACN_Tuan1-4.docx
Trang bìa lấy nguyên từ mẫu [3] Trang bia DACN (đã chuyển sang .docx, đặt ở docs/baocao/tools/trang_bia.docx).
Sau khi sinh, chạy update_fields.ps1 để Word cập nhật mục lục, danh mục bảng/hình và xuất PDF.
"""
import copy
from pathlib import Path

from docx import Document
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.shared import Cm, Pt

from docx_helpers import Report, footer_page_field, new_section, page_numbers, page_setup, setup_styles
from report_ch3 import chapter3

HERE = Path(__file__).resolve().parent
IMG = HERE.parent / "images"
OUT = HERE.parent / "BaoCao_DACN_Tuan1-4.docx"

TITLE = "HỆ THỐNG QUẢN LÝ VẬN HÀNH CHUNG CƯ TÍCH HỢP AI PHÂN LOẠI VÀ ĐIỀU PHỐI SỰ CỐ"
TITLE_VI = "Hệ thống quản lý vận hành chung cư tích hợp AI phân loại và điều phối sự cố"
# Theo phiếu theo dõi tiến độ BM03/QT05/ĐT-KT của nhóm.
GVHD = "ThS. Phan Đức Trí"
STUDENTS = [("Dương Gia Huy", "2380600791"), ("Lý Như Anh", "2380600056")]
LOP, CHUYEN_NGANH = "23DTHE1", "HỆ THỐNG THÔNG TIN ỨNG DỤNG"


# =====================================================================
# Trang bìa
# =====================================================================
def fill_cover(doc):
    def set_text(par, text):
        first = par.runs[0]
        for r in par.runs[1:]:
            r._r.getparent().remove(r._r)
        first.text = text

    def add(par, text, bold=True):
        size = par.runs[0].font.size
        r = par.add_run(text)
        r.font.size, r.bold = size, bold

    # Tên đề tài 2-3 dòng và hai sinh viên: bớt 5 dòng trống để trang bìa vẫn vừa một trang.
    paras = doc.paragraphs
    title_i = next(i for i, p in enumerate(paras) if "<TÊN ĐỀ TÀI" in p.text)
    head_i = next(i for i, p in enumerate(paras) if p.text.strip() == "ĐỒ ÁN CHUYÊN NGÀNH")
    gv_i = next(i for i, p in enumerate(paras) if p.text.startswith("Giảng viên hướng dẫn"))
    drop = (head_i - 1, head_i - 2, head_i - 3, title_i + 1, title_i + 2, gv_i - 1, gv_i - 2)
    for i in drop:
        assert not paras[i].text.strip()
    for i in sorted(drop, reverse=True):
        paras[i]._p.getparent().remove(paras[i]._p)

    for par in doc.paragraphs:
        t = par.text
        if "<TÊN ĐỀ TÀI" in t:
            set_text(par, TITLE)
        elif "<TÊN NGÀNH>" in t:
            par.runs[-1].text = "CÔNG NGHỆ THÔNG TIN"
        elif "<TÊN CHUYÊN NGÀNH>" in t:
            par.runs[-1].text = CHUYEN_NGANH
        elif t.startswith("Giảng viên hướng dẫn"):
            add(par, " " + GVHD)
        elif t.startswith("MSSV"):
            # Một dòng mỗi sinh viên (họ tên, MSSV); dòng mẫu chỉ có chỗ cho một người. Bìa không ghi lớp (theo bản nhóm sửa).
            lines = [f"{k + 1}. {name} – MSSV: {mssv}" for k, (name, mssv) in enumerate(STUDENTS)]
            for k, text in enumerate(lines):
                line = par if k == 0 else par.__class__(copy.deepcopy(par._p), par._parent)
                if k:
                    prev._p.addnext(line._p)
                set_text(line, text)
                line.alignment = WD_ALIGN_PARAGRAPH.LEFT
                prev = line
        elif "TP. Hồ Chí Minh" in t:
            set_text(par, "TP. Hồ Chí Minh, 2026")


# =====================================================================
# Phần đầu: nhận xét, cảm ơn, cam đoan, mục lục, danh mục
# =====================================================================
def front_matter(r):
    r.h1("NHẬN XÉT CỦA GIẢNG VIÊN HƯỚNG DẪN")
    for _ in range(20):
        r.p("." * 118, indent=False)
    r.p("TP. Hồ Chí Minh, ngày ..... tháng ..... năm 2026", align=WD_ALIGN_PARAGRAPH.RIGHT, indent=False)
    r.p("Giảng viên hướng dẫn", align=WD_ALIGN_PARAGRAPH.RIGHT, indent=False)
    r.p("*(Ký và ghi rõ họ tên)*", align=WD_ALIGN_PARAGRAPH.RIGHT, indent=False)

    r.h1("LỜI CẢM ƠN")
    r.p("Chúng em xin chân thành cảm ơn Quý Thầy Cô Khoa Công nghệ Thông tin, Trường Đại học Công nghệ TP. Hồ Chí Minh "
        "đã truyền đạt kiến thức nền tảng về phân tích, thiết kế hệ thống thông tin và cơ sở dữ liệu trong suốt quá "
        f"trình học tập. Đặc biệt, chúng em xin gửi lời cảm ơn đến Thầy {GVHD} đã định hướng đề tài, góp ý phương pháp "
        "làm việc theo từng tuần và yêu cầu nhóm bám sát nghiệp vụ, dữ liệu trước khi nghĩ đến công nghệ.")
    r.p(f"Thông qua đề tài “{TITLE_VI}”, nhóm có cơ hội vận dụng quy trình khảo sát nghiệp vụ, phân tích yêu cầu, mô "
        "hình hoá dữ liệu và đưa một mô hình ngôn ngữ vào một quy trình nghiệp vụ có kiểm soát. Báo cáo này trình bày "
        "kết quả của bốn tuần đầu và sẽ được hoàn thiện dần theo lộ trình của học phần.")
    r.p("Dù đã cố gắng, báo cáo khó tránh khỏi thiếu sót. Chúng em rất mong nhận được ý kiến đóng góp của Thầy Cô để "
        "đề tài được hoàn thiện hơn. Chúng em xin chân thành cảm ơn.")

    r.h1("LỜI CAM ĐOAN")
    r.p(f"Chúng em xin cam đoan đồ án chuyên ngành với đề tài “{TITLE_VI}” là kết quả thực hiện của nhóm dưới sự hướng "
        f"dẫn của Thầy {GVHD}. Các nội dung tham khảo đều được trích dẫn nguồn trong mục Tài liệu tham khảo.")
    r.p("Ban quản lý chung cư, các toà nhà, căn hộ, cư dân và số liệu trong báo cáo là dữ liệu mô phỏng phục vụ học "
        "tập. Các ngưỡng nghiệp vụ như thời hạn SLA được ghi rõ là giả định của đề tài, không phải chính sách đã xác "
        "minh tại một đơn vị thực tế. Số liệu đánh giá mô hình AI là kết quả đo thật trên bộ ca kiểm thử của đề tài, "
        "chưa phải kết quả vận hành tại chung cư.")
    r.p("AI (trợ lý lập trình) được sử dụng như công cụ hỗ trợ soạn thảo, rà soát và viết mã; mọi quyết định nghiệp vụ, "
        "thiết kế dữ liệu và kết luận trong báo cáo do nhóm chịu trách nhiệm giải thích. Chúng em xin chịu trách nhiệm "
        "về tính trung thực của nội dung báo cáo.")
    r.p("TP. Hồ Chí Minh, ngày 01 tháng 11 năm 2026", align=WD_ALIGN_PARAGRAPH.RIGHT, indent=False)
    r.p("Sinh viên thực hiện", align=WD_ALIGN_PARAGRAPH.RIGHT, indent=False)
    for name, _ in STUDENTS:
        r.p(name, align=WD_ALIGN_PARAGRAPH.RIGHT, indent=False)

    r.plain_title("MỤC LỤC")
    r.field('TOC \\o "1-3" \\h \\z \\u')

    r.h1("DANH MỤC CÁC KÝ HIỆU, CÁC CHỮ VIẾT TẮT")
    abbr = [
        ("AI", "Artificial Intelligence – trí tuệ nhân tạo"),
        ("AS-IS / TO-BE", "Quy trình hiện tại / quy trình đề xuất"),
        ("BQL", "Ban quản lý (chung cư)"),
        ("BR", "Business Rule – quy tắc nghiệp vụ"),
        ("CSAT", "Customer Satisfaction – điểm hài lòng của cư dân (1–5)"),
        ("CSDL", "Cơ sở dữ liệu"),
        ("ERD", "Entity Relationship Diagram – mô hình thực thể kết hợp"),
        ("FK / PK / UQ", "Khoá ngoại / khoá chính / ràng buộc duy nhất"),
        ("FR / NFR", "Yêu cầu chức năng / yêu cầu phi chức năng"),
        ("GGUF", "Định dạng tệp mô hình đã lượng tử hoá dùng cho llama.cpp / Ollama"),
        ("GPU", "Graphics Processing Unit – bộ xử lý đồ hoạ, dùng để chạy mô hình AI"),
        ("HTTT", "Hệ thống thông tin"),
        ("JSON", "JavaScript Object Notation – định dạng dữ liệu trao đổi"),
        ("KPI", "Key Performance Indicator – chỉ số đo hiệu quả"),
        ("KTV", "Kỹ thuật viên (thợ điện, nước, máy lạnh, mộc, nhôm kính)"),
        ("LLM", "Large Language Model – mô hình ngôn ngữ lớn"),
        ("LoRA / QLoRA", "Low-Rank Adaptation / LoRA trên mô hình lượng tử hoá 4-bit"),
        ("MTTR", "Mean Time To Resolve – thời gian xử lý trung bình"),
        ("RD", "Relational Diagram – lược đồ quan hệ"),
        ("SLA", "Service Level Agreement – thời hạn phản hồi, hoàn tất cam kết"),
        ("SMTP", "Simple Mail Transfer Protocol – giao thức gửi email"),
        ("SRS", "Software Requirements Specification – đặc tả yêu cầu phần mềm"),
        ("UC", "Use Case – ca sử dụng"),
        ("UML", "Unified Modeling Language – ngôn ngữ mô hình hoá thống nhất"),
        ("3NF", "Third Normal Form – dạng chuẩn 3"),
    ]
    r.chapter = 0
    t = r.doc.add_table(rows=1 + len(abbr), cols=2)
    from docx_helpers import _borders, _cell
    _borders(t)
    _cell(t.cell(0, 0), "Từ viết tắt", 13, bold=True, center=True)
    _cell(t.cell(0, 1), "Ý nghĩa", 13, bold=True, center=True)
    for i, (a, m) in enumerate(abbr, 1):
        _cell(t.cell(i, 0), a, 13)
        _cell(t.cell(i, 1), m, 13)
    from docx_helpers import _fixed_widths
    _fixed_widths(t, [4, 12])

    r.h1("DANH MỤC CÁC BẢNG")
    r.field('TOC \\h \\z \\t "Chu thich Bang,1"')
    r.h1("DANH MỤC CÁC HÌNH VẼ, ĐỒ THỊ")
    r.field('TOC \\h \\z \\t "Chu thich Hinh,1"')


# =====================================================================
# Chương 1
# =====================================================================
def chapter1(r):
    r.h1("CHƯƠNG 1. TỔNG QUAN VÀ PHÂN TÍCH NGHIỆP VỤ", chapter=1)
    r.p("Chương này trả lời câu hỏi “tổ chức nào đang gặp vấn đề gì” trước khi nói đến công nghệ: mô tả đơn vị, quy "
        "trình tiếp nhận và xử lý sự cố hiện tại, điểm bất cập, quy trình đề xuất, quy tắc nghiệp vụ, chỉ số đánh giá, "
        "đặc tả yêu cầu sơ bộ và use case sơ bộ. Đây là sản phẩm của tuần 1 (phiếu đề tài) và tuần 2 (Chương 1).")

    # ---------------- 1.1
    r.h2("1.1. Giới thiệu đề tài")
    r.h3("1.1.1. Bối cảnh")
    r.p("Một khu chung cư thương mại thường do một Ban quản lý (BQL) hoặc đơn vị quản lý vận hành chịu trách nhiệm vận "
        "hành, bảo trì hệ thống kỹ thuật chung và hỗ trợ cư dân. Luật Nhà ở 2023 [10] và quy chế quản lý, sử dụng nhà "
        "chung cư ban hành kèm Thông tư 05/2024/TT-BXD [2] xác định trách nhiệm của đơn vị quản lý vận hành đối với phần "
        "sở hữu chung và việc bảo trì. Trong các nghiệp vụ hằng ngày của BQL, tiếp nhận và xử lý sự cố kỹ thuật (điện, "
        "nước, thiết bị, cửa, thang máy, vệ sinh, côn trùng…) là nghiệp vụ phát sinh liên tục, ảnh hưởng trực tiếp đến "
        "an toàn và mức độ hài lòng của cư dân.")
    r.p("Đề tài mô phỏng Ban quản lý (BQL) của một khu căn hộ thương mại tại TP. Hồ Chí Minh. BQL quản lý hai khu: Khu 1 "
        "gồm Toà A 20 tầng × 8 căn, Toà B 18 tầng × 8 căn, Toà C 15 tầng × 6 căn; Khu 2 gồm Toà R1, R2 cùng 12 tầng × 6 "
        "căn, tổng cộng 5 toà, 77 tầng, 538 căn hộ. Đội kỹ thuật gồm thợ điện, thợ nước, thợ "
        "máy lạnh, thợ mộc và thợ nhôm kính; các việc khác như an ninh, thang máy, vệ sinh, côn trùng, kế toán do các bộ "
        "phận hoặc đơn vị bên ngoài đảm nhận. Quy mô này là giả định của đề tài, được nạp sẵn làm dữ liệu thử.")
    r.h3("1.1.2. Vấn đề nghiệp vụ")
    r.p("Theo hướng dẫn của học phần, đề tài được xác định bằng sáu câu hỏi nghiệp vụ trước khi chọn công nghệ. "
        f"{r.next_tab()} là phiếu đề xuất đề tài (sản phẩm tuần 1).")
    r.table("Phiếu đề xuất đề tài theo sáu câu hỏi nghiệp vụ", ["Câu hỏi", "Trả lời của đề tài"], [
        ("1. Đơn vị nào đang gặp vấn đề?", "Ban quản lý một khu căn hộ tại TP. HCM (mô phỏng): 2 khu, 5 toà, 538 căn; bộ phận "
         "lễ tân, kỹ thuật và quản lý."),
        ("2. Quy trình hiện tại?", "Cư dân gọi hotline, nhắn Zalo hoặc đến quầy; lễ tân ghi sổ/Excel; kỹ thuật trưởng "
         "chọn thợ theo kinh nghiệm và gọi điện giao việc; thợ báo miệng khi xong; cuối tháng tổng hợp báo cáo thủ công."),
        ("3. Bất cập ở bước nào?", "Ghi nhận thiếu thông tin, phân loại và xếp ưu tiên cảm tính, không biết thợ nào đang "
         "quá tải, không có kết quả nghiệm thu, cư dân không theo dõi được tiến độ, không đo được thời gian xử lý (mục 1.3.5)."),
        ("4. Ai tham gia?", "Cư dân, lễ tân/nhân viên BQL, kỹ thuật trưởng/quản lý BQL, kỹ thuật viên, quản trị hệ thống."),
        ("5. Dữ liệu, chứng từ?", "Sổ tiếp nhận sự cố, phiếu yêu cầu sửa chữa, ảnh hiện trạng, phiếu nghiệm thu, danh bạ "
         "thợ, báo cáo sự cố tháng; danh mục khu – toà – tầng – căn và danh sách cư dân."),
        ("6. Hệ thống mới giúp gì?", "Cư dân tự gửi yêu cầu có cấu trúc; AI gợi ý loại và mức ưu tiên, không bỏ sót sự cố "
         "nguy hiểm; tự giao thợ đúng chuyên môn, ít việc nhất; theo dõi trạng thái, hạn SLA, lịch sử; báo cáo KPI tức thời."),
    ], [4.2, 11.8])
    r.p(f"Tên đề tài: {TITLE_VI}. Đề tài "
        "đi theo hướng C – Hệ thống thông tin tích hợp AI trong định hướng của học phần: trọng tâm vẫn là quy trình "
        "nghiệp vụ và dữ liệu sự cố; AI chỉ là một bước hỗ trợ phân loại bên trong quy trình, luôn có quy tắc nghiệp vụ "
        "giới hạn và có phương án khi AI sai hoặc không phản hồi.")

    r.h3("1.1.3. Các giải pháp và nghiên cứu liên quan")
    r.p("Bài toán tiếp nhận – xử lý sự cố thuộc nhóm hệ thống quản lý bảo trì (CMMS) và quản lý sự cố theo ITIL [1]: mỗi "
        "sự cố là một phiếu có vòng đời trạng thái, mức ưu tiên, người phụ trách và cam kết thời hạn (SLA). Các phần mềm "
        "quản lý toà nhà thương mại hiện nay đều có cổng cư dân và phiếu yêu cầu, nhưng việc phân loại, xếp ưu tiên và "
        "chọn người xử lý phần lớn vẫn do nhân viên làm thủ công.")
    r.p("Về kỹ thuật, phân loại văn bản tự do của cư dân (thường viết tắt, không dấu, nhiều ý) là bài toán phù hợp với mô "
        "hình ngôn ngữ lớn. Đề tài dùng mô hình mở Qwen3-4B [12] chạy cục bộ qua Ollama [9], tinh chỉnh bằng QLoRA [5], "
        "[6] trên dữ liệu tự xây dựng để trả về JSON có cấu trúc (loại, mức ưu tiên, nguy hiểm, câu hỏi lại, nhóm xử lý). "
        "Chạy cục bộ giúp dữ liệu cư dân không phải gửi ra dịch vụ bên ngoài. Phần mô hình hoá dữ liệu dựa trên mô hình "
        "quan hệ của Codd [3] và lý thuyết chuẩn hoá trình bày trong [4]; đặc tả yêu cầu theo [13] và UML [8].")

    # ---------------- 1.2
    r.h2("1.2. Nhiệm vụ đồ án")
    r.h3("1.2.1. Tính cấp thiết và lý do hình thành đề tài")
    r.p("Với vài trăm căn hộ, mỗi ngày BQL có thể nhận nhiều yêu cầu qua nhiều kênh khác nhau. Khi thông tin nằm rải rác "
        "trong sổ, điện thoại và tin nhắn, BQL khó trả lời ba câu hỏi cơ bản: yêu cầu nào đang quá hạn, thợ nào đang quá "
        "tải và mức độ hài lòng của cư dân ra sao. Sự cố nguy hiểm (chập điện, rò gas, nước tràn vào ổ điện) nếu bị xếp "
        "chung hàng đợi với việc nhỏ có thể gây thiệt hại người và tài sản. Vì vậy cần một hệ thống ghi nhận tập trung, "
        "có quy tắc xếp ưu tiên và điều phối rõ ràng, đo được hiệu quả xử lý.")
    r.h3("1.2.2. Mục tiêu")
    r.p("Mục tiêu tổng quát: xây dựng hệ thống thông tin quản lý quy trình sự cố của BQL chung cư từ lúc cư dân báo đến "
        "lúc cư dân xác nhận và đánh giá, có AI hỗ trợ phân loại và điều phối, có số liệu KPI để quản lý ra quyết định. "
        "Mục tiêu cụ thể:")
    r.bullets([
        "Mô hình hoá quy trình AS-IS/TO-BE, quy tắc nghiệp vụ và KPI của nghiệp vụ sự cố.",
        "Thiết kế CSDL đạt 3NF cho cơ cấu Khu – Toà – Tầng – Căn, cư dân, người dùng – vai trò và vòng đời sự cố; "
        "ràng buộc toàn vẹn được đặt ở CSDL khi có thể.",
        "Xây dựng ứng dụng web có phân quyền cho bốn nhóm người dùng: cư dân, kỹ thuật viên, quản lý BQL, quản trị viên.",
        "Tích hợp mô hình AI phân loại sự cố có kiểm soát: chỉ được nâng mức ưu tiên, có phương án khi mô hình lỗi, có "
        "bộ ca kiểm thử và số liệu đánh giá.",
        "Cung cấp báo cáo KPI: tỉ lệ hoàn tất đúng hạn SLA, thời gian phản hồi, MTTR, CSAT, tỉ lệ việc hệ thống tự giao được giữ nguyên.",
    ])
    r.h3("1.2.3. Đối tượng và phạm vi")
    r.p("**Đối tượng nghiên cứu:** quy trình tiếp nhận – xử lý sự cố kỹ thuật tại chung cư; dữ liệu cơ cấu chung cư và "
        "cư dân; kỹ thuật tích hợp mô hình ngôn ngữ vào quy trình nghiệp vụ.")
    r.p("**Trong phạm vi:** một quy trình trọng tâm chạy trọn từ đầu đến cuối là quy trình sự cố, cùng các dữ liệu nền "
        "mà quy trình này cần: cơ cấu Khu – Toà – Tầng – Căn, cư dân và tài khoản, phân quyền, mức SLA, nhật ký hệ "
        "thống, báo cáo KPI sự cố.")
    r.p("**Ngoài phạm vi đánh giá:** phân hệ phí dịch vụ – hoá đơn – thanh toán VietQR đã có từ giai đoạn trước của "
        "dự án, được giữ như phân hệ hỗ trợ nhưng không phân tích sâu trong đồ án; tích hợp cảm biến IoT, ứng dụng di động "
        "gốc, cổng thanh toán thật, chatbot hỏi đáp.")
    r.h3("1.2.4. Ý nghĩa khoa học và thực tiễn")
    r.p("Về khoa học, đề tài minh hoạ cách đặt một mô hình ngôn ngữ vào quy trình nghiệp vụ theo nguyên tắc “AI đề xuất, "
        "quy tắc quyết định”: đầu ra của AI bị ràng buộc bởi lược đồ JSON, được kiểm tra nhãn và chỉ có quyền nâng mức "
        "ưu tiên. Về thực tiễn, hệ thống cho BQL một nguồn dữ liệu duy nhất về sự cố, giúp đo được chất lượng dịch vụ "
        "thay vì cảm nhận; các lợi ích về thời gian xử lý mới là **dự kiến**, chỉ được khẳng định sau khi đo trên kịch bản "
        "thử nghiệm ở Chương 3.")

    # ---------------- 1.3
    r.h2("1.3. Khảo sát hiện trạng (AS-IS)")
    r.h3("1.3.1. Phương pháp và nguồn căn cứ nghiệp vụ")
    r.p(f"Quy trình hiện tại được tổng hợp từ các nguồn có thể truy vết ở {r.next_tab()}. Những nội dung chưa có nguồn "
        "xác minh được ghi rõ là giả định.")
    r.table("Nguồn căn cứ nghiệp vụ và mức độ xác minh", ["STT", "Nguồn", "Nội dung sử dụng", "Trạng thái"], [
        ("1", "Luật Nhà ở 2023 [10]; Thông tư 05/2024/TT-BXD [2]", "Trách nhiệm vận hành, bảo trì phần sở hữu chung của "
         "đơn vị quản lý vận hành", "Văn bản công khai"),
        ("2", "Quan sát, trao đổi với cư dân và nhân viên lễ tân tại một chung cư ở TP. HCM", "Các kênh báo sự cố, cách ghi "
         "sổ, cách gọi thợ, thời điểm báo kết quả", "Phỏng vấn trực tiếp; đơn vị không cho phép ghi âm, ghi hình nên không có biên bản"),
        ("3", "Mẫu đơn xin sửa chữa căn hộ của BQL khu chung cư Pruksa; mẫu đơn xin sửa chữa nhà phổ biến (mục 1.3.6)",
         "Trường thông tin của đơn: người báo, liên hệ, căn hộ, mô tả lỗi; cơ sở để bổ sung ảnh, kết quả, nghiệm thu",
         "Tham khảo"),
        ("4", "Thực hành quản lý sự cố ITIL [1]", "Vòng đời trạng thái, mức ưu tiên, SLA phản hồi và hoàn tất", "Tài liệu chuẩn"),
        ("5", "Giả định của đề tài", "Quy mô 5 toà, 538 căn; ngưỡng SLA theo mức ưu tiên (Bảng 1.9)", "Giả định, ghi rõ"),
    ], [1.3, 4.4, 6.2, 4.1], align_cols=[0])
    r.h3("1.3.2. Tác nhân")
    r.table("Tác nhân của quy trình sự cố", ["Tác nhân", "Vai trò trong quy trình", "Vai trò trên hệ thống"], [
        ("Cư dân", "Phát hiện và báo sự cố trong căn hoặc khu vực chung; xác nhận kết quả", "Tenant"),
        ("Lễ tân / nhân viên BQL", "Tiếp nhận yêu cầu qua điện thoại, quầy; ghi sổ (AS-IS)", "Được thay bằng cổng cư dân; "
         "có thể tạo yêu cầu thay cư dân với vai trò Manager"),
        ("Quản lý BQL (kỹ thuật trưởng)", "Đánh giá, phân công, từ chối, theo dõi hạn xử lý, xem báo cáo", "Manager"),
        ("Kỹ thuật viên", "Xử lý sự cố đúng chuyên môn, báo kết quả và ảnh nghiệm thu", "Electrician, Plumber, "
         "AcTechnician, Carpenter, Glazier"),
        ("Quản trị viên", "Quản lý tài khoản, phân quyền, nhật ký; kiêm quyền quản lý", "Admin"),
        ("Mô hình AI (tác nhân hệ thống)", "Đọc mô tả, đề xuất loại, mức ưu tiên, nguy hiểm, câu hỏi lại, nhóm xử lý", "Ollama, "
         "mô hình qwen3-suco4 (Qwen3-4B tinh chỉnh QLoRA)"),
        ("Dịch vụ email (tác nhân hệ thống)", "Gửi thư giao việc, thư cần phân công, thư cảnh báo", "SMTP"),
    ], [4.0, 6.8, 5.2])
    r.h3("1.3.3. Quy trình hiện tại")
    r.p(f"{r.next_fig()} mô tả quy trình AS-IS bằng sơ đồ hoạt động UML có chia làn (swimlane), vẽ bằng StarUML. Các ký "
        "hiệu [P1] – [P6] ghi trong bước là bất cập được phân tích ở mục 1.3.5.")
    r.figure(IMG / "Hinh 1.1 Quy trinh AS-IS.png", "Sơ đồ hoạt động quy trình tiếp nhận và xử lý sự cố hiện tại (AS-IS)", 15.5)
    r.numbered([
        "Cư dân phát hiện sự cố, gọi hotline, nhắn Zalo hoặc đến quầy lễ tân.",
        "Lễ tân ghi vào sổ tiếp nhận hoặc file Excel: căn hộ, tên, số điện thoại, mô tả ngắn.",
        "Lễ tân gọi điện báo kỹ thuật trưởng; kỹ thuật trưởng đánh giá mức độ và chọn thợ theo kinh nghiệm.",
        "Nếu không có thợ phù hợp đang rảnh, kỹ thuật trưởng hẹn lại hoặc liên hệ nhà thầu ngoài.",
        "Thợ nhận cuộc gọi, đến căn hộ kiểm tra, sửa chữa rồi báo miệng hoặc nhắn tin là đã xong.",
        "Lễ tân cập nhật sổ; cư dân có thể không được thông báo, không có bước nghiệm thu, đánh giá.",
        "Cuối tháng, BQL tổng hợp báo cáo sự cố thủ công từ sổ.",
    ])
    r.h3("1.3.4. Dữ liệu và chứng từ")
    r.table("Dữ liệu, chứng từ phát sinh trong quy trình hiện tại", ["Chứng từ / dữ liệu", "Nội dung chính", "Hạn chế"], [
        ("Sổ tiếp nhận sự cố / file Excel", "Ngày giờ, căn hộ, người báo, mô tả, người xử lý, ngày xong", "Thiếu trường "
         "bắt buộc, không có mã duy nhất, nhiều bản sao"),
        ("Phiếu yêu cầu sửa chữa", "Mô tả hỏng hóc, vị trí, chữ ký cư dân", "Thường bỏ qua với việc nhỏ"),
        ("Ảnh hiện trạng", "Ảnh cư dân gửi qua Zalo", "Nằm trong điện thoại cá nhân, không gắn với phiếu"),
        ("Phiếu nghiệm thu", "Kết quả xử lý, vật tư, chữ ký", "Hiếm khi lập; không có đánh giá hài lòng"),
        ("Danh bạ thợ, nhà thầu", "Tên, chuyên môn, số điện thoại", "Không biết khối lượng việc đang làm"),
        ("Báo cáo sự cố tháng", "Số sự cố theo loại, toà", "Tổng hợp tay, không có thời gian xử lý, không đối chiếu được"),
        ("Danh mục căn hộ, cư dân", "Toà, tầng, mã căn; chủ hộ, người thuê", "Lưu ở nhiều file, khó biết ai đang ở"),
    ], [4.3, 5.9, 5.8])
    r.h3("1.3.5. Điểm bất cập")
    r.table("Bất cập của quy trình hiện tại và hệ quả", ["Mã", "Bước", "Bất cập", "Hệ quả"], [
        ("P1", "Ghi nhận", "Thông tin tự do, thiếu vị trí, ảnh; không có mã yêu cầu", "Thợ phải gọi hỏi lại; không truy "
         "vết được một sự cố"),
        ("P2", "Phân loại, xếp ưu tiên", "Dựa vào kinh nghiệm của một người, không có tiêu chí", "Sự cố nguy hiểm có thể "
         "bị xếp chung với việc nhỏ"),
        ("P3", "Giao việc", "Gọi điện, không biết thợ nào đang có bao nhiêu việc", "Việc dồn vào vài người; chậm khi "
         "kỹ thuật trưởng vắng"),
        ("P4", "Báo kết quả", "Báo miệng, không ghi kết quả và ảnh nghiệm thu", "Không có căn cứ khi cư dân khiếu nại"),
        ("P5", "Cập nhật, thông báo", "Trạng thái chỉ nằm trong sổ", "Cư dân phải gọi hỏi tiến độ; tốn thời gian lễ tân"),
        ("P6", "Báo cáo", "Tổng hợp tay cuối tháng", "Không đo được thời gian phản hồi, đúng hạn, mức hài lòng"),
    ], [1.2, 3.0, 6.0, 5.8], align_cols=[0])
    r.h3("1.3.6. Một số biểu mẫu tham khảo")
    f1, f2 = r.next_fig(), f"Hình {r.chapter}.{r.fig + 2}"
    r.p("Để xác định các trường thông tin cần thu khi cư dân báo sự cố, nhóm đối chiếu với hai biểu mẫu đang được dùng "
        f"trong thực tế (nguồn số 3 ở Bảng 1.2). {f1} là mẫu “Đơn xin sửa chữa căn hộ” (mẫu đơn số 1) của Ban quản lý khu "
        f"chung cư Pruksa, dùng để cư dân báo lỗi kỹ thuật trong căn. {f2} là mẫu “Đơn xin sửa chữa nhà” phổ biến, dùng khi "
        "chủ nhà xin phép cơ quan chức năng được sửa chữa, cải tạo. Hai mẫu chỉ để tham khảo; tên đơn vị trên mẫu không "
        "phải đơn vị mô phỏng của đề tài.")
    r.figure(IMG / "Mau don sua chua can ho Pruksa.jpeg",
             "Mẫu “Đơn xin sửa chữa căn hộ” của Ban quản lý khu chung cư Pruksa", 12)
    r.figure(IMG / "Mau don sua chua nha.jpeg", "Mẫu “Đơn xin sửa chữa nhà” phổ biến", 11.6)
    r.p("Mẫu của Ban quản lý Pruksa chỉ thu họ tên, số điện thoại, chủ sở hữu, lô và thời gian nhận bàn giao; lỗi kỹ "
        "thuật được ghi tự do trên các dòng chấm. Mẫu không có phần phân loại sự cố, mức ưu tiên, ảnh hiện trạng, người xử "
        "lý hay xác nhận kết quả, và số phiếu để trống cho người nhận điền tay. Đây chính là các bất cập P1, P2 và P4 ở "
        "mục 1.3.5.")
    r.p("Mẫu đơn xin sửa chữa nhà thiên về xin phép thi công: người làm đơn phải khai họ tên, ngày sinh, số CMND/CCCD, "
        "địa chỉ, địa chỉ nhà cần sửa, lý do, hạng mục, diện tích, thời gian dự kiến, nguồn kinh phí và lời cam kết. Mẫu "
        "này phục vụ việc cấp phép, không phục vụ việc báo và theo dõi một sự cố đang xảy ra.")
    r.p("Từ hai mẫu, đề tài rút ra cách vận dụng như sau:")
    r.bullets([
        "Giữ các thông tin cần thiết để xử lý: người báo, số điện thoại, căn hộ hoặc khu vực chung, vị trí, mô tả sự cố.",
        "Bổ sung những gì hai mẫu còn thiếu: số yêu cầu duy nhất, ảnh hiện trạng, loại và mức ưu tiên, hạn SLA, kết quả "
        "xử lý, ảnh nghiệm thu và đánh giá của cư dân.",
        "Không bắt cư dân khai lại họ tên, số điện thoại, căn hộ; các thông tin này đã có trong hồ sơ cư dân (bảng CuDan) "
        "và hệ thống tự gắn vào phiếu.",
    ])

    # ---------------- 1.4
    r.h2("1.4. Quy trình đề xuất (TO-BE)")
    r.h3("1.4.1. Sơ đồ quy trình đề xuất")
    r.p(f"{r.next_fig()} là sơ đồ hoạt động của quy trình sau khi có hệ thống. Bước ghi [AI] là bước do mô hình AI hỗ "
        "trợ; bước ghi [Tự động] là bước tự động hoá bằng quy tắc nghiệp vụ (không phải AI).")
    r.figure(IMG / "Hinh 1.2 Quy trinh TO-BE.png", "Sơ đồ hoạt động quy trình tiếp nhận và xử lý sự cố đề xuất (TO-BE)", 15.5)
    r.p("Điểm thiết kế quan trọng là tách vai trò: mô hình AI **chỉ phân loại**; việc chọn thợ, gửi email và tính hạn "
        "SLA là mã nguồn thông thường đọc dữ liệu trong CSDL. Nhờ vậy khi đổi nhân sự hoặc thời hạn SLA, BQL chỉ sửa dữ "
        "liệu mà không cần huấn luyện lại mô hình. Khi mô tả mơ hồ hoặc không có thợ chuyên trách (thang máy, an ninh, "
        "côn trùng…), hệ thống không tự giao mà gửi thư cho quản lý kèm nhóm xử lý do AI gợi ý; quản lý vẫn là người "
        "quyết định cuối cùng.")
    r.h3("1.4.2. So sánh AS-IS và TO-BE")
    r.table("Đối chiếu các bước AS-IS và TO-BE", ["Bước", "AS-IS", "TO-BE", "Hình thức"], [
        ("Ghi nhận", "Gọi điện, ghi sổ", "Cư dân tự tạo yêu cầu có cấu trúc, ảnh; hệ thống cấp số yêu cầu", "Số hoá (P1)"),
        ("Phân loại, ưu tiên", "Kỹ thuật trưởng đánh giá", "AI đề xuất loại, ưu tiên, nguy hiểm; quy tắc chỉ cho nâng", "AI + quy tắc (P2)"),
        ("Hạn xử lý", "Không có", "Hạn = lúc báo + thời hạn của mức ưu tiên (bảng MucSla)", "Tự động"),
        ("Giao việc", "Gọi điện theo kinh nghiệm", "Thợ đúng chuyên môn có ít việc chưa xong nhất; email giao việc", "Tự động (P3)"),
        ("Xử lý", "Báo miệng", "KTV cập nhật trạng thái, ghi kết quả, ảnh nghiệm thu; tạm dừng có lý do", "Số hoá (P4)"),
        ("Theo dõi", "Cư dân gọi hỏi", "Cư dân xem trạng thái và lịch sử trên cổng cư dân", "Số hoá (P5)"),
        ("Nghiệm thu", "Không có", "Cư dân xác nhận, đánh giá 1–5 hoặc báo chưa khắc phục", "Số hoá (P4)"),
        ("Báo cáo", "Tổng hợp tay", "Báo cáo KPI theo kỳ, mức ưu tiên, loại, toà, KTV", "Tự động (P6)"),
    ], [2.9, 3.4, 6.7, 3.0])
    r.h3("1.4.3. Vòng đời trạng thái của yêu cầu sự cố")
    r.p(f"Mỗi yêu cầu sự cố đi qua tám trạng thái như {r.next_fig()}. Mỗi mũi tên ghi tác nhân được phép thực hiện; "
        f"điều kiện của từng chuyển trạng thái được liệt kê ở {r.next_tab()}.")
    r.figure(IMG / "Hinh 1.3 So do trang thai yeu cau su co.png", "Sơ đồ trạng thái (state machine) của yêu cầu sự cố", 13)
    r.table("Bảng chuyển trạng thái của yêu cầu sự cố (BR-SC-03)", ["Từ", "Sang", "Ai được làm", "Điều kiện / ghi chú"], [
        ("Mới gửi", "Đã tiếp nhận", "Quản lý, Hệ thống", "Kèm giao việc; ghi mốc tiếp nhận"),
        ("Mới gửi", "Từ chối", "Quản lý", "Bắt buộc ghi lý do"),
        ("Mới gửi", "Đã huỷ", "Cư dân, Quản lý", "Trước khi kỹ thuật bắt đầu"),
        ("Đã tiếp nhận", "Đang xử lý", "KTV được giao, Quản lý", "Phải có người phụ trách"),
        ("Đã tiếp nhận", "Từ chối / Đã huỷ", "Quản lý / Cư dân, Quản lý", "Từ chối phải có lý do"),
        ("Đang xử lý", "Tạm dừng", "KTV được giao, Quản lý", "Bắt buộc ghi lý do (chờ vật tư, hẹn lịch)"),
        ("Tạm dừng", "Đang xử lý", "KTV được giao, Quản lý", "Hạn SLA dời thêm đúng thời gian đã dừng"),
        ("Đang xử lý", "Đã xử lý", "KTV được giao, Quản lý", "Bắt buộc ghi kết quả xử lý"),
        ("Đã xử lý", "Đã đóng", "Cư dân, Quản lý", "Cư dân có thể đánh giá 1–5"),
        ("Đã xử lý", "Đang xử lý", "Cư dân, Quản lý", "Báo chưa khắc phục, bắt buộc ghi lý do"),
    ], [2.6, 3.0, 4.2, 6.2])
    r.p("Mọi chuyển trạng thái khác (ví dụ từ Đã đóng quay lại Đang xử lý, hoặc KTV thao tác trên việc không giao cho "
        "mình) đều bị từ chối. Trong mã nguồn, bảng này được cài đặt ở một nơi duy nhất là lớp `MaintenanceWorkflow`; "
        "mọi màn hình của cư dân, KTV, quản lý và bộ điều phối tự động đều đi qua lớp này nên quy tắc luôn nhất quán.")

    # ---------------- 1.5
    r.h2("1.5. Quy tắc nghiệp vụ")
    r.p(f"{r.next_tab()} liệt kê các quy tắc nghiệp vụ. Mã quy tắc được dùng lại nhất quán trong tài liệu thiết kế, mã "
        "nguồn (chú thích) và ca kiểm thử. Cột cơ chế cho biết quy tắc được đảm bảo ở CSDL hay ở tầng ứng dụng; lý do "
        "lựa chọn được giải thích ở mục 2.5.")
    r.table("Danh mục quy tắc nghiệp vụ", ["Mã", "Quy tắc", "Cơ chế đảm bảo"], [
        ("BR-SC-01", "Chỉ cư dân đang ở (bản ghi cư dân chưa chuyển đi, gắn với tài khoản) mới được báo sự cố, cho căn của "
         "mình hoặc khu vực chung của toà mình.", "Ứng dụng: kiểm tra trước khi lưu"),
        ("BR-SC-02", "Mỗi sự cố ở đúng một nơi: trong một căn, hoặc khu vực chung của một toà và phải ghi vị trí.", "CHECK "
         "CK_YeuCauBaoTri_NoiXayRa"),
        ("BR-SC-03", "Trạng thái chỉ chuyển theo Bảng 1.7; mỗi chuyển chỉ một số tác nhân được làm.", "Ứng dụng: "
         "MaintenanceWorkflow"),
        ("BR-SC-04", "Kỹ thuật viên chỉ thấy và thao tác trên việc được giao cho chính mình.", "Ứng dụng: lọc theo người "
         "phụ trách"),
        ("BR-SC-05", "Phải có người phụ trách trước khi bắt đầu xử lý; hệ thống chỉ tự giao yêu cầu mới gửi.", "Ứng dụng"),
        ("BR-SC-06", "Hoàn tất phải có kết quả xử lý; từ chối, tạm dừng, mở lại phải có lý do.", "Ứng dụng"),
        ("BR-SC-07", "Hạn xử lý = thời điểm báo + thời hạn hoàn tất của mức ưu tiên, chốt tại lúc báo; thời gian tạm dừng "
         "được cộng vào hạn. Hạn không được trước thời điểm báo.", "Ứng dụng + CHECK CK_YeuCauBaoTri_HanXuLy"),
        ("BR-SC-08", "Mức ưu tiên phải là một mức có trong bảng SLA; không xoá được mức đang dùng.", "FK → MucSla, ON DELETE "
         "RESTRICT"),
        ("BR-SC-09", "AI chỉ điền loại khi cư dân chọn “Khác”, chỉ được nâng mức ưu tiên, không hạ; sự cố nguy hiểm được "
         "nâng lên Khẩn cấp.", "Ứng dụng: MaintenanceTriageService.Apply"),
        ("BR-SC-10", "AI lỗi hoặc quá thời gian chờ thì yêu cầu vẫn được lưu theo lựa chọn của cư dân; quản trị viên nhận "
         "cảnh báo, tối đa một thư mỗi 30 phút.", "Ứng dụng"),
        ("BR-SC-11", "Mô tả mơ hồ (AI trả câu hỏi lại) thì không tự giao thợ, chuyển quản lý hỏi lại cư dân.", "Ứng dụng"),
        ("BR-SC-12", "Tự giao cho thợ đúng chuyên môn đang có ít yêu cầu chưa xong nhất.", "Ứng dụng: MaintenanceDispatcher"),
        ("BR-SC-13", "Mọi chuyển trạng thái và giao việc đều ghi lịch sử: từ, sang, ai, khi nào, ghi chú.", "Bảng "
         "LichSuTrangThaiSuCo"),
        ("BR-SC-14", "Đánh giá từ 1 đến 5; chi phí ước tính, thực tế không âm; số yêu cầu không trùng.", "CHECK, UNIQUE"),
        ("BR-CH-01", "Số tầng không trùng trong một toà; mã căn không trùng trong một toà.", "UNIQUE (ToaNhaId, SoTang), "
         "UNIQUE (ToaNhaId, MaCanHo)"),
        ("BR-CH-02", "Diện tích căn > 0; số phòng ngủ ≥ 0.", "CHECK"),
        ("BR-CD-01", "Ngày chuyển đi không trước ngày vào ở; mỗi căn có một người đại diện nhận thông báo.", "CHECK + ứng "
         "dụng"),
        ("BR-CD-02", "Tài khoản cư dân chỉ gắn vào căn qua thư mời do BQL gửi, có hạn 14 ngày, không gắn theo email trùng.",
         "Ứng dụng: mã mời ký số"),
        ("BR-PQ-01", "Quản lý BQL có mọi quyền nghiệp vụ trừ quản lý người dùng và phân quyền; quản trị viên có toàn quyền.",
         "Bảng VaiTro_Quyen + kiểm tra vai trò"),
    ], [2.2, 9.0, 4.8])
    r.p(f"Thời hạn của từng mức ưu tiên dùng cho BR-SC-07 được cho ở {r.next_tab()}. Đây là **giả định** của đề tài, "
        "chưa xác minh tại đơn vị thực tế; quản trị viên chỉnh được trong bảng MucSla mà không cần sửa mã nguồn.")
    r.table("Mức ưu tiên và thời hạn SLA (giả định)", ["Mức ưu tiên", "Ví dụ tình huống", "Hạn tiếp nhận", "Hạn hoàn tất"], [
        ("Khẩn cấp", "Cháy, khói, rò gas, chập điện, nước tràn lan rộng, người mắc kẹt", "15 phút", "2 giờ"),
        ("Cao", "Mất điện, mất nước, toilet duy nhất bị tắc, không khoá được cửa", "30 phút", "8 giờ"),
        ("Trung bình", "Hỏng hóc gây bất tiện nhưng vẫn sinh hoạt được", "2 giờ", "24 giờ"),
        ("Thấp", "Vấn đề nhỏ, thẩm mỹ, không gấp", "12 giờ", "3 ngày"),
    ], [2.6, 8.4, 2.5, 2.5], align_cols=[2, 3])

    # ---------------- 1.6
    r.h2("1.6. Chỉ số đánh giá hiệu quả (KPI)")
    r.p(f"KPI được định nghĩa ngay từ giai đoạn phân tích để thiết kế dữ liệu ghi đủ các mốc thời gian cần thiết. "
        f"{r.next_tab()} nêu công thức và nguồn dữ liệu; công thức SQL tương ứng ở mục 2.8.")
    r.table("KPI của quy trình sự cố", ["KPI", "Công thức", "Dữ liệu dùng"], [
        ("Tỉ lệ hoàn tất đúng hạn SLA", "Số yêu cầu có NgayGiaiQuyet ≤ HanXuLy / số yêu cầu đã xử lý có hạn", "NgayGiaiQuyet, HanXuLy"),
        ("MTTR (giờ)", "Trung bình (NgayGiaiQuyet − NgayBaoCao) của các yêu cầu đã xử lý", "NgayBaoCao, NgayGiaiQuyet"),
        ("Thời gian phản hồi TB (phút)", "Trung bình (NgayTiepNhan − NgayBaoCao)", "NgayTiepNhan"),
        ("Tỉ lệ phản hồi đúng SLA", "Số yêu cầu tiếp nhận trong PhutPhanHoi của mức ưu tiên / số đã tiếp nhận", "MucSla.PhutPhanHoi"),
        ("Số yêu cầu đang quá hạn", "Đang mở (Mới, Tiếp nhận, Đang xử lý, Tạm dừng) và HanXuLy < hiện tại", "TrangThai, HanXuLy"),
        ("CSAT", "Trung bình DanhGia (1–5) của các yêu cầu được đánh giá", "DanhGia"),
        ("Tỉ lệ việc hệ thống tự giao được giữ", "Số yêu cầu hệ thống tự giao mà quản lý không giao lại / số yêu cầu hệ thống tự giao",
         "LichSuTrangThaiSuCo"),
        ("Phân rã", "Các chỉ số trên theo mức ưu tiên, loại sự cố, toà, kỹ thuật viên", "Các cột tương ứng"),
    ], [4.0, 7.6, 4.4])
    r.p("Mục tiêu định lượng dự kiến (giả định, sẽ đo trên kịch bản thử ở Chương 3): ≥ 90% yêu cầu hoàn tất đúng hạn, "
        "100% sự cố khẩn cấp được phản hồi trong 15 phút, CSAT ≥ 4,0. Riêng thành phần AI đã có số đo sơ bộ trên bộ 120 ca "
        "kiểm thử tự xây dựng (mô hình qwen3-suco4): đúng loại 93%, bắt được 21/22 sự cố nguy hiểm, đúng mức ưu tiên 72% "
        "(99% lệch tối đa một bậc), đúng nhóm xử lý 39/47 so với 30/47 của mô hình gốc chưa tinh chỉnh. Đây là kết quả đo "
        "trên dữ liệu thử, chưa phải kết quả vận hành thật.")

    # ---------------- 1.7
    r.h2("1.7. Đặc tả yêu cầu sơ bộ (SRS rút gọn)")
    r.h3("1.7.1. Yêu cầu chức năng")
    r.p("Phạm vi được quản lý bằng phương pháp MoSCoW: Must-have là chức năng bắt buộc để quy trình chạy trọn; Should-have "
        "là chức năng quan trọng làm sau phần lõi; Could-have là mở rộng; Won't-have là chưa làm trong kỳ đồ án.")
    r.table("Yêu cầu chức năng, mức ưu tiên và tiêu chí nghiệm thu", ["Mã", "Yêu cầu", "Tác nhân", "Ưu tiên", "Tiêu chí nghiệm thu"], [
        ("FR-01", "Đăng nhập, phân quyền theo vai trò", "Tất cả", "Must", "Cư dân không mở được trang quản trị; KTV chỉ vào "
         "được cổng kỹ thuật"),
        ("FR-02", "Quản lý Khu – Toà – Tầng – Căn", "Quản lý", "Must", "Không tạo được hai căn cùng mã trong một toà"),
        ("FR-03", "Quản lý cư dân theo căn, gửi thư mời tạo tài khoản", "Quản lý", "Must", "Thư mời quá 14 ngày hoặc sai mã "
         "thì không gắn được tài khoản"),
        ("FR-04", "Cư dân gửi yêu cầu sự cố (căn / khu vực chung, mô tả, ảnh)", "Cư dân", "Must", "Khu vực chung không ghi vị "
         "trí thì bị chặn; yêu cầu có số duy nhất"),
        ("FR-05", "AI phân loại loại, mức ưu tiên, nguy hiểm, câu hỏi lại, nhóm xử lý", "Hệ thống", "Should", "Ca “mùi gas” "
         "cư dân chọn mức Thấp được nâng lên Khẩn cấp"),
        ("FR-06", "Tự điều phối: giao thợ đúng chuyên môn, ít việc nhất; email theo vai trò", "Hệ thống", "Should", "Sự cố "
         "điện được giao cho thợ điện đang ít việc nhất"),
        ("FR-07", "Quản lý tiếp nhận, phân công, giao lại, đổi ưu tiên, từ chối", "Quản lý", "Must", "Từ chối không có lý "
         "do thì bị chặn"),
        ("FR-08", "KTV xem việc của mình, bắt đầu, tạm dừng, hoàn tất kèm kết quả và ảnh", "KTV", "Must", "KTV điện mở việc "
         "của thợ nước thì nhận lỗi không tìm thấy"),
        ("FR-09", "Báo cáo KPI sự cố theo kỳ, ưu tiên, loại, toà, KTV", "Quản lý", "Must", "Số liệu trùng với truy vấn SQL "
         "đối chiếu trên CSDL"),
        ("FR-10", "Cư dân theo dõi, huỷ, xác nhận và đánh giá, báo chưa khắc phục", "Cư dân", "Must", "Không huỷ được yêu "
         "cầu đang xử lý"),
        ("FR-11", "Hạn SLA và danh sách quá hạn", "Quản lý", "Should", "Tạm dừng 2 giờ thì hạn dời đúng 2 giờ"),
        ("FR-12", "Nhật ký hệ thống ghi mọi thay đổi dữ liệu", "Quản trị", "Should", "Sửa một yêu cầu sinh một dòng nhật ký "
         "có giá trị cũ, mới"),
        ("FR-13", "Phí dịch vụ, hoá đơn, thanh toán VietQR", "Quản lý, Cư dân", "Could", "Phân hệ hỗ trợ, ngoài phạm vi đánh giá"),
        ("—", "Cảm biến IoT, ứng dụng di động gốc, cổng thanh toán thật, chatbot", "—", "Won't", "Không làm trong kỳ"),
    ], [1.4, 4.6, 2.0, 1.5, 6.5], size=11.5, align_cols=[0, 3])
    r.h3("1.7.2. Yêu cầu phi chức năng")
    r.table("Yêu cầu phi chức năng", ["Mã", "Nhóm", "Yêu cầu kiểm tra được"], [
        ("NFR-01", "Bảo mật", "Phân quyền kiểm tra ở máy chủ: gọi thẳng URL hoặc API không đúng vai trò bị từ chối; form có "
         "chống giả mạo yêu cầu (CSRF)."),
        ("NFR-02", "Toàn vẹn", "Mọi ràng buộc đặt được ở CSDL (PK, FK, UNIQUE, CHECK) phải đặt ở CSDL, không chỉ ở giao diện."),
        ("NFR-03", "Sẵn sàng", "AI không chạy, quá thời gian chờ hoặc trả sai định dạng thì việc báo sự cố vẫn thành công."),
        ("NFR-04", "Hiệu năng", "AI phân loại trung bình dưới 5 giây khi mô hình đã nạp (máy GTX 1650); gửi yêu cầu kể cả "
         "email dưới 15 giây."),
        ("NFR-05", "Riêng tư", "Mô tả sự cố chỉ gửi tới mô hình chạy cục bộ; email demo dồn về một hộp thư thử."),
        ("NFR-06", "Truy vết", "Mọi thay đổi dữ liệu có nhật ký; mọi chuyển trạng thái có lịch sử; xoá là xoá mềm."),
        ("NFR-07", "Khả dụng", "Giao diện tiếng Việt, dùng được trên điện thoại; thông báo lỗi nêu rõ lý do."),
    ], [1.7, 2.3, 12.0])

    # ---------------- 1.8
    r.h2("1.8. Use case sơ bộ")
    r.h3("1.8.1. Sơ đồ use case")
    r.p(f"{r.next_fig()} là sơ đồ use case tổng quát. UC03 “Gửi yêu cầu sự cố” luôn bao gồm UC07 “Phân loại sự cố bằng "
        "AI” và UC08 “Điều phối và gửi email”.")
    r.figure(IMG / "Hinh 1.4 So do use case tong quat.png", "Sơ đồ use case tổng quát của hệ thống", 16)
    r.h3("1.8.2. Danh sách use case")
    r.table("Danh sách use case và yêu cầu chức năng liên quan", ["Mã", "Tên use case", "Tác nhân chính", "FR"], [
        ("UC01", "Đăng nhập", "Mọi người dùng", "FR-01"),
        ("UC02", "Kích hoạt tài khoản qua thư mời", "Cư dân", "FR-03"),
        ("UC03", "Gửi yêu cầu sự cố", "Cư dân", "FR-04"),
        ("UC04", "Theo dõi yêu cầu của mình", "Cư dân", "FR-10"),
        ("UC05", "Huỷ yêu cầu", "Cư dân", "FR-10"),
        ("UC06", "Xác nhận hoàn tất và đánh giá", "Cư dân", "FR-10"),
        ("UC07", "Phân loại sự cố bằng AI", "Mô hình AI", "FR-05"),
        ("UC08", "Điều phối và gửi email", "Dịch vụ email", "FR-06"),
        ("UC09", "Quản lý Khu – Toà – Tầng – Căn", "Quản lý BQL", "FR-02"),
        ("UC10", "Quản lý cư dân và thư mời", "Quản lý BQL", "FR-03"),
        ("UC11", "Tiếp nhận, phân công, từ chối sự cố", "Quản lý BQL", "FR-07, FR-11"),
        ("UC12", "Xem báo cáo KPI sự cố", "Quản lý BQL", "FR-09"),
        ("UC13", "Quản lý người dùng và phân quyền", "Quản trị viên", "FR-01"),
        ("UC14", "Xem nhật ký hệ thống", "Quản trị viên", "FR-12"),
        ("UC15", "Xem việc được giao", "Kỹ thuật viên", "FR-08"),
        ("UC16", "Cập nhật xử lý: bắt đầu, tạm dừng, hoàn tất", "Kỹ thuật viên", "FR-08"),
    ], [1.6, 7.0, 4.0, 3.4], align_cols=[0, 3])
    r.h3("1.8.3. Đặc tả use case trọng tâm")
    r.table("Đặc tả UC03 – Gửi yêu cầu sự cố", ["Mục", "Nội dung"], [
        ("Tác nhân", "Cư dân (chính); Mô hình AI, Dịch vụ email (phụ)"),
        ("Tiền điều kiện", "Cư dân đã đăng nhập, tài khoản gắn với một bản ghi cư dân đang ở (BR-SC-01)."),
        ("Hậu điều kiện", "Có một yêu cầu trạng thái Mới gửi hoặc Đã tiếp nhận, có hạn SLA, có dòng lịch sử “Tạo yêu cầu”."),
        ("Luồng chính", "1. Cư dân chọn Báo sự cố → Tạo yêu cầu.\n2. Nhập tiêu đề, mô tả; chọn trong căn hoặc khu vực chung; "
         "đính kèm ảnh; có thể chọn loại và mức ưu tiên.\n3. Hệ thống gửi mô tả cho mô hình AI, nhận JSON phân loại.\n4. Hệ "
         "thống gộp lựa chọn cư dân với đề xuất AI (BR-SC-09), cấp số yêu cầu, tính hạn SLA, lưu yêu cầu, ảnh, lịch sử.\n5. "
         "Hệ thống chọn thợ đúng chuyên môn ít việc nhất, chuyển sang Đã tiếp nhận, gửi email giao việc (BR-SC-12).\n6. "
         "Hiển thị chi tiết yêu cầu cho cư dân."),
        ("Luồng thay thế", "3a. AI không phản hồi trong thời gian chờ: giữ lựa chọn của cư dân, gửi cảnh báo cho quản trị "
         "viên (BR-SC-10).\n5a. Mô tả mơ hồ: không giao thợ, cư dân thấy câu hỏi lại, quản lý nhận thư “Cần hỏi lại cư dân”.\n"
         "5b. Không có thợ chuyên trách: quản lý nhận thư “Cần phân công” kèm nhóm xử lý gợi ý.\n5c. Sự cố Khẩn cấp: quản lý "
         "nhận thêm thư “Cần theo dõi”."),
        ("Ngoại lệ", "E1. Tài khoản chưa gắn căn: báo lỗi, không cho tạo.\nE2. Chọn khu vực chung nhưng không ghi vị trí: "
         "báo lỗi tại trường Vị trí.\nE3. Trùng số yêu cầu do hai người gửi cùng lúc: cấp lại số và lưu lại.\nE4. Gửi email "
         "lỗi: chỉ ghi log, yêu cầu đã lưu không bị ảnh hưởng."),
    ], [3.2, 12.8])
    r.table("Đặc tả UC16 – Cập nhật xử lý của kỹ thuật viên", ["Mục", "Nội dung"], [
        ("Tác nhân", "Kỹ thuật viên"),
        ("Tiền điều kiện", "Yêu cầu được giao cho chính KTV đang đăng nhập (BR-SC-04)."),
        ("Hậu điều kiện", "Trạng thái mới được lưu, có dòng lịch sử; nếu hoàn tất thì có kết quả xử lý và ảnh nghiệm thu."),
        ("Luồng chính", "1. KTV mở danh sách Việc của tôi, chọn một yêu cầu.\n2. Bấm Bắt đầu xử lý (Đã tiếp nhận → Đang xử "
         "lý).\n3. Sau khi sửa xong, nhập nguyên nhân, cách xử lý, tải ảnh nghiệm thu, bấm Hoàn tất (Đang xử lý → Đã xử "
         "lý).\n4. Hệ thống ghi mốc giải quyết, lưu lịch sử; cư dân thấy nút xác nhận."),
        ("Luồng thay thế", "2a. Chờ vật tư: bấm Tạm dừng và ghi lý do; khi tiếp tục, hạn SLA được dời thêm đúng thời gian đã "
         "dừng (BR-SC-07)."),
        ("Ngoại lệ", "E1. Mở yêu cầu không giao cho mình: trả về không tìm thấy.\nE2. Hoàn tất khi chưa ghi kết quả: bị chặn "
         "(BR-SC-06).\nE3. Hai người cùng sửa một yêu cầu: cột phiên bản (PhienBan) phát hiện xung đột, yêu cầu tải lại."),
    ], [3.2, 12.8])

    # ---------------- 1.9
    r.h2("1.9. Kế hoạch thực hiện và phân công")
    r.p("Nhóm gồm hai sinh viên. Bảng dưới đây là phân công theo năng lực của từng thành viên; mọi sản phẩm đều được "
        "thành viên còn lại rà soát, và theo yêu cầu nghiệm thu tuần 10, cả hai đều phải giải thích được nghiệp vụ, "
        "ERD/3NF và luồng AI.")
    r.table("Phân công công việc trong nhóm", ["Hạng mục", "Phụ trách chính", "Phối hợp, rà soát"], [
        ("Khảo sát BQL, nguồn xác minh, AS-IS/TO-BE, quy tắc nghiệp vụ, KPI", "Lý Như Anh", "Dương Gia Huy"),
        ("SRS (FR/NFR/BR, tiêu chí nghiệm thu), use case và đặc tả", "Lý Như Anh", "Dương Gia Huy"),
        ("Phân tích dữ liệu: thực thể, ERD, lược đồ quan hệ, 3NF, ràng buộc", "Dương Gia Huy", "Lý Như Anh"),
        ("CSDL PostgreSQL: script, trigger, stored procedure, dữ liệu thử", "Dương Gia Huy", "Lý Như Anh"),
        ("Ứng dụng ASP.NET MVC: phân quyền, quy trình sự cố, SLA, báo cáo", "Dương Gia Huy", "Lý Như Anh"),
        ("AI Agent: dữ liệu huấn luyện, bộ test có nhãn, đánh giá", "Dương Gia Huy", "Lý Như Anh"),
        ("Giao diện, kiểm thử chức năng và phân quyền, đối chiếu báo cáo với CSDL", "Lý Như Anh", "Dương Gia Huy"),
        ("Sơ đồ UML (StarUML), báo cáo, video demo, hướng dẫn cài đặt", "Lý Như Anh", "Dương Gia Huy"),
    ], [8.6, 3.7, 3.7])
    r.p("Kế hoạch thực hiện bám theo phiếu theo dõi tiến độ đã đăng ký với giảng viên hướng dẫn:")
    r.table("Kế hoạch thực hiện theo tuần", ["Tuần", "Thời gian", "Nội dung và sản phẩm", "Trạng thái"], [
        ("1", "12–18/10", "Khảo sát và chốt phạm vi: lý do chọn đề tài, bối cảnh BQL; trả lời 6 câu hỏi Bước 1; tác nhân, "
         "nguồn dữ liệu, mục tiêu, phạm vi (MoSCoW), phân công", "Hoàn thành"),
        ("2", "19–25/10", "Chương 1: khảo sát BQL và nguồn xác minh; AS-IS/TO-BE, tác nhân, quy tắc nghiệp vụ (SLA ghi rõ "
         "là giả định), KPI; SRS sơ bộ (FR/NFR/BR, nghiệm thu), use case sơ bộ; duyệt bài toán và yêu cầu", "Hoàn thành"),
        ("3", "26/10–01/11", "Phân tích dữ liệu: thực thể từ nghiệp vụ và lý do cần từng bảng; ERD, bản số, bảng trung "
         "gian, PK/FK; lược đồ quan hệ, phụ thuộc hàm, 3NF; danh mục ràng buộc (PK, FK, UNIQUE, CHECK; giao dịch, "
         "trigger, stored procedure); từ điển dữ liệu; minh hoạ một nghiệp vụ qua dữ liệu; truy vấn KPI", "Hoàn thành"),
        ("4", "02–08/11", "UML bằng StarUML: use case chi tiết và đặc tả, activity, class, sequence (AI phân loại, điều "
         "phối, hoàn tất); kiến trúc ASP.NET MVC – PostgreSQL – dịch vụ AI; kiểm tra nhất quán tự động mô hình – mã "
         "nguồn – CSDL; hoàn thiện CSDL (ràng buộc còn thiếu, trigger, thủ tục, dữ liệu thử); duyệt mô hình dữ liệu và "
         "hệ thống", "UML, kiến trúc xong; CSDL đang làm"),
        ("5", "09–15/11", "Demo luồng đầu tiên: đăng nhập, phân quyền bốn vai trò; cư dân gửi phản ánh, quản lý giao việc, "
         "đổi trạng thái có lưu lịch sử, ghi thật vào CSDL thử", "Kế hoạch"),
        ("6", "16–22/11", "Quy trình xuyên suốt tiếp nhận → giao việc → xử lý → nghiệm thu → đánh giá; theo dõi SLA, cảnh "
         "báo quá hạn; ngoại lệ (phân công lại, tạm dừng, từ chối, mở lại, huỷ); giao diện gọi dịch vụ AI có dự phòng "
         "thủ công", "Kế hoạch"),
        ("7", "23–29/11", "Báo cáo theo loại, khu vực, thời gian, thời gian xử lý, tỷ lệ quá hạn, đối chiếu SQL; thử AI "
         "(phân loại, chỉ nâng ưu tiên, phát hiện nguy hiểm, hỏi lại khi mô tả mơ hồ, tự giao khi mô tả rõ và có thợ "
         "chuyên trách) trên bộ 120 ca có nhãn và 200 ca dò; nghiệm thu MVP", "Kế hoạch"),
        ("8", "30/11–06/12", "Kiểm thử chức năng, ngoại lệ, ràng buộc, giao dịch, phân quyền; đánh giá AI (độ chính xác "
         "loại và mức ưu tiên, tỉ lệ việc hệ thống tự giao được giữ nguyên, ca sai và cách xử lý); sửa lỗi", "Kế hoạch"),
        ("9", "07–13/12", "Đánh giá khả năng sử dụng; tách kết quả đã đo với lợi ích dự kiến; hoàn thiện báo cáo, dữ liệu "
         "demo, video, hướng dẫn cài đặt", "Kế hoạch"),
        ("10", "14–20/12", "Nghiệm thu thử với GVHD, sửa lỗi cuối, khoá phiên bản; mỗi thành viên giải thích ERD/3NF, "
         "nghiệp vụ, luồng AI", "Kế hoạch"),
    ], [1.3, 2.4, 9.8, 2.5], size=11.5, align_cols=[0])
    r.p("Công cụ và môi trường: ASP.NET Core MVC trên .NET 10 [7], Entity Framework Core với PostgreSQL [11], ASP.NET Core "
        "Identity cho đăng nhập, Ollama chạy mô hình Qwen3-4B tinh chỉnh (huấn luyện bằng Unsloth trên Kaggle), Visual "
        "Studio Code, Git/GitHub, Microsoft Word. Toàn bộ sơ đồ UML, ERD và lược đồ quan hệ trong báo cáo được vẽ bằng "
        "StarUML; tệp gốc là staruml/BaoCao_DACN.mdj đi kèm báo cáo.")

    # ---------------- 1.10
    r.h2("1.10. Cấu trúc đồ án")
    r.p("Ngoài phần mở đầu, đồ án gồm năm chương:")
    r.bullets([
        "**Chương 1 – Tổng quan và phân tích nghiệp vụ:** bối cảnh, vấn đề, AS-IS/TO-BE, quy tắc nghiệp vụ, KPI, SRS và use case sơ bộ.",
        "**Chương 2 – Phân tích và thiết kế dữ liệu:** thực thể, ERD, lược đồ quan hệ, chuẩn hoá 3NF, ràng buộc toàn vẹn, "
        "từ điển dữ liệu, minh hoạ một nghiệp vụ qua dữ liệu và truy vấn KPI.",
        "**Chương 3 – Thiết kế hệ thống** (tuần 4): kiến trúc, use case chi tiết và đặc tả, sơ đồ hoạt động, lớp, tuần tự "
        "và kiểm tra nhất quán giữa mô hình, mã nguồn, CSDL.",
        "**Chương 4 – Xây dựng, kiểm thử và đánh giá** (tuần 5–9): cài đặt, tích hợp AI, kiểm thử theo quy trình, đánh giá AI "
        "và khả năng sử dụng.",
        "**Chương 5 – Kết luận và kiến nghị** (tuần 9–10).",
    ])


# =====================================================================
# Chương 2
# =====================================================================
def chapter2(r, doc):
    r.h1("CHƯƠNG 2. PHÂN TÍCH VÀ THIẾT KẾ DỮ LIỆU", chapter=2)
    r.p("Chương này là sản phẩm tuần 3. Mỗi bảng được giải thích từ nghiệp vụ sinh ra nó, lý do chọn khoá, bản số của "
        "các mối kết hợp, các phụ thuộc hàm, quá trình chuẩn hoá đến 3NF và cách mỗi quy tắc nghiệp vụ được đảm bảo. "
        "Thiết kế được đối chiếu với CSDL PostgreSQL thật của hệ thống (tạo bằng migration của Entity Framework Core) để "
        "tài liệu và cài đặt không lệch nhau.")

    # ---------------- 2.1
    r.h2("2.1. Xác định thực thể từ nghiệp vụ")
    r.h3("2.1.1. Danh sách thực thể")
    r.p(f"Đi lần lượt qua từng bước của quy trình TO-BE và từng chứng từ ở Bảng 1.4, đề tài xác định 16 bảng trong phạm "
        f"vi đồ án ({r.next_tab()}). Tên bảng trong CSDL dùng tiếng Việt không dấu; tên thực thể trên ERD viết hoa.")
    r.table("Thực thể, nghiệp vụ phát sinh và lý do cần bảng", ["Thực thể (bảng)", "Phát sinh từ nghiệp vụ", "Vì sao cần bảng riêng", "Khoá chính"], [
        ("KHU_VUC (KhuVuc)", "Danh mục địa bàn (tỉnh/thành)", "Một địa bàn có nhiều khu, nhiều toà", "Id"),
        ("KHU (DuAn)", "Khu căn hộ gồm nhiều toà", "Thông tin khu (tên, địa chỉ) lặp lại cho mỗi toà nếu gộp", "Id"),
        ("TOA_NHA (ToaNha)", "Toà nhà, quản lý toà, chính sách thu phí", "Nơi xảy ra sự cố khu vực chung; có người quản lý", "Id"),
        ("TANG (Tang)", "Cơ cấu Toà → Tầng → Căn", "Tầng có nhãn riêng; căn thuộc tầng", "Id"),
        ("CAN_HO (CanHo)", "Căn hộ – nơi xảy ra sự cố trong căn", "Diện tích, tình trạng sử dụng, mã căn", "Id"),
        ("CU_DAN (CuDan)", "Người đang ở căn: chủ hộ, người thuê, thành viên", "Một căn nhiều người, một người có thể ở "
         "nhiều căn theo thời gian; có người chưa có tài khoản", "Id"),
        ("NGUOI_DUNG (NguoiDung)", "Tài khoản đăng nhập", "Cư dân, quản lý, KTV, quản trị đều đăng nhập", "Id"),
        ("VAI_TRO (VaiTro)", "Nhóm người dùng, chuyên môn thợ", "Phân quyền và chọn thợ theo chuyên môn", "Id"),
        ("NguoiDung_VaiTro", "Một người có thể nhiều vai trò", "Bảng trung gian của quan hệ n – n", "(NguoiDungId, VaiTroId)"),
        ("QUYEN (Quyen)", "Danh mục quyền theo phân hệ", "Phân quyền chi tiết hơn vai trò", "Id"),
        ("VaiTro_Quyen", "Vai trò được cấp nhiều quyền", "Bảng trung gian của quan hệ n – n", "(VaiTroId, QuyenId)"),
        ("YEU_CAU_SU_CO (YeuCauBaoTri)", "Phiếu yêu cầu sửa chữa / sổ tiếp nhận", "Thực thể trung tâm của quy trình", "Id; "
         "khoá thay thế SoYeuCau"),
        ("MUC_SLA (MucSla)", "Thời hạn phản hồi, hoàn tất theo mức ưu tiên", "Thời hạn đổi được mà không sửa mã nguồn", "MucDoUuTien"),
        ("ANH_SU_CO (AnhSuCo)", "Ảnh hiện trạng, ảnh nghiệm thu", "Một yêu cầu nhiều ảnh (tránh vi phạm 1NF)", "Id"),
        ("LICH_SU_TRANG_THAI (LichSuTrangThaiSuCo)", "Mỗi lần đổi trạng thái, giao việc", "Truy vết, đo KPI, phát hiện "
         "giao lại", "Id"),
        ("NHAT_KY_HE_THONG (NhatKyHeThong)", "Ghi vết mọi thay đổi dữ liệu", "Kiểm toán, không phụ thuộc bảng nghiệp vụ", "Id"),
    ], [4.3, 4.3, 5.0, 2.4], size=11.5)
    r.p("Các bảng phân hệ phí dịch vụ (HoaDon, ChiTietHoaDon, ThanhToan, LoaiDichVu, ChiSoDichVu, PhiDinhKy) và các bảng "
        "kỹ thuật của ASP.NET Identity (NguoiDung_Claim, NguoiDung_DangNhap, NguoiDung_Token, VaiTro_Claim) có trong CSDL "
        "nhưng nằm ngoài phạm vi phân tích của đồ án.")
    r.h3("2.1.2. Nguyên tắc chọn khoá")
    r.bullets([
        "**Khoá thay thế (surrogate key)** kiểu số nguyên tự tăng (`integer` identity) cho các thực thể nghiệp vụ: khoá ngắn, "
        "không đổi khi dữ liệu nghiệp vụ thay đổi (ví dụ đổi mã căn khi BQL đánh số lại).",
        "**Khoá tự nhiên được giữ thành ràng buộc UNIQUE**: SoYeuCau (số phiếu cư dân nhìn thấy), (ToaNhaId, MaCanHo), "
        "(ToaNhaId, SoTang), Slug, Ma của quyền. Nhờ vậy không mất ràng buộc nghiệp vụ khi dùng khoá thay thế.",
        "**NguoiDung.Id** là chuỗi GUID do ASP.NET Identity sinh; đề tài giữ nguyên để dùng cơ chế đăng nhập chuẩn.",
        "**MucSla dùng khoá tự nhiên MucDoUuTien** (0 Thấp, 1 Trung bình, 2 Cao, 3 Khẩn cấp): miền giá trị nhỏ, cố định, "
        "và chính là giá trị cột MucDoUuTien của yêu cầu nên khoá ngoại không cần thêm cột.",
        "**Bảng trung gian n – n dùng khoá chính ghép** từ hai khoá ngoại nên một cặp không bị lặp.",
    ])

    # ---------------- 2.2
    r.h2("2.2. Mô hình thực thể kết hợp (ERD)")
    r.h3("2.2.1. Ký hiệu sử dụng")
    r.p("ERD được vẽ bằng StarUML theo ký pháp chân gà (Crow's Foot – Information Engineering). Ký hiệu ở đầu đường "
        "nối cạnh một thực thể cho biết có bao nhiêu thể hiện của thực thể đó ứng với một thể hiện ở đầu bên kia; cặp "
        "(min,max) theo ký pháp Chen tương ứng được liệt kê ở Bảng 2.3.")
    r.table("Ký hiệu trên ERD (ký pháp chân gà)", ["Ký hiệu ở đầu đường nối", "Ý nghĩa", "Tương ứng (min,max)"], [
        ("Hai vạch đứng", "Đúng một", "(1,1)"),
        ("Vòng tròn và một vạch", "Không hoặc một", "(0,1)"),
        ("Vạch và chân gà", "Một hoặc nhiều", "(1,n)"),
        ("Vòng tròn và chân gà", "Không hoặc nhiều", "(0,n)"),
        ("Khung thực thể", "Tên thực thể, cột PK là khoá chính, các thuộc tính mô tả", "—"),
        ("Nhãn trên đường nối", "Tên mối kết hợp, đọc từ thực thể A sang thực thể B", "—"),
    ], [4.6, 7.6, 3.8], align_cols=[2])
    r.h3("2.2.2. Sơ đồ ERD")
    r.p(f"ERD ở mức khái niệm ({r.next_fig()}) được xoay 90° trong trang để đủ cỡ chữ khi in. Ở mức khái niệm, "
        "thực thể chỉ có khoá chính và thuộc tính mô tả, chưa có khoá ngoại; ANH_SU_CO và LICH_SU_TRANG_THAI là thực thể "
        "yếu, chỉ tồn tại cùng YEU_CAU_SU_CO. Danh sách thuộc tính đầy đủ ở từ điển dữ liệu mục 2.6.")
    r.figure(IMG / "Hinh 2.1 ERD.png", "Mô hình thực thể kết hợp (ERD) của hệ thống, vẽ bằng StarUML", 16, rotate=True)
    r.h3("2.2.3. Mối kết hợp và bản số")
    r.table("Các mối kết hợp và cặp bản số (min, max)", ["Thực thể A", "Mối kết hợp", "Thực thể B", "A (min,max)", "B (min,max)", "Diễn giải"], [
        ("KHU_VUC", "có", "KHU", "(0,n)", "(1,1)", "Mỗi khu thuộc đúng một khu vực"),
        ("KHU_VUC", "chứa", "TOA_NHA", "(0,n)", "(1,1)", "Toà luôn có khu vực, kể cả toà độc lập"),
        ("KHU", "gồm", "TOA_NHA", "(0,n)", "(0,1)", "Toà có thể không thuộc khu nào"),
        ("TOA_NHA", "có", "TANG", "(1,n)", "(1,1)", "Tầng thuộc đúng một toà"),
        ("TANG", "có", "CAN_HO", "(1,n)", "(1,1)", "Căn nằm trên đúng một tầng"),
        ("CAN_HO", "có", "CU_DAN", "(0,n)", "(1,1)", "Căn trống thì chưa có cư dân"),
        ("NGUOI_DUNG", "là tài khoản của", "CU_DAN", "(0,n)", "(0,1)", "Cư dân có thể chưa có tài khoản"),
        ("NGUOI_DUNG", "quản lý", "TOA_NHA", "(0,n)", "(0,1)", "Một quản lý phụ trách nhiều toà"),
        ("NGUOI_DUNG", "đóng vai", "VAI_TRO", "(1,n)", "(0,n)", "Quan hệ n – n qua NguoiDung_VaiTro"),
        ("VAI_TRO", "được cấp", "QUYEN", "(0,n)", "(0,n)", "Quan hệ n – n qua VaiTro_Quyen"),
        ("CAN_HO", "phát sinh tại", "YEU_CAU_SU_CO", "(0,n)", "(0,1)", "Sự cố trong căn"),
        ("TOA_NHA", "khu vực chung", "YEU_CAU_SU_CO", "(0,n)", "(0,1)", "Sự cố ở khu vực chung; loại trừ với dòng trên"),
        ("NGUOI_DUNG", "báo", "YEU_CAU_SU_CO", "(0,n)", "(1,1)", "Mỗi yêu cầu có đúng một người báo"),
        ("NGUOI_DUNG", "phụ trách", "YEU_CAU_SU_CO", "(0,n)", "(0,1)", "Yêu cầu mới gửi có thể chưa có người phụ trách"),
        ("MUC_SLA", "áp dụng", "YEU_CAU_SU_CO", "(0,n)", "(1,1)", "Mỗi yêu cầu có đúng một mức ưu tiên"),
        ("YEU_CAU_SU_CO", "có ảnh", "ANH_SU_CO", "(0,n)", "(1,1)", "Ảnh thuộc đúng một yêu cầu"),
        ("YEU_CAU_SU_CO", "ghi nhận", "LICH_SU_TRANG_THAI", "(1,n)", "(1,1)", "Ít nhất một dòng “Tạo yêu cầu”"),
        ("NGUOI_DUNG", "thực hiện", "LICH_SU_TRANG_THAI", "(0,n)", "(0,1)", "Trống nghĩa là hệ thống tự thực hiện"),
    ], [2.7, 2.3, 3.1, 1.6, 1.6, 4.7], size=11, align_cols=[3, 4])
    r.p("Ba bản số không khai báo được bằng khoá ngoại và được đảm bảo ở tầng ứng dụng: (1) TOA_NHA (1,n) TANG và TANG "
        "(1,n) CAN_HO – khi tạo toà, hệ thống tạo đủ tầng và căn theo số tầng khai báo; (2) YEU_CAU_SU_CO (1,n) "
        "LICH_SU_TRANG_THAI – dòng “Tạo yêu cầu” được thêm trong cùng lần lưu với yêu cầu; (3) mối kết hợp “báo” là (1,1) "
        "về nghiệp vụ nhưng cột NguoiBaoCaoId cho phép NULL để không mất yêu cầu khi tài khoản người báo bị xoá; hệ thống "
        "luôn ghi người báo khi tạo (cư dân, hoặc quản lý khi tạo thay cư dân gọi điện).")

    # ---------------- 2.3
    r.h2("2.3. Chuyển ERD sang lược đồ quan hệ (RD)")
    r.h3("2.3.1. Quy tắc chuyển đổi")
    r.bullets([
        "Mỗi thực thể thành một quan hệ; thuộc tính định danh thành khoá chính.",
        "Mối kết hợp 1 – n: đưa khoá chính phía 1 sang phía n làm khoá ngoại; NOT NULL nếu bản số tối thiểu phía n là 1 "
        "(ví dụ Tang.ToaNhaId), cho phép NULL nếu là 0 (ví dụ ToaNha.DuAnId, YeuCauBaoTri.NguoiPhuTrachId).",
        "Mối kết hợp n – n: tạo bảng trung gian có khoá chính ghép từ hai khoá ngoại (NguoiDung_VaiTro, VaiTro_Quyen).",
        "Hai mối kết hợp loại trừ nhau (sự cố trong căn / ở khu vực chung): hai khoá ngoại cùng cho phép NULL kèm ràng buộc "
        "CHECK bắt buộc đúng một khoá có giá trị.",
        "Thực thể yếu: khoá ngoại tới thực thể chủ với ON DELETE CASCADE (AnhSuCo, LichSuTrangThaiSuCo).",
        "Thuộc tính đa trị (danh sách ảnh) không đặt thành một cột mà tách thành bảng riêng.",
    ])
    r.h3("2.3.2. Các lược đồ quan hệ")
    r.p("Quy ước: thuộc tính gạch dưới là khoá chính, thuộc tính *in nghiêng* là khoá ngoại. Mọi bảng nghiệp vụ còn có "
        "tám cột kiểm toán kế thừa từ lớp BaseEntity (NgayTao, NguoiTao, NgayCapNhat, NguoiCapNhat, DaXoa, NgayXoa, "
        "NguoiXoa, PhienBan), không lặp lại dưới đây.")
    schemas = [
        "KhuVuc (**Id**, Ten, Slug, UrlAnh)",
        "DuAn (**Id**, *KhuVucId*, Ten, Slug, DiaChi, UrlAnhDaiDien, MoTaNgan)",
        "ToaNha (**Id**, *KhuVucId*, *DuAnId*, *QuanLyId*, Ma, Ten, Slug, DiaChi, SoLuongTang, UrlAnhDaiDien, MoTa, "
        "TrangThai, NgayChotKyMacDinh, SoNgayTreHanMacDinh, PhanTramPhiTreMacDinh, DonGiaPhiQuanLy)",
        "Tang (**Id**, *ToaNhaId*, SoTang, NhanTang)",
        "CanHo (**Id**, *ToaNhaId*, *TangId*, MaCanHo, DienTich, SoPhongNgu, SoPhongTam, HuongNha, TinhTrangThue, MoTa)",
        "CuDan (**Id**, *CanHoId*, *NguoiDungId*, HoTen, SoDienThoai, Email, SoGiayTo, VaiTro, DaiDienCanHo, NgayVaoO, "
        "NgayChuyenDi, GhiChu)",
        "NguoiDung (**Id**, UserName, Email, PasswordHash, HoTen, SoDienThoai, UrlAvatar, NgayDangNhapCuoi, …)",
        "VaiTro (**Id**, Name)",
        "NguoiDung_VaiTro (***NguoiDungId***, ***VaiTroId***)",
        "Quyen (**Id**, Ma, Module, TenHienThi, MoTa)",
        "VaiTro_Quyen (***VaiTroId***, ***QuyenId***)",
        "MucSla (**MucDoUuTien**, PhutPhanHoi, PhutHoanTat)",
        "YeuCauBaoTri (**Id**, SoYeuCau, *CanHoId*, *ToaNhaKhuVucChungId*, ViTri, *NguoiBaoCaoId*, *NguoiPhuTrachId*, "
        "TieuDe, MoTa, LoaiBaoTri, *MucDoUuTien*, TrangThai, GhiChuXuLy, AiTriageJson, ChiPhiUocTinh, ChiPhiThucTe, "
        "TinhPhiKhachThue, NgayBaoCao, NgayTiepNhan, NgayGiaiQuyet, NgayDongLai, HanXuLy, DanhGia, NhanXet)",
        "AnhSuCo (**Id**, *YeuCauBaoTriId*, DuongDan, GiaiDoan, *NguoiTaiLenId*, NgayTaiLen)",
        "LichSuTrangThaiSuCo (**Id**, *YeuCauBaoTriId*, TuTrangThai, SangTrangThai, *NguoiThucHienId*, ThoiDiem, GhiChu)",
        "NhatKyHeThong (**Id**, TenBang, KhoaBang, HanhDong, NguoiDungId, TenNguoiDung, ThoiGian, GiaTriCu, GiaTriMoi, "
        "CotThayDoi, DiaChiIp, UserAgent)",
    ]
    for s in schemas:
        # **x** trong danh sách trên là khoá chính (gạch dưới), ***x*** là vừa khoá chính vừa khoá ngoại.
        s = s.replace("***", "~~").replace("**", "__")
        r.p(s, indent=False).paragraph_format.left_indent = Cm(0.6)
    r.h3("2.3.3. Sơ đồ lược đồ quan hệ")
    r.p(f"{r.next_fig()} là lược đồ quan hệ 16 bảng vẽ bằng StarUML. Cột thứ nhất của mỗi bảng ghi PK (khoá chính), FK "
        "(khoá ngoại), U (duy nhất), N (cho phép NULL); kiểu dữ liệu và tám cột kiểm toán ở từ điển dữ liệu mục 2.6. Chân "
        "gà nằm ở phía bảng chứa khoá ngoại.")
    r.figure(IMG / "Hinh 2.2 Luoc do quan he.png", "Lược đồ quan hệ (RD) trong phạm vi đồ án, vẽ bằng StarUML", 16,
             rotate=True)
    r.h3("2.3.4. Lý do tạo các bảng trung gian và bảng tách riêng")
    r.bullets([
        "**NguoiDung_VaiTro:** một người có thể vừa là quản lý vừa là quản trị; nếu đặt cột VaiTro trong NguoiDung thì "
        "phải lặp dòng người dùng hoặc lưu danh sách trong một cột (vi phạm 1NF).",
        "**VaiTro_Quyen:** cấp, thu hồi một quyền cho một vai trò chỉ là thêm, xoá một dòng; không phải sửa mã nguồn.",
        "**CuDan:** không phải bảng trung gian thuần giữa CanHo và NguoiDung vì có thuộc tính riêng (vai trò trong hộ, ngày "
        "vào ở, ngày chuyển đi) và NguoiDungId được để trống cho người chưa có tài khoản. Nhờ vậy BQL ghi nhận đủ người ở "
        "trước, cư dân tạo tài khoản sau qua thư mời.",
        "**LichSuTrangThaiSuCo:** thay vì chỉ giữ trạng thái hiện tại, mỗi lần đổi trạng thái là một dòng; đây là nguồn "
        "dữ liệu cho KPI (giao lại, việc hệ thống tự giao) và cho tính hạn khi tạm dừng.",
        "**AnhSuCo:** một yêu cầu có nhiều ảnh, mỗi ảnh có giai đoạn (lúc báo / nghiệm thu) và người tải lên.",
        "**MucSla:** thời hạn là dữ liệu cấu hình của BQL, cần sửa được mà không triển khai lại ứng dụng.",
    ])

    # ---------------- 2.4
    r.h2("2.4. Phụ thuộc hàm và chuẩn hoá 3NF")
    r.h3("2.4.1. Cơ sở")
    r.p("Một lược đồ đạt 1NF khi mọi thuộc tính là nguyên tố; đạt 2NF khi đạt 1NF và mọi thuộc tính không khoá phụ thuộc "
        "đầy đủ vào khoá; đạt 3NF khi đạt 2NF và không có thuộc tính không khoá nào phụ thuộc bắc cầu vào khoá, tức với "
        "mọi phụ thuộc hàm không tầm thường X → A thì X là siêu khoá hoặc A là thuộc tính khoá [4]. Các bảng có khoá chính "
        "một cột tự động đạt 2NF; vì vậy việc kiểm tra tập trung vào 1NF và phụ thuộc bắc cầu.")
    r.h3("2.4.2. Phụ thuộc hàm và kết luận dạng chuẩn")
    r.table("Phụ thuộc hàm chính và kết luận dạng chuẩn", ["Quan hệ", "Khoá ứng viên", "Phụ thuộc hàm không tầm thường", "Dạng chuẩn"], [
        ("KhuVuc", "Id; Slug", "Id → Ten, Slug, UrlAnh; Slug → Id", "3NF"),
        ("DuAn", "Id; Slug", "Id → KhuVucId, Ten, Slug, DiaChi, …", "3NF"),
        ("ToaNha", "Id; Slug", "Id → KhuVucId, DuAnId, QuanLyId, Ma, Ten, …", "3NF (xem 2.4.3 b)"),
        ("Tang", "Id; (ToaNhaId, SoTang)", "Id → ToaNhaId, SoTang, NhanTang; (ToaNhaId, SoTang) → Id", "3NF"),
        ("CanHo", "Id; (ToaNhaId, MaCanHo)", "Id → TangId, MaCanHo, DienTich, …; TangId → ToaNhaId",
         "3NF, chưa đạt BCNF (xem 2.4.3 a)"),
        ("CuDan", "Id", "Id → CanHoId, NguoiDungId, HoTen, VaiTro, NgayVaoO, …", "3NF"),
        ("NguoiDung_VaiTro", "(NguoiDungId, VaiTroId)", "Không có thuộc tính không khoá", "3NF"),
        ("VaiTro_Quyen", "(VaiTroId, QuyenId)", "Không có thuộc tính không khoá", "3NF"),
        ("Quyen", "Id; Ma", "Id → Ma, Module, TenHienThi, MoTa; Ma → Id", "3NF"),
        ("MucSla", "MucDoUuTien", "MucDoUuTien → PhutPhanHoi, PhutHoanTat", "3NF"),
        ("YeuCauBaoTri", "Id; SoYeuCau", "Id → mọi thuộc tính; SoYeuCau → Id", "3NF (xem 2.4.3 c, d)"),
        ("AnhSuCo", "Id", "Id → YeuCauBaoTriId, DuongDan, GiaiDoan, …", "3NF"),
        ("LichSuTrangThaiSuCo", "Id", "Id → YeuCauBaoTriId, TuTrangThai, SangTrangThai, …", "3NF"),
        ("NhatKyHeThong", "Id", "Id → TenBang, KhoaBang, HanhDong, ThoiGian, …", "3NF"),
    ], [3.4, 3.4, 6.4, 2.8], size=11)
    r.h3("2.4.3. Các quyết định chuẩn hoá")
    r.p("**a) CanHo – dư thừa ToaNhaId qua tầng (phát hiện khi chuẩn hoá).** Mỗi tầng thuộc đúng một toà nên TangId → "
        "ToaNhaId; cùng với Id → TangId ta có Id → TangId → ToaNhaId. Tuy vậy ToaNhaId là thành phần của khoá ứng viên "
        "(ToaNhaId, MaCanHo), tức là thuộc tính khoá, nên theo định nghĩa ở mục 2.4.1 CanHo vẫn đạt 3NF; bảng chỉ chưa đạt "
        "BCNF vì TangId không phải siêu khoá. Cột "
        "ToaNhaId được giữ lại vì ràng buộc nghiệp vụ “mã căn không trùng trong một toà” (UNIQUE (ToaNhaId, MaCanHo)) cần "
        "cột này, và các truy vấn theo toà dùng rất nhiều. Đây là dư thừa có chủ đích; để không căn nào ghi ToaNhaId "
        "khác với toà của tầng, ở tuần 4 nhóm đã thêm ràng buộc UNIQUE (Id, ToaNhaId) ở Tang và đổi khoá ngoại của CanHo "
        "thành khoá ngoại ghép (TangId, ToaNhaId) → Tang(Id, ToaNhaId) (migration 20261106042803_Tuan4RangBuocTriggerHam). Từ đó CSDL tự bảo đảm hai cột "
        "luôn khớp, sự dư thừa không còn gây dị thường cập nhật.")
    r.code('''-- Đã cài ở tuần 4 (migration 20261106042803_Tuan4RangBuocTriggerHam)
ALTER TABLE "Tang" ADD CONSTRAINT "UQ_Tang_Id_ToaNha" UNIQUE ("Id", "ToaNhaId");
ALTER TABLE "CanHo" ADD CONSTRAINT "FK_CanHo_Tang_ToaNha"
    FOREIGN KEY ("TangId", "ToaNhaId") REFERENCES "Tang" ("Id", "ToaNhaId") ON DELETE RESTRICT;''')
    r.p("**b) ToaNha – khu vực của toà và của khu.** Nếu nghiệp vụ quy định toà thuộc khu nào thì cùng khu vực với khu "
        "đó, sẽ có DuAnId → KhuVucId trong ToaNha. Đề tài không áp đặt quy định này vì một khu đô thị có thể trải trên hai "
        "quận; KhuVucId của toà là thông tin độc lập, không suy ra từ khu. Nhóm đã xem xét lại ở tuần 4 và giữ quyết định "
        "này, nên ToaNha đạt 3NF; ứng dụng cũng cho chọn khu vực của toà độc lập với khu.")
    r.p("**c) YeuCauBaoTri không lưu toà của sự cố trong căn.** Toà của một sự cố trong căn suy ra được từ căn (CanHoId → "
        "ToaNhaId), nên lưu thêm sẽ tạo phụ thuộc bắc cầu. Cột ToaNhaKhuVucChungId chỉ có giá trị khi sự cố ở khu vực chung "
        "(lúc đó không có căn), và CHECK ở mục 2.5 bảo đảm hai trường hợp loại trừ nhau.")
    r.p("**d) Các cột trông như dẫn xuất nhưng không phải phụ thuộc hàm.** HanXuLy được tính từ NgayBaoCao và "
        "MucSla.PhutHoanTat tại thời điểm báo, sau đó được dời khi tạm dừng; nó là hạn đã cam kết với cư dân, không phụ "
        "thuộc hàm vào giá trị hiện tại của MucSla (SLA có thể đổi về sau). Tương tự NgayTiepNhan, NgayGiaiQuyet là mốc "
        "thời gian lưu để tính KPI nhanh; chúng có thể truy lại từ lịch sử nhưng lịch sử có thể có nhiều lần mở lại, nên "
        "mốc trên phiếu là nguồn chính thức. AiTriageJson là bản ghi nguyên văn đầu ra của mô hình để đối chiếu và gom dữ "
        "liệu huấn luyện; hệ thống không truy vấn từng thành phần bên trong nên được xem là một giá trị nguyên tố.")
    r.p("**e) Sửa vi phạm 1NF trong lịch sử dự án.** Phiên bản trước lưu danh sách ảnh trong một cột DanhSachAnh dạng "
        "chuỗi URL cách nhau dấu phẩy. Migration SuCoQuyTrinhSlaRangBuoc đã tách cột này thành bảng AnhSuCo, chuyển dữ "
        "liệu cũ rồi mới xoá cột:")
    r.code('''-- Trích migration 20261019113817_SuCoQuyTrinhSlaRangBuoc
INSERT INTO "AnhSuCo" ("YeuCauBaoTriId", "DuongDan", "GiaiDoan", "NguoiTaiLenId", "NgayTaiLen")
SELECT y."Id", trim(u), 0, y."NguoiBaoCaoId", y."NgayBaoCao"
FROM "YeuCauBaoTri" y, unnest(string_to_array(y."DanhSachAnh", ',')) AS u
WHERE coalesce(y."DanhSachAnh", '') <> '' AND trim(u) <> '';
ALTER TABLE "YeuCauBaoTri" DROP COLUMN "DanhSachAnh";''')

    # ---------------- 2.5
    r.h2("2.5. Ràng buộc toàn vẹn")
    r.h3("2.5.1. Ràng buộc cài đặt trong CSDL")
    r.p(f"{r.next_tab()} liệt kê các ràng buộc được khai báo trực tiếp trong CSDL PostgreSQL, gắn với mã quy tắc ở Bảng 1.8.")
    r.table("Ràng buộc toàn vẹn khai báo trong CSDL", ["Mã RB", "Loại", "Bảng", "Nội dung", "Quy tắc"], [
        ("RB01", "CHECK", "YeuCauBaoTri", "(CanHoId IS NOT NULL AND ToaNhaKhuVucChungId IS NULL) OR (CanHoId IS NULL AND "
         "ToaNhaKhuVucChungId IS NOT NULL AND ViTri IS NOT NULL)", "BR-SC-02"),
        ("RB02", "CHECK", "YeuCauBaoTri", "HanXuLy IS NULL OR HanXuLy >= NgayBaoCao", "BR-SC-07"),
        ("RB03", "CHECK", "YeuCauBaoTri", "DanhGia IS NULL OR DanhGia BETWEEN 1 AND 5", "BR-SC-14"),
        ("RB04", "CHECK", "YeuCauBaoTri", "ChiPhiUocTinh >= 0 AND ChiPhiThucTe >= 0", "BR-SC-14"),
        ("RB05", "UNIQUE", "YeuCauBaoTri", "SoYeuCau", "BR-SC-14"),
        ("RB06", "FK", "YeuCauBaoTri", "MucDoUuTien → MucSla, ON DELETE RESTRICT", "BR-SC-08"),
        ("RB07", "FK", "YeuCauBaoTri", "CanHoId → CanHo, ToaNhaKhuVucChungId → ToaNha (RESTRICT); NguoiBaoCaoId, "
         "NguoiPhuTrachId → NguoiDung (NO ACTION)", "BR-SC-02"),
        ("RB08", "CHECK", "MucSla", "PhutPhanHoi > 0 AND PhutHoanTat >= PhutPhanHoi", "BR-SC-07"),
        ("RB09", "FK + CASCADE", "AnhSuCo, LichSu…", "YeuCauBaoTriId → YeuCauBaoTri, ON DELETE CASCADE", "BR-SC-13"),
        ("RB10", "UNIQUE", "Tang", "(ToaNhaId, SoTang)", "BR-CH-01"),
        ("RB11", "UNIQUE có lọc", "CanHo", "(ToaNhaId, MaCanHo) WHERE DaXoa = false", "BR-CH-01"),
        ("RB12", "CHECK", "CanHo", "DienTich > 0; SoPhongNgu >= 0", "BR-CH-02"),
        ("RB13", "FK RESTRICT", "CanHo", "ToaNhaId → ToaNha, TangId → Tang: không xoá được toà, tầng còn căn", "BR-CH-01"),
        ("RB14", "CHECK", "CuDan", "NgayChuyenDi IS NULL OR NgayChuyenDi >= NgayVaoO", "BR-CD-01"),
        ("RB15", "FK", "CuDan", "CanHoId → CanHo (CASCADE); NguoiDungId → NguoiDung (SET NULL)", "BR-CD-02"),
        ("RB16", "PK ghép", "NguoiDung_VaiTro, VaiTro_Quyen", "Một người không nhận một vai trò hai lần", "BR-PQ-01"),
        ("RB17", "UNIQUE", "Quyen, KhuVuc, DuAn, ToaNha", "Ma; Slug", "—"),
    ], [1.4, 2.0, 2.9, 7.6, 2.1], size=11, align_cols=[0])
    r.h3("2.5.2. Ràng buộc cần logic ứng dụng hoặc giao dịch")
    r.p("Một số quy tắc không biểu diễn được bằng PK, FK, UNIQUE, CHECK vì phụ thuộc vào **ai đang thao tác** hoặc **dữ "
        "liệu ở nhiều dòng**. Đề tài đặt chúng ở tầng ứng dụng, trong một lớp duy nhất, và lưu trong cùng một giao dịch "
        "(một lần SaveChanges của Entity Framework là một transaction):")
    r.table("Ràng buộc đảm bảo ở tầng ứng dụng và lý do", ["Quy tắc", "Vì sao không đặt bằng CHECK", "Cách đảm bảo"], [
        ("BR-SC-03 bảng chuyển trạng thái", "Cần biết trạng thái cũ và vai trò người thao tác; CHECK chỉ thấy một dòng mới",
         "MaintenanceWorkflow.Check; đổi trạng thái và ghi lịch sử trong cùng giao dịch"),
        ("BR-SC-04 KTV chỉ thấy việc của mình", "Phụ thuộc người đăng nhập", "Lọc NguoiPhuTrachId = người dùng hiện tại ở mọi truy vấn của cổng KTV"),
        ("BR-SC-07 dời hạn khi tạm dừng", "Cần đọc thời điểm tạm dừng trong bảng lịch sử", "Tính lại HanXuLy khi rời trạng "
         "thái Tạm dừng"),
        ("BR-SC-12 chọn thợ ít việc nhất", "Truy vấn tổng hợp trên nhiều dòng", "Đếm yêu cầu đang mở theo người phụ trách"),
        ("BR-SC-14 số yêu cầu tăng dần", "Hai người gửi cùng lúc có thể sinh cùng số", "UNIQUE (RB05) chặn trùng; ứng dụng "
         "bắt lỗi trùng khoá, cấp số mới và lưu lại"),
        ("Cập nhật đồng thời", "Hai người cùng sửa một phiếu", "Cột PhienBan (row version): lưu bản cũ thì bị từ chối"),
        ("Xoá mềm", "Cần giữ dữ liệu cho truy vết", "Xoá chuyển thành DaXoa = true; bộ lọc toàn cục ẩn dòng đã xoá"),
    ], [4.0, 5.6, 6.4], size=11.5)
    r.p("Cột TrangThai, LoaiBaoTri, MucDoUuTien lưu số nguyên của kiểu liệt kê trong mã nguồn. MucDoUuTien được khoá ngoại "
        "tới MucSla bảo vệ; với TrangThai (0–7) và LoaiBaoTri (0–6, 99), tuần 4 đã thêm CHECK miền giá trị để dữ liệu "
        "nhập trực tiếp vào CSDL cũng không ra ngoài miền:")
    r.code('''ALTER TABLE "YeuCauBaoTri" ADD CONSTRAINT "CK_YeuCauBaoTri_TrangThai" CHECK ("TrangThai" BETWEEN 0 AND 7);
ALTER TABLE "YeuCauBaoTri" ADD CONSTRAINT "CK_YeuCauBaoTri_LoaiBaoTri" CHECK ("LoaiBaoTri" IN (0, 1, 2, 3, 4, 5, 6, 99));''')

    r.h3("2.5.3. Ràng buộc dùng trigger, hàm và stored procedure")
    r.p("Các quy tắc ở mục 2.5.2 hiện được bảo đảm ở tầng ứng dụng vì cần biết vai trò người thao tác. Để dữ liệu vẫn "
        "đúng khi có người sửa trực tiếp trong CSDL, nhóm thiết kế thêm một lớp bảo vệ ở PostgreSQL. Trigger và hàm "
        "được cài ở tuần 4 bằng migration 20261106042803_Tuan4RangBuocTriggerHam; thủ tục sp_giao_viec chỉ được thiết kế và chạy thử, không cài (lý do ở "
        "cuối mục). Phân chia trách nhiệm như sau:")
    r.table("Ràng buộc giao cho trigger, hàm và stored procedure", ["Đối tượng", "Loại", "Quy tắc bảo đảm", "Lý do chọn cách này"], [
        ("trg_yeu_cau_chuyen_trang_thai", "Trigger BEFORE UPDATE", "BR-SC-03 (cặp trạng thái hợp lệ), BR-SC-05, BR-SC-06 "
         "(phần bắt buộc có kết quả khi hoàn tất)",
         "Kiểm tra trên dòng cũ và mới, CHECK không làm được; không kiểm vai trò vì CSDL không biết người dùng ứng dụng. "
         "Lý do từ chối, tạm dừng, mở lại do MaintenanceWorkflow.Check kiểm tra ở tầng ứng dụng"),
        ("fn_han_xu_ly", "Hàm (function)", "BR-SC-07: hạn = thời điểm báo + thời hạn của mức ưu tiên",
         "Một công thức dùng chung cho ứng dụng, script dữ liệu thử và câu SQL đối chiếu"),
        ("sp_giao_viec (thiết kế, không cài)", "Stored procedure", "BR-SC-05, BR-SC-13: giao việc, chuyển Đã tiếp "
         "nhận, ghi lịch sử", "Ba thao tác ghi trong cùng một giao dịch; khoá dòng (FOR UPDATE) tránh hai người giao cùng "
         "lúc"),
        ("Giao dịch của ứng dụng", "Transaction", "Tạo yêu cầu + ảnh + lịch sử; đổi trạng thái + lịch sử",
         "Một lần SaveChanges là một giao dịch; lỗi ở bước nào thì không bước nào được ghi"),
    ], [3.9, 2.9, 4.3, 4.9], size=11)
    r.p("Trigger kiểm tra bảng chuyển trạng thái (mã trạng thái theo từ điển dữ liệu: 0 Mới gửi, 1 Đã tiếp nhận, 2 Đang "
        "xử lý, 3 Đã xử lý, 4 Đã đóng, 5 Từ chối, 6 Tạm dừng, 7 Đã huỷ):")
    r.code('''CREATE OR REPLACE FUNCTION fn_kiem_tra_chuyen_trang_thai() RETURNS trigger AS $$
BEGIN
  IF NEW."TrangThai" = OLD."TrangThai" THEN RETURN NEW; END IF;
  IF (OLD."TrangThai", NEW."TrangThai") NOT IN
     ((0,1),(0,5),(0,7),(1,2),(1,5),(1,7),(2,6),(2,3),(6,2),(3,4),(3,2)) THEN
    RAISE EXCEPTION 'Yêu cầu % không thể chuyển từ trạng thái % sang %',
      OLD."SoYeuCau", OLD."TrangThai", NEW."TrangThai";
  END IF;
  IF NEW."TrangThai" = 2 AND NEW."NguoiPhuTrachId" IS NULL THEN
    RAISE EXCEPTION 'Phải giao kỹ thuật viên trước khi bắt đầu xử lý';
  END IF;
  IF NEW."TrangThai" = 3 AND coalesce(trim(NEW."GhiChuXuLy"), '') = '' THEN
    RAISE EXCEPTION 'Phải ghi kết quả xử lý trước khi hoàn tất';
  END IF;
  RETURN NEW;
END $$ LANGUAGE plpgsql;

CREATE TRIGGER trg_yeu_cau_chuyen_trang_thai
  BEFORE UPDATE OF "TrangThai" ON "YeuCauBaoTri"
  FOR EACH ROW EXECUTE FUNCTION fn_kiem_tra_chuyen_trang_thai();''')
    r.p("Hàm tính hạn xử lý và thủ tục giao việc trong một giao dịch:")
    r.code('''CREATE OR REPLACE FUNCTION fn_han_xu_ly(p_ngay_bao timestamp, p_muc int) RETURNS timestamp AS $$
  SELECT p_ngay_bao + make_interval(mins => s."PhutHoanTat") FROM "MucSla" s WHERE s."MucDoUuTien" = p_muc;
$$ LANGUAGE sql STABLE;

CREATE OR REPLACE PROCEDURE sp_giao_viec(p_yeu_cau int, p_ktv text, p_nguoi_giao text, p_ghi_chu text)
LANGUAGE plpgsql AS $$
DECLARE v_cu int; v_moi int;
BEGIN
  SELECT "TrangThai" INTO v_cu FROM "YeuCauBaoTri" WHERE "Id" = p_yeu_cau FOR UPDATE;
  IF v_cu IS NULL OR v_cu NOT IN (0, 1, 2, 6) THEN
    RAISE EXCEPTION 'Yêu cầu không tồn tại hoặc đã kết thúc, không giao việc được';
  END IF;
  v_moi := CASE WHEN v_cu = 0 THEN 1 ELSE v_cu END;
  UPDATE "YeuCauBaoTri"
     SET "NguoiPhuTrachId" = p_ktv, "TrangThai" = v_moi,
         "NgayTiepNhan" = coalesce("NgayTiepNhan", now() AT TIME ZONE 'UTC')
   WHERE "Id" = p_yeu_cau;
  INSERT INTO "LichSuTrangThaiSuCo"
    ("YeuCauBaoTriId", "TuTrangThai", "SangTrangThai", "NguoiThucHienId", "ThoiDiem", "GhiChu")
  VALUES (p_yeu_cau, v_cu, v_moi, p_nguoi_giao, now() AT TIME ZONE 'UTC', p_ghi_chu);
END $$;''')
    r.p("Ứng dụng thực hiện cùng logic giao việc trong lớp MaintenanceWorkflow (một giao dịch Entity Framework). Để "
        "tránh ghi trùng lịch sử, nhóm chọn ứng dụng là nơi duy nhất ghi lịch sử giao việc nên không cài sp_giao_viec; "
        "thủ tục được giữ trong báo cáo như phương án thiết kế và đã chạy thử đúng (mục 3.6). Trigger chỉ kiểm tra, không "
        "ghi dữ liệu, nên chạy song song với ứng dụng mà không trùng lặp.")

    # ---------------- 2.6
    r.h2("2.6. Từ điển dữ liệu")
    r.p("Từ điển dữ liệu dưới đây lấy theo lược đồ thật của CSDL (kiểu dữ liệu PostgreSQL do migration sinh ra). Các bảng "
        "nghiệp vụ có thêm tám cột kiểm toán chung được mô tả một lần ở Bảng 2.16.")
    dd = {
        "YeuCauBaoTri": [
            ("Id", "integer identity", "PK", "Khoá thay thế"),
            ("SoYeuCau", "varchar(40)", "NOT NULL, UNIQUE", "Số yêu cầu cư dân nhìn thấy"),
            ("CanHoId", "integer", "FK → CanHo, NULL", "Căn xảy ra sự cố"),
            ("ToaNhaKhuVucChungId", "integer", "FK → ToaNha, NULL", "Toà có sự cố khu vực chung"),
            ("ViTri", "varchar(200)", "NULL; bắt buộc khi khu vực chung", "Mô tả vị trí khu vực chung"),
            ("NguoiBaoCaoId", "text", "FK → NguoiDung, NULL", "Người báo (ứng dụng luôn ghi khi tạo yêu cầu)"),
            ("NguoiPhuTrachId", "text", "FK → NguoiDung, NULL", "Kỹ thuật viên phụ trách"),
            ("TieuDe", "varchar(250)", "NOT NULL", "Tiêu đề ngắn"),
            ("MoTa", "varchar(4000)", "NOT NULL", "Mô tả của cư dân"),
            ("LoaiBaoTri", "integer", "NOT NULL", "0 Nước, 1 Điện, 2 Thiết bị, 3 Kết cấu, 4 Vệ sinh, 5 Côn trùng, 6 Mạng, 99 Khác"),
            ("MucDoUuTien", "integer", "NOT NULL, FK → MucSla", "0 Thấp, 1 Trung bình, 2 Cao, 3 Khẩn cấp"),
            ("TrangThai", "integer", "NOT NULL", "0 Mới gửi … 7 Đã huỷ (Hình 1.5)"),
            ("GhiChuXuLy", "varchar(2000)", "NULL", "Kết quả xử lý / lý do từ chối"),
            ("AiTriageJson", "text", "NULL", "JSON nguyên văn mô hình AI trả về"),
            ("ChiPhiUocTinh, ChiPhiThucTe", "numeric(18,0)", "CHECK ≥ 0", "Chi phí (đồng)"),
            ("TinhPhiKhachThue", "boolean", "NOT NULL", "Có tính phí cho cư dân"),
            ("NgayBaoCao", "timestamp", "NOT NULL", "Thời điểm báo (UTC)"),
            ("NgayTiepNhan, NgayGiaiQuyet, NgayDongLai", "timestamp", "NULL", "Các mốc của vòng đời"),
            ("HanXuLy", "timestamp", "NULL, CHECK ≥ NgayBaoCao", "Hạn hoàn tất theo SLA"),
            ("DanhGia", "integer", "NULL, CHECK 1–5", "Điểm hài lòng"),
            ("NhanXet", "varchar(1000)", "NULL", "Nhận xét của cư dân"),
        ],
        "LichSuTrangThaiSuCo": [
            ("Id", "integer identity", "PK", "Khoá thay thế"),
            ("YeuCauBaoTriId", "integer", "NOT NULL, FK CASCADE", "Yêu cầu"),
            ("TuTrangThai", "integer", "NULL", "Trạng thái trước; NULL khi tạo yêu cầu"),
            ("SangTrangThai", "integer", "NOT NULL", "Trạng thái sau; bằng TuTrangThai khi chỉ giao lại"),
            ("NguoiThucHienId", "text", "FK → NguoiDung, NULL", "NULL = hệ thống tự động"),
            ("ThoiDiem", "timestamp", "NOT NULL", "Thời điểm thực hiện"),
            ("GhiChu", "varchar(1000)", "NULL", "Lý do, nội dung giao việc"),
        ],
        "AnhSuCo": [
            ("Id", "integer identity", "PK", "Khoá thay thế"),
            ("YeuCauBaoTriId", "integer", "NOT NULL, FK CASCADE", "Yêu cầu"),
            ("DuongDan", "varchar(500)", "NOT NULL", "Đường dẫn ảnh đã tải lên"),
            ("GiaiDoan", "integer", "NOT NULL", "0 lúc báo, 1 nghiệm thu"),
            ("NguoiTaiLenId", "text", "FK → NguoiDung, NULL", "Người tải ảnh"),
            ("NgayTaiLen", "timestamp", "NOT NULL", "Thời điểm tải"),
        ],
        "MucSla": [
            ("MucDoUuTien", "integer", "PK", "0 Thấp, 1 Trung bình, 2 Cao, 3 Khẩn cấp"),
            ("PhutPhanHoi", "integer", "CHECK > 0", "Thời hạn tiếp nhận (phút)"),
            ("PhutHoanTat", "integer", "CHECK ≥ PhutPhanHoi", "Thời hạn hoàn tất (phút)"),
        ],
        "CanHo": [
            ("Id", "integer identity", "PK", "Khoá thay thế"),
            ("ToaNhaId", "integer", "NOT NULL, FK RESTRICT", "Toà (xem 2.4.3 a)"),
            ("TangId", "integer", "NOT NULL, FK RESTRICT", "Tầng"),
            ("MaCanHo", "varchar(40)", "NOT NULL; UNIQUE (ToaNhaId, MaCanHo)", "Mã căn, ví dụ A-12.05"),
            ("DienTich", "double precision", "CHECK > 0", "Diện tích (m²)"),
            ("SoPhongNgu, SoPhongTam", "integer", "CHECK SoPhongNgu ≥ 0", "Số phòng"),
            ("HuongNha", "integer", "NULL", "Hướng (0 Đông … 7 Tây Nam)"),
            ("TinhTrangThue", "integer", "NOT NULL", "0 Trống, 1 Chưa bàn giao, 2 Đang ở, 3 Đang sửa chữa"),
            ("MoTa", "text", "NULL", "Ghi chú"),
        ],
        "CuDan": [
            ("Id", "integer identity", "PK", "Khoá thay thế"),
            ("CanHoId", "integer", "NOT NULL, FK CASCADE", "Căn đang ở"),
            ("NguoiDungId", "text", "FK → NguoiDung, SET NULL", "Tài khoản (nếu có)"),
            ("HoTen", "varchar(150)", "NOT NULL", "Họ tên"),
            ("SoDienThoai, Email, SoGiayTo", "varchar(30/200/30)", "NULL", "Liên hệ, giấy tờ"),
            ("VaiTro", "integer", "NOT NULL", "0 Chủ hộ, 1 Người thuê, 2 Thành viên"),
            ("DaiDienCanHo", "boolean", "NOT NULL", "Người đại diện nhận thông báo"),
            ("NgayVaoO", "timestamp", "NOT NULL", "Ngày vào ở"),
            ("NgayChuyenDi", "timestamp", "NULL, CHECK ≥ NgayVaoO", "NULL = đang ở"),
            ("GhiChu", "varchar(1000)", "NULL", "Ghi chú"),
        ],
        "ToaNha": [
            ("Id", "integer identity", "PK", "Khoá thay thế"),
            ("KhuVucId", "integer", "NOT NULL, FK RESTRICT", "Khu vực"),
            ("DuAnId", "integer", "FK → DuAn, SET NULL", "Khu (nếu có)"),
            ("QuanLyId", "text", "FK → NguoiDung, SET NULL", "Quản lý toà"),
            ("Ma, Ten, Slug", "varchar(40/200/220)", "NOT NULL; Slug UNIQUE", "Mã, tên toà"),
            ("DiaChi", "varchar(500)", "NOT NULL", "Địa chỉ"),
            ("SoLuongTang", "integer", "NOT NULL", "Số tầng khai báo"),
            ("TrangThai", "integer", "NOT NULL", "0 Hoạt động, 1 Bảo trì, 2 Đóng"),
            ("NgayChotKyMacDinh", "integer", "CHECK 1–28", "Chính sách phí (phân hệ hỗ trợ)"),
            ("DonGiaPhiQuanLy", "numeric(18,0)", "CHECK ≥ 0", "Đơn giá phí quản lý/m²"),
        ],
        "Tang": [
            ("Id", "integer identity", "PK", "Khoá thay thế"),
            ("ToaNhaId", "integer", "NOT NULL, FK CASCADE", "Toà"),
            ("SoTang", "integer", "NOT NULL; UNIQUE (ToaNhaId, SoTang)", "Số thứ tự tầng"),
            ("NhanTang", "varchar(50)", "NOT NULL", "Nhãn hiển thị, ví dụ “Tầng 12”"),
        ],
    }
    for name, cols in dd.items():
        r.table(f"Cấu trúc bảng {name}", ["STT", "Thuộc tính", "Kiểu dữ liệu", "Ràng buộc", "Mô tả"],
                [(i, *c) for i, c in enumerate(cols, 1)], [1.3, 3.8, 2.8, 3.9, 4.2], size=11, align_cols=[0])
    r.table("Tám cột kiểm toán chung của các bảng nghiệp vụ", ["Thuộc tính", "Kiểu dữ liệu", "Mô tả"], [
        ("NgayTao, NguoiTao", "timestamp, varchar(450)", "Thời điểm và người tạo dòng"),
        ("NgayCapNhat, NguoiCapNhat", "timestamp, varchar(450)", "Lần sửa gần nhất"),
        ("DaXoa, NgayXoa, NguoiXoa", "boolean, timestamp, varchar(450)", "Xoá mềm: dòng được đánh dấu, không xoá vật lý"),
        ("PhienBan", "bytea (row version)", "Phát hiện cập nhật đồng thời"),
    ], [5.0, 4.6, 6.4])

    # ---------------- 2.7
    r.h2("2.7. Minh hoạ một nghiệp vụ hoàn chỉnh qua dữ liệu")
    r.p("Kịch bản: cư dân căn A-12.05 (Toà A) báo “Ổ điện phòng ngủ phát ra tiếng xẹt xẹt và có mùi khét, mặt ổ cắm nóng "
        "ran”, để nguyên loại “Khác” và mức “Trung bình”. Dữ liệu dưới đây là dữ liệu mô phỏng, thời gian theo giờ Việt "
        "Nam để dễ đọc (trong CSDL lưu UTC). Luồng dữ liệu đi qua các bảng:")
    r.p("NguoiDung (cư dân) → CuDan → CanHo → **YeuCauBaoTri** ← MucSla; YeuCauBaoTri → AnhSuCo, LichSuTrangThaiSuCo "
        "→ NguoiDung (thợ điện); mọi thay đổi → NhatKyHeThong.", align=WD_ALIGN_PARAGRAPH.CENTER, indent=False)
    r.p("Bước 1 – Kiểm tra người báo (BR-SC-01). Tài khoản của cư dân căn A-12.05 có một dòng CuDan (CanHoId = căn "
        "A-12.05, NgayChuyenDi = NULL) nên được phép báo cho căn này.")
    r.p("**Bước 2 – AI phân loại và gộp với lựa chọn của cư dân (BR-SC-09).** Mô hình trả về:")
    r.code('{"category":"electrical","handler":"technician","priority":"urgent","is_dangerous":true,\n'
           ' "need_more_info":false,"follow_up_question":"","summary":"Ổ điện phòng ngủ xẹt lửa, có mùi khét, nóng."}')
    r.p("Cư dân chọn “Khác” nên loại lấy theo AI là Điện (1); sự cố nguy hiểm nên mức ưu tiên được nâng từ Trung bình "
        "(1) lên Khẩn cấp (3). Hạn xử lý = 08:00 + 120 phút của mức Khẩn cấp trong MucSla = 10:00.")
    r.table("Dòng được ghi vào YeuCauBaoTri (các cột chính)", ["Cột", "Giá trị", "Cột", "Giá trị"], [
        ("SoYeuCau", "MR-202611-0001", "LoaiBaoTri", "1 (Điện)"),
        ("CanHoId / ToaNhaKhuVucChungId", "id căn A-12.05 / NULL", "MucDoUuTien", "3 (Khẩn cấp)"),
        ("NguoiBaoCaoId", "id tài khoản cư dân", "TrangThai", "1 (Đã tiếp nhận)"),
        ("NguoiPhuTrachId", "id tài khoản thợ điện", "NgayBaoCao", "02/11/2026 08:00"),
        ("NgayTiepNhan", "02/11/2026 08:00", "HanXuLy", "02/11/2026 10:00"),
    ], [4.4, 4.0, 3.2, 4.4], size=11.5)
    r.p("Số yêu cầu có dạng MR-<năm tháng>-<số thứ tự trong tháng>. **Bước 3 – Điều phối (BR-SC-12).** Loại Điện ứng với vai "
        "trò Electrician; thợ điện có ít yêu cầu đang mở nhất được giao, yêu cầu chuyển sang Đã tiếp nhận ngay khi lưu, "
        "thợ nhận thư “KHẨN CẤP – Giao việc”, quản lý nhận thư “Cần theo dõi”. **Bước 4 – Xử lý và nghiệm thu.** Bảng "
        "lịch sử sau khi kết thúc:")
    r.table("Các dòng LichSuTrangThaiSuCo của yêu cầu minh hoạ", ["ThoiDiem", "TuTrangThai", "SangTrangThai", "NguoiThucHienId", "GhiChu"], [
        ("08:00", "NULL", "0 Mới gửi", "cư dân", "Tạo yêu cầu"),
        ("08:00", "0 Mới gửi", "1 Đã tiếp nhận", "NULL (hệ thống)", "Giao cho Thợ điện: Hệ thống điều phối theo loại sự cố"),
        ("08:20", "1 Đã tiếp nhận", "2 Đang xử lý", "thợ điện", "—"),
        ("09:10", "2 Đang xử lý", "3 Đã xử lý", "thợ điện", "Thay ổ cắm cháy, siết lại dây"),
        ("18:30", "3 Đã xử lý", "4 Đã đóng", "cư dân", "Đánh giá 5"),
    ], [2.0, 2.6, 2.9, 3.0, 5.5], size=11)
    r.p("Cùng lúc, AnhSuCo có một ảnh GiaiDoan = 0 (lúc báo) và một ảnh GiaiDoan = 1 (nghiệm thu); YeuCauBaoTri được cập "
        "nhật NgayGiaiQuyet = 09:10, GhiChuXuLy, NgayDongLai = 18:30, DanhGia = 5; mỗi lần lưu sinh dòng NhatKyHeThong "
        "với giá trị cũ, giá trị mới. Từ dữ liệu này: thời gian phản hồi 0 phút (≤ 15 phút), thời gian xử lý 1,17 giờ, "
        "hoàn tất trước hạn 10:00, việc do hệ thống tự giao và không bị giao lại – đúng các cột cần cho KPI ở mục 1.6.")

    # ---------------- 2.8
    r.h2("2.8. Truy vấn và tổng hợp dữ liệu")
    r.p("Các KPI ở Bảng 1.10 được tính bằng truy vấn tổng hợp trên ba bảng YeuCauBaoTri, MucSla, LichSuTrangThaiSuCo. "
        "Báo cáo trên web (FR-09) dùng đúng công thức này; khi kiểm thử, số liệu trên màn hình được đối chiếu với kết quả "
        "chạy trực tiếp các câu SQL sau trên CSDL (tham số :tu, :den là đầu và cuối kỳ theo UTC).")
    r.code('''-- KPI-1..4: đúng hạn SLA, MTTR, thời gian phản hồi, phản hồi đúng SLA
SELECT COUNT(*)                                                     AS tong_yeu_cau,
       COUNT(*) FILTER (WHERE y."NgayGiaiQuyet" IS NOT NULL)         AS da_xu_ly,
       ROUND(100.0 * COUNT(*) FILTER (WHERE y."NgayGiaiQuyet" <= y."HanXuLy")
             / NULLIF(COUNT(*) FILTER (WHERE y."NgayGiaiQuyet" IS NOT NULL
                                         AND y."HanXuLy" IS NOT NULL), 0), 1) AS ty_le_dung_han,
       ROUND((AVG(EXTRACT(EPOCH FROM y."NgayGiaiQuyet" - y."NgayBaoCao")) / 3600)::numeric, 1) AS mttr_gio,
       ROUND((AVG(EXTRACT(EPOCH FROM y."NgayTiepNhan" - y."NgayBaoCao")) / 60)::numeric, 1)    AS phan_hoi_phut,
       ROUND(100.0 * COUNT(*) FILTER (WHERE y."NgayTiepNhan" - y."NgayBaoCao"
                                              <= make_interval(mins => s."PhutPhanHoi"))
             / NULLIF(COUNT(y."NgayTiepNhan"), 0), 1)                   AS ty_le_phan_hoi_dung_sla,
       ROUND(AVG(y."DanhGia"), 2)                                       AS csat
FROM "YeuCauBaoTri" y
JOIN "MucSla" s ON s."MucDoUuTien" = y."MucDoUuTien"
WHERE NOT y."DaXoa" AND y."NgayBaoCao" >= :tu AND y."NgayBaoCao" < :den;''')
    r.p("AVG bỏ qua giá trị NULL nên MTTR và thời gian phản hồi chỉ tính trên các yêu cầu đã có mốc tương ứng; NULLIF "
        "tránh chia cho 0 khi kỳ chưa có yêu cầu nào được xử lý (báo cáo hiển thị “—” thay vì 0%).")
    r.code('''-- KPI-5: yêu cầu đang mở và đã quá hạn, sắp theo mức trễ
SELECT y."SoYeuCau", y."MucDoUuTien", y."TrangThai", y."HanXuLy",
       now() AT TIME ZONE 'UTC' - y."HanXuLy" AS tre
FROM "YeuCauBaoTri" y
WHERE NOT y."DaXoa" AND y."TrangThai" IN (0, 1, 2, 6)
  AND y."HanXuLy" < now() AT TIME ZONE 'UTC'
ORDER BY tre DESC;''')
    r.code('''-- KPI-7: tỉ lệ việc hệ thống tự giao mà quản lý không giao lại
WITH ai AS (
    SELECT DISTINCT h."YeuCauBaoTriId" AS id
    FROM "LichSuTrangThaiSuCo" h
    WHERE h."NguoiThucHienId" IS NULL
      AND h."GhiChu" LIKE '%Hệ thống điều phối theo loại sự cố%'
), giao_lai AS (
    SELECT DISTINCT h."YeuCauBaoTriId" AS id
    FROM "LichSuTrangThaiSuCo" h
    WHERE h."TuTrangThai" = h."SangTrangThai" AND h."NguoiThucHienId" IS NOT NULL
)
SELECT COUNT(*) AS ai_giao,
       ROUND(100.0 * COUNT(*) FILTER (WHERE g.id IS NULL) / NULLIF(COUNT(*), 0), 1) AS ty_le_giu_nguyen
FROM ai JOIN "YeuCauBaoTri" y ON y."Id" = ai.id
LEFT JOIN giao_lai g ON g.id = ai.id
WHERE NOT y."DaXoa" AND y."NgayBaoCao" >= :tu AND y."NgayBaoCao" < :den;''')
    r.p("Một lần giao lại được nhận biết là dòng lịch sử có trạng thái trước và sau bằng nhau do người dùng thực hiện, "
        "vì giao việc khi yêu cầu đã tiếp nhận không đổi trạng thái. Các chỉ số phân rã theo mức ưu tiên, loại, toà và kỹ "
        "thuật viên dùng cùng công thức với GROUP BY; toà của một yêu cầu lấy theo CanHo.ToaNhaId hoặc "
        "ToaNhaKhuVucChungId. Về hiệu năng, CSDL đã có chỉ mục trên TrangThai, MucDoUuTien, NguoiPhuTrachId, HanXuLy và "
        "(YeuCauBaoTriId, ThoiDiem) của bảng lịch sử; chỉ mục trên NgayBaoCao được bổ sung ở tuần 4 vì "
        "mọi báo cáo đều lọc theo kỳ báo.")

    # ---------------- 2.9
    r.h2("2.9. Kết quả đạt được đến tuần 3 và việc tiếp theo")
    r.p("Đến hết tuần 3, đề tài đã có: phiếu đề tài; mô hình AS-IS/TO-BE với sáu điểm bất cập và cách TO-BE xử lý từng "
        "điểm; 19 quy tắc nghiệp vụ có mã; 7 KPI có công thức kèm phân rã; SRS rút gọn gồm 13 yêu cầu chức năng có tiêu chí nghiệm "
        "thu và 7 yêu cầu phi chức năng; 16 use case; ERD 14 thực thể, 18 mối kết hợp có bản số; lược đồ 16 bảng có kiểm "
        "tra 3NF; 17 nhóm ràng buộc trong CSDL, 7 ràng buộc ở tầng ứng dụng và thiết kế trigger, hàm, thủ tục; minh hoạ một nghiệp vụ hoàn "
        "chỉnh qua dữ liệu và câu SQL tính KPI.")
    r.p("Các việc chuyển sang tuần 4: (1) cài đặt khoá ngoại ghép (TangId, ToaNhaId) cho CanHo; (2) thêm CHECK miền giá "
        "trị cho TrangThai và LoaiBaoTri; (3) thêm chỉ mục trên NgayBaoCao; (4) xác nhận quy định khu vực của toà (mục "
        "2.4.3 b); (5) cài đặt và kiểm thử trigger, hàm, thủ tục ở mục 2.5.3; (6) bổ sung biên bản phỏng vấn cho nguồn "
        "căn cứ số 2 ở Bảng 1.2; (7) vẽ bằng StarUML sơ đồ lớp, sơ đồ tuần tự luồng AI phân loại, sơ đồ kiến trúc. Các việc "
        "(1), (2), (3), (5), (7) đã hoàn thành ở tuần 4; việc (4) và (6) được khép lại như ghi ở mục 3.7.")


def references(r):
    r.h1("TÀI LIỆU THAM KHẢO")
    refs = [
        "[1] AXELOS (2019). ITIL Foundation: ITIL 4 Edition. TSO, London.",
        "[2] Bộ Xây dựng (2024). Thông tư số 05/2024/TT-BXD quy định chi tiết một số điều của Luật Nhà ở (kèm Quy chế quản "
        "lý, sử dụng nhà chung cư).",
        "[3] Codd, E.F. (1970). A relational model of data for large shared data banks. Communications of the ACM, 13(6), 377 – 387.",
        "[4] Elmasri, R. & Navathe, S.B. (2016). Fundamentals of Database Systems, 7th edition. Pearson.",
        "[5] Dettmers, T., Pagnoni, A., Holtzman, A. & Zettlemoyer, L. (2023). QLoRA: Efficient finetuning of quantized LLMs. "
        "In Advances in Neural Information Processing Systems 36 (NeurIPS 2023).",
        "[6] Hu, E.J., Shen, Y., Wallis, P. et al. (2022). LoRA: Low-rank adaptation of large language models. In Proceedings "
        "of the International Conference on Learning Representations (ICLR 2022).",
        "[7] Microsoft (2026). ASP.NET Core documentation. https://learn.microsoft.com/aspnet/core",
        "[8] Object Management Group (2017). OMG Unified Modeling Language (UML), Version 2.5.1. https://www.omg.org/spec/UML/2.5.1",
        "[9] Ollama (2026). Ollama – get up and running with large language models. https://github.com/ollama/ollama",
        "[10] Quốc hội nước CHXHCN Việt Nam (2023). Luật Nhà ở số 27/2023/QH15.",
        "[11] PostgreSQL Global Development Group (2026). PostgreSQL documentation – Constraints. "
        "https://www.postgresql.org/docs/current/ddl-constraints.html",
        "[12] Qwen Team (2025). Qwen3 Technical Report. arXiv:2505.09388.",
        "[13] Wiegers, K. & Beatty, J. (2013). Software Requirements, 3rd edition. Microsoft Press.",
    ]
    for ref in refs:
        para = r.p(ref, indent=False)
        para.paragraph_format.left_indent = Cm(1)
        para.paragraph_format.first_line_indent = Cm(-1)
        para.alignment = WD_ALIGN_PARAGRAPH.LEFT


# =====================================================================
def main():
    doc = Document(str(HERE / "trang_bia.docx"))
    setup_styles(doc)
    fill_cover(doc)
    r = Report(doc)

    front = new_section(doc)
    page_numbers(front, fmt="lowerRoman", start=1)
    footer_page_field(front)
    front_matter(r)

    main_sec = new_section(doc)
    page_numbers(main_sec, fmt="decimal", start=1)
    chapter1(r)
    chapter2(r, doc)
    chapter3(r)
    references(r)
    doc.core_properties.title = TITLE
    doc.core_properties.author = ", ".join(n for n, _ in STUDENTS)
    doc.save(str(OUT))
    print("saved", OUT)


if __name__ == "__main__":
    main()
