/**
 * Отображает уведомление
 * @param {string} message - Текст сообщения
 * @param {string} type - Тип уведомления ('success' или 'error')
 */
function showNotification(message, type) {
    let notification = document.createElement('div');
    notification.classList.add('notification');
    notification.classList.add(`notification-${type}`);
    notification.textContent = message;

    document.body.appendChild(notification);
    
    setTimeout(() => {
        notification.classList.add('hiding');
        setTimeout(() => {
            if (notification.parentNode) {
                document.body.removeChild(notification);
            }
        }, 500);
    }, 2000);
} 