import pandas as pd
import re
import os

def fix_review_and_label():
    input_file = r'D:\PROJECT_SWP_SP26\swp391_sp26-develop\Python\Data\coursera_label_cleaned.csv'
    output_csv = r'D:\PROJECT_SWP_SP26\swp391_sp26-develop\Python\Data\coursera_label_fixed.csv'
    output_xlsx = r'D:\PROJECT_SWP_SP26\swp391_sp26-develop\Python\Data\coursera_label_fixed.xlsx'

    if not os.path.exists(input_file):
        print(f"❌ File not found: {input_file}")
        return

    print(f"📂 Loading {input_file} ...")
    df = pd.read_csv(input_file)

    print(f"Raw rows: {len(df)}")

    # ===== 1. Chuẩn hóa Review =====
    df['Review'] = df['Review'].astype(str).str.strip()
    df['Review'] = df['Review'].str.replace(r'\s+', ' ', regex=True)

    # ===== 2. Tách số rating ở cuối Review =====
    # Ví dụ:
    # "good and interesting,5" -> review="good and interesting", rating=5
    # "This course is bad,2" -> review="This course is bad", rating=2
    def extract_rating_from_review(text):
        if not isinstance(text, str):
            return text, None

        text = text.strip()

        # match dấu phẩy + số 1-5 ở cuối
        match = re.match(r'^(.*?)[,\s]+([1-5])$', text)
        if match:
            review_text = match.group(1).strip()
            rating = int(match.group(2))
            return review_text, rating

        return text, None

    extracted_reviews = []
    extracted_ratings = []

    for review in df['Review']:
        clean_review, rating = extract_rating_from_review(review)
        extracted_reviews.append(clean_review)
        extracted_ratings.append(rating)

    df['Review'] = extracted_reviews
    df['Rating'] = extracted_ratings

    # ===== 3. Nếu không tách được rating từ Review thì thử lấy từ Label cũ =====
    def try_parse_old_label(x):
        x = str(x).strip().lower()
        if x in ['1', '2', '3', '4', '5']:
            return int(x)
        return None

    old_label_rating = df['Label'].apply(try_parse_old_label)

    # nếu Rating mới đang null thì lấy từ Label cũ nếu có
    df['Rating'] = df['Rating'].fillna(old_label_rating)

    # ===== 4. Bỏ row không có rating =====
    before = len(df)
    df = df[df['Rating'].notna()]
    print(f"✅ Rows with valid rating: {len(df)} / {before}")

    df['Rating'] = df['Rating'].astype(int)

    # ===== 5. Bỏ review rác =====
    def is_valid_review(text):
        if not isinstance(text, str):
            return False

        text = text.strip()

        if len(text) < 10:
            return False

        letters = re.findall(r'[A-Za-z]', text)
        if len(letters) < 3:
            return False

        return True

    df = df[df['Review'].apply(is_valid_review)]
    print(f"✅ After removing broken reviews: {len(df)}")

    # ===== 6. Tạo Label sentiment từ Rating =====
    def rating_to_sentiment(r):
        if r in [1, 2]:
            return 'negative'
        elif r == 3:
            return 'normal'
        elif r in [4, 5]:
            return 'positive'
        return None

    df['Label_Text'] = df['Rating'].apply(rating_to_sentiment)

    sentiment_map = {
        'negative': 0,
        'normal': 1,
        'positive': 2
    }
    df['Label'] = df['Label_Text'].map(sentiment_map)

    # ===== 7. Chuẩn hóa aspect labels =====
    aspect_cols = ['Technical', 'Content', 'Instructor', 'General', 'Assessment']

    def normalize_aspect_label(x):
        x = str(x).strip().lower()
        if x in ['positive', 'negative', 'normal', 'mentioned', 'notmentioned']:
            return x
        if x == 'neutral':
            return 'normal'
        return 'notmentioned'

    for col in aspect_cols:
        if col in df.columns:
            df[col] = df[col].apply(normalize_aspect_label)

    # ===== 8. Drop cột vô dụng =====
    useless_cols = []
    for col in aspect_cols:
        if col in df.columns and df[col].nunique() <= 1:
            useless_cols.append(col)

    if useless_cols:
        print(f"⚠ Removing useless columns: {useless_cols}")
        df = df.drop(columns=useless_cols)

    # ===== 9. Encode aspect labels thành số =====
    aspect_label_map = {
        'notmentioned': 0,
        'mentioned': 1,
        'normal': 2,
        'positive': 3,
        'negative': 4
    }

    for col in df.columns:
        if col not in ['Id', 'Review', 'Rating', 'Label', 'Label_Text']:
            df[col + '_Text'] = df[col]
            df[col] = df[col].map(aspect_label_map)

    # ===== 10. Xóa duplicate =====
    df = df.drop_duplicates(subset=['Review', 'Rating'])
    df = df.reset_index(drop=True)

    # ===== 11. Save =====
    df.to_csv(output_csv, index=False, encoding='utf-8-sig')
    print(f"💾 Saved CSV: {output_csv}")

    try:
        df.to_excel(output_xlsx, index=False)
        print(f"📗 Saved Excel: {output_xlsx}")
    except Exception as e:
        print(f"⚠ Could not save xlsx: {e}")

    # ===== 12. Summary =====
    print("\n📊 Final shape:", df.shape)

    print("\n📊 Rating distribution:")
    print(df['Rating'].value_counts().sort_index())

    print("\n📊 Sentiment distribution:")
    print(df['Label_Text'].value_counts())

    print("\n📊 Label numeric meaning:")
    print(sentiment_map)

    print("\n📊 Aspect numeric meaning:")
    print(aspect_label_map)

    print("\n🔎 Sample:")
    print(df[['Review', 'Rating', 'Label', 'Label_Text']].head(10))

    print("\n✅ DONE. Review and label are now separated correctly.")

if __name__ == "__main__":
    fix_review_and_label()