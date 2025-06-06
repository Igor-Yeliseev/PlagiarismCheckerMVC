# PlagiarismCheckerMVC

Система обнаружения плагиата в документах Word с использованием как поисковых API (для веб-источников), так и сравнения с известными документами в базе данных.

## Описание

PlagiarismCheckerMVC - это полноценное веб-приложение на ASP.NET Core MVC для обнаружения плагиата в текстовых документах. Система анализирует загруженные документы, определяет потенциальные заимствования из интернет-источников и сравнивает с базой ранее проверенных документов.

## Ключевые возможности

- Загрузка и анализ документов Microsoft Word
- Обнаружение заимствований из интернет-источников через API поисковых систем
- Сравнение с базой данных ранее загруженных документов
- Генерация отчетов о плагиате с подробной статистикой
- Интерфейс администратора для управления пользователями и документами
- Защита API с использованием JWT-аутентификации

## Технический стек

- **Backend**: ASP.NET Core MVC (.NET 9)
- **Хранение данных**: PostgreSQL
- **ORM**: Entity Framework Core
- **Frontend**: HTML, CSS, JavaScript
- **API интеграции**: Google Search API, Yandex Search API
- **Алгоритмы анализа**: текстовые алгоритмы поиска плагиата

## Требования для запуска

- .NET 9.0 SDK
- PostgreSQL 15+
- API ключи для поисковых систем (Google/Yandex)
- Современный веб-браузер

## Установка и запуск

1. Клонируйте репозиторий:
```bash
git clone https://github.com/Igor-Yeliseev/PlagiarismCheckerMVC.git
cd PlagiarismCheckerMVC
```

2. Настройте подключение к базе данных:
```bash
# Скопируйте пример файла настроек и заполните своими данными
cp PlagiarismCheckerMVC/appsettings.example.json PlagiarismCheckerMVC/appsettings.json
```

3. Восстановите зависимости и запустите миграции:
```bash
dotnet restore
dotnet ef database update
```

4. Запустите приложение:
```bash
dotnet run --project PlagiarismCheckerMVC
```

## Конфигурация

Скопируйте `appsettings.example.json` в `appsettings.json` и заполните следующие настройки:

- Строка подключения к PostgreSQL
- API ключи для поисковых систем
- Настройки JWT для аутентификации
- Параметры алгоритма обнаружения плагиата

## Лицензия

Этот проект распространяется под лицензией MIT. Подробности в файле [LICENSE](LICENSE).