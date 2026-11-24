"""Thử bộ kiểm tra bằng cách cố ý làm sai mô hình: mỗi lỗi cấy vào phải bị kiem_tra_uml phát hiện,
còn mô hình gốc phải sạch. Chạy: python test_kiem_tra_uml.py"""
import contextlib
import io
import json
import tempfile
from pathlib import Path

import kiem_tra_uml as k

ORIGINAL = json.loads(k.MDJ.read_text(encoding="utf8"))
REPORT_SRC = k.REPORT.read_text(encoding="utf8")


def walk(o):
    if isinstance(o, dict):
        yield o
        for v in o.values():
            yield from walk(v)
    elif isinstance(o, list):
        for v in o:
            yield from walk(v)


def find(doc, typ, name=None, **kw):
    for o in walk(doc):
        if o.get("_type") == typ and (name is None or o.get("name") == name) and all(o.get(a) == b for a, b in kw.items()):
            return o
    raise LookupError(typ, name)


def by_id(doc, i):
    return next(o for o in walk(doc) if o.get("_id") == i)


def ends(doc, assoc_type, a, b):
    for o in walk(doc):
        if o.get("_type") == assoc_type:
            names = {by_id(doc, o["end1"]["reference"]["$ref"]).get("name"),
                     by_id(doc, o["end2"]["reference"]["$ref"]).get("name")}
            if names == {a, b}:
                return o
    raise LookupError(a, b)


def m_attr(d):
    find(find(d, "UMLClass", "YeuCauBaoTri"), "UMLAttribute", "hanXuLy")["name"] = "hanChot"


def m_enum(d):
    lits = find(d, "UMLEnumeration", "TrangThaiSuCo")["literals"]
    lits[:] = [x for x in lits if x["name"] != "TamDung"]


def m_param(d):
    find(find(d, "UMLClass", "YeuCauBaoTri"), "UMLOperation", "giaoViec")["name"] = "giaoViecNhanh"


def m_mult(d):
    ends(d, "UMLAssociation", "MucSla", "YeuCauBaoTri")["end1"]["multiplicity"] = "0..1"


def m_comp(d):
    ends(d, "UMLAssociation", "CanHo", "YeuCauBaoTri")["end1"]["aggregation"] = "composite"


def m_drop_assoc(d):
    a = ends(d, "UMLAssociation", "NguoiDung", "LichSuTrangThaiSuCo")
    for o in walk(d):
        if o.get("_type") == "UMLAssociationView" and o.get("model", {}).get("$ref") == a["_id"]:
            o["model"] = {"$ref": "khong-co"}


def m_state(d):
    tr = next(o for o in walk(d) if o.get("_type") == "UMLTransition" and o.get("name", "").startswith("KTV/QL: bắt đầu"))
    tr["target"] = {"$ref": find(d, "UMLState", "Đã đóng")["_id"]}


def m_guard(d):
    flow = next(o for o in walk(d) if o.get("_type") == "UMLControlFlow" and o.get("guard") == "có thợ phù hợp")
    flow["guard"] = ""


def m_message(d):
    find(d, "UMLMessage", "TriageAsync(description)")["name"] = "ClassifyAsync(description)"


def m_lifeline(d):
    for o in walk(d):
        if o.get("_type") == "UMLLifeline" and o.get("name") == ":AppDbContext":
            o["name"] = ":DbContextX"


def m_column(d):
    ent = find(d, "ERDEntity", "YeuCauBaoTri")
    find(ent, "ERDColumn", "HanXuLy")["name"] = "HanChot"


def m_null(d):
    ent = find(d, "ERDEntity", "CanHo")
    find(ent, "ERDColumn", "TangId")["nullable"] = True


MDJ_CASES = {"thuộc tính sai tên": m_attr, "thiếu literal enum": m_enum, "thao tác chưa ánh xạ": m_param,
             "bội số sai": m_mult, "hợp thành sai": m_comp, "thiếu quan hệ khoá ngoại": m_drop_assoc,
             "chuyển trạng thái sai": m_state, "nhánh quyết định thiếu điều kiện": m_guard,
             "thông điệp gọi hàm không có": m_message, "đường đời không có lớp": m_lifeline,
             "cột RD sai tên": m_column, "cờ NULL RD sai": m_null}
REPORT_CASES = {
    "từ điển: sai hành vi xoá": ('"CanHoId", "integer", "NOT NULL, FK CASCADE"', '"CanHoId", "integer", "NOT NULL, FK RESTRICT"'),
    "trigger: thừa chuyển (4,2)": ("(3,4),(3,2)) THEN", "(3,4),(3,2),(4,2)) THEN"),
}


def run(doc=ORIGINAL, report=REPORT_SRC):
    with tempfile.TemporaryDirectory() as tmp:
        mdj, rep = Path(tmp) / "m.mdj", Path(tmp) / "r.py"
        mdj.write_text(json.dumps(doc), encoding="utf8")
        rep.write_text(report, encoding="utf8")
        k.MDJ, k.REPORT, k.OUT = mdj, rep, Path(tmp) / "out.txt"
        k.errors.clear()
        k.notes.clear()
        with contextlib.redirect_stdout(io.StringIO()):
            k.main()
        return list(k.errors)


if __name__ == "__main__":
    import sys
    sys.stdout.reconfigure(encoding="utf-8")
    base = run()
    assert not base, f"mô hình gốc có lỗi: {base}"
    failed = 0
    for label, mutate in MDJ_CASES.items():
        doc = json.loads(json.dumps(ORIGINAL))
        mutate(doc)
        found = run(doc)
        failed += not found
        print(f"{'OK ' if found else 'SÓT'} {label}: {found[0] if found else 'không phát hiện'}")
    for label, (a, b) in REPORT_CASES.items():
        assert REPORT_SRC.count(a) == 1, a
        found = run(report=REPORT_SRC.replace(a, b))
        failed += not found
        print(f"{'OK ' if found else 'SÓT'} {label}: {found[0] if found else 'không phát hiện'}")
    print(f"{len(MDJ_CASES) + len(REPORT_CASES) - failed}/{len(MDJ_CASES) + len(REPORT_CASES)} lỗi cấy bị phát hiện")
    sys.exit(1 if failed else 0)
