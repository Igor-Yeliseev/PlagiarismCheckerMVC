-- Скрипт для добавления тестовых пользователей в таблицу Users
-- Хеши паролей сгенерированы через https://bcrypt-generator.com/ с 12 rounds

-- Подключение к базе данных
\c plag_search_db;

-- Вставка тестовых пользователей с реальными BCrypt хешами
INSERT INTO "Users" ("Id", "Username", "Email", "PhoneNumber", "HashedPassword", "CreatedAt", "Role") VALUES
-- 1. John Anderson (User) - Пароль: Y4ej5MdEs8@PIE
(
    '1502a1e4-25ad-432f-993d-9b774d085c1c',
    'John Anderson',
    'john-anderson87@gmail.com',
    '+375 29 444 32 65',
    '$2a$12$D9/zDhChYecfPyU08MrFJ.tiRPmKCeYLgOf/YHvPIAIF/MpFJNfNC',
    '2025-05-14 09:51:45'::timestamp,
    0
),
-- 2. Ivan Petrov (Admin) - Пароль: nQ6Ah&TbXR#zJ7
(
    '5927478d-0941-4535-b44c-ba61c02dd068',
    'Ivan Petrov',
    'ivan-petrov365@gmail.com',
    '+375 29 444 32 65',
    '$2a$12$YzWQ9/Y/lVl.NJSKk9KdS.DvSEUXd4fKMgS4zKr7SrKI3qbfgjsc2',
    '2025-04-21 16:40:28'::timestamp,
    1
),
-- 3. Igor Yeliseyev (User) - Пароль: Hwz3V8uj6LGx5C6
(
    uuid_generate_v4(),
    'Igor Yeliseyev',
    'igor-yeliseyev@gmail.com',
    '+375 29 155 16 96',
    '$2a$12$1ag6w1Gx7dqvHkbO0TOHietYg6ri9SdkRqg5oKFJ3mpHUxE49qIRC',
    '2025-05-10 13:21:04'::timestamp,
    0
),
-- 4. Robert Green (Admin) - Пароль: F7whXPkW5MCCp6
(
    uuid_generate_v4(),
    'Robert Green',
    'bob-green98@gmail.com',
    '+375 44 201 23 61',
    '$2a$12$QZoKIGAMqnTBKX1x5v0MgeY8uSoYaAzAGrWXpiQvVUKYw7LJ/oMwW',
    '2025-05-10 10:43:26'::timestamp,
    1
);

-- Проверка вставленных данных
SELECT "Id", "Username", "Email", "PhoneNumber", "CreatedAt", "Role" 
FROM "Users" 
ORDER BY "CreatedAt" DESC;

-- Информация о ролях:
-- 0 = User (обычный пользователь)
-- 1 = Admin (администратор)

-- Хеши сгенерированы через https://bcrypt-generator.com/ с cost factor = 12 rounds
-- Это обеспечивает высокий уровень безопасности для production-среды 