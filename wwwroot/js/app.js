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
        initializeResizableTables();
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
        var page = document.querySelector('.page');

        if (!sidebar || !page) return;

        // Remove stale offcanvas class from older behavior.
        sidebar.classList.remove('collapse');

        sidebar.querySelectorAll('.nav-link').forEach(function (link) {
            link.addEventListener('click', function () {
                if (window.innerWidth < 768) {
                    window.setSidebarCollapsedState(true);
                }
            });
        });

        window.addEventListener('resize', function () {
            if (window.innerWidth >= 768) {
                sidebar.classList.remove('collapse');
            }
        });
    }

    function initializeResizableTables() {
        document.querySelectorAll('table.table-resizable').forEach(function (table) {
            if (table.dataset.resizeInitialized === 'true') return;
            table.dataset.resizeInitialized = 'true';

            var headerRow = table.querySelector('thead tr');
            if (!headerRow) return;

            var headers = Array.from(headerRow.querySelectorAll('th'));
            var bodyRows = Array.from(table.querySelectorAll('tbody tr'));

            headers.forEach(function (header, columnIndex) {
                if (header.querySelector('.column-resize-handle')) return;

                var handle = document.createElement('span');
                handle.className = 'column-resize-handle';
                handle.title = 'Arraste para redimensionar';
                header.appendChild(handle);

                handle.addEventListener('pointerdown', function (event) {
                    event.preventDefault();
                    event.stopPropagation();

                    var startX = event.clientX;
                    var startWidth = header.getBoundingClientRect().width;
                    var minWidth = Math.max(72, parseFloat(window.getComputedStyle(header).minWidth) || 72);

                    header.classList.add('is-resizing');
                    document.body.classList.add('is-resizing-columns');

                    function applyWidth(width) {
                        var nextWidth = Math.max(minWidth, width);
                        header.style.width = nextWidth + 'px';
                        header.style.minWidth = nextWidth + 'px';

                        bodyRows.forEach(function (row) {
                            var cell = row.children[columnIndex];
                            if (cell) {
                                cell.style.width = nextWidth + 'px';
                                cell.style.minWidth = nextWidth + 'px';
                            }
                        });

                        table.style.minWidth = Math.max(table.offsetWidth, table.getBoundingClientRect().width) + 'px';
                    }

                    function onPointerMove(moveEvent) {
                        applyWidth(startWidth + (moveEvent.clientX - startX));
                    }

                    function onPointerUp() {
                        document.removeEventListener('pointermove', onPointerMove);
                        document.removeEventListener('pointerup', onPointerUp);
                        header.classList.remove('is-resizing');
                        document.body.classList.remove('is-resizing-columns');
                    }

                    document.addEventListener('pointermove', onPointerMove);
                    document.addEventListener('pointerup', onPointerUp, { once: true });
                });
            });
        });
    }

    // ── Funcoes expostas para Blazor JS Interop ────────────────────────────

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

    window.getSidebarCollapsedState = function () {
        return localStorage.getItem('savio-sidebar-collapsed') === 'true';
    };

    window.setSidebarCollapsedState = function (collapsed) {
        var sidebar = document.querySelector('.sidebar');
        var page = document.querySelector('.page');

        if (sidebar) {
            sidebar.classList.toggle('collapse', false);
        }

        if (page) {
            page.classList.toggle('sidebar-collapsed', !!collapsed);
        }

        if (sidebar) {
            sidebar.classList.toggle('is-collapsed', !!collapsed);
        }

        localStorage.setItem('savio-sidebar-collapsed', collapsed ? 'true' : 'false');
    };

    window.toggleSidebarMenu = function (collapsed) {
        window.setSidebarCollapsedState(collapsed);
    };

    window.initializeResizableTables = initializeResizableTables;

    window.getBrowserTimezoneOffsetMinutes = function () {
        return new Date().getTimezoneOffset();
    };

    window.hardNavigate = function (url) {
        window.location.href = url;
    };

    window.formNavigate = function (url) {
        var form = document.createElement('form');
        form.method = 'get';
        form.action = url;
        document.body.appendChild(form);
        form.submit();
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
