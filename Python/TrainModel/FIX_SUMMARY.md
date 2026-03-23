# Tóm Tắt Vấn Đề và Giải Pháp - Model Deep Learning

## 🚨 Vấn Đề Phát Hiện

### 1. **Data Imbalance (Dữ liệu Mất Cân Bằng)**
- **Positive labels**: 75% (quá nhiều)
- **Negative labels**: 2.5% (quá ít - chỉ 1 mẫu!)
- **Neutral labels**: 22.5%

**Hậu quả**: Model chỉ học dự đoán Positive, hoàn toàn bỏ qua Negative!

### 2. **Threshold Quá Cao (0.5)**
- Điều này khiến nhiều predictions nhỏ không được nhận diện
- Giảm xuống 0.3 sẽ giúp bắt được predictions yếu hơn

### 3. **Hyperparameters Không Tối Ưu**
- Learning rate 2e-5 → tăng lên 5e-5 
- Epochs 3 → tăng lên 5 epochs

## ✅ Giải Pháp Áp Dụng

### 1. **Cân Bằng Dữ Liệu (Oversampling)**
```python
# Phát hiện mẫu với Negative
# Tăng số lượng mẫu Negative bằng Random Sampling
# Oversample factor: 3-5x
```
- Mục đích: Cân bằng tỷ lệ Positive vs Negative
- Phương pháp: Random oversampling negative samples

### 2. **Điều Chỉnh Threshold**
```
Cũ: 0.5
Mới: 0.3
```
- Ngưỡng thấp hơn = Bắt được predictions yếu hơn

### 3. **Tối Ưu Hyperparameters**
```
Learning Rate: 2e-5 → 5e-5
Epochs: 3 → 5
Warmup Steps: +500 (ổn định training)
Seed: RANDOM_STATE (reproducibility)
```

### 4. **Tăng Cường Training**
- Thêm warmup steps (500)
- Thêm gradient accumulation steps
- Thêm seed control

## 📊 Kết Quả Dự Kiến

| Metric | Trước | Sau |
|--------|-------|-----|
| Nhận diện Negative | ❌ Không (0%) | ✅ Có (10-50%) |
| Accuracy trên positive | 90% | 75-80% (tradeoff) |
| Overall F1-score | Thấp | Cao hơn (balanced) |

## 🔄 Cách Chạy Training Cải Tiến

```bash
# Chạy file training cầu cân bằng
cd Python/TrainModel
python Deep-Learning.py
```

## ⚠️ Ghi Chú Quan Trọng

1. **Training sẽ mất thời gian hơn**: Vì dữ liệu lớn hơn + 5 epochs
2. **Model sẽ kém hơn trên positive nhưng tốt hơn trên negative**: Đây là tradeoff
3. **Cần xem xét thêm dữ liệu Negative thực tế**: Nếu có thể, thu thập thêm reviews tiêu cực

## 🔮 Cải Thiện Tiếp Theo (Nếu Cần)

1. **Data Augmentation**: Tạo thêm Negative samples từ EasyDataAugmentation (EDA)
2. **Focal Loss**: Sử dụng Focal Loss thay vì Binary Cross Entropy
3. **Class Weights**: Cấu hình explicit class weights trong loss function
4. **Thêm dữ liệu tương tự**: Tìm kiếm thêm reviews tiêu cực từ dataset khác
5. **Fine-tune trên domain cụ thể**: Có thể fine-tune model trên text tiếng Việt tương tự
