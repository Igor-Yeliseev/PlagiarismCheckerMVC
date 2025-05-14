using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using PlagiarismCheckerMVC.Models;

namespace PlagiarismCheckerMVC.Services
{
    public class DocumentService : IDocumentService
    {
        private readonly ApplicationDbContext _context;
        private readonly IStorageService _storageService;

        public DocumentService(ApplicationDbContext context, IStorageService storageService)
        {
            _context = context;
            _storageService = storageService;
        }

        public async Task<Document> UploadAsync(IFormFile file, Guid userId)
        {
            // Проверяем количество документов пользователя
            int userDocumentsCount = await GetDocumentCountByUserIdAsync(userId);
            if (userDocumentsCount >= 4)
            {
                throw new InvalidOperationException("Вы достигли максимального количества документов (4)");
            }

            // Загружаем файл в хранилище
            string fileUrl = await _storageService.UploadFileAsync(file, $"user_{userId}");

            // Создаем запись в базе данных
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

            return document;
        }

        public async Task<IEnumerable<Document>> GetUserDocumentsAsync(Guid userId)
        {
            return await _context.Documents
                .Where(d => d.UserId == userId)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<DocumentDTO>> GetUserDocumentsWithOriginalityAsync(Guid userId)
        {
            // Получаем все документы пользователя
            var documents = await _context.Documents
                .Where(d => d.UserId == userId)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();

            var result = new List<DocumentDTO>();

            foreach (var doc in documents)
            {
                // Пытаемся найти результат проверки на плагиат для документа
                var checkResult = await _context.DocumentCheckResults
                    .Where(cr => cr.DocumentId == doc.Id)
                    .OrderByDescending(cr => cr.CheckedAt) // Берем самый последний результат проверки
                    .FirstOrDefaultAsync();

                // Создаем DTO объект
                var docDto = new DocumentDTO
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
            return await _context.Documents.FindAsync(id);
        }

        public async Task DeleteAsync(Guid id, Guid userId)
        {
            var document = await _context.Documents
                .FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId);

            if (document == null)
            {
                throw new InvalidOperationException("Документ не найден или не принадлежит данному пользователю");
            }

            // Удаляем файл из хранилища
            await _storageService.DeleteFileAsync(document.DocFileUrl);

            // Удаляем запись из базы данных
            _context.Documents.Remove(document);
            await _context.SaveChangesAsync();
        }

        public async Task<int> GetDocumentCountByUserIdAsync(Guid userId)
        {
            return await _context.Documents.CountAsync(d => d.UserId == userId);
        }
    }
}