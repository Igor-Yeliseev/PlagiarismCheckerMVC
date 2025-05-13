document.addEventListener('DOMContentLoaded', () => {
    // Проверяем авторизацию
    const token = localStorage.getItem('token');
    if (!token) {
        window.location.href = '/login.html';
        return;
    }

    const documentsContainer = document.getElementById('documents-container');
    const loadingMessage = document.getElementById('loading-message');
    const fileInput = document.getElementById('document-file');
    const uploadDocumentBtn = document.getElementById('upload-document-btn');
    const selectedFileModal = document.getElementById('selected-file-modal');
    const fileNameElement = document.getElementById('file-name');
    const closeFileBtn = document.getElementById('close-file-btn');
    const submitDocumentBtn = document.getElementById('submit-document');
    const uploadSpinner = document.getElementById('upload-spinner');
    const confirmModal = document.getElementById('confirm-modal');
    const yesBtn = document.getElementById('yes-btn');
    const noBtn = document.getElementById('no-btn');

    let documentToDelete = null;

    // Показываем сообщение о загрузке документов
    showLoadingMessage();
    
    // Загружаем документы пользователя
    loadDocuments();

    // Обработчик для кнопки загрузки
    uploadDocumentBtn.addEventListener('click', () => {
        fileInput.click();
    });

    // Обработчик выбора файла
    fileInput.addEventListener('change', function (e) {
        if (this.files.length > 0) {
            const file = this.files[0];
            fileNameElement.textContent = file.name;
            selectedFileModal.style.display = 'block';
        } else {
            selectedFileModal.style.display = 'none';
        }
    });

    // Обработчик кнопки закрытия блока с файлом
    closeFileBtn.addEventListener('click', () => {
        selectedFileModal.style.display = 'none';
        fileInput.value = '';
    });

    // Обработчик отправки документа в базу
    submitDocumentBtn.addEventListener('click', uploadDocument);
    
    // Функция для отображения сообщения о загрузке
    function showLoadingMessage() {
        if (loadingMessage) {
            loadingMessage.style.display = 'flex';
        }
    }
    
    // Функция для скрытия сообщения о загрузке
    function hideLoadingMessage() {
        if (loadingMessage) {
            loadingMessage.style.display = 'none';
        }
    }

    // Функция для загрузки документов
    async function loadDocuments() {
        try {
            const response = await fetch('/plag-api/documents', {
                method: 'GET',
                headers: {
                    'Authorization': `Bearer ${token}`
                }
            });

            if (response.ok) {
                const documents = await response.json();
                hideLoadingMessage(); // Скрываем сообщение о загрузке
                renderDocuments(documents);
            }
            else {
                // В случае ошибки загрузки просто отображаем пустую таблицу
                console.error('Ошибка загрузки документов, код: ', response.status);
                hideLoadingMessage(); // Скрываем сообщение о загрузке
                renderDocuments([]);
            }
        }
        catch (error) {
            // При любой ошибке просто показываем пустую таблицу
            console.error('Ошибка:', error);
            hideLoadingMessage(); // Скрываем сообщение о загрузке
            renderDocuments([]);
        }
    }

    function addEmptyMessage(tbody) {
        const emptyRow = document.createElement('tr');
        emptyRow.innerHTML = `<td colspan="4" class="empty-message">У вас пока нет загруженных документов</td>`;
        tbody.appendChild(emptyRow);
    }

    // Отображение документов
    function renderDocuments(documents) {
        documentsContainer.innerHTML = '';

        // Создаем таблицу всегда, независимо от наличия документов
        const table = document.createElement('table');
        table.classList.add('documents-table');

        const thead = document.createElement('thead');
        thead.innerHTML = `
            <tr>
                <th>Номер</th>
                <th>Имя файла</th>
                <th>Процент уникальности</th>
                <th>Дата загрузки</th>
                <th class="actions-column"></th>
            </tr>
        `;

        const tbody = document.createElement('tbody');

        if (documents.length === 0) {
            // Если документов нет, добавляем строку с сообщением
            addEmptyMessage(tbody);
        }
        else {
            documents.forEach((doc, index) => {
                const row = document.createElement('tr');

                const formatDate = (dateString) => {
                    const date = new Date(dateString);
                    return date.toLocaleDateString('ru-RU') + ' ' + date.toLocaleTimeString('ru-RU');
                };

                // Создаем данные строки с отдельной ячейкой для кнопок
                row.innerHTML = `
                    <td>${index + 1}</td>
                    <td>${doc.name}</td>
                    <td>${doc.similarity}%</td>
                    <td>${formatDate(doc.uploadDate)}</td>
                    <td class="actions-cell">
                        <div class="record-actions">
                            <button class="action-check-doc" data-id="${doc.id}" title="Проверить документ на плагиат">Проверить</button>
                            <button class="action-delete" data-id="${doc.id}" title="Удалить документ">
                                <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                                    <polyline points="3 6 5 6 21 6"></polyline>
                                    <path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"></path>
                                    <line x1="10" y1="11" x2="10" y2="17"></line>
                                    <line x1="14" y1="11" x2="14" y2="17"></line>
                                </svg>
                            </button>
                        </div>
                    </td>
                `;

                tbody.appendChild(row);
            });
        }

        table.appendChild(thead);
        table.appendChild(tbody);
        documentsContainer.appendChild(table);

        // Добавляем обработчики только если есть документы
        if (documents.length > 0) {
            // Добавляем обработчики событий для кнопок
            document.querySelectorAll('.action-check-doc').forEach(btn => {
                btn.addEventListener('click', () => checkUploaded(btn.dataset.id));
            });

            document.querySelectorAll('.action-delete').forEach(btn => {
                btn.addEventListener('click', () => confirmDelete(btn.dataset.id));
            });
        }
    }

    // Загрузка документа
    async function uploadDocument() {
        const file = fileInput.files[0];

        if (!file) {
            showNotification('Выберите файл для загрузки', 'error');
            return;
        }

        if (!file.name.endsWith('.docx')) {
            showNotification('Можно загружать только файлы формата .docx', 'error');
            return;
        }

        // Показываем спиннер и блокируем кнопку отправки
        uploadSpinner.style.display = 'flex';
        submitDocumentBtn.disabled = true;

        try {
            const formData = new FormData();
            formData.append('file', file);

            const currentToken = localStorage.getItem('token');

            if (!currentToken) {
                throw new Error('Ошибка авторизации. Пожалуйста, войдите в систему заново.');
            }

            const response = await fetch('/plag-api/add-doc', {
                method: 'POST',
                headers: {
                    // 'Authorization': `Bearer ${token}`
                    'Authorization': `Bearer ${currentToken}`
                },
                body: formData
            });

            if (!response.ok) {
                throw new Error('Ошибка при загрузке файла');
            }

            // После успешной загрузки перезагружаем список документов
            selectedFileModal.style.display = 'none';
            fileInput.value = '';
            
            // Показываем сообщение о загрузке документов
            showLoadingMessage();
            
            // Перезагружаем список документов
            loadDocuments();
            showNotification('Документ успешно загружен', 'success');
        }
        catch (error) {
            console.error('Ошибка:', error);
            showNotification(error.message, 'error');
        }
        finally {
            // Скрываем спиннер и разблокируем кнопку
            uploadSpinner.style.display = 'none';
            submitDocumentBtn.disabled = false;
        }
    }

    // Переход на страницу проверки документа
    function checkUploaded(documentId) {
        window.location.href = `/check-uploaded.html?id=${documentId}`;
    }

    // Показ модального окна подтверждения удаления
    function confirmDelete(documentId) {
        documentToDelete = documentId;
        confirmModal.style.display = 'flex';

        // Добавляем обработчики кнопок в модальном окне
        yesBtn.addEventListener('click', deleteDocument);
        noBtn.addEventListener('click', cancelDelete);
    }

    // Удаление документа
    async function deleteDocument() {
        try {
            if (!documentToDelete) return;

            const response = await fetch(`/plag-api/documents/${documentToDelete}`, {
                method: 'DELETE',
                headers: {
                    'Authorization': `Bearer ${token}`
                }
            });

            if (response.ok) {
                // После успешного удаления перезагружаем список документов
                showLoadingMessage(); // Показываем сообщение о загрузке
                loadDocuments();
                showNotification('Документ успешно удален', 'success');
            }
            else {
                const errorData = await response.json();
                throw new Error(errorData.message || 'Ошибка при удалении документа');
            }
        }
        catch (error) {
            console.error('Ошибка:', error);
            showNotification(error.message, 'error');
        }
        finally {
            cancelDelete(); // Скрываем модальное окно
        }
    }

    // Отмена удаления (закрытие модального окна)
    function cancelDelete() {
        confirmModal.style.display = 'none';
        documentToDelete = null;

        // Удаляем обработчики, чтобы избежать множественного назначения
        yesBtn.removeEventListener('click', deleteDocument);
        noBtn.removeEventListener('click', cancelDelete);
    }

    // Функция для отображения уведомлений
    function showNotification(message, type) {
        const notification = document.createElement('div');
        notification.className = `notification notification-${type}`;
        notification.textContent = message;

        document.body.appendChild(notification);

        // Удаляем уведомление через 3 секунды
        setTimeout(() => {
            notification.classList.add('hiding');
            setTimeout(() => {
                document.body.removeChild(notification);
            }, 500);
        }, 3000);
    }
}); 