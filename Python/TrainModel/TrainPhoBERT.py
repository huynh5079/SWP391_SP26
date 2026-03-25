import os
import pandas as pd
import numpy as np
import torch
import torch.nn as nn

from datasets import Dataset
from transformers import (
    AutoTokenizer,
    AutoModelForSequenceClassification,
    TrainingArguments,
    Trainer,
    DataCollatorWithPadding
)

from sklearn.metrics import (
    accuracy_score,
    precision_recall_fscore_support,
    classification_report,
    confusion_matrix
)
from sklearn.utils.class_weight import compute_class_weight

# =========================
# 1. CONFIG
# =========================
TRAIN_FILE = r'D:\PROJECT_SWP_SP26\swp391_sp26-develop\Python\Data\spiltdatatotrain\split\train.csv'
VAL_FILE   = r'D:\PROJECT_SWP_SP26\swp391_sp26-develop\Python\Data\spiltdatatotrain\split\val.csv'
TEST_FILE  = r'D:\PROJECT_SWP_SP26\swp391_sp26-develop\Python\Data\spiltdatatotrain\split\test.csv'

MODEL_NAME = "vinai/phobert-base"
OUTPUT_DIR = r'D:\PROJECT_SWP_SP26\swp391_sp26-develop\Python\TrainModel\phobert_sentiment_model'

MAX_LEN = 256
BATCH_SIZE = 8
EPOCHS = 4
LR = 2e-5
SEED = 42

os.makedirs(OUTPUT_DIR, exist_ok=True)

# =========================
# 2. LOAD DATA
# =========================
print("📂 Loading datasets...")

train_df = pd.read_csv(TRAIN_FILE)
val_df = pd.read_csv(VAL_FILE)
test_df = pd.read_csv(TEST_FILE)

# chỉ giữ cột cần
train_df = train_df[['Review', 'Label']].copy()
val_df = val_df[['Review', 'Label']].copy()
test_df = test_df[['Review', 'Label']].copy()

# ép kiểu cho chắc
train_df['Review'] = train_df['Review'].astype(str)
val_df['Review'] = val_df['Review'].astype(str)
test_df['Review'] = test_df['Review'].astype(str)

train_df['Label'] = train_df['Label'].astype(int)
val_df['Label'] = val_df['Label'].astype(int)
test_df['Label'] = test_df['Label'].astype(int)

print(f"Train: {len(train_df)}")
print(f"Val:   {len(val_df)}")
print(f"Test:  {len(test_df)}")

# đổi tên cột cho HF
train_df = train_df.rename(columns={'Label': 'labels'})
val_df = val_df.rename(columns={'Label': 'labels'})
test_df = test_df.rename(columns={'Label': 'labels'})

# =========================
# 3. CLASS WEIGHTS (chống lệch nhãn)
# =========================
print("\n⚖ Computing class weights...")
classes = np.array([0, 1, 2])  # negative, normal, positive
class_weights = compute_class_weight(
    class_weight="balanced",
    classes=classes,
    y=train_df["labels"].values
)

class_weights = torch.tensor(class_weights, dtype=torch.float)
print("Class weights:", class_weights)

# =========================
# 4. HF DATASET
# =========================
train_ds = Dataset.from_pandas(train_df)
val_ds = Dataset.from_pandas(val_df)
test_ds = Dataset.from_pandas(test_df)

# =========================
# 5. TOKENIZER
# =========================
print("\n🔤 Loading tokenizer...")
tokenizer = AutoTokenizer.from_pretrained(MODEL_NAME)

def tokenize_function(examples):
    return tokenizer(
        examples["Review"],
        truncation=True,
        padding=False,
        max_length=MAX_LEN
    )

train_ds = train_ds.map(tokenize_function, batched=True)
val_ds = val_ds.map(tokenize_function, batched=True)
test_ds = test_ds.map(tokenize_function, batched=True)

train_ds = train_ds.remove_columns(["Review"])
val_ds = val_ds.remove_columns(["Review"])
test_ds = test_ds.remove_columns(["Review"])

# =========================
# 6. MODEL
# =========================
print("\n🧠 Loading PhoBERT model...")
model = AutoModelForSequenceClassification.from_pretrained(
    MODEL_NAME,
    num_labels=3
)

# =========================
# 7. METRICS
# =========================
def compute_metrics(eval_pred):
    logits, labels = eval_pred
    preds = np.argmax(logits, axis=1)

    precision, recall, f1, _ = precision_recall_fscore_support(
        labels, preds, average='macro', zero_division=0
    )
    acc = accuracy_score(labels, preds)

    return {
        "accuracy": acc,
        "macro_precision": precision,
        "macro_recall": recall,
        "macro_f1": f1
    }

# =========================
# 8. CUSTOM TRAINER (weighted loss)
# =========================
class WeightedTrainer(Trainer):
    def compute_loss(self, model, inputs, return_outputs=False, **kwargs):
        labels = inputs.get("labels")
        outputs = model(**inputs)
        logits = outputs.get("logits")

        loss_fct = nn.CrossEntropyLoss(
            weight=class_weights.to(model.device)
        )
        loss = loss_fct(logits, labels)

        return (loss, outputs) if return_outputs else loss

# =========================
# 9. TRAINING ARGS
# =========================
training_args = TrainingArguments(
    output_dir=OUTPUT_DIR,
    eval_strategy="epoch",
    save_strategy="epoch",
    logging_strategy="steps",
    logging_steps=100,
    learning_rate=LR,
    per_device_train_batch_size=BATCH_SIZE,
    per_device_eval_batch_size=BATCH_SIZE,
    num_train_epochs=EPOCHS,
    weight_decay=0.01,
    load_best_model_at_end=True,
    metric_for_best_model="macro_f1",
    greater_is_better=True,
    save_total_limit=2,
    fp16=torch.cuda.is_available(),
    report_to="none",
    seed=SEED
)

# =========================
# 10. TRAINER
# =========================
data_collator = DataCollatorWithPadding(tokenizer=tokenizer)

trainer = WeightedTrainer(
    model=model,
    args=training_args,
    train_dataset=train_ds,
    eval_dataset=val_ds,
    tokenizer=tokenizer,
    data_collator=data_collator,
    compute_metrics=compute_metrics
)

# =========================
# 11. TRAIN
# =========================
print("\n🚀 Starting PhoBERT training...")
trainer.train()

# =========================
# 12. EVALUATE VAL / TEST
# =========================
print("\n📊 Evaluating on validation set...")
val_results = trainer.evaluate(val_ds)
print(val_results)

print("\n📊 Evaluating on test set...")
test_results = trainer.evaluate(test_ds)
print(test_results)

# =========================
# 13. DETAILED REPORT ON TEST
# =========================
print("\n📋 Detailed test report...")
pred_output = trainer.predict(test_ds)
preds = np.argmax(pred_output.predictions, axis=1)
true_labels = pred_output.label_ids

label_names = ["negative", "normal", "positive"]

print("\nClassification Report:")
print(classification_report(true_labels, preds, target_names=label_names, digits=4))

print("\nConfusion Matrix:")
print(confusion_matrix(true_labels, preds))

# =========================
# 14. SAVE MODEL
# =========================
print("\n💾 Saving model...")
trainer.save_model(OUTPUT_DIR)
tokenizer.save_pretrained(OUTPUT_DIR)

print("\n✅ DONE! PhoBERT model saved to:")
print(OUTPUT_DIR)