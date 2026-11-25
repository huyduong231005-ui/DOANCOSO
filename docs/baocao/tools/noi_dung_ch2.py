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

    p("Chương này trình bày phần dữ liệu của hệ thống: thực thể, ERD, lược đồ quan hệ, chuẩn hoá và ràng buộc toàn "
      "vẹn.")

    # ------------------------------------------------------------------ 2.1
    B.append(("h2", "2.1. Thực thể"))
    p(f"Từ quy trình TO-BE và các chứng từ ở Bảng 1.4, nhóm xác định 16 thực thể và 5 mối kết hợp nhiều – nhiều "
      f"(Bảng 2.1). Mỗi mối kết hợp nhiều – nhiều thành một bảng riêng, nên cơ sở dữ liệu có {n_bang} bảng.")
    T("Bảng 2.1. Thực thể, mối kết hợp và lý do cần bảng riêng", ["Thực thể", "Nghiệp vụ", "Lý do cần bảng riêng"], [
        ["KHU", "Khu căn hộ gồm nhiều toà", "Địa chỉ chung của các toà"],
        ["TOA_NHA", "Toà nhà, ngày lập phí, mức phí trễ", "Có quản lý riêng, có sự cố khu vực chung"],
        ["TANG", "Tầng của toà", "Toà có nhiều tầng, căn nằm trên tầng"],
        ["CAN_HO", "Căn hộ", "Nơi ở và nơi tính phí"],
        ["NGUOI_DUNG", "Cư dân, quản lý, kỹ thuật viên, quản trị", "Một người là một tài khoản"],
        ["cư trú (CuTru)", "Ai ở căn nào, từ ngày nào", "Mối kết hợp n – n có thuộc tính riêng"],
        ["VAI_TRO, QUYEN", "Nhóm người dùng và quyền theo phân hệ (mối kết hợp đóng vai, được cấp)",
         "Phân quyền, chọn thợ theo chuyên môn"],
        ["MUC_UU_TIEN", "Mức ưu tiên và thời hạn xử lý", "Đổi thời hạn không cần sửa chương trình"],
        ["YEU_CAU_BAO_TRI", "Yêu cầu sửa chữa khi có sự cố", "Đối tượng chính của quy trình sự cố"],
        ["ANH_YEU_CAU", "Ảnh lúc báo, ảnh nghiệm thu", "Một yêu cầu có nhiều ảnh"],
        ["LICH_SU_YEU_CAU", "Mỗi lần đổi trạng thái, giao việc", "Truy vết và tính KPI"],
        ["NHAT_KY_HE_THONG", "Vết thay đổi dữ liệu", "Kiểm toán"],
        ["LOAI_DICH_VU", "Bảng giá: phí quản lý, điện, nước, gửi xe, sửa chữa", "Mỗi khoản thu một đơn giá, đổi giá "
         "ở một nơi"],
        ["CHI_SO_DICH_VU", "Chỉ số đồng hồ hằng tháng của điện, nước", "Mỗi căn, mỗi loại, mỗi kỳ một dòng"],
        ["đăng ký (DangKyDichVu)", "Căn dùng dịch vụ cố định nào, bao nhiêu (gửi 2 xe máy)",
         "Mối kết hợp n – n giữa căn và loại dịch vụ, có số lượng"],
        ["HOA_DON", "Giấy báo phí của căn", "Chứng từ công nợ của căn"],
        ["gồm dịch vụ (ChiTietHoaDon)", "Các dòng của hoá đơn, mỗi dòng một dịch vụ",
         "Mối kết hợp n – n giữa hoá đơn và loại dịch vụ, có số lượng, đơn giá"],
        ["THANH_TOAN", "Lần nộp tiền", "Một hoá đơn nộp nhiều lần"],
    ], [3.4, 4.0, 4.0])
    p("Bảng kỹ thuật của ASP.NET Identity (claim, token, đăng nhập ngoài) không mang thông tin nghiệp vụ nên không "
      "đưa vào phân tích. Bảng nghiệp vụ dùng khoá số nguyên tự tăng; khoá tự nhiên (SoHoaDon, MaCanHo, MaToaNha…) vẫn "
      "khai báo duy nhất. NguoiDungId, VaiTroId là chuỗi do ASP.NET Identity sinh.")
    p("Phân hệ thu phí có sáu bảng, mỗi bảng do một người ghi vào một thời điểm và giữ một loại thông tin (Bảng 2.2). "
      "Tên và đơn giá của mọi khoản thu chỉ nằm ở LoaiDichVu; các bảng khác chỉ trỏ tới loại dịch vụ.")
    T("Bảng 2.2. Vai trò của các bảng thu phí", ["Bảng", "Ai ghi, khi nào", "Lưu gì", "Không lưu"], [
        ["LoaiDichVu", "Quản lý, khi thêm dịch vụ hoặc đổi giá", "Tên, đơn vị, đơn giá, cách tính của mỗi khoản thu",
         "–"],
        ["ChiSoDichVu", "Ban quản lý, hằng tháng", "Chỉ số cũ, chỉ số mới của điện, nước", "Đơn giá, thành tiền"],
        ["DangKyDichVu", "Quản lý, khi căn đăng ký hoặc huỷ", "Số lượng dịch vụ cố định căn dùng (số xe)",
         "Tên dịch vụ, số tiền"],
        ["HoaDon", "Hệ thống, vào ngày lập phí", "Kỳ, hạn thanh toán, tiền phí, phí trễ, số đã thu", "Từng khoản"],
        ["ChiTietHoaDon", "Hệ thống, cùng lúc lập hoá đơn", "Mỗi dịch vụ một dòng: số lượng, đơn giá lúc lập",
         "Tên dịch vụ, nguồn số liệu"],
        ["ThanhToan", "Cư dân gửi, quản lý duyệt hoặc thu trực tiếp", "Mỗi lần nộp tiền, hoàn tiền", "–"],
    ], [2.6, 3.2, 3.6, 2.0])
    p("Số lượng của mỗi dòng hoá đơn lấy theo cách tính của dịch vụ: diện tích căn với phí quản lý, chỉ số mới trừ "
      "chỉ số cũ với điện nước, số đăng ký với gửi xe, một lần với sửa chữa. Số lượng và đơn giá được chép vào dòng lúc "
      "lập, vì hoá đơn đã phát hành phải giữ nguyên khi bảng giá, diện tích hay đăng ký thay đổi về sau.")

    # ------------------------------------------------------------------ 2.2
    B.append(("h2", "2.2. Mô hình thực thể kết hợp (ERD)"))
    p("Hình chữ nhật là thực thể, thuộc tính khoá gạch chân, thuộc tính có dấu / đứng trước là thuộc tính suy diễn "
      "(tính từ thuộc tính khác). Hình thoi là mối kết hợp; thuộc tính của mối kết hợp ghi trong khung nối nét đứt. "
      "Cặp (min, max) cạnh thực thể cho biết mỗi thể hiện tham gia mối kết hợp ít nhất và nhiều nhất bao nhiêu lần. "
      "Để ERD vừa một hình, mỗi thực thể chỉ ghi khoá và các thuộc tính chính.")
    B.append(("fig", "Hình 2.1. ERD toàn hệ thống", IMG / (sd.erd_tong()["name"] + ".png")))
    p("Một số bản số cần lưu ý. Toà có ít nhất một tầng, yêu cầu có ít nhất một dòng lịch sử, hoá đơn gồm ít nhất một "
      "dịch vụ, người dùng có ít nhất một vai trò. Một yêu cầu xảy ra tại một căn hoặc ở khu vực chung của một toà, "
      "không đồng thời cả hai. Chi tiết hoá đơn là mối kết hợp “gồm dịch vụ” giữa HOA_DON và LOAI_DICH_VU: mỗi hoá "
      "đơn có mỗi dịch vụ nhiều nhất một dòng, với số lượng, đơn giá và thành tiền là thuộc tính của mối kết hợp. "
      "NHAT_KY_HE_THONG không nối với thực thể nào vì nhật ký phải giữ cả thao tác trên dòng đã bị xoá.")

    # ------------------------------------------------------------------ 2.3
    B.append(("h2", "2.3. Lược đồ quan hệ"))
    p("ERD được chuyển sang lược đồ quan hệ theo các quy tắc:")
    b("Mỗi thực thể thành một quan hệ; thuộc tính khoá thành khoá chính.")
    b("Mối kết hợp 1 – n: khoá chính phía một thành khoá ngoại ở phía nhiều; bắt buộc nếu bản số là (1,1), cho phép "
      "rỗng nếu là (0,1).")
    b("Mối kết hợp n – n thành quan hệ riêng gồm hai khoá ngoại và các thuộc tính của mối kết hợp. Hai khoá ngoại là "
      "khoá chính ghép (NguoiDung_VaiTro, VaiTro_Quyen, DangKyDichVu, ChiTietHoaDon); riêng CuTru có khoá riêng vì "
      "một người có thể ở cùng một căn nhiều lần.")
    b("Thuộc tính suy diễn thành cột do cơ sở dữ liệu tự tính (TongTien, ConNo, ThanhTien), không ai ghi tay.")
    p("Hình 2.2 vẽ lược đồ quan hệ theo ký pháp chân gà, cùng bố cục với ERD. Cột trái của mỗi bảng ghi PK (khoá "
      "chính), FK (khoá ngoại), U (duy nhất), N (cho phép rỗng). Chân gà nằm ở bảng chứa "
      "khoá ngoại, kèm vòng tròn nếu dòng cha có thể chưa có dòng con, kèm vạch nếu phải có ít nhất một. Hai khoá "
      "ngoại tới cùng một bảng được vẽ thành hai đường có ghi tên khoá. Mỗi bảng ghi đủ khoá và các cột chính, không ghi "
      "kiểu dữ liệu để hình đủ lớn. Kiểu dữ liệu chọn theo loại thông tin: tiền là numeric(18,0) vì đồng Việt Nam "
      "không có phần lẻ; diện tích, chỉ số, số lượng là numeric có hai chữ số lẻ; ngày là date, thời điểm là "
      "timestamp; trạng thái, loại là integer kèm CHECK; chuỗi là varchar có giới hạn độ dài.")
    B.append(("fig", "Hình 2.2. Lược đồ quan hệ toàn hệ thống", IMG / (sd.rd_tong()["name"] + ".png")))
    p("CuTru là liên kết thường, không phải hợp thành: căn còn người cư trú thì không xoá được, xoá căn không xoá "
      "người. Khi xoá dòng cha, khoá ngoại tới bộ phận của nó (ảnh, lịch sử, dòng hoá đơn, đăng ký dịch vụ) xoá theo; "
      "khoá tuỳ chọn tới người dùng chuyển về rỗng; các khoá còn lại chặn xoá, như loại dịch vụ đã có trên hoá đơn. "
      "Ứng dụng chỉ xoá mềm (cột DaXoa), nên đây là lớp bảo vệ khi sửa trực tiếp trong cơ sở dữ liệu. Mỗi bảng nghiệp "
      "vụ có thêm bảy cột kiểm toán (NgayTao, NguoiTao, NgayCapNhat, NguoiCapNhat, DaXoa, NgayXoa, NguoiXoa) không vẽ "
      "trên hình.")

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
    b("Thu phí: tên và đơn giá của mọi khoản thu chỉ ở LoaiDichVu; DangKyDichVu chỉ lưu số lượng, ChiSoDichVu chỉ lưu "
      "số đồng hồ. ChiSoCu thường bằng chỉ số mới của kỳ trước nhưng vẫn lưu, vì đồng hồ có thể được thay giữa hai kỳ. "
      "TongTien, ConNo, ThanhTien do cơ sở dữ liệu tự tính. Số lượng, đơn giá trên ChiTietHoaDon và TienPhi, DaThu "
      "trên HoaDon được chốt trên chứng từ vì hoá đơn cũ phải giữ đúng số tiền lúc lập; HanXuLy của sự cố cũng là hạn "
      "đã hứa lúc báo.")

    # ------------------------------------------------------------------ 2.5
    B.extend(rbtv_blocks())

    # ------------------------------------------------------------------ 2.6
    B.append(("h2", "2.6. Kết quả"))
    p(f"Thiết kế dữ liệu gồm {n_bang} bảng, thể hiện trên một ERD và một lược đồ quan hệ; mọi quan hệ đạt BCNF. Các "
      "KPI ở Bảng 1.10 tính từ đúng một nguồn: mốc thời gian sự cố từ LichSuYeuCau, căn có người ở từ CuTru, số tiền "
      "từ HoaDon. Mọi khoản thu dùng chung một bảng giá và mỗi dòng hoá đơn là một loại dịch vụ. Bảng 2.7 đối chiếu "
      "thiết kế với năm tính chất của cơ sở dữ liệu.")
    T("Bảng 2.7. Thiết kế đáp ứng các tính chất của cơ sở dữ liệu", ["Tính chất", "Cách bảo đảm", "Mục"], [
        ["Tính cấu trúc", "Bảng có kiểu dữ liệu, khoá và ràng buộc khai báo trong lược đồ; đi từ ERD sang lược đồ "
         "quan hệ", "2.2, 2.3"],
        ["Tính toàn vẹn", "PK, FK, UNIQUE, CHECK cho ràng buộc trong một bảng; ba trigger cho ràng buộc cần dữ liệu "
         "nhiều dòng", "2.5"],
        ["Tính nhất quán", "Thay đổi liên quan lưu trong một giao dịch; tổng tiền, còn nợ, thành tiền do cơ sở dữ "
         "liệu tự tính; khoá lạc quan chặn hai người ghi đè nhau", "2.4, 2.5"],
        ["Tính an toàn và bảo mật", "Phân quyền theo vai trò và quyền; mật khẩu chỉ lưu dạng băm; nhật ký ghi mọi "
         "thay đổi; xoá mềm", "2.3"],
        ["Tính độc lập", "Ứng dụng truy cập dữ liệu qua lớp ánh xạ; đổi cấu trúc bảng chỉ cần sửa lớp ánh xạ, không "
         "sửa mã nghiệp vụ", "2.3"],
        ["Tính không dư thừa", "Mọi quan hệ đạt BCNF; mỗi thông tin chỉ lưu một nơi, đơn giá chỉ ở bảng giá; chỉ chép "
         "số chốt lên chứng từ", "2.1, 2.4"],
    ], [2.8, 6.6, 2.0])
    return B


# ====================================================================== ràng buộc toàn vẹn
# Ràng buộc trên một bảng (miền giá trị và liên thuộc tính): (bối cảnh, nội dung, biểu diễn, tên CHECK)
# Ràng buộc trên một bảng (miền giá trị và liên thuộc tính): (bối cảnh, nội dung, tên CHECK)
MOT_BANG = [
    ("CanHo", "Diện tích lớn hơn 0; số phòng ngủ không âm; tình trạng là đã bàn giao, chưa bàn giao hoặc đang sửa "
     "chữa.", "CK_CanHo_DienTich, CK_CanHo_SoPhongNgu, CK_CanHo_TinhTrangCanHo"),
    ("ToaNha", "Ngày lập phí từ 1 đến 28; số ngày ân hạn không âm; tỉ lệ phí trễ từ 0 đến 100%; trạng thái là đang "
     "vận hành, đang bảo trì hoặc đã đóng.", "CK_ToaNha_NgayLapPhi, CK_ToaNha_PhiTre, CK_ToaNha_TrangThai"),
    ("CuTru", "Vai trò trong hộ là chủ hộ, người thuê hoặc thành viên; ngày chuyển đi (nếu có) không trước ngày vào ở.",
     "CK_CuTru_VaiTroTrongHo, CK_CuTru_NgayChuyenDi"),
    ("MucUuTien", "Có bốn mức: thấp, trung bình, cao, khẩn cấp; thời hạn phản hồi lớn hơn 0 và không vượt thời hạn "
     "hoàn tất.", "CK_MucUuTien_Id, CK_MucUuTien_ThoiHan"),
    ("YeuCauBaoTri", "Trạng thái, loại sự cố thuộc danh sách của hệ thống; điểm đánh giá (nếu có) từ 1 đến 5; chi phí "
     "không âm; hạn xử lý không trước ngày báo.", "CK_YeuCauBaoTri_TrangThai, _LoaiSuCo, _DanhGia, _ChiPhi, _HanXuLy"),
    ("YeuCauBaoTri", "Sự cố xảy ra tại một căn hoặc ở khu vực chung của một toà, không đồng thời cả hai; sự cố khu vực "
     "chung phải ghi vị trí.", "CK_YeuCauBaoTri_NoiXayRa"),
    ("YeuCauBaoTri", "Đã xử lý hoặc đã đóng thì phải có kết quả xử lý.", "CK_YeuCauBaoTri_KetQua"),
    ("AnhYeuCau", "Ảnh là ảnh lúc báo hoặc ảnh nghiệm thu.", "CK_AnhYeuCau_GiaiDoan"),
    ("LoaiDichVu, DangKyDichVu", "Đơn giá không âm; cách tính là theo diện tích, theo chỉ số, theo đăng ký hoặc theo "
     "lần; số lượng đăng ký lớn hơn 0.", "CK_LoaiDichVu_DonGia, CK_LoaiDichVu_CachTinh, CK_DangKyDichVu_SoLuong"),
    ("ChiSoDichVu", "Tháng của kỳ từ 1 đến 12; chỉ số cũ không âm, chỉ số mới không nhỏ hơn chỉ số cũ.",
     "CK_ChiSoDichVu_Ky, CK_ChiSoDichVu_ChiSo"),
    ("HoaDon", "Tháng của kỳ từ 1 đến 12; tiền phí, phí trễ hạn, đã thu không âm; đã thu không vượt tổng tiền; hạn "
     "thanh toán không trước ngày lập; trạng thái thuộc danh sách của hệ thống.",
     "CK_HoaDon_TrangThai, _Ky, _SoTien, _DaThu, _HanThanhToan"),
    ("ChiTietHoaDon", "Số lượng lớn hơn 0; đơn giá không âm.", "CK_ChiTietHoaDon_SoTien"),
    ("ThanhToan", "Số tiền lớn hơn 0; phương thức, trạng thái thuộc danh sách của hệ thống; số hoàn trả không âm và "
     "không vượt số tiền.", "CK_ThanhToan_SoTien, _PhuongThuc, _TrangThai, _HoanTra"),
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
     "người phụ trách.", "∀y → y': (y.TrangThaiYeuCau, y'.TrangThaiYeuCau) ∈ Chuyển ∧ (y'.TrangThaiYeuCau = Đang xử lý ⇒ "
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
     "p.TrangThaiThanhToan ∈ {Thành công, Đã hoàn, Hoàn một phần}) ≤ h.TongTien", "Trigger TG3 và ứng dụng",
     [("ThanhToan", "+", "–", "+ (SoTien, SoTienHoanTra, TrangThaiThanhToan)"), ("HoaDon", "–", "–", "+ (TienPhi)")]),
    ("LB07", "HoaDon, ThanhToan, ChiTietHoaDon", "Đã thu bằng tổng các khoản thành công trừ phần hoàn; tiền phí bằng "
     "tổng thành tiền các dòng.", "∀h: h.DaThu = Σ (p.SoTien − p.SoTienHoanTra | p thành công của h) ∧ "
     "h.TienPhi = Σ (d.ThanhTien | d ∈ ChiTietHoaDon của h)", "Ứng dụng trong cùng giao dịch; khung nhìn đối chiếu",
     [("HoaDon", "–", "–", "+ (TienPhi, DaThu)"), ("ThanhToan", "+", "+", "+"), ("ChiTietHoaDon", "+", "+", "+")]),
    ("LB08", "ChiSoDichVu, HoaDon", "Căn đã có hoá đơn phí tháng chưa huỷ của một kỳ thì chỉ số kỳ đó không được "
     "sửa hay xoá.", "∀s ∈ ChiSoDichVu: (∃h ∈ HoaDon: h.CanHoId = s.CanHoId ∧ h.Ky = s.Ky ∧ h.LoaiHoaDon = phí tháng "
     "∧ h.TrangThaiHoaDon ≠ Đã huỷ) ⇒ s không đổi", "Trigger TG2",
     [("ChiSoDichVu", "–", "+", "+"), ("HoaDon", "–", "–", "–")]),
    ("LB09", "ChiSoDichVu", "Chỉ số cũ của một kỳ bằng chỉ số mới của kỳ liền trước, trừ khi đồng hồ được thay.",
     "∀s, s' cùng căn, cùng loại, s'.Ky là kỳ liền trước của s.Ky: s.ChiSoCu = s'.ChiSoMoi (trừ khi thay đồng hồ)",
     "Ứng dụng điền sẵn, người ghi sửa khi thay đồng hồ", [("ChiSoDichVu", "+", "+", "+ (ChiSoCu, ChiSoMoi)")]),
    ("LB10", "ChiTietHoaDon, HoaDon, ChiSoDichVu, DangKyDichVu, CanHo", "Trên hoá đơn phí tháng, số lượng của mỗi "
     "dòng đúng theo cách tính của dịch vụ lúc lập: diện tích căn, lượng tiêu thụ của chỉ số cùng căn và cùng kỳ, "
     "hoặc số lượng căn đăng ký.", "∀d ∈ ChiTietHoaDon, h là hoá đơn phí tháng của d: d.SoLuong = diện tích căn (theo "
     "diện tích); = s.ChiSoMoi − s.ChiSoCu với s cùng căn, kỳ, loại (theo chỉ số); = k.SoLuong với k ∈ DangKyDichVu "
     "của căn (theo đăng ký)", "Ứng dụng khi lập hoá đơn",
     [("ChiTietHoaDon", "+", "–", "+ (SoLuong, LoaiDichVuId)"), ("ChiSoDichVu", "–", "–", "– (*)"),
      ("DangKyDichVu", "–", "–", "– (*)")]),
    ("LB11", "HoaDon, CanHo", "Căn chưa bàn giao không được lập hoá đơn phí tháng.",
     "∀h, h.LoaiHoaDon = phí tháng: căn của h có TinhTrangCanHo ≠ chưa bàn giao lúc lập", "Ứng dụng",
     [("HoaDon", "+", "–", "–"), ("CanHo", "–", "–", "– (*)")]),
    ("LB12", "HoaDon, ThanhToan", "Hoá đơn đã có thanh toán thành công phải hoàn tiền trước khi huỷ; huỷ hoá đơn thì "
     "các thanh toán đang chờ bị huỷ theo.", "∀h: h.TrangThaiHoaDon = Đã huỷ ⇒ ∄p ∈ ThanhToan của h có trạng thái Chờ xác nhận hoặc Thành công",
     "Ứng dụng, trong một giao dịch", [("HoaDon", "–", "–", "+ (TrangThaiHoaDon)"), ("ThanhToan", "+", "–", "+")]),
    ("LB13", "CuTru, CanHo", "Mỗi căn có người đang ở có đúng một người đại diện; căn chưa bàn giao thì không có người "
     "đang ở.", "∀căn a có người ở: |{c ∈ CuTru | c.CanHoId = a ∧ c.LaDaiDien ∧ c.NgayChuyenDi = NULL}| = 1 ∧ "
     "a.TinhTrangCanHo ≠ chưa bàn giao", "Chỉ mục duy nhất có điều kiện (tối đa một) và ứng dụng",
     [("CuTru", "+", "+", "+ (LaDaiDien, NgayChuyenDi)"), ("CanHo", "–", "–", "+ (TinhTrangCanHo)")]),
    ("LB14", "CuTru, NguoiDung", "Cư trú chỉ hiện trên cổng cư dân khi người đó đã xác nhận qua thư mời.",
     "Căn hiện trên cổng của u ⇔ ∃c ∈ CuTru: c.NguoiDungId = u ∧ c.NgayXacNhan ≠ NULL ∧ c.NgayChuyenDi = NULL",
     "Ứng dụng, mã mời ký số hạn 14 ngày", [("CuTru", "+", "–", "+ (NgayXacNhan)")]),
    ("LB15", "NguoiDung, NguoiDung_VaiTro", "Mỗi tài khoản có ít nhất một vai trò.",
     "∀u ∈ NguoiDung: ∃v ∈ NguoiDung_VaiTro: v.NguoiDungId = u.NguoiDungId", "Ứng dụng",
     [("NguoiDung", "+", "–", "–"), ("NguoiDung_VaiTro", "–", "+", "+")]),
    ("LB16", "ChiSoDichVu, DangKyDichVu, LoaiDichVu", "Chỉ ghi chỉ số cho dịch vụ tính theo chỉ số; căn chỉ đăng ký "
     "dịch vụ tính theo đăng ký.", "∀s ∈ ChiSoDichVu: loại của s có CachTinh = theo chỉ số ∧ ∀k ∈ DangKyDichVu: loại "
     "của k có CachTinh = theo đăng ký", "Ứng dụng",
     [("ChiSoDichVu", "+", "–", "+ (LoaiDichVuId)"), ("DangKyDichVu", "+", "–", "–"),
      ("LoaiDichVu", "–", "–", "+ (CachTinh)")]),
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
    p("Mỗi ràng buộc ghi bối cảnh (các quan hệ liên quan), nội dung và cách cài đặt; ràng buộc liên bộ, liên quan hệ "
      "ghi thêm biểu diễn hình thức. Khoá chính, khoá ngoại và khoá duy nhất đã thể hiện trên lược đồ quan hệ (PK, FK, "
      "U) nên không liệt kê lại.")
    B.append(("h3", "2.5.1. Ràng buộc trên một bảng"))
    p("Ràng buộc miền giá trị và liên thuộc tính chỉ xét các cột của cùng một dòng, nên đều khai báo bằng CHECK trên "
      "bảng đó (Bảng 2.3); chỉ thao tác Thêm và Sửa trên chính bảng đó mới có thể vi phạm.")
    T("Bảng 2.3. Ràng buộc trên một bảng", ["Mã", "Bối cảnh", "Nội dung"],
      [[f"RB{i:02d}", t, nd] for i, (t, nd, ck) in enumerate(MOT_BANG, 1)],
      [1.1, 2.6, 7.8])
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
           "Căn đã có hoá đơn phí tháng chưa huỷ của kỳ thì chỉ số kỳ đó không sửa, không xoá", "Huỷ lệnh, báo lỗi",
           "LB08"],
          ["TG3", "Trigger AFTER INSERT, UPDATE trên ThanhToan",
           "Tổng thu thành công trừ phần hoàn của hoá đơn ≤ TongTien", "Huỷ cả giao dịch, báo lỗi", "LB06"],
          ["FN1", "Hàm fn_han_xu_ly(ngày báo, mức)", "Trả về ngày báo + PhutHoanTat của mức trong MucUuTien",
           "Không có", "LB05"],
      ], [1.1, 3.2, 3.5, 2.2, 1.3])
    return B
