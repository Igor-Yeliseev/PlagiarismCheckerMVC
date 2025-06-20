using Microsoft.EntityFrameworkCore;
using PlagiarismCheckerMVC.Models;

namespace PlagiarismCheckerMVC.Services
{
    public class DocumentService : IDocumentService
    {
        private readonly ApplicationDbContext _context;
        private readonly IStorageService _storageService;
        private readonly HttpClient _httpClient;

        /// <summary> Максимальное количество документов пользователя </summary>
        private int _maxDocsCount = 4;

        public DocumentService(ApplicationDbContext context, IStorageService storageService)
        {
            _context = context;
            _storageService = storageService;
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromMinutes(30); // Увеличен таймаут для отладки
        }

        public async Task<Document> UploadAsync(IFormFile file, Guid userId)
        {
            int userDocumentsCount = await GetUserDocumentCountAsync(userId);
            if (userDocumentsCount >= _maxDocsCount)
            {
                throw new InvalidOperationException($"Вы достигли максимального количества документов ({_maxDocsCount})");
            }

            // Проверяем, существует ли файл с таким именем и размером
            bool fileExists = await IsFileAlreadyExistsAsync(file.FileName, file.Length, userId);
            if (fileExists)
            {
                throw new InvalidOperationException("Файл с таким именем и размером уже существует");
            }

            string fileUrl = await _storageService.UploadFileAsync(file, $"user_{userId}"); // Загружаем файл в хранилище

            var document = new Document
            {
                Id = Guid.NewGuid(),
                Name = file.FileName,
                DocFileUrl = fileUrl,
                Size = file.Length,
                CreatedAt = DateTime.UtcNow,
                UserId = userId
            };

            await _context.Documents.AddAsync(document);
            await _context.SaveChangesAsync();

            await SendDocToDbPyApiAsync(file, document.Id);

            return document;
        }

        private async Task SendDocToDbPyApiAsync(IFormFile file, Guid documentId)
        {
            try
            {
                var apiUrl = "http://localhost:5000//py-api/new-doc";
                using var content = new MultipartFormDataContent();
                using var stream = file.OpenReadStream();
                content.Add(new StringContent(documentId.ToString()), "document_id");
                content.Add(new StreamContent(stream), "file", file.FileName);
                var response = await _httpClient.PostAsync(apiUrl, content);
            }
            catch (Exception ex)
            {
                // Логировать ошибку, но не прерывать основной процесс
                System.Diagnostics.Debug.WriteLine($"Ошибка при отправке файла в Python API: {ex.Message}");
            }
        }

        public async Task<IEnumerable<Document>> GetUserDocumentsAsync(Guid userId)
        {
            return await _context.Documents
                .Where(d => d.UserId == userId)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<DocumentView>> GetUserDocumentsWithOriginalityAsync(Guid userId)
        {
            // Получаем все документы пользователя
            var documents = await _context.Documents
                .Where(d => d.UserId == userId)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();

            var result = new List<DocumentView>();

            foreach (var doc in documents)
            {
                // Пытаемся найти результат проверки на плагиат для документа
                var checkResult = await _context.DocumentCheckResults
                    .Where(cr => cr.DocumentId == doc.Id)
                    .OrderByDescending(cr => cr.CheckedAt) // Берем самый последний результат проверки
                    .FirstOrDefaultAsync();

                var docDto = new DocumentView
                {
                    Id = doc.Id,
                    Name = doc.Name,
                    UploadDate = doc.CreatedAt,
                    // Если есть результат проверки - берем его, иначе ставим 100%
                    Originality = checkResult != null ? checkResult.Originality * 100 : 100
                };

                result.Add(docDto);
            }

            return result;
        }

        public async Task<Document> GetByIdAsync(Guid id)
        {
            return await _context.Documents.FindAsync(id) ?? throw new InvalidOperationException("Документ не был найден");
        }

        public async Task DeleteAsync(Guid docId, Guid userId)
        {
            var document = await _context.Documents
                .FirstOrDefaultAsync(d => d.Id == docId && d.UserId == userId) ?? throw new InvalidOperationException("Документ не найден или не принадлежит данному пользователю");

            await _storageService.DeleteFileAsync(document.DocFileUrl); // Удаляем файл из хранилища
            _context.Documents.Remove(document); // Удаляем запись из базы данных
            await _context.SaveChangesAsync();
        }

        public async Task<int> GetUserDocumentCountAsync(Guid userId)
        {
            return await _context.Documents.CountAsync(d => d.UserId == userId);
        }

        public async Task<bool> IsFileAlreadyExistsAsync(string fileName, long fileSize, Guid userId)
        {
            return await _context.Documents
                .AnyAsync(d => d.UserId == userId && d.Name == fileName && d.Size == fileSize);
        }

        /// <summary> Подсчитывает количество символов в документе</summary>
        public static int CountCharacters(Stream documentStream)
        {
            int totalCharacters = 0;

            // Запоминаем текущую позицию в потоке
            long originalPosition = documentStream.Position;

            try
            {
                // Сбрасываем позицию потока в начало
                documentStream.Position = 0;

                using (var document = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Open(documentStream, false))
                {
                    var body = document?.MainDocumentPart?.Document.Body;
                    if (body == null)
                        return 0;

                    // Получаем количество символов через ExtendedFilePropertiesPart, если возможно
                    var extProps = document?.ExtendedFilePropertiesPart;
                    if (extProps != null &&
                        extProps.Properties?.Characters != null &&
                        int.TryParse(extProps.Properties.Characters.Text, out int charCountFromProps))
                    {
                        totalCharacters = charCountFromProps;
                    }
                    else
                    {
                        // Fallback: считаем вручную
                        string fullText = string.Join("", body.Descendants<DocumentFormat.OpenXml.Wordprocessing.Text>().Select(t => t.Text));
                        totalCharacters = fullText.Length;
                    }
                }

                return totalCharacters;
            }
            finally
            {
                // Восстанавливаем исходную позицию потока
                documentStream.Position = originalPosition;
            }
        }
    }
}