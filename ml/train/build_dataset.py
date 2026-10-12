"""Chuyen cac file raw/*.txt thanh train.jsonl va val.jsonl.

Moi dong raw:  mo ta | muc (L/M/H/U) | D hoac - | tom tat | cau hoi lai (tuy chon)
Loai su co lay tu ten file, hoac tu dong "## <loai>" trong file gop.

    python build_dataset.py
"""
import collections
import json
import random
import unicodedata
from pathlib import Path

HERE = Path(__file__).parent
CATEGORIES = ["plumbing", "electrical", "appliance", "structural",
              "cleaning", "pest", "network", "other"]
PRIORITY = {"L": "low", "M": "medium", "H": "high", "U": "urgent"}
# Nhom tiep nhan su co. Loai co tho thi luon la technician; loai "other" ghi ro bang tieu de "## other:<nhom>".
HANDLERS = ["technician", "cleaning_team", "pest_control", "network_provider",
            "security", "building_engineering", "accounting", "management"]
DEFAULT_HANDLER = {"plumbing": "technician", "electrical": "technician", "appliance": "technician",
                   "structural": "technician", "cleaning": "cleaning_team", "pest": "pest_control",
                   "network": "network_provider", "other": "management"}
VAL_RATIO = 0.1
SEED = 42  # doi seed se doi cach chia train/val


def norm(text):
    return " ".join(text.lower().split())


def has_diacritics(text):
    return any(unicodedata.combining(c) for c in unicodedata.normalize("NFD", text)) or "đ" in text.lower()


def strip_diacritics(text):
    text = unicodedata.normalize("NFD", text.replace("đ", "d").replace("Đ", "D"))
    return unicodedata.normalize("NFC", "".join(c for c in text if not unicodedata.combining(c)))


def load():
    rows, errors = [], []
    for path in sorted((HERE / "raw").glob("*.txt")):
        category = path.stem if path.stem in CATEGORIES else None
        handler = ""
        for n, line in enumerate(path.read_text(encoding="utf-8").splitlines(), 1):
            line = line.strip()
            if line.startswith("## "):
                category, _, handler = line[3:].strip().partition(":")
                continue
            if not line or line.startswith("#"):
                continue
            parts = [p.strip() for p in line.split(" | ")]
            where = f"{path.name}:{n}"
            if len(parts) not in (4, 5) or category not in CATEGORIES or (handler and handler not in HANDLERS):
                errors.append(f"{where}: sai so cot hoac thieu loai")
                continue
            text, pri, danger, summary = parts[:4]
            question = parts[4] if len(parts) == 5 else ""
            if pri not in PRIORITY or danger not in ("D", "-") or not text or not summary:
                errors.append(f"{where}: sai muc uu tien/co nguy hiem hoac thieu noi dung")
                continue
            if len(summary.split()) > 20:
                errors.append(f"{where}: tom tat qua 20 tu")
            rows.append({
                "text": text, "category": category, "handler": handler or DEFAULT_HANDLER[category],
                "priority": PRIORITY[pri],
                "is_dangerous": danger == "D", "need_more_info": bool(question),
                "follow_up_question": question, "summary": summary,
            })
    return rows, errors


def main():
    rows, errors = load()

    # Bo test khong duoc lot vao du lieu train.
    test_path = HERE.parent / "test_cases.jsonl"
    test_texts = {norm(json.loads(l)["text"]) for l in test_path.read_text(encoding="utf-8").splitlines() if l.strip()}
    seen = set()
    for r in rows:
        key = norm(r["text"])
        if key in test_texts:
            errors.append(f"trung voi bo test: {r['text']}")
        if key in seen:
            errors.append(f"trung lap: {r['text']}")
        seen.add(key)
    if errors:
        raise SystemExit("\n".join(errors))

    # Chia theo tung loai de val co du 8 loai.
    rng = random.Random(SEED)
    train, val = [], []
    for category in CATEGORIES:
        group = [r for r in rows if r["category"] == category]
        rng.shuffle(group)
        cut = max(1, round(len(group) * VAL_RATIO))
        val += group[:cut]
        train += group[cut:]
    # Cu dan hay go khong dau va mo hinh sai nhieu nhat o cac mo ta do: them ban khong dau cho
    # mot nua so mau train co dau. Val giu nguyen de so sanh duoc voi cac vong truoc.
    taken = test_texts | {norm(r["text"]) for r in rows}
    for r in list(train):
        plain = strip_diacritics(r["text"]).lower()
        if has_diacritics(r["text"]) and rng.random() < 0.5 and norm(plain) not in taken:
            taken.add(norm(plain))
            train.append({**r, "text": plain})
    rng.shuffle(train)
    rng.shuffle(val)
    for name, data in (("train.jsonl", train), ("val.jsonl", val)):
        with open(HERE / name, "w", encoding="utf-8", newline="\n") as f:
            for r in data:
                f.write(json.dumps(r, ensure_ascii=False) + "\n")

    total = len(rows)
    print(f"Tong: {total}  (train {len(train)}, val {len(val)})")
    for field in ("category", "handler", "priority"):
        counts = collections.Counter(r[field] for r in rows)
        print(f"{field}: " + ", ".join(f"{k} {v}" for k, v in counts.most_common()))
    for label, pred in (("nguy hiem", lambda r: r["is_dangerous"]),
                        ("mo ho", lambda r: r["need_more_info"]),
                        ("khong dau", lambda r: not has_diacritics(r["text"]))):
        k = sum(1 for r in rows if pred(r))
        print(f"{label}: {k} ({100 * k / total:.0f}%)")


if __name__ == "__main__":
    main()
