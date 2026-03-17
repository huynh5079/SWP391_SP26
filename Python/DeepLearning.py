import pandas as pd
from pathlib import Path
from sklearn.model_selection import train_test_split
from transformers import BertTokenizer
import torch

# Build safe path to CSV file
csv_path = Path(__file__).resolve().parents[2] / "Dataset" / "reviews.csv"
if csv_path.is_dir():
    csv_path = csv_path / "reviews.csv"

df = pd.read_csv(csv_path, engine="python", on_bad_lines='skip')

print(df.head())
print("Columns:", df.columns.tolist())
# Clean column names by removing excess whitespace/semicolons
df.columns = df.columns.str.strip(';').str.strip()
df = df[['Comment', 'RatingEvent']]
train_texts, val_texts, train_labels, val_labels = train_test_split(
    df['Comment'].fillna('').astype(str),
    df['RatingEvent'].fillna('3').astype(str).str.strip(';').astype(int),
    test_size=0.2,
    random_state=42
)
tokenizer = BertTokenizer.from_pretrained('bert-base-uncased')

train_encodings = tokenizer(
    train_texts.tolist(),
    truncation=True,
    padding=True,
    max_length=128
)

val_encodings = tokenizer(
    val_texts.tolist(),
    truncation=True,
    padding=True,
    max_length=128
)
class ReviewDataset(torch.utils.data.Dataset):
    def __init__(self, encodings, labels):
        self.encodings = encodings
        self.labels = labels.tolist()

    def __getitem__(self, idx):
        item = {key: torch.tensor(val[idx]) for key, val in self.encodings.items()}
        item['labels'] = torch.tensor(self.labels[idx] - 1)  # label 1–5 → 0–4
        return item

    def __len__(self):
        return len(self.labels)

train_dataset = ReviewDataset(train_encodings, train_labels)
val_dataset = ReviewDataset(val_encodings, val_labels)

from transformers import BertForSequenceClassification

model = BertForSequenceClassification.from_pretrained(
    'bert-base-uncased',
    num_labels=5
)

from transformers import Trainer, TrainingArguments

training_args = TrainingArguments(
    output_dir="./results",
    learning_rate=2e-5,
    per_device_train_batch_size=16,
    per_device_eval_batch_size=16,
    num_train_epochs=3,
    eval_strategy="epoch"
)

trainer = Trainer(
    model=model,
    args=training_args,
    train_dataset=train_dataset,
    eval_dataset=val_dataset
)

trainer.train()
trainer.evaluate()
#dự đoán
text = "Very useful course"

inputs = tokenizer(text, return_tensors="pt")

outputs = model(**inputs)

pred = torch.argmax(outputs.logits)

print("Predicted rating:", pred.item() + 1)


#save
model.save_pretrained("review_model")
tokenizer.save_pretrained("review_model")