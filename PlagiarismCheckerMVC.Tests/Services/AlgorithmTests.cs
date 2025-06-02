using PlagiarismCheckerMVC.Algorithms;
using PlagiarismCheckerMVC.Tests.TestData;

namespace PlagiarismCheckerMVC.Tests.Services;

/// <summary>Тесты для алгоритмов сравнения текстов</summary>
[TestFixture]
public class AlgorithmTests
{
    private const int DefaultNGramSize = 3;

    [SetUp]
    public void Setup()
    {
        // Настройка не требуется для статических методов
    }

    /// <summary>Тест сравнения идентичных текстов</summary>
    [Test]
    public void Compare_IdenticalTexts_ReturnsHighSimilarity()
    {
        // Arrange
        var text1 = TestTexts.QuantumPhysicsOriginal;
        var text2 = TestTexts.QuantumPhysicsOriginal;

        // Act
        var similarity = CalculateTextSimilarity(text1, text2);

        // Assert
        Assert.That(similarity, Is.GreaterThan(0.95), "Идентичные тексты должны иметь очень высокое сходство");
    }

    /// <summary>Тест сравнения похожих текстов (плагиат)</summary>
    [Test]
    public void Compare_SimilarTexts_ReturnsModerateToHighSimilarity()
    {
        // Arrange
        var originalText = TestTexts.QuantumPhysicsOriginal;
        var plagiarizedText = TestTexts.QuantumPhysicsPlagiarized;

        // Act
        var similarity = CalculateTextSimilarity(originalText, plagiarizedText);

        // Assert
        Assert.That(similarity, Is.GreaterThan(0.1), "Похожие тексты должны иметь некоторое сходство");
        Assert.That(similarity, Is.LessThan(0.9), "Перефразированные тексты не должны иметь слишком высокое сходство");
    }

    /// <summary>Тест сравнения разных текстов</summary>
    [Test]
    public void Compare_DifferentTexts_ReturnsLowSimilarity()
    {
        // Arrange
        var text1 = TestTexts.QuantumPhysicsOriginal;
        var text2 = TestTexts.BiotechnologyUnique;

        // Act
        var similarity = CalculateTextSimilarity(text1, text2);

        // Assert
        Assert.That(similarity, Is.LessThan(0.3), "Разные тексты должны иметь низкое сходство");
    }

    /// <summary>Тест сравнения пустых текстов</summary>
    [Test]
    public void Compare_EmptyTexts_ReturnsZeroSimilarity()
    {
        // Arrange
        var text1 = "";
        var text2 = "";

        // Act
        var similarity = CalculateTextSimilarity(text1, text2);

        // Assert
        Assert.That(similarity, Is.EqualTo(0), "Пустые тексты должны иметь нулевое сходство");
    }

    /// <summary>Тест сравнения текста с пустым текстом</summary>
    [Test]
    public void Compare_TextWithEmpty_ReturnsZeroSimilarity()
    {
        // Arrange
        var text1 = TestTexts.BiotechnologyUnique;
        var text2 = "";

        // Act
        var similarity = CalculateTextSimilarity(text1, text2);

        // Assert
        Assert.That(similarity, Is.EqualTo(0), "Сравнение с пустым текстом должно давать нулевое сходство");
    }

    /// <summary>Тест сравнения коротких текстов</summary>
    [Test]
    public void Compare_ShortTexts_WorksCorrectly()
    {
        // Arrange
        var text1 = "Короткий текст для тестирования";
        var text2 = "Короткий текст для проверки";

        // Act
        var similarity = CalculateTextSimilarity(text1, text2);

        // Assert
        Assert.That(similarity, Is.GreaterThan(0), "Короткие похожие тексты должны иметь некоторое сходство");
        Assert.That(similarity, Is.LessThan(1), "Разные короткие тексты не должны быть идентичными");
    }

    /// <summary>Тест устойчивости к регистру</summary>
    [Test]
    public void Compare_DifferentCase_ReturnsHighSimilarity()
    {
        // Arrange
        var text1 = "Тестовый текст для проверки алгоритма";
        var text2 = "ТЕСТОВЫЙ ТЕКСТ ДЛЯ ПРОВЕРКИ АЛГОРИТМА";

        // Act
        var similarity = CalculateTextSimilarity(text1, text2);

        // Assert
        Assert.That(similarity, Is.GreaterThan(0.8), "Тексты с разным регистром должны иметь высокое сходство");
    }

    /// <summary>Тест устойчивости к знакам препинания</summary>
    [Test]
    public void Compare_DifferentPunctuation_ReturnsHighSimilarity()
    {
        // Arrange
        var text1 = "Тестовый текст, для проверки алгоритма.";
        var text2 = "Тестовый текст для проверки алгоритма";

        // Act
        var similarity = CalculateTextSimilarity(text1, text2);

        // Assert
        Assert.That(similarity, Is.GreaterThan(0.7), "Тексты с разной пунктуацией должны иметь высокое сходство");
    }

    /// <summary>Тест производительности алгоритма</summary>
    [Test]
    public void Compare_LargeTexts_CompletesInReasonableTime()
    {
        // Arrange
        var largeText1 = string.Join(" ", Enumerable.Repeat(TestTexts.QuantumPhysicsOriginal, 50));
        var largeText2 = string.Join(" ", Enumerable.Repeat(TestTexts.QuantumPhysicsPlagiarized, 50));
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var similarity = CalculateTextSimilarity(largeText1, largeText2);
        stopwatch.Stop();

        // Assert
        Assert.That(similarity, Is.GreaterThan(0), "Алгоритм должен работать с большими текстами");
        Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(5000), "Сравнение больших текстов должно завершаться менее чем за 5 секунд");
        
        Console.WriteLine($"Время сравнения больших текстов: {stopwatch.ElapsedMilliseconds} мс");
        Console.WriteLine($"Сходство: {similarity:F3}");
    }

    /// <summary>Тест различных размеров N-грамм</summary>
    [Test]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    public void Compare_DifferentNGramSizes_ProducesReasonableResults(int nGramSize)
    {
        // Arrange
        var text1 = TestTexts.ArtificialIntelligenceOriginal;
        var text2 = TestTexts.ArtificialIntelligencePlagiarized;

        // Act
        var similarity = CalculateTextSimilarity(text1, text2, nGramSize);

        // Assert
        Assert.That(similarity, Is.GreaterThanOrEqualTo(0), $"N-граммы размера {nGramSize} должны давать неотрицательное сходство");
        Assert.That(similarity, Is.LessThan(1), $"N-граммы размера {nGramSize} не должны давать полное сходство для разных текстов");
        
        Console.WriteLine($"N-грамм размера {nGramSize}: сходство = {similarity:F3}");
    }

    /// <summary>Тест симметричности алгоритма</summary>
    [Test]
    public void Compare_Symmetry_ReturnsEqualResults()
    {
        // Arrange
        var text1 = TestTexts.QuantumPhysicsOriginal;
        var text2 = TestTexts.QuantumPhysicsPlagiarized;

        // Act
        var similarity1 = CalculateTextSimilarity(text1, text2);
        var similarity2 = CalculateTextSimilarity(text2, text1);

        // Assert
        Assert.That(similarity1, Is.EqualTo(similarity2).Within(0.001), 
            "Алгоритм должен быть симметричным: similarity(A,B) = similarity(B,A)");
    }

    /// <summary>Тест обработки специальных символов</summary>
    [Test]
    public void Compare_SpecialCharacters_HandlesCorrectly()
    {
        // Arrange
        var text1 = "Текст с числами 123 и символами @#$%";
        var text2 = "Текст с числами 456 и символами &*()";

        // Act
        var similarity = CalculateTextSimilarity(text1, text2);

        // Assert
        Assert.That(similarity, Is.GreaterThanOrEqualTo(0), "Тексты со специальными символами должны обрабатываться корректно");
        Assert.That(similarity, Is.LessThan(0.8), "Тексты с разными числами и символами не должны иметь высокое сходство");
    }

    /// <summary>Вспомогательный метод для расчета сходства текстов</summary>
    private double CalculateTextSimilarity(string text1, string text2, int nGramSize = DefaultNGramSize)
    {
        if (string.IsNullOrEmpty(text1) || string.IsNullOrEmpty(text2))
            return 0;

        // Очищаем тексты от пробелов для сравнения
        var cleanText1 = text1.Replace(" ", "").ToLower();
        var cleanText2 = text2.Replace(" ", "").ToLower();

        var set1 = NGramShingleComparator.GetNGramHashes(cleanText1, nGramSize);
        var set2 = NGramShingleComparator.GetNGramHashes(cleanText2, nGramSize);

        return NGramShingleComparator.CalculateSimilarity(set1, set2) / 100.0; // Конвертируем в диапазон 0-1
    }
}