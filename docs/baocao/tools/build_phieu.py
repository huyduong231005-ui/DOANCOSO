"""Phiếu theo dõi tiến độ (BM03/QT05/ĐT-KT) đồng bộ với lộ trình bám sát và việc nhóm đã làm.

Bố cục lấy từ phiếu gốc của nhóm (Word, đã chấp nhận revision) ở `--src`; chỉ thay tên đề tài và nội dung
cột "Nội dung" của 10 tuần, giữ nguyên định dạng ô, dòng đánh giá ở tuần 8 và phần ký tên.
Chạy: python build_phieu.py --src <phieu.docx>  ->  docs/baocao/Phieu_theo_doi_tien_do_DACN.docx
"""
import argparse
import copy
from pathlib import Path

from docx import Document

from build_report import TITLE_VI

OUT = Path(__file__).resolve().parent.parent / "Phieu_theo_doi_tien_do_DACN.docx"

# (tuần, tiêu đề, các gạch đầu dòng) — khớp lộ trình 10 tuần và 4 mốc 25/10, 08/11, 29/11, 20/12.
WEEKS = [
    ("Khảo sát, chốt phạm vi.", [
        "Chốt đề tài, lý do chọn, bối cảnh BQL chung cư; nộp phiếu đề tài.",
        "Trả lời 6 câu hỏi Bước 1; xác định tác nhân, nguồn dữ liệu, mục tiêu, phạm vi (MoSCoW), phân công."]),
    ("Chương 1: nghiệp vụ, yêu cầu.", [
        "Khảo sát BQL, ghi nguồn xác minh.",
        "AS-IS/TO-BE, tác nhân, quy tắc nghiệp vụ (SLA ghi rõ là giả định), KPI.",
        "SRS sơ bộ (FR/NFR/BR, nghiệm thu), use case sơ bộ.",
        "Mốc 25/10: duyệt bài toán, yêu cầu."]),
    ("Phân tích dữ liệu.", [
        "Thực thể từ nghiệp vụ (cư dân, căn hộ, yêu cầu sự cố, ảnh, lịch sử trạng thái, mức SLA), lý do cần từng bảng.",
        "ERD, bản số, bảng trung gian, PK/FK; lược đồ quan hệ, 3NF.",
        "Ràng buộc (PK, FK, UNIQUE, CHECK, trigger, thủ tục); từ điển dữ liệu; minh họa nghiệp vụ qua dữ liệu, truy "
        "vấn KPI."]),
    ("Hoàn thiện thiết kế CSDL, UML, kiến trúc.", [
        "UML (StarUML): Use Case tổng quát, 4 chi tiết (include/extend) + đặc tả; Activity; State; Class; Sequence; "
        "kiến trúc MVC – PostgreSQL – AI.",
        "Kiểm tra tự động mô hình khớp mã nguồn, CSDL.",
        "Hoàn thiện CSDL: ràng buộc còn thiếu, trigger, hàm (migration), dữ liệu thử; đồng bộ ứng dụng với ERD.",
        "Mốc 08/11: duyệt mô hình dữ liệu, hệ thống."]),
    # Tuần 5–6 khớp mục 4.8 của báo cáo: làm trên toàn hệ thống, không chỉ phân hệ sự cố.
    ("Demo luồng đầu tiên của các phân hệ.", [
        "Đăng nhập, phân quyền 4 nhóm người dùng; tài khoản cư dân qua thư mời.",
        "Toà – căn – cư dân; hoá đơn tháng, cư dân xác nhận thanh toán, BQL duyệt.",
        "Gửi sự cố, giao việc, đổi trạng thái có lịch sử; ghi thật vào CSDL."]),
    ("Chức năng chính các phân hệ, ngoại lệ.", [
        "Thu phí: chỉ số, phí định kỳ, hoá đơn tự lập, thanh toán – duyệt – hoàn tiền, công nợ, phí trễ.",
        "Sự cố: tiếp nhận → nghiệm thu → đánh giá; SLA, thư báo quá hạn; AI lỗi vẫn lưu.",
        "Chặn ngoại lệ ở máy chủ (lập trùng kỳ, thu vượt, sửa đồng thời…); kiểm thử toàn hệ thống.",
        "Bổ sung thu phí vào Chương 1–3."]),
    # Tuần 7–10: kế hoạch theo toàn hệ thống (mục 4.8); chữ ngắn để phiếu giữ 3 trang.
    ("Báo cáo các phân hệ; thử nghiệm AI.", [
        "Báo cáo theo khoảng thời gian: tài chính, công nợ phân tuổi, tình trạng căn, KPI sự cố; đối chiếu SQL.",
        "Thử AI: phân loại, chỉ nâng ưu tiên, phát hiện nguy hiểm, hỏi lại khi mơ hồ; 120 ca có nhãn, 200 ca dò.",
        "Mốc 29/11: nghiệm thu MVP, demo đủ các phân hệ."]),
    ("Tích hợp, kiểm thử, đánh giá AI.", [
        "Kiểm thử mọi phân hệ: chức năng, ngoại lệ, ràng buộc, giao dịch, phân quyền; đối chiếu báo cáo với CSDL.",
        "Đánh giá AI: độ chính xác loại, mức ưu tiên, tỉ lệ việc tự giao được giữ, ca AI sai và cách xử lý; sửa lỗi."]),
    ("Đánh giá khả năng sử dụng, hoàn thiện hồ sơ.", [
        "Người dùng thử theo vai trò (nếu có điều kiện); tách kết quả đã đo với lợi ích dự kiến.",
        "Hoàn thiện báo cáo, dữ liệu demo các phân hệ, video, hướng dẫn cài đặt, sử dụng."]),
    ("Nghiệm thu thử, khóa phiên bản.", [
        "Nghiệm thu thử với GVHD, sửa lỗi cuối, khóa phiên bản phần mềm và báo cáo.",
        "Mỗi thành viên giải thích được ERD/3NF, nghiệp vụ các phân hệ, luồng AI.",
        "Mốc 20/12: nghiệm thu hoàn chỉnh."]),
]


DATES = ["12–18/10", "19–25/10", "26/10–01/11", "02–08/11", "09–15/11", "16–22/11", "23–29/11", "30/11–06/12",
         "07–13/12", "14–20/12"]


def set_text(para, text):
    """Giữ định dạng run đầu, bỏ các run sau."""
    for r in para.runs[1:]:
        r._r.getparent().remove(r._r)
    para.runs[0].text = text


def fill_cell(cell, title, bullets):
    paras = cell.paragraphs
    head = paras[0]
    old = [p for p in paras[1:] if p.text.startswith("•")]
    template = copy.deepcopy(old[0]._p)
    anchor = head._p
    for p in old:
        p._p.getparent().remove(p._p)
    set_text(head, title)
    for b in bullets:
        new = copy.deepcopy(template)
        anchor.addnext(new)
        anchor = new
    for p, b in zip(cell.paragraphs[1:1 + len(bullets)], bullets):
        set_text(p, "• " + b)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--src", required=True)
    doc = Document(ap.parse_args().src)
    title = next(p for p in doc.paragraphs if p.text.startswith("Tên đề tài:"))
    title.runs[1].text = " " + TITLE_VI
    table = doc.tables[0]
    for week, (head, bullets) in enumerate(WEEKS, 1):
        fill_cell(table.cell(week, 2), head, bullets)
        date = table.cell(week, 1).paragraphs[0]
        set_text(date, DATES[week - 1])         # phiếu gốc ghi lẫn "26/10 - 01/11" và "12–18/10"
    doc.save(str(OUT))
    print("saved", OUT)


if __name__ == "__main__":
    main()
