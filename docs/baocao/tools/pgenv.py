"""Chạy psql với chuỗi kết nối lấy từ t/.env (không in mật khẩu ra màn hình)."""
import os
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
# Bản mới nhất: pg_dump không chạy được với server mới hơn nó.
PSQL = max(Path(r"C:\Program Files\PostgreSQL").glob("*/bin/psql.exe"), key=lambda p: int(p.parent.parent.name))


def conn():
    line = next(x for x in (ROOT / "t" / ".env").read_text(encoding="utf8").splitlines()
                if x.startswith("ConnectionStrings__DefaultConnection="))
    parts = dict(kv.split("=", 1) for kv in line.split("=", 1)[1].split(";") if "=" in kv)
    return parts


def psql(sql, *flags):
    c = conn()
    env = {**os.environ, "PGPASSWORD": c["Password"], "PGCLIENTENCODING": "UTF8"}
    args = [str(PSQL), "-h", c["Host"], "-p", c.get("Port", "5432"), "-U", c["Username"], "-d", c["Database"],
            "-v", "ON_ERROR_STOP=1", "-X", "-q", *flags]
    return subprocess.run(args, input=sql, capture_output=True, text=True, encoding="utf-8", env=env)


if __name__ == "__main__":
    r = psql("""select version();
select tgname from pg_trigger where not tgisinternal and tgrelid = '"YeuCauBaoTri"'::regclass;
select proname from pg_proc where proname in ('fn_kiem_tra_chuyen_trang_thai','fn_han_xu_ly','sp_giao_viec');
select "TrangThai", count(*) from "YeuCauBaoTri" group by 1 order by 1;
select count(*) as nguoi_dung from "NguoiDung";""", "-A")
    print(r.returncode, r.stdout, r.stderr)
