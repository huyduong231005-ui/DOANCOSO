"""Cấy lỗi vào sơ đồ và kiểm tra bộ kiểm tra bắt được (bộ kiểm tra mà không bắt lỗi thì vô dụng)."""
import copy

import kiem_tra_so_do as kt
import so_do_v2 as sd

ALL = sd.diagrams()


def run(mutate):
    ds = copy.deepcopy(ALL)
    mutate({d["name"]: d for d in ds})
    kt.ERRORS.clear()
    for d in ds:
        if d["kind"] == "activity":
            kt.check_activity(d)
        elif d["kind"] == "sequence":
            kt.check_sequence(d)
        elif d["kind"] == "usecase":
            kt.check_usecase(d)
    kt.check_erds([d for d in ds if d["kind"] == "class" and "ERD" in d["name"]])
    kt.check_classes([d for d in ds if d["kind"] == "class" and "ERD" not in d["name"]])
    kt.check_attributes([d for d in ds if d["kind"] == "class" and "ERD" in d["name"]],
                        [d for d in ds if d["kind"] == "class" and "ERD" not in d["name"]])
    return list(kt.ERRORS)


def find(prefix, ds):
    return next(d for n, d in ds.items() if n.startswith(prefix))


def drop_check_before_decision(ds):
    d = find("Hinh 1.1", ds)
    for e in d["edges"]:
        if e["to"] == "d1":
            e["from"] = "c1"          # nối thẳng từ "Đánh giá mức độ sự cố" vào nút quyết định


def diamond_to_diamond(ds):
    d = find("Hinh 3.7", ds)
    for e in d["edges"]:
        if e["from"] == "d2" and e["to"] == "w9":
            e["to"], e["points"] = "m1", []   # nhánh AI tắt đi thẳng vào nút gộp
    d["edges"] = [e for e in d["edges"] if e["from"] != "w9"]
    d["nodes"] = [n for n in d["nodes"] if n["key"] != "w9"]


def message_without_activation(ds):
    d = find("Hinh 3.13", ds)
    d["edges"][1].pop("activationHeight", None)
    d["edges"][0].pop("activationHeight", None)


def composite_on_restrict(ds):
    d = find("Hinh 3.8", ds)
    next(e for e in d["edges"] if e["from"] == "CanHo" and e["to"] == "CuTru")["agg1"] = "composite"


def wrong_cardinality(ds):
    d = find("Hinh 2.3", ds)
    next(e for e in d["edges"] if e["from"] == "MUC_UU_TIEN")["mult1"] = "(1,1)"


def internal_use_case(ds):
    d = find("Hinh 3.2", ds)
    next(n for n in d["nodes"] if n["key"] == "anh")["name"] = "Hỏi lại AI"


def stale_attribute(ds):
    d = find("Hinh 2.1", ds)
    next(n for n in d["nodes"] if n["key"] == "TOA_NHA")["attrs"].append({"name": "SoLuongTang"})


def test_clean():
    assert run(lambda ds: None) == []


def test_each_injected_bug_is_caught():
    for mutate, needle in [(drop_check_before_decision, "Kiểm tra"), (message_without_activation, "thanh kích hoạt"),
                           (composite_on_restrict, "hợp thành"), (wrong_cardinality, "MucUuTien"),
                           (internal_use_case, "bước nội bộ"), (stale_attribute, "SoLuongTang"),
                           (diamond_to_diamond, "hai hình thoi")]:
        errs = run(mutate)
        assert any(needle in e for e in errs), (mutate.__name__, errs)


if __name__ == "__main__":
    test_clean()
    test_each_injected_bug_is_caught()
    print("ok: sơ đồ sạch, 7/7 lỗi cấy đều bị bắt")
