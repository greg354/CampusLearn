/**
 * CampusLearn™ - Frontend Only JavaScript
 */

// Initialize when page loads
document.addEventListener('DOMContentLoaded', function () {
    initializeAnimations();
    initializeForms();
    initializeAlerts();
});

// Animations
function initializeAnimations() {
    // Fade in animations
    const observer = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.classList.add('fade-in-up');
            }
        });
    });

    document.querySelectorAll('.stats-card, .activity-feed').forEach(el => {
        observer.observe(el);
    });

    // Animate dashboard stats
    if (document.querySelector('.stats-number')) {
        setTimeout(() => {
            document.querySelectorAll('.stats-number').forEach((el, index) => {
                setTimeout(() => animateNumber(el), index * 200);
            });
        }, 500);
    }
}

// Form handling
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
    });

    // Student ID formatting
    const studentIdInputs = document.querySelectorAll('input[name="StudentId"]');
    studentIdInputs.forEach(input => {
        input.addEventListener('input', function () {
            this.value = this.value.replace(/\D/g, '').slice(0, 10);
        });
    });
}

// Alert handling
function initializeAlerts() {
    // Auto-hide alerts after 5 seconds
    setTimeout(() => {
        const alerts = document.querySelectorAll('.alert');
        alerts.forEach(alert => {
            alert.style.opacity = '0';
            setTimeout(() => alert.remove(), 300);
        });
    }, 5000);
}

// Form submission
function handleFormSubmit(e) {
    const form = e.target;
    const submitBtn = form.querySelector('button[type="submit"]');

    if (submitBtn && form.checkValidity()) {
        // Show loading state
        const originalText = submitBtn.innerHTML;
        submitBtn.innerHTML = '<i class="fas fa-spinner fa-spin me-2"></i>Processing...';
        submitBtn.disabled = true;

        // Restore after 2 seconds (since this is just frontend)
        setTimeout(() => {
            submitBtn.innerHTML = originalText;
            submitBtn.disabled = false;
        }, 2000);
    }
}

// Email validation
function validateBelgiumEmail(e) {
    const email = e.target.value;
    const isValid = /^[a-zA-Z0-9._%+-]+@belgiumcampus\.ac\.za$/.test(email);

    if (email && !isValid) {
        showFieldError(e.target, 'Please use your Belgium Campus email address');
    } else if (email && isValid) {
        showFieldSuccess(e.target);
    }
}

// Number animation
function animateNumber(element) {
    const target = parseInt(element.textContent);
    let current = 0;
    const increment = Math.ceil(target / 30);

    const timer = setInterval(() => {
        current += increment;
        if (current >= target) {
            current = target;
            clearInterval(timer);
        }
        element.textContent = current;
    }, 50);
}

// Field validation helpers
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
}

function showFieldSuccess(field) {
    field.classList.add('is-valid');
    field.classList.remove('is-invalid');

    const feedback = field.parentNode.querySelector('.invalid-feedback');
    if (feedback) {
        feedback.remove();
    }
}

// Coming soon modal
function showComingSoon(feature) {
    const modal = document.createElement('div');
    modal.style.cssText = `
        position: fixed;
        top: 0;
        left: 0;
        width: 100%;
        height: 100%;
        background: rgba(0,0,0,0.5);
        display: flex;
        align-items: center;
        justify-content: center;
        z-index: 1000;
    `;

    modal.innerHTML = `
        <div style="background: white; padding: 2rem; border-radius: 1rem; text-align: center; max-width: 400px; margin: 1rem;">
            <i class="fas fa-rocket fa-3x text-primary mb-3"></i>
            <h4>${feature} Coming Soon!</h4>
            <p class="text-muted mb-3">This feature is currently under development.</p>
            <button onclick="this.closest('[style*=fixed]').remove()" class="btn btn-primary">
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
}

// Smooth scrolling
document.querySelectorAll('a[href^="#"]').forEach(anchor => {
    anchor.addEventListener('click', function (e) {
        e.preventDefault();
        const target = document.querySelector(this.getAttribute('href'));
        if (target) {
            target.scrollIntoView({ behavior: 'smooth' });
        }
    });
});

// Keyboard shortcuts
document.addEventListener('keydown', (e) => {
    // Escape to close modals
    if (e.key === 'Escape') {
        const modals = document.querySelectorAll('[style*="position: fixed"]');
        modals.forEach(modal => modal.remove());
    }
});

// Activity item hover effects
document.querySelectorAll('.activity-item').forEach(item => {
    item.addEventListener('mouseenter', function () {
        this.style.transform = 'translateX(10px)';
    });

    item.addEventListener('mouseleave', function () {
        this.style.transform = 'translateX(0)';
    });
});