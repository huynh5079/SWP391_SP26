# AEMS Smart Event Chatbot RAG API

RAG server thông minh này đọc dữ liệu trực tiếp từ database (`Event`, `EventAgenda`, `EventTeam`, `Feedback`, `SystemErrorLog`) và hoạt động như một chatbot tư vấn sự kiện chuyên nghiệp với khả năng tự động cập nhật dữ liệu.

## ✨ Tính năng chính

- 🤖 **Chatbot tư vấn thông minh** - Gợi ý sự kiện dựa trên chất lượng và đánh giá
- 💬 **Bộ nhớ hội thoại** - Nhớ ngữ cảnh cuộc trò chuyện
- 🔄 **Tự động cập nhật** - Làm mới dữ liệu mỗi 1 phút (có thể tùy chỉnh)
- 🔐 **Kiểm soát quyền truy cập** - Admin/Staff/User có quyền xem khác nhau
- 📊 **Dữ liệu phong phú** - Sự kiện, chương trình, thành viên, feedback, logs

## 1) Cài đặt

```powershell
cd Python
python -m venv .venv
.\.venv\Scripts\Activate.ps1
pip install -r requirements.txt
```

## 2) Cấu hình

1. Copy `.env.example` thành `.env`
2. Cập nhật tối thiểu:

```env
GROQ_API_KEY=your_real_key
RAG_AUTO_RELOAD_SECONDS=60  # Tự động reload mỗi 60 giây (1 phút)
```

`AEMS_DB_CONNECTION_STRING` có thể để trống nếu muốn tự lấy từ `AEMS_Solution/appsettings.Development.json`.

### Cấu hình Auto-Reload

- **RAG_AUTO_RELOAD_SECONDS=60** - Tự động reload dữ liệu mỗi 60 giây
- **RAG_AUTO_RELOAD_SECONDS=0** - Tắt auto-reload (chỉ manual reload)
- **Mặc định**: 60 giây nếu không cấu hình

## 3) Chạy server

### Cách 1: Chạy trực tiếp (Recommended)

**Windows - PowerShell**
```powershell
cd Python
python rag_api_server.py
```

**Windows - Command Prompt**
```cmd
cd Python
python rag_api_server.py
```

**Windows - Sử dụng script batch**
```cmd
cd Python
start_rag_server.bat
```

**Linux/Mac**
```bash
cd Python
python rag_api_server.py
# hoặc
chmod +x start_rag_server.sh
./start_rag_server.sh
```

### Cách 2: Chạy trong virtual environment

```powershell
cd Python
.\.venv\Scripts\Activate.ps1
python rag_api_server.py
```

Server chạy tại: `http://localhost:8000`

**Swagger UI (Test API)**: `http://localhost:8000/docs`
**Health Check**: `http://localhost:8000/health`

### Expected Startup Output

Khi server chạy thành công, bạn sẽ thấy:
```
[RAG] Database service initialized
[RAG] Starting up...
[RAG] Auto-reload task started
INFO:     Application startup complete.
INFO:     Uvicorn running on http://0.0.0.0:8000 (Press CTRL+C to quit)
```

## Troubleshooting

### Lỗi: "ModuleNotFoundError: No module named 'groq'"
**Giải pháp**: Cài đặt requirements
```powershell
pip install -r requirements.txt
```

### Lỗi: "GROQ_API_KEY không đặt"
**Giải pháp**: 
1. Kiểm tra file `.env` có tồn tại
2. Kiểm tra `GROQ_API_KEY` đã được cấu hình
3. Copy từ `.env.example` nếu cần

### Lỗi: "Cannot connect to database"
**Giải pháp**: 
1. Kiểm tra SQL Server đang chạy
2. Kiểm tra connection string trong `.env` hoặc `appsettings.Development.json`

### Port 8000 đã được sử dụng
**Giải pháp**: 
1. Tắt process đang dùng port 8000
2. Hoặc thay đổi port trong code (mặc định là 8000)

## 4) API chính

### Quản lý hệ thống
- `GET /health` - Kiểm tra trạng thái server (bao gồm auto-reload status)
- `GET /stats` - Thống kê knowledge base
- `POST /reload` - Manual reload dữ liệu ngay lập tức

### Auto-Reload (Mới! 🔄)
- `GET /auto-reload/status` - Kiểm tra trạng thái auto-reload
- `POST /auto-reload/enable` - Bật auto-reload
- `POST /auto-reload/disable` - Tắt auto-reload

### Chatbot
- `POST /ask` - Hỏi chatbot
- `GET /conversation/{session_id}` - Xem lịch sử hội thoại
- `POST /conversation/{session_id}/clear` - Xóa lịch sử hội thoại

### Request mẫu (POST /ask):

```json
{
  "question": "Có sự kiện nào đáng tham gia không?",
  "top_k": 5,
  "role": "user",
  "session_id": "optional-session-id"
}
```

**Response:**
```json
{
  "session_id": "generated-or-reused",
  "question": "Có sự kiện nào...",
  "answer": "**Tóm tắt nhanh**\nDựa vào dữ liệu...",
  "sources": [...],
  "error": null
}
```

## 5) Tính năng Auto-Reload

Server tự động làm mới dữ liệu từ database theo chu kỳ cấu hình:

✅ **Lợi ích:**
- Không cần restart server khi có sự kiện mới
- Chatbot luôn có thông tin mới nhất
- Tự động phát hiện sự kiện được publish

🎯 **Khi nào dữ liệu được cập nhật?**
- Tự động mỗi 60 giây (hoặc theo cấu hình)
- Khi gọi `/reload` manual
- Khi restart server

📊 **Kiểm tra trạng thái:**
```bash
curl http://localhost:8000/auto-reload/status
```

## 6) Test trên Browser

Mở Swagger UI: `http://localhost:8000/docs`

1. Click vào **POST /ask**
2. Click **"Try it out"**
3. Nhập câu hỏi và click **Execute**

Ví dụ câu hỏi:
- "Có sự kiện nào hay không?"
- "Sự kiện nào có đánh giá tốt nhất?"
- "Cho tôi biết chương trình chi tiết của sự kiện về AI"
- "Tôi muốn tham gia sự kiện online"

## 7) Tích hợp .NET

`RagApiClient` hiện tại đã gọi đúng endpoint:
- `/health`
- `/ask`
- `/stats`

Bạn chỉ cần đảm bảo server Python đang chạy trước khi gọi từ .NET.

## 8) Lưu ý production

- ✅ Cần cài `ODBC Driver 18 for SQL Server` (hoặc 17) trên máy chạy RAG
- ✅ Không commit file `.env` chứa API key lên git
- ✅ Auto-reload đã được tích hợp sẵn, không cần cron job
- ✅ Điều chỉnh `RAG_AUTO_RELOAD_SECONDS` theo nhu cầu (khuyến nghị: 60-300 giây)
- ✅ Server log sẽ hiển thị thời điểm reload dữ liệu
