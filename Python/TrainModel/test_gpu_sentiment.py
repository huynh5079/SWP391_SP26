import pandas as pd
import torch
from transformers import pipeline
import os
from tqdm import tqdm

def test_sentiment():
    # Use the same logic as DataTransfer.py but on 5 rows
    input_file = r'd:\PROJECT_SWP_SP26\swp391_sp26-develop\Python\TrainModel\data\cleaned_multi_label_dataset (2).xlsx'
    df = pd.read_excel(input_file, nrows=5)
    
    categories = ['Technical', 'Content', 'Instructor', 'General', 'Assessment']
    for cat in categories:
        df[cat] = 'notmentioned'
        
    print("Initializing LLM on GPU...")
    device = 0 if torch.cuda.is_available() else -1
    classifier = pipeline(
        "zero-shot-classification",
        model="typeform/distilbert-base-uncased-mnli",
        device=device
    )
    
    candidate_labels = ["positive", "negative", "normal", "notmentioned"]
    
    print("Testing 5 rows...")
    for index, row in tqdm(df.iterrows(), total=len(df)):
        review_text = str(row['Review']) if pd.notna(row['Review']) else ""
        if not review_text.strip():
            for cat in categories:
                df.at[index, cat] = 'mentioned'
            continue
            
        for cat in categories:
            hypothesis = f"The {cat.lower()} aspect is {{}}."
            result = classifier(review_text, candidate_labels, hypothesis_template=hypothesis)
            df.at[index, cat] = result['labels'][0]
            
    print("\n--- TEST RESULTS ---")
    print(df[['Review'] + categories].to_string())
    print("\n--- END OF TEST ---")

if __name__ == "__main__":
    test_sentiment()
