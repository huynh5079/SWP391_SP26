// Chatbot Manager - xử lý logic tương tác chatbot
class ChatbotManager {
    constructor() {
        this.apiUrl = '/api/v1/chatbot';
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
        this.checkHealthStatus();
    }

    attachEventListeners() {
        // Mở/đóng chatbot
        this.chatbotToggle?.addEventListener('click', () => this.togglePanel());
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
                    topK: 5  // Will be mapped to TopK by API
                })
            });

            if (!response.ok) {
                const errorData = await response.json();
                throw new Error(errorData.message || 'Lỗi không xác định');
            }

            const data = await response.json();

            if (data.success) {
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

    addMessageToLog(content, role, sources = null) {
        const messageDiv = document.createElement('div');
        messageDiv.className = `chatbot-msg ${role}`;

        if (role === 'user') {
            messageDiv.innerHTML = `
                <div class="msg-author">Bạn</div>
                <div class="msg-text">${this.escapeHtml(content)}</div>
            `;
        } else {
            let sourcesHtml = '';
            if (sources && sources.length > 0) {
                sourcesHtml = `
                    <div class="msg-sources">
                        <small><strong>Nguồn:</strong></small>
                        <ul>
                            ${sources.map(s => {
                        const score = (s.score * 100).toFixed(1);
                        const title = s.meta?.title || s.meta?.source || 'Không rõ';
                        return `<li><small>${this.escapeHtml(title)} (${score}%)</small></li>`;
                    }).join('')}
                        </ul>
                    </div>
                `;
            }

            messageDiv.innerHTML = `
                <div class="msg-author">Chatbot AEMS</div>
                <div class="msg-text">${this.escapeHtml(content)}</div>
                ${sourcesHtml}
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
