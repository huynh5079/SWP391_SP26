// Chatbot Manager - xử lý logic tương tác chatbot
class ChatbotManager {
    constructor() {
        this.apiUrl = '/api/v1/chatbot';
        this.storageKey = 'aems_chatbot_session_id';
        this.sessionId = sessionStorage.getItem(this.storageKey) || localStorage.getItem(this.storageKey) || null;
        if (this.sessionId) {
            sessionStorage.setItem(this.storageKey, this.sessionId);
            localStorage.removeItem(this.storageKey);
        }
        this.isLoading = false;
        this.conversationHistory = [];
        this.init();
    }

    init() {
        this.chatbotToggle = document.getElementById('chatbotToggle');
        this.chatbotClose = document.getElementById('chatbotClose');
        this.chatbotPanel = document.getElementById('chatbotPanel');
        this.chatbotSend = document.getElementById('chatbotSend');
        this.chatbotInput = document.getElementById('chatbotInput');
        this.chatbotLog = document.getElementById('chatbotLog');
        this.chatbotStartBtn = document.getElementById('chatbotStartBtn');

        if (!this.chatbotToggle || !this.chatbotPanel) {
            console.warn('Chatbot elements not found');
            return;
        }

        // Ensure input is always editable on startup.
        if (this.chatbotInput) {
            this.chatbotInput.disabled = false;
        }

        this.attachEventListeners();
        this.loadConversationHistory();
        this.checkHealthStatus();
    }

    attachEventListeners() {
        // Mở/đóng chatbot - Support both the default FAB and any other triggers
        this.chatbotToggle?.addEventListener('click', (e) => {
            e.preventDefault();
            this.togglePanel();
        });

        // Add listeners for any elements with data-chatbot-trigger attribute
        document.querySelectorAll('[data-chatbot-trigger], #organizerChatbotToggle').forEach(trigger => {
            trigger.addEventListener('click', (e) => {
                e.preventDefault();
                this.togglePanel();
            });
        });

        this.chatbotClose?.addEventListener('click', () => this.closePanel());
        this.chatbotStartBtn?.addEventListener('click', () => this.focusInput());

        // Gửi tin nhắn
        this.chatbotSend?.addEventListener('click', () => this.sendMessage());
        this.chatbotInput?.addEventListener('keypress', (e) => {
            if (e.key === 'Enter' && !e.shiftKey) {
                e.preventDefault();
                this.sendMessage();
            }
        });
    }

    togglePanel() {
        const isHidden = this.chatbotPanel.getAttribute('aria-hidden') === 'true';
        if (isHidden) {
            this.openPanel();
        } else {
            this.closePanel();
        }
    }

    openPanel() {
        this.chatbotPanel.setAttribute('aria-hidden', 'false');
        this.chatbotPanel.classList.add('open');
        this.focusInput();
    }

    closePanel() {
        this.chatbotPanel.setAttribute('aria-hidden', 'true');
        this.chatbotPanel.classList.remove('open');
    }

    focusInput() {
        this.chatbotInput.focus();
    }

    async sendMessage() {
        const message = this.chatbotInput.value.trim();

        if (!message) {
            return;
        }

        if (this.isLoading) {
            console.warn('Đang xử lý câu hỏi trước đó');
            return;
        }

        // Hiển thị tin nhắn người dùng
        this.addMessageToLog(message, 'user');
        this.chatbotInput.value = '';
        this.conversationHistory.push({ role: 'user', message });

        // Gửi tới API
        await this.askChatbot(message);
    }

    async askChatbot(question) {
        this.isLoading = true;
        this.setLoadingState(true);

        try {
            const response = await fetch(`${this.apiUrl}/ask`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    question: question,
                    topK: 5,  // Will be mapped to TopK by API
                    sessionId: this.sessionId
                })
            });

            if (!response.ok) {
                const errorData = await response.json();
                throw new Error(errorData.message || 'Lỗi không xác định');
            }

            const data = await response.json();

            if (data.success) {
                const returnedSessionId = data.data.sessionId || data.data.SessionId;
                if (returnedSessionId && returnedSessionId !== this.sessionId) {
                    this.sessionId = returnedSessionId;
                    sessionStorage.setItem(this.storageKey, returnedSessionId);
                }

                // Hiển thị câu trả lời
                const answer = data.data.answer || 'Không có câu trả lời';
                this.addMessageToLog(answer, 'bot', data.data.sources);
                this.conversationHistory.push({ role: 'bot', message: answer });
            } else {
                this.addMessageToLog(`Lỗi: ${data.message}`, 'bot');
            }
        } catch (error) {
            console.error('Chatbot error:', error);
            this.addMessageToLog(
                `Không thể gửi câu hỏi: ${error.message}. Vui lòng kiểm tra kết nối tới RAG server.`,
                'bot'
            );
        } finally {
            this.isLoading = false;
            this.setLoadingState(false);
            this.focusInput();
        }
    }

    async loadConversationHistory() {
        try {
            const query = this.sessionId ? `?sessionId=${encodeURIComponent(this.sessionId)}&limit=100` : '?limit=100';
            const response = await fetch(`${this.apiUrl}/history${query}`);

            if (!response.ok) {
                return;
            }

            const data = await response.json();
            if (!data?.success || !data?.data) {
                return;
            }

            const historySessionId = data.data.sessionId || data.data.SessionId;
            if (historySessionId) {
                this.sessionId = historySessionId;
                sessionStorage.setItem(this.storageKey, historySessionId);
            }

            const messages = data.data.messages || data.data.Messages || [];
            if (!Array.isArray(messages) || messages.length === 0) {
                return;
            }

            this.chatbotLog.innerHTML = '';
            this.conversationHistory = [];

            messages.forEach(m => {
                const sender = (m.sender || m.Sender || '').toLowerCase();
                const content = m.content || m.Content || '';
                if (!content) return;

                if (sender === 'user') {
                    this.addMessageToLog(content, 'user');
                    this.conversationHistory.push({ role: 'user', message: content });
                } else {
                    this.addMessageToLog(content, 'bot');
                    this.conversationHistory.push({ role: 'bot', message: content });
                }
            });
        } catch (error) {
            console.warn('Không thể tải lịch sử chatbot:', error);
        }
    }

    addMessageToLog(content, role, sources = null) {
        const messageDiv = document.createElement('div');
        messageDiv.className = `chatbot-msg ${role}`;
        const time = new Date().toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' });

        if (role === 'user') {
            messageDiv.innerHTML = `
                <div class="msg-text">${this.escapeHtml(content)}</div>
                <div class="msg-time">${time}</div>
            `;
        } else {
            messageDiv.innerHTML = `
                <div class="msg-text">${this.escapeHtml(content)}</div>
                <div class="msg-time">${time}</div>
            `;
        }

        this.chatbotLog.appendChild(messageDiv);
        this.chatbotLog.scrollTop = this.chatbotLog.scrollHeight;
    }

    setLoadingState(isLoading) {
        if (!this.chatbotSend) {
            return;
        }

        // Keep input editable even while waiting for API response.
        this.chatbotSend.disabled = isLoading;

        if (isLoading) {
            this.chatbotSend.classList.add('loading');
            this.chatbotSend.innerHTML = '<i class="bi bi-hourglass-split"></i>';
        } else {
            this.chatbotSend.classList.remove('loading');
            this.chatbotSend.innerHTML = '<i class="bi bi-send"></i>';
        }
    }

    async checkHealthStatus() {
        try {
            const response = await fetch(`${this.apiUrl}/health`);
            if (!response.ok) {
                console.warn('RAG API không sẵn sàng');
            }
        } catch (error) {
            console.warn('Không thể kết nối tới RAG API:', error.message);
        }
    }

    escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }
}

// Khởi tạo khi DOM sẵn sàng
document.addEventListener('DOMContentLoaded', () => {
    window.chatbotManager = new ChatbotManager();
});
