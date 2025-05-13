using System;
using System.IO;
using System.Threading.Tasks;
using PlagiarismCheckerMVC.Models;

namespace PlagiarismCheckerMVC.Services
{
    public interface IPlagiarismService
    {
        Task<DocCheckReport> CheckDocumentAsync(Guid documentId, SearchEngineType searchEngineType);
        DocCheckReport CheckDocument(Stream docStream, SearchEngineType searchEngineType = SearchEngineType.Google);
    }
} 