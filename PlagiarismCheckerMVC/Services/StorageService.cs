using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client.Platforms.Features.DesktopOs.Kerberos;
using PlagiarismCheckerMVC.Models;
using System.Diagnostics;

namespace PlagiarismCheckerMVC.Services
{
    public class StorageService : IStorageService
    {
        private readonly StorageClient _storageClient;
        private readonly string _bucketName;

        public StorageService(IOptions<CloudStorageSettings> options, IConfiguration configuration)
        {
            string keyPath = configuration["GoogleCloud:CredentialsPath"]; // Вариант 1: Путь через конфигурацию

            if (string.IsNullOrEmpty(keyPath)) // Вариант 2: Относительный путь к файлу в проекте
            {
                keyPath = Path.Combine(Directory.GetCurrentDirectory(), "credentials", "google-service-account.json");
            }

            if (!File.Exists(keyPath))
                throw new FileNotFoundException($"Файл ключа Google не найден по пути: {keyPath}");

            var credential = GoogleCredential.FromFile(keyPath);
            _storageClient = StorageClient.Create(credential);
            _bucketName = options.Value.BucketName;
        }

        public async Task<string> UploadFileAsync(IFormFile file, string filePrefix)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("Файл не может быть пустым");
            }

            string fileExtension = Path.GetExtension(file.FileName);
            string fileName = $"{filePrefix}_{Guid.NewGuid()}{fileExtension}";

            using (var stream = file.OpenReadStream())
            {
                await _storageClient.UploadObjectAsync(
                    _bucketName,
                    fileName,
                    null, // Определение типа контента из расширения файла
                    stream);
            }

            // Возвращаем URL файла
            return fileName;
        }

        public async Task<Stream> DownloadFileAsync(string fileUrl)
        {
            var memoryStream = new MemoryStream();

            await _storageClient.DownloadObjectAsync(
                _bucketName,
                fileUrl,
                memoryStream);

            memoryStream.Position = 0;
            return memoryStream;
        }

        public async Task DeleteFileAsync(string fileUrl)
        {
            await _storageClient.DeleteObjectAsync(_bucketName, fileUrl);
        }
    }
}