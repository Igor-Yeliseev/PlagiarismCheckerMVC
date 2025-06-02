using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using PlagiarismCheckerMVC.Models;
using PlagiarismCheckerMVC.Services;

namespace PlagiarismCheckerMVC.Tests;

/// <summary>Базовый класс для всех тестов с общими настройками и моками</summary>
public abstract class TestBase
{
    protected IServiceProvider ServiceProvider { get; private set; }
    protected ApplicationDbContext DbContext { get; private set; }
    protected Mock<ILogger<PlagiarismService>> MockLogger { get; private set; }
    protected Mock<ISearchService> MockSearchService { get; private set; }
    protected Mock<IStorageService> MockStorageService { get; private set; }
    protected Mock<IConfiguration> MockConfiguration { get; private set; }

    /// <summary>Настройка тестового окружения</summary>
    [SetUp]
    public virtual void Setup()
    {
        // Создаем моки
        MockLogger = new Mock<ILogger<PlagiarismService>>();
        MockSearchService = new Mock<ISearchService>();
        MockStorageService = new Mock<IStorageService>();
        MockConfiguration = new Mock<IConfiguration>();

        // Настраиваем конфигурацию
        SetupConfiguration();

        // Создаем сервисы
        var services = new ServiceCollection();
        ConfigureServices(services);
        ServiceProvider = services.BuildServiceProvider();

        // Получаем контекст базы данных
        DbContext = ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Заполняем тестовыми данными
        SeedTestData();
    }

    /// <summary>Очистка после тестов</summary>
    [TearDown]
    public virtual void TearDown()
    {
        DbContext?.Dispose();
        if (ServiceProvider is IDisposable disposableProvider)
        {
            disposableProvider.Dispose();
        }
    }

    /// <summary>Настройка конфигурации</summary>
    protected virtual void SetupConfiguration()
    {
        var configSection = new Mock<IConfigurationSection>();
        configSection.Setup(x => x.Value).Returns("5");
        MockConfiguration.Setup(x => x.GetSection("PlagiarismSettings:SentMinCount")).Returns(configSection.Object);

        configSection = new Mock<IConfigurationSection>();
        configSection.Setup(x => x.Value).Returns("100");
        MockConfiguration.Setup(x => x.GetSection("PlagiarismSettings:ParaMinChars")).Returns(configSection.Object);

        configSection = new Mock<IConfigurationSection>();
        configSection.Setup(x => x.Value).Returns("25");
        MockConfiguration.Setup(x => x.GetSection("PlagiarismSettings:PlagiarismThreshold")).Returns(configSection.Object);
    }

    /// <summary>Настройка сервисов для тестирования</summary>
    protected virtual void ConfigureServices(IServiceCollection services)
    {
        // Добавляем InMemory базу данных
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()));

        // Добавляем моки сервисов
        services.AddSingleton(MockLogger.Object);
        services.AddSingleton(MockSearchService.Object);
        services.AddSingleton(MockStorageService.Object);
        services.AddSingleton(MockConfiguration.Object);

        // Добавляем реальные сервисы
        services.AddScoped<IPlagiarismService, PlagiarismService>();
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<IAuthService, AuthService>();
    }

    /// <summary>Заполнение базы данных тестовыми данными</summary>
    protected virtual void SeedTestData()
    {
        // Создаем тестового пользователя
        var testUser = new User
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            Email = "test@example.com",
            HashedPassword = "hashed_password_123", // Используем хешированный пароль
            Role = UserRole.User,
            CreatedAt = DateTime.UtcNow
        };

        DbContext.Users.Add(testUser);

        // Создаем тестовые документы
        var testDocument1 = new Document
        {
            Id = Guid.NewGuid(),
            Name = "test_document_1.docx",
            UserId = testUser.Id,
            CreatedAt = DateTime.UtcNow
        };

        var testDocument2 = new Document
        {
            Id = Guid.NewGuid(),
            Name = "test_document_2.docx",
            UserId = testUser.Id,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };

        DbContext.Documents.AddRange(testDocument1, testDocument2);

        DbContext.SaveChanges();
    }

    /// <summary>Получить тестового пользователя из базы данных</summary>
    protected User GetTestUser()
    {
        return DbContext.Users.First();
    }

    /// <summary>Получить тестовые документы из базы данных</summary>
    protected List<Document> GetTestDocuments()
    {
        return DbContext.Documents.ToList();
    }

    /// <summary>Создать поток с тестовым содержимым документа</summary>
    protected MemoryStream CreateTestDocumentStream(string content)
    {
        // Простая имитация DOCX файла (в реальности это ZIP архив)
        var bytes = System.Text.Encoding.UTF8.GetBytes(content);
        return new MemoryStream(bytes);
    }
}