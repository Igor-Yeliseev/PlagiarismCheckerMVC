document.addEventListener('DOMContentLoaded', () => {
    const token = localStorage.getItem('token');
    
    if (!token) {
        window.location.href = '/login.html';
        return;
    }

    loadProfile();
});

async function loadProfile() {
    try {
        const response = await fetch('/plag-api/user/profile', {
            headers: {
                'Authorization': `Bearer ${localStorage.getItem('token')}`
            }
        });

        if (response.ok) {
            const profile = await response.json();
            displayProfile(profile);
        } else {
            console.error('Ошибка загрузки профиля');
            alert('Не удалось загрузить данные профиля');
        }
    } catch (error) {
        console.error('Ошибка:', error);
        alert('Произошла ошибка при загрузке профиля');
    }
}

function displayProfile(profile) {
    // Скрываем загрузку, показываем профиль
    document.getElementById('loading-message').style.display = 'none';
    document.getElementById('profile-block').style.display = 'block';

    // Заполняем данные профиля
    document.getElementById('profile-email').textContent = profile.email;
    document.getElementById('profile-username').textContent = profile.username;
    document.getElementById('profile-phone').textContent = profile.phoneNumber || 'Не указан';
    document.getElementById('profile-role').textContent = profile.role;
    const createdDate = new Date(profile.createdAt);
    const formattedDate = createdDate.toLocaleDateString('ru-RU', {
        year: 'numeric', month: 'long', day: 'numeric', hour: '2-digit', minute: '2-digit'
    });
    document.getElementById('profile-created').textContent = formattedDate;

    // --- Модальное окно для имени ---
    const editUsernameBtn = document.getElementById('edit-username-btn');
    const modalUsername = document.getElementById('modal-username');
    const editUsernameInput = document.getElementById('edit-username-input');
    const editUsernameForm = document.getElementById('edit-username-form-modal');
    const cancelUsernameBtn = document.getElementById('cancel-username-btn-modal');
    const saveUsernameBtn = editUsernameForm.querySelector('button[type="submit"]');
    let initialUsername = '';
    editUsernameBtn.onclick = () => {
        editUsernameInput.value = profile.username;
        initialUsername = profile.username;
        saveUsernameBtn.disabled = true;
        saveUsernameBtn.classList.add('disabled-btn');
        modalUsername.style.display = 'block';
        setTimeout(() => editUsernameInput.focus(), 100);
    };
    editUsernameInput.addEventListener('input', () => {
        saveUsernameBtn.disabled = (editUsernameInput.value.trim() === initialUsername.trim());
        if (saveUsernameBtn.disabled) {
            saveUsernameBtn.classList.add('disabled-btn');
        } else {
            saveUsernameBtn.classList.remove('disabled-btn');
        }
    });
    cancelUsernameBtn.onclick = () => {
        modalUsername.style.display = 'none';
    };
    editUsernameForm.onsubmit = async (e) => {
        e.preventDefault();
        await updateUsername(editUsernameInput.value);
    };

    // --- Модальное окно для пароля ---
    const editPasswordBtn = document.getElementById('edit-password-btn');
    const modalPassword = document.getElementById('modal-password');
    const editPasswordForm = document.getElementById('edit-password-form-modal');
    const cancelPasswordBtn = document.getElementById('cancel-password-btn-modal');

    editPasswordBtn.onclick = () => {
        modalPassword.style.display = 'block';
        setTimeout(() => document.getElementById('current-password').focus(), 100);
        const savePasswordBtn = editPasswordForm.querySelector('button[type="submit"]');
        savePasswordBtn.disabled = true;
        savePasswordBtn.classList.add('disabled-btn');
    };
    cancelPasswordBtn.onclick = () => {
        modalPassword.style.display = 'none';
        editPasswordForm.reset();
        const savePasswordBtn = editPasswordForm.querySelector('button[type="submit"]');
        savePasswordBtn.disabled = true;
        savePasswordBtn.classList.add('disabled-btn');
    };
    editPasswordForm.addEventListener('input', () => {
        const currentPassword = document.getElementById('current-password').value;
        const newPassword = document.getElementById('new-password').value;
        const savePasswordBtn = editPasswordForm.querySelector('button[type="submit"]');
        savePasswordBtn.disabled = !(currentPassword && newPassword);
        if (savePasswordBtn.disabled) {
            savePasswordBtn.classList.add('disabled-btn');
        } else {
            savePasswordBtn.classList.remove('disabled-btn');
        }
    });
    editPasswordForm.onsubmit = async (e) => {
        e.preventDefault();
        await updatePassword();
    };

    // --- Модальное окно для телефона ---
    const editPhoneBtn = document.getElementById('edit-phone-btn');
    const modalPhone = document.getElementById('modal-phone');
    const editPhoneInput = document.getElementById('edit-phone-input');
    const editPhoneForm = document.getElementById('edit-phone-form-modal');
    const cancelPhoneBtn = document.getElementById('cancel-phone-btn-modal');
    const savePhoneBtn = editPhoneForm.querySelector('button[type="submit"]');
    let initialPhone = '';
    editPhoneBtn.onclick = () => {
        editPhoneInput.value = profile.phoneNumber || '';
        initialPhone = profile.phoneNumber || '';
        savePhoneBtn.disabled = (editPhoneInput.value.trim() === initialPhone.trim());
        if (savePhoneBtn.disabled) {
            savePhoneBtn.classList.add('disabled-btn');
        } else {
            savePhoneBtn.classList.remove('disabled-btn');
        }
        modalPhone.style.display = 'block';
        setTimeout(() => editPhoneInput.focus(), 100);
    };
    editPhoneInput.addEventListener('input', () => {
        savePhoneBtn.disabled = (editPhoneInput.value.trim() === initialPhone.trim());
        if (savePhoneBtn.disabled) {
            savePhoneBtn.classList.add('disabled-btn');
        } else {
            savePhoneBtn.classList.remove('disabled-btn');
        }
    });
    cancelPhoneBtn.onclick = () => {
        modalPhone.style.display = 'none';
        editPhoneForm.reset();
    };
    editPhoneForm.onsubmit = async (e) => {
        e.preventDefault();
        await updatePhone(editPhoneInput.value);
    };

    // Закрытие модалок по клику вне окна
    window.onclick = function(event) {
        if (event.target === modalUsername) modalUsername.style.display = 'none';
        if (event.target === modalPassword) modalPassword.style.display = 'none';
        if (event.target === modalPhone) modalPhone.style.display = 'none';
    };
}

async function updateUsername(newUsername) {
    const username = newUsername.trim();
    if (!username) {
        alert('Имя пользователя не может быть пустым');
        return;
    }
    try {
        const response = await fetch('/plag-api/user/username', {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${localStorage.getItem('token')}`
            },
            body: JSON.stringify({ username })
        });
        const data = await response.json();
        if (response.ok) {
            showNotification('Имя пользователя успешно обновлено', 'success');
            localStorage.setItem('username', username);
            document.getElementById('profile-username').textContent = username;
            document.getElementById('modal-username').style.display = 'none';
            const greetingContainer = document.querySelector('.greeting-container');
            if (greetingContainer) {
                greetingContainer.textContent = `Привет, ${username}`;
            }
        } else {
            alert(data.message || 'Ошибка при обновлении имени');
        }
    } catch (error) {
        console.error('Ошибка:', error);
        alert('Произошла ошибка при обновлении имени');
    }
}

async function updatePassword() {
    const currentPassword = document.getElementById('current-password').value;
    const newPassword = document.getElementById('new-password').value;
    if (newPassword.length < 6) {
        alert('Новый пароль должен содержать минимум 6 символов');
        return;
    }
    try {
        const response = await fetch('/plag-api/user/password', {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${localStorage.getItem('token')}`
            },
            body: JSON.stringify({ currentPassword, newPassword })
        });
        const data = await response.json();
        if (response.ok) {
            showNotification('Пароль успешно изменен', 'success');
            document.getElementById('current-password').value = '';
            document.getElementById('new-password').value = '';
            document.getElementById('modal-password').style.display = 'none';
        } else {
            alert(data.message || 'Ошибка при изменении пароля');
        }
    } catch (error) {
        console.error('Ошибка:', error);
        alert('Произошла ошибка при изменении пароля');
    }
}

async function updatePhone(phoneNumber) {
    if (!phoneNumber) {
        alert('Номер телефона не может быть пустым');
        return;
    }
    try {
        const response = await fetch('/plag-api/user/phone', {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${localStorage.getItem('token')}`
            },
            body: JSON.stringify({ phoneNumber })
        });
        const data = await response.json();
        if (response.ok) {
            showNotification('Номер телефона успешно обновлен', 'success');
            document.getElementById('profile-phone').textContent = phoneNumber;
            document.getElementById('modal-phone').style.display = 'none';
        } else {
            alert(data.message || 'Ошибка при обновлении номера телефона');
        }
    } catch (error) {
        console.error('Ошибка:', error);
        alert('Произошла ошибка при обновлении номера телефона');
    }
}