document.addEventListener('DOMContentLoaded', () => {
    // Форма регистрации
    const registerForm = document.getElementById('register-form');
    if (registerForm) {
        registerForm.addEventListener('submit', async (e) => {
            e.preventDefault();
            
            const username = document.getElementById('username').value;
            const email = document.getElementById('email').value;
            const password = document.getElementById('password').value;
            
            try {
                const response = await fetch('/plag-api/register', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify({ username, email, password })
                });
                
                const data = await response.json();
                
                if (response.ok) {
                    // Сохраняем токен и userId в localStorage
                    localStorage.setItem('token', data.token);
                    localStorage.setItem('userId', data.userId);
                    localStorage.setItem('username', data.username);
                    
                    // Перенаправляем на страницу профиля
                    window.location.href = '/my-documents.html';
                } else {
                    // Отображаем ошибку
                    alert(data.message || 'Ошибка при регистрации');
                }
            } catch (error) {
                console.error('Ошибка:', error);
                alert('Произошла ошибка при регистрации');
            }
        });
    }
    
    // Форма входа
    const loginForm = document.getElementById('login-form');
    if (loginForm) {
        loginForm.addEventListener('submit', async (e) => {
            e.preventDefault();
            
            const email = document.getElementById('email').value;
            const password = document.getElementById('password').value;
            
            try {

                const response = await fetch('/plag-api/login', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify({ email, password })
                });
                
                console.log('Получен ответ:', response.status, response.statusText);
                
                // Проверяем, что ответ успешен, перед парсингом JSON
                if (!response.ok) {
                    throw new Error(`Ошибка: ${response.status} ${response.statusText}`);
                }
                
                const data = await response.json();
                
                if (response.ok) {
                    // Сохраняем токен и userId в localStorage
                    localStorage.setItem('token', data.token);
                    localStorage.setItem('userId', data.userId);
                    localStorage.setItem('username', data.username);
                    
                    // Перенаправляем на страницу профиля
                    window.location.href = '/my-documents.html';
                } else {
                    // Отображаем ошибку
                    alert(data.message || 'Неверный email или пароль');
                }
            } catch (error) {
                console.error('Ошибка:', error);
                alert('Произошла ошибка при входе');
            }
        });
    }
    
    // Проверяем, аутентифицирован ли пользователь
    const token = localStorage.getItem('token');
    const navbarControls = document.querySelector('.navbar-controls');
    
    if (token) {
        const username = localStorage.getItem('username');
        
        // Находим контейнер для приветствия
        const greetingContainer = document.querySelector('.greeting-container');
        if (greetingContainer) {
            // Добавляем приветствие в существующий контейнер
            greetingContainer.textContent = `Привет, ${username}`;
        }
        
        // Обновляем кнопки навигации для авторизованного пользователя
        if (navbarControls) {
            // Получаем текущий путь страницы
            const currentPage = window.location.pathname;
            
            // Определяем, какая кнопка должна быть активной
            const isProfilePage = currentPage.includes('my-documents');
            const isCheckPage = currentPage.includes('plag-check-new');
            const isFormatPage = currentPage.includes('format-check');
            
            // Обновляем навигационные кнопки для авторизованного пользователя
            navbarControls.innerHTML = `
                <li><a href="/plag-check-new.html" class="nav-button ${isCheckPage ? 'active' : ''}">Плагиат</a></li>
                <li><a href="/my-documents.html" class="nav-button ${isProfilePage ? 'active' : ''}">Мои документы</a></li>
                <li><a href="/format-check.html" class="nav-button ${isFormatPage ? 'active' : ''}">Оформление</a></li>
                <li><a href="#" id="logout" class="nav-button">Выйти</a></li>
            `;
        }
        
        // Обработчик выхода
        // Используем делегирование событий, так как кнопка может быть создана динамически
        document.addEventListener('click', (e) => {
            if (e.target && e.target.id === 'logout') {
                e.preventDefault();
                localStorage.removeItem('token');
                localStorage.removeItem('userId');
                localStorage.removeItem('username');
                window.location.href = '/index.html';
            }
        });
    }
}); 