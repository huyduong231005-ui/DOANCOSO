"""Chạy thử trigger, hàm, thủ tục ở mục 2.5.3 của báo cáo trên CSDL PostgreSQL thật (t/.env).

Lấy nguyên văn hai khối SQL đang in trong build_report.py, cài trong một giao dịch, thử rồi ROLLBACK:
CSDL không bị thay đổi. Kết quả đối chiếu với bảng Rules trong MaintenanceWorkflow.cs.
Chạy: python test_sql_2_5_3.py   (thoát mã 1 nếu có ca sai)
"""
import re
import sys
from pathlib import Path

from pgenv import ROOT, psql

HERE = Path(__file__).resolve().parent
STATUS = ["Open", "Acknowledged", "InProgress", "Resolved", "Closed", "Rejected", "OnHold", "Cancelled"]


def report_sql():
    src = (HERE / "build_report.py").read_text(encoding="utf8")
    blocks = re.findall(r"r\.code\('''(.*?)'''\)", src, re.S)
    trigger = next(b for b in blocks if "fn_kiem_tra_chuyen_trang_thai" in b)
    proc = next(b for b in blocks if "sp_giao_viec" in b)
    return trigger, proc


def expected_pairs():
    raw = (ROOT / "t" / "Infrastructure" / "Maintenance" / "MaintenanceWorkflow.cs").read_text(encoding="utf-8-sig")
    return {(STATUS.index(a), STATUS.index(b))
            for a, b in re.findall(r"\[\(MaintenanceStatus\.(\w+), MaintenanceStatus\.(\w+)\)\]", raw)}


TESTS = r"""
create temp table kq(ca text, ok boolean, ghi text);

-- Đặt trạng thái đầu mà không kích hoạt trigger (session_replication_role = replica).
create function pg_temp.dat(p_id int, p_tt int, p_ktv text, p_ghichu text) returns void as $$
begin
  perform set_config('session_replication_role', 'replica', true);
  update "YeuCauBaoTri" set "TrangThai" = p_tt, "NguoiPhuTrachId" = p_ktv, "GhiChuXuLy" = p_ghichu where "Id" = p_id;
  perform set_config('session_replication_role', 'origin', true);
end $$ language plpgsql;

do $$
declare v_id int; v_ktv text; f int; t int;
begin
  select "Id" into v_id from "YeuCauBaoTri" order by "Id" limit 1;
  select "Id" into v_ktv from "NguoiDung" order by "Id" limit 1;
  -- 1) Mọi cặp (từ, sang) khác nhau, đã có người phụ trách và kết quả xử lý.
  for f in 0..7 loop for t in 0..7 loop
    continue when f = t;
    perform pg_temp.dat(v_id, f, v_ktv, 'Đã thay ổ cắm');
    begin
      update "YeuCauBaoTri" set "TrangThai" = t where "Id" = v_id;
      insert into kq values (format('chuyen %s->%s', f, t), true, null);
    exception when others then
      insert into kq values (format('chuyen %s->%s', f, t), false, sqlerrm);
    end;
  end loop; end loop;
  -- 2) Bắt đầu xử lý khi chưa giao người.
  perform pg_temp.dat(v_id, 1, null, null);
  begin
    update "YeuCauBaoTri" set "TrangThai" = 2 where "Id" = v_id;
    insert into kq values ('bat dau khi chua giao', true, null);
  exception when others then insert into kq values ('bat dau khi chua giao', false, sqlerrm);
  end;
  -- 3) Hoàn tất khi chưa ghi kết quả.
  perform pg_temp.dat(v_id, 2, v_ktv, '   ');
  begin
    update "YeuCauBaoTri" set "TrangThai" = 3 where "Id" = v_id;
    insert into kq values ('hoan tat khong ket qua', true, null);
  exception when others then insert into kq values ('hoan tat khong ket qua', false, sqlerrm);
  end;
  -- 4) Cập nhật cột khác, giữ trạng thái: trigger không chặn.
  perform pg_temp.dat(v_id, 4, v_ktv, 'x');
  begin
    update "YeuCauBaoTri" set "TieuDe" = "TieuDe" where "Id" = v_id;
    insert into kq values ('sua cot khac', true, null);
  exception when others then insert into kq values ('sua cot khac', false, sqlerrm);
  end;
end $$;

-- 5) fn_han_xu_ly khớp công thức của ứng dụng (lúc báo + PhutHoanTat) cho mọi mức SLA.
insert into kq
select 'han muc ' || s."MucDoUuTien",
       fn_han_xu_ly(timestamp '2026-11-02 08:00', s."MucDoUuTien")
         = timestamp '2026-11-02 08:00' + make_interval(mins => s."PhutHoanTat"),
       null
from "MucSla" s;

-- 6) sp_giao_viec.
do $$
declare v_id int; v_ktv text; v_ql text; v_tt int; v_nhan timestamp; v_ls int; v_ls2 int;
begin
  select "Id" into v_id from "YeuCauBaoTri" order by "Id" limit 1;
  select "Id" into v_ktv from "NguoiDung" order by "Id" limit 1;
  select "Id" into v_ql from "NguoiDung" order by "Id" desc limit 1;
  -- a) Mới gửi → Đã tiếp nhận, ghi người phụ trách, mốc tiếp nhận, một dòng lịch sử (0 → 1).
  perform pg_temp.dat(v_id, 0, null, null);
  update "YeuCauBaoTri" set "NgayTiepNhan" = null where "Id" = v_id;
  -- Đếm trước và sau: dữ liệu thử có thể đã có dòng lịch sử 0 → 1 từ trước.
  select count(*) into v_ls from "LichSuTrangThaiSuCo"
   where "YeuCauBaoTriId" = v_id and "TuTrangThai" = 0 and "SangTrangThai" = 1 and "GhiChu" = 'Giao thử';
  call sp_giao_viec(v_id, v_ktv, v_ql, 'Giao thử');
  select "TrangThai", "NgayTiepNhan" into v_tt, v_nhan from "YeuCauBaoTri" where "Id" = v_id;
  select count(*) into v_ls2 from "LichSuTrangThaiSuCo"
   where "YeuCauBaoTriId" = v_id and "TuTrangThai" = 0 and "SangTrangThai" = 1 and "GhiChu" = 'Giao thử'
     and "NguoiThucHienId" = v_ql;
  insert into kq values ('giao khi moi gui', v_tt = 1 and v_nhan is not null and v_ls2 = v_ls + 1
    and (select "NguoiPhuTrachId" from "YeuCauBaoTri" where "Id" = v_id) = v_ktv, null);
  -- b) Đang xử lý: giữ trạng thái, ghi lịch sử giao lại (2 → 2).
  perform pg_temp.dat(v_id, 2, v_ktv, null);
  call sp_giao_viec(v_id, v_ql, v_ql, 'Giao lại');
  insert into kq values ('giao lai khi dang xu ly',
    (select "TrangThai" from "YeuCauBaoTri" where "Id" = v_id) = 2 and exists (
      select 1 from "LichSuTrangThaiSuCo" where "YeuCauBaoTriId" = v_id and "TuTrangThai" = 2 and "SangTrangThai" = 2),
    null);
  -- c) Đã đóng: thủ tục từ chối giao.
  perform pg_temp.dat(v_id, 4, v_ktv, 'x');
  begin
    call sp_giao_viec(v_id, v_ktv, v_ql, 'x');
    insert into kq values ('giao khi da dong', true, null);
  exception when others then insert into kq values ('giao khi da dong', false, sqlerrm);
  end;
  -- d) Yêu cầu không tồn tại.
  begin
    call sp_giao_viec(-1, v_ktv, v_ql, 'x');
    insert into kq values ('giao yeu cau khong co', true, null);
  exception when others then insert into kq values ('giao yeu cau khong co', false, sqlerrm);
  end;
end $$;

select ca || '|' || ok || '|' || coalesce(ghi, '') from kq;
"""


# Ràng buộc cài bằng migration tuần 4 (khoá ngoại ghép, CHECK miền giá trị): chỉ thử trên CSDL đã cài.
CONSTRAINTS = r"""
do $$
declare v_can int; v_toa int; v_yc int;
begin
  select "Id" into v_can from "CanHo" order by "Id" limit 1;
  select "Id" into v_toa from "ToaNha" where "Id" <> (select "ToaNhaId" from "CanHo" where "Id" = v_can) limit 1;
  select "Id" into v_yc from "YeuCauBaoTri" order by "Id" limit 1;
  begin
    update "CanHo" set "ToaNhaId" = v_toa, "MaCanHo" = 'ZZ-THU' where "Id" = v_can;
    insert into kq values ('can lech toa voi tang', true, null);
  exception when others then insert into kq values ('can lech toa voi tang', false, sqlerrm);
  end;
  perform set_config('session_replication_role', 'replica', true);   -- tắt trigger để chỉ thử CHECK
  begin
    update "YeuCauBaoTri" set "TrangThai" = 9 where "Id" = v_yc;
    insert into kq values ('trang thai ngoai mien', true, null);
  exception when others then insert into kq values ('trang thai ngoai mien', false, sqlerrm);
  end;
  begin
    update "YeuCauBaoTri" set "LoaiBaoTri" = 50 where "Id" = v_yc;
    insert into kq values ('loai ngoai mien', true, null);
  exception when others then insert into kq values ('loai ngoai mien', false, sqlerrm);
  end;
  perform set_config('session_replication_role', 'origin', true);
end $$;
"""
CONSTRAINT_EXPECT = {"can lech toa voi tang": "FK_CanHo_Tang_ToaNha", "trang thai ngoai mien": "CK_YeuCauBaoTri_TrangThai",
                     "loai ngoai mien": "CK_YeuCauBaoTri_LoaiBaoTri"}


def run_pass(label, setup):
    """Một lượt thử trong giao dịch ROLLBACK; trả về số ca sai."""
    tests = TESTS.replace("select ca || '|'", CONSTRAINTS + "\nselect ca || '|'") if label.startswith("CSDL") else TESTS
    r = psql("begin;\n" + setup + "\n" + tests + "\nrollback;\n", "-A", "-t")
    if r.returncode:
        print(f"[{label}] LỖI SQL:", r.stderr)
        return 1
    rows = [line.split("|", 2) for line in r.stdout.splitlines() if line.count("|") >= 2]
    res = {ca: (ok == "true", msg) for ca, ok, msg in rows}  # boolean nối chuỗi in ra "true" / "false"

    want = expected_pairs()
    bad = []
    for f in range(8):
        for t in range(8):
            if f != t and res[f"chuyen {f}->{t}"][0] != ((f, t) in want):
                bad.append(f"chuyển {STATUS[f]}→{STATUS[t]}: trigger {'cho' if res[f'chuyen {f}->{t}'][0] else 'chặn'}, "
                           f"Rules {'cho' if (f, t) in want else 'chặn'}")
    expect = {"bat dau khi chua giao": False, "hoan tat khong ket qua": False, "sua cot khac": True,
              "giao khi moi gui": True, "giao lai khi dang xu ly": True, "giao khi da dong": False,
              "giao yeu cau khong co": False}
    expect.update({ca: True for ca in res if ca.startswith("han muc")})
    for ca, ok in expect.items():
        if res[ca][0] != ok:
            bad.append(f"{ca}: được {res[ca]}, mong đợi {ok}")
    for ca, name in CONSTRAINT_EXPECT.items():
        if ca in res:
            expect[ca] = False
            if res[ca][0] or name not in res[ca][1]:
                bad.append(f"{ca}: được {res[ca]}, mong đợi bị chặn bởi {name}")
    allowed = sum(1 for f in range(8) for t in range(8) if f != t and res[f"chuyen {f}->{t}"][0])
    print(f"[{label}] Chuyển trạng thái: thử 56 cặp, trigger cho {allowed}, Rules cho {len(want)}")
    for ca in expect:
        print(f"  {ca}: {'chạy được' if res[ca][0] else 'bị chặn'}{' – ' + res[ca][1] if res[ca][1] else ''}")
    print(f"[{label}] {len(expect) + 56 - len(bad)}/{len(expect) + 56} ca đúng; sai lệch: {len(bad)}")
    for b in bad:
        print("  " + b)
    return len(bad)


def main():
    sys.stdout.reconfigure(encoding="utf-8")
    trigger, proc = report_sql()
    sp = proc[proc.index("CREATE OR REPLACE PROCEDURE"):]
    # Lượt 1: trigger, hàm đã cài bằng migration (sp_giao_viec không cài nên nạp tạm từ báo cáo để thử).
    # Lượt 2: gỡ tạm bản đã cài, cài đúng đoạn SQL in trong báo cáo rồi thử.
    reinstall = 'DROP TRIGGER IF EXISTS trg_yeu_cau_chuyen_trang_thai ON "YeuCauBaoTri";\n' + trigger + "\n" + proc
    bad = run_pass("CSDL đã cài (migration)", sp) + run_pass("SQL in trong báo cáo", reinstall)
    print("Mọi lượt đều chạy trong giao dịch và ROLLBACK, CSDL không đổi.")
    return 1 if bad else 0


if __name__ == "__main__":
    sys.exit(main())
