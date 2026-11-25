"""Nội dung Chương 2 (thiết kế dữ liệu), bản gọn cho chuyên ngành hệ thống thông tin (25/11/2026).
Số liệu bảng, cột lấy từ lược đồ thật (luoc_do.json, đọc từ PostgreSQL). Trả về danh sách khối để
sua_bao_cao_v2.py dựng vào file Word.

Khối: ("h2", text) ("h3", text) ("p", text) ("bullet", text) ("table", caption, header, rows, widths)
      ("fig", caption, đường dẫn ảnh)
"""
import json
from pathlib import Path

import so_do_v2 as sd

HERE = Path(__file__).resolve().parent
IMG = HERE.parent / "images_v2"
S = json.loads((HERE / "luoc_do.json").read_text(encoding="utf8"))


def rd_line(t):
    """Lược đồ dạng chữ (khối "rd"): (đoạn chữ, gạch chân, in nghiêng). Bản gọn không dùng nhưng giữ cho trình dựng."""
    cs = S[t]["columns"]
    order = [c for c in cs if c["pk"]] + [c for c in cs if c["fk"] and not c["pk"]] + \
            [c for c in cs if not c["pk"] and not c["fk"]]
    parts = [(t + " (", False, False)]
    for i, c in enumerate(order):
        parts.append((c["name"], c["pk"], bool(c["fk"])))
        parts.append((", " if i < len(order) - 1 else ")", False, False))
    return parts


def blocks():
    B = []
    p = lambda s: B.append(("p", s))
    b = lambda s: B.append(("bullet", s))
    T = lambda cap, head, rows, w: B.append(("table", cap, head, rows, w))
    n_bang = len(S)
    n_cot = sum(len(v["columns"]) for v in S.values())

    p("Chương này trình bày phần dữ liệu của hệ thống: thực thể, ERD, lược đồ quan hệ, chuẩn hoá và ràng buộc toàn "
      "vẹn. Mọi bảng và cột lấy từ cơ sở dữ liệu PostgreSQL đang chạy.")

    # ------------------------------------------------------------------ 2.1
    B.append(("h2", "2.1. Thực thể và quy ước đặt tên"))
    p(f"Từ quy trình TO-BE và các chứng từ ở Bảng 1.4, nhóm xác định 18 thực thể và 3 mối kết hợp nhiều – nhiều "
      f"(Bảng 2.1). Ba mối kết hợp này thành ba bảng CuTru, NguoiDung_VaiTro, VaiTro_Quyen, nên cơ sở dữ liệu có "
      f"{n_bang} bảng với {n_cot} cột nghiệp vụ. Tên thực thể trên ERD là tên bảng viết hoa, nối bằng gạch dưới "
      f"(TOA_NHA là bảng ToaNha).")
    T("Bảng 2.1. Thực thể và lý do cần bảng riêng", ["Thực thể", "Nghiệp vụ", "Lý do cần bảng riêng"], [
        ["KHU", "Khu căn hộ gồm nhiều toà", "Địa chỉ chung của các toà"],
        ["TOA_NHA", "Toà nhà và chính sách thu phí", "Có quản lý riêng, có sự cố khu vực chung"],
        ["TANG", "Tầng của toà", "Toà có nhiều tầng, căn nằm trên tầng"],
        ["CAN_HO", "Căn hộ", "Nơi ở và nơi tính phí"],
        ["NGUOI_DUNG", "Cư dân, quản lý, kỹ thuật viên, quản trị", "Một người là một tài khoản"],
        ["cư trú (CuTru)", "Ai ở căn nào, từ ngày nào", "Mối kết hợp n – n có thuộc tính riêng"],
        ["VAI_TRO, QUYEN", "Nhóm người dùng và quyền theo phân hệ", "Phân quyền, chọn thợ theo chuyên môn"],
        ["MUC_UU_TIEN", "Mức ưu tiên và thời hạn xử lý", "Đổi thời hạn không cần sửa chương trình"],
        ["YEU_CAU_BAO_TRI", "Yêu cầu sửa chữa khi có sự cố", "Đối tượng chính của quy trình sự cố"],
        ["ANH_YEU_CAU", "Ảnh lúc báo, ảnh nghiệm thu", "Một yêu cầu có nhiều ảnh"],
        ["LICH_SU_YEU_CAU", "Mỗi lần đổi trạng thái, giao việc", "Truy vết và tính KPI"],
        ["NHAT_KY_HE_THONG", "Vết thay đổi dữ liệu", "Kiểm toán"],
        ["LOAI_DICH_VU", "Điện, nước và đơn giá", "Đơn giá dùng chung"],
        ["CHI_SO_DICH_VU", "Chỉ số đồng hồ hằng tháng", "Mỗi căn, mỗi loại, mỗi kỳ một dòng"],
        ["PHI_DINH_KY", "Phí cố định của căn (gửi xe…)", "Mỗi căn có số khoản khác nhau"],
        ["HOA_DON, CHI_TIET_HOA_DON", "Giấy báo phí và các khoản trên đó", "Chứng từ công nợ của căn"],
        ["THANH_TOAN", "Lần nộp tiền", "Một hoá đơn nộp nhiều lần"],
    ], [3.4, 4.0, 4.0])
    p("Bảng kỹ thuật của ASP.NET Identity (claim, token, đăng nhập ngoài) không mang thông tin nghiệp vụ nên không "
      "đưa vào phân tích. Tên bảng và cột theo ba quy ước ở Bảng 2.2, viết liền không dấu, mỗi từ viết hoa chữ đầu.")
    T("Bảng 2.2. Ba quy ước đặt tên", ["Quy ước", "Nội dung", "Ví dụ"], [
        ["1. Tên bảng", "Danh từ chỉ đối tượng nghiệp vụ, số ít, trùng tên thực thể trên ERD", "Khu, CanHo, HoaDon"],
        ["2. Tên cột", "Ngắn, đọc như cụm từ thường dùng trong nghiệp vụ; chỉ ghép thêm tên đối tượng khi đứng một "
         "mình dễ nhầm", "NgayVaoO, HanXuLy, DaThu, ConNo; TrangThaiHoaDon"],
        ["3. Tên khoá", "Khoá chính: tên bảng + Id. Khoá ngoại: trùng tên khoá chính được tham chiếu; nhiều khoá "
         "tới cùng một bảng thì đặt theo vai trò", "CanHoId; NguoiBaoId, NguoiPhuTrachId"],
    ], [2.4, 5.4, 3.6])
    p("Bảng nghiệp vụ dùng khoá số nguyên tự tăng; khoá tự nhiên (SoHoaDon, MaCanHo, MaToaNha…) vẫn khai báo duy nhất. "
      "NguoiDungId, VaiTroId là chuỗi do ASP.NET Identity sinh. MucUuTienId là mã 0–3, cũng là thứ tự ưu tiên.")

    # ------------------------------------------------------------------ 2.2
    B.append(("h2", "2.2. Mô hình thực thể kết hợp (ERD)"))
    p("Hình chữ nhật là thực thể, thuộc tính khoá gạch chân, thuộc tính có dấu / đứng trước là thuộc tính suy diễn "
      "(tính từ thuộc tính khác). Hình thoi là mối kết hợp; thuộc tính của mối kết hợp ghi trong khung nối nét đứt. "
      "Cặp (min, max) cạnh thực thể cho biết mỗi thể hiện tham gia mối kết hợp ít nhất và nhiều nhất bao nhiêu lần. "
      "ERD chia bốn hình theo nhóm nghiệp vụ.")
    erd = [sd.erd_co_cau(), sd.erd_tai_khoan(), sd.erd_su_co(), sd.erd_thu_phi()]
    for i, (spec, cap) in enumerate(zip(erd, ["ERD cơ cấu chung cư và cư trú", "ERD tài khoản và phân quyền",
                                              "ERD sự cố và bảo trì", "ERD phân hệ thu phí"])):
        B.append(("fig", f"Hình 2.{i + 1}. {cap}", IMG / (spec["name"] + ".png")))
    p("Một số bản số cần lưu ý. Toà có ít nhất một tầng, yêu cầu có ít nhất một dòng lịch sử, hoá đơn có ít nhất một "
      "dòng chi tiết, người dùng có ít nhất một vai trò. Một yêu cầu xảy ra tại một căn hoặc ở khu vực chung của một "
      "toà, không đồng thời cả hai. NHAT_KY_HE_THONG không nối với thực thể nào vì nhật ký phải giữ cả thao tác trên "
      "dòng đã bị xoá.")

    # ------------------------------------------------------------------ 2.3
    B.append(("h2", "2.3. Lược đồ quan hệ"))
    p("ERD được chuyển sang lược đồ quan hệ theo các quy tắc:")
    b("Mỗi thực thể thành một quan hệ; thuộc tính khoá thành khoá chính.")
    b("Mối kết hợp 1 – n: khoá chính phía nhiều thành khoá ngoại ở phía một; bắt buộc nếu bản số là (1,1), cho phép "
      "rỗng nếu là (0,1).")
    b("Mối kết hợp n – n thành quan hệ riêng gồm hai khoá ngoại và các thuộc tính của mối kết hợp.")
    b("Thuộc tính suy diễn thành cột do cơ sở dữ liệu tự tính (TongTien, ConNo, ThanhTien), không ai ghi tay.")
    p("Hình 2.5 đến Hình 2.8 vẽ lược đồ quan hệ theo bốn nhóm như ERD, ký pháp chân gà. Cột đầu của mỗi bảng ghi PK "
      "(khoá chính), FK (khoá ngoại), U (duy nhất), N (cho phép rỗng); cột cuối là kiểu dữ liệu. Chân gà nằm ở bảng "
      "chứa khoá ngoại, kèm vòng tròn nếu dòng cha có thể chưa có dòng con, kèm vạch nếu phải có ít nhất một. Hai khoá "
      "ngoại tới cùng một bảng được vẽ thành hai đường có ghi tên khoá.")
    for i, (f, cap) in enumerate([(sd.rd_co_cau, "Lược đồ quan hệ cơ cấu chung cư và cư trú"),
                                  (sd.rd_tai_khoan, "Lược đồ quan hệ tài khoản và phân quyền"),
                                  (sd.rd_su_co, "Lược đồ quan hệ sự cố và bảo trì"),
                                  (sd.rd_thu_phi, "Lược đồ quan hệ phân hệ thu phí")]):
        B.append(("fig", f"Hình 2.{5 + i}. {cap}", IMG / (f()["name"] + ".png")))
    p("CuTru là liên kết thường, không phải hợp thành: căn còn người cư trú thì không xoá được, xoá căn không xoá "
      "người. Khi xoá dòng cha, khoá ngoại tới bộ phận của nó (ảnh, lịch sử, dòng hoá đơn) xoá theo; khoá tuỳ chọn tới "
      "người dùng chuyển về rỗng; các khoá còn lại chặn xoá. Ứng dụng chỉ xoá mềm (cột DaXoa), nên đây là lớp bảo vệ "
      "khi sửa trực tiếp trong cơ sở dữ liệu. Mỗi bảng nghiệp vụ có thêm bảy cột kiểm toán (NgayTao, NguoiTao, "
      "NgayCapNhat, NguoiCapNhat, DaXoa, NgayXoa, NguoiXoa) không vẽ trên hình.")

    # ------------------------------------------------------------------ 2.4
    B.append(("h2", "2.4. Chuẩn hoá"))
    p("Một lược đồ đạt 3NF khi mọi thuộc tính không khoá phụ thuộc đầy đủ và trực tiếp vào khoá [4]; BCNF chặt hơn: "
      "vế trái của mọi phụ thuộc hàm không tầm thường đều là siêu khoá. Sau các quyết định dưới đây, vế trái của mọi "
      f"phụ thuộc hàm đều là khoá chính hoặc khoá tự nhiên đã khai báo duy nhất, nên cả {n_bang} quan hệ đạt BCNF.")
    b("Toà – tầng – căn: CanHo chỉ giữ TangId, ToaNha không giữ địa chỉ hay số tầng, vì lấy được qua Tang và Khu. Tên "
      "toà không lưu vì luôn là “Toà” + mã toà.")
    b("Người: họ tên, điện thoại, email chỉ nằm ở NguoiDung. Cư dân là người dùng có dòng CuTru đang hiệu lực; “Đang "
      "ở” hay “Để trống” suy ra từ CuTru, TinhTrangCanHo chỉ lưu điều Ban quản lý quyết định.")
    b("Sự cố: mốc tiếp nhận, hoàn tất và trạng thái trước của mỗi lần chuyển lấy từ LichSuYeuCau (dòng liền trước), "
      "không lưu lại.")
    b("Thu phí: lượng tiêu thụ là hiệu hai chỉ số; TongTien, ConNo, ThanhTien do cơ sở dữ liệu tự tính. TienPhi, "
      "DaThu, đơn giá từng dòng được chốt trên chứng từ vì hoá đơn cũ phải giữ đúng số tiền lúc lập dù bảng giá đổi; "
      "HanXuLy của sự cố cũng là hạn đã hứa lúc báo.")

    # ------------------------------------------------------------------ 2.5
    B.extend(rbtv_blocks())

    # ------------------------------------------------------------------ 2.6
    B.append(("h2", "2.6. Kiểu dữ liệu và bảng mã"))
    p("Kiểu dữ liệu chọn theo loại thông tin (Bảng 2.7), để cùng một loại thông tin luôn cùng một kiểu. Các cột trạng "
      "thái, loại lưu mã số; ý nghĩa của từng mã ở Bảng 2.8.")
    T("Bảng 2.7. Quy ước kiểu dữ liệu", ["Loại thông tin", "Kiểu", "Lý do"], [
        ["Khoá bảng nghiệp vụ", "integer tự tăng (nhật ký: bigint)", "Ngắn, không đổi; nhật ký tăng rất nhanh"],
        ["Khoá người dùng, vai trò", "varchar(36)", "Mã GUID do ASP.NET Identity sinh"],
        ["Tiền", "numeric(18,0)", "Đồng Việt Nam không có phần lẻ; số chính xác"],
        ["Diện tích, chỉ số, số lượng", "numeric(8,2), numeric(18,2)", "Lẻ hai chữ số, chính xác khi nhân đơn giá"],
        ["Trạng thái, loại", "integer + CHECK", "Tập mã cố định (Bảng 2.8)"],
        ["Ngày / thời điểm", "date / timestamp (giờ UTC)", "Ngày vào ở, hạn thanh toán không cần giờ; lúc báo sự "
         "cố cần đến phút"],
        ["Chuỗi", "varchar có độ dài", "Mã 40; tên 200; đường dẫn, địa chỉ 500; ghi chú 1000; mô tả 2000"],
    ], [3.4, 3.6, 4.4])
    T("Bảng 2.8. Bảng mã của các cột trạng thái và loại", ["Cột", "Mã và ý nghĩa"], [
        ["ToaNha.TrangThaiToaNha", "0 đang vận hành, 1 đang bảo trì, 2 đã đóng"],
        ["CanHo.TinhTrangCanHo", "0 đã bàn giao, 1 chưa bàn giao, 3 đang sửa chữa"],
        ["CuTru.VaiTroTrongHo", "0 chủ hộ, 1 người thuê, 2 thành viên"],
        ["MucUuTien.MucUuTienId", "0 thấp, 1 trung bình, 2 cao, 3 khẩn cấp"],
        ["YeuCauBaoTri.LoaiSuCo", "0 nước, 1 điện, 2 thiết bị, 3 kết cấu, 4 vệ sinh, 5 côn trùng, 6 mạng, 99 khác"],
        ["YeuCauBaoTri.TrangThaiYeuCau, LichSuYeuCau.TrangThaiMoi",
         "0 mới gửi, 1 đã tiếp nhận, 2 đang xử lý, 3 đã xử lý, 4 đã đóng, 5 từ chối, 6 tạm dừng, 7 đã huỷ"],
        ["AnhYeuCau.GiaiDoan", "0 ảnh lúc báo, 1 ảnh nghiệm thu"],
        ["HoaDon.LoaiHoaDon", "0 phí tháng, 3 phát sinh"],
        ["HoaDon.TrangThaiHoaDon", "0 nháp, 1 đã phát hành, 2 trả một phần, 3 đã thanh toán, 4 quá hạn, 5 đã huỷ, "
         "6 đã hoàn tiền"],
        ["ChiTietHoaDon.LoaiKhoan", "0 phí quản lý, 1 điện nước, 2 phí định kỳ, 3 sửa chữa, 4 khác"],
        ["ThanhToan.PhuongThuc", "0 tiền mặt, 1 chuyển khoản, 2 thẻ, 3 ví điện tử, 4 mã QR"],
        ["ThanhToan.TrangThaiThanhToan", "0 chờ xác nhận, 1 thành công, 2 thất bại, 3 đã hoàn, 4 hoàn một phần, "
         "5 đã huỷ"],
        ["NhatKyHeThong.LoaiThaoTac", "0 thêm, 1 sửa, 2 xoá, 3 xoá mềm, 4 khôi phục, 5 đăng nhập, 6 đăng xuất"],
    ], [4.4, 7.0])

    # ------------------------------------------------------------------ 2.7
    B.append(("h2", "2.7. Kết quả"))
    p(f"Thiết kế dữ liệu gồm {n_bang} bảng, bốn ERD và bốn sơ đồ lược đồ quan hệ; mọi quan hệ đạt BCNF. Các KPI ở "
      "Bảng 1.10 tính từ đúng một nguồn: mốc thời gian sự cố từ LichSuYeuCau, căn có người ở từ CuTru, số tiền từ "
      "HoaDon. Theo nhận xét của giảng viên, nhóm đã bỏ các bảng và cột không dùng hoặc lưu hai nơi, đặt lại tên cho "
      "dễ đọc và áp thay đổi vào cơ sở dữ liệu thật. Bảng 2.9 đối chiếu thiết kế với năm tính chất của cơ sở dữ liệu.")
    T("Bảng 2.9. Thiết kế đáp ứng các tính chất của cơ sở dữ liệu", ["Tính chất", "Cách bảo đảm", "Mục"], [
        ["Tính cấu trúc", "Bảng có kiểu dữ liệu, khoá và ràng buộc khai báo trong lược đồ; đi từ ERD sang lược đồ "
         "quan hệ", "2.2, 2.3, 2.6"],
        ["Tính toàn vẹn", "PK, FK, UNIQUE, CHECK cho ràng buộc trong một bảng; ba trigger cho ràng buộc cần dữ liệu "
         "nhiều dòng", "2.5"],
        ["Tính nhất quán", "Thay đổi liên quan lưu trong một giao dịch; tổng tiền, còn nợ, thành tiền do cơ sở dữ "
         "liệu tự tính; khoá lạc quan chặn hai người ghi đè nhau", "2.4, 2.5"],
        ["Tính an toàn và bảo mật", "Phân quyền theo vai trò và quyền; mật khẩu chỉ lưu dạng băm; nhật ký ghi mọi "
         "thay đổi; xoá mềm", "2.3"],
        ["Tính độc lập", "Ứng dụng truy cập qua lớp ánh xạ, tên cột khai báo riêng: đổi tên cột chỉ cần migration, "
         "không sửa mã nghiệp vụ", "2.6"],
        ["Tính không dư thừa", "Mọi quan hệ đạt BCNF; mỗi thông tin chỉ lưu một nơi, chỉ lưu số chốt trên chứng từ",
         "2.4"],
    ], [2.8, 6.6, 2.0])
    return B


# ====================================================================== ràng buộc toàn vẹn
# Ràng buộc trên một bảng (miền giá trị và liên thuộc tính): (bối cảnh, nội dung, biểu diễn, tên CHECK)
MOT_BANG = [
    ("CanHo", "Diện tích lớn hơn 0; số phòng ngủ không âm; tình trạng thuộc {0, 1, 3}.",
     "∀c ∈ CanHo: c.DienTich > 0 ∧ c.SoPhongNgu ≥ 0 ∧ c.TinhTrangCanHo ∈ {0, 1, 3}",
     "CK_CanHo_DienTich, CK_CanHo_SoPhongNgu, CK_CanHo_TinhTrangCanHo"),
    ("ToaNha", "Phí quản lý không âm; ngày lập phí từ 1 đến 28; số ngày ân hạn không âm; tỉ lệ phí trễ từ 0 đến "
     "100; trạng thái từ 0 đến 2.", "∀t ∈ ToaNha: t.PhiQuanLyM2 ≥ 0 ∧ 1 ≤ t.NgayLapPhi ≤ 28 ∧ t.SoNgayAnHan ≥ 0 ∧ "
     "0 ≤ t.TiLePhiTre ≤ 100 ∧ 0 ≤ t.TrangThaiToaNha ≤ 2",
     "CK_ToaNha_PhiQuanLyM2, CK_ToaNha_NgayLapPhi, CK_ToaNha_PhiTre, CK_ToaNha_TrangThai"),
    ("CuTru", "Vai trò trong hộ thuộc {0, 1, 2}; ngày chuyển đi (nếu có) không trước ngày vào ở.",
     "∀c ∈ CuTru: c.VaiTroTrongHo ∈ {0, 1, 2} ∧ (c.NgayChuyenDi = NULL ∨ c.NgayChuyenDi ≥ c.NgayVaoO)",
     "CK_CuTru_VaiTroTrongHo, CK_CuTru_NgayChuyenDi"),
    ("MucUuTien", "Mã mức từ 0 đến 3; thời hạn phản hồi lớn hơn 0 và không vượt thời hạn hoàn tất.",
     "∀m ∈ MucUuTien: 0 ≤ m.MucUuTienId ≤ 3 ∧ 0 < m.PhutPhanHoi ≤ m.PhutHoanTat",
     "CK_MucUuTien_Id, CK_MucUuTien_ThoiHan"),
    ("YeuCauBaoTri", "Trạng thái, loại sự cố thuộc bảng mã; đánh giá (nếu có) từ 1 đến 5; chi phí không âm; hạn xử "
     "lý không trước ngày báo.", "∀y ∈ YeuCauBaoTri: 0 ≤ y.TrangThaiYeuCau ≤ 7 ∧ y.LoaiSuCo ∈ {0..6, 99} ∧ "
     "(y.DiemDanhGia = NULL ∨ 1 ≤ y.DiemDanhGia ≤ 5) ∧ y.ChiPhiSuaChua ≥ 0 ∧ (y.HanXuLy = NULL ∨ y.HanXuLy ≥ y.NgayBao)",
     "CK_YeuCauBaoTri_TrangThai, _LoaiSuCo, _DanhGia, _ChiPhi, _HanXuLy"),
    ("YeuCauBaoTri", "Đúng một trong hai cột CanHoId, ToaNhaId có giá trị; sự cố khu vực chung phải ghi vị trí.",
     "∀y: (y.CanHoId ≠ NULL ∧ y.ToaNhaId = NULL) ∨ (y.CanHoId = NULL ∧ y.ToaNhaId ≠ NULL ∧ y.ViTri ≠ NULL)",
     "CK_YeuCauBaoTri_NoiXayRa"),
    ("YeuCauBaoTri", "Đã xử lý hoặc đã đóng thì phải có kết quả xử lý.",
     "∀y: y.TrangThaiYeuCau ∈ {3, 4} ⇒ y.KetQuaXuLy ≠ rỗng", "CK_YeuCauBaoTri_KetQua"),
    ("AnhYeuCau", "Giai đoạn ảnh thuộc {0, 1}.", "∀a ∈ AnhYeuCau: a.GiaiDoan ∈ {0, 1}", "CK_AnhYeuCau_GiaiDoan"),
    ("LoaiDichVu, PhiDinhKy", "Đơn giá và số tiền khoản phí không âm.",
     "∀l ∈ LoaiDichVu: l.DonGia ≥ 0; ∀p ∈ PhiDinhKy: p.SoTien ≥ 0", "CK_LoaiDichVu_DonGia, CK_PhiDinhKy_SoTien"),
    ("ChiSoDichVu", "Tháng của kỳ từ 1 đến 12; chỉ số cũ không âm, chỉ số mới không nhỏ hơn chỉ số cũ.",
     "∀s ∈ ChiSoDichVu: 1 ≤ s.Ky mod 100 ≤ 12 ∧ 0 ≤ s.ChiSoCu ≤ s.ChiSoMoi", "CK_ChiSoDichVu_Ky, CK_ChiSoDichVu_ChiSo"),
    ("HoaDon", "Trạng thái từ 0 đến 6; tháng của kỳ từ 1 đến 12; số tiền không âm; đã thu không vượt tổng; hạn thanh "
     "toán không trước ngày lập.", "∀h ∈ HoaDon: 0 ≤ h.TrangThaiHoaDon ≤ 6 ∧ 1 ≤ h.Ky mod 100 ≤ 12 ∧ h.TienPhi, "
     "h.PhiTreHan ≥ 0 ∧ 0 ≤ h.DaThu ≤ h.TienPhi + h.PhiTreHan ∧ h.HanThanhToan ≥ h.NgayLap",
     "CK_HoaDon_TrangThai, _Ky, _SoTien, _DaThu, _HanThanhToan"),
    ("ChiTietHoaDon", "Loại khoản từ 0 đến 4; số lượng lớn hơn 0; đơn giá không âm; dòng điện nước có chỉ số, dòng "
     "phí định kỳ có khoản phí, chỉ dòng sửa chữa mới trỏ tới sự cố.", "∀d: 0 ≤ d.LoaiKhoan ≤ 4 ∧ d.SoLuong > 0 ∧ "
     "d.DonGia ≥ 0 ∧ (d.LoaiKhoan = 1 ⇔ d.ChiSoDichVuId ≠ NULL) ∧ (d.LoaiKhoan = 2 ⇔ d.PhiDinhKyId ≠ NULL) ∧ "
     "(d.YeuCauBaoTriId ≠ NULL ⇒ d.LoaiKhoan = 3)", "CK_ChiTietHoaDon_LoaiKhoan, _SoTien, _Nguon"),
    ("ThanhToan", "Số tiền lớn hơn 0; phương thức, trạng thái thuộc bảng mã; số hoàn trả không âm và không vượt số "
     "tiền.", "∀p ∈ ThanhToan: p.SoTien > 0 ∧ 0 ≤ p.PhuongThuc ≤ 4 ∧ 0 ≤ p.TrangThaiThanhToan ≤ 5 ∧ "
     "0 ≤ p.SoTienHoanTra ≤ p.SoTien", "CK_ThanhToan_SoTien, _PhuongThuc, _TrangThai, _HoanTra"),
]
LB = [  # (mã, bối cảnh, nội dung, biểu diễn, cách đảm bảo, [(quan hệ, T, X, S)])
    ("LB01", "YeuCauBaoTri, CuTru", "Người báo là người đang cư trú tại căn xảy ra sự cố, trừ khi quản lý tạo thay.",
     "∀y ∈ YeuCauBaoTri, y.CanHoId ≠ NULL: ∃c ∈ CuTru: c.NguoiDungId = y.NguoiBaoId ∧ c.CanHoId = y.CanHoId ∧ "
     "c.NgayChuyenDi = NULL", "Ứng dụng, kiểm tra trước khi lưu",
     [("YeuCauBaoTri", "+", "–", "+ (NguoiBaoId, CanHoId)"), ("CuTru", "–", "–", "– (*)")]),
    ("LB02", "YeuCauBaoTri, NguoiDung_VaiTro", "Người phụ trách giữ vai trò kỹ thuật viên hoặc quản lý.",
     "∀y, y.NguoiPhuTrachId ≠ NULL: ∃v ∈ NguoiDung_VaiTro: v.NguoiDungId = y.NguoiPhuTrachId ∧ v.VaiTroId ∈ "
     "{thợ, quản lý}", "Ứng dụng khi giao việc", [("YeuCauBaoTri", "+", "–", "+ (NguoiPhuTrachId)"),
                                                  ("NguoiDung_VaiTro", "–", "+", "+ (VaiTroId)")]),
    ("LB03", "YeuCauBaoTri", "Trạng thái chỉ chuyển theo bảng chuyển trạng thái (Bảng 1.7); bắt đầu xử lý phải có "
     "người phụ trách.", "∀y → y': (y.TrangThaiYeuCau, y'.TrangThaiYeuCau) ∈ Chuyển ∧ (y'.TrangThaiYeuCau = 2 ⇒ "
     "y'.NguoiPhuTrachId ≠ NULL)", "Trigger TG1 và ứng dụng", [("YeuCauBaoTri", "–", "–", "+ (TrangThaiYeuCau)")]),
    ("LB04", "YeuCauBaoTri, LichSuYeuCau", "Yêu cầu có ngay một dòng lịch sử lúc tạo; mỗi lần đổi trạng thái, giao "
     "việc thêm đúng một dòng, trong cùng giao dịch.", "∀y ∈ YeuCauBaoTri: ∃l ∈ LichSuYeuCau: l.YeuCauBaoTriId = "
     "y.YeuCauBaoTriId ∧ l.TrangThaiMoi = y.TrangThaiYeuCau (dòng mới nhất)", "Giao dịch ở ứng dụng",
     [("YeuCauBaoTri", "+", "–", "+ (TrangThaiYeuCau, NguoiPhuTrachId)"), ("LichSuYeuCau", "–", "+", "+")]),
    ("LB05", "YeuCauBaoTri, MucUuTien", "Hạn xử lý = ngày báo + thời hạn hoàn tất của mức ưu tiên, chốt lúc báo, "
     "cộng thời gian tạm dừng.", "∀y: y.HanXuLy = y.NgayBao + m.PhutHoanTat + Σ thời gian tạm dừng "
     "(m ∈ MucUuTien, m.MucUuTienId = y.MucUuTienId lúc báo)", "Ứng dụng, hàm FN1",
     [("YeuCauBaoTri", "+", "–", "+ (MucUuTienId, HanXuLy)"), ("MucUuTien", "–", "–", "– (*)")]),
    ("LB06", "ThanhToan, HoaDon", "Tổng các khoản thành công (trừ phần hoàn) không vượt tổng tiền hoá đơn.",
     "∀h ∈ HoaDon: Σ (p.SoTien − p.SoTienHoanTra | p ∈ ThanhToan, p.HoaDonId = h.HoaDonId, "
     "p.TrangThaiThanhToan ∈ {1, 3, 4}) ≤ h.TongTien", "Trigger TG3 và ứng dụng",
     [("ThanhToan", "+", "–", "+ (SoTien, SoTienHoanTra, TrangThaiThanhToan)"), ("HoaDon", "–", "–", "+ (TienPhi)")]),
    ("LB07", "HoaDon, ThanhToan, ChiTietHoaDon", "Đã thu bằng tổng các khoản thành công trừ phần hoàn; tiền phí bằng "
     "tổng thành tiền các dòng.", "∀h: h.DaThu = Σ (p.SoTien − p.SoTienHoanTra | p thành công của h) ∧ "
     "h.TienPhi = Σ (d.ThanhTien | d ∈ ChiTietHoaDon của h)", "Ứng dụng trong cùng giao dịch; khung nhìn đối chiếu",
     [("HoaDon", "–", "–", "+ (TienPhi, DaThu)"), ("ThanhToan", "+", "+", "+"), ("ChiTietHoaDon", "+", "+", "+")]),
    ("LB08", "ChiSoDichVu, ChiTietHoaDon, HoaDon", "Chỉ số đã đưa vào hoá đơn chưa huỷ thì không được sửa hay xoá.",
     "∀s ∈ ChiSoDichVu: (∃d ∈ ChiTietHoaDon, h ∈ HoaDon: d.ChiSoDichVuId = s.ChiSoDichVuId ∧ d.HoaDonId = h.HoaDonId "
     "∧ h.TrangThaiHoaDon ≠ 5) ⇒ s không đổi", "Trigger TG2",
     [("ChiSoDichVu", "–", "+", "+"), ("ChiTietHoaDon", "–", "–", "–")]),
    ("LB09", "ChiSoDichVu", "Chỉ số cũ của một kỳ bằng chỉ số mới của kỳ liền trước (nếu có).",
     "∀s, s' cùng căn, cùng loại, s'.Ky là kỳ liền trước của s.Ky: s.ChiSoCu = s'.ChiSoMoi", "Ứng dụng",
     [("ChiSoDichVu", "+", "+", "+ (ChiSoCu, ChiSoMoi)")]),
    ("LB10", "ChiTietHoaDon, HoaDon, ChiSoDichVu", "Dòng điện nước trỏ về chỉ số của đúng căn và đúng kỳ của hoá đơn.",
     "∀d, d.ChiSoDichVuId ≠ NULL: s.CanHoId = h.CanHoId ∧ s.Ky = h.Ky (s, h là chỉ số và hoá đơn của d)",
     "Ứng dụng khi lập hoá đơn", [("ChiTietHoaDon", "+", "–", "+ (ChiSoDichVuId)"), ("HoaDon", "–", "–", "+"),
                                  ("ChiSoDichVu", "–", "–", "+")]),
    ("LB11", "HoaDon, CanHo", "Căn chưa bàn giao không được lập hoá đơn phí tháng.",
     "∀h, h.LoaiHoaDon = 0: căn của h có TinhTrangCanHo ≠ 1 lúc lập", "Ứng dụng",
     [("HoaDon", "+", "–", "–"), ("CanHo", "–", "–", "– (*)")]),
    ("LB12", "HoaDon, ThanhToan", "Hoá đơn đã có thanh toán thành công phải hoàn tiền trước khi huỷ; huỷ hoá đơn thì "
     "các thanh toán đang chờ bị huỷ theo.", "∀h: h.TrangThaiHoaDon = 5 ⇒ ∄p ∈ ThanhToan của h có trạng thái 0 hoặc 1",
     "Ứng dụng, trong một giao dịch", [("HoaDon", "–", "–", "+ (TrangThaiHoaDon)"), ("ThanhToan", "+", "–", "+")]),
    ("LB13", "CuTru, CanHo", "Mỗi căn có người đang ở có đúng một người đại diện; căn chưa bàn giao thì không có người "
     "đang ở.", "∀căn a có người ở: |{c ∈ CuTru | c.CanHoId = a ∧ c.LaDaiDien ∧ c.NgayChuyenDi = NULL}| = 1 ∧ "
     "a.TinhTrangCanHo ≠ 1", "Chỉ mục duy nhất có điều kiện (tối đa một) và ứng dụng",
     [("CuTru", "+", "+", "+ (LaDaiDien, NgayChuyenDi)"), ("CanHo", "–", "–", "+ (TinhTrangCanHo)")]),
    ("LB14", "CuTru, NguoiDung", "Cư trú chỉ hiện trên cổng cư dân khi người đó đã xác nhận qua thư mời.",
     "Căn hiện trên cổng của u ⇔ ∃c ∈ CuTru: c.NguoiDungId = u ∧ c.NgayXacNhan ≠ NULL ∧ c.NgayChuyenDi = NULL",
     "Ứng dụng, mã mời ký số hạn 14 ngày", [("CuTru", "+", "–", "+ (NgayXacNhan)")]),
    ("LB15", "NguoiDung, NguoiDung_VaiTro", "Mỗi tài khoản có ít nhất một vai trò.",
     "∀u ∈ NguoiDung: ∃v ∈ NguoiDung_VaiTro: v.NguoiDungId = u.NguoiDungId", "Ứng dụng",
     [("NguoiDung", "+", "–", "–"), ("NguoiDung_VaiTro", "–", "+", "+")]),
]


def _duoi(ck):
    """"CK_HoaDon_TrangThai, _Ky" -> "TrangThai, Ky": bỏ tiền tố CK_<bảng>_ đã nêu ở cột bối cảnh."""
    return ", ".join(x.strip().lstrip("_").split("_", 2)[-1] if x.strip().startswith("CK_") else x.strip().lstrip("_")
                     for x in ck.split(","))


def rbtv_blocks():
    B = []
    p = lambda s: B.append(("p", s))
    T = lambda cap, head, rows, w: B.append(("table", cap, head, rows, w))
    B.append(("h2", "2.5. Ràng buộc toàn vẹn"))
    p("Mỗi ràng buộc ghi bối cảnh (các quan hệ liên quan), nội dung, biểu diễn hình thức và cách cài đặt. Khoá chính, "
      "khoá ngoại và khoá duy nhất đã thể hiện trên lược đồ quan hệ (PK, FK, U) nên không liệt kê lại.")
    B.append(("h3", "2.5.1. Ràng buộc trên một bảng"))
    p("Ràng buộc miền giá trị và liên thuộc tính chỉ xét các cột của cùng một dòng, nên khai báo bằng CHECK. Chỉ thao "
      "tác Thêm và Sửa trên chính bảng đó mới có thể vi phạm. Cột CHECK ghi phần cuối tên ràng buộc trong cơ sở dữ "
      "liệu; tên đầy đủ là CK_<bảng>_<phần cuối>, ví dụ DienTich là CK_CanHo_DienTich.")
    T("Bảng 2.3. Ràng buộc trên một bảng", ["Mã", "Bối cảnh", "Nội dung", "Biểu diễn", "CHECK"],
      [[f"RB{i:02d}", t, nd, bd, _duoi(ck)] for i, (t, nd, bd, ck) in enumerate(MOT_BANG, 1)],
      [1.1, 1.9, 3.1, 3.6, 1.9])
    B.append(("h3", "2.5.2. Ràng buộc liên bộ, liên quan hệ"))
    p("Các ràng buộc ở Bảng 2.4 cần dữ liệu nhiều dòng, nhiều bảng hoặc phụ thuộc người thao tác. Ứng dụng kiểm tra "
      "chúng trong cùng giao dịch với thao tác ghi; ràng buộc phải đúng cả khi sửa trực tiếp cơ sở dữ liệu thì có "
      "thêm trigger. Bảng 2.5 cho tầm ảnh hưởng: thao tác Thêm (T), Xoá (X), Sửa (S) nào trên quan hệ nào có thể làm "
      "vi phạm. Dấu + là phải kiểm tra, – là không ảnh hưởng, + (cột) là chỉ khi sửa các cột đó, (*) là chỉ cần kiểm "
      "tra lúc tạo dòng.")
    T("Bảng 2.4. Ràng buộc liên bộ, liên quan hệ", ["Mã", "Bối cảnh", "Nội dung", "Biểu diễn", "Cách đảm bảo"],
      [[m, bc, nd, bd, cd] for m, bc, nd, bd, cd, _ in LB], [1.1, 2.2, 2.9, 3.6, 1.7])
    T("Bảng 2.5. Tầm ảnh hưởng của các ràng buộc liên bộ, liên quan hệ", ["Mã", "Quan hệ", "Thêm", "Xoá", "Sửa"],
      [[m if j == 0 else "", r, tt, x, ss] for m, *_, rows in LB for j, (r, tt, x, ss) in enumerate(rows)],
      [1.0, 3.6, 1.1, 1.1, 4.4])
    B.append(("h3", "2.5.3. Trigger và hàm"))
    p("Ba trigger và một hàm ở Bảng 2.6 chỉ kiểm tra rồi chấp nhận hoặc từ chối lệnh ghi, không tự ghi thêm dữ liệu; "
      "mỗi cái đã được thử bằng lệnh vi phạm cố ý. Khung nhìn vw_doi_chieu_hoa_don tính lại tiền phí và số đã thu từ "
      "dòng chi tiết và thanh toán để phát hiện chênh lệch (ngày 25/11/2026: 0 hoá đơn lệch).")
    T("Bảng 2.6. Trigger và hàm ở tầng cơ sở dữ liệu",
      ["Mã", "Loại, đối tượng", "Điều kiện kiểm tra", "Khi vi phạm", "Ràng buộc"], [
          ["TG1", "Trigger BEFORE UPDATE OF TrangThaiYeuCau trên YeuCauBaoTri",
           "Cặp (trạng thái cũ, mới) thuộc bảng chuyển; chuyển sang Đang xử lý phải có NguoiPhuTrachId",
           "Huỷ lệnh, báo số yêu cầu và hai trạng thái", "LB03"],
          ["TG2", "Trigger BEFORE UPDATE, DELETE trên ChiSoDichVu",
           "Chỉ số có dòng thuộc hoá đơn chưa huỷ thì không sửa, không xoá", "Huỷ lệnh, báo lỗi", "LB08"],
          ["TG3", "Trigger AFTER INSERT, UPDATE trên ThanhToan",
           "Tổng thu thành công trừ phần hoàn của hoá đơn ≤ TongTien", "Huỷ cả giao dịch, báo lỗi", "LB06"],
          ["FN1", "Hàm fn_han_xu_ly(ngày báo, mức)", "Trả về ngày báo + PhutHoanTat của mức trong MucUuTien",
           "Không có", "LB05"],
      ], [1.1, 3.2, 3.5, 2.2, 1.3])
    return B
