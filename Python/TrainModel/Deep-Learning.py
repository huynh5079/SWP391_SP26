import pandas as pd
import joblib
import ast
import numpy as np
import torch
import inspect
from pathlib import Path
from sklearn.model_selection import train_test_split
from sklearn.metrics import classification_report
from sklearn.preprocessing import MultiLabelBinarizer
from torch.utils.data import Dataset
from transformers import (
    AutoTokenizer,
    AutoModelForSequenceClassification,
    Trainer,
    TrainingArguments,
)

# ==========================================
# 1. CẤU HÌNH THAM SỐ
# ==========================================
MODEL_NAME = "xlm-roberta-base"
MAX_LENGTH = 128
RANDOM_STATE = 42
TEST_SIZE = 0.2
EPOCHS = 5              # Tăng từ 3 lên 5 epochs
TRAIN_BATCH_SIZE = 16  # Giảm xuống 8 nếu bị lỗi Out of Memory (OOM)
EVAL_BATCH_SIZE = 16
LEARNING_RATE = 5e-5   # Tăng learning rate để model học nhanh hơn
THRESHOLD = 0.3        # Giảm threshold để bắt được predictions nhỏ hơn

# Danh sách 20 nhãn mục tiêu (phải khớp hoàn toàn với dữ liệu trong file)
TOP_LABELS = [
    'General_Positive','General_Neutral','General_Negative','General_High_Quality',
    'General_Clear_Explanation','General_Practical_Knowledge','General_Language_Issue',
    'General_Missing_Content','General_Well_Structured',

    'Content_Positive','Content_Neutral','Content_Negative','Content_High_Quality',
    'Content_Clear_Explanation','Content_Practical_Knowledge','Content_Language_Issue',
    'Content_Missing_Content','Content_Well_Structured',

    'Instructor_Positive','Instructor_Neutral','Instructor_Negative','Instructor_High_Quality',
    'Instructor_Clear_Explanation','Instructor_Practical_Knowledge','Instructor_Language_Issue',
    'Instructor_Missing_Content','Instructor_Well_Structured',

    'Assessment_Positive','Assessment_Neutral','Assessment_Negative','Assessment_High_Quality',
    'Assessment_Clear_Explanation','Assessment_Practical_Knowledge','Assessment_Language_Issue',
    'Assessment_Missing_Content','Assessment_Well_Structured',

    'Technical_Positive','Technical_Neutral','Technical_Negative','Technical_High_Quality',
    'Technical_Clear_Explanation','Technical_Practical_Knowledge','Technical_Language_Issue',
    'Technical_Missing_Content','Technical_Well_Structured'
]

# ==========================================
# 2. XỬ LÝ DỮ LIỆU
# ==========================================
# Đường dẫn file CSV bạn đã tải lên
data_path = 'D:\PROJECT_SWP_SP26\swp391_sp26-develop\Python\TrainModel\data\cleaned_multi_label_dataset (2).xlsx'
df = pd.read_excel(data_path)

if not Path(data_path).exists():
    raise FileNotFoundError(f"Không tìm thấy file tại: {data_path}")

print("Đang đọc và xử lý dữ liệu...")
df = pd.read_excel(data_path)

# Chuyển đổi '['Label1']' (string) thành ['Label1'] (list)
def parse_labels(label_str):
    try:
        return ast.literal_eval(label_str)
    except:
        return []

df['parsed_labels'] = df['labels_list'].apply(parse_labels)

# Chỉ giữ lại các nhãn nằm trong danh sách TOP_LABELS
df['filtered_labels'] = df['parsed_labels'].apply(
    lambda x: [l for l in x if l in TOP_LABELS]
)

# Chuyển đổi nhãn sang dạng số 0 và 1
mlb = MultiLabelBinarizer(classes=TOP_LABELS)
y_encoded = mlb.fit_transform(df['filtered_labels'])

# Lọc bỏ các dòng không có nhãn nào sau khi lọc (tránh làm nhiễu model)
mask = y_encoded.sum(axis=1) > 0
X_all = df['Cleaned_Review'][mask].fillna("").astype(str).tolist()
y_all = y_encoded[mask]

# ===== CÂN BẰNG DỮ LIỆU - ĐẠO DIỆN MẪU CÓ NEGATIVE LABELS =====
# Phát hiện mẫu với Negative labels (class imbalance)
neg_indices = []
pos_indices = []

for i in range(len(y_all)):
    has_negative = any(TOP_LABELS[j] for j in range(len(TOP_LABELS)) 
                      if 'Negative' in TOP_LABELS[j] and y_all[i][j] == 1)
    if has_negative:
        neg_indices.append(i)
    else:
        pos_indices.append(i)

print(f"Phát hiện: {len(neg_indices)} mẫu với Negative | {len(pos_indices)} mẫu không có Negative")

# Tăng số mẫu Negative bằng cách đạo diện (oversampling)
if len(neg_indices) > 0:
    oversample_factor = max(3, len(pos_indices) // (len(neg_indices) + 1))  # Tối thiểu 3x
    oversample_neg = np.random.choice(neg_indices, size=min(len(pos_indices)//2, len(neg_indices)*oversample_factor), replace=True)
    all_indices = list(pos_indices) + list(oversample_neg)
    X_all = [X_all[i] for i in all_indices]
    y_all = y_encoded[[i for i in all_indices]]
    print(f"Sau cân bằng: {len(X_all)} mẫu tổng cộng")

# Chia tập Train/Test
X_train, X_test, y_train, y_test = train_test_split(
    X_all, y_all, test_size=TEST_SIZE, random_state=RANDOM_STATE
)

print(f"Tổng số mẫu (sau cân bằng): {len(X_all)}")
print(f"Số mẫu Train: {len(X_train)} | Số mẫu Test: {len(X_test)}")

# ==========================================
# 3. DATASET & METRICS
# ==========================================
class ReviewDataset(Dataset):
    def __init__(self, texts, labels, tokenizer, max_length):
        self.texts = texts
        self.labels = labels
        self.tokenizer = tokenizer
        self.max_length = max_length

    def __len__(self):
        return len(self.texts)

    def __getitem__(self, idx):
        encoding = self.tokenizer(
            self.texts[idx],
            truncation=True,
            padding='max_length',
            max_length=self.max_length,
            return_tensors="pt",
        )
        item = {key: value.squeeze(0) for key, value in encoding.items()}
        item["labels"] = torch.tensor(self.labels[idx], dtype=torch.float32)
        return item

def compute_metrics(eval_pred):
    logits, labels = eval_pred
    # Dùng Sigmoid để lấy xác suất cho từng nhãn độc lập
    probs = 1.0 / (1.0 + np.exp(-logits))
    preds = (probs >= THRESHOLD).astype(int)
    
    report = classification_report(labels, preds, output_dict=True, zero_division=0)
    return {
        "micro_f1": report["micro avg"]["f1-score"],
        "macro_f1": report["macro avg"]["f1-score"],
    }

# ==========================================
# 4. HUẤN LUYỆN (TRAINING)
# ==========================================
print(f"Đang tải model {MODEL_NAME}...")
tokenizer = AutoTokenizer.from_pretrained(MODEL_NAME)
model = AutoModelForSequenceClassification.from_pretrained(
    MODEL_NAME,
    num_labels=len(TOP_LABELS),
    problem_type="multi_label_classification"
)

train_dataset = ReviewDataset(X_train, y_train, tokenizer, MAX_LENGTH)
test_dataset = ReviewDataset(X_test, y_test, tokenizer, MAX_LENGTH)

# Thư mục lưu kết quả
output_dir = Path("artifacts/xlm_roberta")
output_dir.mkdir(parents=True, exist_ok=True)

training_args = TrainingArguments(
    output_dir=str(output_dir / "checkpoints"),
    evaluation_strategy="epoch",
    save_strategy="epoch",
    learning_rate=LEARNING_RATE,
    per_device_train_batch_size=TRAIN_BATCH_SIZE,
    per_device_eval_batch_size=EVAL_BATCH_SIZE,
    num_train_epochs=EPOCHS,
    weight_decay=0.01,
    load_best_model_at_end=True,
    metric_for_best_model="micro_f1",
    fp16=torch.cuda.is_available(), # Tự động bật FP16 nếu có GPU
    logging_steps=100,
    warmup_steps=500,  # Warmup steps để model học ổn định
    gradient_accumulation_steps=1,
    seed=RANDOM_STATE,
)

trainer = Trainer(
    model=model,
    args=training_args,
    train_dataset=train_dataset,
    eval_dataset=test_dataset,
    tokenizer=tokenizer,
    compute_metrics=compute_metrics,
)

print("Bắt đầu quá trình huấn luyện (Training)...")
trainer.train()

# ==========================================
# 5. ĐÁNH GIÁ & LƯU TRỮ
# ==========================================
print("\nĐánh giá kết quả trên tập Test...")
pred_output = trainer.predict(test_dataset)
logits = pred_output.predictions
probs = 1.0 / (1.0 + np.exp(-logits))
y_pred = (probs >= THRESHOLD).astype(int)

print("\n--- BÁO CÁO PHÂN LOẠI CHI TIẾT ---")
print(classification_report(y_test, y_pred, target_names=TOP_LABELS, zero_division=0))

# Lưu Model, Tokenizer và danh sách Nhãn
model.save_pretrained(output_dir / "model")
tokenizer.save_pretrained(output_dir / "tokenizer")
joblib.dump(TOP_LABELS, output_dir / "label_names.pkl")

print(f"\nHoàn tất! Model và nhãn đã được lưu tại: {output_dir}")