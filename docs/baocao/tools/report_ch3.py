"""Chương 3 – Thiết kế hệ thống (sản phẩm tuần 4). Tách khỏi build_report.py cho gọn.

Số liệu kiểm tra nhất quán lấy trực tiếp khi sinh báo cáo (chạy kiem_tra_uml.py và test_kiem_tra_uml.py);
kết quả dotnet test ghi theo lần chạy ngày 25/10/2026.
"""
import re
import subprocess
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
IMG = HERE.parent / "images"


def consistency_results():
    """(dòng thống kê, số lỗi, số ghi chú, dòng tổng kết mutation test) từ hai script kiểm tra."""
    run = lambda f: subprocess.run([sys.executable, str(HERE / f)], capture_output=True, text=True,  # noqa: E731
                                   encoding="utf-8", cwd=HERE)
    check = run("kiem_tra_uml.py")
    lines = check.stdout.splitlines()
    errors = int(re.search(r"LỖI: (\d+)", check.stdout).group(1))
    notes = int(re.search(r"GHI CHÚ: (\d+)", check.stdout).group(1))
    mutation = run("test_kiem_tra_uml.py").stdout.strip().splitlines()[-1]
    return lines[0], errors, notes, mutation


def chapter3(r):
    r.h1("CHƯƠNG 3. THIẾT KẾ HỆ THỐNG", chapter=3)
    r.p("Chương này là sản phẩm tuần 4, trả lời sáu câu hỏi của bộ mô hình thiết kế: ai được làm gì (use case), điều "
        "kiện và luồng xử lý (đặc tả use case), công việc diễn ra theo nhánh nào (hoạt động), hệ thống có những lớp nào "
        "(lớp), các thành phần tương tác ra sao khi xử lý một yêu cầu (tuần tự) và giao diện, máy chủ, CSDL, dịch vụ "
        "ngoài kết nối thế nào (kiến trúc). Mọi sơ đồ được vẽ bằng StarUML trong cùng tệp staruml/BaoCao_DACN.mdj với "
        "các sơ đồ ở Chương 1 và Chương 2. Tên lớp, phương thức, tham số và nhánh rẽ lấy đúng theo mã nguồn; mục 3.6 "
        "trình bày cách nhóm kiểm tra tự động sự nhất quán giữa mô hình, mã nguồn và CSDL.")
    architecture(r)
    use_cases(r)
    activity(r)
    classes(r)
    sequences(r)
    consistency(r)
    week4_result(r)


# ---------------------------------------------------------------- 3.1
def architecture(r):
    r.h2("3.1. Kiến trúc hệ thống")
    r.p(f"{r.next_fig()} là sơ đồ triển khai. Hệ thống gồm bốn máy (nút) và một trình duyệt phía người dùng. Ứng dụng "
        "ASP.NET Core MVC chạy trên một máy chủ, chia ba tầng trong cùng một tiến trình; mô hình AI chạy trên máy có GPU "
        "riêng và chỉ được gọi qua HTTP, nên ứng dụng vẫn chạy khi máy AI tắt.")
    r.figure(IMG / "Hinh 3.1 Kien truc trien khai.png", "Sơ đồ kiến trúc triển khai của hệ thống", 16)
    r.table("Thành phần kiến trúc và trách nhiệm", ["Thành phần", "Công nghệ", "Trách nhiệm"], [
        ("Trình duyệt", "HTML, CSS, JavaScript", "Giao diện cho cư dân, quản lý BQL, kỹ thuật viên, quản trị viên"),
        ("Tầng trình bày", "Controllers, Razor Views, Areas Tenant / Staff / Admin, ASP.NET Identity",
         "Nhận yêu cầu, kiểm tra đăng nhập và vai trò theo Area, kiểm tra dữ liệu nhập, hiển thị kết quả"),
        ("Tầng nghiệp vụ", "MaintenanceWorkflow, MaintenanceDispatcher, MaintenanceKpi",
         "Bảng chuyển trạng thái và quy tắc BR-SC-03 đến BR-SC-07, BR-SC-13; điều phối thợ và email; tính KPI"),
        ("Dịch vụ phân loại AI", "MaintenanceTriageService, HttpClient",
         "Gọi Ollama /api/chat với temperature 0, đọc JSON, gộp với lựa chọn của cư dân (BR-SC-09, BR-SC-10)"),
        ("Tầng dữ liệu", "AppDbContext, Entity Framework Core, Npgsql", "Ánh xạ lớp ↔ bảng tiếng Việt, migration, "
         "mỗi SaveChanges là một giao dịch"),
        ("Gửi email", "IEmailSender: SmtpEmailSender (Gmail SMTP 587) hoặc LoggingEmailSender",
         "Khi chưa cấu hình SMTP thì chỉ ghi log và lưu thư vào wwwroot/dev-emails để kiểm thử"),
        ("Dịch vụ nền", "InvoiceGenerationService, NotificationReminderService", "Sinh hoá đơn định kỳ, nhắc hạn"),
        ("Máy chủ AI", "Ollama, mô hình qwen3-suco4 (Qwen3-4B tinh chỉnh QLoRA [5], định dạng GGUF q4_k_m)",
         "Trả về loại, mức ưu tiên, nguy hiểm, câu hỏi lại, nhóm xử lý dạng JSON"),
        ("Máy chủ CSDL", "PostgreSQL", "Lưu dữ liệu và thực thi ràng buộc PK, FK, UNIQUE, CHECK ở Chương 2"),
    ], [3.3, 5.6, 7.1], size=11)
    r.p("Bốn quyết định kiến trúc chính:")
    r.numbered([
        "AI là thành phần có thể vắng mặt. Để trống cấu hình Ollama:BaseUrl là tắt AI; khi AI lỗi, quá thời gian chờ "
        "(cấu hình Ollama:TimeoutSeconds, 20 giây ở môi trường phát triển) hoặc trả JSON sai định dạng, yêu cầu vẫn được "
        "lưu theo lựa chọn của cư dân và quản trị viên nhận cảnh báo, tối đa một thư mỗi 30 phút.",
        "Mọi thay đổi trạng thái đi qua một lớp duy nhất là MaintenanceWorkflow, dù người thao tác là cư dân, kỹ thuật "
        "viên, quản lý hay hệ thống tự giao việc. Nhờ vậy quy tắc, mốc thời gian, hạn SLA và lịch sử luôn nhất quán.",
        "Email được gửi sau khi đã đọc xong dữ liệu, các thư gửi song song; lỗi gửi thư chỉ ghi log, không làm hỏng yêu "
        "cầu đã lưu.",
        "Phân quyền theo vùng (Area): Tenant cho cư dân, Staff cho năm vai trò thợ, Admin cho quản lý và quản trị viên; "
        "trong vùng Staff, kỹ thuật viên chỉ truy vấn được việc giao cho mình (BR-SC-04).",
    ])


# ---------------------------------------------------------------- 3.2
def use_cases(r):
    r.h2("3.2. Use case chi tiết")
    r.p("Sơ đồ use case tổng quát (Hình 1.6) cho biết tác nhân nào dùng chức năng nào. Mục này chi tiết hoá bốn use case "
        "của luồng chính, mỗi use case một sơ đồ: use case gốc nằm trên cùng, nối với tác nhân chính; bên dưới là các "
        "use case con. Hai loại quan hệ được phân biệt như sau:")
    r.bullets([
        "«include» (gộp): phần luôn được thực hiện mỗi khi use case gốc chạy; mũi tên đi từ use case gốc tới phần được "
        "gộp.",
        "«extend» (mở rộng): phần chỉ chạy khi thoả một điều kiện; mũi tên đi từ phần mở rộng về use case được mở rộng. "
        "Điều kiện của từng quan hệ mở rộng ghi ở bảng đi kèm mỗi sơ đồ.",
    ])
    uc_figure(r, "Hinh 3.2 Use case gui yeu cau su co.png", "Sơ đồ use case chi tiết UC03 – Gửi yêu cầu sự cố",
              "UC03 gồm ba phần luôn chạy: ghi nhận yêu cầu, phân loại bằng AI (UC07, tác nhân phụ Mô hình AI) và điều "
              "phối, gửi thông báo (UC08, tác nhân phụ Dịch vụ email).", [
                  ("Ghi nhận yêu cầu", "include", "Luôn: lưu yêu cầu, ảnh, lịch sử “Tạo yêu cầu”",
                   "Tenant Maintenance.Create"),
                  ("Kiểm tra cư dân đang ở và nơi xảy ra", "include", "Luôn (BR-SC-01, 02)", "GetMyApartmentAsync"),
                  ("Ghi vị trí khu vực chung", "extend", "Khi cư dân chọn sự cố ở khu vực chung", "Location"),
                  ("Tính hạn xử lý theo SLA", "include", "Luôn (BR-SC-07)", "InitializeNewAsync"),
                  ("Áp dụng kết quả AI (chỉ nâng mức ưu tiên)", "include", "Luôn; AI tắt hoặc lỗi thì giữ nguyên "
                   "(BR-SC-09)", "MaintenanceTriageService.Apply"),
                  ("UC07 Phân loại sự cố bằng AI", "include", "Luôn được gọi; AI tắt thì trả về rỗng", "TriageAsync"),
                  ("Hỏi lại khi mô tả mơ hồ", "extend", "Mô hình trả câu hỏi lại (BR-SC-11)", "FollowUpQuestion"),
                  ("Giữ lựa chọn của cư dân khi AI lỗi", "extend", "Lỗi, quá thời gian chờ, JSON sai (BR-SC-10)",
                   "TriageAsync trả null"),
                  ("UC08 Điều phối và gửi thông báo", "include", "Luôn, sau khi đã lưu", "DispatchAsync"),
                  ("Tự giao thợ đúng chuyên môn, ít việc nhất", "extend", "Mô tả rõ và nhóm thợ có người (BR-SC-12)",
                   "RoleFor, LeastBusyAsync"),
                  ("Cảnh báo quản trị viên khi AI không phản hồi", "extend", "AI bật nhưng không trả kết quả, chưa báo "
                   "trong 30 phút", "aiUnavailable"),
                  ("Gửi thư theo vai trò", "include", "Luôn: thợ, quản lý, quản trị nhận đúng loại thư",
                   "IEmailSender.SendAsync"),
              ])
    uc_figure(r, "Hinh 3.3 Use case tiep nhan phan cong.png", "Sơ đồ use case chi tiết UC11 – Tiếp nhận và phân công "
              "sự cố", "Quản lý luôn xem danh sách và giao việc; phân loại lại, từ chối và hoàn tất thay kỹ thuật viên "
              "là các thao tác chỉ làm khi cần.", [
                  ("Xem và lọc yêu cầu", "include", "Luôn", "Admin Maintenance.Index, Details"),
                  ("Lọc yêu cầu quá hạn", "extend", "Khi chọn bộ lọc quá hạn", "IsOverdue"),
                  ("Giao / giao lại kỹ thuật viên", "include", "Luôn; Mới gửi chuyển Đã tiếp nhận (BR-SC-05)",
                   "Assign → AssignAsync"),
                  ("Ghi lịch sử giao việc", "include", "Luôn (BR-SC-13)", "AddHistory"),
                  ("Phân loại lại yêu cầu", "extend", "Khi loại hoặc mức ưu tiên chưa đúng", "Reclassify"),
                  ("Tính lại hạn xử lý", "extend", "Chỉ khi mức ưu tiên đổi (BR-SC-07)", "RecalculateDueAsync"),
                  ("Từ chối yêu cầu", "extend", "Khi yêu cầu không thuộc trách nhiệm BQL", "ChangeStatus"),
                  ("Ghi lý do từ chối", "include", "Luôn khi từ chối (BR-SC-06)", "MaintenanceWorkflow.Check"),
                  ("Hoàn tất thay kỹ thuật viên", "extend", "Khi quản lý tự xử lý", "Resolve"),
                  ("Ghi kết quả xử lý", "include", "Luôn khi hoàn tất (BR-SC-06)", "TransitionAsync"),
                  ("Sinh hoá đơn chi phí sửa chữa (phân hệ hỗ trợ, ngoài phạm vi đánh giá)", "extend", "Sự cố trong căn, tính phí cư dân, chi phí > 0",
                   "GenerateOneOffInvoiceAsync"),
              ])
    uc_figure(r, "Hinh 3.4 Use case cap nhat xu ly.png", "Sơ đồ use case chi tiết UC16 – Cập nhật xử lý sự cố",
              "Kỹ thuật viên luôn xem việc, bắt đầu và hoàn tất; tạm dừng chỉ xảy ra khi phải chờ vật tư hoặc lịch hẹn.", [
                  ("UC15 Xem việc được giao", "include", "Luôn", "Staff Jobs.Index"),
                  ("Chỉ thấy việc giao cho mình", "include", "Luôn (BR-SC-04)", "MyJobs"),
                  ("Bắt đầu xử lý", "include", "Luôn: Đã tiếp nhận → Đang xử lý", "Jobs.ChangeStatus"),
                  ("Kiểm tra quy tắc chuyển trạng thái", "include", "Luôn (BR-SC-03, 05)", "MaintenanceWorkflow.Check"),
                  ("Hoàn tất xử lý", "include", "Luôn: Đang xử lý → Đã xử lý", "Jobs.Resolve"),
                  ("Ghi kết quả xử lý", "include", "Luôn (BR-SC-06)", "TransitionAsync"),
                  ("Tải ảnh nghiệm thu", "extend", "Khi có ảnh (không bắt buộc)", "PhotosFrom"),
                  ("Tạm dừng / tiếp tục xử lý", "extend", "Khi chờ vật tư, nhà thầu, lịch hẹn", "Jobs.ChangeStatus"),
                  ("Ghi lý do tạm dừng", "include", "Luôn khi tạm dừng (BR-SC-06)", "MaintenanceWorkflow.Check"),
                  ("Dời hạn SLA khi tiếp tục", "include", "Luôn khi tiếp tục (BR-SC-07)", "TransitionAsync"),
              ])
    uc_figure(r, "Hinh 3.5 Use case xac nhan hoan tat.png", "Sơ đồ use case chi tiết UC06 – Xác nhận kết quả xử lý",
              "Sau khi kỹ thuật viên hoàn tất, cư dân xác nhận đã xong (UC06) hoặc báo chưa khắc phục (UC06b). UC06b là "
              "use case riêng chứ không phải phần mở rộng của UC06, vì UC06 luôn đóng yêu cầu còn UC06b đưa yêu cầu về "
              "Đang xử lý.", [
                  ("Xem kết quả và ảnh nghiệm thu", "include", "Luôn", "Tenant Maintenance.Details"),
                  ("Đóng yêu cầu, ghi lịch sử", "include", "Luôn: Đã xử lý → Đã đóng", "ChangeStatus(Closed)"),
                  ("Chấm điểm 1–5 và nhận xét", "extend", "Khi cư dân chọn điểm (không bắt buộc)", "Rating"),
                  ("UC06b Báo chưa khắc phục", "—", "Use case riêng: Đã xử lý → Đang xử lý",
                   "ChangeStatus(InProgress)"),
                  ("Ghi lý do chưa khắc phục", "include", "Luôn khi báo chưa khắc phục (BR-SC-06)",
                   "MaintenanceWorkflow.Check"),
              ])
    r.p("Chương 1 đã đặc tả UC03 (gửi yêu cầu sự cố) và UC16 (cập nhật xử lý). Các bảng sau đặc tả thêm những use case "
        "có xử lý tự động hoặc quy tắc phức tạp.")
    spec(r, "UC07 – Phân loại sự cố bằng AI", [
        ("Tác nhân", "Mô hình AI (được hệ thống gọi trong UC03)"),
        ("Tiền điều kiện", "Cư dân đã nhập mô tả hợp lệ; AI được bật (Ollama:BaseUrl có giá trị)."),
        ("Hậu điều kiện", "Có kết quả phân loại hợp lệ (lưu nguyên văn vào AiTriageJson) hoặc kết luận AI không phản hồi."),
        ("Luồng chính", "1. Hệ thống gửi mô tả (cắt theo độ dài tối đa) cùng prompt cố định tới /api/chat, temperature 0.\n"
         "2. Mô hình trả JSON: loại, mức ưu tiên, có nguy hiểm, có cần hỏi lại kèm câu hỏi, nhóm xử lý.\n3. Hệ thống "
         "kiểm tra JSON: loại và mức phải thuộc danh sách, cờ nguy hiểm phải là đúng/sai, nhóm xử lý phải thuộc danh sách.\n"
         "4. Hệ thống gộp với lựa chọn của cư dân (BR-SC-09): loại của AI chỉ dùng khi cư dân chọn “Khác”; mức ưu tiên "
         "chỉ được nâng; nguy hiểm thì thành Khẩn cấp."),
        ("Luồng thay thế", "1a. AI tắt: bỏ qua bước 1–3, giữ nguyên lựa chọn của cư dân, không cảnh báo."),
        ("Ngoại lệ", "E1. Lỗi kết nối, quá thời gian chờ hoặc JSON sai định dạng: trả về null, giữ lựa chọn của cư dân, "
         "đánh dấu AI không phản hồi để UC08 cảnh báo quản trị viên (BR-SC-10)."),
    ])
    spec(r, "UC08 – Điều phối và gửi email", [
        ("Tác nhân", "Dịch vụ email (hệ thống tự thực hiện ngay sau khi lưu yêu cầu)"),
        ("Tiền điều kiện", "Yêu cầu đã được lưu ở trạng thái Mới gửi."),
        ("Hậu điều kiện", "Yêu cầu được giao (Đã tiếp nhận, có dòng lịch sử ghi chú “Hệ thống điều phối theo loại sự cố”) "
         "hoặc vẫn Mới gửi chờ quản lý; mỗi vai trò nhận đúng loại thư của mình."),
        ("Luồng chính", "1. Mô tả rõ (AI không hỏi lại): xác định nhóm thợ theo loại sự cố (điện → thợ điện, nước → thợ "
         "nước, thiết bị → thợ điện lạnh, kết cấu → thợ nhôm kính hoặc thợ mộc theo từ khoá).\n2. Chọn thợ trong nhóm có ít "
         "yêu cầu đang mở nhất (BR-SC-12), giao qua MaintenanceWorkflow với vai trò Hệ thống.\n3. Gửi thư giao việc cho "
         "thợ; nếu Khẩn cấp gửi thêm thư “Cần theo dõi” cho quản lý."),
        ("Luồng thay thế", "1a. Mô tả mơ hồ: không giao thợ, quản lý nhận thư “Cần hỏi lại cư dân” kèm câu hỏi của AI "
         "(BR-SC-11).\n1b. Loại không có thợ chuyên trách hoặc nhóm chưa có thợ: quản lý nhận thư “Cần phân công” kèm nhóm "
         "xử lý gợi ý.\n4a. AI không phản hồi và đã quá 30 phút từ thư trước: quản trị viên nhận thư cảnh báo."),
        ("Ngoại lệ", "E1. Giao việc bị quy trình từ chối (yêu cầu không còn Mới gửi): ghi log, xử lý như 1b.\nE2. Chưa có "
         "tài khoản quản lý: thư của quản lý gửi cho quản trị viên.\nE3. Lỗi gửi thư: chỉ ghi log."),
    ])
    spec(r, "giao / giao lại kỹ thuật viên (thuộc UC11)", [
        ("Tác nhân", "Quản lý BQL"),
        ("Tiền điều kiện", "Yêu cầu đang mở (Mới gửi, Đã tiếp nhận, Đang xử lý, Tạm dừng)."),
        ("Hậu điều kiện", "NguoiPhuTrachId là người được chọn; có dòng lịch sử “Giao cho …”."),
        ("Luồng chính", "1. Quản lý mở chi tiết yêu cầu, chọn người nhận trong danh sách kỹ thuật viên và quản lý, ghi chú "
         "nếu cần.\n2. Hệ thống kiểm tra người nhận có vai trò phù hợp.\n3. Nếu yêu cầu đang Mới gửi: chuyển Đã tiếp nhận, "
         "ghi mốc tiếp nhận và lịch sử; nếu đang ở trạng thái mở khác: giữ trạng thái, ghi lịch sử giao lại."),
        ("Luồng thay thế", "—"),
        ("Ngoại lệ", "E1. Người nhận không phải kỹ thuật viên hoặc quản lý: báo lỗi.\nE2. Yêu cầu đã đóng, từ chối hoặc huỷ: "
         "báo không thể giao việc."),
    ])
    spec(r, "UC06 / UC06b – Xác nhận hoàn tất và đánh giá / báo chưa khắc phục", [
        ("Tác nhân", "Cư dân"),
        ("Tiền điều kiện", "Yêu cầu do mình gửi hoặc thuộc căn mình đang ở, đang ở trạng thái Đã xử lý."),
        ("Hậu điều kiện", "UC06: Đã đóng, có mốc đóng và điểm đánh giá. UC06b: quay lại Đang xử lý, xoá mốc giải quyết."),
        ("Luồng chính", "1. Cư dân xem kết quả xử lý và ảnh nghiệm thu.\n2. Chọn điểm 1–5, nhận xét (không bắt buộc), bấm "
         "Xác nhận.\n3. Hệ thống ghi điểm, chuyển Đã đóng, ghi lịch sử."),
        ("Luồng thay thế", "2a (UC06b). Cư dân bấm Chưa khắc phục và ghi lý do: chuyển về Đang xử lý cho kỹ thuật viên "
         "đang phụ trách."),
        ("Ngoại lệ", "E1. Điểm ngoài 1–5: không ghi điểm (CHECK CK_YeuCauBaoTri_DanhGia chặn ở CSDL).\nE2. Báo chưa khắc "
         "phục không ghi lý do: bị chặn (BR-SC-06)."),
    ])
    spec(r, "UC12 – Xem báo cáo KPI sự cố", [
        ("Tác nhân", "Quản lý BQL, quản trị viên"),
        ("Tiền điều kiện", "Đã đăng nhập với vai trò quản lý hoặc quản trị."),
        ("Hậu điều kiện", "Không thay đổi dữ liệu."),
        ("Luồng chính", "1. Chọn kỳ báo cáo (mặc định từ đầu tháng đến hôm nay, theo giờ Việt Nam) và toà (không bắt buộc).\n"
         "2. Hệ thống đọc các yêu cầu báo trong kỳ, tính: tổng số, đang mở, quá hạn hiện tại, tỉ lệ hoàn tất đúng hạn, "
         "MTTR, thời gian phản hồi trung bình và tỉ lệ phản hồi đúng SLA, số việc hệ thống tự giao và tỉ lệ giữ nguyên người "
         "được giao, CSAT.\n3. Hiển thị phân rã theo mức ưu tiên, loại, toà, kỹ thuật viên."),
        ("Luồng thay thế", "1a. Ngày kết thúc trước ngày bắt đầu: hệ thống tự đổi chỗ hai ngày."),
        ("Ngoại lệ", "E1. Không có mẫu để tính tỉ lệ: hiển thị “—” thay vì 0% để không gây hiểu nhầm."),
    ])


def uc_figure(r, file, caption, intro, rows):
    """Một sơ đồ use case chi tiết kèm bảng ghi rõ quan hệ include / extend và điều kiện thực hiện."""
    from PIL import Image
    r.p(f"{r.next_fig()}: " + intro)
    w = Image.open(IMG / file).size[0] / 4          # ảnh xuất ở tỉ lệ 4×
    r.figure(IMG / file, caption, 16, rotate=w > 1150)
    r.table(caption.replace("Sơ đồ use case chi tiết", "Quan hệ trong use case"),
            ["Use case con", "Quan hệ", "Điều kiện thực hiện", "Cài đặt"], rows, [4.6, 1.8, 5.4, 4.2], size=10.5,
            align_cols=[1])


def spec(r, title, rows):
    r.table(f"Đặc tả {title}", ["Mục", "Nội dung"], rows, [3.2, 12.8])


# ---------------------------------------------------------------- 3.3
def activity(r):
    r.h2("3.3. Sơ đồ hoạt động luồng gửi yêu cầu, AI phân loại và điều phối")
    r.p(f"{r.next_fig()} chi tiết hoá phần tự động hoá của quy trình TO-BE (Hình 1.4), gồm bốn làn: cư dân, hệ thống web, "
        "dịch vụ phân loại AI và dịch vụ email. Mỗi nút quyết định có đúng một luồng vào và các nhánh ra đều có điều kiện; "
        "nơi nhiều luồng gặp nhau luôn có nút gộp, để không hành động nào phải chờ nhiều luồng cùng lúc.")
    r.figure(IMG / "Hinh 3.6 Hoat dong gui yeu cau AI dieu phoi.png",
             "Sơ đồ hoạt động gửi yêu cầu sự cố, AI phân loại và điều phối", 16)
    r.table("Các nút quyết định trên sơ đồ hoạt động và nơi cài đặt", ["Quyết định", "Nhánh", "Cài đặt"], [
        ("Dữ liệu nhập", "hợp lệ / chưa hợp lệ (quay lại nhập)", "Tenant Maintenance.Create: ModelState, vị trí khu vực chung"),
        ("AI có bật", "AI được bật / AI tắt", "MaintenanceTriageService.IsEnabled"),
        ("Kết quả AI", "JSON hợp lệ / lỗi, hết thời gian chờ", "TriageAsync, Parse"),
        ("Mô tả rõ", "AI hỏi lại / mô tả rõ", "DispatchAsync: triage.FollowUpQuestion"),
        ("Có thợ phù hợp", "có / không", "RoleFor, LeastBusyAsync"),
        ("Mức ưu tiên", "Khẩn cấp / mức khác", "ManagerMail"),
        ("Cảnh báo AI", "AI không phản hồi và chưa báo trong 30 phút / ngược lại", "aiUnavailable, AiAlertInterval"),
    ], [3.4, 6.0, 6.6], size=11)


# ---------------------------------------------------------------- 3.4
def classes(r):
    from kiem_tra_uml import OPS_MAP
    r.h2("3.4. Sơ đồ lớp")
    r.p(f"{r.next_fig()} là sơ đồ lớp của hệ thống ở mức phân tích, gồm 14 lớp nghiệp vụ và 3 kiểu liệt kê. Mỗi lớp là "
        "một đối tượng nghiệp vụ, hộp lớp chia ba ngăn: tên lớp, thuộc tính (dấu - là riêng tư) và thao tác (dấu + là "
        "công khai). Tên lớp và thuộc tính đặt theo tên bảng, tên cột của CSDL nên khớp với ERD và lược đồ quan hệ ở "
        "Chương 2; mỗi lớp chỉ ghi những thuộc tính chính, đủ cột xem ở từ điển dữ liệu mục 2.6.")
    r.figure(IMG / "Hinh 3.7 So do lop.png", "Sơ đồ lớp của hệ thống", 16, rotate=True)
    r.p("Các nhóm lớp và quan hệ:")
    r.bullets([
        "Nơi chốn: KhuVuc có nhiều DuAn và ToaNha; một toà có thể thuộc một dự án (0..1). ToaNha gồm các Tang (hợp thành, "
        "hình thoi đặc: xoá toà thì xoá tầng); mỗi CanHo thuộc đúng một toà và một tầng. CanHo gồm các CuDan (hợp thành).",
        "Người dùng và phân quyền: NguoiDung có nhiều VaiTro, VaiTro có nhiều Quyen (nhiều – nhiều, cài đặt bằng hai bảng "
        "nối NguoiDung_VaiTro, VaiTro_Quyen nên bảng nối không vẽ thành lớp). Một người dùng có thể là quản lý của nhiều "
        "toà (quan hệ quanLy) và là tài khoản của bản ghi cư dân.",
        "Sự cố: YeuCauBaoTri phát sinh tại 0..1 căn hộ hoặc khu vực chung của 0..1 toà (toaNhaKhuVucChung), có người báo "
        "(nguoiBaoCao) và kỹ thuật viên phụ trách (nguoiPhuTrach), áp dụng đúng một MucSla. AnhSuCo và "
        "LichSuTrangThaiSuCo là bộ phận của yêu cầu (hợp thành); mỗi ảnh, mỗi dòng lịch sử ghi người tải lên / người thực "
        "hiện.",
        "NhatKyHeThong ghi mọi thay đổi dữ liệu; không có khoá ngoại tới bảng khác (giữ được nhật ký kể cả khi người dùng "
        "bị xoá) nên đứng riêng.",
        "TrangThaiSuCo, MucDoUuTien, LoaiBaoTri là các kiểu liệt kê, đúng thứ tự giá trị lưu trong CSDL (Hình 1.5 và mục "
        "2.6).",
    ])
    r.p("Bội số ghi ở hai đầu mỗi liên kết lấy theo khoá ngoại thật: 1 khi khoá ngoại bắt buộc, 0..1 khi cho phép NULL. "
        "Tên quan hệ (quanLy, nguoiBaoCao…) là tên cột khoá ngoại bỏ hậu tố Id. Thao tác trên sơ đồ là nghiệp vụ của đối "
        "tượng; khi cài đặt, mỗi thao tác do một hàm cụ thể thực hiện:")
    rows = [(f"{cls}.{op}()", f"{target}.{member}") for (cls, op), (target, member) in OPS_MAP.items()]
    r.table("Thao tác trên sơ đồ lớp và hàm cài đặt tương ứng", ["Thao tác nghiệp vụ", "Hàm cài đặt"], rows,
            [7.0, 9.0], size=11)


# ---------------------------------------------------------------- 3.5
def sequences(r):
    r.h2("3.5. Sơ đồ tuần tự")
    r.p("Ba sơ đồ tuần tự mô tả hai luồng quan trọng nhất: cư dân gửi yêu cầu có AI phân loại (tách làm hai hình: phân "
        "loại và lưu, điều phối sau khi lưu) và kỹ thuật viên hoàn tất xử lý. Khung opt là đoạn chỉ chạy khi điều kiện "
        "đúng, khung alt là các nhánh loại trừ nhau; thông điệp nét đứt là giá trị trả về.")
    r.p(f"{r.next_fig()}: cư dân gửi biểu mẫu, bộ điều khiển kiểm tra cư dân đang ở (GetMyApartmentAsync), gọi dịch vụ "
        "phân loại; chỉ khi AI được bật mới có lời gọi tới Ollama. Kết quả (hoặc null) được gộp bằng hàm tĩnh Apply, sau "
        "đó MaintenanceWorkflow tính hạn SLA (RecalculateDueAsync tra bảng MucSla) và ghi dòng lịch sử “Tạo yêu cầu” "
        "trước lần lưu đầu tiên. Lưu xong mới điều phối, nên lỗi điều phối không làm mất yêu cầu.")
    r.figure(IMG / "Hinh 3.8 Tuan tu gui yeu cau AI phan loai.png", "Sơ đồ tuần tự gửi yêu cầu sự cố và AI phân loại",
             16, rotate=True)
    r.p(f"{r.next_fig()}: bên trong DispatchAsync. Nhánh thứ nhất của khung alt chạy khi mô tả rõ và tìm được thợ: chọn "
        "nhóm thợ (RoleFor), chọn người ít việc nhất, giao qua AssignAsync với vai trò System (chuyển Mới gửi → Đã tiếp "
        "nhận), gửi thư giao việc và thư theo dõi nếu Khẩn cấp. Nhánh thứ hai gửi thư cho quản lý. Khung opt cuối cùng là "
        "cảnh báo AI không phản hồi.")
    r.figure(IMG / "Hinh 3.9 Tuan tu dieu phoi sau khi luu.png", "Sơ đồ tuần tự điều phối sau khi lưu yêu cầu", 16)
    r.p(f"{r.next_fig()}: kỹ thuật viên chỉ tìm được yêu cầu trong MyJobs (lọc theo người phụ trách là chính mình), nếu "
        "không thấy thì trả về 404 (BR-SC-04). Hàm Check của MaintenanceWorkflow quyết định nhánh: vi phạm bảng chuyển "
        "trạng thái hoặc thiếu kết quả xử lý thì trả về thông báo lỗi và không lưu gì; hợp lệ thì ghi mốc giải quyết, kết "
        "quả, lịch sử và lưu trong một lần SaveChanges.")
    r.figure(IMG / "Hinh 3.10 Tuan tu ky thuat vien hoan tat.png", "Sơ đồ tuần tự kỹ thuật viên hoàn tất xử lý", 15)


# ---------------------------------------------------------------- 3.6
def consistency(r):
    stats, errors, notes, mutation = consistency_results()
    r.h2("3.6. Kiểm tra tính nhất quán của mô hình")
    r.p("Yêu cầu của giai đoạn này là mô hình phải nhất quán: chức năng có trên use case thì phải có luồng xử lý, phân "
        "quyền, dữ liệu trạng thái và phần mềm tương ứng. Thay vì đối chiếu bằng mắt, nhóm viết công cụ kiem_tra_uml.py "
        "đọc trực tiếp tệp StarUML, mã nguồn C# và model snapshot của Entity Framework (bản mô tả lược đồ CSDL do migration "
        "sinh ra), rồi kiểm tra các nhóm điều kiện ở bảng dưới.")
    r.table("Các nhóm kiểm tra nhất quán tự động", ["Mô hình", "Điều kiện được kiểm tra"], [
        ("Sơ đồ lớp", "Lớp mang tên bảng, thuộc tính là cột của bảng; mỗi thao tác ánh xạ tới một hàm có thật trong mã; "
         "giá trị enum đúng thứ tự trạng thái; mỗi liên kết ứng với một khoá ngoại, đúng bội số, đúng tên vai trò (tên cột "
         "khoá ngoại); hợp thành chỉ khi khoá ngoại bắt buộc và xoá dây chuyền; mọi khoá ngoại giữa hai lớp có trên sơ đồ "
         "đều được vẽ"),
        ("Sơ đồ tuần tự", "Đường đời là lớp có trong mã; thông điệp gọi hàm có thật ở lớp nhận"),
        ("Sơ đồ trạng thái", "Tập chuyển trạng thái bằng đúng bảng Rules trong MaintenanceWorkflow; vai trò ghi trên nhãn "
         "và nhãn “có lý do” khớp mã; trigger ở mục 2.5.3 dùng đúng tập chuyển đó"),
        ("ERD, lược đồ quan hệ", "Bảng, cột, PK, FK, NULL có trong CSDL; mỗi mối kết hợp ứng với khoá ngoại hoặc bảng nối; "
         "bản số phía bảng cha khớp khoá ngoại bắt buộc hay cho phép NULL"),
        ("Từ điển dữ liệu 2.6", "NULL / NOT NULL, bảng đích và hành vi xoá (CASCADE, RESTRICT, SET NULL) khớp CSDL"),
        ("Sơ đồ hoạt động", "Một nút bắt đầu; mọi nút đi tới được và dẫn tới kết thúc; quyết định 1 vào, ít nhất 2 ra có "
         "điều kiện; gộp ít nhất 2 vào, 1 ra; hành động đúng 1 vào, 1 ra"),
        ("Use case", "Mỗi mã use case trên sơ đồ chi tiết (UC03, UC06b…) có use case gốc trên sơ đồ tổng quát"),
    ], [3.6, 12.4], size=11)
    r.p(f"Kết quả lần chạy cuối khi sinh báo cáo: {stats[0].lower() + stats[1:]}; số lỗi: {errors}; số ghi chú: {notes}. "
        "Hai ghi chú không phải lỗi: hai chuyển trạng thái vẽ bằng đường xiên dùng chung nhãn với đường song song bên cạnh, "
        "và ERD khái niệm ghi mỗi yêu cầu có đúng một người báo (quy tắc nghiệp vụ) trong khi cột NguoiBaoCaoId cho phép "
        "NULL (ứng dụng luôn ghi giá trị khi tạo).")
    r.p("Trong quá trình chạy, công cụ đã phát hiện và nhóm đã sửa các sai lệch: sơ đồ lớp bản đầu thiếu liên kết người tải ảnh "
        "(khoá ngoại NguoiTaiLenId); lược đồ quan hệ ghi VaiTro.Name và NguoiDung.UserName là NOT NULL trong khi ASP.NET "
        "Identity cho phép NULL; từ điển dữ liệu ghi NguoiBaoCaoId không NULL; ba sơ đồ hoạt động có hành động nhận hai luồng "
        "vào mà không qua nút gộp (theo UML đó là chờ cả hai luồng); một thông điệp tuần tự gửi nhầm lớp nhận. Để chắc chắn "
        f"công cụ không bỏ sót, nhóm cố ý cấy từng loại lỗi vào bản sao mô hình (test_kiem_tra_uml.py): {mutation}.")
    r.p("Bên cạnh mô hình, quy tắc nghiệp vụ trong mã được kiểm thử tự động bằng xUnit (dotnet test, chạy ngày "
        "25/10/2026): 101 trường hợp kiểm thử, 101 đạt, 0 lỗi. Các lớp kiểm thử liên quan trực tiếp đến mô hình ở chương "
        "này:")
    r.table("Kiểm thử tự động liên quan đến mô hình thiết kế", ["Lớp kiểm thử", "Số phương thức", "Nội dung"], [
        ("MaintenanceWorkflowTests", "11", "Bảng chuyển trạng thái, vai trò, bắt buộc lý do và kết quả, dời hạn khi tạm dừng"),
        ("MaintenanceDispatcherTests", "11", "Chọn nhóm thợ theo loại, thợ ít việc nhất, mô tả mơ hồ, thư theo vai trò"),
        ("MaintenanceTriageTests", "12", "Đọc JSON, nhãn ngoài danh sách, gộp loại và chỉ nâng mức ưu tiên"),
        ("MaintenanceKpiTests", "6", "Công thức KPI, tỉ lệ không có mẫu trả về rỗng"),
        ("MaintenanceFlowTests", "3", "Luồng đầu – cuối qua bộ điều khiển với CSDL kiểm thử"),
    ], [5.4, 2.6, 8.0], size=11, align_cols=[1])
    r.p("Trigger, hàm, thủ tục ở mục 2.5.3 và các ràng buộc cài ở tuần 4 được chạy thử trên CSDL PostgreSQL 18 của "
        "nhóm bằng test_sql_2_5_3.py (ngày 06/11/2026), hai lượt, mỗi lượt trong một giao dịch rồi ROLLBACK nên dữ liệu "
        "không thay đổi. Lượt 1 thử trên chính các đối tượng đã cài bằng migration: 70/70 ca đúng, gồm 67 ca dưới đây và "
        "3 ca ràng buộc mới (căn ghi toà khác toà của tầng bị FK_CanHo_Tang_ToaNha chặn; trạng thái 9 và loại 50 bị hai "
        "CHECK chặn). Lượt 2 gỡ tạm bản đã cài, cài nguyên văn đoạn SQL in trong báo cáo rồi thử lại: 67/67 ca đúng. "
        "Trong 56 cặp trạng thái (từ, sang) khác nhau, trigger cho đúng 11 "
        "cặp có trong bảng Rules của MaintenanceWorkflow và chặn 45 cặp còn lại; chặn bắt đầu xử lý khi chưa có người phụ "
        "trách và hoàn tất khi chưa ghi kết quả; không chặn khi chỉ sửa cột khác; fn_han_xu_ly cho cùng kết quả với công "
        "thức của ứng dụng ở cả bốn mức SLA; sp_giao_viec chuyển Mới gửi → Đã tiếp nhận kèm mốc tiếp nhận và dòng lịch sử, "
        "giữ trạng thái khi giao lại, từ chối giao yêu cầu đã đóng hoặc không tồn tại.")


# ---------------------------------------------------------------- 3.7
def week4_result(r):
    r.h2("3.7. Kết quả tuần 4 và việc tiếp theo")
    r.p("Đến tuần 4, bộ mô hình thiết kế gồm: sơ đồ kiến trúc triển khai; sơ đồ use case tổng quát và bốn sơ đồ use case chi tiết, đặc tả bảy "
        "use case; ba sơ đồ hoạt động (AS-IS, TO-BE, luồng AI); sơ đồ trạng thái; sơ đồ lớp; ba sơ đồ tuần tự; ERD và "
        "lược đồ quan hệ. Tất cả nằm trong một tệp StarUML và đã qua kiểm tra nhất quán tự động với mã nguồn và CSDL.")
    r.p("Các việc về CSDL chuyển tiếp từ mục 2.9 đã làm bằng một migration (20261106042803_Tuan4RangBuocTriggerHam, ngày 06/11/2026), CSDL được sao "
        "lưu trước khi áp dụng: khoá ngoại ghép (TangId, ToaNhaId) cho CanHo kèm UNIQUE (Id, ToaNhaId) ở Tang; CHECK miền "
        "giá trị cho TrangThai và LoaiBaoTri; chỉ mục trên NgayBaoCao; trigger trg_yeu_cau_chuyen_trang_thai và hàm "
        "fn_han_xu_ly. Thủ tục sp_giao_viec không cài vì lịch sử giao việc chỉ do ứng dụng ghi. Sau migration: 101/101 "
        "kiểm thử tự động đạt, kiem_tra_uml.py 0 lỗi, test_sql_2_5_3.py đạt cả hai lượt.")
    r.p("Hai việc còn lại của mục 2.9 cũng đã khép: nhóm giữ quyết định khu vực của toà độc lập với khu (mục 2.4.3 b); "
        "nguồn căn cứ số 2 là phỏng vấn trực tiếp, đơn vị không cho phép ghi âm, ghi hình nên không có biên bản, nội dung "
        "phỏng vấn được dùng để mô tả quy trình AS-IS ở mục 1.3. Chương 4 sẽ trình bày việc xây dựng, kiểm thử theo quy trình nghiệp vụ và đánh giá AI "
        "trên bộ 120 ca có nhãn.")
