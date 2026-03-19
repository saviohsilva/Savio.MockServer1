/**
 * Savio Mock Server - JavaScript Principal
 * Funções utilitárias e interações do cliente
 */

(function () {
    'use strict';

    /**
     * Inicialização quando o DOM está pronto
     */
    document.addEventListener('DOMContentLoaded', function () {
        console.log('?? Savio Mock Server - Inicializado');
        initializeTooltips();
        initializeSidebarToggle();
    });

    /**
     * Inicializa tooltips do Bootstrap (se necessário)
     */
    function initializeTooltips() {
        const tooltipTriggerList = [].slice.call(
            document.querySelectorAll('[data-bs-toggle="tooltip"]')
        );
        
        if (typeof bootstrap !== 'undefined' && bootstrap.Tooltip) {
            tooltipTriggerList.map(function (tooltipTriggerEl) {
                return new bootstrap.Tooltip(tooltipTriggerEl);
            });
        }
    }

    /**
     * Gerencia o toggle do sidebar em dispositivos móveis
     */
    function initializeSidebarToggle() {
        const sidebar = document.querySelector('.sidebar');
        const toggleButton = document.querySelector('.navbar-toggler');
        
        if (!sidebar || !toggleButton) return;

        // Inicia sidebar fechado em mobile
        if (window.innerWidth < 768) {
            sidebar.classList.add('collapse');
        }

        // Toggle do sidebar
        toggleButton.addEventListener('click', function (event) {
            event.stopPropagation();
            sidebar.classList.toggle('collapse');
        });

        // Fecha sidebar ao clicar fora dele em mobile
        document.addEventListener('click', function (event) {
            if (window.innerWidth < 768) {
                const isClickInsideSidebar = sidebar.contains(event.target);
                const isToggleButton = toggleButton.contains(event.target);
                
                if (!isClickInsideSidebar && !isToggleButton && !sidebar.classList.contains('collapse')) {
                    sidebar.classList.add('collapse');
                }
            }
        });

        // Fecha sidebar ao navegar em mobile
        const navLinks = sidebar.querySelectorAll('.nav-link');
        navLinks.forEach(function (link) {
            link.addEventListener('click', function () {
                if (window.innerWidth < 768) {
                    sidebar.classList.add('collapse');
                }
            });
        });

        // Gerencia visibilidade ao redimensionar janela
        window.addEventListener('resize', function () {
            if (window.innerWidth >= 768) {
                sidebar.classList.remove('collapse');
            } else {
                sidebar.classList.add('collapse');
            }
        });
    }

    /**
     * Confirma ação com o usuário
     * @param {string} message - Mensagem de confirmação
     * @returns {boolean} - Resultado da confirmação
     */
    window.confirmAction = function (message) {
        return confirm(message);
    };

    /**
     * Copia texto para a área de transferência
     * @param {string} text - Texto a ser copiado
     */
    window.copyToClipboard = async function (text) {
        try {
            await navigator.clipboard.writeText(text);
            showNotification('Copiado para área de transferência!', 'success');
        } catch (err) {
            console.error('Erro ao copiar:', err);
            showNotification('Erro ao copiar texto', 'error');
        }
    };

    /**
     * Exibe notificação temporária
     * @param {string} message - Mensagem da notificação
     * @param {string} type - Tipo: success, error, warning, info
     */
    function showNotification(message, type = 'info') {
        const notification = document.createElement('div');
        notification.className = `alert alert-${type} position-fixed top-0 end-0 m-3 fade-in`;
        notification.style.zIndex = '9999';
        notification.textContent = message;
        
        document.body.appendChild(notification);
        
        setTimeout(function () {
            notification.remove();
        }, 3000);
    }

    /**
     * Formata JSON para exibição
     * @param {string} jsonString - String JSON
     * @returns {string} - JSON formatado
     */
    window.formatJson = function (jsonString) {
        try {
            const obj = JSON.parse(jsonString);
            return JSON.stringify(obj, null, 2);
        } catch (e) {
            return jsonString;
        }
    };

    /**
     * Abre URL em nova aba
     * @param {string} url - URL a ser aberta
     */
    window.openInNewTab = function (url) {
        window.open(url, '_blank');
    };

    /**
     * Tema: aplica o tema salvo no localStorage ao carregar
     */
    (function applyStoredTheme() {
        var theme = localStorage.getItem('savio-theme');
        if (theme) {
            document.documentElement.setAttribute('data-theme', theme);
        }
    })();

    /**
     * Altera o tema da aplicação e persiste em localStorage
     * @param {string} theme - Nome do tema (vazio para padrão)
     */
    window.setTheme = function (theme) {
        if (theme) {
            document.documentElement.setAttribute('data-theme', theme);
            localStorage.setItem('savio-theme', theme);
        } else {
            document.documentElement.removeAttribute('data-theme');
            localStorage.removeItem('savio-theme');
        }
    };

    /**
     * Retorna o tema atual
     * @returns {string} Nome do tema ou '' se padrão
     */
    window.getTheme = function () {
        return localStorage.getItem('savio-theme') || '';
    };

    /**
     * Retorna o offset de fuso horário do navegador em minutos (igual a Date.getTimezoneOffset).
     * Valores positivos = oeste de UTC (ex.: UTC-3 retorna 180).
     * @returns {number}
     */
    window.getBrowserTimezoneOffsetMinutes = function () {
        return new Date().getTimezoneOffset();
    };

})();
