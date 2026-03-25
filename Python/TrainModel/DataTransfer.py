import pandas as pd
import torch
from transformers import pipeline
from tqdm import tqdm
import os

def analyze_sentiment():
    # File paths
    input_file = r'D:\PROJECT_SWP_SP26\swp391_sp26-develop\Python\Data\coursera_label - coursera_label.csv'
    
    # Check if file exists
    if not os.path.exists(input_file):
        print(f"Error: File not found at {input_file}")
        return

    print(f"Loading data from {input_file}...")
    df = pd.read_csv(input_file, on_bad_lines='skip')

    # Categories to analyze
    categories = ['Technical', 'Content', 'Instructor', 'General', 'Assessment']
    
    # Ensure columns exist, initialize with 'notmentioned' if they don't
    for cat in categories:
        if cat not in df.columns:
            df[cat] = 'notmentioned'

    # Initialize LLM Pipeline (Zero-Shot Classification)
    print("Initializing LLM on GPU...")
    device = 0 if torch.cuda.is_available() else -1
    if device == 0:
        print(f"Using GPU: {torch.cuda.get_device_name(0)}")
    else:
        print("GPU not detected. Using CPU (this will be slower).")

    # typeform/distilbert-base-uncased-mnli is a fast, small (~250MB) and very reliable zero-shot model
    classifier = pipeline(
        "zero-shot-classification",
        model="typeform/distilbert-base-uncased-mnli",
        device=device
    )

    candidate_labels = ["positive", "negative", "normal", "notmentioned"]

    print("Analyzing sentiments...")
    # Iterate through rows
    for index, row in tqdm(df.iterrows(), total=len(df)):
        review_text = str(row['Review']) if pd.notna(row['Review']) else ""
        
        # Rule: If no data (empty review), set to 'mentioned' as requested
        if not review_text.strip():
            for cat in categories:
                df.at[index, cat] = 'mentioned'
            continue

        # For each category, determine the sentiment
        for cat in categories:
            # Simple hypothesis works best for zero-shot sentiment
            hypothesis = f"The {cat.lower()} aspect is {{}}."
            
            result = classifier(review_text, candidate_labels, hypothesis_template=hypothesis)
            
            # The top label (highest score)
            top_label = result['labels'][0]
            df.at[index, cat] = top_label

    # Save the updated file (Writing back to the same file as requested)
    output_file = input_file
    print(f"Saving results to {output_file}...")
    df.to_csv(output_file, index=False)
    print("Done!")

if __name__ == "__main__":
    analyze_sentiment()
