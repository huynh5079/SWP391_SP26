import pandas as pd
import numpy as np
import torch

from datasets import Dataset
from transformers import AutoTokenizer, AutoModelForSequenceClassification, Trainer
from sklearn.metrics import classification_report, confusion_matrix, accuracy_score, precision_recall_fscore_support

# =========================
# CONFIG
# =========================
TEST_FILE = r'D:\PROJECT_SWP_SP26\swp391_sp26-develop\Python\Data\spiltdatatotrain\split\test.csv'
MODEL_PATH = r'D:\PROJECT_SWP_SP26\swp391_sp26-develop\Python\TrainModel\phobert_sentiment_model\checkpoint-17802'

# =========================
# LOAD TEST DATA
# =========================
print("📂 Loading test data...")
test_df = pd.read_csv(TEST_FILE)

test_df = test_df[['Review', 'Label']].copy()
test_df['Review'] = test_df['Review'].astype(str)
test_df['Label'] = test_df['Label'].astype(int)

test_df = test_df.rename(columns={'Label': 'labels'})

print(f"Test samples: {len(test_df)}")

# =========================
# HF DATASET
# =========================
test_ds = Dataset.from_pandas(test_df)

# =========================
# LOAD MODEL + TOKENIZER
# =========================
print("🔤 Loading tokenizer...")
tokenizer = AutoTokenizer.from_pretrained(MODEL_PATH)

print("🧠 Loading model...")
model = AutoModelForSequenceClassification.from_pretrained(MODEL_PATH)

# =========================
# TOKENIZE
# =========================
def tokenize_function(examples):
    return tokenizer(
        examples["Review"],
        truncation=True,
        padding=True,
        max_length=256
    )

test_ds = test_ds.map(tokenize_function, batched=True)
test_ds = test_ds.remove_columns(["Review"])

# =========================
# PREDICT ONLY (NHANH HƠN)
# =========================
trainer = Trainer(model=model)

print("\n📋 Predicting test set...")
pred_output = trainer.predict(test_ds)

preds = np.argmax(pred_output.predictions, axis=1)
true_labels = pred_output.label_ids

# =========================
# METRICS
# =========================
acc = accuracy_score(true_labels, preds)
precision, recall, f1, _ = precision_recall_fscore_support(
    true_labels, preds, average='macro', zero_division=0
)

print("\n📊 Overall Metrics:")
print(f"Accuracy       : {acc:.4f}")
print(f"Macro Precision: {precision:.4f}")
print(f"Macro Recall   : {recall:.4f}")
print(f"Macro F1       : {f1:.4f}")

label_names = ["negative", "normal", "positive"]

print("\n📋 Classification Report:")
print(classification_report(true_labels, preds, target_names=label_names, digits=4))

print("\n🧩 Confusion Matrix:")
print(confusion_matrix(true_labels, preds))