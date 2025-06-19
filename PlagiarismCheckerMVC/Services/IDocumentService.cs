using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using PlagiarismCheckerMVC.Models;

namespace PlagiarismCheckerMVC.Services
{
    public interface IDocumentService
    {
        Task<Document> UploadAsync(IFormFile file, Guid userId);
        Task<IEnumerable<Document>> GetUserDocumentsAsync(Guid userId);
        Task<IEnumerable<DocumentView>> GetUserDocumentsWithOriginalityAsync(Guid userId);
        Task<Document> GetByIdAsync(Guid id);
        Task DeleteAsync(Guid id, Guid userId);
        Task<int> GetUserDocumentCountAsync(Guid userId);
        Task<bool> IsFileAlreadyExistsAsync(string fileName, long fileSize, Guid userId);
    }
}