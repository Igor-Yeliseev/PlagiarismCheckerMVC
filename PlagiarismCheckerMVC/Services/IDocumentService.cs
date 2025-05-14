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
        Task<IEnumerable<DocumentDTO>> GetUserDocumentsWithOriginalityAsync(Guid userId);
        Task<Document> GetByIdAsync(Guid id);
        Task DeleteAsync(Guid id, Guid userId);
        Task<int> GetDocumentCountByUserIdAsync(Guid userId);
    }
}