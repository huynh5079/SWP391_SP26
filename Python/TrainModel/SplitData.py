import pandas as pd
from sklearn.model_selection import train_test_split
import os

def split_dataset():
    input_file = r'D:\PROJECT_SWP_SP26\swp391_sp26-develop\Python\Data\coursera_label_fixed.csv'

    output_dir = r'D:\PROJECT_SWP_SP26\swp391_sp26-develop\Python\Data\spiltdatatotrain\split'
    os.makedirs(output_dir, exist_ok=True)

    train_file = os.path.join(output_dir, 'train.csv')
    val_file = os.path.join(output_dir, 'val.csv')
    test_file = os.path.join(output_dir, 'test.csv')

    df = pd.read_csv(input_file)

    print(f"📂 Loaded {len(df)} rows")

    # ===== 1. Chỉ giữ cột cần cho overall sentiment =====
    df_train = df[['Review', 'Label', 'Label_Text']].copy()

    # ===== 2. Chia train / temp =====
    train_df, temp_df = train_test_split(
        df_train,
        test_size=0.2,
        stratify=df_train['Label'],
        random_state=42
    )

    # ===== 3. Chia temp thành val / test =====
    val_df, test_df = train_test_split(
        temp_df,
        test_size=0.5,
        stratify=temp_df['Label'],
        random_state=42
    )

    # ===== 4. Save =====
    train_df.to_csv(train_file, index=False, encoding='utf-8-sig')
    val_df.to_csv(val_file, index=False, encoding='utf-8-sig')
    test_df.to_csv(test_file, index=False, encoding='utf-8-sig')

    print(f"✅ Train: {len(train_df)}")
    print(f"✅ Val:   {len(val_df)}")
    print(f"✅ Test:  {len(test_df)}")

    print("\n📊 Label distribution:")
    print("\nTrain:")
    print(train_df['Label_Text'].value_counts(normalize=True))

    print("\nVal:")
    print(val_df['Label_Text'].value_counts(normalize=True))

    print("\nTest:")
    print(test_df['Label_Text'].value_counts(normalize=True))

    print("\n💾 Files saved to:", output_dir)

if __name__ == "__main__":
    split_dataset()