class AemsMessengerLayout {
    constructor(root) {
        this.root = root;
        this.panel = root.querySelector('#messengerPanel');
        this.toggle = root.querySelector('#messengerToggle');
        this.close = root.querySelector('#messengerClose');
        this.search = root.querySelector('#messengerSearch');
        this.contactList = root.querySelector('#messengerContactList');
        this.emptyState = root.querySelector('#messengerEmptyState');
        this.thread = root.querySelector('#messengerThread');
        this.messages = root.querySelector('#messengerMessages');
        this.input = root.querySelector('#messengerInput');
        this.send = root.querySelector('#messengerSend');
        this.activeName = root.querySelector('#messengerActiveName');
        this.activeStatus = root.querySelector('#messengerActiveStatus');
        this.activeAvatar = root.querySelector('#messengerActiveAvatar');
        this.fabBadge = root.querySelector('#messengerFabBadge');
        this.currentUser = root.dataset.currentUser || 'Bạn';
        this.currentUserId = root.dataset.currentUserId || '';
        this.currentRole = root.dataset.currentRole || 'User';
        this.contactsUrl = root.dataset.contactsUrl || '/chat/contacts';
        this.conversationUrl = root.dataset.conversationUrl || '/chat/conversation';
        this.chatHubUrl = root.dataset.chatHubUrl || '/hub/v1/chat';
        this.contacts = [];
        this.conversations = {};
        this.activeContactId = null;
        this.connection = null;
        this.filterMode = 'history'; // default: only show chats with history
    }

    async init() {
        if (!this.panel || !this.toggle) {
            return;
        }

        this.bindEvents();
        await this.startConnection();
        await this.loadContacts();
        this.updateFabBadge();
    }

    bindEvents() {
        this.toggle.addEventListener('click', async (event) => {
            event.stopPropagation();
            await this.togglePanel();
        });

        this.close?.addEventListener('click', (event) => {
            event.stopPropagation();
            this.closePanel();
        });

        this.search?.addEventListener('input', () => this.renderContacts());

        this.send?.addEventListener('click', async (event) => {
            event.stopPropagation();
            await this.sendMessage();
        });

        this.input?.addEventListener('keydown', async (event) => {
            if (event.key === 'Enter') {
                event.preventDefault();
                await this.sendMessage();
            }
        });

        this.messages?.addEventListener('click', async (event) => {
            const toggle = event.target.closest('.messenger-message-menu-toggle');
            if (toggle) {
                event.stopPropagation();
                const container = toggle.closest('.messenger-message-actions');
                const menu = container?.querySelector('.messenger-message-menu');
                this.closeMessageMenus(container);
                menu?.classList.toggle('open');
                return;
            }

            const recall = event.target.closest('[data-action="recall-message"]');
            if (recall) {
                event.stopPropagation();
                await this.recallMessage(recall.dataset.messageId);
            }
        });

        document.addEventListener('click', (event) => {
            if (!event.target.closest('.messenger-message-actions')) {
                this.closeMessageMenus();
            }

            if (!this.panel.classList.contains('open')) {
                return;
            }

            const eventPath = typeof event.composedPath === 'function' ? event.composedPath() : [];
            if (eventPath.includes(this.root) || this.root.contains(event.target) || event.target.closest('[data-open-chat-user]')) {
                return;
            }

            this.closePanel();
        });
    }

    async startConnection() {
        if (typeof signalR === 'undefined') {
            console.warn('SignalR chưa được tải.');
            return;
        }

        this.connection = new signalR.HubConnectionBuilder()
            .withUrl(this.chatHubUrl)
            .withAutomaticReconnect()
            .build();

        this.connection.on('ReceivePrivateMessage', async (message) => {
            await this.handleIncomingMessage(message);
        });

        this.connection.on('MessageRecalled', async (message) => {
            await this.handleRecalledMessage(message);
        });

        this.connection.on('ConversationRead', async (payload) => {
            this.handleConversationRead(payload);
        });

        try {
            await this.connection.start();
        } catch (error) {
            console.error('Không thể kết nối chat hub:', error);
        }
    }

    async togglePanel() {
        if (this.panel.classList.contains('open')) {
            this.closePanel();
            return;
        }

        await this.openPanel();
    }

    async openPanel() {
        console.log('Opening messenger panel...');
        this.panel.classList.add('open');
        this.panel.setAttribute('aria-hidden', 'false');

        if (this.contacts.length === 0) {
            await this.loadContacts();
        }

        if (!this.activeContactId && this.contacts.length > 0) {
            await this.selectContact(this.contacts[0].userId);
        }
    }

    async openChatWith(contactId) {
        console.log('openChatWith called for userId:', contactId);
        if (!contactId) {
            return;
        }

        await this.openPanel();
        console.log('Panel opened, checking status for contactId:', contactId);

        if (!this.contacts.some(x => String(x.userId).toLowerCase() === String(contactId).toLowerCase())) {
            await this.loadContacts(false);
        }

        if (!this.contacts.some(x => String(x.userId).toLowerCase() === String(contactId).toLowerCase())) {
            // User is allowed but has no chat history yet — fetch their info and inject as temp contact
            try {
                const res = await fetch(`${this.conversationUrl}?otherUserId=${encodeURIComponent(contactId)}`, {
                    headers: { 'X-Requested-With': 'XMLHttpRequest' }
                });

                if (res.ok) {
                    // Inject placeholder with lastMessageAt=now so it passes the history filter
                    const tempContact = {
                        userId: contactId,
                        fullName: 'Thành viên',
                        roleName: 'Student',
                        avatarUrl: null,
                        isOnline: false,
                        lastMessage: null,
                        lastMessageAt: new Date().toISOString(),
                        unreadCount: 0
                    };
                    this.contacts.unshift(tempContact);
                    this.conversations[contactId] = [];
                    this.renderContacts();

                    // Select conversation first so UI is ready
                    await this.selectContact(contactId);

                    // Auto-send a greeting to create a real chat session in DB
                    if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
                        try {
                            await this.connection.invoke('SendPrivateMessage', contactId, 'Xin chào! Chúng ta sẽ cùng làm việc trong team, liên hệ mình nếu cần nhé 👋');
                        } catch (err) {
                            console.warn('Auto-greeting failed:', err);
                        }
                    }

                    // Reload contacts so the real lastMessageAt from DB is used
                    await this.loadContacts(true);
                    return;
                } else if (res.status === 403) {
                    console.warn('openChatWith: permission denied for', contactId);
                    alert('Bạn không có quyền chat với người dùng này.');
                    return;
                } else {
                    console.warn('openChatWith: contact not found', contactId);
                    alert('Không thể mở chat với người dùng này.');
                    return;
                }
            } catch (err) {
                console.error('openChatWith probe failed:', err);
                return;
            }
        }

        await this.selectContact(contactId);
    }

    closePanel() {
        this.panel.classList.remove('open');
        this.panel.setAttribute('aria-hidden', 'true');
    }

    async loadContacts(preserveActive = true) {
        try {
            const response = await fetch(this.contactsUrl, {
                headers: {
                    'X-Requested-With': 'XMLHttpRequest'
                }
            });

            if (!response.ok) {
                throw new Error('Không tải được danh sách chat.');
            }

            const result = await response.json();
            this.contacts = Array.isArray(result.data) ? result.data : [];
            this.renderContacts();
            this.updateFabBadge();

            if (!preserveActive || !this.activeContactId) {
                return;
            }

            const activeStillExists = this.contacts.some(x => x.userId === this.activeContactId);
            if (!activeStillExists) {
                this.activeContactId = null;
                this.thread.classList.add('d-none');
                this.emptyState.classList.remove('d-none');
            }
        } catch (error) {
            console.error(error);
            this.contactList.innerHTML = '<div class="messenger-empty-list">Không tải được danh sách chat.</div>';
        }
    }

    renderContacts() {
        const keyword = (this.search?.value || '').trim().toLowerCase();
        let contacts = this.contacts
            .filter(contact => !keyword
                || contact.fullName.toLowerCase().includes(keyword)
                || contact.roleName.toLowerCase().includes(keyword));

        // Apply filter mode
        if (this.filterMode === 'history') {
            contacts = contacts.filter(c => c.lastMessageAt);
        }

        contacts = contacts.sort((a, b) => {
            const aTime = a.lastMessageAt || '';
            const bTime = b.lastMessageAt || '';
            return bTime.localeCompare(aTime) || a.fullName.localeCompare(b.fullName);
        });

        if (contacts.length === 0) {
            this.contactList.innerHTML = '<div class="messenger-empty-list">Không có người dùng phù hợp.</div>';
            return;
        }

        this.contactList.innerHTML = contacts.map(contact => {
            const preview = contact.lastMessage || `Bắt đầu chat với ${contact.fullName}`;
            const unread = contact.unreadCount || 0;
            const status = contact.isOnline ? 'Đang hoạt động' : 'Ngoại tuyến';
            return `
                <div class="messenger-contact ${contact.userId === this.activeContactId ? 'active' : ''}" data-contact-id="${contact.userId}">
                    <div class="messenger-avatar messenger-avatar-presence ${contact.isOnline ? 'online' : ''}" style="background:${this.getAccent(contact.roleName)}">${this.getInitials(contact.fullName)}</div>
                    <div class="messenger-contact-body">
                        <div class="messenger-contact-top">
                            <div class="messenger-contact-name">${this.escapeHtml(contact.fullName)}</div>
                            <div class="messenger-contact-time">${contact.lastMessageAt ? this.formatTime(contact.lastMessageAt) : ''}</div>
                        </div>
                        <div class="messenger-contact-bottom">
                            <div>
                                <div class="messenger-contact-role">${this.escapeHtml(contact.roleName)} • ${status}</div>
                                <div class="messenger-contact-preview">${this.escapeHtml(preview)}</div>
                            </div>
                            ${unread > 0 ? `<span class="messenger-unread">${unread}</span>` : ''}
                        </div>
                    </div>
                </div>
            `;
        }).join('');

        this.contactList.querySelectorAll('[data-contact-id]').forEach(item => {
            item.addEventListener('click', async (event) => {
                event.stopPropagation();
                await this.selectContact(item.dataset.contactId);
            });
        });
    }

    async selectContact(contactId) {
        this.activeContactId = contactId;
        const contact = this.contacts.find(x => String(x.userId).toLowerCase() === String(contactId).toLowerCase());
        if (!contact) {
            return;
        }

        await this.loadConversation(contactId);
        contact.unreadCount = 0;

        this.activeName.textContent = contact.fullName;
        this.activeStatus.textContent = `${contact.roleName} • ${contact.isOnline ? 'Đang hoạt động' : 'Ngoại tuyến'}`;
        this.activeAvatar.textContent = this.getInitials(contact.fullName);
        this.activeAvatar.style.background = this.getAccent(contact.roleName);
        this.activeAvatar.classList.toggle('online', !!contact.isOnline);
        this.activeAvatar.classList.add('messenger-avatar-presence');
        this.emptyState.classList.add('d-none');
        this.thread.classList.remove('d-none');
        this.input.disabled = false;
        this.input.focus();

        this.renderMessages();
        this.renderContacts();
        this.updateFabBadge();

        if (this.connection) {
            try {
                await this.connection.invoke('MarkConversationRead', contactId);
            } catch (error) {
                console.error('Không đánh dấu đã đọc được:', error);
            }
        }
    }

    async loadConversation(contactId) {
        const url = `${this.conversationUrl}?otherUserId=${encodeURIComponent(contactId)}`;
        const response = await fetch(url, {
            headers: {
                'X-Requested-With': 'XMLHttpRequest'
            }
        });

        if (!response.ok) {
            throw new Error('Không tải được nội dung cuộc trò chuyện.');
        }

        const result = await response.json();
        this.conversations[contactId] = Array.isArray(result.data) ? result.data : [];
    }

    renderMessages() {
        const conversation = this.conversations[this.activeContactId] || [];
        const contact = this.contacts.find(x => x.userId === this.activeContactId || (x.userId && this.activeContactId && String(x.userId).toLowerCase() === String(this.activeContactId).toLowerCase()));
        const lastReadMineMessageId = [...conversation]
            .reverse()
            .find(message => message.senderId === this.currentUserId && message.isReadByReceiver === true)?.messageId;

        this.messages.innerHTML = conversation.length === 0
            ? '<div class="messenger-empty-list">Chưa có tin nhắn. Hãy gửi tin nhắn đầu tiên.</div>'
            : conversation.map(message => {
                const mine = message.senderId === this.currentUserId;
                const recalled = message.isRecalled === true;
                const showReadReceipt = mine && lastReadMineMessageId === message.messageId && message.isReadByReceiver === true;
                return `
                    <div class="messenger-message-row ${mine ? 'mine' : 'theirs'}">
                        ${mine ? '' : `<div class="messenger-avatar messenger-avatar-presence ${contact?.isOnline ? 'online' : ''}" style="background:${this.getAccent(contact?.roleName || 'User')}">${this.getInitials(contact?.fullName || 'U')}</div>`}
                        <div class="messenger-message-content ${recalled ? 'recalled' : ''}">
                            ${mine && !recalled ? `
                                <div class="messenger-message-actions">
                                    <button type="button" class="messenger-message-menu-toggle" aria-label="Tùy chọn tin nhắn">
                                        <i class="bi bi-three-dots"></i>
                                    </button>
                                    <div class="messenger-message-menu">
                                        <button type="button" data-action="recall-message" data-message-id="${message.messageId}">Thu hồi</button>
                                    </div>
                                </div>` : ''}
                            <div class="messenger-bubble ${recalled ? 'recalled' : ''}">
                                <div class="messenger-message-author">${mine ? 'Bạn' : this.escapeHtml(contact?.fullName || 'Người dùng')}</div>
                                <div class="messenger-message-text">${this.escapeHtml(message.content)}</div>
                                <div class="messenger-message-time">${this.formatMessageTime(message.sentAt)}</div>
                            </div>
                            ${showReadReceipt ? '<div class="messenger-read-receipt">Đã đọc</div>' : ''}
                        </div>
                    </div>
                `;
            }).join('');

        this.messages.scrollTop = this.messages.scrollHeight;
    }

    async sendMessage() {
        if (!this.activeContactId || !this.input) {
            return;
        }

        const text = this.input.value.trim();
        if (!text) {
            this.input.focus();
            return;
        }

        if (!this.connection || this.connection.state !== signalR.HubConnectionState.Connected) {
            alert('Chat realtime chưa kết nối được tới server.');
            return;
        }

        try {
            await this.connection.invoke('SendPrivateMessage', this.activeContactId, text);
            this.input.value = '';
            this.input.focus();
        } catch (error) {
            console.error(error);
            alert(error?.message || 'Không gửi được tin nhắn.');
        }
    }

    async recallMessage(messageId) {
        if (!messageId || !this.connection || this.connection.state !== signalR.HubConnectionState.Connected) {
            return;
        }

        try {
            await this.connection.invoke('RecallPrivateMessage', messageId);
            this.closeMessageMenus();
        } catch (error) {
            console.error(error);
            alert(error?.message || 'Không thu hồi được tin nhắn.');
        }
    }

    async handleIncomingMessage(message) {
        const contactId = message.senderId === this.currentUserId ? message.receiverId : message.senderId;
        if (!this.conversations[contactId]) {
            this.conversations[contactId] = [];
        }

        this.conversations[contactId].push(message);

        const isFromOther = message.senderId !== this.currentUserId;
        const isChatInFocus = (this.activeContactId === contactId || (this.activeContactId && contactId && String(this.activeContactId).toLowerCase() === String(contactId).toLowerCase())) && this.panel.classList.contains('open');

        const contact = this.contacts.find(x => x.userId === contactId || (x.userId && contactId && String(x.userId).toLowerCase() === String(contactId).toLowerCase()));
        if (contact) {
            contact.lastMessage = message.content;
            contact.lastMessageAt = message.sentAt;
            if (!isChatInFocus) {
                contact.unreadCount = (contact.unreadCount || 0) + 1;

                // Update notification bell badge when receiving a message from someone else
                if (isFromOther) {
                    this._incrementNotifBadge();
                    if (typeof toastr !== 'undefined') {
                        toastr.info(
                            message.content.length > 60 ? message.content.substring(0, 60) + '...' : message.content,
                            'Tin nhắn mới từ ' + (contact.fullName || 'Người dùng'),
                            { timeOut: 4000, closeButton: true, progressBar: true, positionClass: 'toast-top-right' }
                        );
                    }
                }
            } else {
                contact.unreadCount = 0;
                if (this.connection) {
                    try {
                        await this.connection.invoke('MarkConversationRead', contactId);
                    } catch (error) {
                        console.error(error);
                    }
                }
            }
        } else {
            // Contact not yet in list — still increment the bell if message is from someone else
            if (isFromOther) {
                this._incrementNotifBadge();
            }
            await this.loadContacts(false);
        }

        if (this.activeContactId === contactId) {
            this.renderMessages();
        }

        this.renderContacts();
        this.updateFabBadge();
    }

    _incrementNotifBadge() {
        let badge = document.querySelector('.notif-badge.alert-count');
        if (badge) {
            const cur = parseInt(badge.innerText) || 0;
            badge.innerText = cur + 1;
            badge.style.display = '';
            // ensure badge visible with count
            badge.style.width = 'auto';
            badge.style.height = 'auto';
            badge.style.padding = '2px 5px';
            badge.style.fontSize = '10px';
        } else {
            // Badge doesn't exist (was 0) — create it
            const bellBtn = document.querySelector('.notif-bell-btn');
            if (bellBtn) {
                const newBadge = document.createElement('span');
                newBadge.className = 'notif-badge alert-count';
                newBadge.innerText = '1';
                newBadge.style.width = 'auto';
                newBadge.style.height = 'auto';
                newBadge.style.padding = '2px 5px';
                newBadge.style.fontSize = '10px';
                bellBtn.prepend(newBadge);
            }
        }
    }


    async handleRecalledMessage(message) {
        const contactId = message.senderId === this.currentUserId ? message.receiverId : message.senderId;
        if (!this.conversations[contactId]) {
            this.conversations[contactId] = [];
        }

        const index = this.conversations[contactId].findIndex(x => x.messageId === message.messageId);
        if (index >= 0) {
            this.conversations[contactId][index] = message;
        } else {
            this.conversations[contactId].push(message);
        }

        const contact = this.contacts.find(x => x.userId === contactId || (x.userId && contactId && String(x.userId).toLowerCase() === String(contactId).toLowerCase()));
        if (contact) {
            const lastMessage = this.conversations[contactId][this.conversations[contactId].length - 1];
            if (lastMessage) {
                contact.lastMessage = lastMessage.content;
                contact.lastMessageAt = lastMessage.sentAt;
            }
        }

        if (this.activeContactId === contactId) {
            this.renderMessages();
        }

        this.renderContacts();
    }

    handleConversationRead(payload) {
        const readerUserId = payload?.readerUserId || payload?.ReaderUserId;
        const otherUserId = payload?.otherUserId || payload?.OtherUserId;
        if (!readerUserId || otherUserId !== this.currentUserId) {
            return;
        }

        const conversation = this.conversations[readerUserId] || [];
        conversation.forEach(message => {
            if (message.senderId === this.currentUserId && message.receiverId === readerUserId) {
                message.isReadByReceiver = true;
            }
        });

        if (this.activeContactId === readerUserId) {
            this.renderMessages();
        }
    }

    closeMessageMenus(exceptContainer = null) {
        this.messages?.querySelectorAll('.messenger-message-menu.open').forEach(menu => {
            if (exceptContainer && exceptContainer.contains(menu)) {
                return;
            }

            menu.classList.remove('open');
        });
    }

    updateFabBadge() {
        const totalUnread = this.contacts.reduce((sum, item) => sum + (item.unreadCount || 0), 0);
        if (!this.fabBadge) {
            return;
        }

        this.fabBadge.textContent = totalUnread;
        this.fabBadge.classList.toggle('d-none', totalUnread === 0);
    }

    getInitials(name) {
        return (name || 'U')
            .split(' ')
            .filter(Boolean)
            .slice(0, 2)
            .map(part => part[0].toUpperCase())
            .join('');
    }

    getAccent(roleName) {
        const role = (roleName || '').toLowerCase();
        if (role.includes('admin')) return '#7c3aed';
        if (role.includes('staff')) return '#0f766e';
        if (role.includes('organizer')) return '#2563eb';
        if (role.includes('approver') || role.includes('approval')) return '#ea580c';
        if (role.includes('student')) return '#db2777';
        return '#4f46e5';
    }

    formatTime(value) {
        const date = new Date(value);
        return Number.isNaN(date.getTime())
            ? ''
            : date.toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' });
    }

    formatMessageTime(value) {
        const date = new Date(value);
        return Number.isNaN(date.getTime())
            ? ''
            : date.toLocaleString('vi-VN', { hour: '2-digit', minute: '2-digit', day: '2-digit', month: '2-digit' });
    }

    escapeHtml(value) {
        const div = document.createElement('div');
        div.textContent = value || '';
        return div.innerHTML;
    }
}

document.addEventListener('DOMContentLoaded', async () => {
    for (const root of document.querySelectorAll('#messengerLauncher')) {
        const manager = new AemsMessengerLayout(root);
        await manager.init();
        window.aemsMessengerLayout = manager;
    }

    document.addEventListener('aems:open-chat', async (event) => {
        const userId = event.detail?.userId;
        if (!userId) {
            return;
        }

        if (!window.aemsMessengerLayout) {
            console.error('aems:open-chat received but messenger is not initialized');
            alert('Hệ thống chat đang được tải. Vui lòng thử lại sau giây lát.');
            return;
        }

        await window.aemsMessengerLayout.openChatWith(userId);
    });
});
