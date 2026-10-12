"""Chay thu cac mo hinh Ollama tren bo tinh huong su co va cham diem.

Chi dung thu vien chuan cua Python, khong can pip install.

Vi du:
    python run_test.py --models qwen3:4b granite4.2:3b --limit 3
    python run_test.py --models qwen3:4b granite4.2:3b phi4-mini llama3.2:3b gemma3:4b
"""
import argparse
import csv
import json
import sys
import time
import urllib.error
import urllib.request
from pathlib import Path

CATEGORIES = ["plumbing", "electrical", "appliance", "structural",
              "cleaning", "pest", "network", "other"]
PRIORITIES = ["low", "medium", "high", "urgent"]

SCHEMA = {
    "type": "object",
    "properties": {
        "category": {"type": "string", "enum": CATEGORIES},
        "priority": {"type": "string", "enum": PRIORITIES},
        "is_dangerous": {"type": "boolean"},
        "need_more_info": {"type": "boolean"},
        "follow_up_question": {"type": "string"},
        "summary": {"type": "string"},
    },
    "required": ["category", "priority", "is_dangerous",
                 "need_more_info", "follow_up_question", "summary"],
}

SYSTEM_PROMPT = """Bạn là trợ lý tiếp nhận sự cố của ban quản lý chung cư.
Cư dân gửi một mô tả sự cố. Căn hộ và người báo đã được hệ thống biết sẵn, không cần hỏi lại.
Hãy đọc mô tả và trả về đúng một đối tượng JSON, không viết gì thêm.

category - chọn một:
- plumbing: cấp thoát nước, rò rỉ, tắc nghẽn, thấm nước
- electrical: điện, ổ cắm, đèn, tủ điện, cầu dao
- appliance: thiết bị gia dụng (máy lạnh, máy giặt, bếp, tủ lạnh, máy nước nóng)
- structural: tường, trần, sàn, cửa, cửa sổ, khóa
- cleaning: vệ sinh, rác, mùi hôi khu vực chung
- pest: côn trùng, chuột
- network: internet, wifi, truyền hình
- other: không thuộc loại nào ở trên hoặc không đủ thông tin để xác định

priority - chọn một:
- urgent: đe dọa an toàn của người hoặc tài sản ngay lúc này (cháy, khói, rò gas, chập điện, điện giật, ngập nước lan rộng, người mắc kẹt, vật sắp rơi)
- high: mất một chức năng thiết yếu của căn hộ (mất điện, mất nước, mất internet khi cần gấp, toilet duy nhất bị tắc, không khóa được cửa, nước đang rò liên tục)
- medium: hỏng hóc gây bất tiện nhưng vẫn sinh hoạt được
- low: vấn đề nhỏ, thẩm mỹ, không gấp

is_dangerous: true nếu có nguy cơ cháy nổ, điện giật, ngạt khí, hoặc thương tích cho người.
need_more_info: true chỉ khi mô tả quá mơ hồ, không xác định được hỏng cái gì hoặc hỏng ở đâu.
follow_up_question: nếu need_more_info là true, viết MỘT câu hỏi ngắn, lịch sự bằng tiếng Việt để hỏi lại cư dân; nếu không thì để chuỗi rỗng.
summary: tóm tắt sự cố trong một câu tiếng Việt có dấu, tối đa 20 từ."""


def post_json(url, payload, timeout):
    data = json.dumps(payload).encode("utf-8")
    req = urllib.request.Request(url, data=data,
                                 headers={"Content-Type": "application/json"})
    with urllib.request.urlopen(req, timeout=timeout) as resp:
        return json.loads(resp.read().decode("utf-8"))


def get_json(url, timeout=30):
    with urllib.request.urlopen(url, timeout=timeout) as resp:
        return json.loads(resp.read().decode("utf-8"))


class Runner:
    def __init__(self, host, num_ctx, timeout):
        self.host = host.rstrip("/")
        self.num_ctx = num_ctx
        self.timeout = timeout
        self.send_think = {}  # model -> co gui "think": false hay khong

    def chat(self, model, text):
        payload = {
            "model": model,
            "messages": [
                {"role": "system", "content": SYSTEM_PROMPT},
                {"role": "user", "content": text},
            ],
            "stream": False,
            "format": SCHEMA,
            "options": {"temperature": 0, "num_ctx": self.num_ctx},
        }
        # Mo hinh co che do "thinking" (vi du qwen3) se cham hon nhieu neu khong tat.
        if self.send_think.get(model, True):
            payload["think"] = False
        try:
            return post_json(self.host + "/api/chat", payload, self.timeout)
        except urllib.error.HTTPError as err:
            body = err.read().decode("utf-8", "replace")
            if err.code == 400 and "think" in body.lower() and "think" in payload:
                # Mo hinh khong ho tro tham so think: bo di va thu lai.
                self.send_think[model] = False
                del payload["think"]
                return post_json(self.host + "/api/chat", payload, self.timeout)
            raise RuntimeError(f"HTTP {err.code}: {body[:300]}") from err

    def gpu_percent(self, model):
        """Ty le mo hinh nam tren GPU, theo /api/ps (giong lenh `ollama ps`)."""
        try:
            for m in get_json(self.host + "/api/ps").get("models", []):
                # /api/ps ghi "phi4-mini:latest" khi tag khong co phan sau dau ":"
                if {m.get("name"), m.get("model")} & {model, model + ":latest"}:
                    size = m.get("size") or 0
                    if size:
                        return round(100 * (m.get("size_vram") or 0) / size)
        except Exception:
            pass
        return None

    def unload(self, model):
        try:
            post_json(self.host + "/api/generate",
                      {"model": model, "keep_alive": 0}, 60)
        except Exception:
            pass


def score_case(case, out):
    """So ket qua cua mo hinh voi nhan. None = khong cham."""
    s = {"json_ok": out is not None}
    if out is None:
        return s
    s["category_ok"] = out.get("category") in case["category"]
    pred, gold = out.get("priority"), case["priority"]
    s["priority_ok"] = pred == gold
    s["priority_near"] = (pred in PRIORITIES and
                          abs(PRIORITIES.index(pred) - PRIORITIES.index(gold)) <= 1)
    if case["dangerous"] is not None:
        s["danger_ok"] = out.get("is_dangerous") == case["dangerous"]
    s["vague_ok"] = out.get("need_more_info") == case["vague"]
    return s


def pct(hits, total):
    return f"{100 * hits / total:.0f}%" if total else "-"


def main():
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")

    ap = argparse.ArgumentParser()
    ap.add_argument("--models", nargs="+", required=True)
    ap.add_argument("--cases", default="test_cases.jsonl")
    ap.add_argument("--out", default="results")
    ap.add_argument("--host", default="http://localhost:11434")
    ap.add_argument("--num-ctx", type=int, default=2048)
    ap.add_argument("--timeout", type=int, default=300)
    ap.add_argument("--limit", type=int, default=0, help="chi chay N tinh huong dau")
    args = ap.parse_args()

    with open(args.cases, encoding="utf-8") as f:
        cases = [json.loads(line) for line in f if line.strip()]
    if args.limit:
        cases = cases[:args.limit]

    out_dir = Path(args.out)
    out_dir.mkdir(exist_ok=True)
    runner = Runner(args.host, args.num_ctx, args.timeout)

    try:
        get_json(runner.host + "/api/version")
    except Exception as err:
        sys.exit(f"Khong ket noi duoc Ollama tai {runner.host}: {err}\n"
                 "Hay mo ung dung Ollama roi chay lai.")

    summary_rows, review_rows = [], []

    for model in args.models:
        print(f"\n=== {model} ===")
        try:
            t0 = time.time()
            runner.chat(model, "Xin chào")  # nap mo hinh vao bo nho truoc khi do
            print(f"Nap mo hinh: {time.time() - t0:.1f}s")
        except Exception as err:
            print(f"BO QUA {model}: {err}")
            continue
        gpu = runner.gpu_percent(model)
        print(f"Ty le tren GPU: {gpu if gpu is not None else '?'}%")

        scores, latencies, tok_s = [], [], []
        detail_path = out_dir / f"detail_{model.replace(':', '_').replace('/', '_')}.jsonl"
        with open(detail_path, "w", encoding="utf-8") as detail:
            for case in cases:
                t0 = time.time()
                raw, out, error = "", None, ""
                try:
                    resp = runner.chat(model, case["text"])
                    raw = resp.get("message", {}).get("content", "")
                    out = json.loads(raw)
                    if not isinstance(out, dict):
                        out = None
                    if resp.get("eval_duration"):
                        tok_s.append(resp["eval_count"] / resp["eval_duration"] * 1e9)
                except json.JSONDecodeError:
                    error = "JSON khong hop le"
                except Exception as err:
                    error = str(err)
                elapsed = time.time() - t0
                latencies.append(elapsed)
                s = score_case(case, out)
                scores.append(s)
                detail.write(json.dumps({"id": case["id"], "text": case["text"],
                                         "expected": case, "output": out, "raw": raw,
                                         "error": error, "seconds": round(elapsed, 2),
                                         "score": s}, ensure_ascii=False) + "\n")
                review_rows.append({
                    "model": model, "id": case["id"], "mo_ta": case["text"],
                    "category": (out or {}).get("category", ""),
                    "priority": (out or {}).get("priority", ""),
                    "priority_dung": case["priority"],
                    "cau_hoi_lai": (out or {}).get("follow_up_question", ""),
                    "tom_tat": (out or {}).get("summary", ""),
                    "diem_tieng_viet_1_5": "",
                })
                mark = "ok " if s.get("category_ok") and s.get("priority_ok") else "-- "
                print(f"  {mark}#{case['id']:>2} {elapsed:5.1f}s "
                      f"{(out or {}).get('category', error)} / {(out or {}).get('priority', '')}")

        n = len(scores)

        def count(key, value=True):
            return sum(1 for s in scores if s.get(key) is value)

        dangerous_ids = [i for i, c in enumerate(cases) if c["dangerous"] is True]
        safe_ids = [i for i, c in enumerate(cases) if c["dangerous"] is False]
        vague_ids = [i for i, c in enumerate(cases) if c["vague"]]
        clear_ids = [i for i, c in enumerate(cases) if not c["vague"]]
        danger_caught = sum(1 for i in dangerous_ids if scores[i].get("danger_ok"))
        false_alarms = sum(1 for i in safe_ids if scores[i].get("danger_ok") is False)
        vague_caught = sum(1 for i in vague_ids if scores[i].get("vague_ok"))
        over_asked = sum(1 for i in clear_ids if scores[i].get("vague_ok") is False)
        lat_sorted = sorted(latencies)

        summary_rows.append({
            "model": model,
            "gpu_%": gpu if gpu is not None else "",
            "json_hop_le": pct(count("json_ok"), n),
            "dung_loai": pct(count("category_ok"), n),
            "dung_uu_tien": pct(count("priority_ok"), n),
            "uu_tien_lech_toi_da_1_bac": pct(count("priority_near"), n),
            "bat_nguy_hiem": f"{danger_caught}/{len(dangerous_ids)}",
            "bao_dong_gia": f"{false_alarms}/{len(safe_ids)}",
            "bat_mo_ta_mo_ho": f"{vague_caught}/{len(vague_ids)}",
            "hoi_thua": f"{over_asked}/{len(clear_ids)}",
            "giay_trung_binh": f"{sum(latencies) / n:.1f}",
            "giay_cham_nhat": f"{lat_sorted[-1]:.1f}",
            "token_moi_giay": f"{sum(tok_s) / len(tok_s):.1f}" if tok_s else "",
        })
        runner.unload(model)  # giai phong VRAM truoc khi nap mo hinh ke tiep

    if not summary_rows:
        sys.exit("Khong co mo hinh nao chay duoc.")

    # utf-8-sig de Excel hien dung tieng Viet
    with open(out_dir / "summary.csv", "w", newline="", encoding="utf-8-sig") as f:
        w = csv.DictWriter(f, fieldnames=list(summary_rows[0]))
        w.writeheader()
        w.writerows(summary_rows)
    with open(out_dir / "review.csv", "w", newline="", encoding="utf-8-sig") as f:
        w = csv.DictWriter(f, fieldnames=list(review_rows[0]))
        w.writeheader()
        w.writerows(review_rows)

    print("\n=== TONG KET ===")
    for row in summary_rows:
        print(" | ".join(f"{k}={v}" for k, v in row.items()))
    print(f"\nDa ghi: {out_dir / 'summary.csv'} va {out_dir / 'review.csv'}")


if __name__ == "__main__":
    main()
