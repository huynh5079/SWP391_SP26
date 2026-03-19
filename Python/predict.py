"""
predict.py – Load trained BERT model and predict RatingEvent (1–5) for a review.

Usage:
    # Single text via command line:
    python predict.py "This course was very helpful and well structured"

    # Interactive mode (no argument):
    python predict.py
"""

import sys
from pathlib import Path

import torch
from transformers import BertForSequenceClassification, BertTokenizer

MODEL_DIR = Path(__file__).parent / "review_model"
RESULTS_DIR = Path(__file__).parent / "results"


def _latest_checkpoint() -> Path | None:
    """Return the checkpoint folder with the highest step number, or None."""
    if not RESULTS_DIR.exists():
        return None
    checkpoints = sorted(
        (d for d in RESULTS_DIR.iterdir() if d.is_dir() and d.name.startswith("checkpoint-")),
        key=lambda d: int(d.name.split("-")[1]),
    )
    return checkpoints[-1] if checkpoints else None


def load_model():
    # Prefer the final saved model; fall back to latest checkpoint.
    if MODEL_DIR.exists():
        source = MODEL_DIR
        tokenizer = BertTokenizer.from_pretrained(str(source))
    else:
        source = _latest_checkpoint()
        if source is None:
            raise FileNotFoundError(
                "No trained model found. "
                f"Expected '{MODEL_DIR}' or checkpoints under '{RESULTS_DIR}'. "
                "Please run DeepLearning.py first to train the model."
            )
        print(f"[INFO] 'review_model' not found. Loading from checkpoint: {source.name}")
        # Checkpoints only store model weights; tokenizer must be loaded from HuggingFace.
        tokenizer = BertTokenizer.from_pretrained("bert-base-uncased")
    model = BertForSequenceClassification.from_pretrained(str(source))
    model.eval()
    return tokenizer, model


def predict(text: str, tokenizer, model) -> int:
    """Return predicted rating (1–5) for the given review text."""
    inputs = tokenizer(
        text,
        return_tensors="pt",
        truncation=True,
        padding=True,
        max_length=128,
    )
    with torch.no_grad():
        outputs = model(**inputs)
    rating = torch.argmax(outputs.logits).item() + 1  # 0–4 → 1–5
    return rating


def main():
    tokenizer, model = load_model()
    source = MODEL_DIR if MODEL_DIR.exists() else _latest_checkpoint()
    print(f"Model loaded from: {source}\n")

    # If text is passed as a command-line argument, predict and exit.
    if len(sys.argv) > 1:
        text = " ".join(sys.argv[1:])
        rating = predict(text, tokenizer, model)
        print(f"Review : {text}")
        print(f"Predicted rating: {rating} / 5")
        return

    # Otherwise, run interactively.
    print("Interactive prediction mode. Type 'quit' to exit.\n")
    while True:
        text = input("Enter review text: ").strip()
        if text.lower() in ("quit", "exit", "q"):
            break
        if not text:
            continue
        rating = predict(text, tokenizer, model)
        print(f"  → Predicted rating: {rating} / 5\n")


if __name__ == "__main__":
    main()
