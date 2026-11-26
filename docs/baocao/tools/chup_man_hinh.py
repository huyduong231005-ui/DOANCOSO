"""Chụp lại ảnh màn hình Chương 4 từ ứng dụng đang chạy ở localhost (dữ liệu thật của CSDL).

Dùng: python chup_man_hinh.py [4.1 4.3 ...]   (bỏ trống = chụp cả 7 hình)
Tài khoản là tài khoản thử của dữ liệu mẫu (SeedData). Trước khi chụp, script ẩn đuôi email và tên mã của ứng
dụng trên trang để báo cáo không lộ tên này (Bảng 4.1 cũng chỉ ghi phần tên trước @).
"""
import sys
from pathlib import Path

from playwright.sync_api import sync_playwright

BASE = "http://localhost:5073"
OUT = Path(__file__).resolve().parent.parent / "images_v2"
QUAN_LY = ("quanly@luxehaven.vn", "Quanly@123")
CU_DAN = ("renter@luxehaven.vn", "Renter@123")

# (số hình, tên file, tài khoản, đường dẫn, cách chụp: "trang" | "khung" | selector chứa chữ)
HINH = [
    ("4.1", "Trang chi tiet can ho", QUAN_LY, "/Admin/Apartments/Details/93", "trang", 1700),  # bảng hoá đơn đủ cột
    ("4.2", "Hoa don qua han co phi tre", QUAN_LY, "/Admin/Invoices/Details/211", "trang"),
    ("4.3", "Cu dan theo doi yeu cau", CU_DAN, "/Tenant/Maintenance/Details/6", "trang"),
    ("4.4", "Lich su xu ly ngoai le", QUAN_LY, "/Admin/Maintenance/Details/7", "Lịch sử xử lý"),
    ("4.5", "Bang dieu khien", QUAN_LY, "/Admin/Dashboard", "khung"),
    ("4.6", "Cong no phan tuoi", QUAN_LY, "/Admin/Reports/Arrears", "khung"),
    ("4.7", "Bao cao KPI su co", QUAN_LY, "/Admin/Reports/Maintenance?from=2026-11-01&to=2026-11-25", "trang", 1900),
]

AN_TEN = """() => {
  const thay = s => s.replace(/@luxehaven\\.vn/gi, '').replace(/Luxe Haven\\s*/g, '');
  const w = document.createTreeWalker(document.body, NodeFilter.SHOW_TEXT);
  for (let n; (n = w.nextNode());) n.nodeValue = thay(n.nodeValue);
  document.querySelectorAll('input[value]').forEach(i => i.value = thay(i.value));
  // trình duyệt chụp không theo locale vi-VN cho ô ngày: hiện dd/mm/yyyy như máy tiếng Việt
  document.querySelectorAll('input[type=date]').forEach(i => {
    const [y, m, d] = i.value.split('-'); if (!d) return;
    i.type = 'text'; i.value = `${d}/${m}/${y}`;
  });
}"""


def dang_nhap(page, tk):
    page.goto(BASE + "/Home/Login")
    page.fill("#Email", tk[0])
    page.fill("#login-password", tk[1])
    page.click("form button[type=submit]")
    page.wait_for_load_state("networkidle")


def main(chon):
    OUT.mkdir(exist_ok=True)
    with sync_playwright() as p:
        browser = p.chromium.launch(channel="chromium", args=["--lang=vi-VN"])   # ô ngày hiện dd/mm/yyyy
        ctx = {}
        for so, ten, tk, url, cach, *rong in HINH:
            rong = rong[0] if rong else 1440   # bảng hai cột của báo cáo KPI cần khung rộng hơn
            if chon and so not in chon:
                continue
            if (tk, rong) not in ctx:
                c = browser.new_context(viewport={"width": rong, "height": 900}, device_scale_factor=2,
                                        locale="vi-VN", timezone_id="Asia/Ho_Chi_Minh")
                pg = c.new_page()
                dang_nhap(pg, tk)
                ctx[(tk, rong)] = pg
            page = ctx[(tk, rong)]
            page.goto(BASE + url)
            page.wait_for_load_state("networkidle")
            if page.url.rstrip("/").endswith(("Login", "AccessDenied")) or "403" in page.title():
                raise SystemExit(f"{so}: không vào được {url} ({page.url})")
            page.evaluate(AN_TEN)
            page.wait_for_timeout(600)            # biểu đồ vẽ xong
            f = OUT / f"Hinh {so} {ten}.png"
            # Chỉ chụp vùng nội dung (thanh trên + trang), bỏ thanh bên trái và ngăn kéo ẩn.
            main = page.locator(".admin-main").first
            if cach == "trang":
                main.screenshot(path=str(f))
            elif cach == "khung":
                b = main.bounding_box()
                page.screenshot(path=str(f), clip={"x": b["x"], "y": 0, "width": b["width"], "height": 900})
            else:
                page.locator(".card", has_text=cach).first.screenshot(path=str(f))
            print("ok", so, f.name)
        browser.close()


if __name__ == "__main__":
    main(sys.argv[1:])
