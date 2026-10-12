// AcxiomCRM Global JavaScript Utilities

document.addEventListener('DOMContentLoaded', function () {
    // 1. Mobile Sidebar Toggle
    const sidebarToggle = document.getElementById('sidebarToggle');
    const sidebar = document.getElementById('crmSidebar');
    if (sidebarToggle && sidebar) {
        sidebarToggle.addEventListener('click', function () {
            sidebar.classList.toggle('show');
        });
    }

    // 2. Global Search Shortcut (Press '/' or 'Ctrl+K' to open search)
    document.addEventListener('keydown', function (e) {
        if ((e.key === '/' || (e.ctrlKey && e.key === 'k')) && 
            document.activeElement.tagName !== 'INPUT' && 
            document.activeElement.tagName !== 'TEXTAREA') {
            e.preventDefault();
            const searchModal = document.getElementById('globalSearchModal');
            if (searchModal) {
                const modal = new bootstrap.Modal(searchModal);
                modal.show();
                setTimeout(() => {
                    document.getElementById('globalSearchInput')?.focus();
                }, 300);
            }
        }
    });

    // 3. Password Show/Hide Toggle
    const pwdToggles = document.querySelectorAll('.toggle-password');
    pwdToggles.forEach(btn => {
        btn.addEventListener('click', function () {
            const targetId = this.getAttribute('data-target');
            const input = document.getElementById(targetId);
            const icon = this.querySelector('i');
            if (input) {
                if (input.type === 'password') {
                    input.type = 'text';
                    if (icon) {
                        icon.classList.remove('bi-eye');
                        icon.classList.add('bi-eye-slash');
                    }
                } else {
                    input.type = 'password';
                    if (icon) {
                        icon.classList.remove('bi-eye-slash');
                        icon.classList.add('bi-eye');
                    }
                }
            }
        });
    });

    // 4. Auto-dismiss alerts after 5 seconds
    const alerts = document.querySelectorAll('.alert-dismissible');
    alerts.forEach(alert => {
        setTimeout(() => {
            const bsAlert = bootstrap.Alert.getOrCreateInstance(alert);
            if (bsAlert) bsAlert.close();
        }, 6000);
    });

    // 5. Delete Confirmation Modal Hook
    const deleteModal = document.getElementById('deleteConfirmModal');
    if (deleteModal) {
        deleteModal.addEventListener('show.bs.modal', function (event) {
            const button = event.relatedTarget;
            const actionUrl = button.getAttribute('data-action');
            const itemName = button.getAttribute('data-name');
            const itemType = button.getAttribute('data-type') || 'item';

            const form = deleteModal.querySelector('#deleteConfirmForm');
            const nameSpan = deleteModal.querySelector('#deleteItemName');
            const typeSpan = deleteModal.querySelector('#deleteItemType');

            if (form) form.action = actionUrl;
            if (nameSpan) nameSpan.textContent = itemName;
            if (typeSpan) typeSpan.textContent = itemType;
        });
    }

    // 6. Global Search Live Filter
    const globalInput = document.getElementById('globalSearchInput');
    const globalResults = document.getElementById('globalSearchResults');
    if (globalInput && globalResults) {
        globalInput.addEventListener('input', function () {
            const term = this.value.toLowerCase().trim();
            const items = globalResults.querySelectorAll('.search-result-item');
            let foundCount = 0;

            items.forEach(item => {
                const text = item.textContent.toLowerCase();
                if (term === '' || text.includes(term)) {
                    item.style.display = 'flex';
                    foundCount++;
                } else {
                    item.style.display = 'none';
                }
            });

            const emptyState = document.getElementById('searchEmptyState');
            if (emptyState) {
                emptyState.style.display = foundCount === 0 ? 'block' : 'none';
            }
        });
    }
});
