// HealthTrack client-side helpers
(function () {
    'use strict';

    // Mobile sidebar toggle
    document.addEventListener('click', function (e) {
        const toggle = e.target.closest('[data-toggle-sidebar]');
        const sidebar = document.getElementById('sidebar');
        if (!sidebar) return;
        if (toggle) { sidebar.classList.toggle('open'); return; }
        if (sidebar.classList.contains('open') && !e.target.closest('#sidebar')) sidebar.classList.remove('open');
    });

    // Confirmation for destructive actions: <form data-confirm="Are you sure?">
    document.addEventListener('submit', function (e) {
        const form = e.target;
        const message = form.getAttribute('data-confirm');
        if (message && !window.confirm(message)) e.preventDefault();
    });

    // Auto-hide success messages after a few seconds
    setTimeout(function () {
        document.querySelectorAll('.flash.alert-success').forEach(function (el) {
            el.style.transition = 'opacity .4s'; el.style.opacity = '0';
            setTimeout(function () { el.remove(); }, 400);
        });
    }, 5000);
})();

/**
 * Adds / removes repeating form rows (plan exercises, diet items) and keeps the
 * MVC model-binding indexes (Items[0], Items[1] ...) continuous.
 */
function initDynamicRows(containerId, templateId, addButtonId) {
    const container = document.getElementById(containerId);
    const template = document.getElementById(templateId);
    const addButton = document.getElementById(addButtonId);
    if (!container || !template || !addButton) return;

    function reindex() {
        container.querySelectorAll('.dynamic-row').forEach(function (row, index) {
            row.querySelectorAll('[name]').forEach(function (el) {
                el.name = el.name.replace(/\[\d+\]/, '[' + index + ']');
                if (el.id) el.id = el.id.replace(/_\d+__/, '_' + index + '__');
            });
            row.querySelectorAll('[data-valmsg-for]').forEach(function (el) {
                el.setAttribute('data-valmsg-for', el.getAttribute('data-valmsg-for').replace(/\[\d+\]/, '[' + index + ']'));
            });
            row.querySelectorAll('label[for]').forEach(function (el) {
                el.htmlFor = el.htmlFor.replace(/_\d+__/, '_' + index + '__');
            });
            const number = row.querySelector('.row-number');
            if (number) number.textContent = index + 1;
        });
    }

    addButton.addEventListener('click', function () {
        const index = container.querySelectorAll('.dynamic-row').length;
        container.insertAdjacentHTML('beforeend', template.innerHTML.replace(/__i__/g, index));
        reindex();
        const rows = container.querySelectorAll('.dynamic-row');
        const firstInput = rows[rows.length - 1].querySelector('input, select');
        if (firstInput) firstInput.focus();
    });

    container.addEventListener('click', function (e) {
        const button = e.target.closest('.remove-row');
        if (!button) return;
        if (container.querySelectorAll('.dynamic-row').length <= 1) {
            alert('A plan needs at least one item.');
            return;
        }
        button.closest('.dynamic-row').remove();
        reindex();
    });
}

/** Shared Chart.js defaults so every chart matches the UI. */
function applyChartDefaults() {
    if (!window.Chart) return;
    Chart.defaults.font.family = '"Manrope", "Segoe UI", system-ui, sans-serif';
    Chart.defaults.color = '#5f6f6a';
    Chart.defaults.plugins.legend.labels.boxWidth = 12;
    Chart.defaults.maintainAspectRatio = false;
}
