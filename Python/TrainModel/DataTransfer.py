import pandas as pd
import torch
from transformers import pipeline
from tqdm import tqdm
import os

def analyze_sentiment():
    input_file = r'D:\PROJECT_SWP_SP26\swp391_sp26-develop\Python\Data\coursera_label - coursera_label.csv'
    output_file = r'D:\PROJECT_SWP_SP26\swp391_sp26-develop\Python\Data\coursera_label_processed.csv'

    if not os.path.exists(input_file):
        print(f"Error: File not found at {input_file}")
        return

    print(f"Loading data from {input_file}...")
    df = pd.read_csv(input_file, on_bad_lines='skip')

    categories = ['Technical', 'Content', 'Instructor', 'General', 'Assessment']

    for cat in categories:
        if cat not in df.columns:
            df[cat] = 'notmentioned'

    print("Initializing LLM on GPU...")
    device = 0 if torch.cuda.is_available() else -1
    if device == 0:
        print(f"Using GPU: {torch.cuda.get_device_name(0)}")
    else:
        print("GPU not detected. Using CPU (this will be slower).")

    classifier = pipeline(
        "zero-shot-classification",
        model="typeform/distilbert-base-uncased-mnli",
        device=device
    )

    candidate_labels = ["positive", "negative", "normal", "notmentioned"]

    # Xác định đã xử lý đến đâu
    processed_count = 0
    if os.path.exists(output_file):
        try:
            processed_df = pd.read_csv(output_file, on_bad_lines='skip')
            processed_count = len(processed_df)
            print(f"Found existing output file. Resuming from row {processed_count}")
        except:
            print("Could not read existing output file. Starting from scratch.")
            processed_count = 0

    print("Analyzing sentiments and saving row by row...")

    for index, row in tqdm(df.iloc[processed_count:].iterrows(), total=len(df) - processed_count):
        row_data = row.copy()
        review_text = str(row['Review']) if pd.notna(row['Review']) else ""

        if not review_text.strip():
            for cat in categories:
                row_data[cat] = 'mentioned'
        else:
            for cat in categories:
                hypothesis = f"The {cat.lower()} aspect is {{}}."
                result = classifier(review_text, candidate_labels, hypothesis_template=hypothesis)
                top_label = result['labels'][0]
                row_data[cat] = top_label

        row_df = pd.DataFrame([row_data])
        row_df.to_csv(
            output_file,
            mode='a',
            header=not os.path.exists(output_file),
            index=False
        )

    print(f"Done! Results saved to {output_file}")

if __name__ == "__main__":
    analyze_sentiment()