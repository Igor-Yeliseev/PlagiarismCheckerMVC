using Microsoft.Extensions.DependencyInjection;
using Moq;
using PlagiarismCheckerMVC.Models;
using PlagiarismCheckerMVC.Services;
using PlagiarismCheckerMVC.Tests.TestData;

namespace PlagiarismCheckerMVC.Tests.Services;

/// <summary>Упрощенные тесты для сервиса проверки плагиата</summary>
[TestFixture]
public class SimplePlagiarismServiceTests : TestBase
{
    private IPlagiarismService _plagiarismService;

    [SetUp]
    public override void Setup()
    {
        base.Setup();
        _plagiarismService = ServiceProvider.GetRequiredService<IPlagiarismService>();
    }

    /// <summary>Тест проверки документа с текстом</summary>
    [Test]
    public void CheckDocument_WithTextContent_ReturnsReport()
    {
        // Arrange
        var documentStream = CreateTestDocumentStream(TestTexts.QuantumPhysicsOriginal);
        
        // Настраиваем мок поискового сервиса
        var searchResults = new List<SearchItem>
        {
            new SearchItem
            {
                Title = "Тестовая статья",
                Link = "https://example.com/test",
                Snippet = "Тестовый фрагмент текста"
            }
        };

        MockSearchService.Setup(x => x.SearchGoogleAsync(It.IsAny<string>()))
            .ReturnsAsync(searchResults);
        MockSearchService.Setup(x => x.SearchYandexAsync(It.IsAny<string>()))
            .ReturnsAsync(searchResults);

        // Act
        var result = _plagiarismService.CheckDocument(documentStream, SearchEngineType.Google);

        // Assert
        Assert.That(result, Is.Not.Null, "Результат проверки не должен быть null");
        Assert.That(result.DocumentName, Is.Not.Null.And.Not.Empty, "Имя документа должно быть установлено");
        Assert.That(result.PlagiarismPercentage, Is.GreaterThanOrEqualTo(0), "Процент плагиата должен быть неотрицательным");
        Assert.That(result.Results, Is.Not.Null, "Результаты поиска не должны быть null");
        Assert.That(result.CheckedAt, Is.Not.EqualTo(default(DateTime)), "Дата проверки должна быть установлена");
    }

    /// <summary>Тест проверки пустого документа</summary>
    [Test]
    public void CheckDocument_WithEmptyContent_ReturnsZeroSimilarity()
    {
        // Arrange
        var documentStream = CreateTestDocumentStream("");

        // Act
        var result = _plagiarismService.CheckDocument(documentStream, SearchEngineType.Google);

        // Assert
        Assert.That(result, Is.Not.Null, "Результат проверки не должен быть null");
        Assert.That(result.PlagiarismPercentage, Is.EqualTo(0), "Процент плагиата для пустого документа должен быть 0");
    }

    /// <summary>Тест проверки с разными поисковыми системами</summary>
    [Test]
    [TestCase(SearchEngineType.Google)]
    [TestCase(SearchEngineType.Yandex)]
    public void CheckDocument_WithDifferentSearchEngines_WorksCorrectly(SearchEngineType searchEngine)
    {
        // Arrange
        var documentStream = CreateTestDocumentStream(TestTexts.BiotechnologyUnique);
        
        var searchResults = new List<SearchItem>
        {
            new SearchItem
            {
                Title = "Тестовая статья",
                Link = "https://example.com/test",
                Snippet = "Тестовый фрагмент"
            }
        };

        MockSearchService.Setup(x => x.SearchGoogleAsync(It.IsAny<string>()))
            .ReturnsAsync(searchResults);
        MockSearchService.Setup(x => x.SearchYandexAsync(It.IsAny<string>()))
            .ReturnsAsync(searchResults);

        // Act
        var result = _plagiarismService.CheckDocument(documentStream, searchEngine);

        // Assert
        Assert.That(result, Is.Not.Null, "Результат проверки не должен быть null");
        Assert.That(result.SearchEngine, Does.Contain(searchEngine.ToString()), 
            "Поисковая система должна соответствовать запрошенной");
    }

    /// <summary>Тест обработки ошибки поискового сервиса</summary>
    [Test]
    public void CheckDocument_WhenSearchServiceFails_HandlesGracefully()
    {
        // Arrange
        var documentStream = CreateTestDocumentStream(TestTexts.QuantumPhysicsOriginal);

        MockSearchService.Setup(x => x.SearchGoogleAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Search service error"));
        MockSearchService.Setup(x => x.SearchYandexAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Search service error"));

        // Act & Assert
        Assert.DoesNotThrow(() =>
        {
            var result = _plagiarismService.CheckDocument(documentStream, SearchEngineType.Google);
            Assert.That(result, Is.Not.Null, "Результат должен быть возвращен даже при ошибке поиска");
        });
    }

    /// <summary>Тест производительности</summary>
    [Test]
    public void CheckDocument_PerformanceTest_CompletesInReasonableTime()
    {
        // Arrange
        var documentStream = CreateTestDocumentStream(TestTexts.ArtificialIntelligenceOriginal);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        MockSearchService.Setup(x => x.SearchGoogleAsync(It.IsAny<string>()))
            .ReturnsAsync(new List<SearchItem>());
        MockSearchService.Setup(x => x.SearchYandexAsync(It.IsAny<string>()))
            .ReturnsAsync(new List<SearchItem>());

        // Act
        var result = _plagiarismService.CheckDocument(documentStream, SearchEngineType.Google);
        stopwatch.Stop();

        // Assert
        Assert.That(result, Is.Not.Null, "Результат не должен быть null");
        Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(5000), 
            "Проверка должна завершиться менее чем за 5 секунд");
        
        Console.WriteLine($"Время выполнения: {stopwatch.ElapsedMilliseconds} мс");
    }

    /// <summary>Тест сравнения текстов</summary>
    [Test]
    public void CompareSimilarity_WithSimilarTexts_ReturnsPositiveScore()
    {
        // Arrange
        var text1 = TestTexts.QuantumPhysicsOriginal;
        var text2 = TestTexts.QuantumPhysicsPlagiarized;

        // Act
        var similarity = CalculateTextSimilarity(text1, text2);

        // Assert
        Assert.That(similarity, Is.GreaterThan(0), "Сходство между похожими текстами должно быть положительным");
        Assert.That(similarity, Is.LessThan(1), "Сходство не должно превышать 100%");
    }

    /// <summary>Тест сравнения разных текстов</summary>
    [Test]
    public void CompareSimilarity_WithDifferentTexts_ReturnsLowScore()
    {
        // Arrange
        var text1 = TestTexts.QuantumPhysicsOriginal;
        var text2 = TestTexts.BiotechnologyUnique;

        // Act
        var similarity = CalculateTextSimilarity(text1, text2);

        // Assert
        Assert.That(similarity, Is.LessThan(0.5), "Сходство между разными текстами должно быть низким");
    }

    /// <summary>Вспомогательный метод для расчета сходства текстов</summary>
    private double CalculateTextSimilarity(string text1, string text2)
    {
        if (string.IsNullOrEmpty(text1) || string.IsNullOrEmpty(text2))
            return 0;

        // Простой алгоритм сходства на основе общих слов
        var words1 = text1.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        var words2 = text2.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        
        var intersection = words1.Intersect(words2).Count();
        var union = words1.Union(words2).Count();
        
        return union > 0 ? (double)intersection / union : 0;
    }
}