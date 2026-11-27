# Quy trình tìm và sửa lỗi báo cáo

Công cụ chính: `tools/kiem_loi_bao_cao.py`. Mã thoát = số lỗi; 0 là sạch.

```
python kiem_loi_bao_cao.py          # kiểm một lần, liệt kê lỗi theo nhóm
python kiem_loi_bao_cao.py --lap    # chạy lại mỗi khi file nguồn đổi, dừng khi 0 lỗi
python kiem_loi_bao_cao.py --chup   # lưu bản chữ của file Word làm mốc (ngay sau khi dựng)
```

## Công cụ kiểm gì

| Nhóm | Kiểm | Sửa ở đâu |
|------|------|-----------|
| Sơ đồ | Chạy `kiem_tra_so_do.py`: thực thể, thuộc tính, bản số, khoá khớp CSDL thật | `so_do_v2.py` |
| CSDL | Mỗi khoá ngoại phải được dùng trong logic (lọc, so sánh, đếm, nối), không chỉ ghi/hiển thị | Mã nguồn `t/` + migration |
| Chữ | Từ cấm; Hình/Bảng/mục được nhắc phải tồn tại và ngược lại; mã BR/FR/NFR/UC… có định nghĩa; viết tắt có dùng; tài liệu tham khảo đúng thứ tự, đều được trích; số bảng/thực thể khớp CSDL; tên dạng CamelCase phải có trong CSDL | `gon_bao_cao.py`, `noi_dung_ch2.py`, `cau_viet_lai.py` |
| Chỉnh tay | File Word khác mốc lần dựng trước → người dùng đã sửa tay | Đưa chỗ sửa vào công cụ trước khi dựng lại |

## Các bước

1. **Giữ chỗ người dùng sửa.** Chạy công cụ; nếu nhóm "Chỉnh tay" có lỗi, chép từng chỗ sửa vào công cụ dựng
   (hoặc sửa thẳng trong Word) rồi mới dựng lại. `sua_bao_cao_v2.py` cũng tự dừng khi file Word khác mốc, nên không
   thể dựng đè nhầm; chỉ chạy `--chup` khi đã đưa hết chỗ sửa vào công cụ.
2. **Liệt kê lỗi:** `python kiem_loi_bao_cao.py --lap` để cửa sổ chạy; mỗi lần lưu file nguồn nó kiểm lại.
3. **Sửa theo nhóm**, từ gốc ra ngọn (CSDL → sơ đồ → chữ), vì sửa gốc kéo theo sơ đồ và chữ:
   - CSDL: `pg_dump` sao lưu vào `db_backup/` → thêm migration → `dotnet build` → `thu_mig.py` (bản _thu) →
     `thu_mig.py --that` (bản thật) → đối chiếu số liệu chính (tổng hoá đơn) → `lay_luoc_do.py`.
   - Sơ đồ: sửa `so_do_v2.py` → vẽ lại hình đó → `kiem_tra_so_do.py` → **mở ảnh xem bằng mắt**.
   - Chữ: sửa câu trong công cụ (áp cho mọi chỗ cùng loại, không chỉ chỗ bị báo).
4. **Dựng nháp** khi Word đang mở: đặt `BAOCAO_OUT` ra thư mục tạm, chạy `sua_bao_cao_v2.py` và công cụ kiểm trên bản nháp.
5. **Dựng thật:** đóng Word → `sua_bao_cao_v2.py` → `cap_nhat_word.ps1` (cập nhật mục lục, xuất PDF, tự lưu mốc) →
   `kiem_loi_bao_cao.py` phải báo 0 lỗi.
   Bước dựng tự dừng khi một câu thay không còn khớp (câu nguồn đã bị bước trước đổi) hoặc ô bảng cần sửa không có;
   đọc dòng "SUA_SAU không gặp câu nguồn" trong đầu ra, phải là "không".
6. **Xem bằng mắt** các trang đã đổi trong PDF (bố cục, hình nhảy trang, chữ). Công cụ đạt không thay được bước này.
7. **Ghi sổ** `DanhSach_YeuCau_BaoCao.md`; commit khi được yêu cầu.

## Thêm luật kiểm mới

Khi phát hiện một loại lỗi mới bằng mắt, thêm luật vào `kiem_loi_bao_cao.py` để lần sau tự bắt. Báo nhầm (ví dụ tên
riêng như IoT bị coi là tên bảng) thì thêm vào danh sách bỏ qua tương ứng, không tắt cả luật.
