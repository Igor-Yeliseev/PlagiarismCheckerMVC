-- Скрипт создания базы данных для приложения PlagiarismSearchApp

-- Создание базы данных, если она не существует
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_database WHERE datname = 'plag_search_db') THEN
        PERFORM pg_terminate_backend(pg_stat_activity.pid)
        FROM pg_stat_activity
        WHERE pg_stat_activity.datname = 'plag_search_db';
        
        CREATE DATABASE plag_search_db;
    END IF;
END
$$;

-- Подключение к созданной базе данных
\c plag_search_db;

-- Создание таблицы пользователей
CREATE TABLE IF NOT EXISTS "Users" (
    "Id" UUID PRIMARY KEY,
    "Username" VARCHAR(50) NOT NULL,
    "Email" VARCHAR(100) NOT NULL,
    "HashedPassword" TEXT NOT NULL,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "Role" VARCHAR(50) NOT NULL DEFAULT 'user'
);

-- Добавление уникального индекса на Email
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Users_Email" ON "Users" ("Email");

-- Создание таблицы документов
CREATE TABLE IF NOT EXISTS "Documents" (
    "Id" UUID PRIMARY KEY,
    "Name" VARCHAR(255) NOT NULL,
    "DocFileUrl" TEXT NOT NULL,
    "Size" BIGINT NOT NULL,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UserId" UUID NOT NULL,
    CONSTRAINT "FK_Documents_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

-- Создание таблицы результатов проверки документов
CREATE TABLE IF NOT EXISTS public."DocumentCheckResults"
(
    "Id" SERIAL PRIMARY KEY,
    "DocumentId" UUID NOT NULL,
    "Originality" DECIMAL(3,2) NOT NULL CHECK ("Originality" >= 0 AND "Originality" <= 1),
    "CheckedAt" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_Document" FOREIGN KEY ("DocumentId")
        REFERENCES public."Documents" ("Id")
        ON DELETE CASCADE
);
-- Создание индексов для более быстрого поиска результатов проверки документов
CREATE INDEX "IX_DocumentCheckResults_DocumentId" ON public."DocumentCheckResults"("DocumentId");
CREATE INDEX "IX_DocumentCheckResults_CheckedAt" ON public."DocumentCheckResults"("CheckedAt"); 

-- Создание индекса для более быстрого поиска документов пользователя
CREATE INDEX IF NOT EXISTS "IX_Documents_UserId" ON "Documents" ("UserId");

-- Создание расширения для генерации UUID, если оно еще не установлено
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Комментарии к таблицам и полям для документации
COMMENT ON TABLE "Users" IS 'Таблица пользователей системы';
COMMENT ON COLUMN "Users"."Id" IS 'Уникальный идентификатор пользователя';
COMMENT ON COLUMN "Users"."Username" IS 'Имя пользователя';
COMMENT ON COLUMN "Users"."Email" IS 'Email пользователя (используется для входа)';
COMMENT ON COLUMN "Users"."HashedPassword" IS 'Хешированный пароль пользователя';
COMMENT ON COLUMN "Users"."CreatedAt" IS 'Дата и время создания учетной записи';
COMMENT ON COLUMN "Users"."Role" IS 'Роль пользователя в системе';

COMMENT ON TABLE "Documents" IS 'Таблица документов для проверки на плагиат';
COMMENT ON COLUMN "Documents"."Id" IS 'Уникальный идентификатор документа';
COMMENT ON COLUMN "Documents"."Name" IS 'Имя файла документа';
COMMENT ON COLUMN "Documents"."DocFileUrl" IS 'URL или путь к файлу документа в хранилище';
COMMENT ON COLUMN "Documents"."Size" IS 'Размер файла в байтах';
COMMENT ON COLUMN "Documents"."CreatedAt" IS 'Дата и время загрузки документа';
COMMENT ON COLUMN "Documents"."UserId" IS 'Идентификатор пользователя, загрузившего документ';

COMMENT ON TABLE "DocumentCheckResults" IS 'Таблица результатов проверки документов на плагиат';
COMMENT ON COLUMN "DocumentCheckResults"."Id" IS 'Уникальный идентификатор результата проверки';
COMMENT ON COLUMN "DocumentCheckResults"."DocumentId" IS 'Идентификатор документа, который был проверен';
COMMENT ON COLUMN "DocumentCheckResults"."Originality" IS 'Оценка оригинальности документа (0.00 - 1.00)';
COMMENT ON COLUMN "DocumentCheckResults"."CheckedAt" IS 'Дата и время проверки документа';

-- Инструкция для подключения из приложения
-- Строка подключения: "Server=localhost;Database=plag_search_db;User=postgres;Password=2004;TrustServerCertificate=True;"