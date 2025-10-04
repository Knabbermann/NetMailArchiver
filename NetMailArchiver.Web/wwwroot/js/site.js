/**
 * NetMailArchiver - Modern JavaScript Enhancements
 * Enhanced with Vapor theme styling and #1b6ec2 color scheme
 */

// Global configuration
const NetMailArchiver = {
    config: {
        primaryColor: '#1b6ec2',
        toastDuration: 5000,
        animationDuration: 300,
        refreshInterval: 30000
    },
    
    // Initialization
    init() {
        this.setupGlobalEnhancements();
        this.setupAnimations();
        this.setupUtilities();
        this.setupNotifications();
        this.setupFormEnhancements();
        console.log('NetMailArchiver UI initialized');
    },

    // Global UI enhancements
    setupGlobalEnhancements() {
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

        // Enhanced tooltips
        this.initializeTooltips();

        // Loading states for buttons
        this.setupButtonLoadingStates();

        // Auto-hide alerts
        this.setupAutoHideAlerts();

        // Responsive table handling
        this.setupResponsiveTables();
    },

    // Animation system
    setupAnimations() {
        // Intersection Observer for fade-in animations
        const observerOptions = {
            threshold: 0.1,
            rootMargin: '0px 0px -50px 0px'
        };

        const observer = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    entry.target.classList.add('animate-in');
                    observer.unobserve(entry.target);
                }
            });
        }, observerOptions);

        // Observe all fade-in elements
        document.querySelectorAll('.fade-in-up, .fade-in, .stat-card, .card').forEach(el => {
            observer.observe(el);
        });

        // Stagger animations for groups
        this.staggerAnimations();
    },

    // Stagger animations for grouped elements
    staggerAnimations() {
        const groups = document.querySelectorAll('.row .col-lg-3, .row .col-md-6, .row .col-xl-4');
        groups.forEach((element, index) => {
            element.style.animationDelay = `${index * 0.1}s`;
        });
    },

    // Utility functions
    setupUtilities() {
        // Copy to clipboard functionality
        window.copyToClipboard = (text) => {
            navigator.clipboard.writeText(text).then(() => {
                this.showToast('Copied to clipboard!', 'success');
            }).catch(() => {
                this.showToast('Failed to copy to clipboard', 'error');
            });
        };

        // Format numbers
        window.formatNumber = (num) => {
            return new Intl.NumberFormat().format(num);
        };

        // Format dates
        window.formatDate = (date) => {
            return new Date(date).toLocaleDateString('de-DE', {
                year: 'numeric',
                month: 'short',
                day: 'numeric',
                hour: '2-digit',
                minute: '2-digit'
            });
        };

        // Debounce function
        window.debounce = (func, wait) => {
            let timeout;
            return function executedFunction(...args) {
                const later = () => {
                    clearTimeout(timeout);
                    func(...args);
                };
                clearTimeout(timeout);
                timeout = setTimeout(later, wait);
            };
        };
    },

    // Enhanced notifications
    setupNotifications() {
        // Configure toastr if available
        if (typeof toastr !== 'undefined') {
            toastr.options = {
                closeButton: true,
                debug: false,
                newestOnTop: true,
                progressBar: true,
                positionClass: "toast-bottom-right",
                preventDuplicates: false,
                onclick: null,
                showDuration: "300",
                hideDuration: "1000",
                timeOut: this.config.toastDuration,
                extendedTimeOut: "1000",
                showEasing: "swing",
                hideEasing: "linear",
                showMethod: "fadeIn",
                hideMethod: "fadeOut"
            };
        }

        // Custom toast function
        this.showToast = (message, type = 'info', title = '') => {
            if (typeof toastr !== 'undefined') {
                toastr[type](message, title);
            } else {
                console.log(`[${type.toUpperCase()}] ${title ? title + ': ' : ''}${message}`);
            }
        };

        // Global error handler
        window.addEventListener('error', (e) => {
            console.error('Global error:', e.error);
            this.showToast('An unexpected error occurred', 'error');
        });
    },

    // Form enhancements
    setupFormEnhancements() {
        // Real-time validation
        document.querySelectorAll('input[type="email"]').forEach(input => {
            input.addEventListener('blur', this.validateEmail.bind(this));
        });

        // Auto-resize textareas
        document.querySelectorAll('textarea').forEach(textarea => {
            textarea.addEventListener('input', this.autoResizeTextarea);
        });

        // Enhanced file uploads
        document.querySelectorAll('input[type="file"]').forEach(input => {
            input.addEventListener('change', this.handleFileUpload.bind(this));
        });

        // Form submission with loading states
        document.querySelectorAll('form').forEach(form => {
            form.addEventListener('submit', this.handleFormSubmission.bind(this));
        });
    },

    // Email validation
    validateEmail(event) {
        const email = event.target.value;
        const isValid = /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
        
        if (email && !isValid) {
            event.target.classList.add('is-invalid');
            this.showValidationError(event.target, 'Please enter a valid email address');
        } else {
            event.target.classList.remove('is-invalid');
            this.hideValidationError(event.target);
        }
    },

    // Auto-resize textarea
    autoResizeTextarea(event) {
        const textarea = event.target;
        textarea.style.height = 'auto';
        textarea.style.height = textarea.scrollHeight + 'px';
    },

    // File upload handler
    handleFileUpload(event) {
        const files = event.target.files;
        if (files.length > 0) {
            const file = files[0];
            const maxSize = 10 * 1024 * 1024; // 10MB
            
            if (file.size > maxSize) {
                this.showToast('File size must be less than 10MB', 'error');
                event.target.value = '';
                return;
            }
            
            this.showToast(`File "${file.name}" selected`, 'success');
        }
    },

    // Form submission handler
    handleFormSubmission(event) {
        const form = event.target;
        const submitBtn = form.querySelector('button[type="submit"], input[type="submit"]');
        
        if (submitBtn) {
            this.setButtonLoading(submitBtn, true);
            
            // Re-enable button after a delay (in case of client-side navigation)
            setTimeout(() => {
                this.setButtonLoading(submitBtn, false);
            }, 3000);
        }
    },

    // Button loading states
    setupButtonLoadingStates() {
        this.setButtonLoading = (button, loading) => {
            if (loading) {
                button.disabled = true;
                const originalText = button.innerHTML;
                button.setAttribute('data-original-text', originalText);
                button.innerHTML = '<i class="fas fa-spinner fa-spin me-2"></i>Loading...';
                button.classList.add('loading');
            } else {
                button.disabled = false;
                const originalText = button.getAttribute('data-original-text');
                if (originalText) {
                    button.innerHTML = originalText;
                }
                button.classList.remove('loading');
            }
        };
    },

    // Auto-hide alerts
    setupAutoHideAlerts() {
        document.querySelectorAll('.alert:not(.alert-permanent)').forEach(alert => {
            setTimeout(() => {
                alert.style.transition = 'opacity 0.5s ease';
                alert.style.opacity = '0';
                setTimeout(() => {
                    if (alert.parentNode) {
                        alert.parentNode.removeChild(alert);
                    }
                }, 500);
            }, 5000);
        });
    },

    // Responsive tables
    setupResponsiveTables() {
        document.querySelectorAll('.table-responsive').forEach(wrapper => {
            const table = wrapper.querySelector('table');
            if (table) {
                // Add scroll indicators
                this.addScrollIndicators(wrapper);
            }
        });
    },

    // Add scroll indicators to tables
    addScrollIndicators(wrapper) {
        const leftIndicator = document.createElement('div');
        const rightIndicator = document.createElement('div');
        
        leftIndicator.className = 'scroll-indicator scroll-indicator-left';
        rightIndicator.className = 'scroll-indicator scroll-indicator-right';
        
        wrapper.appendChild(leftIndicator);
        wrapper.appendChild(rightIndicator);
        
        const updateIndicators = () => {
            const { scrollLeft, scrollWidth, clientWidth } = wrapper;
            leftIndicator.style.opacity = scrollLeft > 0 ? '1' : '0';
            rightIndicator.style.opacity = scrollLeft < (scrollWidth - clientWidth - 1) ? '1' : '0';
        };
        
        wrapper.addEventListener('scroll', updateIndicators);
        updateIndicators();
    },

    // Initialize tooltips
    initializeTooltips() {
        if (typeof bootstrap !== 'undefined') {
            document.querySelectorAll('[data-bs-toggle="tooltip"]').forEach(el => {
                new bootstrap.Tooltip(el);
            });
        }
    },

    // Validation helpers
    showValidationError(element, message) {
        let errorDiv = element.parentNode.querySelector('.invalid-feedback');
        if (!errorDiv) {
            errorDiv = document.createElement('div');
            errorDiv.className = 'invalid-feedback';
            element.parentNode.appendChild(errorDiv);
        }
        errorDiv.textContent = message;
    },

    hideValidationError(element) {
        const errorDiv = element.parentNode.querySelector('.invalid-feedback');
        if (errorDiv) {
            errorDiv.remove();
        }
    },

    // Page refresh with loading indicator
    refreshPage() {
        const loader = this.createLoader();
        document.body.appendChild(loader);
        
        setTimeout(() => {
            location.reload();
        }, 500);
    },

    // Create loading overlay
    createLoader() {
        const loader = document.createElement('div');
        loader.className = 'loading-overlay';
        loader.innerHTML = `
            <div class="loading-spinner"></div>
            <div class="loading-text">Loading...</div>
        `;
        return loader;
    },

    // Connection status checker
    checkConnection(url, callback) {
        fetch(url, { method: 'HEAD', mode: 'no-cors' })
            .then(() => callback(true))
            .catch(() => callback(false));
    },

    // Real-time clock update
    updateClock() {
        const clockElement = document.getElementById('timeDisplay');
        if (clockElement) {
            const now = new Date();
            const timeString = now.toLocaleTimeString('de-DE', {
                hour: '2-digit',
                minute: '2-digit',
                second: '2-digit'
            });
            clockElement.textContent = timeString;
        }
    },

    // Performance monitoring
    monitorPerformance() {
        if ('performance' in window) {
            window.addEventListener('load', () => {
                setTimeout(() => {
                    const perfData = performance.timing;
                    const loadTime = perfData.loadEventEnd - perfData.navigationStart;
                    console.log(`Page load time: ${loadTime}ms`);
                    
                    if (loadTime > 3000) {
                        console.warn('Page load time is slow');
                    }
                }, 0);
            });
        }
    }
};

// Enhanced CSS for animations and effects
const additionalStyles = `
<style>
/* Loading overlay */
.loading-overlay {
    position: fixed;
    top: 0;
    left: 0;
    width: 100%;
    height: 100%;
    background: rgba(255, 255, 255, 0.9);
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    z-index: 9999;
    backdrop-filter: blur(5px);
}

.loading-spinner {
    width: 50px;
    height: 50px;
    border: 4px solid #e9ecef;
    border-top: 4px solid #1b6ec2;
    border-radius: 50%;
    animation: spin 1s linear infinite;
    margin-bottom: 1rem;
}

.loading-text {
    font-size: 1.1rem;
    color: #1b6ec2;
    font-weight: 600;
}

/* Animation classes */
.animate-in {
    opacity: 1 !important;
    transform: translateY(0) !important;
}

.fade-in-up {
    opacity: 0;
    transform: translateY(30px);
    transition: all 0.6s ease-out;
}

/* Scroll indicators */
.scroll-indicator {
    position: absolute;
    top: 0;
    bottom: 0;
    width: 20px;
    background: linear-gradient(90deg, rgba(0,0,0,0.1), transparent);
    pointer-events: none;
    transition: opacity 0.3s ease;
    z-index: 10;
}

.scroll-indicator-left {
    left: 0;
    background: linear-gradient(90deg, rgba(0,0,0,0.1), transparent);
}

.scroll-indicator-right {
    right: 0;
    background: linear-gradient(270deg, rgba(0,0,0,0.1), transparent);
}

/* Button loading state */
.btn.loading {
    pointer-events: none;
}

/* Enhanced transitions */
.card, .btn, .nav-link {
    transition: all 0.3s ease;
}

/* Status indicators pulse animation */
.status-indicator.online {
    animation: pulse-green 2s infinite;
}

@keyframes pulse-green {
    0% {
        box-shadow: 0 0 0 0 rgba(40, 167, 69, 0.7);
    }
    70% {
        box-shadow: 0 0 0 10px rgba(40, 167, 69, 0);
    }
    100% {
        box-shadow: 0 0 0 0 rgba(40, 167, 69, 0);
    }
}

/* Dark theme enhancements */
[data-bs-theme="dark"] .loading-overlay {
    background: rgba(33, 37, 41, 0.9);
}

[data-bs-theme="dark"] .loading-text {
    color: #1b6ec2;
}

/* Responsive enhancements */
@media (max-width: 768px) {
    .fade-in-up {
        transform: translateY(15px);
    }
    
    .stat-card {
        margin-bottom: 1rem;
    }
}
</style>
`;

// Inject additional styles
document.head.insertAdjacentHTML('beforeend', additionalStyles);

// Initialize when DOM is ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => NetMailArchiver.init());
} else {
    NetMailArchiver.init();
}

// Start performance monitoring
NetMailArchiver.monitorPerformance();

// Set up real-time clock updates
setInterval(() => NetMailArchiver.updateClock(), 1000);

// Export for global access
window.NetMailArchiver = NetMailArchiver;
