# Kịch bản demo: báo sự cố, AI phân loại, tự giao thợ và gửi email

Kịch bản này chạy toàn bộ luồng trên máy cá nhân, không cần deploy. Mười sự cố được gửi lần lượt bằng
một tài khoản cư dân mẫu; sau mỗi lần gửi, hộp thư demo nhận email thật theo đúng vai trò (thợ, quản lý, quản trị viên).

Thời lượng: khoảng 15 phút. Kết quả ở cột "Mô hình trả về" là kết quả thật của `qwen3-suco3` khi chạy
thử các câu này; mô hình chạy ở nhiệt độ 0 nên lặp lại được.

## 1. Chuẩn bị (làm một lần)

1. **Ollama** đang chạy và có mô hình `qwen3-suco3` (`ollama list` để kiểm tra).
2. **Email gửi**: một tài khoản Gmail đã bật xác minh 2 bước, tạo mật khẩu ứng dụng 16 ký tự tại
   <https://myaccount.google.com/apppasswords>.
3. **Đặt cấu hình SMTP** bằng biến môi trường (PowerShell), thay ba giá trị trong ngoặc nhọn:

   ```powershell
   [Environment]::SetEnvironmentVariable('Smtp__Host', 'smtp.gmail.com', 'User')
   [Environment]::SetEnvironmentVariable('Smtp__User', '<email gửi>', 'User')
   [Environment]::SetEnvironmentVariable('Smtp__Password', '<mật khẩu ứng dụng 16 ký tự>', 'User')
   [Environment]::SetEnvironmentVariable('Smtp__RedirectAllTo', '<email nhận>', 'User')
   ```

4. **Đóng rồi mở lại** VS Code / terminal để nhận biến mới, sau đó chạy `dotnet run --project t`.

`Smtp__RedirectAllTo` dồn **mọi** email của app về một hộp thư và ghi người nhận gốc vào tiêu đề, ví dụ
`[Gửi cho tho.dien@luxehaven.vn] ...`. Nhờ vậy các tài khoản mẫu `@luxehaven.vn` không bao giờ nhận thư
thật. Lưu ý: khi SMTP bật, email nhắc hóa đơn và hợp đồng của app cũng về hộp thư này, mỗi ngày một lượt.

## 2. Tài khoản dùng trong demo

App tự tạo các tài khoản này khi khởi động.

| Vai trò | Email | Mật khẩu | Ghi chú |
|---|---|---|---|
| Cư dân | `renter@luxehaven.vn` | `Renter@123` | Dùng để gửi sự cố |
| Quản trị | `admin@luxehaven.vn` | `Admin@123` | Dùng để xem và phân công lại |
| Quản lý tòa nhà | `quanly@luxehaven.vn` | không có | Chỉ nhận email |
| Thợ điện | `tho.dien@luxehaven.vn` | không có | Chỉ nhận email |
| Thợ nước | `tho.nuoc@luxehaven.vn` | không có | Chỉ nhận email |
| Thợ máy lạnh | `tho.maylanh@luxehaven.vn` | không có | Chỉ nhận email |
| Thợ mộc | `tho.moc@luxehaven.vn` | không có | Chỉ nhận email |
| Thợ nhôm kính | `tho.nhomkinh@luxehaven.vn` | không có | Chỉ nhận email |

## 3. Các bước

Đăng nhập bằng tài khoản cư dân, vào **Báo sự cố → Tạo yêu cầu**. Với mỗi dòng dưới đây: nhập tiêu đề
ngắn tùy ý, dán mô tả, **để nguyên loại "Khác" và mức ưu tiên "Trung bình"** (trừ bước 10), rồi gửi.
Mỗi lần gửi mất khoảng 10 giây: mô hình phân loại khoảng 4 giây, phần còn lại là gửi email qua Gmail.

Mỗi vai trò chỉ nhận loại thư của mình:

| Tình huống | Thợ | Quản lý | Quản trị viên |
|---|---|---|---|
| Giao được thợ, mức thường | Thư "Giao việc" | không | không |
| Giao được thợ, khẩn cấp | Thư "KHẨN CẤP - Giao việc" | Thư "Cần theo dõi" | không |
| Không có thợ phù hợp | không | Thư "Cần phân công" | không |
| Mô tả mơ hồ | không | Thư "Cần hỏi lại cư dân" | không |
| Mô hình AI không phản hồi | như các dòng trên | như các dòng trên | Thư "Mô hình phân loại sự cố không phản hồi" (tối đa 30 phút một thư) |

Quản trị viên nhận thay quản lý nếu hệ thống chưa có tài khoản quản lý nào.

| # | Mô tả dán vào | Mô hình trả về | Kết quả trong app |
|---|---|---|---|
| 1 | Ổ điện trong phòng ngủ của con em phát ra tiếng xẹt xẹt và có mùi khét, mặt ổ cắm nóng ran. | Điện, khẩn cấp, nguy hiểm | Giao thợ điện, tiêu đề email có "KHẨN CẤP", thân email có cảnh báo an toàn |
| 2 | Vòi nước bồn rửa mặt bị rỉ ở cổ vòi, nước nhỏ giọt suốt đêm. | Nước, trung bình | Giao thợ nước |
| 3 | may lanh phong ngu bat len khong mat, chi co gio thoi | Thiết bị, trung bình | Giao thợ máy lạnh (mô tả gõ không dấu vẫn hiểu) |
| 4 | Cánh cửa tủ bếp dưới bị rớt bản lề, không đóng lại được. | Kết cấu, thấp | Giao thợ mộc; ưu tiên giữ "Trung bình" vì app không hạ mức cư dân chọn |
| 5 | Cửa kính lùa ra ban công bị trật ray, kéo rất nặng và kêu rít. | Kết cấu, trung bình | Giao thợ nhôm kính (nhờ từ khóa "kính") |
| 6 | Thang máy block A đứng im ở tầng 3 từ sáng, bấm gọi không chạy. | Thiết bị, cao | **Mô hình xếp sai**: giao nhầm thợ máy lạnh. Dùng bước này để trình bày việc quản lý phân công lại (mục 4) |
| 7 | Nhà em bị hư rồi, nhờ BQL xem giúp. | Khác, cao, cần hỏi lại | Không giao thợ; cư dân thấy câu hỏi "Anh/chị cho biết căn hộ bị hư cái gì ạ?"; email quản lý có kèm câu hỏi |
| 8 | Chào BQL, tối qua em phát hiện nước từ nhà tắm tràn ra hành lang trong nhà, sáng nay nước đã ngấm tới ổ điện sát sàn ở phòng khách, em thấy ổ điện kêu lẹt xẹt nên đã ngắt cầu dao. Nhờ BQL cho người qua gấp. | Nước, khẩn cấp, nguy hiểm | Giao thợ nước, email "KHẨN CẤP" (mô tả dài nhiều ý vẫn bắt được nguy hiểm) |
| 9 | Nhà bếp có nhiều gián, nhờ BQL phun thuốc giúp. | Côn trùng, thấp | Không có thợ chuyên trách, chỉ quản lý nhận |
| 10 | Em ngửi thấy mùi gas trong bếp dù đã khóa van bình. | Khác, khẩn cấp, nguy hiểm | **Chọn mức ưu tiên "Thấp" trước khi gửi**: app tự nâng lên "Khẩn cấp" và báo quản lý |

## 4. Phía quản lý

1. Đăng xuất, đăng nhập bằng tài khoản quản trị, vào **Bảo trì**.
2. Danh sách có 10 yêu cầu vừa gửi kèm loại, mức ưu tiên và người phụ trách đã được điền sẵn.
3. Mở yêu cầu ở bước 6 (thang máy), chọn người phụ trách khác rồi bấm phân công. Đây là cách xử lý khi
   mô hình giao nhầm.
4. Mở hộp thư demo: 12 email. Bảy thư giao việc cho thợ (bước 1, 2, 3, 4, 5, 6, 8), hai thư "Cần theo dõi"
   cho quản lý (bước 1, 8), ba thư cho quản lý ở các bước không giao thợ (7, 9, 10). Tiêu đề cho biết mỗi thư
   vốn gửi cho ai.
5. Muốn trình bày thư của quản trị viên: tắt Ollama rồi gửi thêm một sự cố. Yêu cầu vẫn được lưu theo lựa
   chọn của cư dân, và quản trị viên nhận thư báo mô hình không phản hồi.

## 5. Các điểm nên nêu khi trình bày

- Mô hình chỉ phân loại; việc chọn thợ và gửi email là code thường, nên đổi nhân sự không cần train lại.
- Thợ được chọn là người đang có ít yêu cầu chưa xong nhất trong đúng chuyên môn.
- Mô hình chỉ được nâng mức ưu tiên, không được hạ (bước 4 và bước 10).
- Khi Ollama tắt hoặc quá 8 giây, app dùng loại và mức ưu tiên cư dân chọn; việc giao thợ và gửi email vẫn chạy.
- Số đo trên bộ test 100 ca: đúng loại 96%, bắt sự cố nguy hiểm 20/20, đúng mức ưu tiên 74%
  (99% lệch tối đa một bậc). Bước 6 là ví dụ cho 4% sai loại.
