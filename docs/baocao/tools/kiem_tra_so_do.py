"""Kiểm tra tự động các sơ đồ của so_do_v2.py theo quy cách UML/ERD và theo CSDL thật (luoc_do.json).
Chạy: python kiem_tra_so_do.py   -> in danh sách lỗi, mã thoát 1 nếu còn lỗi.

Quy tắc (theo góp ý GVHD và UML 2.5):
  Hoạt động: nút quyết định có đúng 1 luồng vào, >= 2 luồng ra đều có điều kiện, trước nó là hành động "Kiểm tra ...",
             có câu hỏi; nút gộp >= 2 vào, 1 ra; hành động 1 vào 1 ra, tên ngắn, không diễn giải (không có , ; ( : );
             mọi nút đi được từ nút bắt đầu và tới được nút kết thúc; đường gấp không cắt qua nút khác.
  Tuần tự:   mọi thông điệp trừ thông điệp đầu đi ra từ thanh kích hoạt đang mở; lời gọi đồng bộ có trả về;
             thanh của bên được gọi kết thúc trước thanh của bên gọi.
  Use case:  tên là động từ + đối tượng theo mục tiêu tác nhân, không là bước nội bộ; use case nào cũng nối tới
             tác nhân hoặc qua include/extend.
  ERD:       mỗi mối kết hợp có 2 nhánh với bản số (min,max); khớp từng khoá ngoại của CSDL (bên (x,1) giữ khoá ngoại,
             min = 0 thì khoá ngoại cho phép NULL); n–n ứng với bảng trung gian.
  Lớp:       bội số khớp khoá ngoại (1 khi NOT NULL, 0..1 khi NULL); hợp thành chỉ khi xoá cha thì xoá con (CASCADE).
  Chung:     các hộp không chồng lên nhau.
"""
import re
import sys
from collections import defaultdict

import so_do_v2 as sd

ERRORS = []


def err(d, msg):
    ERRORS.append(f"[{d['name']}] {msg}")


def boxes(d, kinds):
    return {n["key"]: (n["x"], n["y"], n.get("w", 0), n.get("h", 0)) for n in d["nodes"] if n["kind"] in kinds}


def overlap(a, b, pad=2):
    ax, ay, aw, ah = a
    bx, by, bw, bh = b
    return ax + pad < bx + bw and bx + pad < ax + aw and ay + pad < by + bh and by + pad < ay + ah


def seg_hits_box(p, q, box, pad=3):
    """Đoạn thẳng ngang/dọc p-q có đi xuyên qua hộp không (chỉ xét đoạn vuông góc)."""
    x, y, w, h = box
    x1, y1, x2, y2 = x + pad, y + pad, x + w - pad, y + h - pad
    (px, py), (qx, qy) = p, q
    if py == qy and y1 < py < y2:
        return min(px, qx) < x2 and max(px, qx) > x1
    if px == qx and x1 < px < x2:
        return min(py, qy) < y2 and max(py, qy) > y1
    return False


def seg_cross_box(p, q, box, pad=4):
    """Đoạn thẳng bất kỳ p-q có đi qua phần trong của hộp không (cắt Liang–Barsky)."""
    x, y, w, h = box
    x1, y1, x2, y2 = x + pad, y + pad, x + w - pad, y + h - pad
    (px, py), (qx, qy) = p, q
    dx, dy = qx - px, qy - py
    t0, t1 = 0.0, 1.0
    for pk, qk in ((-dx, px - x1), (dx, x2 - px), (-dy, py - y1), (dy, y2 - py)):
        if pk == 0:
            if qk < 0:
                return False
            continue
        t = qk / pk
        if pk < 0:
            t0 = max(t0, t)
        else:
            t1 = min(t1, t)
        if t0 > t1:
            return False
    return True


def seg_cross(p1, q1, p2, q2):
    """Giao điểm trong của hai đoạn thẳng (không tính chạm đầu mút), hoặc None."""
    def o(a, b, c):
        return (b[0] - a[0]) * (c[1] - a[1]) - (b[1] - a[1]) * (c[0] - a[0])
    d1, d2, d3, d4 = o(p2, q2, p1), o(p2, q2, q1), o(p1, q1, p2), o(p1, q1, q2)
    if not (d1 * d2 < 0 and d3 * d4 < 0):
        return None
    t = d3 / (d3 - d4)
    return p2[0] + t * (q2[0] - p2[0]), p2[1] + t * (q2[1] - p2[1])


def check_rd(d):
    """Lược đồ quan hệ: đủ khoá chính, khoá ngoại; đường nối không cắt qua bảng khác; hai khoá ngoại cùng cặp bảng
    phải tách và có tên."""
    for n in d["nodes"]:
        if n["kind"] != "entity":
            continue
        shown = {c["name"].lstrip("/") for c in n["columns"]}
        for c in sd.SCHEMA[n["key"]]["columns"]:
            if (c["pk"] or c["fk"]) and c["name"] not in shown:
                err(d, f"bảng {n['key']} thiếu khoá {c['name']}")
    b = boxes(d, ("entity",))
    center = {k: (x + w / 2, y + h / 2) for k, (x, y, w, h) in b.items()}
    seen = {}
    for e in d["edges"]:
        pts = e.get("points") or [center[e["from"]], center[e["to"]]]
        for p, q in zip(pts, pts[1:]):
            for k, box in b.items():
                if k not in (e["from"], e["to"]) and seg_cross_box(p, q, box):
                    err(d, f"đường {e['from']} → {e['to']} cắt qua bảng {k}")
        seen.setdefault((e["from"], e["to"]), []).append(e)
    segs = []
    for i, e in enumerate(d["edges"]):
        pts = e.get("points") or [center[e["from"]], center[e["to"]]]
        segs += [(i, e, p, q) for p, q in zip(pts, pts[1:])]
    for i1, e1, p1, q1 in segs:
        for i2, e2, p2, q2 in segs:
            x = seg_cross(p1, q1, p2, q2) if i1 < i2 else None
            if not x:
                continue
            chung = {e1["from"], e1["to"]} & {e2["from"], e2["to"]}
            # hai đường cùng nối vào một bảng được gặp nhau sát bảng đó (trong 60px), không được cắt nhau ở xa
            if any(b[k][0] - 60 < x[0] < b[k][0] + b[k][2] + 60 and b[k][1] - 60 < x[1] < b[k][1] + b[k][3] + 60
                   for k in chung):
                continue
            err(d, f"đường {e1['from']} → {e1['to']} cắt đường {e2['from']} → {e2['to']}")
    for (a, c), es in seen.items():
        if len(es) > 1:
            if len({str(e.get("points")) for e in es}) < len(es) or not all(e.get("name") for e in es):
                err(d, f"{len(es)} khoá ngoại {c} → {a} vẽ chồng nhau hoặc thiếu tên khoá")


def check_overlaps(d, kinds):
    b = boxes(d, kinds)
    ks = list(b)
    for i, k in enumerate(ks):
        for k2 in ks[i + 1:]:
            if overlap(b[k], b[k2]):
                err(d, f"hộp {k} chồng lên {k2}")


# ------------------------------------------------------------------ hoạt động
BAD_ACTION = re.compile(r"[,;:(]")


def check_activity(d):
    kind = {n["key"]: n["kind"] for n in d["nodes"]}
    name = {n["key"]: n.get("name", "") for n in d["nodes"]}
    inc, out = defaultdict(list), defaultdict(list)
    for e in d["edges"]:
        out[e["from"]].append(e)
        inc[e["to"]].append(e)
    diamond = ("decision", "merge")
    for e in d["edges"]:
        if kind.get(e["from"]) in diamond and kind.get(e["to"]) in diamond:
            err(d, f"hai hình thoi nối liền {e['from']} → {e['to']}: giữa chúng phải có một hành động")
    for k, kd in kind.items():
        if kd == "decision":
            if len(inc[k]) != 1:
                err(d, f"nút quyết định {k} có {len(inc[k])} luồng vào (phải đúng 1)")
            if len(out[k]) < 2 or any(not e.get("guard") for e in out[k]):
                err(d, f"nút quyết định {k} phải có >= 2 nhánh ra, mỗi nhánh có điều kiện")
            prev = inc[k][0]["from"] if inc[k] else None
            if prev and not (kind[prev] == "action" and name[prev].startswith("Kiểm tra")):
                err(d, f"trước nút quyết định {k} phải là hành động 'Kiểm tra ...' (đang là {prev})")
            if k + "_q" not in kind:
                err(d, f"nút quyết định {k} thiếu câu hỏi")
        elif kd == "merge":
            if len(inc[k]) < 2 or len(out[k]) != 1:
                err(d, f"nút gộp {k}: {len(inc[k])} vào, {len(out[k])} ra (phải >= 2 vào, 1 ra)")
        elif kd == "action":
            if len(inc[k]) != 1 or len(out[k]) != 1:
                err(d, f"hành động '{name[k]}': {len(inc[k])} vào, {len(out[k])} ra (phải 1 vào, 1 ra)")
            if BAD_ACTION.search(name[k]) or len(name[k].split()) > 6:
                err(d, f"hành động '{name[k]}' không phải một hành động ngắn gọn")
        elif kd == "initial" and (inc[k] or len(out[k]) != 1):
            err(d, f"nút bắt đầu {k} sai số luồng")
        elif kd == "final" and (not inc[k] or out[k]):
            err(d, f"nút kết thúc {k} sai số luồng")
    flowing = [k for k, kd in kind.items() if kd in ("action", "decision", "merge", "initial", "final")]
    start = [k for k in flowing if kind[k] == "initial"]
    seen, stack = set(start), list(start)
    while stack:
        for e in out[stack.pop()]:
            if e["to"] not in seen:
                seen.add(e["to"])
                stack.append(e["to"])
    for k in flowing:
        if k not in seen:
            err(d, f"nút {k} ({name[k]}) không đi tới được từ nút bắt đầu")
    finals = [k for k in flowing if kind[k] == "final"]
    back, stack = set(finals), list(finals)
    while stack:
        for e in inc[stack.pop()]:
            if e["from"] not in back:
                back.add(e["from"])
                stack.append(e["from"])
    for k in flowing:
        if k not in back:
            err(d, f"nút {k} ({name[k]}) không tới được nút kết thúc")
    b = boxes(d, ("action", "decision", "merge", "initial", "final"))
    for e in d["edges"]:
        pts = e.get("points") or []
        for p, q in zip(pts, pts[1:]):
            for k, box in b.items():
                if k not in (e["from"], e["to"]) and seg_hits_box(p, q, box):
                    err(d, f"luồng {e['from']}->{e['to']} cắt qua {k} ({name[k]})")
    check_overlaps(d, ("action", "decision", "merge", "initial", "final", "text"))


# ------------------------------------------------------------------ tuần tự
def check_sequence(d):
    msgs = [e for e in d["edges"] if e["kind"] == "message"]
    acts = []          # (lifeline, y bắt đầu, y kết thúc)
    for i, m in enumerate(msgs):
        a, c, y = m["from"], m["to"], m["y"]
        if i > 0:
            active = any(l == a and y0 <= y <= y1 + 1 for l, y0, y1 in acts)
            if not active:
                err(d, f"thông điệp {i + 1} '{m['name']}' không đi ra từ thanh kích hoạt của {a}")
        if m.get("sort", "synchCall") != "reply" and a != c:
            h = m.get("activationHeight")
            if not h:
                err(d, f"lời gọi {i + 1} '{m['name']}' không có thông điệp trả về")
                h = 20
            acts.append((c, y, y + h))
        elif a == c:
            acts.append((c, y, y + 30))
    for l, y0, y1 in acts:
        for l2, z0, z1 in acts:
            if l != l2 and z0 < y0 < z1 and y1 > z1 + 1:
                err(d, f"thanh kích hoạt của {l} ({y0}-{y1}) kết thúc sau thanh của bên gọi {l2} ({z0}-{z1})")


# ------------------------------------------------------------------ use case
INTERNAL = ("Ghi nhận", "Hỏi lại", "Kiểm tra", "Tính ", "Lưu ", "Cảnh báo", "Gửi thư", "Làm rõ", "Áp dụng")


def check_usecase(d):
    ucs = {n["key"]: n["name"] for n in d["nodes"] if n["kind"] == "usecase"}
    actors = {n["key"] for n in d["nodes"] if n["kind"] == "actor"}
    linked = set()
    for e in d["edges"]:
        if e["kind"] == "assoc":
            linked |= {e["from"], e["to"]}
        if e["kind"] in ("include", "extend"):
            linked |= {e["from"], e["to"]}
            if e["from"] in actors or e["to"] in actors:
                err(d, "include/extend không được nối với tác nhân")
    for k, n in ucs.items():
        bare = re.sub(r"^UC\d+b?\s+", "", n)
        if bare.startswith(INTERNAL):
            err(d, f"use case '{n}' là bước nội bộ của hệ thống, không phải mục tiêu của tác nhân")
        if k not in linked:
            err(d, f"use case '{n}' không nối tới tác nhân hay use case nào")
    check_overlaps(d, ("usecase", "actor"))


# ------------------------------------------------------------------ ERD / lớp so với CSDL
def table_of(ent):
    m = {"YEU_CAU_BAO_TRI": "YeuCauBaoTri", "LICH_SU_YEU_CAU": "LichSuYeuCau"}
    return m.get(ent) or "".join(w.capitalize() for w in ent.lower().split("_"))


# Bảng của mối kết hợp n – n trên ERD: (thực thể hai đầu, thuộc tính ghi trên khung của mối kết hợp).
NN = {"CuTru": (("NguoiDung", "CanHo"), sd.CU_TRU_ATTR), "NguoiDung_VaiTro": (("NguoiDung", "VaiTro"), []),
      "VaiTro_Quyen": (("VaiTro", "Quyen"), []), "DangKyDichVu": (("CanHo", "LoaiDichVu"), sd.DANG_KY_ATTR),
      "ChiTietHoaDon": (("HoaDon", "LoaiDichVu"), sd.CHI_TIET_ATTR)}


def fks():
    out = []
    for t, v in sd.SCHEMA.items():
        for c in v["columns"]:
            if c["fk"] and t not in NN:
                out.append((c["fk"], t, c["name"], c["null"], c["on_delete"]))
    return out


def check_erds(erds):
    rels = []      # (A, cardA, B, cardB, diagram)
    for d in erds:
        by = defaultdict(list)
        for e in d["edges"]:
            if e["kind"] == "assoc":
                by[e["to"]].append((table_of(e["from"]), e.get("mult1", "")))
        for key, ends in by.items():
            if len(ends) != 2 or any(not re.fullmatch(r"\([01],[1n]\)", c) for _, c in ends):
                err(d, f"mối kết hợp {key} phải có đúng 2 nhánh với bản số (min,max): {ends}")
                continue
            (a, ca), (b, cb) = ends
            rels.append((a, ca, b, cb, d))
        check_overlaps(d, ("class", "nary", "note"))
    used = set()
    for parent, child, col, nullable, _ in fks():
        ok = False
        for i, (a, ca, b, cb, d) in enumerate(rels):
            if {a, b} != {parent, child} or i in used:
                continue
            card_child, card_parent = (ca, cb) if a == child else (cb, ca)
            if card_child.endswith("1)") and card_parent.endswith("n)") and (card_child == "(0,1)") == nullable:
                used.add(i)
                ok = True
                break
        if not ok:
            ERRORS.append(f"[ERD] khoá ngoại {child}.{col} -> {parent} (NULL={nullable}) không có mối kết hợp với bản số khớp")
    nn = {frozenset(ends) for ends, _ in NN.values()}
    for t, (ends, _) in NN.items():
        if not any({r[0], r[2]} == set(ends) and r[1].endswith("n)") and r[3].endswith("n)") for r in rels):
            ERRORS.append(f"[ERD] thiếu mối kết hợp n–n {ends[0]}–{ends[1]} (bảng {t})")
    for a, ca, b, cb, d in rels:
        if ca.endswith("n)") and cb.endswith("n)") and frozenset((a, b)) not in nn:
            err(d, f"mối kết hợp n–n {a}–{b} không có bảng trung gian trong CSDL")


def check_attributes(erds, classes):
    """Thuộc tính trên ERD phải là cột thật (không gồm khoá ngoại), thuộc tính đầu là khoá chính; ERD tổng chỉ ghi
    các thuộc tính chính nên không đòi đủ cột. Thuộc tính của lớp là tên cột viết kiểu camelCase."""
    shown = defaultdict(set)
    for d in erds:
        for n in d["nodes"]:
            if n["kind"] == "class":
                t = table_of(n["key"])
                cols = {c["name"]: c for c in sd.SCHEMA[t]["columns"]}
                for a in n["attrs"]:
                    name = a["name"].lstrip("/")          # "/TongTien": thuộc tính suy diễn (cột CSDL tự tính)
                    shown[t].add(name)
                    if name not in cols:
                        err(d, f"thuộc tính {n['key']}.{a['name']} không có trong bảng {t}")
                    elif cols[name]["fk"]:
                        err(d, f"ERD không vẽ khoá ngoại: {n['key']}.{a['name']}")
                    elif cols[name].get("generated") != a["name"].startswith("/"):
                        err(d, f"{n['key']}.{a['name']}: dấu / phải đi đúng với cột tự tính")
    for t, v in sd.SCHEMA.items():
        if t in NN:
            continue
        pk = [c["name"] for c in v["columns"] if c["pk"]]
        if not shown[t]:
            ERRORS.append(f"[ERD] bảng {t} không có thực thể trên ERD")
        elif any(k not in shown[t] for k in pk):
            ERRORS.append(f"[ERD] thực thể của {t} thiếu khoá chính {pk}")
    for t, (_, attrs) in NN.items():
        cols = {c["name"]: c for c in sd.SCHEMA[t]["columns"]}
        for a in attrs:
            c = cols.get(a.lstrip("/"))
            if c is None or c["fk"]:
                ERRORS.append(f"[ERD] thuộc tính {a} của mối kết hợp ứng với bảng {t} không phải cột thường")
            elif c.get("generated") != a.startswith("/"):
                ERRORS.append(f"[ERD] {t}.{a}: dấu / phải đi đúng với cột tự tính")
    for d in classes:
        for n in d["nodes"]:
            if n["kind"] != "class" or n["key"] not in sd.SCHEMA:
                continue
            cols = {c["name"][0].lower() + c["name"][1:] for c in sd.SCHEMA[n["key"]]["columns"]}
            for a in n.get("attrs", []):
                if a["name"] not in cols:
                    err(d, f"thuộc tính lớp {n['key']}.{a['name']} không ứng với cột nào")


def check_classes(classes):
    for d in classes:
        check_overlaps(d, ("class", "enum"))
        for e in d["edges"]:
            a, b = e["from"], e["to"]
            cols = [c for c in sd.SCHEMA.get(b, {}).get("columns", []) if c["fk"] == a]
            if not cols:
                continue
            nullable = any(c["null"] for c in cols)
            if len(cols) == 1 and e.get("mult1") != ("0..1" if nullable else "1"):
                err(d, f"liên kết {a}–{b}: bội số phía {a} là {e.get('mult1')} nhưng khoá ngoại NULL={nullable}")
            cascade = all(c["on_delete"] == "CASCADE" for c in cols)
            if e.get("agg1") == "composite" and not cascade:
                err(d, f"{a}◆{b} là hợp thành nhưng CSDL không xoá theo (ON DELETE {cols[0]['on_delete']})")
            if cascade and e.get("agg1") != "composite" and b not in ("NguoiDung_VaiTro", "VaiTro_Quyen"):
                err(d, f"{a}–{b}: CSDL xoá theo (CASCADE) nhưng sơ đồ lớp không vẽ hợp thành")


def main():
    ds = sd.diagrams()
    erds, classes = [], []
    for d in ds:
        if d["kind"] == "activity":
            check_activity(d)
        elif d["kind"] == "sequence":
            check_sequence(d)
        elif d["kind"] == "usecase":
            check_usecase(d)
        elif d["kind"] == "class" and "ERD" in d["name"]:
            erds.append(d)
        elif d["kind"] == "class":
            classes.append(d)
        elif d["kind"] == "erd":
            check_overlaps(d, ("entity",))
            check_rd(d)
    check_erds(erds)
    check_classes(classes)
    check_attributes(erds, classes)
    sys.stdout.reconfigure(encoding="utf-8")
    for e in ERRORS:
        print(e)
    print(f"{len(ERRORS)} lỗi trên {len(ds)} sơ đồ")
    return 1 if ERRORS else 0


if __name__ == "__main__":
    sys.exit(main())
