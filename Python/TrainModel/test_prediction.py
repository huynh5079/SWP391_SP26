import torch
import numpy as np
from transformers import AutoTokenizer, AutoModelForSequenceClassification
from pathlib import Path

# ===== LOAD =====
BASE_DIR = Path(__file__).resolve().parent
CHECKPOINT_DIR = (BASE_DIR / "artifacts" / "xlm_roberta" / "checkpoints" / "checkpoint-5351").as_posix()

device = torch.device("cuda" if torch.cuda.is_available() else "cpu")

tokenizer = AutoTokenizer.from_pretrained(
    CHECKPOINT_DIR,
    local_files_only=True,
    trust_remote_code=True
)
model = AutoModelForSequenceClassification.from_pretrained(CHECKPOINT_DIR, local_files_only=True)
model.to(device)
model.eval()

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

    return result, probs

# ===== TEST WITH VARIOUS SAMPLES =====
print("=" * 80)
print("TEST PREDICTION RESULTS")
print("=" * 80)

test_samples = [
    "Khóa học này không đáng với tôi",
    "Giảng viên dạy rất tệ",
    "Nội dung bài giảng rất hay và dễ hiểu",
    "Chất lượng video không tốt, có nhiều vấn đề kỹ thuật",
    "Bài kiểm tra quá khó, không có trong nội dung bài giảng",
    "Tôi thích khóa học này, nó rất hữu ích",
    "Giáo viên không thân thiện và không hỗ trợ",
    "Tất cả mọi thứ đều tốt, rất đáng học",
]

for i, text in enumerate(test_samples):
    print(f"\n{i+1}. Text: \"{text}\"")
    print(f"   Predictions:")
    predictions, all_probs = predict(text)
    if predictions:
        for label, prob in predictions:
            print(f"   - {label}: {prob:.3f}")
    else:
        print(f"   - No predictions above threshold")
        # Show top 3 predictions anyway
        print(f"   - Top predictions:")
        top_indices = np.argsort(all_probs)[-3:][::-1]
        for idx in top_indices:
            print(f"     * {labels[idx]}: {all_probs[idx]:.3f}")
