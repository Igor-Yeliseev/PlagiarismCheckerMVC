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
                alert('Отображение результатов проверки');
                // displayResults(data);
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
    function displayResults(data) {
        if (!data.results || data.results.length === 0) {
            resultsContainer.innerHTML = `
                <div class="results-header">
                    <h2 class="results-title">Результаты проверки для "${data.documentName}"</h2>
                    <div class="results-meta">
                        <span>Поисковая система: ${data.searchEngine}</span>
                        <span>Дата проверки: ${new Date(data.checkedAt).toLocaleString()}</span>
                    </div>
                </div>
                <div class="no-results">
                    <p>Заимствований не найдено.</p>
                </div>
            `;
            return;
        }
        
        // Формируем HTML для результатов
        let resultsHtml = `
            <div class="results-header">
                <h2 class="results-title">Результаты проверки для "${data.documentName}"</h2>
                <div class="results-meta">
                    <span>Поисковая система: ${data.searchEngine}</span>
                    <span>Дата проверки: ${new Date(data.checkedAt).toLocaleString()}</span>
                </div>
            </div>
            <div class="results-summary">
                <p>Найдено источников с заимствованиями: ${data.results.length}</p>
            </div>
            <div class="results-list">
        `;
        
        data.results.forEach(result => {
            const similarityPercentage = (result.similarityScore * 100).toFixed(1);
            const similarityClass = getSimilarityClass(result.similarityScore);
            
            resultsHtml += `
                <div class="result-item">
                    <h3 class="result-title">${escapeHtml(result.sourceTitle)}</h3>
                    <a href="${result.sourceUrl}" target="_blank" class="result-url">${result.sourceUrl}</a>
                    <div class="result-content">
                        <p class="result-text">${escapeHtml(result.originalText)}</p>
                        <div class="similarity-score ${similarityClass}">
                            <span>${similarityPercentage}%</span>
                        </div>
                    </div>
                </div>
            `;
        });
        
        resultsHtml += '</div>';
        resultsContainer.innerHTML = resultsHtml;
    }
    
    // Функция для определения класса схожести на основе значения
    function getSimilarityClass(score) {
        if (score >= 0.7) return 'high-similarity';
        if (score >= 0.5) return 'medium-similarity';
        return 'low-similarity';
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
});