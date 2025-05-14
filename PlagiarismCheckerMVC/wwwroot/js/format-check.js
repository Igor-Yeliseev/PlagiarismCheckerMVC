document.addEventListener('DOMContentLoaded', function () {
    const fileInput = document.getElementById('format-document-file');
    const formatDocumentBtn = document.getElementById('format-document-btn');
    const selectedFileContainer = document.getElementById('selected-file-container');
    const fileName = document.getElementById('file-name');
    const submitButton = document.getElementById('submit-format-check');
    const closeFileBtn = document.getElementById('close-file-btn');
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

    // Обработчик клика по кнопке "Загрузить документ"
    formatDocumentBtn.addEventListener('click', function () {
        fileInput.click();
    });

    // Обработчик клика по кнопке закрытия
    closeFileBtn.addEventListener('click', function () {
        selectedFileContainer.style.display = 'none';
        fileInput.value = ''; // Сбрасываем выбранный файл
    });

    // Обработчик выбора файла
    fileInput.addEventListener('change', function (e) {
        if (this.files.length > 0) {
            const file = this.files[0];
            fileName.textContent = file.name;
            selectedFileContainer.style.display = 'block';
        } else {
            selectedFileContainer.style.display = 'none';
        }
    });

    // Обработчик отправки файла
    submitButton.addEventListener('click', async function () {
        const file = fileInput.files[0];
        if (!file) {
            alert('Пожалуйста, выберите файл');
            return;
        }

        // Отображаем спиннер загрузки
        const spinner = document.getElementById('format-spinner');
        spinner.style.display = 'flex';

        // Запускаем таймер
        startTimer();

        // Скрываем кнопку отправки, чтобы избежать повторных отправок
        submitButton.disabled = true;

        // Имитация задержки загрузки (3 секунды)
        // setTimeout(() => {
        //     // Скрываем спиннер и разблокируем кнопку
        //     spinner.style.display = 'none';
        //     submitButton.disabled = false;

        //     // Останавливаем таймер
        //     stopTimer();

        //     // Показываем сообщение
        //     alert('Файл был отправлен');
        // }, 3000);

        const formData = new FormData();
        formData.append('file', file);

        await fetch('https://localhost:7060/word-formatting-api/check-doc', {
            method: 'POST',
            headers: {
                'Authorization': `Bearer ${localStorage.getItem('token')}`
            },
            body: formData
        })
        .then(response => {
            if (!response.ok) {
                throw new Error('Ошибка сервера: ' + response.status);
            }

            // Получаем имя файла из заголовка ответа или используем имя "формат_[исходное имя файла]"
            const contentDisposition = response.headers.get('Content-Disposition');
            let filename = 'формат_' + file.name;
            if (contentDisposition) {
                const filenameMatch = contentDisposition.match(/filename="(.+)"/);
                if (filenameMatch && filenameMatch[1]) {
                    filename = filenameMatch[1];
                }
            }

            // Получаем бинарные данные документа
            return response.blob().then(blob => {
                return {
                    blob: blob,
                    filename: filename
                };
            });
        })
        .then(data => {
            // Создаем ссылку для скачивания документа
            const url = URL.createObjectURL(data.blob);
            const a = document.createElement('a');
            a.href = url;
            a.download = data.filename;
            document.body.appendChild(a);
            a.click();

            // Удаляем ссылку после скачивания
            setTimeout(() => {
                document.body.removeChild(a);
                URL.revokeObjectURL(url);
            }, 100);

            spinner.style.display = 'none';
            submitButton.disabled = false;

            stopTimer();

            console.log('Успех: документ с форматированием загружен');
        })
        .catch((error) => {
            console.error('Ошибка:', error);
            alert('Произошла ошибка при отправке файла: ' + error.message);

            // Скрываем спиннер и разблокируем кнопку
            spinner.style.display = 'none';
            submitButton.disabled = false;

            stopTimer();
        });
    });
}); 