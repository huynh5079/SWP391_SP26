import torch
import numpy as np
import joblib
from transformers import AutoTokenizer, AutoModelForSequenceClassification
from pathlib import Path
import os

# ===== LOAD =====
BASE_DIR = Path(__file__).resolve().parent

# Sử dụng checkpoint thay vì các đường dẫn riêng lẻ
CHECKPOINT_DIR = (BASE_DIR / "artifacts" / "xlm_roberta" / "checkpoints" / "checkpoint-5351").as_posix()
LABEL_PATH = (BASE_DIR / "artifacts" / "xlm_roberta" / "label_names.pkl").as_posix()

device = torch.device("cuda" if torch.cuda.is_available() else "cpu")

tokenizer = AutoTokenizer.from_pretrained(
    CHECKPOINT_DIR,
    local_files_only=True,
    trust_remote_code=True
)
model = AutoModelForSequenceClassification.from_pretrained(CHECKPOINT_DIR, local_files_only=True)
model.to(device)
model.eval()

# Danh sách nhãn từ Deep-Learning.py
labels = [
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

# ===== PREDICT FUNCTION =====
def predict(text, threshold=0.3):
    inputs = tokenizer(
        text,
        return_tensors="pt",
        truncation=True,
        padding=True,
        max_length=128
    ).to(device)

    with torch.no_grad():
        outputs = model(**inputs)
        logits = outputs.logits
        probs = torch.sigmoid(logits).cpu().numpy()[0]

    preds = (probs >= threshold).astype(int)

    result = []
    for i, label in enumerate(labels):
        if preds[i] == 1:
            result.append((label, float(probs[i])))

    return result

# ===== TEST =====
if __name__ == "__main__":
    # Ví dụ test
    test_text = "Khóa học này không đáng với tôi"
    predictions = predict(test_text)
    print("Predictions:", predictions)

    text = "Giảng viên dạy rất tệ"
    result = predict(text)
    print("Text:", text)
    print("Labels predicted:")
    for label, prob in result:
        print(f"{label}: {prob:.3f}")