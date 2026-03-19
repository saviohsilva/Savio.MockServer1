/**
 * Savio Mock Server - JavaScript Principal
 * Funcoes utilitarias e interacoes do cliente
 */

(function () {
    'use strict';

    // Aplica o tema salvo antes de qualquer render para evitar flash
    (function applyStoredTheme() {
        var theme = localStorage.getItem('savio-theme');
        if (theme) {
            document.documentElement.setAttribute('data-theme', theme);
        }
    })();

    document.addEventListener('DOMContentLoaded', function () {
        console.log('Savio Mock Server - Inicializado');
        initializeTooltips();
        initializeSidebarToggle();
    });

    function initializeTooltips() {
        var tooltipTriggerList = [].slice.call(
            document.querySelectorAll('[data-bs-toggle="tooltip"]')
        );
        if (typeof bootstrap !== 'undefined' && bootstrap.Tooltip) {
            tooltipTriggerList.map(function (el) {
                return new bootstrap.Tooltip(el);
            });
        }
    }

    function initializeSidebarToggle() {
        var sidebar = document.querySelector('.sidebar');
        var toggleButton = document.querySelector('.navbar-toggler');

        if (!sidebar || !toggleButton) return;

        if (window.innerWidth < 768) {
            sidebar.classList.add('collapse');
        }

        toggleButton.addEventListener('click', function (event) {
            event.stopPropagation();
            sidebar.classList.toggle('collapse');
        });

        document.addEventListener('click', function (event) {
            if (window.innerWidth < 768) {
                var isClickInsideSidebar = sidebar.contains(event.target);
                var isToggleButton = toggleButton.contains(event.target);
                if (!isClickInsideSidebar && !isToggleButton && !sidebar.classList.contains('collapse')) {
                    sidebar.classList.add('collapse');
                }
            }
        });

        sidebar.querySelectorAll('.nav-link').forEach(function (link) {
            link.addEventListener('click', function () {
                if (window.innerWidth < 768) {
                    sidebar.classList.add('collapse');
                }
            });
        });

        window.addEventListener('resize', function () {
            if (window.innerWidth >= 768) {
                sidebar.classList.remove('collapse');
            } else {
                sidebar.classList.add('collapse');
            }
        });
    }

    // ── Funcoes expostas para Blazor JS Interop ────────────────────────────

    window.confirmAction = function (message) {
        return confirm(message);
    };

    window.copyToClipboard = async function (text) {
        try {
            await navigator.clipboard.writeText(text);
            showNotification('Copiado!', 'success');
        } catch (err) {
            console.error('Erro ao copiar:', err);
            showNotification('Erro ao copiar texto', 'error');
        }
    };

    window.formatJson = function (jsonString) {
        try {
            return JSON.stringify(JSON.parse(jsonString), null, 2);
        } catch (e) {
            return jsonString;
        }
    };

    window.openInNewTab = function (url) {
        window.open(url, '_blank');
    };

    window.setTheme = function (theme) {
        if (theme) {
            document.documentElement.setAttribute('data-theme', theme);
            localStorage.setItem('savio-theme', theme);
        } else {
            document.documentElement.removeAttribute('data-theme');
            localStorage.removeItem('savio-theme');
        }
    };

    window.getTheme = function () {
        return localStorage.getItem('savio-theme') || '';
    };

    window.getBrowserTimezoneOffsetMinutes = function () {
        return new Date().getTimezoneOffset();
    };

    // ── Utilitario interno ─────────────────────────────────────────────────

    function showNotification(message, type) {
        type = type || 'info';
        var notification = document.createElement('div');
        notification.className = 'alert alert-' + type + ' position-fixed top-0 end-0 m-3';
        notification.style.zIndex = '9999';
        notification.textContent = message;
        document.body.appendChild(notification);
        setTimeout(function () {
            notification.remove();
        }, 3000);
    }

})();
