# Biên bản kiểm thử toàn hệ thống — 06/11/2026

Môi trường: web chạy cục bộ (`dotnet run`, cổng 5073, Development) trên CSDL PostgreSQL 18 thật `LuxeHavenBql` (đã áp migration tuần 4). Ollama tắt trong phần lớn buổi thử.
Sao lưu trước khi thử: `db_backup/LuxeHavenBql_truoc_kiem_thu_toan_he_thong_20261106.dump`.
Cách thử: quét tự động mọi trang GET theo 5 vai trò; thao tác trên trình duyệt; gửi POST có antiforgery token như form thật (cả dữ liệu đúng và sai, cả request giả bỏ qua kiểm tra phía trình duyệt); đối chiếu từng số liệu báo cáo bằng SQL trực tiếp.

## 1. Kết quả tổng quát

| Hạng mục | Kết quả |
|---|---|
| Quét 75 trang × 5 vai trò (khách, quản trị, quản lý, cư dân, thợ) + 8 trang công khai | 0 lỗi 500, không trang nào chậm quá 1,5 s |
| Chặn truy cập theo vai trò (khách → Login, cư dân/thợ vào quản trị → 403, cư dân/thợ xem dữ liệu người khác → 404) | Đạt |
| Luồng sự cố đầy đủ, kể cả ngoại lệ (tạm dừng, mở lại, huỷ, giao tay, phân loại lại, tắt AI) | Đạt (thử ở buổi trước, cùng ngày) |
| Đối chiếu báo cáo: tình trạng căn, công nợ theo toà, tuổi nợ 5 nhóm, KPI sự cố, bảng điều khiển quản lý và cư dân | Khớp SQL từng số |
| Phát hiện lỗi | 9 mức cao, 8 mức trung bình, 7 mức thấp (mục 3) |

## 2. Các phần đạt

- Khu / toà / căn: bắt buộc tên; đơn giá, ngày chốt, phí trễ ngoài khoảng bị chặn; tạo toà tự sinh tầng và căn; mã căn trùng trong toà, tầng của toà khác, diện tích âm bị chặn; xoá khu còn toà, toà còn căn, căn có hoá đơn hoặc cư dân bị chặn; căn đã xoá mềm tạo lại được cùng mã.
- Cư dân: thêm người đầu tiên tự làm đại diện, căn tự chuyển "Đang ở"; đổi đại diện; chuyển đi tự chuyển đại diện sang người còn ở, người cuối đi thì căn về "Để trống"; chuyển đi lần hai trả 404.
- Phí cố định: phí âm hoặc thiếu mô tả bị chặn. Lập hoá đơn tháng: đúng phí quản lý (diện tích × đơn giá) + phí cố định; không lập trùng kỳ; căn chưa bàn giao không lập.
- Hoá đơn và thanh toán: ghi tiền 0 hoặc vượt số còn lại bị chặn; huỷ hoá đơn đã có tiền bị chặn; hoàn tiền vượt hoặc âm bị chặn.
- Cổng cư dân: gửi xác nhận chuyển khoản chặn số 0, số vượt, số cộng dồn với khoản đang chờ; cư dân căn khác gửi cho hoá đơn này → 404; quản trị duyệt, từ chối, không duyệt lại khoản đã xử lý.
- Chỉ số điện nước: chỉ số mới nhỏ hơn chỉ số cũ bị chặn; trùng (căn, loại, kỳ) bị chặn; chỉ số đã tính tiền không sửa, xoá được.
- Phí trễ: chỉ áp sau thời gian ân hạn của toà (64 hoá đơn đã áp đúng luật).
- Sự cố do quản lý ghi thay: bắt buộc chọn căn, hoặc toà kèm vị trí khu vực chung.

## 3. Phát hiện

### Mức cao

| # | Phân hệ | Hiện tượng (đã tái hiện) | Nguyên nhân |
|---|---|---|---|
| C1 | Phân quyền | Quản lý vào được Người dùng, Phân quyền, Nhật ký và tự thêm vai trò Admin cho chính mình (đã hoàn tác). Trái BR-PQ-01 "trừ quản lý người dùng và phân quyền". | `UsersController`, `RolesController`, `AuditLogsController` kế thừa `[Authorize(Roles = "Admin,Manager")]`; không chặn tự sửa vai trò. |
| C2 | Phân quyền | Trang "Phân quyền" lưu bảng `VaiTro_Quyen` nhưng không chỗ nào dùng bảng này để kiểm tra quyền; báo cáo ghi BR-PQ-01 được bảo đảm bằng "Bảng VaiTro_Quyen". | Hệ thống chỉ kiểm tra theo vai trò. |
| C3 | Nhật ký | Chi tiết nhật ký của tài khoản lưu nguyên `PasswordHash` và `SecurityStamp` (quản lý cũng xem được). | Bộ ghi nhật ký chụp mọi cột của `AppUser`. |
| C4 | Tài khoản | Người lạ đăng ký bằng email của cư dân sắp vào ở; BQL thêm cư dân với email đó → tài khoản người lạ được gắn vào căn, xem được hoá đơn. | Đăng ký công khai đặt `EmailConfirmed = true` không xác minh; `AddResident` tự liên kết theo email. |
| C5 | Tài khoản | Ở môi trường Development (cấu hình mặc định của dự án) khách gọi `api/dev/auth/reset-token` lấy được mã đặt lại mật khẩu của người vừa bấm "Quên mật khẩu"; đăng nhập sai 12 lần không bị khoá; `api/auth/email-exists` cho biết email nào có tài khoản. | `DevAuthApiController` chỉ chặn ngoài Development; `PasswordSignInAsync(..., lockoutOnFailure: false)`. |
| C6 | Hoá đơn | Sửa dòng hoá đơn, bỏ dòng "Gửi xe 120.000" → dòng bị xoá mềm nhưng tổng vẫn 220.000 trong khi các dòng còn lại cộng 100.000. | `RecomputeTotals` cộng `inv.Items` trong bộ nhớ, dòng vừa `Remove` vẫn nằm trong danh sách. |
| C7 | Hoá đơn | Hoá đơn đã trả 500.000, sửa dòng cho tổng còn 220.000 → hoá đơn thành "Đã thanh toán", 280.000 trả dư biến mất, không cảnh báo. | Cho sửa dòng hoá đơn đã có tiền; số còn lại bị kẹp về 0. |
| C8 | Hoá đơn | Tiến trình tự phát hành chỉ chạy khi `ngày chốt == hôm nay`; máy chủ tắt đúng ngày chốt thì cả tháng không có hoá đơn (tháng 11/2026 hiện chưa có hoá đơn nào cho căn thật). | `GenerateForBillingDayAsync` lọc `DefaultBillingDay == asOf.Day`, không bù kỳ lỡ. |
| C9 | Hoá đơn | Hạn thanh toán luôn là (ngày chốt + 7) của tháng hiện tại: hoá đơn lập ngày 06/11 hạn 08/11, lập sau ngày 08 thì quá hạn ngay và bị tính phí trễ; hoá đơn phát sinh nhận hạn năm 2020. | `InvoiceGenerator.cs:46-47`; `CreateOneOff` không kiểm tra `DueDate`. |

### Mức trung bình

| # | Phân hệ | Hiện tượng |
|---|---|---|
| T1 | Thanh toán | Sau một lần hoàn một phần, không hoàn tiếp được phần còn lại ("Chỉ có thể hoàn tiền cho thanh toán đã thành công"); luật "hoàn tiền trước khi huỷ hoá đơn" cũng bỏ qua thanh toán đã hoàn một phần. |
| T2 | Điện nước | Ghi chỉ số cho kỳ đã lập hoá đơn → lưu bình thường nhưng không bao giờ được tính tiền, không cảnh báo. Kỳ 202613 (tháng 13) lưu được. |
| T3 | Căn hộ | Căn đang có 3 người ở đổi được sang "Để trống" (báo cáo lấp đầy đếm sai); gửi trạng thái không tồn tại (9) bị ghi thành "Để trống". |
| T4 | Cư dân | Thêm trùng cùng một người (cùng tên, cùng số điện thoại) vào cùng căn. |
| T5 | Người dùng | Đổi email sang email đã có → Identity từ chối nhưng màn hình báo "Đã cập nhật người dùng"; theo code, phần vai trò vẫn được ghi dù cập nhật thất bại. |
| T6 | Người dùng | Khoá tài khoản không đẩy phiên đang đăng nhập ra (vẫn dùng tiếp); chỉ chặn lần đăng nhập mới. |
| T7 | Giao diện | Khoảng 10 form dùng `asp-validation-summary="ModelOnly"` và thiếu chỗ hiện lỗi từng ô: ví dụ thêm toà bắt buộc Địa chỉ nhưng bấm Lưu chỉ thấy form quay lại, không báo gì; nhập chỉ số trùng kỳ cũng vậy. |
| T8 | Tài khoản | "Quên mật khẩu" không gửi email; mã đặt lại chỉ lưu tạm khi chạy Development. |

### Mức thấp

| # | Hiện tượng |
|---|---|
| L1 | Nhật ký hiển thị giờ UTC (05:22) trong khi mọi trang khác dùng giờ Việt Nam; cột "Cột đổi" của tài khoản liệt kê mọi cột. |
| L2 | Báo cáo Tài chính: "Tỉ lệ thu 212,3%" (thu tháng này gồm cả tiền của hoá đơn tháng trước chia cho phát hành tháng này); "Đã thu tháng này" bỏ qua thanh toán đã hoàn một phần. |
| L3 | Cổng cư dân ghi "đến hạn 08/08" cho hoá đơn đã quá hạn từ tháng 8; hoá đơn quá hạn được trả một phần chuyển về "Trả một phần" đến lần chạy tiến trình kế tiếp. |
| L4 | Trang `/Home/Privacy` (link "Bảo mật", "Điều khoản") còn nội dung mẫu tiếng Anh. |
| L5 | Request giả với căn không tồn tại → lỗi 500 (nhập chỉ số); theo code, sửa khu/căn với Id không tồn tại cũng ném lỗi 500. |
| L6 | Toà chọn khu vực khác khu vực của dự án vẫn lưu được (việc 2.4.3 b đang chờ nhóm xác nhận quy định). |
| L7 | Theo code, tài khoản bị khoá đăng nhập nhận thông báo "Email hoặc mật khẩu không chính xác". |

## 4. Dữ liệu phát sinh khi thử

Khu "Khu Kiểm Thử QA" (Id 3), "Toà QA" (Id 6) với các căn QA-01.01, QA-02.01, QA-02.02, QA-01.02 (một căn cũ đã xoá mềm); cư dân thử của căn QA-01.01 (đã chuyển đi) và "Cư dân thật QA" ở QA-02.01; hoá đơn INV-202611-0001..0003 và thanh toán PMT-20261106-0001; tài khoản `qa.nguoila@example.test`; sự cố khu vực chung toà QA (Id 9); chỉ số kỳ 202611 và 202613 của QA-01.01.
Trên dữ liệu thật: hoá đơn INV-202610-0157 (căn B-08.03) có thêm thanh toán 1.000.000 đã duyệt (PMT-20261106-0002) và 1.811.000 bị từ chối (PMT-20261106-0003). Vai trò của `quanly` và khoá của `renter3` đã được hoàn tác.
Khôi phục toàn bộ về trước buổi thử: `pg_restore --clean` từ bản sao lưu ở đầu biên bản.

## 5. Kết quả sửa lỗi (18/11/2026)

Kiểm tra sau khi sửa: 115/115 kiểm thử tự động đạt (thêm 12 kiểm thử cho các lỗi ở mục 3, tệp `t.Tests/Integration/HighSeverityFixTests.cs`); `kiem_tra_uml.py` 0 lỗi; thử lại trên web với CSDL thật cho từng lỗi bên dưới.

| # | Cách sửa | Thử lại trên web |
|---|---|---|
| C1, C2 | Bộ lọc `[Permission]` tra bảng `VaiTro_Quyen` cho từng phân hệ (GET cần quyền xem, POST cần quyền thao tác); không tự khoá mình, không tự bỏ vai trò Admin; Admin luôn giữ `roles.manage`. | Quản lý xem được danh sách người dùng; mở form sửa, trang phân quyền, gửi yêu cầu tự thêm Admin → trang 403. Admin tự khoá mình → bị chặn. |
| C3 | Bộ ghi nhật ký che `PasswordHash`, `SecurityStamp`, `ConcurrencyStamp`, chỉ ghi cột thật sự đổi; migration `AnGiaTriBiMatTrongNhatKy` che dữ liệu cũ. | Nhật ký còn 0 bản ghi chứa mã băm. |
| C4 | Đóng đăng ký công khai (chỉ qua thư mời); thêm cư dân không tự gắn tài khoản theo email, luôn gửi thư mời (BR-CD-02). | Mở `/Home/Register` không có thư mời → về trang đăng nhập kèm hướng dẫn. |
| C5 | Bỏ `api/auth/email-exists` và `api/dev/auth/reset-token`; Quên mật khẩu gửi liên kết qua email (sửa luôn T8); sai 5 lần khoá 5 phút, báo rõ lý do (sửa luôn L7). | Hai API trả 404; sai 5 lần rồi nhập đúng vẫn bị từ chối. |
| C6, C7 | Dòng bị bỏ được rút khỏi danh sách trước khi tính tổng; không cho tổng mới nhỏ hơn số đã thu. | Bỏ dòng 120.000 → tổng 100.000; đã thu 80.000, giảm còn 50.000 → bị chặn. |
| C8 | Tiến trình lập hoá đơn chạy cho mọi toà có ngày chốt ≤ hôm nay (không trùng nhờ kiểm tra kỳ). | Khởi động lại ngày 18/11 bù 493 hoá đơn tháng 11. |
| C9 | Hạn = ngày lập + 7; hoá đơn phát sinh không nhận hạn trước hôm nay. | Hoá đơn bù kỳ hạn 25/11; hoá đơn phát sinh hạn 2020 bị chặn. |
| T1 | Hoàn tiền tiếp được sau lần hoàn một phần; luật huỷ hoá đơn tính cả thanh toán đã hoàn một phần. | Kiểm thử tự động. |
| T2 | Từ chối chỉ số cho kỳ đã lập hoá đơn tháng và kỳ có tháng ngoài 01–12; căn không tồn tại báo lỗi thay vì lỗi 500. | Ghi chỉ số điện kỳ 11 cho căn đã có hoá đơn → bị chặn. |
| T3 | Căn còn cư dân không đổi được sang Để trống / Chưa bàn giao; trạng thái không hợp lệ → 400. | Kiểm thử tự động. |
| T4 | Chặn thêm trùng cư dân đang ở (trùng email, số giấy tờ, hoặc trùng họ tên và số điện thoại). | Kiểm thử tự động. |
| T5 | Báo lỗi khi đổi sang email đã có, không ghi vai trò. | "Email renter@luxehaven.vn đã được tài khoản khác sử dụng." |
| T6 | Khoá tài khoản đổi tem bảo mật; phiên được đối chiếu mỗi phút → phiên cũ bị đăng xuất trong vòng 1 phút. | Chưa thử trên web. |
| T7 | 9 form hiện toàn bộ lỗi ở đầu form và thêm chỗ hiện lỗi dưới từng ô còn thiếu. | Thêm toà bỏ trống địa chỉ → lỗi hiện dưới ô Địa chỉ và Mã toà. |
| L1 | Nhật ký hiển thị và lọc theo giờ Việt Nam ("đến ngày" tính trọn ngày). | — |
| L2 | Báo cáo Tài chính và bảng điều khiển: mốc tháng/ngày theo giờ Việt Nam, thực thu trừ phần đã hoàn, tỉ lệ thu chỉ xét hoá đơn phát hành trong tháng. | — |
| L3 | Trả một phần hoá đơn đã quá hạn vẫn giữ Quá hạn; cổng cư dân ghi "quá hạn từ" khi đã qua hạn. | — |
| L4 | Trang Chính sách bảo mật viết lại bằng tiếng Việt theo đúng dữ liệu hệ thống lưu; link ở trang đăng nhập trỏ về trang này. | — |
| L5 | Sửa khu/toà/căn/chỉ số/loại dịch vụ với Id không tồn tại trả 404. | — |
| L6 | Không phải lỗi: ngày 18/11 nhóm giữ quyết định ở mục 2.4.3 b — khu vực của toà là thông tin độc lập, không suy ra từ khu (một khu có thể trải trên nhiều quận). | — |

Ghi chú dữ liệu: khi khởi động lại ngày 18/11, tiến trình còn áp phí trễ cho 174 hoá đơn tháng 10 đã quá thời gian ân hạn — đúng luật. Dữ liệu thử phát sinh thêm: hoá đơn phát sinh INV id 1631 của căn QA-01.01 (đã thu 80.000) và toà "Toà thiếu địa chỉ" (Id 7) trong Khu Kiểm Thử QA.
