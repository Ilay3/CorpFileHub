// CorpFileHub - Файловый менеджер JavaScript
window.corpFileManager = {
    // SignalR соединение
    connection: null,

    // Состояние приложения
    state: {
        isConnected: false,
        currentFolder: null,
        selectedItems: [],
        uploadQueue: [],
        isUploading: false
    },

    // Инициализация
    init: function () {
        console.log('🚀 Инициализация CorpFileHub FileManager...');

        this.setupSignalR();
        this.setupEventHandlers();
        this.setupDragAndDrop();
        this.setupKeyboardShortcuts();

        console.log('✅ FileManager успешно инициализирован');
    },

    // Настройка SignalR соединения
    setupSignalR: function () {
        if (typeof signalR === 'undefined') {
            console.warn('⚠️ SignalR не загружен, пропускаем инициализацию');
            return;
        }

        this.connection = new signalR.HubConnectionBuilder()
            .withUrl("/fileOperationHub")
            .withAutomaticReconnect()
            .build();

        // Обработчики событий SignalR
        this.connection.on("FileUploaded", (data) => {
            console.log('📁 Файл загружен:', data);
            this.showNotification(`Файл "${data.FileName}" успешно загружен`, 'success');
            this.refreshCurrentView();
        });

        this.connection.on("FileDeleted", (data) => {
            console.log('🗑️ Файл удален:', data);
            this.showNotification('Файл успешно удален', 'info');
            this.refreshCurrentView();
        });

        this.connection.on("FolderCreated", (data) => {
            console.log('📂 Папка создана:', data);
            this.showNotification(`Папка "${data.FolderName}" создана`, 'success');
            this.refreshCurrentView();
        });

        this.connection.on("FileOpenedForEditing", (data) => {
            console.log('✏️ Файл открыт для редактирования:', data);
            this.showNotification('Файл открыт для редактирования', 'info');
        });

        this.connection.on("FolderMoved", (data) => {
            console.log('📁 Папка перемещена:', data);
            this.showNotification('Папка успешно перемещена', 'success');
            this.refreshCurrentView();
        });

        this.connection.on("FileVersionRolledBack", (data) => {
            console.log('⏪ Версия файла откачена:', data);
            this.showNotification(`Файл откачен к версии ${data.TargetVersion}`, 'success');
        });

        // Обработка переподключения
        this.connection.onreconnecting(() => {
            this.state.isConnected = false;
            this.showNotification('Переподключение к серверу...', 'warning');
        });

        this.connection.onreconnected(() => {
            this.state.isConnected = true;
            this.showNotification('Соединение с сервером восстановлено', 'success');
        });

        this.connection.onclose(() => {
            this.state.isConnected = false;
            this.showNotification('Соединение с сервером потеряно', 'error');
        });

        // Запуск соединения
        this.connection.start()
            .then(() => {
                this.state.isConnected = true;
                console.log('🔗 SignalR соединение установлено');
            })
            .catch(err => {
                console.error('❌ Ошибка SignalR соединения:', err);
                this.showNotification('Ошибка подключения к серверу', 'error');
            });
    },

    // Настройка обработчиков событий
    setupEventHandlers: function () {
        // Предотвращение стандартного поведения drag & drop в браузере
        ['dragenter', 'dragover', 'dragleave', 'drop'].forEach(eventName => {
            document.addEventListener(eventName, this.preventDefaults, false);
        });

        // Выделение/снятие выделения файлов
        document.addEventListener('click', (e) => {
            const fileItem = e.target.closest('.file-item');
            if (fileItem) {
                this.toggleItemSelection(fileItem, e.ctrlKey || e.metaKey);
            } else if (!e.target.closest('.dropdown-menu')) {
                this.clearSelection();
            }
        });

        // Контекстное меню
        document.addEventListener('contextmenu', (e) => {
            const fileItem = e.target.closest('.file-item');
            if (fileItem) {
                e.preventDefault();
                this.showContextMenu(e, fileItem);
            }
        });

        // Закрытие контекстного меню
        document.addEventListener('click', () => {
            this.hideContextMenu();
        });
    },

    // Настройка Drag & Drop
    setupDragAndDrop: function () {
        const dropzones = document.querySelectorAll('.upload-dropzone, .file-content-panel');

        dropzones.forEach(zone => {
            zone.addEventListener('dragenter', this.handleDragEnter.bind(this));
            zone.addEventListener('dragover', this.handleDragOver.bind(this));
            zone.addEventListener('dragleave', this.handleDragLeave.bind(this));
            zone.addEventListener('drop', this.handleDrop.bind(this));
        });
    },

    // Настройка клавиатурных сокращений
    setupKeyboardShortcuts: function () {
        document.addEventListener('keydown', (e) => {
            // Игнорируем если фокус на поле ввода
            if (e.target.tagName === 'INPUT' || e.target.tagName === 'TEXTAREA') {
                return;
            }

            switch (true) {
                case e.ctrlKey && e.key === 'u': // Ctrl+U - Загрузить файл
                    e.preventDefault();
                    this.triggerFileUpload();
                    break;

                case e.ctrlKey && e.shiftKey && e.key === 'N': // Ctrl+Shift+N - Создать папку
                    e.preventDefault();
                    this.triggerCreateFolder();
                    break;

                case e.key === 'F5': // F5 - Обновить
                    e.preventDefault();
                    this.refreshCurrentView();
                    break;

                case e.key === 'Delete': // Delete - Удалить выбранное
                    e.preventDefault();
                    this.deleteSelectedItems();
                    break;

                case e.ctrlKey && e.key === 'a': // Ctrl+A - Выделить все
                    e.preventDefault();
                    this.selectAllItems();
                    break;

                case e.key === 'Escape': // Escape - Снять выделение
                    this.clearSelection();
                    this.hideContextMenu();
                    break;
            }
        });
    },

    // Обработчики Drag & Drop
    preventDefaults: function (e) {
        e.preventDefault();
        e.stopPropagation();
    },

    handleDragEnter: function (e) {
        if (this.hasFiles(e)) {
            e.currentTarget.classList.add('drag-over');
        }
    },

    handleDragOver: function (e) {
        if (this.hasFiles(e)) {
            e.dataTransfer.dropEffect = 'copy';
        }
    },

    handleDragLeave: function (e) {
        if (!e.currentTarget.contains(e.relatedTarget)) {
            e.currentTarget.classList.remove('drag-over');
        }
    },

    handleDrop: function (e) {
        const dropzone = e.currentTarget;
        dropzone.classList.remove('drag-over');

        if (this.hasFiles(e)) {
            const files = Array.from(e.dataTransfer.files);
            const folderId = this.getCurrentFolderId();
            this.handleFileUpload(files, folderId);
        }
    },

    hasFiles: function (e) {
        return e.dataTransfer.types && e.dataTransfer.types.includes('Files');
    },

    // Работа с файлами
    handleFileUpload: function (files, folderId = 0) {
        if (!files || files.length === 0) return;

        console.log(`📤 Загрузка ${files.length} файлов в папку ${folderId}`);

        // Валидация файлов
        const validFiles = files.filter(file => this.validateFile(file));
        if (validFiles.length === 0) return;

        // Добавляем файлы в очередь загрузки
        this.state.uploadQueue.push(...validFiles.map(file => ({
            file,
            folderId,
            status: 'pending',
            progress: 0
        })));

        // Запускаем загрузку если не идет
        if (!this.state.isUploading) {
            this.processUploadQueue();
        }
    },

    validateFile: function (file) {
        const maxSize = 100 * 1024 * 1024; // 100MB
        const allowedTypes = [
            'application/vnd.openxmlformats-officedocument.wordprocessingml.document', // .docx
            'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet', // .xlsx  
            'application/vnd.openxmlformats-officedocument.presentationml.presentation', // .pptx
            'application/pdf',
            'text/plain',
            'image/jpeg',
            'image/png',
            'image/gif',
            'image/bmp'
        ];

        if (file.size > maxSize) {
            this.showNotification(`Файл "${file.name}" слишком большой (максимум 100 МБ)`, 'error');
            return false;
        }

        if (!allowedTypes.includes(file.type) && !this.isAllowedExtension(file.name)) {
            this.showNotification(`Тип файла "${file.name}" не поддерживается`, 'error');
            return false;
        }

        return true;
    },

    isAllowedExtension: function (filename) {
        const allowedExtensions = ['.docx', '.xlsx', '.pptx', '.pdf', '.txt', '.jpg', '.jpeg', '.png', '.gif', '.bmp'];
        const extension = filename.toLowerCase().substring(filename.lastIndexOf('.'));
        return allowedExtensions.includes(extension);
    },

    processUploadQueue: async function () {
        if (this.state.isUploading || this.state.uploadQueue.length === 0) return;

        this.state.isUploading = true;
        this.showUploadProgress();

        try {
            while (this.state.uploadQueue.length > 0) {
                const uploadItem = this.state.uploadQueue.shift();
                await this.uploadSingleFile(uploadItem);
            }
        } finally {
            this.state.isUploading = false;
            this.hideUploadProgress();
        }
    },

    uploadSingleFile: async function (uploadItem) {
        const formData = new FormData();
        formData.append('file', uploadItem.file);
        formData.append('folderId', uploadItem.folderId);
        formData.append('comment', '');

        try {
            uploadItem.status = 'uploading';
            this.updateUploadProgress(uploadItem, 0);

            const response = await fetch('/api/files/upload', {
                method: 'POST',
                body: formData
            });

            if (response.ok) {
                uploadItem.status = 'completed';
                uploadItem.progress = 100;
                this.updateUploadProgress(uploadItem, 100);

                const result = await response.json();
                console.log('✅ Файл загружен:', result);
            } else {
                throw new Error(`HTTP ${response.status}: ${response.statusText}`);
            }
        } catch (error) {
            console.error('❌ Ошибка загрузки файла:', error);
            uploadItem.status = 'error';
            uploadItem.error = error.message;
            this.showNotification(`Ошибка загрузки "${uploadItem.file.name}": ${error.message}`, 'error');
        }
    },

    // Работа с выделением
    toggleItemSelection: function (item, multiSelect = false) {
        const itemId = item.dataset.itemId;
        if (!itemId) return;

        if (!multiSelect) {
            this.clearSelection();
        }

        const isSelected = item.classList.contains('selected');
        if (isSelected) {
            item.classList.remove('selected');
            this.state.selectedItems = this.state.selectedItems.filter(id => id !== itemId);
        } else {
            item.classList.add('selected');
            this.state.selectedItems.push(itemId);
        }

        this.updateSelectionUI();
    },

    selectAllItems: function () {
        const items = document.querySelectorAll('.file-item');
        this.state.selectedItems = [];

        items.forEach(item => {
            item.classList.add('selected');
            const itemId = item.dataset.itemId;
            if (itemId) {
                this.state.selectedItems.push(itemId);
            }
        });

        this.updateSelectionUI();
    },

    clearSelection: function () {
        const selectedItems = document.querySelectorAll('.file-item.selected');
        selectedItems.forEach(item => item.classList.remove('selected'));
        this.state.selectedItems = [];
        this.updateSelectionUI();
    },

    updateSelectionUI: function () {
        const count = this.state.selectedItems.length;
        const selectionInfo = document.getElementById('selection-info');

        if (selectionInfo) {
            if (count > 0) {
                selectionInfo.textContent = `Выбрано элементов: ${count}`;
                selectionInfo.style.display = 'block';
            } else {
                selectionInfo.style.display = 'none';
            }
        }
    },

    // Контекстное меню
    showContextMenu: function (e, item) {
        this.hideContextMenu();

        const menu = document.createElement('div');
        menu.className = 'context-menu';
        menu.innerHTML = this.getContextMenuHTML(item);

        document.body.appendChild(menu);

        // Позиционирование
        const rect = menu.getBoundingClientRect();
        const x = Math.min(e.clientX, window.innerWidth - rect.width - 10);
        const y = Math.min(e.clientY, window.innerHeight - rect.height - 10);

        menu.style.left = x + 'px';
        menu.style.top = y + 'px';
        menu.style.display = 'block';

        // Обработчики кликов
        menu.addEventListener('click', (e) => {
            e.stopPropagation();
            const action = e.target.dataset.action;
            if (action) {
                this.handleContextMenuAction(action, item);
                this.hideContextMenu();
            }
        });
    },

    hideContextMenu: function () {
        const menu = document.querySelector('.context-menu');
        if (menu) {
            menu.remove();
        }
    },

    getContextMenuHTML: function (item) {
        const isFile = item.classList.contains('file-item');
        const isFolder = item.classList.contains('folder-item');

        let html = '<div class="context-menu-content">';

        if (isFile) {
            html += '<div class="context-menu-item" data-action="download"><i class="bi bi-download"></i> Скачать</div>';
            html += '<div class="context-menu-item" data-action="edit"><i class="bi bi-pencil"></i> Редактировать</div>';
            html += '<div class="context-menu-item" data-action="versions"><i class="bi bi-clock-history"></i> История версий</div>';
            html += '<div class="context-menu-divider"></div>';
        }

        if (isFolder) {
            html += '<div class="context-menu-item" data-action="open"><i class="bi bi-folder-open"></i> Открыть</div>';
            html += '<div class="context-menu-divider"></div>';
        }

        html += '<div class="context-menu-item" data-action="rename"><i class="bi bi-pencil-square"></i> Переименовать</div>';
        html += '<div class="context-menu-item" data-action="move"><i class="bi bi-folder-symlink"></i> Переместить</div>';
        html += '<div class="context-menu-item" data-action="properties"><i class="bi bi-info-circle"></i> Свойства</div>';
        html += '<div class="context-menu-divider"></div>';
        html += '<div class="context-menu-item text-danger" data-action="delete"><i class="bi bi-trash"></i> Удалить</div>';
        html += '</div>';

        return html;
    },

    handleContextMenuAction: function (action, item) {
        const itemId = item.dataset.itemId;
        const itemType = item.classList.contains('file-item') ? 'file' : 'folder';

        console.log(`🔧 Контекстное действие: ${action} для ${itemType} ${itemId}`);

        switch (action) {
            case 'download':
                this.downloadItem(itemId);
                break;
            case 'edit':
                this.editItem(itemId);
                break;
            case 'versions':
                this.showVersionHistory(itemId);
                break;
            case 'open':
                this.openFolder(itemId);
                break;
            case 'rename':
                this.renameItem(itemId, itemType);
                break;
            case 'move':
                this.moveItem(itemId, itemType);
                break;
            case 'properties':
                this.showProperties(itemId, itemType);
                break;
            case 'delete':
                this.deleteItems([itemId], itemType);
                break;
        }
    },

    // API методы
    downloadItem: function (itemId) {
        const link = document.createElement('a');
        link.href = `/api/files/${itemId}/download`;
        link.style.display = 'none';
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);

        this.showNotification('Скачивание началось', 'info');
    },

    editItem: async function (itemId) {
        try {
            const response = await fetch(`/api/files/${itemId}/edit`, {
                method: 'POST'
            });

            if (response.ok) {
                const result = await response.json();
                if (result.editLink) {
                    window.open(result.editLink, '_blank');
                    this.showNotification('Файл открыт для редактирования', 'success');
                }
            } else {
                throw new Error('Не удалось открыть файл для редактирования');
            }
        } catch (error) {
            console.error('Ошибка редактирования:', error);
            this.showNotification('Ошибка при открытии файла', 'error');
        }
    },

    deleteItems: async function (itemIds, itemType = 'file') {
        if (!itemIds || itemIds.length === 0) return;

        const confirmMessage = itemIds.length === 1
            ? `Вы уверены, что хотите удалить этот ${itemType === 'file' ? 'файл' : 'папку'}?`
            : `Вы уверены, что хотите удалить ${itemIds.length} элементов?`;

        if (!confirm(confirmMessage)) return;

        try {
            for (const itemId of itemIds) {
                const endpoint = itemType === 'file' ? `/api/files/${itemId}` : `/api/folders/${itemId}`;
                const response = await fetch(endpoint, { method: 'DELETE' });

                if (!response.ok) {
                    throw new Error(`Не удалось удалить элемент ${itemId}`);
                }
            }

            this.showNotification(itemIds.length === 1 ? 'Элемент удален' : `Удалено элементов: ${itemIds.length}`, 'success');
            this.clearSelection();
        } catch (error) {
            console.error('Ошибка удаления:', error);
            this.showNotification('Ошибка при удалении', 'error');
        }
    },

    deleteSelectedItems: function () {
        if (this.state.selectedItems.length === 0) return;
        this.deleteItems(this.state.selectedItems);
    },

    // Утилиты
    getCurrentFolderId: function () {
        const folderElement = document.querySelector('[data-current-folder-id]');
        return folderElement ? parseInt(folderElement.dataset.currentFolderId) || 0 : 0;
    },

    refreshCurrentView: function () {
        // Вызываем Blazor метод обновления если доступен
        if (window.blazorFileManager && window.blazorFileManager.invokeMethodAsync) {
            window.blazorFileManager.invokeMethodAsync('RefreshView');
        } else {
            // Обновляем страницу как fallback
            location.reload();
        }
    },

    triggerFileUpload: function () {
        const fileInput = document.querySelector('input[type="file"]');
        if (fileInput) {
            fileInput.click();
        }
    },

    triggerCreateFolder: function () {
        if (window.blazorFileManager && window.blazorFileManager.invokeMethodAsync) {
            window.blazorFileManager.invokeMethodAsync('ShowCreateFolderDialog');
        }
    },

    showUploadProgress: function () {
        // Реализация показа прогресса загрузки
        console.log('📊 Показ прогресса загрузки');
    },

    hideUploadProgress: function () {
        // Реализация скрытия прогресса загрузки
        console.log('📊 Скрытие прогресса загрузки');
    },

    updateUploadProgress: function (uploadItem, progress) {
        // Реализация обновления прогресса конкретного файла
        console.log(`📊 Прогресс "${uploadItem.file.name}": ${progress}%`);
    },

    showNotification: function (message, type = 'info') {
        if (window.showNotification) {
            window.showNotification(message, type);
        } else {
            console.log(`${type.toUpperCase()}: ${message}`);
        }
    },

    // Форматирование
    formatFileSize: function (bytes) {
        const sizes = ['B', 'KB', 'MB', 'GB'];
        if (bytes === 0) return '0 B';
        const i = Math.floor(Math.log(bytes) / Math.log(1024));
        return Math.round(bytes / Math.pow(1024, i) * 100) / 100 + ' ' + sizes[i];
    }
};

// Стили для контекстного меню
const contextMenuStyles = `
.context-menu {
    position: fixed;
    background: white;
    border: 2px solid #ffc107;
    border-radius: 8px;
    box-shadow: 0 8px 32px rgba(255, 193, 7, 0.3);
    z-index: 9999;
    min-width: 200px;
    display: none;
}

.context-menu-content {
    padding: 0.5rem 0;
}

.context-menu-item {
    padding: 0.5rem 1rem;
    cursor: pointer;
    display: flex;
    align-items: center;
    gap: 0.5rem;
    transition: all 0.2s ease;
    font-size: 0.9rem;
}

.context-menu-item:hover {
    background: rgba(255, 193, 7, 0.1);
    color: #333;
}

.context-menu-item.text-danger:hover {
    background: rgba(220, 53, 69, 0.1);
    color: #dc3545;
}

.context-menu-divider {
    height: 1px;
    background: linear-gradient(90deg, transparent, rgba(255, 193, 7, 0.3), transparent);
    margin: 0.5rem 0;
}

.file-item.selected {
    background: rgba(255, 193, 7, 0.2) !important;
    border-color: #ffc107 !important;
    transform: translateY(-2px);
    box-shadow: 0 4px 15px rgba(255, 193, 7, 0.3);
}
`;

// Добавляем стили в документ
if (!document.getElementById('context-menu-styles')) {
    const styleSheet = document.createElement('style');
    styleSheet.id = 'context-menu-styles';
    styleSheet.textContent = contextMenuStyles;
    document.head.appendChild(styleSheet);
}

// Автоинициализация при загрузке DOM
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => {
        window.corpFileManager.init();
    });
} else {
    window.corpFileManager.init();
}