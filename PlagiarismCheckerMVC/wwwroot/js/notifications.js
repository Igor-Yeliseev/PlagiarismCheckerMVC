// /**
//  * Отображает уведомление об успешном действии
//  * @param {string} message - Текст сообщения
//  * @param {number} duration - Продолжительность отображения в миллисекундах
//  */
// function notificationSuccess(message, duration = 3000) {
//     showNotification(message, 'success', duration);
// }

// /**
//  * Отображает уведомление об ошибке
//  * @param {string} message - Текст сообщения
//  * @param {number} duration - Продолжительность отображения в миллисекундах
//  */
// function notificationError(message, duration = 3000) {
//     showNotification(message, 'error', duration);
// }

// /**
//  * Отображает уведомление
//  * @param {string} message - Текст сообщения
//  * @param {string} type - Тип уведомления ('success' или 'error')
//  * @param {number} duration - Продолжительность отображения в миллисекундах
//  */
// function showNotification(message, type, duration) {
//     // Проверяем, существует ли контейнер для уведомлений
//     let notificationsContainer = document.getElementById('notifications-container');
    
//     // Если контейнер не существует, создаем его
//     if (!notificationsContainer) {
//         notificationsContainer = document.createElement('div');
//         notificationsContainer.id = 'notifications-container';
//         notificationsContainer.style.position = 'fixed';
//         notificationsContainer.style.top = '20px';
//         notificationsContainer.style.right = '20px';
//         notificationsContainer.style.zIndex = '9999';
//         document.body.appendChild(notificationsContainer);
//     }
    
//     // Создаем элемент уведомления
//     const notification = document.createElement('div');
//     notification.className = `notification ${type}`;
//     notification.style.padding = '15px 20px';
//     notification.style.marginBottom = '10px';
//     notification.style.borderRadius = '4px';
//     notification.style.boxShadow = '0 4px 6px rgba(0, 0, 0, 0.1)';
//     notification.style.transition = 'all 0.3s ease';
//     notification.style.opacity = '0';
//     notification.style.transform = 'translateY(-20px)';
//     notification.style.fontSize = '14px';
//     notification.style.fontWeight = '500';
    
//     // Устанавливаем цвет фона в зависимости от типа уведомления
//     if (type === 'success') {
//         notification.style.backgroundColor = '#4CAF50';
//         notification.style.color = 'white';
//     } else if (type === 'error') {
//         notification.style.backgroundColor = '#F44336';
//         notification.style.color = 'white';
//     }
    
//     // Добавляем текст уведомления
//     notification.textContent = message;
    
//     // Добавляем уведомление в контейнер
//     notificationsContainer.appendChild(notification);
    
//     // Анимация появления
//     setTimeout(() => {
//         notification.style.opacity = '1';
//         notification.style.transform = 'translateY(0)';
//     }, 10);
    
//     // Удаляем уведомление через указанное время
//     setTimeout(() => {
//         notification.style.opacity = '0';
//         notification.style.transform = 'translateY(-20px)';
        
//         // Удаляем элемент из DOM через 300мс после начала анимации исчезновения
//         setTimeout(() => {
//             if (notification.parentNode) {
//                 notification.parentNode.removeChild(notification);
//             }
//         }, 300);
//     }, duration);
// }

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