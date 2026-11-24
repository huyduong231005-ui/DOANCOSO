"""Gọi StarUML CLI: dựng sơ đồ từ spec JSON (extension baocao-uml-builder) và xuất ảnh.

Hai vấn đề đã gặp và cách xử lý:
- StarUML làm xong việc nhưng khi thoát lại ghi trạng thái cửa sổ vào settings.json (ghi kiểu đổi tên tệp tạm).
  Lỗi đổi tên làm hiện hộp thoại "A JavaScript error occurred in the main process" và tiến trình không thoát,
  script phải chờ hết thời gian chờ (240 giây mỗi lệnh). Vì vậy:
  (1) chạy StarUML với thư mục dữ liệu riêng (--user-data-dir) nên không đụng settings.json của người dùng;
  (2) đọc stdout, thấy dòng "Total ... generated/exported" là kết thúc tiến trình ngay, không đợi nó tự thoát.
"""
import json
import os
import shutil
import subprocess
import sys
import threading
import time
from pathlib import Path

STARUML = r"C:\Program Files\StarUML\StarUML.exe"
HERE = Path(__file__).resolve().parent
EXT_SRC = HERE / "staruml_ext"
# Thư mục dữ liệu riêng cho các lần chạy tự động (settings, extension).
DATA_DIR = HERE.parent / "staruml" / "_starumldata"
EXT_DST = DATA_DIR / "extensions" / "user" / "baocao-uml-builder"
EMPTY = '{"_type":"Project","_id":"AAAAAAFF+h6SjaM2Hec=","name":"Untitled","ownedElements":[]}'
DONE = ("files were generated", "diagrams were exported")


def _kill(pid):
    subprocess.run(["taskkill", "/PID", str(pid), "/T", "/F"], capture_output=True)


def run(args, timeout=240):
    # Lệnh CLI phải đứng đầu (ejs/image/...), nếu không StarUML mở giao diện thường và treo.
    assert str(args[0]) in ("ejs", "image", "html", "pdf"), args
    DATA_DIR.mkdir(parents=True, exist_ok=True)
    p = subprocess.Popen([STARUML, *map(str, args), f"--user-data-dir={DATA_DIR}"],
                         stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    lines, done = [], threading.Event()

    def reader():
        for raw in p.stdout:
            line = raw.decode("utf8", "replace")
            lines.append(line)
            if any(m in line for m in DONE):
                done.set()
        done.set()

    threading.Thread(target=reader, daemon=True).start()
    finished = done.wait(timeout)
    # Đã xong việc: cho 3 giây để tự thoát, sau đó kết thúc hẳn (kể cả khi có hộp thoại lỗi lúc thoát).
    if finished:
        for _ in range(30):
            if p.poll() is not None:
                break
            time.sleep(0.1)
    if p.poll() is None:
        _kill(p.pid)
    p.wait()
    out = "".join(lines)
    ok = any(m in out for m in DONE)
    return (0 if ok else 1), out


def install_extension():
    EXT_DST.mkdir(parents=True, exist_ok=True)
    for f in EXT_SRC.iterdir():
        shutil.copy2(f, EXT_DST / f.name)


def build(spec_path, work_dir):
    """Lệnh `exec` của StarUML 5.1 thoát ngay mà không chạy lệnh, nên gọi qua `ejs`:
    template EJS có biến `app` nên gọi được lệnh baocao:build của extension."""
    install_extension()
    work = Path(work_dir)
    empty = work / "_empty.mdj"
    empty.write_text(EMPTY, encoding="utf8")
    tpl = work / "_build.ejs"
    tpl.write_text("<% app.commands.execute('baocao:build', " + json.dumps(str(spec_path)) + ") %>ok", encoding="utf8")
    return run(["ejs", empty, "-t", tpl, "-s", "@Project", "-o", work / "_build.txt"])


def export(mdj, out_dir, fmt="png", select="@Diagram"):
    Path(out_dir).mkdir(parents=True, exist_ok=True)
    # Tên file lấy từ documentation (tên không dấu), tên hiển thị của sơ đồ là tiếng Việt có dấu.
    pattern = str(Path(out_dir) / ("<%=filenamify(element.documentation || element.name)%>." + fmt))
    # Hệ số phóng của Chromium: ảnh PNG đủ nét khi in (mặc định chỉ 1× theo màn hình).
    return run(["image", mdj, "-s", select, "-f", fmt, "-o", pattern, "--force-device-scale-factor=4"])


if __name__ == "__main__":
    cmd = sys.argv[1]
    code, out = build(sys.argv[2], sys.argv[3]) if cmd == "build" else export(sys.argv[2], sys.argv[3])
    print(code)
    print(out)
