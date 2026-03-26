from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
import torch
from transformers import AutoTokenizer, AutoModelForSequenceClassification
import uvicorn
import os

app = FastAPI()

# =========================
# CONFIG
# =========================
MODEL_PATH = r'D:\PROJECT_SWP_SP26\swp391_sp26-develop\Python\TrainModel\phobert_sentiment_model'
DEVICE = "cuda" if torch.cuda.is_available() else "cpu"

print(f"📡 Loading model from {MODEL_PATH} on {DEVICE}...")

try:
    tokenizer = AutoTokenizer.from_pretrained(MODEL_PATH)
    model = AutoModelForSequenceClassification.from_pretrained(MODEL_PATH).to(DEVICE)
    model.eval()
    print("✅ Model loaded successfully!")
except Exception as e:
    print(f"❌ Error loading model: {e}")
    # Fallback or exit if critical
    raise e

label_map = {0: "negative", 1: "normal", 2: "positive"}

class FeedbackRequest(BaseModel):
    comment: str
    eventId: str = ""

class FeedbackResponse(BaseModel):
    eventId: str
    comment: str
    Label: int
    Label_Text: str
    Technical: int
    Technical_Text: str
    Content: int
    Content_Text: str
    Instructor: int
    Instructor_Text: str
    Assessment: int
    Assessment_Text: str

@app.post("/predict", response_model=FeedbackResponse)
async def predict(request: FeedbackRequest):
    if not request.comment:
        raise HTTPException(status_code=400, detail="Comment is empty")

    # Tokenizer
    inputs = tokenizer(request.comment, return_tensors="pt", truncation=True, padding=True, max_length=256).to(DEVICE)

    with torch.no_grad():
        outputs = model(**inputs)
        logits = outputs.logits
        prediction = torch.argmax(logits, dim=1).item()

    label_text = label_map.get(prediction, "unknown")

    # Since the current model is single-label, we use the same sentiment for all aspects for now.
    # In a real ABSA scenario, we'd have multiple models or a multi-head model.
    return FeedbackResponse(
        eventId=request.eventId,
        comment=request.comment,
        Label=prediction,
        Label_Text=label_text,
        Technical=prediction,
        Technical_Text=label_text,
        Content=prediction,
        Content_Text=label_text,
        Instructor=prediction,
        Instructor_Text=label_text,
        Assessment=prediction,
        Assessment_Text=label_text
    )

@app.get("/health")
async def health():
    return {"status": "ok"}

if __name__ == "__main__":
    uvicorn.run(app, host="127.0.0.1", port=8011)
