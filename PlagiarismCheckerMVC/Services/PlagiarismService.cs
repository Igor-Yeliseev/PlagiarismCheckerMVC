using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Options;
using PlagiarismCheckerMVC.Models;
using System.Net.Http;
using AngleSharp;
using AngleSharp.Html.Parser;
using FuzzySharp;
using FuzzySharp.SimilarityRatio;
using FuzzySharp.SimilarityRatio.Scorer.StrategySensitive;
using System.Diagnostics;
using System.Text.Json;

namespace PlagiarismCheckerMVC.Services
{
    public class PlagiarismService : IPlagiarismService
    {
        private readonly IDocumentService _documentService;
        private readonly IStorageService _storageService;
        private readonly ISearchService _searchService;
        private readonly PlagiarismSettings _plagiarismSettings;
        private readonly HttpClient _httpClient;
        
        // Поле для хранения текущего процента заимствований (умноженное на 100 для точности до сотых)
        private long _plagiarismPercentage;
        
        // Поле для общего количества слов в тексте
        private int _totalWordCount;
        
        // Флаг для прекращения проверки при превышении порога
        private bool _isThresholdExceeded;

        public PlagiarismService(
            IDocumentService documentService,
            IStorageService storageService,
            ISearchService searchService,
            IOptions<PlagiarismSettings> plagiarismSettings)
        {
            _documentService = documentService;
            _storageService = storageService;
            _searchService = searchService;
            _plagiarismSettings = plagiarismSettings.Value;
            _httpClient = new HttpClient();
        }

        /// <summary> Проверяет документ на плагиат и возвращает отчет</summary>
        public async Task<DocCheckReport> CheckDocumentAsync(Guid documentId, SearchEngineType searchEngineType)
        {
            var document = await _documentService.GetByIdAsync(documentId);
            if (document == null)
            {
                throw new InvalidOperationException("Документ не найден");
            }

            using var docStream = await _storageService.DownloadFileAsync(document.DocFileUrl); // Загружаем файл документа
            
            try
            {
                var paragraphs = ExtractParagraphs(docStream); // Извлекаем абзацы из документа

                var results = SearchPlag(paragraphs, searchEngineType); // Проверяем на плагиат

                // Проверяем уникальность через внешний API
                var dbCheckResult = await CheckPlagDbAsync(docStream);
                
                return new DocCheckReport
                {
                    DocumentName = document.Name,
                    Results = results,
                    CheckedAt = DateTime.UtcNow,
                    SearchEngine = searchEngineType.ToString(),
                    PlagiarismPercentage = Math.Max((decimal)_plagiarismPercentage / 100, 100 - dbCheckResult)
                };
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary> Проверяет переданный поток документа на плагиат и возвращает отчет</summary>
        public DocCheckReport CheckDocument(Stream docStream, SearchEngineType searchEngineType = SearchEngineType.Google)
        {
            try
            {
                // Сбрасываем позицию потока перед чтением
                docStream.Position = 0;
                
                // Извлекаем абзацы из документа
                var paragraphs = ExtractParagraphs(docStream);
                
                // Проверяем на плагиат
                var results = SearchPlag(paragraphs, searchEngineType);
                
                // Проверяем уникальность через внешний API
                var dbCheckResult = CheckPlagDbAsync(docStream).GetAwaiter().GetResult();
                
                // Формируем отчет
                return new DocCheckReport
                {
                    DocumentName = "Документ",
                    Results = results,
                    CheckedAt = DateTime.UtcNow,
                    SearchEngine = searchEngineType.ToString(),
                    PlagiarismPercentage = Math.Max((decimal)_plagiarismPercentage / 100, 100 - dbCheckResult)
                };
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary> Проверяет документ на плагиат через внешний API и возвращает процент уникальности</summary>
        private async Task<decimal> CheckPlagDbAsync(Stream docStream)
        {
            try
            {
                // Сбрасываем позицию потока перед чтением
                docStream.Position = 0;
                
                // Создаем контент для отправки
                var content = new MultipartFormDataContent();
                var streamContent = new StreamContent(docStream);
                content.Add(streamContent, "file", "document.docx");
                
                // Отправляем запрос на внешний API
                var response = await _httpClient.PostAsync("http://localhost:5000/py-api/check-with-db", content);
                
                // Если запрос неуспешен, возвращаем 100% уникальности (0% плагиата)
                if (!response.IsSuccessStatusCode)
                {
                    return 100m;
                }
                
                // Читаем ответ
                var jsonString = await response.Content.ReadAsStringAsync();
                using JsonDocument doc = JsonDocument.Parse(jsonString);
                
                // Получаем процент уникальности (предполагается, что API возвращает uniqueness_percentage)
                if (doc.RootElement.TryGetProperty("uniqueness_percentage", out JsonElement element) && 
                    element.TryGetDecimal(out decimal uniquenessPercentage))
                {
                    return uniquenessPercentage;
                }
                
                // По умолчанию возвращаем 100% уникальности, если не удалось получить данные
                return 100m;
            }
            catch (Exception ex)
            {
                // Логируем ошибку
                Debug.WriteLine($"Ошибка при проверке документа через внешний API: {ex.Message}");
                
                // Возвращаем 100% уникальности в случае ошибки
                return 100m;
            }
            finally
            {
                // Сбрасываем позицию потока для дальнейшего использования
                docStream.Position = 0;
            }
        }

        /// <summary> Извлекает абзацы и предложения из потока .docx документа</summary>
        private List<WordParagraph> ExtractParagraphs(Stream documentStream)
        {
            var paragraphs = new List<WordParagraph>();

            using (WordprocessingDocument document = WordprocessingDocument.Open(documentStream, false))
            {
                var body = document?.MainDocumentPart?.Document.Body;
                if (body == null)
                    return new List<WordParagraph>();

                // Обрабатываем абзацы
                foreach (var paragraph in body.Descendants<Paragraph>())
                {
                    string paragraphText = paragraph.InnerText.Trim();
                    
                    // Пропускаем пустые абзацы
                    if (string.IsNullOrWhiteSpace(paragraphText))
                        continue;

                    // Разбиваем текст на предложения с помощью регулярного выражения
                    var sentences = Regex.Split(paragraphText, @"(?<=[.!?])\s+")
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .ToList();

                    // Проверяем минимальное количество предложений и минимальную длину абзаца
                    if (sentences.Count >= _plagiarismSettings.SentMinCount ||
                        paragraphText.Length >= _plagiarismSettings.ParaMinChars)
                    {
                        // Создаем объект WordParagraph и добавляем его в список
                        var wordParagraph = new WordParagraph { Sentences = sentences };
                        paragraphs.Add(wordParagraph);
                    }
                }
            }

            return paragraphs;
        }

        /// <summary> Проверяет список абзацев на плагиат, возвращает найденные совпадения</summary>
        private List<QuerySearchResult> SearchPlag(List<WordParagraph> paragraphs, SearchEngineType searchEngineType)
        {
            // Сбрасываем счетчик заимствований перед проверкой
            _plagiarismPercentage = 0;
            _isThresholdExceeded = false;
            
            // Рассчитываем общее количество слов в тексте
            _totalWordCount = CountTotalWords(paragraphs);
            
            // Если текст пустой, возвращаем пустой результат
            if (_totalWordCount == 0)
                return new List<QuerySearchResult>();
                
            var tasks = new Task<List<QuerySearchResult>>[paragraphs.Count];
            int sentNum = 0;
            for (int i = 0; i < paragraphs.Count; i++)
            {
                int startSentNum = sentNum;
                int paraIndex = i;
                tasks[i] = Task.Run(() => 
                {
                    // Проверяем, не превышен ли порог заимствований
                    if (_isThresholdExceeded)
                        return new List<QuerySearchResult>();
                        
                    return CheckParaSentences(paragraphs[paraIndex], startSentNum, searchEngineType, paraIndex);
                });
                sentNum += paragraphs[i].Sentences.Count;
            }
            Task.WaitAll(tasks);
            var results = tasks.SelectMany(t => t.Result).OrderByDescending(r => r.SimilarityScore).ToList();
            
            return results;
        }

        /// <summary> Проверяет предложения абзаца на плагиат, возвращает найденные совпадения</summary>
        private List<QuerySearchResult> CheckParaSentences(WordParagraph paragraph, int startSentNum, SearchEngineType searchEngineType, int paraNum)
        {
            var paraResults = new List<QuerySearchResult>();
            int sentNum = startSentNum;
            
            // Словарь для хранения загруженного контента ресурсов
            var cachedContent = new Dictionary<string, string>();
            
            foreach (var sentence in paragraph.Sentences)
            {
                // Проверяем, не превышен ли порог
                if (_isThresholdExceeded)
                    return paraResults;
                    
                if (string.IsNullOrWhiteSpace(sentence) || sentence.Length < _plagiarismSettings.SentMinChars)
                {
                    sentNum++;
                    continue;
                }

                bool foundInCachedSources = false;
                string cleanedSentence = CleanText(sentence).ToLower();
                
                // Проверяем на плагиат в уже загруженных источниках
                foreach (var source in cachedContent)
                {
                    var (localizedText, localSimilarity) = FindMostSimilarFragment(cleanedSentence, source.Value);
                    
                    if (localSimilarity > 0.75)
                    {
                        foundInCachedSources = true;
                        
                        // Добавляем результат
                        paraResults.Add(new QuerySearchResult
                        {
                            SourceUrl = source.Key,
                            SourceTitle = $"Источник с {source.Key}",
                            SimilarityScore = localSimilarity,
                            ParaNum = paraNum,
                            SentNum = sentNum
                        });
                        
                        // Учитываем процент заимствования
                        UpdatePlagiarismPercentage(sentence);
                        break;
                    }
                }
                
                // Если нашли плагиат в кешированных источниках, переходим к следующему предложению
                if (foundInCachedSources)
                {
                    sentNum++;
                    continue;
                }

                // Формируем запрос для поиска из предложения
                string query = GenerateQuery(sentence);

                // Получаем результаты поиска
                List<SearchItem> searchItems;
                switch (searchEngineType)
                {
                    case SearchEngineType.Google:
                        searchItems = _searchService.SearchGoogleAsync(query).GetAwaiter().GetResult();
                        break;
                    case SearchEngineType.Yandex:
                        searchItems = _searchService.SearchYandexAsync(query).GetAwaiter().GetResult();
                        break;
                    default:
                        throw new ArgumentException("Некорректное значение параметра searchEngineType");
                }

                // Обрабатываем результаты поиска с сохранением контента в кеш
                var queryResults = ProcessSearchItemsWithCaching(searchItems, sentence, query, cachedContent);
                
                if (queryResults.Count > 0)
                {
                    paraResults.AddRange(queryResults.Select(res =>
                    {
                        res.ParaNum = paraNum;
                        res.SentNum = sentNum;
                        return res;
                    }));
                    
                    // Учитываем процент заимствования
                    UpdatePlagiarismPercentage(sentence);
                }
                sentNum++;
            }
            return paraResults;
        }

        /// <summary> Обновляет общий процент заимствования для текста на основе найденного предложения с плагиатом</summary>
        private void UpdatePlagiarismPercentage(string sentence)
        {
            // Рассчитываем количество слов в предложении
            int sentenceWordCount = sentence.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
            
            // Рассчитываем процент от общего текста (с точностью до сотых)
            long sentencePercentage = (long)(sentenceWordCount * 10000.0 / _totalWordCount);
            
            // Атомарно добавляем к общему проценту заимствований (без блокировок)
            long currentPercentage = Interlocked.Add(ref _plagiarismPercentage, sentencePercentage);
            
            // Проверяем превышение порога в 25%
            if (currentPercentage >= 2500)
            {
                _isThresholdExceeded = true;
            }
        }

        /// <summary> Генерирует поисковый запрос из предложения</summary>
        private string GenerateQuery(string sentence)
        {
            // Очищаем предложение от знаков препинания и специальных символов
            string cleanedSentence = CleanText(sentence);

            // Выделяем ключевые слова из предложения
            var keywords = ExtractKeywords(cleanedSentence);

            // Формируем запрос из ключевых слов
            string query = string.Join(" ", keywords);

            // Если длина запроса превышает 2048 символов, обрезаем
            if (query.Length > 2048)
            {
                query = query[..2048];
            }

            return query;
        }

        /// <summary> Извлекает ключевые слова из текста</summary>
        private List<string> ExtractKeywords(string text)
        {
            // Словарь стоп-слов (часто встречающиеся слова, не несущие смысловой нагрузки)
            var stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "и", "в", "во", "не", "что", "он", "на", "я", "с", "со", "как", "а", "то", "все", "она", "так",
                "его", "но", "да", "ты", "к", "у", "же", "вы", "за", "бы", "по", "только", "ее", "мне", "было",
                "вот", "от", "меня", "еще", "нет", "о", "из", "ему", "теперь", "когда", "даже", "ну", "вдруг",
                "ли", "если", "уже", "или", "ни", "быть", "был", "него", "до", "вас", "нибудь", "опять", "уж",
                "вам", "сказал", "ведь", "там", "потом", "себя", "ничего", "ей", "может", "они", "тут", "где",
                "есть", "надо", "ней", "для", "мы", "тебя", "их", "чем", "была", "сам", "чтоб", "без", "будто",
                "человек", "чего", "раз", "тоже", "себе", "под", "жизнь", "будет", "ж", "тогда", "кто", "этот",
                "того", "потому", "этого", "какой", "совсем", "ним", "здесь", "этом", "один", "почти", "мой", "тем",
                "чтобы", "нее", "кажется", "сейчас", "были", "куда", "зачем", "сказать", "всех", "никогда", "сегодня",
                "можно", "при", "наконец", "два", "об", "другой", "хоть", "после", "над", "больше", "тот", "через",
                "эти", "нас", "про", "всего", "них", "какая", "много", "разве", "сказала", "три", "эту", "моя",
                "впрочем", "хорошо", "свою", "этой", "перед", "иногда", "лучше", "чуть", "том", "нельзя", "такой",
                "им", "более", "всегда", "конечно", "всю", "между",
                // Английские стоп-слова
                "the", "and", "a", "of", "to", "in", "is", "you", "that", "it", "he", "was", "for", "on", "are",
                "as", "with", "his", "they", "at", "be", "this", "have", "from", "or", "one", "had", "by", "but",
                "not", "what", "all", "were", "we", "when", "your", "can", "said", "there", "use", "an", "each",
                "which", "she", "do", "how", "their", "if", "will", "up", "other", "about", "out", "many", "then",
                "them", "these", "so", "some", "her", "would", "make", "like", "him", "into", "time", "has", "look",
                "two", "more", "write", "go", "see", "number", "no", "way", "could", "people", "my", "than", "first",
                "water", "been", "call"
            };

            // Разбиваем текст на слова
            var words = Regex.Split(text, @"\W+")
                .Where(w => !string.IsNullOrWhiteSpace(w))
                .Select(w => w.ToLower())
                .Where(w => w.Length > 1 && !stopWords.Contains(w))
                .ToList();

            // Вычисляем частоту каждого слова (Term Frequency)
            var wordFrequency = new Dictionary<string, int>();
            foreach (var word in words)
            {
                if (wordFrequency.ContainsKey(word))
                    wordFrequency[word]++;
                else
                    wordFrequency[word] = 1;
            }

            // Вычисляем вес слова (простая имитация TF-IDF)
            var wordScore = new Dictionary<string, double>();
            foreach (var word in wordFrequency.Keys)
            {
                // Учитываем частоту слова и его длину (более длинные слова часто более значимы)
                wordScore[word] = wordFrequency[word] * (0.5 + 0.5 * Math.Min(word.Length, 10) / 10.0);

                // Если слово написано с большой буквы в оригинальном тексте, увеличиваем его вес
                if (text.Contains(" " + char.ToUpper(word[0]) + word.Substring(1)))
                    wordScore[word] *= 1.5;
            }

            // Выбираем топ N ключевых слов (или все слова, если их меньше N)
            int topN = Math.Min(10, wordScore.Count);

            return wordScore
                .OrderByDescending(kv => kv.Value)
                .Take(topN)
                .Select(kv => kv.Key)
                .ToList();
        }

        /// <summary> Обрабатывает результаты поиска и определяет потенциальные заимствования, сохраняя загруженный контент</summary>
        private List<QuerySearchResult> ProcessSearchItemsWithCaching(List<SearchItem> searchItems, string originalSentence, string query, Dictionary<string, string> cachedContent)
        {
            var results = new List<QuerySearchResult>();

            if (searchItems == null || !searchItems.Any())
                return results;

            string cleanedOriginalSentence = CleanText(originalSentence).ToLower();

            // Предварительная фильтрация по сниппетам
            foreach (var item in searchItems)
            {
                if (string.IsNullOrWhiteSpace(item.Snippet))
                    continue;

                string cleanedSnippet = CleanText(item.Snippet).ToLower();

                // Используем быстрое сравнение с помощью FuzzySharp для определения схожести
                double similarityScore = CalculateSimilarity(cleanedOriginalSentence, cleanedSnippet);

                if (similarityScore < 0.5)
                    continue;

                // Если схожесть превышает порог, загружаем страницу для более детального анализа
                try
                {
                    string pageContent;
                    
                    // Проверяем, был ли этот URL уже загружен
                    if (cachedContent.ContainsKey(item.Link))
                    {
                        pageContent = cachedContent[item.Link];
                    }
                    else
                    {
                        // Загружаем страницу и сохраняем в кеш
                        pageContent = DownloadAndExtractMainContent(item.Link).GetAwaiter().GetResult();
                        if (!string.IsNullOrWhiteSpace(pageContent))
                        {
                            cachedContent[item.Link] = pageContent;
                        }
                    }

                    if (string.IsNullOrWhiteSpace(pageContent))
                        continue;

                    // Локализуем фрагмент с наибольшим сходством
                    var (localizedText, localSimilarity) = FindMostSimilarFragment(cleanedOriginalSentence, pageContent);

                    // Если сходство достаточно высокое, добавляем в результаты
                    if (localSimilarity < 0.75)
                        continue;

                    results.Add(new QuerySearchResult
                    {
                        SourceUrl = item.Link,
                        SourceTitle = item.Title,
                        SimilarityScore = localSimilarity
                    });
                }
                catch (Exception ex)
                {
                    // Логируем ошибку, но продолжаем обработку других результатов
                    Debug.WriteLine($"Ошибка при загрузке страницы {item.Link}: {ex.Message}");
                }
            }

            return results;
        }

        /// <summary> Вычисляет схожесть между двумя строками</summary>
        private double CalculateSimilarity(string str1, string str2)
        {
            // Используем как Jaccard (для коротких текстов), так и алгоритм Левенштейна (для общего случая)
            double ratio = Fuzz.TokenSortRatio(str1, str2) / 100.0;
            double partialRatio = Fuzz.PartialRatio(str1, str2) / 100.0;
            double tokenSetRatio = Fuzz.TokenSetRatio(str1, str2) / 100.0;

            // Комбинируем разные метрики для более точного результата
            return Math.Max(ratio, Math.Max(partialRatio, tokenSetRatio));
        }

        /// <summary> Загружает страницу и извлекает основной контент</summary>
        private async Task<string> DownloadAndExtractMainContent(string url)
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");

            try
            {
                var response = await httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                    return string.Empty;

                var html = await response.Content.ReadAsStringAsync();

                // Используем AngleSharp для парсинга HTML
                var config = Configuration.Default;
                var context = BrowsingContext.New(config);
                var parser = context.GetService<IHtmlParser>();
                var document = await parser.ParseDocumentAsync(html);

                // Извлекаем основной контент, удаляя меню, навигацию, скрипты и стили
                var contentNodes = document.QuerySelectorAll("p, h1, h2, h3, h4, h5, h6, article, section, div.content, div.main");

                var contentBuilder = new StringBuilder();
                foreach (var node in contentNodes)
                {
                    // Пропускаем элементы навигации, футера и т.д.
                    if (node.ClassList.Contains("nav") || node.ClassList.Contains("footer") ||
                        node.ClassList.Contains("header") || node.ClassList.Contains("menu") ||
                        node.ParentElement?.ClassList.Contains("nav") == true)
                        continue;

                    string text = node.TextContent.Trim();
                    if (!string.IsNullOrWhiteSpace(text))
                        contentBuilder.AppendLine(text);
                }

                return contentBuilder.ToString();
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        /// <summary> Находит фрагмент в тексте с наибольшим сходством с исходным предложением</summary>
        private (string text, double similarity) FindMostSimilarFragment(string originalSentence, string content)
        {
            // Разбиваем контент на предложения
            var sentences = Regex.Split(content, @"(?<=[.!?])\s+")
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => CleanText(s).ToLower())
                .ToList();

            string bestMatch = string.Empty;
            double bestSimilarity = 0;

            // Ищем отдельные предложения с высоким сходством
            foreach (var sentence in sentences)
            {
                double similarity = CalculateSimilarity(originalSentence, sentence);
                if (similarity > bestSimilarity)
                {
                    bestSimilarity = similarity;
                    bestMatch = sentence;
                }
            }

            // Если не нашли хорошего совпадения среди отдельных предложений,
            // пробуем скользящее окно для поиска фрагментов текста
            if (bestSimilarity < 0.7)
            {
                // Объединяем все предложения в единый текст
                string fullText = string.Join(" ", sentences);
                string[] words = fullText.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                // Определяем размер окна примерно равный длине исходного предложения
                int originalWordCount = originalSentence.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
                int windowSize = Math.Min(words.Length, Math.Max(5, originalWordCount * 2));

                // Перебираем скользящие окна
                for (int i = 0; i <= words.Length - windowSize; i += windowSize / 2)
                {
                    string fragment = string.Join(" ", words.Skip(i).Take(windowSize));
                    double similarity = CalculateSimilarity(originalSentence, fragment);

                    if (similarity > bestSimilarity)
                    {
                        bestSimilarity = similarity;
                        bestMatch = fragment;
                    }
                }
            }

            return (bestMatch, bestSimilarity);
        }

        /// <summary> Очищает текст от лишних символов и стоп-слов</summary>
        private string CleanText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            // Удаляем лишние пробелы, табуляции и переносы строк
            var cleanedText = Regex.Replace(text, @"\s+", " ").Trim();

            // Удаляем стоп-слова
            var stopWords = new[] { "и", "в", "во", "не", "что", "он", "на", "я", "с", "со", "как", "а", "то", "все", "она", "так", "его", "но", "да", "ты", "к", "у", "же", "вы", "за", "бы", "по", "только", "ее", "мне", "было", "вот", "от", "меня", "еще", "нет", "о", "из", "ему", "теперь", "когда", "даже", "ну", "вдруг", "ли", "если", "уже", "или", "ни", "быть", "был", "него", "до", "вас", "нибудь", "опять", "уж", "вам", "сказал", "ведь", "там", "потом", "себя", "ничего", "ей", "может", "они", "тут", "где", "есть", "надо", "ней", "для", "мы", "тебя", "их", "чем", "была", "сам", "чтоб", "без", "будто", "человек", "чего", "раз", "тоже", "себе", "под", "жизнь", "будет", "ж", "тогда", "кто", "этот", "того", "потому", "этого", "какой", "совсем", "ним", "здесь", "этом", "один", "почти", "мой", "тем", "чтобы", "нее", "кажется", "сейчас", "были", "куда", "зачем", "сказать", "всех", "никогда", "сегодня", "можно", "при", "наконец", "два", "об", "другой", "хоть", "после", "над", "больше", "тот", "через", "эти", "нас", "про", "всего", "них", "какая", "много", "разве", "сказала", "три", "эту", "моя", "впрочем", "хорошо", "свою", "этой", "перед", "иногда", "лучше", "чуть", "том", "нельзя", "такой", "им", "более", "всегда", "конечно", "всю", "между" };

            var words = cleanedText.Split(' ');
            var filteredWords = words.Where(w => !stopWords.Contains(w.ToLower())).ToList();

            return string.Join(" ", filteredWords);
        }

        /// <summary> Подсчитывает общее количество слов во всех абзацах</summary>
        private int CountTotalWords(List<WordParagraph> paragraphs)
        {
            int totalWords = 0;
            foreach (var paragraph in paragraphs)
            {
                foreach (var sentence in paragraph.Sentences)
                {
                    // Подсчитываем количество слов в каждом предложении
                    totalWords += sentence.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
                }
            }
            return totalWords;
        }
    }
}