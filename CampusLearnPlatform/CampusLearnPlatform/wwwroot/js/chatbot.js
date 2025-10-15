// chatbot.js - CampusLearn AI Chatbot Frontend

class CampusLearnChatbot {
    constructor() {
        console.log('Initializing CampusLearn Chatbot...');
        this.isOpen = false;
        this.sessionId = null;
        this.studentId = this.getStudentId();
        this.messageHistory = [];

        if (!this.studentId) {
            console.error('Cannot initialize chatbot: No student ID found');
            return;
        }

        this.init();
    }

    init() {
        console.log('Creating chat window...');
        this.createChatWindow();
        this.attachEventListeners();
        console.log('Chatbot initialized successfully');
    }

    getStudentId() {
        const studentId = sessionStorage.getItem('UserId');

        if (!studentId || studentId === 'null' || studentId === 'undefined') {
            console.error('Student ID not found in session. User must be logged in to use chatbot.');
            // Show error in UI
            setTimeout(() => {
                alert('Please log in to use the chatbot.');
            }, 500);
            return null;
        }

        console.log('Student ID retrieved:', studentId);
        return studentId;
    }

    createChatWindow() {
        const chatWindowHTML = `
            <div id="chatbot-window" class="chatbot-window" style="display: none;">
                <div class="chatbot-header">
                    <div class="chatbot-header-content">
                        <div class="chatbot-avatar">
                            <i class="fas fa-robot"></i>
                        </div>
                        <div class="chatbot-title">
                            <h5>CampusLearn AI Assistant</h5>
                            <p class="chatbot-status">
                                <span class="status-dot"></span> Online
                            </p>
                        </div>
                    </div>
                    <button class="chatbot-close" onclick="campusLearnBot.toggleChat()">
                        <i class="fas fa-times"></i>
                    </button>
                </div>

                <div class="chatbot-messages" id="chatbot-messages">
                    <div class="chatbot-welcome">
                        <div class="welcome-icon">
                            <i class="fas fa-graduation-cap"></i>
                        </div>
                        <h6>Welcome to CampusLearn AI! 👋</h6>
                        <p>I'm here to help you with:</p>
                        <ul>
                            <li>Academic questions</li>
                            <li>Study tips and strategies</li>
                            <li>Platform navigation</li>
                            <li>Frequently asked questions</li>
                        </ul>
                        <p class="text-muted small">How can I assist you today?</p>
                    </div>
                </div>

                <div class="chatbot-quick-actions" id="quick-actions">
                    <button class="quick-action-btn" onclick="campusLearnBot.sendQuickMessage('How do I create a topic?')">
                        <i class="fas fa-question-circle"></i> Create Topic
                    </button>
                    <button class="quick-action-btn" onclick="campusLearnBot.sendQuickMessage('How do I find a tutor?')">
                        <i class="fas fa-user-graduate"></i> Find Tutor
                    </button>
                    <button class="quick-action-btn" onclick="campusLearnBot.sendQuickMessage('Study tips for exams')">
                        <i class="fas fa-book"></i> Study Tips
                    </button>
                </div>

                <div class="chatbot-input-area">
                    <div class="chatbot-typing-indicator" id="typing-indicator" style="display: none;">
                        <span></span><span></span><span></span>
                    </div>
                    <form id="chatbot-form" class="chatbot-form">
                        <input 
                            type="text" 
                            id="chatbot-input" 
                            class="chatbot-input" 
                            placeholder="Type your question..."
                            autocomplete="off"
                        />
                        <button type="submit" class="chatbot-send-btn">
                            <i class="fas fa-paper-plane"></i>
                        </button>
                    </form>
                </div>
            </div>
        `;

        document.body.insertAdjacentHTML('beforeend', chatWindowHTML);
        console.log('Chat window created');
    }

    attachEventListeners() {
        // Chat form submission
        const form = document.getElementById('chatbot-form');
        if (form) {
            form.addEventListener('submit', (e) => {
                e.preventDefault();
                this.sendMessage();
            });
            console.log('Form listener attached');
        }

        // Toggle button - directly attach to existing button
        const toggleBtn = document.getElementById('chatbot-toggle-btn');
        if (toggleBtn) {
            toggleBtn.onclick = () => {
                console.log('Chatbot button clicked');
                this.toggleChat();
            };
            console.log('Toggle button listener attached');
        } else {
            console.error('Chatbot toggle button not found!');
        }
    }

    toggleChat() {
        console.log('Toggle chat called, current state:', this.isOpen);

        if (!this.studentId) {
            alert('Please log in to use the chatbot.');
            return;
        }

        this.isOpen = !this.isOpen;
        const chatWindow = document.getElementById('chatbot-window');

        if (!chatWindow) {
            console.error('Chat window element not found!');
            return;
        }

        if (this.isOpen) {
            console.log('Opening chatbot');
            chatWindow.style.display = 'flex';
            setTimeout(() => chatWindow.classList.add('active'), 10);
            const input = document.getElementById('chatbot-input');
            if (input) input.focus();
        } else {
            console.log('Closing chatbot');
            chatWindow.classList.remove('active');
            setTimeout(() => chatWindow.style.display = 'none', 300);
        }
    }

    async sendMessage() {
        const input = document.getElementById('chatbot-input');
        const message = input.value.trim();

        if (!message) {
            console.log('Empty message, not sending');
            return;
        }

        console.log('Sending message:', message);

        // Clear input
        input.value = '';

        // Hide quick actions after first message
        const quickActions = document.getElementById('quick-actions');
        if (quickActions) quickActions.style.display = 'none';

        // Add user message to chat
        this.addMessage(message, true);

        // Show typing indicator
        this.showTypingIndicator();

        try {
            console.log('Making API call to /api/ChatBot/chat');
            const response = await fetch('/api/ChatBot/chat', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    message: message,
                    sessionId: this.sessionId
                })
            });

            console.log('Response status:', response.status);

            if (!response.ok) {
                const errorText = await response.text();
                console.error('API error:', response.status, errorText);
                throw new Error(`API error: ${response.status} - ${errorText}`);
            }

            const data = await response.json();
            console.log('Response data:', data);

            // Store session ID
            if (data.sessionId) {
                this.sessionId = data.sessionId;
                console.log('Session ID stored:', this.sessionId);
            }

            // Hide typing indicator
            this.hideTypingIndicator();

            // Add bot response
            this.addMessage(data.response, false);

            // If escalation is recommended, show escalation button
            if (data.shouldEscalate) {
                console.log('Escalation recommended');
                this.showEscalationOption(message);
            }

        } catch (error) {
            console.error('Error sending message:', error);
            this.hideTypingIndicator();
            this.addMessage('Sorry, I encountered an error. Please try again. Error: ' + error.message, false, true);
        }
    }

    sendQuickMessage(message) {
        console.log('Quick message:', message);
        const input = document.getElementById('chatbot-input');
        if (input) {
            input.value = message;
            this.sendMessage();
        }
    }

    addMessage(text, isUser = false, isError = false) {
        const messagesContainer = document.getElementById('chatbot-messages');
        if (!messagesContainer) {
            console.error('Messages container not found');
            return;
        }

        const messageDiv = document.createElement('div');
        messageDiv.className = `chatbot-message ${isUser ? 'user-message' : 'bot-message'} ${isError ? 'error-message' : ''}`;

        const time = new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });

        messageDiv.innerHTML = `
            <div class="message-content">
                ${!isUser ? '<div class="message-avatar"><i class="fas fa-robot"></i></div>' : ''}
                <div class="message-bubble">
                    <p>${this.formatMessage(text)}</p>
                    <span class="message-time">${time}</span>
                </div>
            </div>
        `;

        messagesContainer.appendChild(messageDiv);
        this.scrollToBottom();

        // Store in history
        this.messageHistory.push({ text, isUser, timestamp: new Date() });
    }

    formatMessage(text) {
        // Convert URLs to links
        text = text.replace(/(https?:\/\/[^\s]+)/g, '<a href="$1" target="_blank">$1</a>');
        // Convert newlines to <br>
        text = text.replace(/\n/g, '<br>');
        return text;
    }

    showEscalationOption(originalQuery) {
        const messagesContainer = document.getElementById('chatbot-messages');
        if (!messagesContainer) return;

        const escalationDiv = document.createElement('div');
        escalationDiv.className = 'chatbot-escalation';
        escalationDiv.innerHTML = `
            <div class="escalation-card">
                <div class="escalation-icon">
                    <i class="fas fa-user-graduate"></i>
                </div>
                <h6>Need More Help?</h6>
                <p>Connect with a peer tutor for personalized assistance</p>
                <button class="btn btn-primary btn-sm" onclick="campusLearnBot.escalateToTutor('${originalQuery.replace(/'/g, "\\'")}')">
                    <i class="fas fa-paper-plane me-1"></i> Contact Tutor
                </button>
            </div>
        `;

        messagesContainer.appendChild(escalationDiv);
        this.scrollToBottom();
    }

    async escalateToTutor(query) {
        try {
            console.log('Escalating query:', query);
            this.showTypingIndicator();

            const response = await fetch('/api/ChatBot/escalate', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    sessionId: this.sessionId,
                    query: query,
                    module: 'General'
                })
            });

            const data = await response.json();
            this.hideTypingIndicator();

            if (data.success) {
                this.addMessage('✅ ' + data.message, false);
            } else {
                this.addMessage('There was an issue escalating your query. Please try again.', false, true);
            }

        } catch (error) {
            console.error('Error escalating:', error);
            this.hideTypingIndicator();
            this.addMessage('Failed to connect with tutor service. Please try again.', false, true);
        }
    }

    showTypingIndicator() {
        const indicator = document.getElementById('typing-indicator');
        if (indicator) {
            indicator.style.display = 'flex';
            this.scrollToBottom();
        }
    }

    hideTypingIndicator() {
        const indicator = document.getElementById('typing-indicator');
        if (indicator) {
            indicator.style.display = 'none';
        }
    }

    scrollToBottom() {
        const messagesContainer = document.getElementById('chatbot-messages');
        if (messagesContainer) {
            setTimeout(() => {
                messagesContainer.scrollTop = messagesContainer.scrollHeight;
            }, 100);
        }
    }

    async loadFAQs() {
        try {
            const response = await fetch('/api/ChatBot/faqs');
            const data = await response.json();
            console.log('FAQs loaded:', data.faqs);
        } catch (error) {
            console.error('Error loading FAQs:', error);
        }
    }
}

// Initialize chatbot when DOM is ready
let campusLearnBot;
document.addEventListener('DOMContentLoaded', function () {
    console.log('DOM loaded, initializing chatbot...');
    try {
        campusLearnBot = new CampusLearnChatbot();
        window.campusLearnBot = campusLearnBot;
        console.log('Chatbot ready!');
    } catch (error) {
        console.error('Failed to initialize chatbot:', error);
    }
});