import csv
import os
import re
import pandas as pd
from tqdm import tqdm

def clean_broken_csv():
    input_file = r'D:\PROJECT_SWP_SP26\swp391_sp26-develop\Python\Data\coursera_label_processed.csv'
    output_file = r'D:\PROJECT_SWP_SP26\swp391_sp26-develop\Python\Data\coursera_label_cleaned.csv'
    bad_rows_file = r'D:\PROJECT_SWP_SP26\swp391_sp26-develop\Python\Data\coursera_label_bad_rows.csv'

    if not os.path.exists(input_file):
        print(f"❌ File not found: {input_file}")
        return

    print(f"📂 Reading raw file: {input_file}")

    # ===== 1. Đọc raw text =====
    with open(input_file, 'r', encoding='utf-8-sig', errors='replace') as f:
        raw_lines = f.readlines()

    print(f"📄 Total raw lines: {len(raw_lines)}")

    # ===== 2. Header chuẩn mong muốn =====
    expected_columns = [
        'Id', 'Review', 'Label',
        'Technical', 'Content', 'Instructor', 'General', 'Assessment'
    ]
    expected_col_count = len(expected_columns)

    # ===== 3. Hàm kiểm tra row hợp lệ =====
    def is_valid_id(val):
        return str(val).strip().isdigit()

    def normalize_label(val):
        if pd.isna(val):
            return None
        v = str(val).strip().lower()
        mapping = {
            'positive': 'positive',
            'negative': 'negative',
            'normal': 'normal',
            'neutral': 'normal',
            'notmentioned': 'notmentioned',
            'not_mentioned': 'notmentioned',
            'mentioned': 'mentioned'
        }
        return mapping.get(v, None)

    # ===== 4. Ghép các dòng bị vỡ =====
    repaired_rows = []
    bad_rows = []

    current_buffer = ""

    print("🛠 Repairing broken rows...")

    for line in tqdm(raw_lines):
        line = line.rstrip('\n')

        # Bỏ dòng trống
        if not line.strip():
            continue

        # Ghép buffer
        if current_buffer:
            current_buffer += "\n" + line
        else:
            current_buffer = line

        # Thử parse buffer như 1 CSV row
        try:
            parsed = next(csv.reader([current_buffer]))
        except Exception:
            continue

        # Nếu cột còn thiếu, có thể row chưa kết thúc (review bị xuống dòng)
        if len(parsed) < expected_col_count:
            continue

        # Nếu cột nhiều quá thì vẫn có thể cứu bằng cách cắt merge
        repaired_rows.append(parsed)
        current_buffer = ""

    # Nếu cuối file còn buffer chưa xử lý
    if current_buffer.strip():
        try:
            parsed = next(csv.reader([current_buffer]))
            repaired_rows.append(parsed)
        except:
            bad_rows.append([current_buffer, "Unparsable leftover buffer"])

    print(f"✅ Parsed candidate rows: {len(repaired_rows)}")

    # ===== 5. Chuẩn hóa rows =====
    clean_data = []

    print("🧹 Normalizing rows...")

    for row in tqdm(repaired_rows):
        # Nếu row là header thì bỏ
        row_join = ",".join(row).lower()
        if "id" in row_join and "review" in row_join and "technical" in row_join:
            continue

        # Nếu ít hơn số cột chuẩn => row lỗi
        if len(row) < expected_col_count:
            bad_rows.append([str(row), "Too few columns"])
            continue

        # Nếu nhiều hơn số cột chuẩn:
        # Giả định cột Review có dấu phẩy nên bị tách quá nhiều
        if len(row) > expected_col_count:
            # Cấu trúc mong muốn:
            # Id | Review | Label | Technical | Content | Instructor | General | Assessment
            # => giữ cột đầu là Id, 6 cột cuối là label, phần giữa gộp lại thành Review
            fixed = [
                row[0],                                # Id
                ",".join(row[1:len(row)-6]),          # Review
                row[len(row)-6],                      # Label
                row[len(row)-5],                      # Technical
                row[len(row)-4],                      # Content
                row[len(row)-3],                      # Instructor
                row[len(row)-2],                      # General
                row[len(row)-1],                      # Assessment
            ]
            row = fixed

        if len(row) != expected_col_count:
            bad_rows.append([str(row), "Column count mismatch after fixing"])
            continue

        row_dict = dict(zip(expected_columns, row))

        # ===== 6. Validate =====
        # ID phải là số
        if not is_valid_id(row_dict['Id']):
            bad_rows.append([str(row), "Invalid Id"])
            continue

        # Review phải có nội dung
        review = str(row_dict['Review']).strip()
        if not review or review.lower() == 'nan':
            bad_rows.append([str(row), "Empty Review"])
            continue

        # Label tổng thể (nếu là rating số hoặc text thì giữ nguyên)
        overall_label = str(row_dict['Label']).strip()
        if not overall_label or overall_label.lower() == 'nan':
            bad_rows.append([str(row), "Empty Label"])
            continue

        # Chuẩn hóa các cột aspect
        aspect_ok = True
        for col in ['Technical', 'Content', 'Instructor', 'General', 'Assessment']:
            normalized = normalize_label(row_dict[col])
            if normalized is None:
                # Nếu rác thì set notmentioned thay vì bỏ cả row
                row_dict[col] = 'notmentioned'
            else:
                row_dict[col] = normalized

        # Chuẩn hóa review
        review = re.sub(r'\s+', ' ', review).strip()
        row_dict['Review'] = review

        clean_data.append(row_dict)

    print(f"✅ Clean usable rows: {len(clean_data)}")
    print(f"⚠ Bad rows: {len(bad_rows)}")

    # ===== 7. Xuất file sạch =====
    df_clean = pd.DataFrame(clean_data, columns=expected_columns)
    df_clean.drop_duplicates(subset=['Id', 'Review'], inplace=True)

    df_clean.to_csv(output_file, index=False, encoding='utf-8-sig')
    print(f"💾 Cleaned file saved to: {output_file}")

    # ===== 8. Xuất file lỗi =====
    if bad_rows:
        df_bad = pd.DataFrame(bad_rows, columns=['RawRow', 'Reason'])
        df_bad.to_csv(bad_rows_file, index=False, encoding='utf-8-sig')
        print(f"🗑 Bad rows saved to: {bad_rows_file}")

    # ===== 9. Báo cáo nhanh =====
    print("\n📊 Summary:")
    print(df_clean.info())
    print("\n🔎 Sample:")
    print(df_clean.head(10))

if __name__ == "__main__":
    clean_broken_csv()