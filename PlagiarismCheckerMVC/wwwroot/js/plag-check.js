document.addEventListener('DOMContentLoaded', () => {
    // Проверяем авторизацию
    const token = localStorage.getItem('token');
    if (!token) {
        window.location.href = '/login.html';
        return;
    }

    const checkForm = document.getElementById('check-form');
    const closeFileBtn = document.getElementById('close-file-btn');
    const fileInput = document.getElementById('document-file');
    const timerContainer = document.getElementById('timer-container');
    const timerDisplay = document.getElementById('timer');
    
    let timer;
    let startTime;
    
    // Функция запуска таймера
    function startTimer() {
        startTime = new Date();
        timerContainer.style.display = 'block';
        updateTimerDisplay();
        timer = setInterval(updateTimerDisplay, 10); // Обновляем каждые 10 мс для отображения миллисекунд
    }
    
    // Функция остановки таймера
    function stopTimer() {
        clearInterval(timer);
    }
    
    // Функция обновления отображения таймера
    function updateTimerDisplay() {
        const currentTime = new Date();
        const elapsedTime = new Date(currentTime - startTime);
        
        const minutes = Math.floor(elapsedTime / 60000);
        const seconds = Math.floor((elapsedTime % 60000) / 1000);
        const milliseconds = Math.floor((elapsedTime % 1000) / 10);
        
        timerDisplay.textContent = `${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}.${milliseconds.toString().padStart(2, '0')}`;
    }

    // Скрываем кнопку крестика если файл не выбран
    fileInput.addEventListener('change', () => {
        if (fileInput.files.length > 0) {
            closeFileBtn.style.display = 'block';
        } else {
            closeFileBtn.style.display = 'none';
        }
    });

    // Обработчик для кнопки очистки формы
    closeFileBtn.addEventListener('click', () => {
        fileInput.value = '';
        closeFileBtn.style.display = 'none';
        showNotification('Поле выбора файла очищено', 'success');
    });

    // Обработчик отправки формы
    checkForm.addEventListener('submit', async (e) => {
        e.preventDefault();

        const file = fileInput.files[0];

        if (!file) {
            showNotification('Выберите файл для проверки', 'error');
            return;
        }
        if (!file.name.endsWith('.docx')) {
            showNotification('Можно загружать только файлы формата .docx', 'error');
            return;
        }
        if (file.size === 0) {
            showNotification('Файл пустой или поврежден', 'error');
            return;
        }

        // Получаем выбранный поисковый движок перед отправкой файла
        const searchEngine = document.querySelector('input[name="searchEngine"]:checked').value;

        await sendToPlagCheck(file, searchEngine);
    });

    // Функция для отправки документа на сервер
    async function sendToPlagCheck(file, searchEngine) {
        try {
            const formContainer = document.getElementById('check-form-container');
            closeFileBtn.style.display = 'none';
            const originalContent = formContainer.innerHTML;

            // Создаем элемент спиннера
            const spinnerElement = `
                <div class="spinner">
                    <div class="spinner-border"></div>
                    <p>Проверка через ${searchEngine} Search API...</p>
                </div>
            `;

            // Очищаем и добавляем спиннер
            formContainer.innerHTML = spinnerElement;
            
            // Запускаем таймер
            startTimer();

            const formData = new FormData();
            formData.append('docFile', file);
            formData.append('searchEngine', searchEngine);

            // Запрос на загрузку файла для проверки
            const response = await fetch('/plag-api/check-new', {
                method: 'POST',
                headers: {
                    'Authorization': `Bearer ${token}`
                },
                body: formData
            });

            if (!response.ok) {
                throw new Error('Ошибка при проверке файла');
            }

            // Получаем ID проверки и перенаправляем на страницу результатов
            const result = await response.json();
            //window.location.href = `/check-uploaded.html?id=${results.id}`;
            
            stopTimer();

            formContainer.innerHTML = originalContent; // Удаляем спиннер и восстанавливаем форму
            
            renderResultsTable(result);
        }
        catch (error) {
            console.error('Ошибка:', error);
            
            // Останавливаем таймер
            stopTimer();
            
            showNotification('Ошибка при проверке документа', 'error');
            location.reload(); // Восстанавливаем форму
        }
    }

    // Функция для отображения таблицы результатов проверки на плагиат
    function renderResultsTable(report) {
        let container = document.getElementById('plag-results-table-container');
        if (!container) {
            container = document.createElement('div');
            container.id = 'plag-results-table-container';
            // Вставляем после формы
            const formContainer = document.getElementById('check-form-container');
            formContainer.parentNode.insertBefore(container, formContainer.nextSibling);
        }
        if (!report || !report.queryResults || !Array.isArray(report.queryResults) || report.queryResults.length === 0) {
            container.innerHTML = '<p>Результаты не найдены.</p>';
            return;
        }
        let html = `<h3>Результаты проверки</h3>`;
        if (report.plagiarismPercentage) {
            if (report.plagiarismPercentage.webPlagPercentage !== undefined && report.plagiarismPercentage.webPlagPercentage !== null) {
                html += `<p>Web-плагиат: <b>${(report.plagiarismPercentage.webPlagPercentage * 100).toFixed(1)}%</b></p>`;
            }
        }
        html += `<table class="plag-results-table"><thead><tr><th>Параграф</th><th>Предложение</th><th>Источник</th><th>Заголовок</th><th>Сходство</th></tr></thead><tbody>`;
        for (const r of report.queryResults) {
            html += `<tr>`;
            html += `<td>${r.paraNum ?? ''}</td>`;
            html += `<td>${r.sentNum ?? ''}</td>`;
            html += `<td><a href="${r.sourceUrl}" target="_blank">${r.sourceUrl}</a></td>`;
            html += `<td>${r.sourceTitle ?? ''}</td>`;
            html += `<td>${(r.similarityScore * 100).toFixed(1)}%</td>`;
            html += `</tr>`;
        }
        html += `</tbody></table>`;
        container.innerHTML = html;
    }
}); 