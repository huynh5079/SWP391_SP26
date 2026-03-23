import pandas as pd
import ast
from collections import Counter

print("=" * 80)
print("DATA QUALITY CHECK")
print("=" * 80)

# Convert to CSV for easier loading (faster than Excel)
try:
    excel_file = 'data/cleaned_multi_label_dataset (2).xlsx'
    print(f"Trying to read {excel_file}...")
    df = pd.read_excel(excel_file, nrows=100, engine='openpyxl')  # Read only first 100 rows
    print(f"✓ Loaded {len(df)} rows")
except Exception as e:
    print(f"✗ Error reading Excel: {e}")
    print("Stopping data check.")
    exit(1)

print(f"\nColumns: {df.columns.tolist()}")
print(f"\nData types:\n{df.dtypes}")

# Parse labels
def parse_labels(label_str):
    try:
        return ast.literal_eval(str(label_str))
    except:
        return []

df['parsed_labels'] = df['labels_list'].apply(parse_labels)

print(f"\n--- SAMPLE ANALYSIS (First 20 rows) ---")
print(f"\nTotal rows checked: {len(df)}")

pos_count = Counter()
neutral_count = Counter()
neg_count = Counter()

for idx, row in df.iterrows():
    text = str(row['Cleaned_Review'])[:60]
    labels = row['parsed_labels']
    
    if idx < 20:
        print(f"\n{idx+1}. Text: \"{text}...\"")
        print(f"   Labels: {labels}")
    
    # Count sentiment patterns
    for label in labels:
        if 'Positive' in label:
            pos_count[label] += 1
        elif 'Negative' in label:
            neg_count[label] += 1
        elif 'Neutral' in label:
            neutral_count[label] += 1

print(f"\n--- LABEL DISTRIBUTION SUMMARY ---")
print(f"\nPositive labels found: {sum(pos_count.values())} total")
for label, count in sorted(pos_count.items(), key=lambda x: x[1], reverse=True)[:5]:
    print(f"  {label}: {count}")

print(f"\nNegative labels found: {sum(neg_count.values())} total")
for label, count in sorted(neg_count.items(), key=lambda x: x[1], reverse=True)[:5]:
    print(f"  {label}: {count}")

print(f"\nNeutral labels found: {sum(neutral_count.values())} total")
for label, count in sorted(neutral_count.items(), key=lambda x: x[1], reverse=True)[:5]:
    print(f"  {label}: {count}")

print("\n--- RECOMMENDATION ---")
total_pos = sum(pos_count.values())
total_neg = sum(neg_count.values())
total_neutral = sum(neutral_count.values())

print(f"\nSentiment balance:")
print(f"  Positive: {total_pos} ({100*total_pos/(total_pos+total_neg+total_neutral):.1f}%)")
print(f"  Negative: {total_neg} ({100*total_neg/(total_pos+total_neg+total_neutral):.1f}%)")
print(f"  Neutral: {total_neutral} ({100*total_neutral/(total_pos+total_neg+total_neutral):.1f}%)")

if total_pos > total_neg * 2:
    print("\n⚠️  WARNING: Too many Positive samples! Model biased toward Positive.")
elif total_neg > total_pos * 2:
    print("\n⚠️  WARNING: Too many Negative samples! Model biased toward Negative.")
else:
    print("\n✓ Good balance between sentiments")
