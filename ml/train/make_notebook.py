"""Sinh train_kaggle.ipynb (notebook fine-tune Qwen3 4B tren Kaggle, du lieu lay tu GitHub).

    python make_notebook.py
"""
import json
from pathlib import Path

CELLS = []


def md(text):
    CELLS.append({"cell_type": "markdown", "metadata": {}, "source": text.strip("\n")})


def code(text):
    CELLS.append({"cell_type": "code", "metadata": {}, "execution_count": None,
                  "outputs": [], "source": text.strip("\n")})


md("""
# Fine-tune Qwen3 4B phân loại sự cố chung cư

Trước khi chạy:
1. **Settings → Accelerator**: chọn `GPU T4 x2`. **Internet**: bật.
2. Bấm **Save Version → Save & Run All**. Không cần gắn Input: dữ liệu được tải từ GitHub.
3. Khi chạy xong, tải file `.gguf` ở tab **Output**.

Muốn train lại với dữ liệu mới: sửa `ml/train/raw/*.txt` trong repo, chạy `python build_dataset.py`,
push lên nhánh `main`, rồi bấm Save Version lần nữa.
""")

code("""
%%capture
!pip install unsloth
""")

code('''
import glob, json, os, urllib.request

# Du lieu lay thang tu GitHub. Sau khi push, GitHub co the mat vai phut moi tra ve ban moi.
DATA_URL = "https://raw.githubusercontent.com/huyduong231005-ui/DOANCOSO/main/ml/train"
TRAIN_PATH, VAL_PATH = "/kaggle/working/train.jsonl", "/kaggle/working/val.jsonl"
for path in (TRAIN_PATH, VAL_PATH):
    urllib.request.urlretrieve(f"{DATA_URL}/{os.path.basename(path)}", path)

BASE_MODEL = "unsloth/Qwen3-4B-unsloth-bnb-4bit"  # ban 4-bit cua Qwen/Qwen3-4B (Apache 2.0)
MAX_SEQ_LENGTH = 1024
EPOCHS = 3
OUT_DIR = "/kaggle/working/qwen3-suco"

# PHAI giong tung chu voi prompt trong run_test.py va MaintenanceTriageService.cs.
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

handler - nhóm nên tiếp nhận sự cố, chọn một:
- technician: thợ sửa chữa (luôn dùng khi category là plumbing, electrical, appliance hoặc structural)
- cleaning_team: đội vệ sinh
- pest_control: đơn vị diệt côn trùng
- network_provider: kỹ thuật mạng, nhà mạng
- security: bảo vệ (an ninh, người lạ, giữ xe, tiếng ồn, hàng xóm, vật nuôi, và ứng cứu khi có cháy hoặc người gặp nạn)
- building_engineering: kỹ thuật tòa nhà (thang máy, thiết bị phòng cháy, cổng, barie, tiện ích và cây xanh khu vực chung)
- accounting: kế toán (phí, hóa đơn, thanh toán)
- management: ban quản lý tự xử lý (thủ tục, đăng ký, góp ý, khiếu nại, hoặc mô tả chưa rõ)

priority - chọn một:
- urgent: đe dọa an toàn của người hoặc tài sản ngay lúc này (cháy, khói, rò gas, chập điện, điện giật, ngập nước lan rộng, người mắc kẹt, vật sắp rơi)
- high: mất một chức năng thiết yếu của căn hộ (mất điện, mất nước, mất internet khi cần gấp, toilet duy nhất bị tắc, không khóa được cửa, nước đang rò liên tục)
- medium: hỏng hóc gây bất tiện nhưng vẫn sinh hoạt được
- low: vấn đề nhỏ, thẩm mỹ, không gấp

is_dangerous: true nếu có nguy cơ cháy nổ, điện giật, ngạt khí, hoặc thương tích cho người.
need_more_info: true chỉ khi mô tả quá mơ hồ, không xác định được hỏng cái gì hoặc hỏng ở đâu.
follow_up_question: nếu need_more_info là true, viết MỘT câu hỏi ngắn, lịch sự bằng tiếng Việt để hỏi lại cư dân; nếu không thì để chuỗi rỗng.
summary: tóm tắt sự cố trong một câu tiếng Việt có dấu, tối đa 20 từ."""

FIELDS = ["category", "handler", "priority", "is_dangerous", "need_more_info", "follow_up_question", "summary"]
''')

code('''
from unsloth import FastLanguageModel

model, tokenizer = FastLanguageModel.from_pretrained(
    model_name=BASE_MODEL, max_seq_length=MAX_SEQ_LENGTH, load_in_4bit=True)

model = FastLanguageModel.get_peft_model(
    model, r=16, lora_alpha=16, lora_dropout=0, bias="none",
    target_modules=["q_proj", "k_proj", "v_proj", "o_proj", "gate_proj", "up_proj", "down_proj"],
    use_gradient_checkpointing="unsloth", random_state=3407)
''')

code('''
from datasets import Dataset


def read_jsonl(path):
    with open(path, encoding="utf-8") as f:
        return [json.loads(line) for line in f if line.strip()]


def prompt_for(text):
    """Phan dau vao, ket thuc dung o cho mo hinh bat dau tra loi (che do suy nghi tat)."""
    return tokenizer.apply_chat_template(
        [{"role": "system", "content": SYSTEM_PROMPT}, {"role": "user", "content": text}],
        tokenize=False, add_generation_prompt=True, enable_thinking=False)


def answer_for(row):
    return json.dumps({k: row[k] for k in FIELDS}, ensure_ascii=False)


def to_text(row):
    return {"text": prompt_for(row["text"]) + answer_for(row) + "<|im_end|>\\n"}


train_rows, val_rows = read_jsonl(TRAIN_PATH), read_jsonl(VAL_PATH)
train_ds = Dataset.from_list(train_rows).map(to_text, remove_columns=list(train_rows[0]))
val_ds = Dataset.from_list(val_rows).map(to_text, remove_columns=list(val_rows[0]))
print(len(train_ds), len(val_ds))
print(train_ds[0]["text"][-400:])  # kiem tra bang mat: phai ket thuc bang JSON roi <|im_end|>
''')

code('''
from trl import SFTConfig, SFTTrainer
from unsloth.chat_templates import train_on_responses_only

trainer = SFTTrainer(
    model=model, tokenizer=tokenizer, train_dataset=train_ds, eval_dataset=val_ds,
    args=SFTConfig(
        dataset_text_field="text",
        per_device_train_batch_size=8, gradient_accumulation_steps=2,
        num_train_epochs=EPOCHS, learning_rate=2e-4, warmup_steps=10,
        lr_scheduler_type="linear", weight_decay=0.01, optim="adamw_8bit",
        logging_steps=5, eval_strategy="epoch", save_strategy="no",
        seed=3407, output_dir="/kaggle/working/outputs", report_to="none"))

# Chi tinh loss tren phan JSON tra loi, khong tinh tren prompt he thong lap lai o moi vi du.
trainer = train_on_responses_only(
    trainer, instruction_part="<|im_start|>user\\n", response_part="<|im_start|>assistant\\n")

stats = trainer.train()
print(stats.metrics)
''')

md("""
## Kiểm tra nhanh trên tập val

Nếu `eval_loss` ở epoch 3 cao hơn epoch 2 thì mô hình bắt đầu học vẹt: giảm `EPOCHS` xuống 2 rồi chạy lại.
""")

code('''
import torch

FastLanguageModel.for_inference(model)


def predict(text):
    inputs = tokenizer(prompt_for(text), return_tensors="pt").to(model.device)
    with torch.no_grad():
        out = model.generate(**inputs, max_new_tokens=200, do_sample=False)
    raw = tokenizer.decode(out[0][inputs["input_ids"].shape[1]:], skip_special_tokens=True)
    try:
        return json.loads(raw)
    except json.JSONDecodeError:
        return None


hits = {k: 0 for k in ["json", "category", "handler", "priority", "is_dangerous", "need_more_info"]}
for row in val_rows:
    pred = predict(row["text"])
    if pred is None:
        continue
    hits["json"] += 1
    for k in list(hits)[1:]:
        hits[k] += pred.get(k) == row[k]
for k, v in hits.items():
    print(f"{k}: {v}/{len(val_rows)}")

for sample in ["nha em bi hu roi, nho sua giup", "Có khói bốc ra từ tủ điện ở hành lang tầng 9."]:
    print(sample, "->", predict(sample))
''')

md("""
## Xuất mô hình

Gộp LoRA vào mô hình gốc và xuất GGUF 4-bit (`q4_k_m`, cùng mức nén với `qwen3:4b` trên Ollama).
Bước này tải và biên dịch llama.cpp nên mất khoảng 10-20 phút.
""")

code('''
model.save_pretrained(OUT_DIR + "-lora")       # ban LoRA nho, de du phong
tokenizer.save_pretrained(OUT_DIR + "-lora")
model.save_pretrained_gguf(OUT_DIR, tokenizer, quantization_method="q4_k_m")

for path in glob.glob("/kaggle/working/**/*.gguf", recursive=True):
    print(path, f"{os.path.getsize(path) / 1e9:.2f} GB")
''')

md("""
Xong: bấm **Save Version → Save & Run All** để giữ file, rồi tải file `.gguf` ở tab **Output**
(hoặc tải ngay từ khung file bên phải trong phiên đang chạy).

## Đưa vào Ollama trên máy

```
echo FROM ./ten-file.gguf > Modelfile
ollama create qwen3-suco -f Modelfile
ollama show qwen3-suco
```

`ollama show` phải liệt kê `thinking` trong Capabilities: ứng dụng luôn gửi `think=false`, mô hình
không có khả năng này sẽ bị Ollama từ chối (HTTP 400) và tính năng phân loại tắt lặng lẽ.

Sau đó chấm điểm bằng đúng bộ test đã dùng cho mô hình gốc, rồi mới đổi `Ollama:Model`. Bản fine-tune phải
chạy với `--json-mode` (và `Ollama:JsonMode=true` trong app): gửi schema thì Ollama ép khóa JSON theo thứ
tự bảng chữ cái, lệch thứ tự lúc train, mô hình sẽ hỏi lại thừa.

```
python run_test.py --models qwen3:4b
python run_test.py --models qwen3-suco --json-mode --out results_suco
```
""")

notebook = {
    "cells": CELLS,
    "metadata": {"kernelspec": {"display_name": "Python 3", "language": "python", "name": "python3"},
                 "language_info": {"name": "python"}},
    "nbformat": 4, "nbformat_minor": 5,
}
out = Path(__file__).parent / "train_kaggle.ipynb"
out.write_text(json.dumps(notebook, ensure_ascii=False, indent=1), encoding="utf-8")
print("Da ghi", out)
