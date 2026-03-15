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
        this.chatbotReloadBtn = document.getElementById('chatbotReloadBtn');
        this.chatbotSessionBtn = document.getElementById('chatbotSessionBtn');
        this.chatbotSessionsPanel = document.getElementById('chatbotSessionsPanel');
        this.chatbotSessionsList = document.getElementById('chatbotSessionsList');

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
        // Mở/đóng chatbot
        this.chatbotToggle?.addEventListener('click', () => this.togglePanel());
        this.chatbotClose?.addEventListener('click', () => this.closePanel());
        this.chatbotReloadBtn?.addEventListener('click', () => this.confirmResetSession());
        this.chatbotSessionBtn?.addEventListener('click', () => this.toggleSessionPanel());

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

    confirmResetSession() {
        const ok = window.confirm('Bạn muốn reset phiên chat hiện tại? Nội dung đang mở sẽ được xóa khỏi khung chat, nhưng lịch sử vẫn được lưu.');
        if (ok) {
            this.startNewSession();
        }
    }

    openPanel() {
        this.chatbotPanel.setAttribute('aria-hidden', 'false');
        this.chatbotPanel.classList.add('open');
        this.loadSessions();
        this.focusInput();
    }

    closePanel() {
        this.chatbotPanel.setAttribute('aria-hidden', 'true');
        this.chatbotPanel.classList.remove('open');
        this.chatbotSessionsPanel?.classList.remove('open');
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

            this.loadSessions();
        } catch (error) {
            console.warn('Không thể tải lịch sử chatbot:', error);
        }
    }

    async loadSessions() {
        if (!this.chatbotSessionsList) {
            return;
        }

        try {
            const response = await fetch(`${this.apiUrl}/sessions?limit=20`);
            if (!response.ok) {
                return;
            }

            const data = await response.json();
            if (!data?.success || !Array.isArray(data?.data)) {
                return;
            }

            this.chatbotSessionsList.innerHTML = '';

            data.data.forEach(s => {
                const id = s.sessionId || s.SessionId;
                const title = s.title || s.Title || 'Cuộc trò chuyện';
                const timeRaw = s.lastMessageAt || s.LastMessageAt;

                const btn = document.createElement('button');
                btn.type = 'button';
                btn.className = `chatbot-session-item ${id === this.sessionId ? 'active' : ''}`;
                btn.innerHTML = `
                    <div class="chatbot-session-title">${this.escapeHtml(title)}</div>
                    <div class="chatbot-session-time">${timeRaw ? this.formatDateTime(timeRaw) : ''}</div>
                `;
                btn.addEventListener('click', () => this.selectSession(id));
                this.chatbotSessionsList.appendChild(btn);
            });
        } catch (error) {
            console.warn('Không thể tải danh sách phiên chat:', error);
        }
    }

    async startNewSession() {
        try {
            const response = await fetch(`${this.apiUrl}/sessions/new`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    currentSessionId: this.sessionId
                })
            });
            if (!response.ok) {
                throw new Error('Không tạo được phiên chat mới');
            }

            const data = await response.json();
            const newSessionId = data?.data?.sessionId || data?.data?.SessionId;
            if (!newSessionId) {
                throw new Error('Không nhận được session mới');
            }

            this.sessionId = newSessionId;
            sessionStorage.setItem(this.storageKey, this.sessionId);
            this.chatbotSessionsPanel?.classList.remove('open');
            this.chatbotLog.innerHTML = '';
            this.conversationHistory = [];
            this.addMessageToLog('Sẵn sàng hỗ trợ bạn tra cứu lịch sự kiện, vé và thông tin chung.', 'bot');
            this.loadSessions();
            this.focusInput();
        } catch (error) {
            console.warn('Không thể tạo phiên chat mới:', error);
        }
    }

    toggleSessionPanel() {
        if (!this.chatbotSessionsPanel) {
            return;
        }
        this.chatbotSessionsPanel.classList.toggle('open');
        if (this.chatbotSessionsPanel.classList.contains('open')) {
            this.loadSessions();
        }
    }

    async selectSession(sessionId) {
        if (!sessionId) {
            return;
        }

        this.sessionId = sessionId;
        sessionStorage.setItem(this.storageKey, sessionId);
        await this.loadConversationHistory();
    }

    formatDateTime(value) {
        const d = new Date(value);
        if (Number.isNaN(d.getTime())) {
            return '';
        }
        return d.toLocaleString('vi-VN', {
            day: '2-digit',
            month: '2-digit',
            hour: '2-digit',
            minute: '2-digit'
        });
    }

    addMessageToLog(content, role, sources = null) {
        const messageDiv = document.createElement('div');
        messageDiv.className = `chatbot-msg ${role}`;

        if (role === 'user') {
            messageDiv.innerHTML = `
                <div class="msg-author">Bạn</div>
                <div class="msg-text">${this.escapeHtml(content)}</div>
            `;
        } else {
            // Sources display hidden per user request
            // let sourcesHtml = '';
            // if (sources && sources.length > 0) {
            //     sourcesHtml = `
            //         <div class="msg-sources">
            //             <small><strong>Nguồn:</strong></small>
            //             <ul>
            //                 ${sources.map(s => {
            //             const score = (s.score * 100).toFixed(1);
            //             const title = s.meta?.title || s.meta?.source || 'Không rõ';
            //             return `<li><small>${this.escapeHtml(title)} (${score}%)</small></li>`;
            //         }).join('')}
            //             </ul>
            //         </div>
            //     `;
            // }

            messageDiv.innerHTML = `
                <div class="msg-author">Chatbot AEMS</div>
                <div class="msg-text">${this.escapeHtml(content)}</div>
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
