import pandas as pd
import ast

df = pd.read_excel('data/cleaned_multi_label_dataset (2).xlsx')

print('=== DATASET ANALYSIS ===')
print(f'Total rows: {len(df)}')
print(f'Columns: {df.columns.tolist()}')
print(f'\nFirst 3 samples:')
for i in range(min(3, len(df))):
    text = df["Cleaned_Review"].iloc[i]
    labels = df["labels_list"].iloc[i]
    print(f'{i+1}. Text: {text[:80]}...')
    print(f'   Labels: {labels}')
    print()

# Parse labels
def parse_labels(label_str):
    try:
        return ast.literal_eval(label_str)
    except:
        return []

df['parsed_labels'] = df['labels_list'].apply(parse_labels)

# Count unique labels
all_labels = set()
for labels in df['parsed_labels']:
    all_labels.update(labels)

print(f'Total unique labels: {len(all_labels)}')
print(f'Labels: {sorted(all_labels)}')

# Count label frequency
label_count = {}
for labels in df['parsed_labels']:
    for label in labels:
        label_count[label] = label_count.get(label, 0) + 1

print(f'\nLabel distribution:')
for label in sorted(label_count.keys()):
    print(f'  {label}: {label_count[label]} samples')
