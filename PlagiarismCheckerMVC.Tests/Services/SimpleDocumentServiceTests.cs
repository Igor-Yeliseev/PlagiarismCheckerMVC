using Microsoft.Extensions.DependencyInjection;
using PlagiarismCheckerMVC.Models;
using PlagiarismCheckerMVC.Services;
using PlagiarismCheckerMVC.Tests.TestData;
using Microsoft.AspNetCore.Http;
using Moq;

namespace PlagiarismCheckerMVC.Tests.Services;

/// <summary>Упрощенные тесты для сервиса работы с документами</summary>
[TestFixture]
public class SimpleDocumentServiceTests : TestBase
{
    private IDocumentService _documentService;

    [SetUp]
    public override void Setup()
    {
        base.Setup();
        _documentService = ServiceProvider.GetRequiredService<IDocumentService>();
    }

    /// <summary>Тест получения документов пользователя</summary>
    [Test]
    public async Task GetUserDocuments_ReturnsCorrectDocuments()
    {
        // Arrange
        var testUser = GetTestUser();

        // Act
        var documents = await _documentService.GetUserDocumentsAsync(testUser.Id);

        // Assert
        Assert.That(documents, Is.Not.Null, "Список документов не должен быть null");
        Assert.That(documents.Count(), Is.EqualTo(2), "Должно быть возвращено 2 тестовых документа");
        Assert.That(documents.All(d => d.UserId == testUser.Id), Is.True, "Все документы должны принадлежать пользователю");
    }

    /// <summary>Тест получения документов несуществующего пользователя</summary>
    [Test]
    public async Task GetUserDocuments_WithNonExistentUser_ReturnsEmpty()
    {
        // Arrange
        var nonExistentUserId = Guid.NewGuid();

        // Act
        var documents = await _documentService.GetUserDocumentsAsync(nonExistentUserId);

        // Assert
        Assert.That(documents, Is.Not.Null, "Список документов не должен быть null");
        Assert.That(documents.Count(), Is.EqualTo(0), "Для несуществующего пользователя должен возвращаться пустой список");
    }

    /// <summary>Тест добавления нового документа</summary>
    [Test]
    public async Task UploadDocument_CreatesNewDocument()
    {
        // Arrange
        var testUser = GetTestUser();
        var fileName = "new_test_document.docx";
        var mockFile = CreateMockFormFile(fileName, TestTexts.BiotechnologyUnique);
        var initialCount = DbContext.Documents.Count();

        // Act
        var document = await _documentService.UploadAsync(mockFile.Object, testUser.Id);

        // Assert
        Assert.That(document, Is.Not.Null, "Документ должен быть создан");
        Assert.That(document.Id, Is.Not.EqualTo(Guid.Empty), "Должен быть возвращен валидный ID документа");
        
        var newCount = DbContext.Documents.Count();
        Assert.That(newCount, Is.EqualTo(initialCount + 1), "Количество документов должно увеличиться на 1");
        
        Assert.That(document.Name, Is.EqualTo(fileName), "Имя документа должно совпадать");
        Assert.That(document.UserId, Is.EqualTo(testUser.Id), "ID пользователя должен совпадать");
    }

    /// <summary>Тест удаления документа</summary>
    [Test]
    public async Task DeleteDocument_RemovesDocument()
    {
        // Arrange
        var testUser = GetTestUser();
        var testDocuments = GetTestDocuments();
        var documentToDelete = testDocuments.First();
        var initialCount = DbContext.Documents.Count();

        // Act
        await _documentService.DeleteAsync(documentToDelete.Id, testUser.Id);

        // Assert
        var newCount = DbContext.Documents.Count();
        Assert.That(newCount, Is.EqualTo(initialCount - 1), "Количество документов должно уменьшиться на 1");
        
        var deletedDocument = DbContext.Documents.Find(documentToDelete.Id);
        Assert.That(deletedDocument, Is.Null, "Документ не должен быть найден в базе данных");
    }

    /// <summary>Тест получения документа по ID</summary>
    [Test]
    public async Task GetDocument_ReturnsCorrectDocument()
    {
        // Arrange
        var testDocuments = GetTestDocuments();
        var expectedDocument = testDocuments.First();

        // Act
        var document = await _documentService.GetByIdAsync(expectedDocument.Id);

        // Assert
        Assert.That(document, Is.Not.Null, "Документ должен быть найден");
        Assert.That(document.Id, Is.EqualTo(expectedDocument.Id), "ID документа должен совпадать");
        Assert.That(document.Name, Is.EqualTo(expectedDocument.Name), "Имя документа должно совпадать");
    }

    /// <summary>Тест получения документов с оригинальностью</summary>
    [Test]
    public async Task GetUserDocumentsWithOriginality_ReturnsDocumentViews()
    {
        // Arrange
        var testUser = GetTestUser();

        // Act
        var documentViews = await _documentService.GetUserDocumentsWithOriginalityAsync(testUser.Id);

        // Assert
        Assert.That(documentViews, Is.Not.Null, "Список документов должен быть возвращен");
        Assert.That(documentViews.Count(), Is.EqualTo(2), "Должно быть возвращено 2 документа");
        
        foreach (var docView in documentViews)
        {
            Assert.That(docView.Id, Is.Not.EqualTo(Guid.Empty), "ID документа должен быть валидным");
            Assert.That(docView.Name, Is.Not.Null.And.Not.Empty, "Имя документа не должно быть пустым");
        }
    }

    /// <summary>Тест получения количества документов пользователя</summary>
    [Test]
    public async Task GetDocumentCount_ReturnsCorrectCount()
    {
        // Arrange
        var testUser = GetTestUser();

        // Act
        var count = await _documentService.GetUserDocumentCountAsync(testUser.Id);

        // Assert
        Assert.That(count, Is.EqualTo(2), "Количество документов пользователя должно быть 2");
    }

    /// <summary>Тест получения количества документов для несуществующего пользователя</summary>
    [Test]
    public async Task GetDocumentCount_WithNonExistentUser_ReturnsZero()
    {
        // Arrange
        var nonExistentUserId = Guid.NewGuid();

        // Act
        var count = await _documentService.GetUserDocumentCountAsync(nonExistentUserId);

        // Assert
        Assert.That(count, Is.EqualTo(0), "Количество документов для несуществующего пользователя должно быть 0");
    }

    /// <summary>Тест обработки исключений при загрузке null файла</summary>
    [Test]
    public void UploadDocument_WithNullFile_ThrowsException()
    {
        // Arrange
        var testUser = GetTestUser();
        IFormFile nullFile = null;

        // Act & Assert
        Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _documentService.UploadAsync(nullFile, testUser.Id));
    }

    /// <summary>Тест обработки исключений при удалении несуществующего документа</summary>
    [Test]
    public void DeleteDocument_WithNonExistentId_ThrowsException()
    {
        // Arrange
        var testUser = GetTestUser();
        var nonExistentDocumentId = Guid.NewGuid();

        // Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _documentService.DeleteAsync(nonExistentDocumentId, testUser.Id));
    }

    /// <summary>Тест производительности получения документов</summary>
    [Test]
    public async Task GetUserDocuments_PerformanceTest_CompletesQuickly()
    {
        // Arrange
        var testUser = GetTestUser();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var documents = await _documentService.GetUserDocumentsAsync(testUser.Id);
        stopwatch.Stop();

        // Assert
        Assert.That(documents, Is.Not.Null, "Документы должны быть получены");
        Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(1000), 
            "Получение документов должно завершиться менее чем за 1 секунду");
        
        Console.WriteLine($"Время получения документов: {stopwatch.ElapsedMilliseconds} мс");
    }

    /// <summary>Создает мок IFormFile для тестирования</summary>
    private Mock<IFormFile> CreateMockFormFile(string fileName, string content)
    {
        var mockFile = new Mock<IFormFile>();
        var contentBytes = System.Text.Encoding.UTF8.GetBytes(content);
        var stream = new MemoryStream(contentBytes);

        mockFile.Setup(f => f.FileName).Returns(fileName);
        mockFile.Setup(f => f.Length).Returns(contentBytes.Length);
        mockFile.Setup(f => f.OpenReadStream()).Returns(stream);
        mockFile.Setup(f => f.ContentType).Returns("application/vnd.openxmlformats-officedocument.wordprocessingml.document");

        return mockFile;
    }
}