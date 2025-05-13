using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace PlagiarismCheckerMVC.Services
{
    public interface IStorageService
    {
        Task<string> UploadFileAsync(IFormFile file, string filePrefix);
        Task<Stream> DownloadFileAsync(string fileUrl);
        Task DeleteFileAsync(string fileUrl);
    }
} 