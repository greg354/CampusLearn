/* ========================================
   CampusLearn™ - Enhanced Interactivity
   Theme Management & UI Components
   ======================================== */

// ========================================
// THEME MANAGEMENT
// ========================================

const ThemeManager = {
    // Get current theme from localStorage or system preference
    getCurrentTheme: function () {
        const savedTheme = localStorage.getItem('campuslearn-theme');
        if (savedTheme) {
            return savedTheme;
        }

        // Check system preference
        if (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches) {
            return 'dark';
        }

        return 'light';
    },

    // Set theme
    setTheme: function (theme) {
        document.documentElement.setAttribute('data-theme', theme);
        localStorage.setItem('campuslearn-theme', theme);

        // Update theme toggle icon
        this.updateToggleIcon(theme);

        // Dispatch custom event for other components
        window.dispatchEvent(new CustomEvent('themechange', { detail: { theme } }));
    },

    // Toggle between light and dark
    toggleTheme: function () {
        const currentTheme = this.getCurrentTheme();
        const newTheme = currentTheme === 'light' ? 'dark' : 'light';
        this.setTheme(newTheme);

        // Add ripple effect
        this.addRippleEffect();
    },

    // Update toggle icon
    updateToggleIcon: function (theme) {
        const sunIcon = document.querySelector('.theme-toggle .fa-sun');
        const moonIcon = document.querySelector('.theme-toggle .fa-moon');

        if (theme === 'dark') {
            if (sunIcon) sunIcon.style.display = 'block';
            if (moonIcon) moonIcon.style.display = 'none';
        } else {
            if (sunIcon) sunIcon.style.display = 'none';
            if (moonIcon) moonIcon.style.display = 'block';
        }
    },

    // Add ripple effect on toggle
    addRippleEffect: function () {
        const toggle = document.querySelector('.theme-toggle');
        if (!toggle) return;

        toggle.style.transform = 'scale(0.9)';
        setTimeout(() => {
            toggle.style.transform = 'scale(1.1)';
        }, 100);
        setTimeout(() => {
            toggle.style.transform = 'scale(1)';
        }, 200);
    },

    // Initialize theme
    init: function () {
        const theme = this.getCurrentTheme();
        this.setTheme(theme);

        // Listen for system theme changes
        if (window.matchMedia) {
            window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', (e) => {
                if (!localStorage.getItem('campuslearn-theme')) {
                    this.setTheme(e.matches ? 'dark' : 'light');
                }
            });
        }
    }
};

// ========================================
// PAGE INITIALIZATION
// ========================================

document.addEventListener('DOMContentLoaded', function () {
    // Initialize theme
    ThemeManager.init();

    // Initialize other components
    initializeAnimations();
    initializeForms();
    initializeAlerts();
    createThemeToggle();
    initializeStatsAnimations();
    initializeActivityItems();
});

// ========================================
// CREATE THEME TOGGLE BUTTON
// ========================================

function createThemeToggle() {
    // Check if toggle already exists
    if (document.querySelector('.theme-toggle')) {
        // If it exists, just attach event listener
        document.querySelector('.theme-toggle').addEventListener('click', () => {
            ThemeManager.toggleTheme();
        });
        return;
    }

    // Create toggle button
    const toggle = document.createElement('button');
    toggle.className = 'theme-toggle';
    toggle.setAttribute('aria-label', 'Toggle theme');
    toggle.innerHTML = `
        <i class="fas fa-sun"></i>
        <i class="fas fa-moon"></i>
    `;

    // Add to body
    document.body.appendChild(toggle);

    // Add click event
    toggle.addEventListener('click', () => {
        ThemeManager.toggleTheme();
    });
}

// ========================================
// ANIMATIONS
// ========================================

function initializeAnimations() {
    // Intersection Observer for fade-in animations
    const observer = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.classList.add('fade-in-up');
                observer.unobserve(entry.target);
            }
        });
    }, {
        threshold: 0.1,
        rootMargin: '50px'
    });

    // Observe elements
    const elementsToAnimate = document.querySelectorAll('.stats-card, .activity-feed, .card');
    elementsToAnimate.forEach(el => {
        observer.observe(el);
    });
}

// ========================================
// STATS ANIMATIONS
// ========================================

function initializeStatsAnimations() {
    const statsNumbers = document.querySelectorAll('.stats-number');

    if (statsNumbers.length === 0) return;

    setTimeout(() => {
        statsNumbers.forEach((element, index) => {
            setTimeout(() => {
                animateNumber(element);
            }, index * 200);
        });
    }, 500);
}

function animateNumber(element) {
    const text = element.textContent;
    const isPercentage = text.includes('%');
    const target = parseInt(text.replace('%', ''));

    if (isNaN(target)) return;

    let current = 0;
    const increment = Math.ceil(target / 30);
    const duration = 1000;
    const steps = 30;
    const stepDuration = duration / steps;

    const timer = setInterval(() => {
        current += increment;
        if (current >= target) {
            current = target;
            clearInterval(timer);

            // Add bounce effect
            element.style.transform = 'scale(1.2)';
            setTimeout(() => {
                element.style.transform = 'scale(1)';
            }, 200);
        }
        element.textContent = current + (isPercentage ? '%' : '');
    }, stepDuration);
}

// ========================================
// FORM HANDLING
// ========================================

function initializeForms() {
    // Form validation
    const forms = document.querySelectorAll('form');
    forms.forEach(form => {
        form.addEventListener('submit', handleFormSubmit);
    });

    // Real-time email validation
    const emailInputs = document.querySelectorAll('input[type="email"]');
    emailInputs.forEach(input => {
        input.addEventListener('blur', validateBelgiumEmail);
        input.addEventListener('input', debounce(validateBelgiumEmail, 500));
    });

    // Password strength indicator
    const passwordInputs = document.querySelectorAll('input[type="password"]');
    passwordInputs.forEach(input => {
        if (input.id === 'password') {
            input.addEventListener('input', debounce(showPasswordStrength, 300));
        }
    });
}

// ========================================
// FORM SUBMISSION
// ========================================

function handleFormSubmit(e) {
    const form = e.target;
    const submitBtn = form.querySelector('button[type="submit"]');

    if (submitBtn && form.checkValidity()) {
        // Show loading state
        const originalText = submitBtn.innerHTML;
        submitBtn.innerHTML = '<i class="fas fa-spinner fa-spin me-2"></i>Processing...';
        submitBtn.disabled = true;

        // Note: Form will actually submit, this is just for visual feedback
        // The loading state will be cleared by page reload
    }
}

// ========================================
// EMAIL VALIDATION
// ========================================

function validateBelgiumEmail(e) {
    const email = e.target.value;
    const emailType = e.target.getAttribute('data-email-type');

    if (!email || !emailType) return;

    let isValid = false;
    let errorMessage = '';

    if (emailType === 'student') {
        isValid = /[0-9]{6}@student\.belgiumcampus\.ac\.za$/.test(email);
        errorMessage = 'Please use your Belgium Campus student email (format: 123456@student.belgiumcampus.ac.za)';
    } else if (emailType === 'tutor') {
        isValid = /^[a-zA-Z0-9._%+-]+@tutor\.belgiumcampus\.ac\.za$/.test(email);
        errorMessage = 'Please use your Belgium Campus tutor email (format: name@tutor.belgiumcampus.ac.za)';
    }

    if (email && !isValid) {
        showFieldError(e.target, errorMessage);
    } else if (email && isValid) {
        showFieldSuccess(e.target);
    }
}

// ========================================
// PASSWORD STRENGTH
// ========================================

function showPasswordStrength(e) {
    const password = e.target.value;

    if (!password) return;

    let strength = 0;
    let feedback = '';

    // Check length
    if (password.length >= 8) strength++;
    if (password.length >= 12) strength++;

    // Check character types
    if (/[a-z]/.test(password)) strength++;
    if (/[A-Z]/.test(password)) strength++;
    if (/[0-9]/.test(password)) strength++;
    if (/[^a-zA-Z0-9]/.test(password)) strength++;

    // Determine strength level
    if (strength <= 2) {
        feedback = 'Weak password';
    } else if (strength <= 4) {
        feedback = 'Medium password';
    } else {
        feedback = 'Strong password';
    }

    // Show feedback (you can customize this)
    console.log('Password strength:', feedback);
}



// Number animation
function animateNumber(element) {
    // Check if already animated
    if (element.dataset.animated === 'true') {
        return;
    }

    // Mark as animated to prevent double animation
    element.dataset.animated = 'true';

    const text = element.textContent.trim();
    const target = parseInt(text, 10);

    // If target is not a valid number, don't animate
    if (isNaN(target) || target < 0) {
        element.textContent = '0';
        return;
    }

    let current = 0;
    const increment = Math.ceil(target / 30) || 1;

    const timer = setInterval(() => {
        current += increment;
        if (current >= target) {
            element.textContent = target;
            clearInterval(timer);
        } else {
            element.textContent = current;
        }
    }, 50);
}

// ========================================
// FIELD VALIDATION HELPERS
// ========================================

function showFieldError(field, message) {
    field.classList.add('is-invalid');
    field.classList.remove('is-valid');

    let feedback = field.parentNode.querySelector('.invalid-feedback');
    if (!feedback) {
        feedback = document.createElement('div');
        feedback.className = 'invalid-feedback';
        field.parentNode.appendChild(feedback);
    }
    feedback.textContent = message;
    feedback.style.display = 'block';
}

function showFieldSuccess(field) {
    field.classList.add('is-valid');
    field.classList.remove('is-invalid');

    const feedback = field.parentNode.querySelector('.invalid-feedback');
    if (feedback) {
        feedback.style.display = 'none';
    }
}

// ========================================
// ALERT HANDLING
// ========================================

function initializeAlerts() {
    // Auto-hide alerts after 5 seconds (but NOT persistent ones)
    setTimeout(() => {
        const alerts = document.querySelectorAll('.alert:not([data-persistent])');
        alerts.forEach(alert => {
            fadeOut(alert, 300);
        });
    }, 5000);
}

function fadeOut(element, duration) {
    element.style.transition = `opacity ${duration}ms ease`;
    element.style.opacity = '0';
    setTimeout(() => {
        element.remove();
    }, duration);
}

// ========================================
// ACTIVITY ITEMS
// ========================================

function initializeActivityItems() {
    const activityItems = document.querySelectorAll('.activity-item');

    activityItems.forEach(item => {
        item.addEventListener('mouseenter', function () {
            this.style.transform = 'translateX(10px)';
        });

        item.addEventListener('mouseleave', function () {
            this.style.transform = 'translateX(0)';
        });
    });
}

// ========================================
// COMING SOON MODAL
// ========================================

function showComingSoon(feature) {
    // Remove existing modal if any
    const existingModal = document.querySelector('.coming-soon-modal');
    if (existingModal) {
        existingModal.remove();
    }

    const modal = document.createElement('div');
    modal.className = 'coming-soon-modal';
    modal.style.cssText = `
        position: fixed;
        top: 0;
        left: 0;
        width: 100%;
        height: 100%;
        background: rgba(0, 0, 0, 0.7);
        display: flex;
        align-items: center;
        justify-content: center;
        z-index: 2000;
        animation: fadeIn 0.3s ease;
        backdrop-filter: blur(5px);
    `;

    const currentTheme = ThemeManager.getCurrentTheme();
    const bgColor = currentTheme === 'dark' ? '#1e293b' : '#ffffff';
    const textColor = currentTheme === 'dark' ? '#f1f5f9' : '#1a1a1a';

    modal.innerHTML = `
        <div style="
            background: ${bgColor}; 
            padding: 2.5rem; 
            border-radius: 1.5rem; 
            text-align: center; 
            max-width: 400px; 
            margin: 1rem;
            box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.25);
            animation: slideUp 0.3s ease;
            color: ${textColor};
        ">
            <div style="
                width: 80px;
                height: 80px;
                margin: 0 auto 1.5rem;
                background: linear-gradient(135deg, #6366f1 0%, #8b5cf6 100%);
                border-radius: 50%;
                display: flex;
                align-items: center;
                justify-content: center;
            ">
                <i class="fas fa-rocket fa-3x" style="color: white;"></i>
            </div>
            <h4 style="color: ${textColor}; margin-bottom: 1rem; font-weight: 700;">${feature} Coming Soon!</h4>
            <p style="color: ${currentTheme === 'dark' ? '#cbd5e1' : '#6c757d'}; margin-bottom: 1.5rem;">
                This exciting feature is currently under development. Stay tuned for updates!
            </p>
            <button onclick="this.closest('.coming-soon-modal').remove()" 
                style="
                    background: linear-gradient(135deg, #6366f1 0%, #8b5cf6 100%);
                    color: white;
                    border: none;
                    padding: 0.75rem 2rem;
                    border-radius: 0.5rem;
                    font-weight: 600;
                    cursor: pointer;
                    transition: all 0.2s ease;
                "
                onmouseover="this.style.transform='scale(1.05)'"
                onmouseout="this.style.transform='scale(1)'">
                <i class="fas fa-check me-2"></i>Got It
            </button>
        </div>
    `;

    document.body.appendChild(modal);

    // Close on background click
    modal.addEventListener('click', (e) => {
        if (e.target === modal) {
            modal.remove();
        }
    });

    // Close on Escape key
    const escapeHandler = (e) => {
        if (e.key === 'Escape') {
            modal.remove();
            document.removeEventListener('keydown', escapeHandler);
        }
    };
    document.addEventListener('keydown', escapeHandler);
}

// Add CSS animations
const style = document.createElement('style');
style.textContent = `
    @keyframes fadeIn {
        from { opacity: 0; }
        to { opacity: 1; }
    }
    
    @keyframes slideUp {
        from { 
            opacity: 0;
            transform: translateY(30px); 
        }
        to { 
            opacity: 1;
            transform: translateY(0); 
        }
    }
`;
document.head.appendChild(style);

// ========================================
// UTILITY FUNCTIONS
// ========================================

// Debounce function for performance
function debounce(func, wait) {
    let timeout;
    return function executedFunction(...args) {
        const later = () => {
            clearTimeout(timeout);
            func.apply(this, args);
        };
        clearTimeout(timeout);
        timeout = setTimeout(later, wait);
    };
}

// Smooth scrolling for anchor links
document.querySelectorAll('a[href^="#"]').forEach(anchor => {
    anchor.addEventListener('click', function (e) {
        e.preventDefault();
        const target = document.querySelector(this.getAttribute('href'));
        if (target) {
            target.scrollIntoView({
                behavior: 'smooth',
                block: 'start'
            });
        }
    });
});

// ========================================
// KEYBOARD SHORTCUTS
// ========================================

document.addEventListener('keydown', (e) => {
    // Ctrl/Cmd + D to toggle theme
    if ((e.ctrlKey || e.metaKey) && e.key === 'd') {
        e.preventDefault();
        ThemeManager.toggleTheme();
    }

    // Escape to close modals
    if (e.key === 'Escape') {
        const modals = document.querySelectorAll('.coming-soon-modal, [style*="position: fixed"]');
        modals.forEach(modal => {
            if (modal.classList.contains('theme-toggle')) return;
            modal.remove();
        });
    }
});

// ========================================
// TOAST NOTIFICATIONS
// ========================================

const CampusLearn = {
    utils: {
        showToast: function (message, type = 'info', duration = 3000) {
            const toast = document.createElement('div');
            const currentTheme = ThemeManager.getCurrentTheme();
            const bgColor = currentTheme === 'dark' ? '#1e293b' : '#ffffff';
            const textColor = currentTheme === 'dark' ? '#f1f5f9' : '#1a1a1a';

            let iconClass, accentColor;
            switch (type) {
                case 'success':
                    iconClass = 'fa-check-circle';
                    accentColor = '#10b981';
                    break;
                case 'error':
                    iconClass = 'fa-exclamation-circle';
                    accentColor = '#ef4444';
                    break;
                case 'warning':
                    iconClass = 'fa-exclamation-triangle';
                    accentColor = '#f59e0b';
                    break;
                default:
                    iconClass = 'fa-info-circle';
                    accentColor = '#6366f1';
            }

            toast.style.cssText = `
                position: fixed;
                top: 90px;
                right: 20px;
                background: ${bgColor};
                color: ${textColor};
                padding: 1rem 1.5rem;
                border-radius: 0.75rem;
                box-shadow: 0 10px 25px rgba(0, 0, 0, 0.2);
                z-index: 1500;
                display: flex;
                align-items: center;
                gap: 0.75rem;
                animation: slideInRight 0.3s ease;
                border-left: 4px solid ${accentColor};
                max-width: 350px;
            `;

            toast.innerHTML = `
                <i class="fas ${iconClass}" style="color: ${accentColor}; font-size: 1.25rem;"></i>
                <span style="flex: 1;">${message}</span>
            `;

            document.body.appendChild(toast);

            setTimeout(() => {
                toast.style.animation = 'slideOutRight 0.3s ease';
                setTimeout(() => toast.remove(), 300);
            }, duration);
        }
    }
};

// Add slide animations
const toastStyle = document.createElement('style');
toastStyle.textContent = `
    @keyframes slideInRight {
        from {
            transform: translateX(400px);
            opacity: 0;
        }
        to {
            transform: translateX(0);
            opacity: 1;
        }
    }
    
    @keyframes slideOutRight {
        from {
            transform: translateX(0);
            opacity: 1;
        }
        to {
            transform: translateX(400px);
            opacity: 0;
        }
    }
`;
document.head.appendChild(toastStyle);

// ========================================
// CONSOLE MESSAGE
// ========================================

console.log('%cCampusLearn™', 'font-size: 2rem; font-weight: bold; background: linear-gradient(135deg, #6366f1 0%, #8b5cf6 100%); -webkit-background-clip: text; -webkit-text-fill-color: transparent;');
console.log('%cTheme: ' + ThemeManager.getCurrentTheme(), 'font-size: 1rem; color: #6366f1;');
console.log('%cPress Ctrl+D to toggle theme', 'font-size: 0.875rem; color: #8b5cf6;');