"""Câu viết lại trong Chương 1, 3, 4 (chữ gốc của nhóm): tách câu dài, bỏ câu thừa, sửa chỗ sai so với CSDL mới.

Mỗi cặp (câu cũ, câu mới) được thay nguyên văn trong đoạn văn và ô bảng (xem sua_bao_cao_v2.fix_text).
Rà lại bằng: python ra_van_phong.py <file.docx> --dai 50
"""

TEN_MOI = [  # tên bảng, cột đổi sau đợt rà soát từ gốc 24/11/2026
    ("LichSuTrangThaiSuCo", "LichSuYeuCau"),
    ("AnhSuCo", "AnhYeuCau"),
    # 25/11/2026: tên cột kiểu máy hoặc sai nghĩa
    ("ToaNhaKhuVucChungId", "ToaNhaId"), ("KetQuaAiJson", "PhanLoaiAi"), ("TinhPhiKhachThue", "TinhPhiCuDan"),
    ("LoaiBaoTri", "LoaiSuCo"), ("TrangThaiSuCo", "TrangThaiYeuCau"), ("KhoaBang", "KhoaDong"),
    ("SoNgayTreHan", "SoNgayAnHan"),
    ("NgayBaoCao", "NgayBaoSuCo"),   # 25/11/2026: tên thuộc tính rõ nghĩa
    # 25/11/2026: số chứng từ tiền tố tiếng Việt
    ("MR-", "YC-"), ("INV-", "HD-"), ("PMT-", "PT-"),
]

CAU = [
    # ---------------------------------------------------------------- Chương 1
    ("Chương này trả lời câu hỏi “tổ chức nào đang gặp vấn đề gì” trước khi nói đến công nghệ: mô tả đơn vị và các "
     "nghiệp vụ vận hành, quy trình trọng tâm là tiếp nhận và xử lý sự cố hiện tại, điểm bất cập, quy trình đề xuất, "
     "quy tắc nghiệp vụ, chỉ số đánh giá, đặc tả yêu cầu sơ bộ và use case sơ bộ.",
     "Chương này mô tả đơn vị và vấn đề đơn vị đang gặp trước khi bàn đến công nghệ. Nội dung gồm các nghiệp vụ vận "
     "hành, quy trình xử lý sự cố hiện tại và bất cập, quy trình đề xuất, quy tắc nghiệp vụ, chỉ số đánh giá, yêu cầu "
     "và use case sơ bộ."),
    ("Trong các nghiệp vụ hằng ngày của BQL, tiếp nhận và xử lý sự cố kỹ thuật (điện, nước, thiết bị, cửa, thang "
     "máy, vệ sinh, côn trùng…) là nghiệp vụ phát sinh liên tục, ảnh hưởng trực tiếp đến an toàn và mức độ hài lòng "
     "của cư dân.",
     "Trong các nghiệp vụ hằng ngày của BQL, xử lý sự cố kỹ thuật (điện, nước, thiết bị, cửa, thang máy, vệ sinh, "
     "côn trùng…) phát sinh liên tục. Nghiệp vụ này ảnh hưởng trực tiếp đến an toàn và sự hài lòng của cư dân."),
    ("BQL quản lý hai khu: Khu 1 gồm Toà A 20 tầng × 8 căn, Toà B 18 tầng × 8 căn, Toà C 15 tầng × 6 căn; Khu 2 gồm "
     "Toà R1, R2 cùng 12 tầng × 6 căn, tổng cộng 5 toà, 77 tầng, 538 căn hộ.",
     "BQL quản lý 5 toà, 77 tầng, 538 căn thuộc hai khu. Khu 1 gồm Toà A (20 tầng × 8 căn), Toà B (18 tầng × 8 căn), "
     "Toà C (15 tầng × 6 căn); Khu 2 gồm Toà R1, R2 (cùng 12 tầng × 6 căn)."),
    ("Đề tài đi theo hướng C – Hệ thống thông tin tích hợp AI trong định hướng của học phần: hệ thống bao quát các "
     "nghiệp vụ vận hành của BQL (cơ cấu khu – toà – căn, cư dân và tài khoản, thu phí và công nợ, sự cố, báo cáo); "
     "quy trình được phân tích sâu là tiếp nhận – xử lý sự cố, và AI chỉ là một bước hỗ trợ phân loại bên trong quy "
     "trình này, luôn có quy tắc nghiệp vụ giới hạn và có phương án khi AI sai hoặc không phản hồi.",
     "Đề tài theo hướng C – Hệ thống thông tin tích hợp AI của học phần. Hệ thống bao quát các nghiệp vụ vận hành "
     "của BQL: cơ cấu khu – toà – căn, cư dân và tài khoản, thu phí và công nợ, sự cố, báo cáo. Quy trình được phân "
     "tích sâu là tiếp nhận – xử lý sự cố. AI chỉ là một bước hỗ trợ phân loại trong quy trình này, bị giới hạn bởi "
     "quy tắc nghiệp vụ và có phương án dự phòng khi AI sai hoặc không phản hồi."),
    ("Mục tiêu tổng quát: xây dựng hệ thống thông tin quản lý vận hành chung cư cho BQL, gồm quản lý cơ cấu khu – toà "
     "– căn và cư dân, thu phí và công nợ, tiếp nhận – xử lý sự cố từ lúc cư dân báo đến lúc cư dân xác nhận và đánh "
     "giá (có AI hỗ trợ phân loại và điều phối), cùng các báo cáo để quản lý ra quyết định.",
     "Mục tiêu tổng quát: xây dựng hệ thống thông tin quản lý vận hành chung cư cho BQL. Hệ thống quản lý cơ cấu khu "
     "– toà – căn và cư dân, thu phí và công nợ, sự cố từ lúc cư dân báo đến lúc cư dân xác nhận (có AI hỗ trợ phân "
     "loại và điều phối), và cung cấp báo cáo cho quản lý."),
    ("Trong phạm vi: toàn bộ các phân hệ được xây dựng và kiểm thử: cơ cấu Khu – Toà – Tầng – Căn; cư dân và tài "
     "khoản (thư mời); phân quyền; thu phí (hoá đơn tháng và phát sinh, thanh toán – duyệt – hoàn tiền, công nợ, phí "
     "trễ, chỉ số điện nước); sự cố và bảo trì; báo cáo (tình trạng căn, tài chính, công nợ phân tuổi, KPI sự cố); "
     "nhật ký hệ thống.",
     "Trong phạm vi: mọi phân hệ đã xây dựng và kiểm thử, gồm cơ cấu Khu – Toà – Tầng – Căn; cư dân và tài khoản "
     "(thư mời); phân quyền; thu phí (hoá đơn, thanh toán, hoàn tiền, công nợ, phí trễ, điện nước); sự cố và bảo trì; "
     "báo cáo; nhật ký hệ thống."),
    ("Về khoa học, đề tài minh hoạ cách đặt một mô hình ngôn ngữ vào quy trình nghiệp vụ theo nguyên tắc “AI đề xuất, "
     "quy tắc quyết định”: đầu ra của AI bị ràng buộc bởi lược đồ JSON, được kiểm tra nhãn và chỉ có quyền nâng mức "
     "ưu tiên.",
     "Về khoa học, đề tài minh hoạ cách đặt mô hình ngôn ngữ vào quy trình nghiệp vụ theo nguyên tắc “AI đề xuất, "
     "quy tắc quyết định”. Đầu ra của AI phải theo lược đồ JSON, được kiểm tra nhãn và chỉ được nâng mức ưu tiên."),
    ("Về thực tiễn, hệ thống cho BQL một nguồn dữ liệu duy nhất về căn hộ, cư dân, thu phí và sự cố, giúp đo được "
     "chất lượng dịch vụ thay vì cảm nhận; các lợi ích về thời gian xử lý mới là dự kiến, chỉ được khẳng định sau khi "
     "đo trên kịch bản thử nghiệm ở Chương 4.",
     "Về thực tiễn, BQL có một nguồn dữ liệu duy nhất về căn hộ, cư dân, thu phí và sự cố, nên đo được chất lượng "
     "dịch vụ thay vì cảm nhận. Lợi ích về thời gian xử lý mới là dự kiến, chỉ khẳng định sau khi đo ở Chương 4."),
    ("P7, P8 là bất cập của nghiệp vụ thu phí, mô tả theo giả định (nguồn số 5 ở Bảng 1.2) vì nhóm chưa khảo sát quy "
     "trình thu phí tại đơn vị thực tế; quy trình trọng tâm được khảo sát và mô hình hoá chi tiết là quy trình sự cố.",
     "P7, P8 là bất cập của nghiệp vụ thu phí, mô tả theo giả định (nguồn số 5 ở Bảng 1.2) vì nhóm chưa khảo sát thu "
     "phí tại đơn vị thực tế. Quy trình được khảo sát và mô hình hoá chi tiết là quy trình sự cố."),
    ("Hoá đơn có vòng đời đơn giản hơn: hoá đơn tháng và hoá đơn phát sinh được lập ở trạng thái Đã phát hành; ghi "
     "nhận hoặc duyệt thanh toán chuyển hoá đơn sang Trả một phần hoặc Đã thanh toán; qua hạn mà còn nợ thì tác vụ "
     "nền chuyển sang Quá hạn và cộng phí trễ (BR-TP-04); quản lý chỉ huỷ được hoá đơn chưa có thanh toán thành công "
     "(BR-TP-05); hoàn tiền làm số đã trả giảm, hoá đơn lùi về Trả một phần hoặc Đã phát hành, và thành Đã hoàn tiền "
     "khi mọi thanh toán đều đã hoàn.",
     "Vòng đời hoá đơn đơn giản hơn. Hoá đơn tháng và hoá đơn phát sinh lập ra ở trạng thái Đã phát hành. Ghi nhận "
     "hoặc duyệt thanh toán chuyển hoá đơn sang Trả một phần hoặc Đã thanh toán. Quá hạn mà còn nợ thì tác vụ nền "
     "chuyển sang Quá hạn và cộng phí trễ (BR-TP-04). Quản lý chỉ huỷ được hoá đơn chưa có thanh toán thành công "
     "(BR-TP-05). Hoàn tiền làm số đã trả giảm: hoá đơn lùi về Trả một phần hoặc Đã phát hành, và thành Đã hoàn tiền "
     "khi mọi thanh toán đều đã hoàn."),
    ("Riêng thành phần AI đã có số đo sơ bộ trên bộ 120 ca kiểm thử tự xây dựng (mô hình qwen3-suco4): đúng loại 93%, "
     "bắt được 21/22 sự cố nguy hiểm, đúng mức ưu tiên 72% (99% lệch tối đa một bậc), đúng nhóm xử lý 39/47 so với "
     "30/47 của mô hình gốc chưa tinh chỉnh.",
     "Riêng thành phần AI đã có số đo sơ bộ trên 120 ca kiểm thử tự xây dựng (mô hình qwen3-suco4): đúng loại 93%, "
     "bắt được 21/22 sự cố nguy hiểm, đúng mức ưu tiên 72% (99% lệch tối đa một bậc). Đúng nhóm xử lý 39/47, so với "
     "30/47 của mô hình gốc chưa tinh chỉnh."),
    # ---------------------------------------------------------------- Chương 3
    ("Chương này là sản phẩm tuần 4, trả lời sáu câu hỏi của bộ mô hình thiết kế: ai được làm gì (use case), điều "
     "kiện và luồng xử lý (đặc tả use case), công việc diễn ra theo nhánh nào (hoạt động), hệ thống có những lớp nào "
     "(lớp), các thành phần tương tác ra sao khi xử lý một yêu cầu (tuần tự) và giao diện, máy chủ, cơ sở dữ liệu, "
     "dịch vụ ngoài kết nối thế nào (kiến trúc).",
     "Chương này là sản phẩm tuần 4, gồm bộ mô hình thiết kế: kiến trúc, use case và đặc tả, sơ đồ hoạt động, sơ đồ "
     "lớp và sơ đồ tuần tự."),
    ("Qua lần đối chiếu cuối, nhóm đã sửa các sai lệch sau: sơ đồ lớp bản đầu thiếu liên kết người tải ảnh; một số "
     "tên use case còn đặt theo cách làm thay vì theo mục tiêu của tác nhân; ba sơ đồ hoạt động có hành động nhận hai "
     "luồng vào mà không qua nút gộp (theo UML đó là chờ cả hai luồng); một thông điệp tuần tự gửi nhầm lớp nhận; ERD "
     "và lược đồ quan hệ có thuộc tính trùng tên, cột lưu trùng và bản số ghi chưa nhất quán (mục 2.1.3).",
     "Qua lần đối chiếu cuối, nhóm đã sửa năm sai lệch. Sơ đồ lớp bản đầu thiếu liên kết người tải ảnh. Một số use "
     "case đặt tên theo cách làm thay vì mục tiêu của tác nhân. Ba sơ đồ hoạt động có hành động nhận hai luồng vào mà "
     "không qua nút gộp. Một thông điệp tuần tự gửi nhầm lớp nhận. ERD và lược đồ quan hệ có thuộc tính trùng tên, "
     "cột lưu trùng và bản số chưa nhất quán (mục 2.1.3)."),
    ("Đến tuần 4, bộ mô hình thiết kế gồm: sơ đồ kiến trúc triển khai; sơ đồ use case tổng quát và năm sơ đồ use case "
     "chi tiết kèm bảng đặc tả; ba sơ đồ hoạt động (AS-IS, TO-BE, luồng AI); sơ đồ trạng thái; hai sơ đồ lớp; bốn sơ "
     "đồ tuần tự; ERD và lược đồ quan hệ ở Chương 2.",
     "Đến tuần 4, bộ mô hình thiết kế gồm: sơ đồ kiến trúc; sơ đồ use case tổng quát và năm sơ đồ use case chi tiết "
     "kèm đặc tả; ba sơ đồ hoạt động; sơ đồ trạng thái; hai sơ đồ lớp; bốn sơ đồ tuần tự. ERD và lược đồ quan hệ ở "
     "Chương 2."),
    ("Bảng chuyển trạng thái của yêu cầu (Bảng 1.7) được kiểm tra ở mức logic trên 56 cặp trạng thái (từ, sang) "
     "khác nhau: đúng 11 cặp được phép và 45 cặp bị chặn; việc bắt đầu xử lý khi chưa có người phụ trách hoặc hoàn "
     "tất khi chưa ghi kết quả cũng bị chặn.",
     "Bảng chuyển trạng thái (Bảng 1.7) được kiểm tra trên đủ 56 cặp trạng thái (từ, sang): 11 cặp được phép, 45 "
     "cặp bị chặn. Bắt đầu xử lý khi chưa có người phụ trách, hoặc hoàn tất khi chưa ghi kết quả, cũng bị chặn."),
    ("Bước kiểm tra của quy trình quyết định nhánh: vi phạm bảng chuyển trạng thái hoặc thiếu kết quả xử lý thì trả "
     "về thông báo lỗi và không lưu gì; hợp lệ thì ghi mốc giải quyết, kết quả và lịch sử trong cùng một giao dịch.",
     "Bước kiểm tra quyết định nhánh. Vi phạm bảng chuyển trạng thái hoặc thiếu kết quả xử lý thì báo lỗi và không "
     "lưu gì; hợp lệ thì ghi kết quả và dòng lịch sử trong cùng một giao dịch."),
    # ---------------------------------------------------------------- Chương 4
    ("Chương này trình bày phần xây dựng và kiểm thử theo tiến độ tuần 5 và tuần 6, đi lần lượt qua các phân hệ của "
     "hệ thống: tài khoản và phân quyền; khu, toà, căn hộ và cư dân; thu phí (hoá đơn, thanh toán, công nợ, điện "
     "nước); sự cố và bảo trì; báo cáo.",
     "Chương này trình bày phần xây dựng và kiểm thử của tuần 5 và tuần 6, theo từng phân hệ: tài khoản và phân "
     "quyền; khu, toà, căn hộ và cư dân; thu phí; sự cố và bảo trì; báo cáo."),
    ("Ứng dụng được xây dựng bằng ASP.NET Core MVC trên .NET 10, truy cập dữ liệu bằng Entity Framework Core 10 với "
     "PostgreSQL 18; mô hình phân loại qwen3-suco4 chạy trên Ollama: thiết kế cho phép đặt mô hình ở một máy riêng có "
     "GPU (Hình 3.1), còn trong môi trường thử Ollama chạy cùng máy với ứng dụng (GPU GTX 1650); thư thông báo gửi qua "
     "SMTP.",
     "Ứng dụng viết bằng ASP.NET Core MVC trên .NET 10, truy cập dữ liệu bằng Entity Framework Core 10 với PostgreSQL "
     "18; thư thông báo gửi qua SMTP. Mô hình phân loại qwen3-suco4 chạy trên Ollama. Thiết kế cho phép đặt mô hình ở "
     "máy riêng có GPU (Hình 3.1); trong môi trường thử, Ollama chạy cùng máy với ứng dụng (GPU GTX 1650)."),
    ("CSDL thử là CSDL của nhóm đã triển khai trước đợt rà soát thiết kế dữ liệu theo nhận xét của giảng viên; lược "
     "đồ mới ở Chương 2 sẽ được đồng bộ sau (mục 2.8), gồm 5 toà, 538 căn, 748 cư dân đang ở và hoá đơn phí các kỳ từ "
     "tháng 8 đến tháng 11/2026.",
     "CSDL thử dùng đúng lược đồ ở Chương 2, gồm 5 toà, 538 căn, 748 cư dân đang ở và hoá đơn phí các kỳ từ tháng 8 "
     "đến tháng 11/2026."),
    ("Phân quyền được kiểm tra ở máy chủ theo hai lớp: vai trò quyết định người dùng vào được khu nào (quản trị, kỹ "
     "thuật viên, cư dân); trong khu quản trị, mỗi phân hệ còn tra bảng VaiTro_Quyen theo mã quyền, trang xem cần "
     "quyền xem, mọi thao tác ghi cần quyền thao tác.",
     "Phân quyền được kiểm tra ở máy chủ theo hai lớp. Vai trò quyết định người dùng vào được khu nào (quản trị, kỹ "
     "thuật viên, cư dân). Trong khu quản trị, mỗi phân hệ còn tra bảng VaiTro_Quyen: trang xem cần quyền xem, thao "
     "tác ghi cần quyền thao tác."),
    ("Kết quả: không trang nào lỗi máy chủ (mã 500), không trang nào phản hồi quá 1,5 giây; khách bị chuyển về trang "
     "đăng nhập; cư dân, kỹ thuật viên vào khu quản trị nhận trang báo không có quyền (mã 403) và vẫn giữ phiên đăng "
     "nhập; cư dân mở hoá đơn hoặc yêu cầu của căn khác, kỹ thuật viên mở việc của người khác đều nhận mã 404; quản "
     "lý mở trang sửa tài khoản hoặc trang phân quyền nhận mã 403.",
     "Kết quả: không trang nào lỗi máy chủ (mã 500) hay phản hồi quá 1,5 giây. Khách bị chuyển về trang đăng nhập. "
     "Cư dân, kỹ thuật viên vào khu quản trị nhận mã 403 và vẫn giữ phiên. Cư dân mở hoá đơn, yêu cầu của căn khác, "
     "hay kỹ thuật viên mở việc của người khác, đều nhận mã 404. Quản lý mở trang sửa tài khoản hoặc phân quyền nhận "
     "mã 403."),
    ("Ngoài phần hiển thị, một tiến trình nền cứ 15 phút tìm các yêu cầu còn mở đã qua hạn mà chưa được báo, gửi một "
     "thư tổng hợp cho quản lý BQL (không có quản lý thì gửi quản trị viên) và ghi một dòng LichSuTrangThaiSuCo do hệ "
     "thống thực hiện, giữ nguyên trạng thái, ghi chú “Quá hạn SLA, hệ thống đã báo quản lý”.",
     "Ngoài phần hiển thị, cứ 15 phút một tiến trình nền tìm các yêu cầu còn mở đã quá hạn mà chưa được báo. Tiến "
     "trình gửi một thư tổng hợp cho quản lý BQL (không có quản lý thì gửi quản trị viên) và ghi một dòng LichSuYeuCau "
     "do hệ thống thực hiện, giữ nguyên trạng thái, ghi chú “Quá hạn SLA, hệ thống đã báo quản lý”."),
    ("Để hai người không ghi đè lên nhau, bảng YeuCauBaoTri dùng khoá lạc quan theo cột hệ thống xmin của PostgreSQL: "
     "lệnh cập nhật chỉ thành công nếu dòng chưa bị ai sửa kể từ lúc đọc; nếu đã bị sửa, hoặc trigger TG1 ở mục 2.5.7 "
     "chặn chuyển trạng thái, cả giao dịch bị huỷ và người thao tác sau nhận thông báo tải lại trang thay vì lỗi máy "
     "chủ. Cột PhienBan trong thiết kế mới là số nguyên tăng sau mỗi lần sửa, có thể thay cho cách này khi đồng bộ mã "
     "nguồn.",
     "Để hai người không ghi đè lên nhau, bảng YeuCauBaoTri dùng khoá lạc quan theo cột hệ thống xmin của PostgreSQL: "
     "lệnh cập nhật chỉ thành công nếu dòng chưa bị ai sửa kể từ lúc đọc. Nếu dòng đã bị sửa, hoặc trigger TG1 (mục "
     "2.5.7) chặn chuyển trạng thái, cả giao dịch bị huỷ và người thao tác sau nhận thông báo tải lại trang thay vì "
     "lỗi máy chủ."),
    ("Hệ thống có bảng điều khiển cho quản lý (Hình 4.5) và bốn báo cáo: tình trạng căn theo toà (đang ở, cho thuê, "
     "trống, chưa bàn giao, sửa chữa, còn phải thu), tài chính (phát hành, đã thu, công nợ, thanh toán chờ duyệt, "
     "doanh thu sáu tháng), công nợ phân tuổi (Hình 4.6) và KPI sự cố (Hình 4.7).",
     "Hệ thống có bảng điều khiển cho quản lý (Hình 4.5) và bốn báo cáo: tình trạng căn theo toà, tài chính, công nợ "
     "phân tuổi (Hình 4.6) và KPI sự cố (Hình 4.7). Báo cáo tình trạng căn đếm căn đang ở, cho thuê, trống, chưa bàn "
     "giao, đang sửa chữa và số còn phải thu; báo cáo tài chính gồm số phát hành, đã thu, công nợ, thanh toán chờ "
     "duyệt và doanh thu sáu tháng."),
    ("Trong lúc đối chiếu, nhóm phát hiện và sửa thêm ba lỗi: trang hoá đơn của quản lý không hiện dòng phí trễ nên "
     "các dòng cộng không ra tổng; báo cáo tuổi nợ tính số ngày trễ theo ngày UTC; ô số tiền trên báo cáo tuổi nợ bị "
     "cắt khi số dài.",
     "Trong lúc đối chiếu, nhóm phát hiện và sửa thêm ba lỗi. Trang hoá đơn của quản lý không hiện dòng phí trễ nên "
     "các dòng cộng không ra tổng. Báo cáo tuổi nợ tính số ngày trễ theo ngày UTC. Ô số tiền trên báo cáo tuổi nợ bị "
     "cắt khi số dài."),
    ("Các lỗi mức trung bình và thấp cũng đã được sửa, phần lớn thuộc các phân hệ ngoài sự cố: không hoàn tiếp được "
     "sau một lần hoàn một phần;",
     "Các lỗi mức trung bình và thấp cũng đã được sửa, phần lớn thuộc các phân hệ ngoài sự cố. Đó là: không hoàn "
     "tiếp được sau một lần hoàn một phần;"),
    ("Tuần 6: phân quyền chi tiết theo bảng VaiTro_Quyen; mọi phân hệ chạy được trọn nghiệp vụ trên CSDL của nhóm – "
     "lập toà và căn, quản lý cư dân, lập hoá đơn và thu tiền, công nợ và phí trễ, điện nước, sự cố từ lúc báo đến "
     "lúc đánh giá kể cả khi không có AI; các ngoại lệ và ràng buộc được chặn ở máy chủ; hai người cùng thao tác "
     "không ghi đè nhau; cảnh báo quá hạn gửi thư cho quản lý; các báo cáo khớp với dữ liệu.",
     "Tuần 6: phân quyền chi tiết theo bảng VaiTro_Quyen. Mọi phân hệ chạy trọn nghiệp vụ trên CSDL của nhóm, kể cả "
     "sự cố khi không có AI. Ngoại lệ và ràng buộc được chặn ở máy chủ; hai người cùng thao tác không ghi đè nhau; "
     "yêu cầu quá hạn có thư cảnh báo cho quản lý; báo cáo khớp dữ liệu."),
]


# 25/11/2026: cắt gọn báo cáo và sửa chỗ còn sai so với CSDL mới (RaSoat_DoDai_BaoCao_20261125.md, mục A)
CAU += [
    ("Bảng 1.10 nêu công thức và nguồn dữ liệu; cách tính từ dữ liệu ở mục 2.7.",
     "Bảng 1.10 nêu công thức và nguồn dữ liệu."),
    ("CSAT (cách tính ở mục 2.7)", "CSAT (cách tính ở Bảng 1.10)"),
    (" Bảng 3.9 mô tả ý nghĩa của các thao tác trên sơ đồ lớp.", ""),
    ("Kèm giao việc; ghi mốc tiếp nhận", "Kèm giao việc; ghi dòng lịch sử"),
    ("NgayBaoCao, mốc giải quyết", "NgayBaoCao, LichSuYeuCau"),
    ("Mốc tiếp nhận (từ lịch sử)", "LichSuYeuCau"),
    ("Mốc giải quyết (từ lịch sử), HanXuLy", "LichSuYeuCau, HanXuLy"),
    ("4. Hệ thống ghi mốc giải quyết, lưu lịch sử;", "4. Hệ thống lưu kết quả và dòng lịch sử;"),
    ("chuyển Đã tiếp nhận, ghi mốc tiếp nhận và lịch sử;", "chuyển Đã tiếp nhận và ghi lịch sử;"),
    ("UC06: Đã đóng, có mốc đóng và điểm đánh giá. UC06b: quay lại Đang xử lý, bỏ mốc giải quyết.",
     "UC06: Đã đóng, có dòng lịch sử đóng và điểm đánh giá. UC06b: quay lại Đang xử lý, lý do ghi vào lịch sử."),
    ("Trung bình DanhGia (1–5)", "Trung bình DiemDanhGia (1–5)"),
    ("Khung nhìn công nợ hoá đơn", "HoaDon"),
    ("với vai trò Manager", "với vai trò Quản lý"),
    ("Mô hình trả về loại plumbing, mức medium, không nguy hiểm",
     "Mô hình trả về loại Cấp/thoát nước, mức Trung bình, không nguy hiểm"),
]
