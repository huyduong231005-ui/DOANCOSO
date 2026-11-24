# Rà soát bản `BaoCao_DACN_Tuan1-6_sua.docx` theo góp ý tuần 6 của GVHD (24/11/2026)

So sánh: `Downloads/BaoCao_DACN_Tuan1-6.docx` (bản gốc 19/11) và `Downloads/BaoCao_DACN_Tuan1-6_sua.docx` (bản sửa 24/11).
Đã đọc toàn văn và xem trực tiếp từng ảnh sơ đồ trong bản sửa.

Kết luận chung: **20 góp ý: 5 đạt, 7 đạt một phần, 8 chưa đạt.** Bản sửa còn tạo ra thêm 3 lỗi mới (mục C).

## A. Đối chiếu từng góp ý

| # | Góp ý của thầy | Bản sửa | Bằng chứng |
|---|---|---|---|
| 1 | Để ngang sơ đồ, không để dọc | **Chưa đạt** | Mọi ERD, RD, sơ đồ lớp vẫn là ảnh đứng cao: Hình 2.1 rộng 350pt × cao 600pt, Hình 2.6 rộng 145pt × cao 600pt (một dải hẹp), Hình 3.8 rộng 222pt × cao 530pt. Không có trang ngang nào (cả file chỉ có 3 section, đều khổ dọc). |
| 2 | Thuộc tính trùng tên quá nhiều | Đạt **trên giấy** | Đã thêm hậu tố (TenKhuVuc, MoTaCanHo, TrangThaiHoaDon…) và Bảng 2.2 quy ước. Nhưng CSDL và mã nguồn vẫn dùng `Ten`, `MoTa`, `TrangThai` (xem C1). |
| 3 | Xoá chú thích vẽ hình ở đâu (StarUML…) | Đạt | Không còn chữ “StarUML” trong chú thích hay bảng phân công. |
| 4 | Hai bảng lưu một thông tin | Đạt một phần | Đã thêm mục 2.1.3, bỏ HoTen ở CuDan, KhuVucId ở ToaNha, ToaNhaId ở CanHo, cột tổng tiền ở HoaDon. Tuy nhiên vẫn giữ CU_DAN và NGUOI_DUNG là hai thực thể về cùng một con người (xem #17). |
| 5 | ERD sai cơ bản nhiều | **Chưa đạt** | Hình 2.1: nói dùng ký pháp Chen nhưng thuộc tính lại nằm trong hộp như kiểu chân gà (lai hai ký pháp); nhãn bản số đè lên nhau (“(0,n)(0,n)”, “gồm(1,1)”); đường “quản lý” cắt ngang thực thể CU_DAN; tên thực thể khác tên bảng (KHU ↔ DuAn, YEU_CAU_SU_CO ↔ YeuCauBaoTri); thuộc tính kỹ thuật (Slug, Anh…) vẫn có trong ERD mức khái niệm. |
| 6 | Xem lại toàn bộ ERD, bản số, mối quan hệ | Đạt một phần | Bản số (min,max) đã thống nhất ở các bảng 2.5–2.8, nhưng các lỗi mối quan hệ của #15, #17 còn nguyên. |
| 7 | Ràng buộc toàn vẹn trình bày chi tiết | Đạt một phần | Đã chia 5 loại có mã, có cột Thêm/Xoá/Sửa. Còn thiếu: phát biểu hình thức (∀ t ∈ R: …), bảng tầm ảnh hưởng **theo từng quan hệ** cho ràng buộc liên quan hệ (LB08 dính 3 bảng nhưng chỉ một dòng +/–), và ràng buộc khoá ngoại (Bảng 2.13) không có bảng tầm ảnh hưởng. |
| 8 | 2.5.2 không thống nhất ngôi (AI viết nhiều) | Đạt | Thân bài thống nhất dùng “nhóm”; “chúng em” chỉ còn ở lời cảm ơn, cam đoan. |
| 9 | Không đưa mã trigger, mã AI | Đạt | Đoạn `CREATE OR REPLACE FUNCTION fn_kiem_tra_chuyen_trang_thai…` đã bỏ; trigger chỉ mô tả bằng bảng 2.15. |
| 10 | Bỏ minh hoạ dữ liệu | Đạt | Mục 2.7 “Minh hoạ nghiệp vụ hoàn chỉnh qua dữ liệu” và Bảng 2.24–2.25 đã bỏ. |
| 11 | Use case đặt tên sai (vd “hỏi lại AI”, “ghi nhận yêu cầu”) | **Chưa đạt** | Hình 3.2 vẫn có “Làm rõ mô tả sự cố”, “Cảnh báo lỗi AI”, “Cảnh báo sự cố khẩn cấp” — đều là việc bên trong hệ thống, không phải mục tiêu của tác nhân. UC07 “Phân loại sự cố” (tác nhân Mô hình AI), UC08 “Điều phối sự cố” (tác nhân Dịch vụ email) cũng cùng lỗi. Biểu tượng tác nhân là hình người tô xanh, không phải hình người que UML. |
| 12 | Điều chỉnh lại use case | Đạt một phần | Đã bỏ bảng quan hệ, viết lại đoạn mô tả; nhưng tập use case gần như giữ nguyên. |
| 13 | Không trình bày bảng quan hệ trong use case | Đạt | Câu “các sơ đồ không kèm bảng quan hệ riêng”, không còn bảng quan hệ UC. |
| 14 | Activity sai: mỗi hình chữ nhật một hành động cụ thể; trước nút quyết định phải có hành vi kiểm tra | **Chưa đạt** | Hình 3.7 và Hình 1.4: ô hành động gộp nhiều việc và có diễn giải (“Nhập mô tả, chọn nơi xảy ra, đính kèm ảnh, gửi yêu cầu”; “Gộp kết quả AI… (chỉ nâng ưu tiên)”; “Hỏi lại cư dân, phân công thủ công hoặc từ chối (ghi lý do)”; “Gửi thư giao việc cho thợ; thư Cần theo dõi nếu khẩn cấp” — có cả điều kiện trong ô). Nút quyết định nối thẳng từ ô nhập liệu hoặc từ nút quyết định khác, không có hành động “Kiểm tra …” đứng trước. “Rõ và có thợ phù hợp?” gộp hai câu hỏi, nhánh “không có thợ” không có nhãn. Ô có hai luồng ra mà không có thanh fork; ô nhận hai luồng vào mà không có nút gộp (“Tính hạn SLA…”, “Xử lý; tạm dừng…”); các nhánh gửi thư cụt, không về nút kết thúc; đường nối chạy dọc trên vạch chia làn. |
| 15 | Căn hộ – cư dân dùng hợp thành sai (huỷ căn thì cư dân không mất) | **Chưa đạt** | Sơ đồ lớp Hình 3.8 vẫn vẽ hình thoi đặc CanHo ◆— CuDan; câu “CanHo gồm các CuDan (hợp thành)”; Bảng 2.13 TC06 `CuDan.CanHoId … CASCADE` (xoá căn xoá luôn cư dân). |
| 16 | Yêu cầu bảo trì, ảnh, trạng thái xem lại | Đạt một phần | Tên vẫn lệch: thực thể YEU_CAU_SU_CO / ANH_SU_CO nhưng bảng YeuCauBaoTri; “bảo trì” và “sự cố” dùng lẫn. |
| 17 | Người dùng và cư dân là một | **Chưa đạt** | Bản sửa giữ hai thực thể và còn viết lập luận ngược lại góp ý (mục 2.2.2: “CU_DAN được vẽ là thực thể chứ không phải mối kết hợp”). |
| 18 | Đặt lại tên Mức độ ưu tiên | **Chưa đạt** | Vẫn `MucDoUuTien` vừa là khoá chính của MucSla vừa là cột của YeuCauBaoTri vừa là tên kiểu liệt kê; thực thể tên MUC_SLA không nói lên “mức ưu tiên”. |
| 19 | Xem lại hết ChiTietHoaDon | Đạt một phần | Đã bỏ ThanhTien, đổi tên cột. Còn thiếu: dòng chi tiết không cho biết khoản đó đến từ đâu (chỉ số điện nước nào, phí định kỳ nào, sự cố nào) — chỉ có chuỗi NoiDungKhoan; không có loại khoản; chỉ số đã xuất hoá đơn chỉ đánh dấu bằng cờ DaXuatHoaDon, không truy được về dòng hoá đơn. |
| 20 | Sequence: mỗi mũi tên phải đi ra từ một thanh kích hoạt, trừ mũi tên đầu | **Chưa đạt** | Hình 3.11–3.14 không có thanh kích hoạt nào; mọi mũi tên đi ra từ đường sống nét đứt. Trong khung alt, mũi tên trả về “Thông báo lỗi” không có lời gọi tương ứng. |

## B. Vì sao kết quả không như ý (nguyên nhân gốc)

1. **Sửa chữ thay vì sửa mô hình.** Phần dễ (đổi tên, xoá đoạn, thêm bảng) làm hết; phần khó (vẽ lại ERD, activity, sequence theo đúng quy cách UML) chỉ đổi công cụ vẽ rồi giữ nguyên lỗi. Các sơ đồ được sinh tự động bằng thư viện vẽ đồ thị (bố cục tự xếp) nên không kiểm soát được hướng ngang, nhãn đè, đường cắt.
2. **Không kiểm tra từng góp ý bằng mắt sau khi sửa.** Góp ý 15, 17, 20 có thể phát hiện chỉ bằng cách mở ảnh ra xem; chúng vẫn sai y nguyên.
3. **Cãi lại góp ý thay vì làm theo.** Góp ý 17 bị viết thành một đoạn lập luận ngược (CU_DAN phải là thực thể) — người chấm đọc đoạn này sẽ thấy nhóm không tiếp thu.
4. **Quy tắc cũ đè lên góp ý mới.** Quy ước trước đó “không dùng trang ngang, hình lớn xoay 90° trong trang dọc” chính là cái thầy bắt sửa ở góp ý 1, nhưng không ai cập nhật lại quy ước.
5. **Báo cáo đi trước hệ thống.** Đổi lược đồ trên giấy (bỏ cột, đổi tên, bỏ ToaNhaId/KhuVucId/HoTen/TongTien) mà CSDL thật, mã nguồn, ảnh chụp màn hình Chương 4 và các truy vấn đối chiếu vẫn theo lược đồ cũ → vi phạm thứ tự “đúng nghiệp vụ → đúng dữ liệu → đúng hệ thống” mà thầy yêu cầu.
6. **Sinh lại cả file thay vì sửa trên file của nhóm.** Bản sửa dùng bộ style khác hẳn bản gốc (bản gốc lưu bằng WPS, style mã số) và có ảnh đặt lệch chỗ, nên bố cục nhóm đã chỉnh bị mất.

## C. Lỗi mới phát sinh trong bản sửa

- C1. Lược đồ Chương 2 không còn khớp CSDL thật: `AppDbContextModelSnapshot.cs` vẫn có `Ten`, `MoTa`, `TrangThai`, `CuDan.HoTen`, `CanHo.ToaNhaId`, `ToaNha.KhuVucId`, `HoaDon.TongTien/DaThanhToan/ConLai`, `ChiTietHoaDon.ThanhTien`. Chương 4 lại khẳng định “đối chiếu với SQL trên CSDL”. Mục 2.8 tự thừa nhận “việc tiếp theo là đồng bộ CSDL và mã nguồn”.
- C2. Hình đặt sai chỗ: Hình 3.1 nằm trước đề mục 3.1; Hình 3.2 chen giữa danh sách gạch đầu dòng của 3.1; Hình 3.7 nằm trong 3.2 trước đề mục 3.3; Hình 3.11 nằm trong 3.4 trước Bảng 3.9.
- C3. Sơ đồ không còn file gốc của công cụ UML (StarUML `.mdj`) để chứng minh — thầy chỉ yêu cầu bỏ dòng chú thích, không yêu cầu đổi công cụ.

## D. Hướng sửa đề xuất

| Góp ý | Cách sửa |
|---|---|
| 1 | Sơ đồ lớn đặt trong section trang ngang (A4 xoay ngang chỉ cho trang chứa hình), bố cục sơ đồ trải theo chiều ngang, không xoay ảnh. |
| 15, 17 | Bỏ thực thể CU_DAN; NGUOI_DUNG là người. Quan hệ **cư trú** NGUOI_DUNG (0,n) — (0,n) CAN_HO có thuộc tính VaiTroTrongHo, LaDaiDien, NgayVaoO, NgayChuyenDi → bảng `CuTru(CuTruId, NguoiDungId, CanHoId, …)`. Liên kết thường (không hợp thành), FK `CanHoId` RESTRICT: căn còn người cư trú thì không xoá được, xoá căn không xoá người. |
| 11, 12 | Use case = động từ + đối tượng, nhìn từ mục tiêu của tác nhân người: “Báo sự cố”, “Theo dõi yêu cầu”, “Phân công kỹ thuật viên”, “Bổ sung thông tin sự cố”… AI, email là tác nhân phụ hoặc là bước trong đặc tả, không thành use case. |
| 14 | Mỗi ô một động từ (“Nhập mô tả”, “Đính kèm ảnh”, “Gửi yêu cầu”, “Kiểm tra dữ liệu”); trước mỗi nút quyết định là ô “Kiểm tra …”, nút quyết định là một câu hỏi duy nhất (“Hợp lệ?”); nhiều luồng ra dùng fork/join, nhiều luồng vào dùng merge; nhánh nào cũng về nút kết thúc. |
| 20 | Vẽ thanh kích hoạt trên mọi đường sống nhận lời gọi; mũi tên gọi đi ra từ thanh kích hoạt, mũi tên trả về kết thúc thanh. |
| 18 | Thực thể MUC_UU_TIEN (MucUuTienId, TenMucUuTien, PhutPhanHoi, PhutHoanTat); YeuCauBaoTri.MucUuTienId. |
| 19 | ChiTietHoaDon thêm LoaiKhoan và khoá ngoại nguồn tuỳ chọn (ChiSoDichVuId / PhiDinhKyId / YeuCauBaoTriId) để truy vết; bỏ cờ DaXuatHoaDon (suy ra từ việc có dòng chi tiết trỏ tới). |
| 7 | Mỗi ràng buộc: bối cảnh, phát biểu lời, biểu diễn hình thức, bảng tầm ảnh hưởng theo từng quan hệ × (Thêm/Xoá/Sửa, kèm thuộc tính). |
| C1 | Hoặc sửa CSDL + mã cho khớp lược đồ mới (migration đổi tên, chạy lại kiểm thử, chụp lại hình Chương 4), hoặc giữ tên CSDL cũ và ghi tên trong báo cáo đúng như CSDL. Không được để hai bên lệch. |

## E. Kết quả sau khi sửa — bản `BaoCao_DACN_Tuan1-6_sua_v2.docx/.pdf` (24/11/2026, 141 trang)

Kiểm bằng máy: `kiem_tra_so_do.py` (0 lỗi trên 26 sơ đồ; `test_kiem_tra_so_do.py` bắt đủ 6/6 lỗi cài sẵn), quét chữ trên docx, đối chiếu `luoc_do.json` lấy từ CSDL thật, 116/116 test của `t.Tests` (SQLite và PostgreSQL).

| # | Trạng thái | Bằng chứng trên bản v2 |
|---|---|---|
| 1 | Đạt | 22 hình trên trang ngang (19 nhóm section ngang: ERD, RD, use case, lớp, tuần tự rộng); số trang chạy liền 1 → 130. |
| 2 | Đạt | CSDL đổi sang tên cột duy nhất; mọi cột không phải khoá ngoại không trùng tên giữa các bảng (kiểm trên lược đồ thật). |
| 3 | Đạt | Không còn “StarUML” trong chú thích. |
| 4 | Đạt | Bỏ CuDan (gộp vào NguoiDung + CuTru), KhuVucId ở ToaNha, SoLuongTang, ToaNhaId ở CanHo, Phone/Currency trùng, cờ DaXuatHoaDon; cột dẫn xuất giữ lại có CHECK và giải thích ở 2.4.3. |
| 5, 6 | Đạt | ERD ký pháp Chen thống nhất (thực thể, hình thoi, (min,max) ở đầu nhánh, thuộc tính mối kết hợp ở ghi chú); tên thực thể = tên bảng; validator đối chiếu từng mối kết hợp với khoá ngoại thật. |
| 7 | Đạt | MG/TT/DN/TC/LB đều có bối cảnh, phát biểu, biểu diễn hình thức ∀…, tầm ảnh hưởng T/X/S; Bảng 2.15 tầm ảnh hưởng theo từng quan hệ cho LB. |
| 8 | Đạt | 2.5.2 cùng một khuôn câu, thân bài dùng “nhóm”. |
| 9 | Đạt | Không còn mã SQL/trigger/prompt AI; trigger mô tả bằng bảng 2.16. |
| 10 | Đạt | Không còn mục minh hoạ dữ liệu. |
| 11, 12, 13 | Đạt | Tên UC là mục tiêu của tác nhân người (Báo sự cố, Phân công xử lý sự cố…); AI và email là tác nhân phụ; không có bảng quan hệ UC, chỉ có đặc tả. |
| 14 | Đạt | Mỗi ô một hành động; trước mỗi quyết định là ô “Kiểm tra …”, nút quyết định một luồng vào, có merge; validator kiểm. |
| 15 | Đạt | CuTru liên kết thường, FK RESTRICT; hợp thành chỉ còn YCBT–AnhSuCo, YCBT–LichSu, HoaDon–ChiTiet. |
| 16 | Đạt | YEU_CAU_BAO_TRI ↔ YeuCauBaoTri, ANH_SU_CO ↔ AnhSuCo, trạng thái theo bảng chuyển 2.17 + trigger TG1. |
| 17 | Đạt | Một thực thể NGUOI_DUNG; cư trú là mối kết hợp NGUOI_DUNG–CAN_HO. |
| 18 | Đạt | MUC_UU_TIEN / MucUuTien(MucUuTienId, TenMucUuTien, …). |
| 19 | Đạt | ChiTietHoaDon có LoaiKhoan và FK nguồn (ChiSoDichVuId / PhiDinhKyId / YeuCauBaoTriId) + CHECK đúng một nguồn theo loại. |
| 20 | Đạt | Hình 3.11–3.14 có thanh kích hoạt; mọi mũi tên (trừ mũi tên đầu) đi ra từ thanh kích hoạt; validator kiểm. |
| C1 | Đạt | CSDL + mã đổi theo lược đồ mới (6 migration Tuan7*, đã chạy trên CSDL thật). Hình 4.1–4.7 là ảnh giao diện, không lộ tên cột nên chưa chụp lại. |

Còn lại: bản in vẫn nên xem bằng mắt một lượt trước khi nộp (đặc biệt hình 3.7 trang dọc, chữ nhỏ).
