/**
 * Декодирует JWT токен и возвращает объект с данными
 * @param {string} token - JWT токен
 * @returns {Object|null} - Объект с данными токена или null при ошибке
 */
function parseJwt(token) {
    if (!token) return null;

    try {
        const base64Url = token.split('.')[1];
        const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
        const jsonPayload = decodeURIComponent(atob(base64).split('').map(function(c) {
            return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2);
        }).join(''));

        return JSON.parse(jsonPayload);
    } catch (e) {
        console.error('Ошибка парсинга JWT', e);
        return null;
    }
}

/**
 * Проверяет, является ли текущий пользователь администратором
 * @returns {boolean} - true если пользователь администратор, иначе false
 */
function isUserAdmin() {
    const token = localStorage.getItem('token');
    if (!token) return false;

    const tokenData = parseJwt(token);
    if (!tokenData) return false;

    // Проверяем поле role или http://schemas.microsoft.com/ws/2008/06/identity/claims/role
    const role = tokenData.role || tokenData['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];
    
    // Роль может быть либо строкой "Admin", либо числом 1 (UserRole.Admin)
    return role === 'Admin' || role === 1 || role === '1';
}

/**
 * Получает текущую роль пользователя
 * @returns {string|null} - Роль пользователя или null, если не удалось получить
 */
function getUserRole() {
    const token = localStorage.getItem('token');
    if (!token) return null;

    const tokenData = parseJwt(token);
    if (!tokenData) return null;

    return tokenData.role || tokenData['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];
} 