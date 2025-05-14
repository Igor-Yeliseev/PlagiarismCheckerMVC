using System;
using System.Text.Json.Serialization;

namespace PlagiarismCheckerMVC.Models
{
    public class DocumentDTO
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }
        
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        
        [JsonPropertyName("similarity")]
        public decimal Originality { get; set; } // Процент уникальности в формате от 0 до 100
        
        [JsonPropertyName("uploadDate")]
        public DateTime UploadDate { get; set; } // Дата загрузки документа
    }
} 