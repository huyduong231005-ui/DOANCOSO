"""Đọc lược đồ thật từ PostgreSQL (cột, kiểu, NULL, PK, FK, UNIQUE, CHECK) ra luoc_do.json.
Sơ đồ quan hệ và từ điển dữ liệu của báo cáo lấy từ file này để luôn khớp với CSDL."""
import json
import sys
from pathlib import Path

import pgenv

TABLES = ["Khu", "ToaNha", "Tang", "CanHo", "CuTru", "NguoiDung", "VaiTro", "NguoiDung_VaiTro", "Quyen",
          "VaiTro_Quyen", "MucUuTien", "YeuCauBaoTri", "AnhYeuCau", "LichSuYeuCau", "NhatKyHeThong",
          "LoaiDichVu", "ChiSoDichVu", "PhiDinhKy", "HoaDon", "ChiTietHoaDon", "ThanhToan"]
AUDIT = {"NgayTao", "NguoiTao", "NgayCapNhat", "NguoiCapNhat", "DaXoa", "NgayXoa", "NguoiXoa", "PhienBan"}
IDENTITY_TECH = {"NormalizedUserName", "NormalizedEmail", "EmailConfirmed", "SecurityStamp", "ConcurrencyStamp",
                 "PhoneNumber", "PhoneNumberConfirmed", "TwoFactorEnabled", "LockoutEnd", "LockoutEnabled",
                 "AccessFailedCount", "NormalizedName"}

SQL = """
select json_agg(t) from (
  select c.table_name, c.column_name, c.ordinal_position, c.is_nullable, c.is_generated,
         case when c.data_type = 'character varying' then 'varchar(' || c.character_maximum_length || ')'
              when c.data_type = 'numeric' then 'numeric(' || c.numeric_precision || ',' || c.numeric_scale || ')'
              when c.data_type like 'timestamp%' then 'timestamp'
              else c.data_type end as type
  from information_schema.columns c where c.table_schema = 'public' and c.table_name = any(%s)
) t;
select json_agg(t) from (
  select tc.table_name, tc.constraint_type, tc.constraint_name, kcu.column_name,
         ccu.table_name as ref_table, rc.delete_rule
  from information_schema.table_constraints tc
  join information_schema.key_column_usage kcu on kcu.constraint_name = tc.constraint_name and kcu.table_name = tc.table_name
  left join information_schema.referential_constraints rc on rc.constraint_name = tc.constraint_name
  left join information_schema.constraint_column_usage ccu on ccu.constraint_name = tc.constraint_name
       and tc.constraint_type = 'FOREIGN KEY'
  where tc.table_schema = 'public' and tc.table_name = any(%s)
) t;
select json_agg(t) from (
  select tablename as table_name, indexname, indexdef from pg_indexes
  where schemaname = 'public' and tablename = any(%s) and indexdef like 'CREATE UNIQUE%%'
) t;
select json_agg(t) from (
  select conrelid::regclass::text as table_name, conname, pg_get_constraintdef(oid) as def
  from pg_constraint where contype = 'c' and connamespace = 'public'::regnamespace
) t;
"""


def main():
    arr = "ARRAY[" + ",".join(f"'{t}'" for t in TABLES) + "]"
    parts = []
    for q in SQL.replace("%s", arr).replace("%%", "%").split(";"):
        if not q.strip():
            continue
        r = pgenv.psql(q + ";", "-A", "-t")
        if r.returncode:
            sys.exit(r.stderr)
        parts.append(json.loads(r.stdout))   # json_agg in nhiều dòng: đọc nguyên khối
    cols, cons, uniq, checks = parts
    out = {}
    for t in TABLES:
        cs = sorted((c for c in cols if c["table_name"] == t), key=lambda c: c["ordinal_position"])
        pk = {k["column_name"] for k in cons if k["table_name"] == t and k["constraint_type"] == "PRIMARY KEY"}
        fk = {k["column_name"]: (k["ref_table"], k["delete_rule"]) for k in cons
              if k["table_name"] == t and k["constraint_type"] == "FOREIGN KEY"}
        uq_cols = set()
        uq_defs = []
        for u in uniq:
            if u["table_name"] != t or u["indexname"].startswith("PK_"):
                continue
            inside = u["indexdef"].split("(", 1)[1].split(")")[0]
            names = [x.strip().strip('"') for x in inside.split(",")]
            where = u["indexdef"].split(" WHERE ", 1)[1] if " WHERE " in u["indexdef"] else ""
            uq_defs.append({"cols": names, "where": where, "name": u["indexname"]})
            if len(names) == 1 and not where:   # UNIQUE có điều kiện lọc không đánh dấu U ở cột
                uq_cols.add(names[0])
        out[t] = {
            "columns": [{"name": c["column_name"], "type": c["type"], "null": c["is_nullable"] == "YES",
                         "pk": c["column_name"] in pk, "fk": fk.get(c["column_name"], [None])[0],
                         "on_delete": fk.get(c["column_name"], [None, None])[1],
                         "unique": c["column_name"] in uq_cols, "generated": c["is_generated"] == "ALWAYS"}
                        for c in cs if c["column_name"] not in AUDIT | IDENTITY_TECH],
            "unique": uq_defs,
            "checks": [{"name": k["conname"], "def": k["def"]} for k in checks if k["table_name"].strip('"') == t],
        }
    path = Path(__file__).with_name("luoc_do.json")
    path.write_text(json.dumps(out, ensure_ascii=False, indent=1), encoding="utf8")
    print(path, sum(len(v["columns"]) for v in out.values()))


if __name__ == "__main__":
    main()
