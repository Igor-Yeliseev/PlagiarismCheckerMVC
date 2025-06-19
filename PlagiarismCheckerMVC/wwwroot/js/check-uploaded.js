document.addEventListener('DOMContentLoaded', () => {
    const token = localStorage.getItem('token');
    
    if (!token) {
        window.location.href = '/login.html';
        return;
    }
    
    // Получаем ID документа из URL
    const urlParams = new URLSearchParams(window.location.search);
    const documentId = urlParams.get('id');
    
    if (!documentId) {
        window.location.href = '/profile.html';
        return;
    }
    
    // Элементы страницы
    const checkButton = document.getElementById('check-plag-btn');
    const spinner = document.getElementById('spinner');
    const resultsContainer = document.getElementById('results-container');
    const searchEngineSelect = document.getElementById('search-engine');
    const timerContainer = document.getElementById('timer-container');
    const timerDisplay = document.getElementById('timer');
    
    let selectedEngine = searchEngineSelect ? searchEngineSelect.value : 'google';
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
    
    // Обработчик выбора поисковой системы
    if (searchEngineSelect) {
        searchEngineSelect.addEventListener('change', function() {
            selectedEngine = this.value;
        });
    }
    
    // Обработчик кнопки проверки на плагиат
    checkButton.addEventListener('click', async () => {
        try {
            // Показываем спиннер
            spinner.style.display = 'flex';
            resultsContainer.innerHTML = '';
            
            // Запускаем таймер
            startTimer();
            
            const response = await fetch(`/plag-api/check-uploaded?documentId=${documentId}&searchEngine=${selectedEngine}`, {
                method: 'POST',
                headers: {
                    'Authorization': `Bearer ${token}`,
                    'Content-Type': 'application/json'
                }
            });
            
            const data = await response.json();
            
            spinner.style.display = 'none'; // Скрываем спиннер
            
            // Останавливаем таймер (но не скрываем его)
            stopTimer();
            
            if (response.ok) {
                renderResultsTable(data);
            }
            else {
                showError(data.message || 'Ошибка при проверке на плагиат');
            }
        }
        catch (error) {
            console.error('Ошибка:', error);
            spinner.style.display = 'none';
            
            // Останавливаем таймер
            stopTimer();
            
            showError('Произошла ошибка при проверке на плагиат');
        }
    });

    // Функция для отображения ошибок
    function showError(message) {
        resultsContainer.innerHTML = `
            <div class="error-message">
                <p>${message}</p>
            </div>
        `;
    }
    
    // Функция отображения результатов
    function renderResultsTable(report) {
        // Очищаем контейнер перед выводом
        resultsContainer.innerHTML = '';
        let html = '';
        html += `<div class="results-header">
            <h2 class="results-title">Результаты проверки для "${report.documentName}"</h2>
            <div class="results-meta">
                <span>Поисковая система: ${report.searchEngine}</span>
                <span>Дата проверки: ${new Date(report.checkedAt).toLocaleString()}</span>
            </div>
        </div>`;
        // Вывод процентов
        if (report.plagiarismPercentage) {
            if (report.plagiarismPercentage.webPlagPercentage !== undefined && report.plagiarismPercentage.webPlagPercentage !== null) {
                const webPercent = (report.plagiarismPercentage.webPlagPercentage * 100).toFixed(1);
                const webClass = getOriginalityClass(Number(webPercent));
                html += `<div class=\"percent-labels\">
                    <span><b>Процент плагиата из сети:</b> <span class=\"${webClass}\">${webPercent}%</span></span>
                </div>`;
            }
            if (report.plagiarismPercentage.dbPlagPercentage !== undefined && report.plagiarismPercentage.dbPlagPercentage !== null) {
                const dbPercent = (report.plagiarismPercentage.dbPlagPercentage * 100).toFixed(1);
                const dbClass = getOriginalityClass(Number(dbPercent));
                html += `<div class=\"percent-labels\">
                    <span><b>Процент плагиата с документами БД:</b> <span class=\"${dbClass}\">${dbPercent}%</span></span>
                </div>`;
            }
        }
        if (!report.queryResults || !Array.isArray(report.queryResults) || report.queryResults.length === 0) {
            html += '<div class="no-results"><p>Заимствований не найдено.</p></div>';
            resultsContainer.innerHTML = html;
            return;
        }
        html += `<table class="plag-results-table"><thead><tr><th>Абзац</th><th>Предложение</th><th>Источник</th><th>Заголовок</th></tr></thead><tbody>`;
        for (const r of report.queryResults) {
            html += `<tr>`;
            html += `<td>${r.paraNum ?? ''}</td>`;
            html += `<td>${r.sentNum ?? ''}</td>`;
            html += `<td><a href="${r.sourceUrl}" target="_blank" class="source-link">${r.sourceUrl}</a></td>`;
            html += `<td>${r.sourceTitle ?? ''}</td>`;
            html += `</tr>`;
        }
        html += `</tbody></table>`;
        resultsContainer.innerHTML = html;
    }
    
    
    // Функция для экранирования HTML
    function escapeHtml(text) {
        return text
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;")
            .replace(/'/g, "&#039;");
    }

    function getOriginalityClass(percent) {
        if (percent <= 25) return 'originality-green';
        if (percent <= 55) return 'originality-yellow';
        if (percent <= 75) return 'originality-orange';
        return 'originality-red';
    }
});