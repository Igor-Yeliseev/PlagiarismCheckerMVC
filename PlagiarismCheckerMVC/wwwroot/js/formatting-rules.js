document.addEventListener('DOMContentLoaded', function() {
    // Проверяем, является ли пользователь администратором
    if (isUserAdmin()) {
        // Если администратор, создаем блок правил оформления
        createFormattingRulesBlock();
    }
});

/** Создает блок правил оформления для администраторов */
function createFormattingRulesBlock() {
    const container = document.getElementById('formatting-rules-container');
    if (!container) return;

    // Создаем структуру блока
    const formattingRulesBlock = document.createElement('div');
    formattingRulesBlock.className = 'formatting-rules-block';
    
    // Заголовок блока с кнопкой сворачивания/разворачивания
    const header = document.createElement('div');
    header.className = 'formatting-rules-header';
    
    const title = document.createElement('h3');
    title.className = 'formatting-rules-title';
    title.textContent = 'Правила оформления';
    
    const toggleButton = document.createElement('button');
    toggleButton.className = 'formatting-rules-toggle';
    toggleButton.innerHTML = '&#9650;'; // Unicode-символ стрелки вверх
    toggleButton.setAttribute('aria-label', 'Свернуть/развернуть блок правил');
    
    header.appendChild(title);
    header.appendChild(toggleButton);
    
    // Содержимое блока
    const content = document.createElement('div');
    content.className = 'formatting-rules-content';
    
    // Поля для правил
    const fieldsContainer = document.createElement('div');
    fieldsContainer.className = 'formatting-rules-fields';
    
    // Примерные поля для правил (будут заменены на реальные позже)
    const rulesFields = [
        { id: 'fontFamily', label: 'Шрифт основного текста', type: 'text', defaultValue: 'Times New Roman' },
        { id: 'fontSize', label: 'Размер шрифта основного текста', type: 'number', defaultValue: '14' },
        { id: 'headingFontSize', label: 'Размер шрифта заголовков', type: 'number', defaultValue: '16' },
        { id: 'lineSpacing', label: 'Межстрочный интервал', type: 'number', step: '0.1', defaultValue: '1.5' },
        { id: 'paragraphIndent', label: 'Отступ абзаца (см)', type: 'number', step: '0.1', defaultValue: '1.25' },
        { id: 'margins', label: 'Поля страницы (см)', type: 'text', defaultValue: '2 2 2 2' }
    ];
    
    // Создаем поля для каждого правила
    rulesFields.forEach(field => {
        const fieldContainer = document.createElement('div');
        fieldContainer.className = 'formatting-rules-field';
        
        const label = document.createElement('label');
        label.setAttribute('for', field.id);
        label.textContent = field.label;
        
        const input = document.createElement('input');
        input.setAttribute('type', field.type);
        input.setAttribute('id', field.id);
        input.setAttribute('name', field.id);
        input.setAttribute('value', field.defaultValue);
        
        if (field.step) {
            input.setAttribute('step', field.step);
        }
        
        // Слушатель для отслеживания изменений
        input.addEventListener('input', checkForChanges);
        
        fieldContainer.appendChild(label);
        fieldContainer.appendChild(input);
        fieldsContainer.appendChild(fieldContainer);
    });
    
    content.appendChild(fieldsContainer);
    
    // Контейнер для кнопок
    const actionsContainer = document.createElement('div');
    actionsContainer.className = 'formatting-rules-actions';
    
    // Кнопка "Применить"
    const applyButton = document.createElement('button');
    applyButton.className = 'formatting-rules-apply-btn';
    applyButton.textContent = 'Применить';
    applyButton.setAttribute('id', 'apply-formatting-rules');
    
    // Создаем скрытый input для выбора файла
    const fileInput = document.createElement('input');
    fileInput.setAttribute('type', 'file');
    fileInput.setAttribute('id', 'word-document-upload');
    fileInput.setAttribute('accept', '.docx');
    fileInput.style.display = 'none';
    
    // Кнопка "Задать через документ"
    const uploadDocButton = document.createElement('button');
    uploadDocButton.className = 'formatting-rules-upload-btn';
    uploadDocButton.textContent = 'Задать через документ';
    uploadDocButton.setAttribute('id', 'upload-document-rules');
    
    // Добавляем кнопки и input в контейнер
    actionsContainer.appendChild(applyButton);
    actionsContainer.appendChild(uploadDocButton);
    actionsContainer.appendChild(fileInput);
    
    content.appendChild(actionsContainer);
    
    // Добавляем все элементы в блок
    formattingRulesBlock.appendChild(header);
    formattingRulesBlock.appendChild(content);
    
    // Добавляем блок в контейнер
    container.appendChild(formattingRulesBlock);
    
    // Создаем модальное окно подтверждения
    const confirmModal = document.getElementById('confirm-modal');
    const yesBtn = document.getElementById('yes-btn');
    const noBtn = document.getElementById('no-btn');
    let selectedFile = null;
    
    // Обработчик для кнопки сворачивания/разворачивания
    toggleButton.addEventListener('click', function() {
        content.classList.toggle('collapsed');
        toggleButton.classList.toggle('collapsed');
        
        // Меняем символ в зависимости от состояния
        if (content.classList.contains('collapsed')) {
            toggleButton.innerHTML = '&#9660;'; // Unicode-символ стрелки вниз
        } else {
            toggleButton.innerHTML = '&#9650;'; // Unicode-символ стрелки вверх
        }
    });
    
    // Обработчик для кнопки "Применить"
    applyButton.addEventListener('click', function() {
        if (!applyButton.classList.contains('active')) return;
        
        // Собираем данные из полей
        const formattingRules = {};
        rulesFields.forEach(field => {
            const input = document.getElementById(field.id);
            formattingRules[field.id] = input.value;
        });
        
        // Отправляем данные на сервер
        sendFormattingRules(formattingRules);
    });
    
    // Обработчик для кнопки "Задать через документ"
    uploadDocButton.addEventListener('click', function() {
        // Активируем скрытое поле для выбора файла
        fileInput.click();
    });
    
    // Обработчик изменения выбранного файла
    fileInput.addEventListener('change', function(event) {
        if (event.target.files.length > 0) {
            // Сохраняем выбранный файл
            selectedFile = event.target.files[0];
            
            // Показываем модальное окно подтверждения
            confirmModal.style.display = 'flex';
        }
    });
    
    // Обработчик для кнопки "Да" в модальном окне
    yesBtn.addEventListener('click', function() {
        // Скрываем модальное окно
        confirmModal.style.display = 'none';
        
        // Отправляем файл на сервер, если он выбран
        if (selectedFile) {
            uploadDocumentForRules(selectedFile);
            selectedFile = null; // Сбрасываем выбранный файл
        }
    });
    
    // Обработчик для кнопки "Нет" в модальном окне
    noBtn.addEventListener('click', function() {
        // Скрываем модальное окно
        confirmModal.style.display = 'none';
        
        // Сбрасываем выбранный файл и input
        selectedFile = null;
        fileInput.value = '';
    });
    
    // Загружаем текущие правила с сервера
    loadFormattingRules();
}

/** Проверяет, были ли изменены поля, и активирует кнопку "Применить" */
function checkForChanges() {
    const inputs = document.querySelectorAll('.formatting-rules-field input');
    const applyButton = document.getElementById('apply-formatting-rules');
    
    if (!applyButton) return;
    
    let hasChanges = false;
    
    inputs.forEach(input => {
        // Проверяем, было ли изменено значение поля
        if (input.value !== input.getAttribute('data-original')) {
            hasChanges = true;
        }
    });
    
    // Активируем или деактивируем кнопку в зависимости от наличия изменений
    if (hasChanges) {
        applyButton.classList.add('active');
    } else {
        applyButton.classList.remove('active');
    }
}

/** Загружает текущие правила оформления с сервера */
function loadFormattingRules() {
    fetch('https://localhost:7060/word-formatting-api/get-rules', {
        method: 'GET',
        headers: {
            'Authorization': `Bearer ${localStorage.getItem('token')}`
        }
    })
    .then(response => {
        if (!response.ok) {
            throw new Error('Ошибка загрузки правил оформления');
        }
        return response.json();
    })
    .then(data => {
        // Заполняем поля значениями из ответа
        Object.keys(data).forEach(key => {
            const input = document.getElementById(key);
            if (input) {
                input.value = data[key];
                // Сохраняем исходное значение для сравнения
                input.setAttribute('data-original', data[key]);
            }
        });
    })
    .catch(error => {
        console.error('Ошибка при загрузке правил:', error);
        // Не показываем ошибку пользователю, просто используем значения по умолчанию
    });
}

/**
 * Отправляет обновленные правила оформления на сервер
 * @param {Object} rules - Объект с правилами оформления
 */
function sendFormattingRules(rules) {
    fetch('https://localhost:7060/word-formatting-api/setup-rules', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${localStorage.getItem('token')}`
        },
        body: JSON.stringify(rules)
    })
    .then(response => {
        if (!response.ok) {
            throw new Error('Ошибка сохранения правил оформления');
        }
        return response.json();
    })
    .then(data => {
        // Обновляем атрибуты data-original
        Object.keys(rules).forEach(key => {
            const input = document.getElementById(key);
            if (input) {
                input.setAttribute('data-original', input.value);
            }
        });
        
        // Деактивируем кнопку "Применить"
        const applyButton = document.getElementById('apply-formatting-rules');
        if (applyButton) {
            applyButton.classList.remove('active');
        }
        
        showNotification('Правила оформления успешно сохранены', 'success');
    })
    .catch(error => {
        console.error('Ошибка при сохранении правил:', error);
        showNotification('Ошибка при сохранении правил оформления', 'error');
    });
}

/**
 * Загружает Word-документ для извлечения из него правил форматирования
 * @param {File} file - Word-документ для загрузки
 */
function uploadDocumentForRules(file) {
    // Создаем объект FormData для отправки файла
    const formData = new FormData();
    formData.append('file', file);
    
    // Отправляем файл на сервер
    fetch('https://localhost:7060/word-formatting-api/get-rules-from-file', {
        method: 'POST',
        headers: {
            'Authorization': `Bearer ${localStorage.getItem('token')}`
        },
        body: formData
    })
    .then(response => {
        if (!response.ok) {
            throw new Error('Ошибка при загрузке документа');
        }
        return response.json();
    })
    .then(data => {
        // Object.keys(data).forEach(key => {
        //     const input = document.getElementById(key);
        //     if (input) {
        //         input.value = data[key];
        //         // Сохраняем исходное значение для сравнения
        //         input.setAttribute('data-original', data[key]);
        //     }
        // });
        
        showNotification('Правила форматирования успешно извлечены из документа', 'success');
    })
    .catch(error => {
        console.error('Ошибка при загрузке документа:', error);
        showNotification('Ошибка при загрузке документа', 'error');
    });
} 