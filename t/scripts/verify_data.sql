SET NOCOUNT ON;

PRINT '=== 1. NguoiDung (Identity-derived) ===';
SELECT TOP 5 Id, UserName, Email, HoTen, SoDienThoai, LaChuNha, DanhXungChuNha
FROM NguoiDung;

PRINT '';
PRINT '=== 2. VaiTro + Quyen ===';
SELECT v.Name AS VaiTro, COUNT(vq.QuyenId) AS SoQuyen
FROM VaiTro v
LEFT JOIN VaiTro_Quyen vq ON v.Id = vq.VaiTroId
GROUP BY v.Name
ORDER BY v.Name;

PRINT '';
PRINT '=== 3. CanHo - sample 3 dong moi nhat ===';
SELECT TOP 3
    c.Id, c.TieuDe, c.Gia, c.DiaChi, c.SoPhongNgu, c.SoPhongTam,
    k.Ten AS KhuVuc, d.Ten AS DanhMuc, u.HoTen AS ChuNha
FROM CanHo c
JOIN KhuVuc k ON c.KhuVucId = k.Id
JOIN DanhMuc d ON c.DanhMucId = d.Id
JOIN NguoiDung u ON c.ChuNhaId = u.Id
ORDER BY c.Id DESC;

PRINT '';
PRINT '=== 4. CanHo + TienIch (junction many-to-many) ===';
SELECT TOP 3
    c.TieuDe,
    STUFF((
        SELECT ', ' + t.Ten
        FROM CanHo_TienIch ct
        JOIN TienIch t ON ct.TienIchId = t.Id
        WHERE ct.CanHoId = c.Id
        FOR XML PATH('')
    ), 1, 2, '') AS DanhSachTienIch
FROM CanHo c
ORDER BY c.Id;

PRINT '';
PRINT '=== 5. HopDongThue + NguoiThueChinh + CanHo ===';
SELECT
    h.SoHopDong, h.TienThueThang, h.TienDatCoc, h.NgayBatDau, h.NgayKetThuc,
    h.TrangThai, c.TieuDe AS CanHo, u.HoTen AS NguoiThue
FROM HopDongThue h
JOIN CanHo c ON h.CanHoId = c.Id
JOIN NguoiDung u ON h.NguoiThueChinhId = u.Id;

PRINT '';
PRINT '=== 6. HoaDon + ChiTietHoaDon ===';
SELECT h.SoHoaDon, h.LoaiHoaDon, h.TongTien, h.ConLai, h.TrangThai,
       (SELECT COUNT(*) FROM ChiTietHoaDon WHERE HoaDonId = h.Id) AS SoDongChiTiet
FROM HoaDon h;

PRINT '';
PRINT '=== 7. NhatKyHeThong - sample 5 action gan day ===';
SELECT TOP 5 TenBang, HanhDong, ThoiGian, TenNguoiDung
FROM NhatKyHeThong
ORDER BY ThoiGian DESC;

PRINT '';
PRINT '=== 8. KiemKeCanHo + YeuCauBaoTri + GiaoDichDatCoc ===';
SELECT 'KiemKeCanHo' AS Loai, COUNT(*) AS SoRecord FROM KiemKeCanHo
UNION ALL SELECT 'YeuCauBaoTri', COUNT(*) FROM YeuCauBaoTri
UNION ALL SELECT 'GiaoDichDatCoc', COUNT(*) FROM GiaoDichDatCoc;

PRINT '';
PRINT '=== 9. Verify FK integrity - cac ban ghi co FK NULL/invalid ===';
SELECT 'CanHo co ChuNhaId khong ton tai' AS KiemTra, COUNT(*) AS SoRecord
FROM CanHo c WHERE NOT EXISTS (SELECT 1 FROM NguoiDung u WHERE u.Id = c.ChuNhaId)
UNION ALL
SELECT 'CanHo co KhuVucId khong ton tai', COUNT(*)
FROM CanHo c WHERE NOT EXISTS (SELECT 1 FROM KhuVuc k WHERE k.Id = c.KhuVucId)
UNION ALL
SELECT 'HopDongThue co CanHoId khong ton tai', COUNT(*)
FROM HopDongThue h WHERE NOT EXISTS (SELECT 1 FROM CanHo c WHERE c.Id = h.CanHoId)
UNION ALL
SELECT 'HoaDon co HopDongId khong ton tai', COUNT(*)
FROM HoaDon h WHERE NOT EXISTS (SELECT 1 FROM HopDongThue l WHERE l.Id = h.HopDongId);

PRINT '';
PRINT '=== 10. Soft-delete: ban ghi DaXoa = 1 ===';
SELECT 'CanHo' AS Bang, SUM(CAST(DaXoa AS INT)) AS DaXoa FROM CanHo
UNION ALL SELECT 'NguoiDung', SUM(CAST(DaXoa AS INT)) FROM NguoiDung
UNION ALL SELECT 'HopDongThue', SUM(CAST(DaXoa AS INT)) FROM HopDongThue
UNION ALL SELECT 'HoaDon', SUM(CAST(DaXoa AS INT)) FROM HoaDon;
