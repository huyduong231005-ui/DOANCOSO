# Danh sách yêu cầu sửa báo cáo DACN (tài liệu theo dõi nội bộ, không đưa vào báo cáo)

Mỗi lần sửa báo cáo: đọc lại toàn bộ danh sách này, rà **từng chương** theo từng dòng, ghi kết quả vào cột Trạng thái.
Một yêu cầu nêu cho một chỗ (ví dụ "bỏ cột biểu diễn ở Bảng 2.3") phải áp cho **mọi chỗ cùng loại** nếu lý do của
người dùng áp được (ở đây: "quá trừu tượng, ưu tiên dễ hiểu").

## A. Nguyên tắc chung (áp mọi chương)

| # | Yêu cầu | Nguồn |
|---|---|---|
| A1 | Báo cáo là nhóm báo cáo với thầy (người với người): không đưa quy ước, thoả thuận giữa người dùng và Claude, quy trình làm việc nội bộ, câu "theo nhận xét của giảng viên…" vào báo cáo | Người dùng 25/11 |
| A2 | Ưu tiên dễ hiểu: bỏ biểu diễn hình thức (∀, ∃, ⇒…), bỏ chi tiết kỹ thuật trừu tượng khi lời thường nói được | Người dùng 25/11 |
| A3 | Không còn quy ước đặt tên trong báo cáo (bảng quy ước, câu "tên… là…", "bỏ tiền tố CK_", "bỏ hậu tố Id"…) | Người dùng 25/11 |
| A4 | Văn phong: ngắn gọn, giống người viết, một ngôi "nhóm"; câu nào bỏ đi mà không mất nghĩa thì bỏ | Người dùng 24/11 |
| A5 | Tên trong bảng, sơ đồ đọc hiểu ngay; tên CSDL ngắn, không hậu tố máy móc | Người dùng 25/11 |
| A6 | Không ghi "Luxe Haven" trong báo cáo | Người dùng |
| A7 | Chuyên ngành HTTT: bớt phần CSDL không cần thiết | Người dùng 25/11 |
| A8 | Trích dẫn dễ hiểu: không dồn số [x], [y] liền nhau; số đi kèm tên tài liệu | Người dùng 25/11 |
| A9 | Đồng bộ mọi chỗ liên quan khi xoá/sửa một phần (tham chiếu mục, bảng, hình; kế hoạch; cấu trúc đồ án; Chương 1, 3, 4) | Người dùng 25/11 |

## B. Góp ý tuần 6 của thầy (vẫn giữ)

| # | Yêu cầu |
|---|---|
| B1 | Sơ đồ đặt trang ngang, không xoay ảnh; không ghi "vẽ bằng StarUML" ở chú thích |
| B2 | Người dùng và cư dân là một; căn hộ – người là quan hệ cư trú, không phải hợp thành |
| B3 | Activity: mỗi ô một hành động cụ thể; trước nút quyết định là hành động kiểm tra; nút quyết định là câu hỏi |
| B4 | Sequence: mọi mũi tên (trừ mũi tên đầu) đi ra từ thanh kích hoạt |
| B5 | Use case đặt tên theo mục tiêu tác nhân; không có UC kiểu "hỏi lại AI", "ghi nhận yêu cầu"; không kèm bảng quan hệ UC |
| B6 | Không đưa mã trigger, mã AI; không minh hoạ dữ liệu |
| B7 | Một thông tin chỉ lưu ở một bảng; không trùng tên thuộc tính máy móc |

## C. Cơ sở dữ liệu

| # | Yêu cầu |
|---|---|
| C1 | Lọc toàn bộ CSDL: bỏ bảng/cột thừa, lặp; tên cột ngắn, dễ đọc |
| C2 | LoaiDichVu, ChiSoDichVu, PhiDinhKy không trùng thông tin; phân rõ vai trò từng bảng thu phí |
| C3 | ChiTietHoaDon là bảng chi tiết chuẩn (khoá ghép hoá đơn + loại dịch vụ) |
| C4 | Phí quản lý một giá chung trong bảng giá (bỏ đơn giá theo toà) |
| C5 | Không gộp cư trú vào người dùng (đã trả lời lý do) |
| C6 | Gộp bảng Tầng vào Căn hộ: căn ghi ToaNhaId + SoTang (tầng không có thông tin riêng); CSDL còn 20 bảng |

## D. Chương 2

| # | Yêu cầu |
|---|---|
| D1 | Bỏ Bảng 2.2 "Ba quy ước đặt tên" |
| D2 | Một ERD tổng, một RD tổng; cột rút gọn; RD không ghi kiểu dữ liệu; RD vẽ rộng |
| D3 | Bỏ cột Biểu diễn ở bảng ràng buộc (Bảng 2.3 và cả Bảng 2.4) |
| D4 | Bỏ mục 2.6 "Kiểu dữ liệu và bảng mã" (giữ Bảng trigger và hàm) |
| D5 | Bảng 2.1: tên các dòng thống nhất một kiểu |

## E. Use case, sơ đồ lớp, Chương 1, 3

| # | Yêu cầu |
|---|---|
| E1 | Use case chỉ trình bày một lần (mục 1.8); chỉ use case trọng tâm UC03, UC16 có hình chi tiết và đặc tả |
| E2 | Hình UC03, UC16 phân rã tới chức năng lá; đặc tả bỏ phần hình đã thể hiện |
| E3 | Một sơ đồ lớp tổng |
| E4 | Đoạn kỹ thuật mục 1.1.3 viết lại dễ hiểu |

## Kết quả rà ngày 25/11/2026 (tối)

Đã đọc toàn văn từng chương theo danh sách; mọi mục A1–E4 đạt trên bản dựng mới. Chỗ sót đã sửa trong lượt này:
- D3: Bảng 2.4 còn cột Biểu diễn → bỏ; câu mở mục 2.5 không còn nhắc "biểu diễn hình thức".
- A2: Bảng 2.6 viết lại bằng lời (bỏ "BEFORE UPDATE OF", tên hàm, ký hiệu ≤).
- D4/A7: mục 2.3 còn câu kiểu dữ liệu (numeric, varchar…) và câu khoá số nguyên tự tăng → bỏ.
- D5: Bảng 2.1 dòng gộp "VAI_TRO, QUYEN" → tách hai dòng.
- A1: đoạn sơ đồ tuần tự còn luật "thanh kích hoạt"; câu "bội số lấy theo khoá ngoại" → bỏ.
- A9: câu "chạy thử … thuộc phần việc tiếp theo" (Chương 3) đã cũ → bỏ; danh mục viết tắt bỏ GUID, HTTT, LLM, RD (không còn dùng).

## Kết quả rà Chương 4 ngày 25/11/2026 (đêm)

Đọc toàn văn Chương 4, đối chiếu từng câu với mã nguồn, từng số với CSDL (truy vấn chỉ đọc), chụp lại và xem từng hình.
- Số liệu đúng: 5 toà, 538 căn, 748 cư dân, kỳ 8–11/2026; giá quản lý 15.000 đ/m², xe máy 120.000 đ; hạn = ngày lập + 7;
  khoá 5 lần/5 phút; phiên hết hiệu lực sau 1 phút; quét quá hạn 15 phút; Bảng 4.5 khớp lịch sử YC-202611-0001.
- Bảng 4.7 đổi sang ngày 25/11 (khớp ngày chụp Hình 4.5–4.7); nhóm tuổi nợ đổi: 1–30 ngày = 0, 31–60 = 316.048.734.
  Mọi số trên app (bảng điều khiển, tình trạng căn, tài chính, tuổi nợ, KPI) khớp SQL.
- Sai với mã nguồn → sửa: phân loại lại chỉ tính lại hạn khi đổi mức ưu tiên (Bảng 4.6).
- A2/A5: bỏ prompt, tham số sinh, token, xmin, KetQuaXuLy, "tem bảo mật"; Bảng 4.2 ghi tên quyền tiếng Việt thay mã
  projects.view…; UTC giải thích bằng lời; bỏ câu thừa về tên miền email thử; câu "đổi loại thành" lặp → gọn.
- Đồng bộ tên trạng thái: giao diện đổi theo báo cáo (Đang xử lý, Đã xử lý, Từ chối); Bảng 1.10 "Mới gửi, Đã tiếp nhận".
- Giao diện (thấy khi xem ảnh): "Quản lí"→"Quản lý", "tòa"→"toà", dòng phí quản lý lên đầu hoá đơn, dòng tổng thẳng cột,
  kỳ ghi thống nhất 8/2026, nhãn trạng thái không xuống dòng, trục biểu đồ "600tr" (bỏ ".0"), "còn dư/Số dư"→"còn nợ",
  câu báo lỗi "đã Đã huỷ"; Hình 4.1 chụp khung rộng hơn cho đủ cột; ô ngày Hình 4.7 hiện dd/mm/yyyy.

## Rà toàn báo cáo ngày 26/11/2026

Quét tự động mọi mục A–E và đọc toàn văn Chương 1–4; xem lại các trang sơ đồ trên PDF. Sửa thêm:
- A5: Chương 1 còn tên bảng máy trong câu (NguoiDung, CuTru) và trong cột công thức Bảng KPI (HanXuLy, NgayBao,
  PhutPhanHoi, DiemDanhGia, HanThanhToan) → viết bằng lời; cột "Dữ liệu dùng" vẫn ghi tên bảng.
- A9: bảng kế hoạch theo tuần: tuần 6 → Hoàn thành, tuần 7 → Đang thực hiện.
- A9: Chương 3 nói "Chương 4 trình bày đánh giá AI trên 120 ca" trong khi Chương 4 ghi việc này làm ở tuần 7–9 → bỏ.
Không còn: Luxe Haven, quy ước đặt tên, cột Biểu diễn, mã trigger/SQL, ký hiệu ∀∃⇒, trích dẫn dồn số, chữ "lí/tòa".

## Rà mọi chương lần 2 ngày 26/11/2026 (sau câu hỏi về 2a, E1)

Tiêu chí thêm: mọi ký hiệu, mã số người đọc phải hiểu ngay, không cần biết quy ước riêng. Xem cả 80 trang PDF bằng mắt.
- Đặc tả UC03, UC16: "2a, 4a…" → "Ở bước 2, nếu…"; "E1–E4" → bỏ mã, ghi thẳng tình huống lỗi.
- Bảng 1.8: thêm câu nói rõ mã RB, LB, TG, FN là ràng buộc, trigger, hàm ở mục 2.5.
- Bảng kế hoạch "câu hỏi Bước 1" → "câu hỏi nghiệp vụ (Bảng 1.1)"; Bảng 3.1 "q4_k_m", "ghi log", "nhắc hạn" → lời thường;
  mã 403, 404 giải thích lần đầu; tác vụ nền ở Bảng 1.3 thêm việc báo yêu cầu quá hạn (khớp mục 4.5.3).
- Trình bày: 11 dòng gạch đầu dòng mất tab (chữ dính vào dấu gạch, lệch lề) → sửa trong pipeline (sua_gach_dau_dong);
  Hình 4.5, 4.6 giới hạn cao 10 cm để chung một trang (trước mỗi hình bỏ trống nửa trang). Báo cáo còn 79 trang.

## Gộp Tầng vào Căn hộ ngày 26/11/2026 (C6)

- CSDL: migration Tuan9GopTangVaoCanHo (sao lưu db_backup/LuxeHavenBql_truoc_gop_tang_20261126.dump); thử trên bản sao
  rồi áp bản thật. Toà, tầng của cả 538 căn khớp trước/sau; tổng tiền, công nợ hoá đơn không đổi; thêm CHECK SoTang ≥ 1.
- Ứng dụng: bỏ lớp Floor; form căn nhập số tầng; sơ đồ toà nhóm căn theo số tầng; tạo toà sinh căn từng tầng.
- Sơ đồ: ERD bỏ TANG (toà – "có căn" – căn, CAN_HO thêm SoTang), RD bỏ bảng Tang (CanHo.ToaNhaId), sơ đồ lớp bỏ
  lớp Tang; đường mới đi vòng, không cắt hình nào. Validator 0 lỗi.
- Báo cáo: 15 thực thể, 20 bảng; Bảng 2.1 bỏ dòng TANG; mục 2.4 giải thích vì sao tầng là cột; RB01 thêm số tầng;
  BR-CH-01 viết lại; câu bản số, sơ đồ lớp, mục 4.3, Bảng 4.3 sửa theo.

## 26/11/2026 (sau khi người dùng tự sửa trong Word)

- Người dùng sửa tay file .docx và lưu: bỏ "(P1)…(P8)" ở cột Hình thức của Bảng 1.6. Đã đưa vào gon_bao_cao.SUA_SAU.
- Đoạn dưới lược đồ quan hệ (mục 2.3) thay bằng bản gọn: bỏ câu liên kết thường/hợp thành (trùng mục 3.3) và tên 7 cột.
- Mục 4.6 chỉ nói kết quả đối chiếu; ba lỗi tìm khi đối chiếu (18/11) chuyển sang đoạn lỗi mức trung bình, thấp ở 4.7.
- Cách làm: sửa thẳng vào file người dùng đã lưu (chỉ thay chữ đúng đoạn), rồi đưa chỗ sửa vào công cụ dựng; dựng
  thử ra thư mục tạm và so phần thân bài với file người dùng: giống hệt.

## Rà sơ đồ, gọn sơ đồ tuần tự, bỏ tổng kết tuần cũ ngày 26/11/2026

- Sơ đồ trạng thái: hai mũi tên chéo nhau không có nhãn (Mới gửi → Đã huỷ, Đã tiếp nhận → Từ chối) → đặt Đã huỷ bên
  trái, Từ chối bên phải, mũi tên nào cũng có nhãn, không còn đường cắt nhau; nhãn mũi tên dọc đẩy ra khỏi đường kẻ.
- Sơ đồ triển khai: tên lớp tiếng Anh (MaintenanceWorkflow, IEmailSender…) → tên tiếng Việt; dịch vụ nền đủ 4 việc;
  bỏ q4_k_m, "/api/chat".
- Sơ đồ hoạt động: nhãn điều kiện đẩy ra khỏi đường (công cụ vẽ có thêm labelDistance); nhãn nhánh trái còn chạm nhẹ.
- Sơ đồ tuần tự: chỉ giữ 3 (báo sự cố có AI, KTV hoàn tất, duyệt chuyển khoản); bỏ sơ đồ điều phối (đã có ở sơ đồ
  hoạt động Hình 3.2).
- Bỏ mục 3.6 "Kết quả tuần 4 và việc tiếp theo"; câu kiểm tra 56 cặp trạng thái chuyển sang mục 4.5.2. Chỉ giữ tổng
  kết tuần mới (mục 4.8). Báo cáo còn 78 trang.

## Sửa nhãn sơ đồ hoạt động và logic UC03 ngày 26/11/2026

- UC03: UC08 Điều phối luôn "Tìm kỹ thuật viên phù hợp" («include»); "Giao việc cho kỹ thuật viên" là «extend» (chỉ khi
  có thợ, mô tả rõ) và luôn kèm «include» "Thông báo giao việc". Câu mô tả Hình 1.7 sửa theo.
- Sơ đồ hoạt động: nhãn điều kiện gắn ở đầu nhánh, sát hình thoi (nhánh ngang: trên đường; nhánh xuống: bên phải);
  câu hỏi của nút quyết định tự chọn chỗ trống trong làn, không đè hộp, đường hay vạch chia làn. Hình 3.2 nới làn
  hệ thống và dời "Giữ lựa chọn cư dân" xuống một hàng.

## Rà toàn báo cáo lần 3 ngày 26/11/2026

Quét lại mọi mục A–E: không còn vi phạm. Kiểm mọi tham chiếu Hình/Bảng/mục: không có số trỏ sai.
- 17 bảng, hình không được câu văn nào nhắc tới (Bảng 1.3, 1.6, 1.11–1.16, Hình 2.1, Bảng 3.1, 4.1–4.4, 4.6, Hình 4.4,
  Bảng 4.8) → thêm câu dẫn (gon_bao_cao.dan_bang_hinh, chạy sau khi đánh số lại).
- Bảng 3.1 "Hai tác vụ chạy định kỳ" lệch với sơ đồ triển khai (bốn dịch vụ nền) → sửa; Bảng 1.3 thêm nhắc hạn
  thanh toán; tầng nghiệp vụ thêm lập hoá đơn.
- Mục 1.10: Chương 5 ghi rõ chưa có trong báo cáo này.
- Trình bày: Hình 4.7 giới hạn cao để chung trang với Hình 4.6. Báo cáo còn 77 trang.

## Rà toàn báo cáo lần 4 ngày 26/11/2026

Đối chiếu chéo mã (BR, FR, NFR, RB, LB, TG, FN, UC): mọi mã dùng đều có định nghĩa; số trạng thái (8), cặp được phép (11/56)
khớp Bảng 1.7 và Hình 1.5. Đọc lại toàn văn. Sửa:
- Mục 1.4.1 nói Hình 1.4 có nhãn [AI], [Tự động] nhưng hình không có → viết lại theo đúng hình.
- Hình 1.5 dùng CD, QL, HT chưa giải thích → thêm chú thích ký hiệu.
- Đoạn mở Bảng 1.8 nhắc "mục 2.5" hai lần → gộp; Bảng 1.10 "TB" → "trung bình".
- Mục 2.5.3 nói hàm FN1 "kiểm tra rồi chấp nhận hoặc từ chối" (sai: FN1 chỉ tính hạn) → sửa.

## Kiểm PDF ngày 27/11/2026

- Phông: chỉ Times New Roman thường, nghiêng, đậm, đều nhúng; không có ký tự lỗi, ô vuông, phông ký hiệu.
- Lớp chữ: PyMuPDF đọc dính chữ sau một số chữ có dấu, nhưng pdftotext và pdfminer (cách trình xem PDF đọc) ra đủ dấu
  cách → không phải lỗi file.
- Bố cục: trang chỉ có 2 dòng cuối Chương 3 → Hình 3.6 giới hạn cao (CAO_TOI_DA áp cả hình Chương 3). Còn 76 trang.
  Các trang trống nửa dưới còn lại đều đứng trước trang ngang hoặc trước chương mới.

## Rà lần 5 ngày 27/11/2026 (đọc kĩ nội dung từng bảng)

Đọc toàn bộ 32 bảng; đối chiếu Bảng 2.3 với CHECK trong CSDL (khớp); thử thật trên ứng dụng dòng "số tầng 0" của
Bảng 4.3 (máy chủ chặn, không lưu). Sửa:
- Bảng 1.11 FR-02: tiêu chí "hai căn cùng mã trong một toà" cũ (mã căn nay duy nhất toàn hệ thống) → "trùng mã; số tầng ≥ 1".
- Bảng 1.16 tuần 4: "activity, class, sequence" → sơ đồ hoạt động, lớp, tuần tự.
- Bảng 2.7: "ba trigger cho ràng buộc cần dữ liệu nhiều dòng" (TG1 chỉ xét một dòng) → viết lại; cột Mục: tính nhất quán
  thêm 4.5.4 (khoá lạc quan), tính độc lập trỏ 3.1 (lớp ánh xạ) thay vì 2.3.

## Rà lần 6 ngày 27/11/2026 (đối chiếu chữ trong sơ đồ với bài viết)

- Mục 3.5 khẳng định "mỗi use case có đặc tả" (chỉ UC03, UC16 có) và "mỗi thông điệp tuần tự gọi thao tác của lớp
  nhận" (đối tượng trong sơ đồ tuần tự là bộ phận xử lý, không phải lớp của sơ đồ lớp) → viết lại cho đúng phạm vi.
- Mục 3.4 thêm câu giải thích đối tượng có dấu “:” là bộ phận xử lý của ứng dụng (Hình 3.1).
- Bảng 1.11 FR-03: tác nhân thêm Cư dân (UC02 kích hoạt tài khoản do cư dân làm). Thêm hàm sua_o_theo_dong.
- Câu thêm ở 3.4 đẩy Hình 3.4 sang trang riêng → rút câu, giới hạn cao Hình 3.4 (CAO_TOI_DA["Hinh 3.11"]). Vẫn 76 trang.

## Rà lần 7 ngày 27/11/2026 (danh mục viết tắt, tài liệu tham khảo)

- Mọi chữ viết tắt trong danh mục đều có dùng trong bài; mọi tài liệu đều được trích ít nhất một lần.
- Tài liệu tham khảo chưa theo ABC và xen tiếng Việt với tiếng Anh → xếp tiếng Việt trước (Bộ Xây dựng, Quốc hội),
  tiếng nước ngoài sau, mỗi nhóm theo ABC; đánh số lại 18 trích dẫn (gon_bao_cao.xep_tai_lieu), kiểm từng trích dẫn
  vẫn trỏ đúng tài liệu.

## Rà lần 8 ngày 27/11/2026 (ERD: quan hệ nào thật sự dùng; công cụ kiểm lỗi vòng lặp)

- Đối chiếu từng khoá ngoại trong ERD với mã nguồn: ToaNha.QuanLyId ("quản lý toà") chỉ được chọn và hiển thị, thư
  báo sự cố gửi cho mọi tài khoản vai trò Quản lý, không quy tắc nào dùng; AnhYeuCau.NguoiTaiId chỉ được ghi, suy ra
  được (ảnh lúc báo = người báo, ảnh nghiệm thu = người ở dòng lịch sử "Đã xử lý") → bỏ cả hai (migration
  Tuan9BoCotKhongDung, sao lưu trước, thử bản _thu rồi mới chạy bản thật; tổng hoá đơn không đổi).
- Sơ đồ: bỏ mối kết hợp "quản lý toà", "tải ảnh" (ERD) và quanLy, nguoiTai (sơ đồ lớp); lược đồ quan hệ tự cập nhật.
- Chữ: Bảng 2.1 lý do TOA_NHA; hai câu mục 3.3 về quanLy và "người tải lên".
- Thêm tools/kiem_loi_bao_cao.py (chạy --lap tới khi 0 lỗi) và QuyTrinh_SuaLoi.md.

## Rà lần 9 ngày 27/11/2026 (lỗi nghiệp vụ, logic trong các sơ đồ)

Đối chiếu từng hình với mã nguồn (bảng chuyển trạng thái, bộ điều phối, controller, dịch vụ nền). Sửa:
- Code: quản lý giao việc bằng tay không gửi thư cho thợ (chỉ khi hệ thống tự giao mới gửi), trái với UC11 →
  thêm MaintenanceDispatcher.NotifyAssigneeAsync, gọi sau khi giao; thêm test (126/126 đạt).
- Hình 1.6: UC09 còn ghi "tầng" dù tầng đã gộp vào căn → "khu – toà – căn hộ"; thêm UC11 – Dịch vụ email và
  UC16 «include» UC15 cho khớp sơ đồ chi tiết. Chữ "Khu – Toà – Tầng – Căn" sửa theo (mục 1.x, FR-02, 4.x).
- Hình 1.7 (UC03): thiếu nhánh mô tả mơ hồ → thêm "Báo quản lý hỏi lại cư dân"; chữ mô tả sửa theo.
- Hình 1.8 (UC16): "Bắt đầu", "Hoàn tất" là «include» (nghĩa là lần nào cũng làm) → «extend».
- Hình 1.5: "CD/QL: xác nhận, đánh giá" nhưng quản lý chỉ đóng, không chấm điểm → "CD: xác nhận, đánh giá; QL: đóng".
- Hình 2.1, 3.3: nhật ký hệ thống thiếu người thao tác → thêm NguoiThaoTacId (không phải khoá ngoại).
- Hình 3.3: hai liên kết căn hộ / toà của yêu cầu loại trừ nhau (CHECK trong CSDL) → ghi chú {xor}.
- Hình 3.1: dịch vụ nền lập hoá đơn qua tầng nghiệp vụ và nạp sẵn mô hình AI → thêm hai «use»; sắp lại bố cục.
- Hình 3.2: thiếu nhánh khẩn cấp (đã giao thợ vẫn báo quản lý theo dõi) → thêm; giới hạn cao để không nhảy trang.
- Hình 3.6 (tuần tự duyệt): điều kiện "hoá đơn đã huỷ" thiếu nháp, đã hoàn tiền → ghi đủ.
- Sơ đồ chi tiết UC06, UC11, UC20 (không in trong báo cáo): đường cắt qua use case khác, nhãn chồng → sắp lại.
Ghi nhận, chưa sửa: hoá đơn phí sửa chữa sinh từ yêu cầu sự cố chỉ ghi số yêu cầu trong ghi chú, không có khoá
ngoại nối hai bảng.

## Hoá đơn phí sửa chữa nối với yêu cầu sự cố (27/11/2026)

- Trước: hoá đơn phí sửa chữa chỉ ghi số yêu cầu trong ghi chú; mở lại yêu cầu rồi hoàn tất lần nữa thì lập thêm hoá
  đơn thứ hai (thu tiền hai lần).
- Sửa: HoaDon thêm khoá ngoại YeuCauBaoTriId (NULL với hoá đơn khác) và UNIQUE có điều kiện
  UX_HoaDon_SuaChua_MoiYeuCau (mỗi yêu cầu một hoá đơn chưa huỷ); InvoiceGenerator kiểm tra trước khi lập.
  Migration Tuan9HoaDonNoiYeuCauSuCo: sao lưu, chạy bản _thu, rồi bản thật; tổng hoá đơn không đổi.
- Thử thật trên localhost: hoàn tất có tính phí → mở lại → hoàn tất lần nữa: lần hai báo "đã có hoá đơn sửa chữa",
  chỉ một hoá đơn nối với yêu cầu; dữ liệu thử đã xoá. Thêm test (127/127 đạt).
- Sơ đồ: ERD thêm mối kết hợp "phát sinh phí", sơ đồ lớp thêm phiSuaChua, lược đồ quan hệ thêm đường (cắt đường
  toà – căn một lần, không tránh được với bố cục hiện tại, ghi ngoại lệ trong kiem_tra_so_do). Chữ: mục 2.2, 3.3,
  BR-SC-14.
- Quy trình: sua_bao_cao_v2.py tự dừng nếu file Word khác mốc (bị sửa tay); cap_nhat_word.ps1 tự lưu mốc sau khi xuất.

## Rà lần 10 ngày 27/11/2026 (đọc lại toàn bài sau các đợt sửa)

- Lời cảm ơn: thiếu "để" ("mô hình hoá dữ liệu để xây dựng").
- Bảng 1.3, Bảng 3.1: Dịch vụ email còn gửi thư mời cư dân, nhắc hạn thanh toán (đặt lại mật khẩu) → bổ sung.
- Bảng 2.2: HoaDon còn do quản lý lập (hoá đơn phát sinh) và ghi yêu cầu sinh ra phí sửa chữa → bổ sung.
- Bảng 1.16 tuần 4: sơ đồ tuần tự ghi "điều phối" nhưng Chương 3 là báo sự cố, hoàn tất, duyệt chuyển khoản → sửa.
- Mục 3.5: một câu thêm ở bước thay chữ sớm làm câu sửa của rà lần 6 không còn khớp, câu sai cũ ("mỗi thông điệp
  tuần tự gọi thao tác có trong lớp nhận") quay lại → sửa đúng chỗ; thêm "khoá ngoại hoặc bảng trung gian".
- Mục 4.7: ghi hai lỗi nghiệp vụ tìm được ngày 27/11 và số kiểm thử 127.
- Bố cục: giới hạn cao ERD, Hình 3.2, Hình 3.6 để không nhảy trang; vẫn 76 trang.
- Công cụ: bước dựng dừng khi câu thay của DAN_THAY không còn khớp hoặc ô bảng cần sửa không tìm thấy; in danh sách
  câu SUA_SAU không gặp; tự lưu mốc ngay sau khi ghi file (tránh báo nhầm "sửa tay" khi chưa xuất PDF).
