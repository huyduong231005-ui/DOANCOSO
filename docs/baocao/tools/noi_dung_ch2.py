"""Nội dung Chương 2 (phân tích và thiết kế dữ liệu) sau góp ý tuần 6. Các bảng từ điển dữ liệu, khoá, ràng buộc
được sinh từ lược đồ thật (luoc_do.json, đọc từ PostgreSQL) nên luôn khớp CSDL. Trả về danh sách khối để
sua_bao_cao_v2.py dựng vào file Word.

Khối: ("h2", text) ("h3", text) ("p", text) ("rd", bảng) ("bullet", text) ("table", caption, header, rows, widths)
      ("fig", caption, đường dẫn ảnh)
"""
import json
from pathlib import Path

import so_do_v2 as sd

HERE = Path(__file__).resolve().parent
IMG = HERE.parent / "images_v2"
S = json.loads((HERE / "luoc_do.json").read_text(encoding="utf8"))

TABLE_ORDER = ["KhuVuc", "DuAn", "ToaNha", "Tang", "CanHo", "NguoiDung", "CuTru", "VaiTro", "Quyen", "MucUuTien",
               "YeuCauBaoTri", "AnhYeuCau", "LichSuYeuCau", "NhatKyHeThong", "LoaiDichVu", "ChiSoDichVu",
               "PhiDinhKy", "HoaDon", "ChiTietHoaDon", "ThanhToan"]

# Mô tả từng cột (khoá "Bảng.Cột"), đủ cho mọi cột của lược đồ hiện tại.
DESC = {k: (v[0] if isinstance(v, list) else v)
        for k, v in json.loads((HERE / "mo_ta_cot.json").read_text(encoding="utf8")).items()}

ENT_TABLE = {"KHU_VUC": "KhuVuc", "DU_AN": "DuAn", "TOA_NHA": "ToaNha", "TANG": "Tang", "CAN_HO": "CanHo",
             "NGUOI_DUNG": "NguoiDung", "VAI_TRO": "VaiTro", "QUYEN": "Quyen", "MUC_UU_TIEN": "MucUuTien",
             "YEU_CAU_BAO_TRI": "YeuCauBaoTri", "ANH_YEU_CAU": "AnhYeuCau",
             "LICH_SU_YEU_CAU": "LichSuYeuCau", "NHAT_KY_HE_THONG": "NhatKyHeThong",
             "LOAI_DICH_VU": "LoaiDichVu", "CHI_SO_DICH_VU": "ChiSoDichVu", "PHI_DINH_KY": "PhiDinhKy",
             "HOA_DON": "HoaDon", "CHI_TIET_HOA_DON": "ChiTietHoaDon", "THANH_TOAN": "ThanhToan"}

# Diễn giải từng mối kết hợp (khoá = tên trên hình thoi).
REL_TEXT = {
    "có dự án": "Một khu vực có thể chưa có hoặc có nhiều dự án; mỗi dự án thuộc đúng một khu vực.",
    "gồm toà": "Một dự án có thể chưa có hoặc có nhiều toà; mỗi toà thuộc đúng một dự án.",
    "có tầng": "Một toà có ít nhất một tầng; mỗi tầng thuộc đúng một toà.",
    "có căn": "Một tầng có thể chưa có hoặc có nhiều căn; mỗi căn nằm trên đúng một tầng.",
    "quản lý toà": "Một người dùng có thể quản lý nhiều toà; một toà có tối đa một người quản lý.",
    "cư trú": "Một người có thể ở nhiều căn theo thời gian; một căn có thể chưa có hoặc có nhiều người ở. "
              "Mối kết hợp mang các thuộc tính vai trò trong hộ, người đại diện, ngày vào ở, ngày chuyển đi, "
              "ngày xác nhận.",
    "đóng vai": "Mỗi người dùng giữ ít nhất một vai trò; một vai trò có thể chưa có hoặc có nhiều người giữ.",
    "được cấp": "Một vai trò được cấp nhiều quyền; một quyền có thể thuộc nhiều vai trò.",
    "xảy ra tại căn": "Một căn có thể có nhiều yêu cầu; mỗi yêu cầu xảy ra tại tối đa một căn.",
    "ở khu vực chung": "Một toà có thể có nhiều sự cố ở khu vực chung; mỗi yêu cầu thuộc khu vực chung của tối đa một "
                       "toà. Mỗi yêu cầu thuộc đúng một trong hai mối kết hợp này.",
    "có mức ưu tiên": "Mỗi yêu cầu có đúng một mức ưu tiên; một mức áp dụng cho nhiều yêu cầu.",
    "báo sự cố": "Mỗi yêu cầu có đúng một người báo; một người có thể báo nhiều yêu cầu.",
    "phụ trách": "Mỗi yêu cầu có tối đa một người phụ trách (chưa giao thì chưa có); một kỹ thuật viên phụ trách "
                 "nhiều yêu cầu.",
    "có ảnh": "Một yêu cầu có thể có nhiều ảnh; mỗi ảnh thuộc đúng một yêu cầu.",
    "tải ảnh": "Một người tải nhiều ảnh; ảnh có thể không ghi người tải.",
    "có lịch sử": "Mỗi yêu cầu có ít nhất một dòng lịch sử (dòng tạo yêu cầu); mỗi dòng thuộc đúng một yêu cầu.",
    "thực hiện": "Một người thực hiện nhiều lần chuyển trạng thái; dòng do hệ thống tự làm thì không có người.",
    "thuộc loại": "Mỗi dòng chỉ số thuộc đúng một loại dịch vụ; một loại có nhiều dòng chỉ số.",
    "ghi chỉ số": "Một căn có nhiều dòng chỉ số theo kỳ; mỗi dòng thuộc đúng một căn.",
    "áp phí": "Một căn có thể có nhiều khoản phí định kỳ; mỗi khoản thuộc đúng một căn.",
    "được lập": "Một căn có nhiều hoá đơn; mỗi hoá đơn thuộc đúng một căn.",
    "gồm khoản": "Mỗi hoá đơn có ít nhất một dòng chi tiết; mỗi dòng thuộc đúng một hoá đơn.",
    "lập từ chỉ số": "Dòng điện nước được lập từ đúng một dòng chỉ số; một chỉ số có thể nằm ở dòng của hoá đơn "
                     "đã huỷ và hoá đơn lập lại.",
    "lập từ phí": "Dòng phí định kỳ được lập từ một khoản phí; một khoản xuất hiện ở hoá đơn của nhiều kỳ.",
    "tính phí sửa chữa": "Dòng sửa chữa tính cho đúng một sự cố; một sự cố có thể được tính ở nhiều dòng.",
    "được thanh toán": "Một hoá đơn có thể chưa có hoặc có nhiều lần thanh toán; mỗi thanh toán thuộc đúng một hoá đơn.",
}


def ent_name(key):
    return key


def erd_relations(spec):
    """[(A, tên, B, (min,max) A, (min,max) B)] theo thứ tự hình thoi trên sơ đồ."""
    nodes = {n["key"]: n for n in spec["nodes"]}
    ends = {}
    for e in spec["edges"]:
        if e["kind"] == "assoc":
            ends.setdefault(e["to"], []).append((e["from"], e.get("mult1", "")))
    out = []
    for n in spec["nodes"]:
        if n["kind"] == "nary":
            (a, ca), (b, cb) = ends[n["key"]]
            out.append((a, n["name"], b, ca, cb))
    return out


def cols(t):
    return S[t]["columns"]


def constraint_text(c, t):
    k = []
    if c["pk"]:
        k.append("PK")
    if c["fk"]:
        k.append(f"FK → {c['fk']}")
    if c["unique"]:
        k.append("UNIQUE")
    k.append("NULL" if c["null"] else "NOT NULL")
    return "; ".join(k)


def rd_line(t):
    """Lược đồ dạng chữ: trả về danh sách (đoạn chữ, gạch chân, in nghiêng)."""
    cs = cols(t)
    order = [c for c in cs if c["pk"]] + [c for c in cs if c["fk"] and not c["pk"]] + \
            [c for c in cs if not c["pk"] and not c["fk"]]
    parts = [(t + " (", False, False)]
    for i, c in enumerate(order):
        parts.append((c["name"], c["pk"], bool(c["fk"])))
        parts.append((", " if i < len(order) - 1 else ")", False, False))
    return parts


def candidate_keys(t):
    keys = [" , ".join(c["name"] for c in cols(t) if c["pk"])]
    for u in S[t]["unique"]:
        name = ", ".join(x for x in u["cols"])
        if name in ("NormalizedUserName", "NormalizedName"):
            continue
        keys.append(("(" + name + ")" if len(u["cols"]) > 1 else name) + (" *" if u["where"] else ""))
    if t == "NguoiDung":
        keys.append("TenDangNhap")
    return keys


def blocks():
    B = []
    p = lambda s: B.append(("p", s))
    b = lambda s: B.append(("bullet", s))
    T = lambda cap, head, rows, w: B.append(("table", cap, head, rows, w))

    p("Chương này thiết kế cơ sở dữ liệu theo trình tự: xác định thực thể, vẽ ERD, chuyển sang lược đồ quan hệ, "
      "chuẩn hoá, đặt ràng buộc toàn vẹn và lập từ điển dữ liệu. Mọi bảng, cột và ràng buộc trong chương lấy từ cơ "
      "sở dữ liệu PostgreSQL đang chạy của hệ thống.")

    # ------------------------------------------------------------------ 2.1
    B.append(("h2", "2.1. Xác định thực thể từ nghiệp vụ"))
    B.append(("h3", "2.1.1. Danh sách thực thể"))
    p("Từ quy trình TO-BE và các chứng từ ở Bảng 1.4, nhóm xác định 19 thực thể và 3 mối kết hợp nhiều – nhiều "
      "(Bảng 2.1). Ba mối kết hợp này thành ba bảng CuTru, NguoiDung_VaiTro, VaiTro_Quyen, nên cơ sở dữ liệu có 22 "
      "bảng. Tên thực thể trên ERD trùng tên bảng, viết hoa và nối bằng gạch dưới (KHU_VUC là bảng KhuVuc).")
    ent_rows = [
        ["KHU_VUC", "Địa bàn (tỉnh, thành)", "Một địa bàn có nhiều dự án", "KhuVucId"],
        ["DU_AN", "Khu căn hộ gồm nhiều toà", "Địa chỉ, mô tả chung của các toà", "DuAnId"],
        ["TOA_NHA", "Toà nhà và chính sách thu phí", "Có quản lý riêng, có sự cố khu vực chung", "ToaNhaId"],
        ["TANG", "Tầng của toà", "Toà có nhiều tầng, căn nằm trên tầng", "TangId"],
        ["CAN_HO", "Căn hộ", "Nơi ở và nơi tính phí", "CanHoId"],
        ["NGUOI_DUNG", "Cư dân, quản lý, kỹ thuật viên, quản trị", "Một người là một tài khoản", "NguoiDungId"],
        ["cư trú (CuTru)", "Ai ở căn nào, từ ngày nào", "Mối kết hợp n – n có thuộc tính riêng", "CuTruId"],
        ["VAI_TRO", "Nhóm người dùng, chuyên môn của thợ", "Phân quyền và chọn thợ", "VaiTroId"],
        ["QUYEN", "Quyền theo phân hệ", "Cấp quyền chi tiết hơn vai trò", "QuyenId"],
        ["MUC_UU_TIEN", "Mức ưu tiên và thời hạn SLA", "Đổi thời hạn không cần sửa chương trình", "MucUuTienId"],
        ["YEU_CAU_BAO_TRI", "Yêu cầu sửa chữa khi có sự cố", "Đối tượng chính của quy trình sự cố", "YeuCauBaoTriId"],
        ["ANH_YEU_CAU", "Ảnh hiện trạng, ảnh nghiệm thu", "Một yêu cầu có nhiều ảnh", "AnhYeuCauId"],
        ["LICH_SU_YEU_CAU", "Mỗi lần đổi trạng thái, giao việc", "Truy vết và tính KPI", "LichSuYeuCauId"],
        ["NHAT_KY_HE_THONG", "Vết thay đổi dữ liệu", "Kiểm toán", "NhatKyId"],
        ["LOAI_DICH_VU", "Điện, nước và đơn giá", "Đơn giá dùng chung", "LoaiDichVuId"],
        ["CHI_SO_DICH_VU", "Chỉ số đồng hồ hằng tháng", "Mỗi căn, mỗi loại, mỗi kỳ một dòng", "ChiSoDichVuId"],
        ["PHI_DINH_KY", "Phí cố định của căn (gửi xe…)", "Mỗi căn có số khoản khác nhau", "PhiDinhKyId"],
        ["HOA_DON", "Giấy báo phí, hoá đơn phát sinh", "Chứng từ công nợ của căn", "HoaDonId"],
        ["CHI_TIET_HOA_DON", "Các khoản trên hoá đơn", "Một hoá đơn có nhiều khoản", "ChiTietHoaDonId"],
        ["THANH_TOAN", "Lần nộp tiền", "Một hoá đơn nộp nhiều lần", "ThanhToanId"],
    ]
    T("Bảng 2.1. Thực thể và lý do cần bảng riêng",
      ["Thực thể", "Nghiệp vụ", "Lý do cần bảng riêng", "Khoá chính"], ent_rows, [2.6, 3.2, 3.4, 2.0])
    p("Các bảng kỹ thuật của ASP.NET Identity (claim, token, đăng nhập ngoài) và các cột kỹ thuật của Identity trong "
      "NguoiDung không mang thông tin nghiệp vụ nên không đưa vào phân tích.")

    B.append(("h3", "2.1.2. Quy ước đặt tên thuộc tính và chọn khoá"))
    p("Nhóm đặt tên thuộc tính theo Bảng 2.2 để mỗi tên chỉ thuộc một bảng. Ngoài khoá ngoại (cố ý trùng tên khoá "
      "chính được tham chiếu) và bảy cột kiểm toán dùng chung, không có hai bảng nào có cột trùng tên.")
    T("Bảng 2.2. Quy ước đặt tên thuộc tính", ["Loại thuộc tính", "Quy ước", "Ví dụ"], [
        ["Khoá chính", "Tên bảng + Id", "KhuVucId, NguoiDungId, MucUuTienId"],
        ["Khoá ngoại", "Trùng tên khoá chính được tham chiếu", "CanHoId trong CuTru, HoaDonId trong ThanhToan"],
        ["Nhiều khoá ngoại tới một bảng", "Đặt theo vai trò", "NguoiBaoCaoId, NguoiPhuTrachId, QuanLyId"],
        ["Thuộc tính mô tả", "Nội dung + tên đối tượng", "TenKhuVuc, MoTaCanHo, TrangThaiHoaDon"],
        ["Mốc thời gian", "Ngay, ThoiDiem, ThoiGian + sự kiện", "NgayBaoCao, NgayVaoO, ThoiDiemChuyen"],
        ["Cột kiểm toán", "Bảy cột giống nhau, mô tả ở mục 2.6",
         "NgayTao, NguoiTao, NgayCapNhat, NguoiCapNhat, DaXoa, NgayXoa, NguoiXoa"],
    ], [3.2, 3.8, 4.2])
    p("Cách chọn khoá:")
    b("Thực thể nghiệp vụ dùng khoá số nguyên tự tăng, không đổi khi dữ liệu nghiệp vụ đổi.")
    b("Khoá tự nhiên vẫn khai báo duy nhất: SoYeuCau, SoHoaDon, SoThanhToan, MaToaNha, MaCanHo, MaQuyen, "
      "MaLoaiDichVu, (ToaNhaId, SoTang), (CanHoId, LoaiDichVuId, KyChiSo).")
    b("NguoiDungId, VaiTroId là chuỗi do ASP.NET Identity sinh.")
    b("MucUuTienId là khoá tự nhiên 0–3 (Thấp đến Khẩn cấp), cũng là thứ tự ưu tiên.")
    b("CuTru dùng khoá CuTruId vì một người có thể rời đi rồi quay lại cùng một căn.")

    # ------------------------------------------------------------------ 2.2
    B.append(("h2", "2.2. Mô hình thực thể kết hợp (ERD)"))
    B.append(("h3", "2.2.1. Ký hiệu sử dụng"))
    p("ERD vẽ ở mức khái niệm theo ký pháp thực thể – mối kết hợp. Hình chữ nhật là thực thể, thuộc tính khoá gạch "
      "chân. Hình thoi là mối kết hợp, đặt tên bằng động từ; thuộc tính của mối kết hợp ghi trong khung chú thích "
      "nối nét đứt vào hình thoi. Cặp (min, max) cạnh thực thể cho biết mỗi thể hiện của nó tham gia mối kết hợp ít "
      "nhất và nhiều nhất bao nhiêu lần: (0,1) không có hoặc một, (1,1) đúng một, (0,n) không có hoặc nhiều, (1,n) "
      "ít nhất một. ERD không vẽ khoá ngoại.")
    B.append(("h3", "2.2.2. Sơ đồ ERD và các mối kết hợp"))
    p("ERD chia thành bốn hình theo nhóm nghiệp vụ, mỗi hình trên một trang ngang, kèm bảng các mối kết hợp. Thực "
      "thể đã mô tả ở hình khác chỉ ghi khoá chính.")
    erd_specs = [sd.erd_co_cau(), sd.erd_tai_khoan(), sd.erd_su_co(), sd.erd_thu_phi()]
    erd_caps = ["ERD cơ cấu chung cư và cư trú", "ERD tài khoản và phân quyền", "ERD sự cố và bảo trì",
                "ERD phân hệ thu phí"]
    for i, (spec, cap) in enumerate(zip(erd_specs, erd_caps)):
        B.append(("fig", f"Hình 2.{i + 1}. {cap}", IMG / (spec["name"] + ".png")))
        rows = [[a, name, bb, ca, cb, REL_TEXT[name]] for a, name, bb, ca, cb in erd_relations(spec)]
        T(f"Bảng 2.{5 + i}. Mối kết hợp trong {cap}",
          ["Thực thể A", "Mối kết hợp", "Thực thể B", "A", "B", "Diễn giải"], rows, [2.3, 1.9, 2.3, 1.0, 1.0, 4.4])
        if i == 1:
            p("NHAT_KY_HE_THONG không có khoá ngoại vì nhật ký phải giữ cả thao tác trên dòng và người đã bị xoá.")
    p("Các điều kiện bản số sau không biểu diễn được bằng khoá ngoại, được đảm bảo ở ứng dụng (mục 2.5.6): toà có "
      "ít nhất một tầng, yêu cầu có ít nhất một dòng lịch sử, hoá đơn có ít nhất một dòng, người dùng có ít nhất một "
      "vai trò. Hai mối kết hợp “xảy ra tại căn” và “ở khu vực chung” loại trừ nhau, được kiểm tra bằng CHECK.")

    # ------------------------------------------------------------------ 2.3
    B.append(("h2", "2.3. Chuyển ERD sang lược đồ quan hệ (RD)"))
    B.append(("h3", "2.3.1. Quy tắc chuyển đổi"))
    b("Mỗi thực thể thành một quan hệ; thuộc tính khoá thành khoá chính.")
    b("Mối kết hợp 1 – n: khoá chính phía (0,n)/(1,n) thành khoá ngoại ở phía (x,1); NOT NULL nếu là (1,1), cho phép "
      "NULL nếu là (0,1).")
    b("Mối kết hợp n – n thành quan hệ riêng chứa hai khoá ngoại, thuộc tính của mối kết hợp thành cột của quan hệ "
      "đó: NguoiDung_VaiTro, VaiTro_Quyen (khoá ghép) và CuTru (khoá CuTruId).")
    b("Hai mối kết hợp loại trừ nhau (sự cố tại căn hoặc ở khu vực chung): hai khoá ngoại cùng cho phép NULL, kèm "
      "CHECK buộc đúng một khoá có giá trị.")
    b("Thuộc tính đa trị (danh sách ảnh) tách thành quan hệ riêng.")
    B.append(("h3", "2.3.2. Các lược đồ quan hệ"))
    p("Thuộc tính gạch dưới là khoá chính, in nghiêng là khoá ngoại. Bảy cột kiểm toán không ghi lại ở đây.")
    for t in TABLE_ORDER[:6] + ["CuTru", "VaiTro", "NguoiDung_VaiTro", "Quyen", "VaiTro_Quyen"] + TABLE_ORDER[9:]:
        if t == "CuTru" and ("rd", "CuTru") in B:
            continue
        B.append(("rd", t))
    B.append(("h3", "2.3.3. Sơ đồ lược đồ quan hệ"))
    p("Hình 2.5 đến Hình 2.8 vẽ lược đồ quan hệ của 22 bảng theo bốn nhóm như ERD, dùng ký pháp chân gà. Cột đầu "
      "của mỗi bảng ghi PK (khoá chính), FK (khoá ngoại), U (duy nhất), N (cho phép NULL); cột cuối là kiểu dữ liệu. "
      "Chân gà nằm ở bảng chứa khoá ngoại, kèm vòng tròn nếu dòng cha có thể chưa có dòng con, kèm vạch nếu phải có ít "
      "nhất một (đầu (1,n) trên ERD). Đầu phía bảng cha có hai vạch nếu khoá ngoại bắt buộc, vòng tròn và vạch nếu cho "
      "phép NULL. Hai khoá ngoại tới cùng một bảng được vẽ thành hai đường có ghi tên khoá. Khoá duy nhất ghép, như "
      "(ToaNhaId, SoTang), không ghi được trên từng cột nên được liệt kê ở mục 2.5.4.")
    for i, cap in enumerate(["Lược đồ quan hệ cơ cấu chung cư và cư trú", "Lược đồ quan hệ tài khoản và phân quyền",
                             "Lược đồ quan hệ sự cố và bảo trì", "Lược đồ quan hệ phân hệ thu phí"]):
        spec = [sd.rd_co_cau, sd.rd_tai_khoan, sd.rd_su_co, sd.rd_thu_phi][i]()
        B.append(("fig", f"Hình 2.{5 + i}. {cap}", IMG / (spec["name"] + ".png")))
    B.append(("h3", "2.3.4. Lý do tạo các bảng trung gian và bảng tách riêng"))
    b("CuTru: mối kết hợp cư trú giữa người và căn. Đây là liên kết thường, không phải hợp thành: căn còn người cư "
      "trú thì không xoá được, xoá căn không xoá người.")
    b("NguoiDung_VaiTro: một người có thể giữ nhiều vai trò.")
    b("VaiTro_Quyen: cấp hay thu hồi quyền chỉ là thêm, xoá một dòng.")
    b("LichSuYeuCau: mỗi lần đổi trạng thái một dòng; là nguồn của các mốc thời gian và KPI.")
    b("AnhYeuCau: một yêu cầu có nhiều ảnh.")
    b("MucUuTien: thời hạn SLA là dữ liệu, Ban quản lý sửa được.")
    b("ChiTietHoaDon: mỗi khoản một dòng, trỏ về nguồn (chỉ số, phí định kỳ, sự cố).")
    b("ThanhToan: một hoá đơn nộp nhiều lần.")

    # ------------------------------------------------------------------ 2.4
    B.append(("h2", "2.4. Phụ thuộc hàm và chuẩn hoá 3NF"))
    B.append(("h3", "2.4.1. Cơ sở"))
    p("Một lược đồ đạt 1NF khi mọi thuộc tính là nguyên tố. Đạt 2NF khi đạt 1NF và mọi thuộc tính không khoá phụ "
      "thuộc đầy đủ vào khoá. Đạt 3NF khi đạt 2NF và không có thuộc tính không khoá nào phụ thuộc bắc cầu vào khoá: "
      "với mọi phụ thuộc hàm không tầm thường X → A, X là siêu khoá hoặc A là thuộc tính khoá [4]. BCNF chặt hơn: "
      "mọi vế trái X đều là siêu khoá.")
    B.append(("h3", "2.4.2. Phụ thuộc hàm và kết luận dạng chuẩn"))
    p("Bảng 2.9 liệt kê khoá ứng viên của từng quan hệ, lấy từ khoá chính và các ràng buộc duy nhất. Sau khi bỏ các "
      "thông tin lưu hai nơi (mục 2.4.3), vế trái của mọi phụ thuộc hàm không tầm thường đều là khoá ứng viên, nên "
      "cả 22 quan hệ đạt BCNF, tức đạt 3NF. Bảng 2.9 chỉ liệt kê các quan hệ có thêm khoá tự nhiên; các quan hệ "
      "còn lại chỉ có khoá chính làm khoá ứng viên. Khoá có dấu * chỉ duy nhất trên các dòng chưa xoá mềm.")
    fd_rows = []
    for t in TABLE_ORDER[:6] + ["CuTru", "VaiTro", "NguoiDung_VaiTro", "Quyen", "VaiTro_Quyen"] + TABLE_ORDER[9:]:
        ks = candidate_keys(t)
        non = [c["name"] for c in cols(t) if not c["pk"]]
        fd = "Không có thuộc tính ngoài khoá" if not non else \
            f"{ks[0]} → {', '.join(non) if len(non) <= 4 else 'mọi thuộc tính còn lại'}"
        extra = [f"{k.rstrip(' *')} → {ks[0]}" for k in ks[1:]]
        if len(ks) > 1:   # quan hệ chỉ có khoá chính: hiển nhiên, không liệt kê
            fd_rows.append([t, "; ".join(ks), "; ".join([fd] + extra), "BCNF"])
    T("Bảng 2.9. Quan hệ có khoá tự nhiên, phụ thuộc hàm và dạng chuẩn",
      ["Quan hệ", "Khoá ứng viên", "Phụ thuộc hàm không tầm thường", "Dạng chuẩn"], fd_rows, [2.8, 3.0, 4.4, 1.2])
    B.append(("h3", "2.4.3. Các quyết định chuẩn hoá"))
    p("a) Toà – tầng – căn. CanHo chỉ giữ TangId; nếu giữ thêm ToaNhaId sẽ có phụ thuộc bắc cầu CanHoId → TangId → "
      "ToaNhaId. Tương tự, ToaNha không giữ khu vực, địa chỉ, số tầng vì đều lấy được từ DuAn và Tang.")
    p("b) Người và cư dân. Họ tên, điện thoại, email, số giấy tờ chỉ nằm ở NguoiDung. Cư dân là người dùng có dòng "
      "cư trú đang hiệu lực, không phải thực thể riêng. Mỗi email ứng với một tài khoản.")
    p("c) Tình trạng căn. “Đang ở” hay “Để trống” suy ra từ CuTru. TinhTrangCanHo chỉ lưu điều Ban quản lý quyết "
      "định: đã bàn giao, chưa bàn giao, đang sửa chữa.")
    p("d) Mốc thời gian của sự cố lấy từ LichSuYeuCau, không lưu lại ở YeuCauBaoTri.")
    p("e) Số chốt trên chứng từ. Hoá đơn phải giữ đúng số tiền lúc lập dù bảng giá đổi về sau, nên TamTinh, TongTien, "
      "DaThanhToan, ConLai và đơn giá, thành tiền từng dòng được lưu. CHECK buộc TongTien = TamTinh − GiamGia + Thue "
      "+ PhiTreHan, ConLai = TongTien − DaThanhToan, ThanhTienKhoan = SoLuongKhoan × DonGiaKhoan. Trigger TG3 chặn "
      "tổng thu vượt tổng tiền; khung nhìn vw_doi_chieu_hoa_don tính lại để phát hiện chênh lệch. HanXuLy cũng là số "
      "chốt: hạn đã hứa lúc báo, không đổi khi MucUuTien đổi.")
    p("f) Chỉ số điện nước. ChiSoDichVu chỉ lưu chỉ số cũ và mới; lượng tiêu thụ là hiệu hai chỉ số. Đơn giá và "
      "thành tiền chỉ có ở dòng hoá đơn lập từ chỉ số.")
    p("g) Nguồn của dòng hoá đơn. Mỗi dòng ghi loại khoản và trỏ về chỉ số, khoản phí hoặc sự cố đã lập ra nó; CHECK "
      "buộc dòng điện nước có chỉ số, dòng phí định kỳ có khoản phí.")
    p("h) Ảnh. Mỗi ảnh là một dòng AnhYeuCau, không gộp nhiều đường dẫn vào một cột.")

    # ------------------------------------------------------------------ 2.5
    B.extend(rbtv_blocks())

    # ------------------------------------------------------------------ 2.6
    B.append(("h2", "2.6. Từ điển dữ liệu"))
    p("Mỗi bảng có một bảng mô tả cột; kiểu và ràng buộc lấy từ cơ sở dữ liệu. Bảy cột kiểm toán mô tả một lần ở "
      "cuối mục; hai bảng trung gian phân quyền gộp chung một bảng.")
    for t in TABLE_ORDER:
        rows = []
        for i, c in enumerate(cols(t), 1):
            rows.append([c["name"], c["type"], constraint_text(c, t), DESC.get(f"{t}.{c['name']}", "")])
        T(f"Bảng {{n}}. Cấu trúc bảng {t}", ["Thuộc tính", "Kiểu dữ liệu", "Ràng buộc", "Mô tả"], rows,
          [3.2, 2.3, 2.7, 3.9])
    T("Bảng {n}. Cấu trúc hai bảng trung gian", ["Bảng", "Thuộc tính", "Kiểu dữ liệu", "Ràng buộc", "Mô tả"], [
        ["NguoiDung_VaiTro", "NguoiDungId", "text", "PK; FK → NguoiDung", "Người dùng"],
        ["NguoiDung_VaiTro", "VaiTroId", "text", "PK; FK → VaiTro", "Vai trò"],
        ["VaiTro_Quyen", "VaiTroId", "text", "PK; FK → VaiTro", "Vai trò"],
        ["VaiTro_Quyen", "QuyenId", "integer", "PK; FK → Quyen", "Quyền"],
    ], [2.8, 2.4, 1.8, 2.6, 2.6])
    T("Bảng {n}. Bảy cột kiểm toán chung", ["Thuộc tính", "Kiểu dữ liệu", "Mô tả"], [
        ["NgayTao, NguoiTao", "timestamp, varchar(450)", "Lúc tạo và người tạo dòng"],
        ["NgayCapNhat, NguoiCapNhat", "timestamp, varchar(450)", "Lần sửa gần nhất"],
        ["DaXoa, NgayXoa, NguoiXoa", "boolean, timestamp, varchar(450)", "Xoá mềm: chỉ đánh dấu, không xoá dòng"],
    ], [3.8, 3.6, 3.8])

    # ------------------------------------------------------------------ 2.7
    B.append(("h2", "2.7. Giá trị dẫn xuất, truy vấn tổng hợp và KPI"))
    p("Mỗi giá trị dẫn xuất chỉ tính từ một nguồn: mốc thời gian sự cố từ LichSuYeuCau, căn đang có người ở từ "
      "CuTru, lượng tiêu thụ từ hai chỉ số. Số tiền chốt trên hoá đơn được đối chiếu bằng khung nhìn "
      "vw_doi_chieu_hoa_don.")
    T("Bảng {n}. Khung nhìn của hệ thống", ["Khung nhìn", "Nguồn", "Nội dung"], [
        ["vw_doi_chieu_hoa_don", "HoaDon, ChiTietHoaDon, ThanhToan",
         "Mỗi hoá đơn: tạm tính và số đã trả đang lưu, so với số tính lại từ dòng chi tiết và thanh toán. "
         "Ngày 24/11/2026: 0 hoá đơn lệch."],
    ], [3.4, 3.4, 4.4])
    p("Các KPI và công thức ở Bảng 1.10 được tính từ đúng các nguồn trên. Kỳ chưa có dữ liệu hiển thị “—” thay vì 0%. Các cột hay dùng để lọc có chỉ mục: TrangThaiYeuCau, MucUuTienId, "
      "NguoiPhuTrachId, HanXuLy, NgayBaoCao và cặp (YeuCauBaoTriId, ThoiDiemChuyen).")

    # ------------------------------------------------------------------ 2.8
    B.append(("h2", "2.8. Kết quả đạt được và việc tiếp theo"))
    n_rel = sum(len(erd_relations(s)) for s in erd_specs)
    p(f"Hết tuần 6, thiết kế dữ liệu gồm 22 bảng, bốn ERD với {n_rel} mối kết hợp và bốn sơ đồ lược đồ quan hệ; cả 22 quan "
      "hệ đạt BCNF. Kèm theo là danh mục ràng buộc toàn vẹn có biểu diễn hình thức, ba trigger, một hàm, một khung "
      "nhìn và từ điển dữ liệu. Theo nhận xét của giảng viên, nhóm đã bỏ mọi thông tin lưu hai nơi và các cột không dùng, gộp cư dân "
      "vào người dùng, đặt lại tên trùng và áp thay đổi vào cơ sở dữ liệu thật. 116 ca kiểm thử tự động đều đạt, kể "
      "cả khi chạy trên PostgreSQL với đủ ràng buộc và trigger.")
    p("Mọi sơ đồ trong báo cáo được một bộ kiểm tra tự động đối chiếu với khoá ngoại và cột thật của cơ sở dữ liệu "
      "trước khi đưa vào.")
    return B


# ====================================================================== ràng buộc toàn vẹn
MG = [  # (mã, bảng, nội dung, biểu diễn, tên CHECK)
    ("MG01", "CanHo", "Diện tích lớn hơn 0; số phòng ngủ, phòng tắm không âm; hướng nhà (nếu có) từ 0 đến 7; tình "
     "trạng căn thuộc {0, 1, 3}.", "∀c ∈ CanHo: c.DienTich > 0 ∧ c.SoPhongNgu ≥ 0 ∧ c.SoPhongTam ≥ 0 ∧ "
     "(c.HuongNha = NULL ∨ 0 ≤ c.HuongNha ≤ 7) ∧ c.TinhTrangCanHo ∈ {0, 1, 3}",
     "CK_CanHo_DienTich, CK_CanHo_SoPhong, CK_CanHo_HuongNha, CK_CanHo_TinhTrangCanHo"),
    ("MG02", "ToaNha", "Đơn giá phí quản lý không âm; ngày chốt kỳ từ 1 đến 28; số ngày ân hạn không âm; tỉ lệ phí "
     "trễ từ 0 đến 100; trạng thái toà từ 0 đến 2.", "∀t ∈ ToaNha: t.DonGiaPhiQuanLy ≥ 0 ∧ 1 ≤ t.NgayChotKy "
     "≤ 28 ∧ t.SoNgayAnHan ≥ 0 ∧ 0 ≤ t.PhanTramPhiTre ≤ 100 ∧ 0 ≤ t.TrangThaiToaNha ≤ 2",
     "CK_ToaNha_DonGiaPhiQuanLy, CK_ToaNha_NgayChotKy, CK_ToaNha_PhiTre, CK_ToaNha_TrangThai"),
    ("MG03", "CuTru", "Vai trò trong hộ thuộc {0, 1, 2}.", "∀c ∈ CuTru: c.VaiTroTrongHo ∈ {0, 1, 2}",
     "CK_CuTru_VaiTroTrongHo"),
    ("MG04", "MucUuTien", "Mức ưu tiên thuộc {0, 1, 2, 3}; thời hạn phản hồi lớn hơn 0.",
     "∀m ∈ MucUuTien: m.MucUuTienId ∈ {0..3} ∧ m.PhutPhanHoi > 0", "CK_MucUuTien_Id, CK_MucUuTien_ThoiHan"),
    ("MG05", "YeuCauBaoTri", "Trạng thái thuộc 0–7; loại sự cố thuộc {0–6, 99}; đánh giá (nếu có) từ 1 đến 5; chi "
     "phí không âm.", "∀y ∈ YeuCauBaoTri: 0 ≤ y.TrangThaiYeuCau ≤ 7 ∧ y.LoaiSuCo ∈ {0..6, 99} ∧ (y.DiemDanhGia = NULL "
     "∨ 1 ≤ y.DiemDanhGia ≤ 5) ∧ y.ChiPhiUocTinh ≥ 0 ∧ y.ChiPhiThucTe ≥ 0",
     "CK_YeuCauBaoTri_TrangThai, CK_YeuCauBaoTri_LoaiSuCo, CK_YeuCauBaoTri_DanhGia, CK_YeuCauBaoTri_ChiPhi"),
    ("MG06", "AnhYeuCau", "Giai đoạn ảnh thuộc {0, 1}.", "∀a ∈ AnhYeuCau: a.GiaiDoanAnh ∈ {0, 1}", "CK_AnhYeuCau_GiaiDoan"),
    ("MG07", "LoaiDichVu, PhiDinhKy", "Đơn giá và số tiền khoản phí không âm.",
     "∀l ∈ LoaiDichVu: l.DonGia ≥ 0; ∀p ∈ PhiDinhKy: p.SoTienKhoanPhi ≥ 0",
     "CK_LoaiDichVu_DonGia, CK_PhiDinhKy_SoTien"),
    ("MG08", "ChiSoDichVu", "Chỉ số cũ không âm; tháng của kỳ từ 1 đến 12.",
     "∀s ∈ ChiSoDichVu: s.ChiSoCu ≥ 0 ∧ 1 ≤ s.KyChiSo mod 100 ≤ 12", "CK_ChiSoDichVu_ChiSo, CK_ChiSoDichVu_Ky"),
    ("MG09", "HoaDon", "Trạng thái thuộc 0–6; tháng của kỳ từ 1 đến 12; các khoản tiền không âm.",
     "∀h ∈ HoaDon: 0 ≤ h.TrangThaiHoaDon ≤ 6 ∧ 1 ≤ h.KyHoaDon mod 100 ≤ 12 ∧ h.TongTien, h.DaThanhToan, "
     "h.GiamGia, h.Thue, h.PhiTreHan ≥ 0", "CK_HoaDon_TrangThai, CK_HoaDon_Ky, CK_HoaDon_SoTien"),
    ("MG10", "ChiTietHoaDon", "Loại khoản thuộc 0–4; số lượng lớn hơn 0; đơn giá không âm.",
     "∀d ∈ ChiTietHoaDon: 0 ≤ d.LoaiKhoan ≤ 4 ∧ d.SoLuongKhoan > 0 ∧ d.DonGiaKhoan ≥ 0",
     "CK_ChiTietHoaDon_LoaiKhoan, CK_ChiTietHoaDon_SoTien"),
    ("MG11", "ThanhToan", "Số tiền lớn hơn 0; phương thức thuộc 0–4; trạng thái thuộc 0–5.",
     "∀p ∈ ThanhToan: p.SoTienThanhToan > 0 ∧ 0 ≤ p.PhuongThuc ≤ 4 ∧ 0 ≤ p.TrangThaiThanhToan ≤ 5",
     "CK_ThanhToan_SoTien, CK_ThanhToan_MaSo"),
]
TT = [
    ("TT01", "CuTru", "Ngày chuyển đi (nếu có) không trước ngày vào ở.",
     "∀c ∈ CuTru: c.NgayChuyenDi = NULL ∨ c.NgayChuyenDi ≥ c.NgayVaoO", "CK_CuTru_NgayChuyenDi"),
    ("TT02", "YeuCauBaoTri", "Đúng một trong hai cột CanHoId, ToaNhaId có giá trị; sự cố khu vực chung "
     "phải ghi vị trí.", "∀y: (y.CanHoId ≠ NULL ∧ y.ToaNhaId = NULL) ∨ (y.CanHoId = NULL ∧ "
     "y.ToaNhaId ≠ NULL ∧ y.ViTri ≠ NULL)", "CK_YeuCauBaoTri_NoiXayRa"),
    ("TT03", "YeuCauBaoTri", "Hạn xử lý (nếu có) không trước ngày báo.", "∀y: y.HanXuLy = NULL ∨ y.HanXuLy ≥ "
     "y.NgayBaoCao", "CK_YeuCauBaoTri_HanXuLy"),
    ("TT04", "YeuCauBaoTri", "Đã xử lý hoặc đã đóng thì phải có kết quả xử lý.",
     "∀y: y.TrangThaiYeuCau ∈ {3, 4} ⇒ y.KetQuaXuLy ≠ rỗng", "CK_YeuCauBaoTri_KetQua"),
    ("TT05", "MucUuTien", "Thời hạn hoàn tất không nhỏ hơn thời hạn phản hồi.", "∀m: m.PhutHoanTat ≥ m.PhutPhanHoi",
     "CK_MucUuTien_ThoiHan"),
    ("TT06", "ChiSoDichVu", "Chỉ số mới không nhỏ hơn chỉ số cũ.", "∀s: s.ChiSoMoi ≥ s.ChiSoCu",
     "CK_ChiSoDichVu_ChiSo"),
    ("TT07", "HoaDon", "Hạn thanh toán không trước ngày lập; tổng tiền và số còn lại đúng công thức.",
     "∀h: h.NgayDenHan ≥ h.NgayPhatHanh ∧ h.TongTien = h.TamTinh − h.GiamGia + h.Thue + h.PhiTreHan ∧ h.ConLai = "
     "h.TongTien − h.DaThanhToan ∧ 0 ≤ h.ConLai ≤ h.TongTien",
     "CK_HoaDon_HanThanhToan, CK_HoaDon_CongThuc, CK_HoaDon_ConLai"),
    ("TT08", "ChiTietHoaDon", "Thành tiền dòng đúng công thức; dòng điện nước có chỉ số, dòng phí định kỳ có khoản "
     "phí, chỉ dòng sửa chữa mới trỏ tới sự cố.", "∀d: d.ThanhTienKhoan = round(d.SoLuongKhoan × d.DonGiaKhoan) ∧ "
     "(d.LoaiKhoan = 1 ⇔ d.ChiSoDichVuId ≠ NULL) ∧ (d.LoaiKhoan = 2 ⇔ d.PhiDinhKyId ≠ NULL) ∧ (d.YeuCauBaoTriId ≠ "
     "NULL ⇒ d.LoaiKhoan = 3)", "CK_ChiTietHoaDon_ThanhTien, CK_ChiTietHoaDon_Nguon"),
    ("TT09", "ThanhToan", "Số tiền hoàn trả không âm và không vượt số tiền thanh toán.",
     "∀p: 0 ≤ p.SoTienHoanTra ≤ p.SoTienThanhToan", "CK_ThanhToan_HoanTra"),
]
LB = [  # (mã, bối cảnh, nội dung, biểu diễn, cách đảm bảo, [(quan hệ, T, X, S)])
    ("LB01", "YeuCauBaoTri, CuTru, CanHo, Tang", "Người báo là người đang cư trú tại căn xảy ra sự cố (hoặc tại một căn "
     "của toà có khu vực chung), trừ khi quản lý tạo thay.",
     "∀y ∈ YeuCauBaoTri, y.CanHoId ≠ NULL: ∃c ∈ CuTru: c.NguoiDungId = y.NguoiBaoCaoId ∧ c.CanHoId = y.CanHoId ∧ "
     "c.NgayChuyenDi = NULL", "Ứng dụng, kiểm tra trước khi lưu",
     [("YeuCauBaoTri", "+", "–", "+ (NguoiBaoCaoId, CanHoId)"), ("CuTru", "–", "–", "– (*)")]),
    ("LB02", "YeuCauBaoTri, NguoiDung_VaiTro", "Người phụ trách giữ vai trò kỹ thuật viên hoặc quản lý.",
     "∀y, y.NguoiPhuTrachId ≠ NULL: ∃v ∈ NguoiDung_VaiTro: v.NguoiDungId = y.NguoiPhuTrachId ∧ v.VaiTroId ∈ "
     "{thợ, quản lý}", "Ứng dụng khi giao việc", [("YeuCauBaoTri", "+", "–", "+ (NguoiPhuTrachId)"),
                                                  ("NguoiDung_VaiTro", "–", "+", "+ (VaiTroId)")]),
    ("LB03", "YeuCauBaoTri", "Trạng thái chỉ chuyển theo bảng chuyển trạng thái (Bảng 1.7); bắt đầu xử lý phải có "
     "người phụ trách.", "∀y cũ → y mới: (y.TrangThaiYeuCau, y'.TrangThaiYeuCau) ∈ Chuyển ∧ (y'.TrangThaiYeuCau = "
     "2 ⇒ y'.NguoiPhuTrachId ≠ NULL)", "Trigger TG1 và ứng dụng",
     [("YeuCauBaoTri", "–", "–", "+ (TrangThaiYeuCau)")]),
    ("LB04", "YeuCauBaoTri, LichSuYeuCau", "Yêu cầu có ngay một dòng lịch sử “Tạo yêu cầu”; mỗi lần đổi trạng "
     "thái, giao việc thêm đúng một dòng, trong cùng giao dịch.", "∀y ∈ YeuCauBaoTri: ∃l ∈ LichSuYeuCau: "
     "l.YeuCauBaoTriId = y.YeuCauBaoTriId ∧ l.TrangThaiTruoc = NULL", "Giao dịch ở ứng dụng",
     [("YeuCauBaoTri", "+", "–", "+ (TrangThaiYeuCau, NguoiPhuTrachId)"), ("LichSuYeuCau", "–", "+", "+")]),
    ("LB05", "YeuCauBaoTri, MucUuTien, LichSuYeuCau", "Hạn xử lý = ngày báo + thời hạn hoàn tất của mức ưu tiên, "
     "chốt lúc báo, cộng thời gian tạm dừng.", "∀y: y.HanXuLy = y.NgayBaoCao + m.PhutHoanTat + Σ thời gian tạm dừng "
     "(m ∈ MucUuTien, m.MucUuTienId = y.MucUuTienId lúc báo)", "Ứng dụng, hàm FN1",
     [("YeuCauBaoTri", "+", "–", "+ (MucUuTienId, HanXuLy)"), ("MucUuTien", "–", "–", "– (*)")]),
    ("LB06", "YeuCauBaoTri", "AI chỉ điền loại khi cư dân chọn “Khác”, chỉ được nâng mức ưu tiên.",
     "∀y: y.MucUuTienId ≥ mức cư dân chọn", "Ứng dụng", [("YeuCauBaoTri", "+", "–", "–")]),
    ("LB07", "ThanhToan, HoaDon", "Tổng các khoản thành công (trừ phần hoàn) không vượt tổng tiền hoá đơn.",
     "∀h ∈ HoaDon: Σ (p.SoTienThanhToan − p.SoTienHoanTra | p ∈ ThanhToan, p.HoaDonId = h.HoaDonId, "
     "p.TrangThaiThanhToan ∈ {1, 3, 4}) ≤ h.TongTien", "Trigger TG3 và ứng dụng",
     [("ThanhToan", "+", "–", "+ (SoTienThanhToan, SoTienHoanTra, TrangThaiThanhToan)"),
      ("HoaDon", "–", "–", "+ (TongTien)")]),
    ("LB08", "HoaDon, ThanhToan", "Số đã trả của hoá đơn bằng tổng các khoản thành công trừ phần hoàn.",
     "∀h: h.DaThanhToan = Σ (p.SoTienThanhToan − p.SoTienHoanTra | p thành công của h)",
     "Ứng dụng cập nhật trong cùng giao dịch; khung nhìn đối chiếu",
     [("HoaDon", "–", "–", "+ (DaThanhToan)"), ("ThanhToan", "+", "+", "+")]),
    ("LB09", "HoaDon, ChiTietHoaDon", "Tạm tính bằng tổng thành tiền các dòng chi tiết.",
     "∀h: h.TamTinh = Σ (d.ThanhTienKhoan | d ∈ ChiTietHoaDon, d.HoaDonId = h.HoaDonId)",
     "Ứng dụng; khung nhìn đối chiếu", [("HoaDon", "–", "–", "+ (TamTinh)"), ("ChiTietHoaDon", "+", "+", "+")]),
    ("LB10", "ChiSoDichVu, ChiTietHoaDon, HoaDon", "Chỉ số đã đưa vào hoá đơn chưa huỷ thì không được sửa hay xoá.",
     "∀s ∈ ChiSoDichVu: (∃d ∈ ChiTietHoaDon, h ∈ HoaDon: d.ChiSoDichVuId = s.ChiSoDichVuId ∧ d.HoaDonId = h.HoaDonId "
     "∧ h.TrangThaiHoaDon ≠ 5) ⇒ s không đổi", "Trigger TG2",
     [("ChiSoDichVu", "–", "+", "+"), ("ChiTietHoaDon", "–", "–", "–")]),
    ("LB11", "ChiSoDichVu", "Chỉ số cũ của một kỳ bằng chỉ số mới của kỳ liền trước (nếu có).",
     "∀s, s' cùng căn, cùng loại, s'.KyChiSo là kỳ liền trước: s.ChiSoCu = s'.ChiSoMoi", "Ứng dụng",
     [("ChiSoDichVu", "+", "+", "+ (ChiSoCu, ChiSoMoi)")]),
    ("LB12", "HoaDon, CanHo", "Căn chưa bàn giao không được lập hoá đơn phí tháng.",
     "∀h, h.LoaiHoaDon = 0: căn của h có TinhTrangCanHo ≠ 1 lúc lập", "Ứng dụng",
     [("HoaDon", "+", "–", "–"), ("CanHo", "–", "–", "– (*)")]),
    ("LB13", "HoaDon, ThanhToan", "Hoá đơn đã có thanh toán thành công phải hoàn tiền trước khi huỷ; huỷ hoá đơn thì "
     "các thanh toán đang chờ bị huỷ theo.", "∀h: h.TrangThaiHoaDon = 5 ⇒ ∄p ∈ ThanhToan của h có trạng thái 0 hoặc "
     "1", "Ứng dụng, trong một giao dịch", [("HoaDon", "–", "–", "+ (TrangThaiHoaDon)"), ("ThanhToan", "+", "–", "+")]),
    ("LB14", "CuTru", "Mỗi căn có người đang ở có đúng một người đại diện.",
     "∀căn có c ∈ CuTru đang ở: |{c | c.LaDaiDien ∧ c.NgayChuyenDi = NULL}| = 1",
     "Chỉ mục duy nhất có điều kiện (tối đa một) và ứng dụng (ít nhất một)",
     [("CuTru", "+", "+", "+ (LaDaiDien, NgayChuyenDi)")]),
    ("LB15", "CuTru, NguoiDung", "Cư trú chỉ có hiệu lực trên cổng cư dân khi người đó đã xác nhận qua thư mời.",
     "Căn hiện trên cổng của u ⇔ ∃c ∈ CuTru: c.NguoiDungId = u ∧ c.NgayXacNhan ≠ NULL ∧ c.NgayChuyenDi = NULL",
     "Ứng dụng, mã mời ký số hạn 14 ngày", [("CuTru", "+", "–", "+ (NgayXacNhan)")]),
    ("LB16", "NguoiDung, NguoiDung_VaiTro", "Mỗi tài khoản có ít nhất một vai trò.",
     "∀u ∈ NguoiDung: ∃v ∈ NguoiDung_VaiTro: v.NguoiDungId = u.NguoiDungId", "Ứng dụng",
     [("NguoiDung", "+", "–", "–"), ("NguoiDung_VaiTro", "–", "+", "+")]),
    ("LB17", "ChiTietHoaDon, HoaDon, ChiSoDichVu", "Dòng điện nước trỏ về chỉ số của đúng căn và đúng kỳ của hoá đơn.",
     "∀d, d.ChiSoDichVuId ≠ NULL: s.CanHoId = h.CanHoId ∧ s.KyChiSo = h.KyHoaDon (s, h là chỉ số và hoá đơn của d)",
     "Ứng dụng khi lập hoá đơn", [("ChiTietHoaDon", "+", "–", "+ (ChiSoDichVuId)"), ("HoaDon", "–", "–", "+"),
                                  ("ChiSoDichVu", "–", "–", "+")]),
    ("LB18", "CuTru, CanHo", "Căn chưa bàn giao thì không có người đang cư trú.",
     "∀c ∈ CuTru, c.NgayChuyenDi = NULL: căn của c có TinhTrangCanHo ≠ 1", "Ứng dụng",
     [("CuTru", "+", "–", "+ (NgayChuyenDi)"), ("CanHo", "–", "–", "+ (TinhTrangCanHo)")]),
]


def rbtv_blocks():
    B = []
    p = lambda s: B.append(("p", s))
    T = lambda cap, head, rows, w: B.append(("table", cap, head, rows, w))
    B.append(("h2", "2.5. Ràng buộc toàn vẹn"))
    B.append(("h3", "2.5.1. Phân loại và cách trình bày"))
    p("Ràng buộc toàn vẹn chia năm loại: miền giá trị, liên thuộc tính, duy nhất, tham chiếu và liên bộ – liên quan hệ. Mỗi ràng buộc có bối cảnh (các quan hệ liên quan), nội dung, biểu diễn hình thức và tầm ảnh hưởng: thao tác Thêm (T), Xoá (X), Sửa (S) nào có thể làm vi phạm. Dấu + là phải kiểm tra, – là không ảnh hưởng, + (cột) là chỉ khi sửa các cột đó. Ràng buộc miền giá trị, liên thuộc tính và duy nhất chỉ có thể bị vi phạm khi Thêm hoặc Sửa trên chính quan hệ đó. Cột cài đặt ghi đúng tên ràng buộc trong cơ sở dữ liệu.")
    B.append(("h3", "2.5.2. Ràng buộc miền giá trị"))
    p("Mỗi ràng buộc giới hạn giá trị của một thuộc tính. Thêm và Sửa có thể vi phạm, Xoá thì không.")
    T("Bảng 2.10. Ràng buộc miền giá trị", ["Mã", "Bối cảnh", "Nội dung", "Biểu diễn", "Cài đặt"],
      [[m, t, nd, bd, ck] for m, t, nd, bd, ck in MG], [1.2, 1.7, 3.1, 3.4, 2.2])
    B.append(("h3", "2.5.3. Ràng buộc liên thuộc tính"))
    p("Các ràng buộc này so sánh thuộc tính trong cùng một dòng nên khai báo được bằng CHECK. Công thức của các số chốt trên chứng từ (mục 2.4.3 e) nằm ở nhóm này.")
    T("Bảng 2.11. Ràng buộc liên thuộc tính", ["Mã", "Bối cảnh", "Nội dung", "Biểu diễn", "Cài đặt"],
      [[m, t, nd, bd, ck] for m, t, nd, bd, ck in TT], [1.2, 1.7, 3.1, 3.4, 2.2])
    B.append(("h3", "2.5.4. Ràng buộc duy nhất"))
    dn = []
    i = 0
    for t in TABLE_ORDER + ["NguoiDung_VaiTro", "VaiTro_Quyen"]:
        for u in S[t]["unique"]:
            if u["name"] in ("UserNameIndex", "RoleNameIndex"):
                continue
            i += 1
            colsn = ", ".join(u["cols"])
            cond = ""
            if "LaDaiDien" in u["where"]:
                cond = " trong các dòng là đại diện, đang ở, chưa xoá"
            elif "NgayChuyenDi" in u["where"]:
                cond = " trong các dòng đang ở, chưa xoá"
            elif "LoaiHoaDon" in u["where"]:
                cond = " trong các hoá đơn phí tháng chưa huỷ, chưa xoá"
            elif "NormalizedEmail" in u["where"]:
                cond = " khi có email"
            elif u["where"]:
                cond = " trong các dòng chưa xoá"
            colsn = colsn.replace("NormalizedEmail", "Email")
            dn.append([f"DN{i:02d}", t, f"({colsn}) không trùng{cond}",
                       f"∀x, x' ∈ {t}, x ≠ x'{' (thoả điều kiện)' if cond else ''}: x.({colsn}) ≠ x'.({colsn})",
                       "UNIQUE có điều kiện" if u["where"] else "UNIQUE"])
    p("Khoá tự nhiên được khai báo duy nhất. Ràng buộc chỉ áp cho một phần dòng (đang ở, chưa huỷ, chưa xoá mềm) dùng chỉ mục duy nhất có điều kiện.")
    T("Bảng 2.12. Ràng buộc duy nhất", ["Mã", "Bối cảnh", "Nội dung", "Biểu diễn", "Cài đặt"], dn,
      [1.2, 2.0, 3.2, 3.2, 2.0])
    B.append(("h3", "2.5.5. Ràng buộc tham chiếu (khoá ngoại)"))
    p("Mỗi khoá ngoại bảo đảm: ∀x ∈ Con, x.K ≠ NULL: ∃y ∈ Cha: y.PK = x.K. Thêm, Sửa khoá ngoại ở quan hệ con và Xoá ở quan hệ cha có thể vi phạm. Khi xoá dòng cha: CASCADE với bộ phận của cha (ảnh, lịch sử, dòng hoá đơn), SET NULL với tham chiếu tuỳ chọn tới người dùng, RESTRICT với phần còn lại. Ứng dụng dùng xoá mềm, nên đây là lớp bảo vệ khi xoá trực tiếp trong cơ sở dữ liệu. Bảng 2.13 chỉ liệt kê các khoá không dùng RESTRICT; các khoá còn lại chặn xoá và đã thể hiện ở lược đồ quan hệ, từ điển dữ liệu.")
    tc = []
    i = 0
    for t in TABLE_ORDER + ["NguoiDung_VaiTro", "VaiTro_Quyen"]:
        for c in S[t]["columns"]:
            if c["fk"]:
                i += 1
                rule = c["on_delete"].replace("NO ACTION", "RESTRICT")
                if rule != "RESTRICT":     # RESTRICT là mặc định, chỉ liệt kê ngoại lệ
                    tc.append([f"TC{i:02d}", f"{t}.{c['name']}", c["fk"], "Có" if c["null"] else "Không", rule])
    T("Bảng 2.13. Khoá ngoại không chặn xoá dòng cha",
      ["Mã", "Quan hệ con và khoá ngoại", "Quan hệ cha", "Cho phép NULL", "Khi xoá ở quan hệ cha"], tc,
      [1.0, 4.4, 2.4, 1.6, 2.2])
    B.append(("h3", "2.5.6. Ràng buộc liên bộ, liên quan hệ"))
    p("Các ràng buộc ở Bảng 2.14 cần dữ liệu nhiều dòng, nhiều bảng hoặc phụ thuộc người thao tác, nên PK, FK, UNIQUE, CHECK không diễn đạt được. Ứng dụng kiểm tra chúng trong cùng giao dịch với thao tác ghi; ràng buộc nào phải đúng cả khi sửa trực tiếp cơ sở dữ liệu thì có thêm trigger (mục 2.5.7). Bảng 2.15 cho tầm ảnh hưởng trên từng quan hệ; (*) là thao tác chỉ cần kiểm tra lúc tạo dòng.")
    T("Bảng 2.14. Ràng buộc liên bộ, liên quan hệ", ["Mã", "Bối cảnh", "Nội dung", "Biểu diễn", "Cách đảm bảo"],
      [[m, bc, nd, bd, cd] for m, bc, nd, bd, cd, _ in LB], [1.2, 2.2, 2.9, 3.5, 1.7])
    T("Bảng 2.15. Tầm ảnh hưởng của các ràng buộc liên bộ, liên quan hệ", ["Mã", "Quan hệ", "Thêm", "Xoá", "Sửa"],
      [[m if j == 0 else "", r, tt, x, ss] for m, *_, rows in LB for j, (r, tt, x, ss) in enumerate(rows)],
      [1.0, 3.6, 1.1, 1.1, 4.4])
    B.append(("h3", "2.5.7. Trigger và hàm bảo vệ dữ liệu"))
    p("Ba trigger và một hàm ở Bảng 2.16 chỉ kiểm tra rồi chấp nhận hoặc từ chối lệnh ghi, không tự ghi thêm dữ liệu; mỗi cái đã được thử bằng lệnh vi phạm cố ý. TG1 kiểm tra theo bảng chuyển trạng thái ở Bảng 1.7; ai được chuyển do ứng dụng xác định theo vai trò (Bảng 1.7).")
    T("Bảng 2.16. Trigger và hàm ở tầng cơ sở dữ liệu",
      ["Mã", "Loại, đối tượng", "Điều kiện kiểm tra", "Khi vi phạm", "Ràng buộc"], [
          ["TG1", "Trigger BEFORE UPDATE OF TrangThaiYeuCau trên YeuCauBaoTri",
           "Cặp (trạng thái cũ, mới) thuộc bảng chuyển; chuyển sang Đang xử lý phải có NguoiPhuTrachId",
           "Huỷ lệnh, báo số yêu cầu và hai trạng thái", "LB03"],
          ["TG2", "Trigger BEFORE UPDATE, DELETE trên ChiSoDichVu",
           "Chỉ số có dòng thuộc hoá đơn chưa huỷ thì không sửa, không xoá", "Huỷ lệnh, báo lỗi", "LB10"],
          ["TG3", "Trigger AFTER INSERT, UPDATE trên ThanhToan",
           "Tổng thu thành công trừ phần hoàn của hoá đơn ≤ TongTien", "Huỷ cả giao dịch, báo lỗi", "LB07"],
          ["FN1", "Hàm fn_han_xu_ly(ngày báo, mức)", "Trả về ngày báo + PhutHoanTat của mức trong MucUuTien",
           "Không có", "LB05"],
      ], [1.1, 3.2, 3.5, 2.2, 1.3])
    return B
